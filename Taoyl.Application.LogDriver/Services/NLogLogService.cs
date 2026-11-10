using NLog;
using NLog.Config;
using NLog.Targets;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.Share.Logging;
using LogLevel = Taoyl.Application.LogDriver.Models.LogLevel;

namespace Taoyl.Application.LogDriver.Services
{
    /// <summary>
    /// NLog 日志服务（<see cref="ILogService"/> 的默认实现）
    /// 全库共享单例枢纽：业务层 LogHelp 静态转发、UI 层 LogForm 事件订阅均指向同一实例，
    /// 保证「业务写入 → 文件 + UI 事件」链路不分裂。
    /// 公共 API 不暴露 NLog 类型，NLog 仅为内部实现细节。
    /// </summary>
    public class NLogLogService : ILogService
    {
        #region 单例

        private static readonly Lazy<NLogLogService> _instance =
            new Lazy<NLogLogService>(() => new NLogLogService());

        /// <summary>
        /// 共享单例（Autofac 注册与 LogHelp 转发均使用此实例）
        /// </summary>
        public static NLogLogService Instance => _instance.Value;

        #endregion

        #region 私有字段

        // 日志器字典（线程安全）
        private readonly ConcurrentDictionary<LogCategory, Logger> _loggers =
            new ConcurrentDictionary<LogCategory, Logger>();

        // 事件触发锁
        private readonly object _eventLock = new object();

        // 已初始化的日志根路径（幂等判断用）
        private string _initializedRootDir;

        // L3 紧急日志写锁（多线程兜底写入串行化）
        private readonly object _emergencyWriteLock = new object();

        // L3 紧急日志文件大小上限（10 MB，超限截断重写）
        private const long EmergencyFileMaxBytes = 10 * 1024 * 1024;

        #endregion

        /// <summary>
        /// 私有构造函数（无副作用：日志初始化由显式 <see cref="Initialize"/> 完成）
        /// </summary>
        private NLogLogService()
        {
        }

        #region 事件与配置（ILogService 实现）

        /// <summary>
        /// 日志写入事件（单一事件处理所有类别）
        /// </summary>
        public event EventHandler<LogEventArgs> LogWritten;

        /// <summary>
        /// 是否启用事件触发（默认为 true）
        /// </summary>
        public bool EnableEvents { get; set; } = true;

        /// <summary>
        /// 事件触发最小级别（默认 Info 及以上）
        /// </summary>
        public LogLevel MinimumEventLevel { get; set; } = LogLevel.Info;

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化日志系统：NLog.config 载入走「甲路径」（构造 → overlay → 赋值，S0 实验 1 定案——
        /// 乙路径的 Reload 会重新解析 XML 把 overlay 冲掉），overlay 由 <see cref="LogTargetConfigurator"/>
        /// 按 <see cref="ILogSettings"/>（LogConfig 经 Share 中继）执行。
        /// 🔴 LogConfig 任何改动一律要求进程重启生效（不做热生效）。
        /// 初始化失败时降级（三层降级：L2 最小配置承接 + L3 直接写盘兜底），不抛出（写路径永不抛）。
        /// </summary>
        public void Initialize(ILogSettings logSettings)
        {
            try
            {
                if (logSettings == null)
                    throw new ArgumentNullException(nameof(logSettings));

                // 根路径归一：相对路径基于程序目录转绝对（保持 DevConfig.LogPath 时代兼容语义）；
                // 为空由 LogTargetConfigurator 拒绝——不再静默回退盘符根目录（fail fast 优于静默兜底）
                string rootDir = logSettings.RootPath;
                if (!string.IsNullOrWhiteSpace(rootDir) && !Path.IsPathRooted(rootDir))
                {
                    rootDir = Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rootDir));
                }

                // 同一根路径重复初始化幂等短路。LogConfig 任何改动一律要求进程重启，
                // 运行期重放 overlay 无意义（旧版此处重放编码，是「配置双份真相」的来源，已随编码接线移除）
                if (_initializedRootDir != null &&
                    string.Equals(_initializedRootDir, rootDir, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }

                // 将日志根目录注入 NLog 全局上下文，供 NLog.config 中 ${gdc:item=logRoot} 默认形态使用
                //（fileName 必然被 LogTargetConfigurator 覆盖，此处保证 XML 单独工作时的自洽）
                GlobalDiagnosticsContext.Set("logRoot", rootDir);

                // 甲路径：构造（只 parse 不 Initialize）→ overlay → 赋值（此刻才 Initialize 全部 target）。
                // overlay 必须早于赋值：晚于则编码等属性存在被 XML 值覆盖回去的时序风险（F3 约束 C1）
                var config = new XmlLoggingConfiguration("NLog.config");
                LogTargetConfigurator.Apply(config, logSettings);
                LogManager.Configuration = config;

                // 确保所有日志器已创建（正常名；配置整体替换后 NLog 自动重匹配，S0 实验 4 已证）
                EnsureLoggersCreated();

                // 仅成功路径记录已初始化路径（失败时下次调用可重试，不被幂等短路）
                _initializedRootDir = rootDir;

                ScheduleArchiveCleanup(logSettings);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志系统初始化失败: {ex.Message}");
                Console.WriteLine($"堆栈跟踪: {ex.StackTrace}");
                WriteToEmergencyLog($"日志系统初始化失败: {ex.Message}");
                SetupFallbackConfiguration();
                // 🔴 F1 修复：失败不再创建 Fallback.{category} logger——GetOrAdd 不覆盖已存在键，
                // 污染会永续（即使后续初始化成功也恒走 Fallback.*）。_loggers 保持正常名，
                // 下次 Initialize 成功后日志自动回到正常通道（NLog 按名懒匹配 + 自动重匹配）；
                // 降级期间由 L2 最小配置承接日志，L3 直接写盘兜底（三层降级，每层只有一个写入者）。
            }
        }

        /// <summary>
        /// 启动后清一次过期归档（目录分层后 NLog 只扫当前一层，删除权已移交 <see cref="LogArchiveCleaner"/>）。
        /// 🔴 异步且不等待：不阻塞启动，也不参与初始化的成败——清理失败与日志系统无关。
        /// 🔴 不挂定时器：LogConfig 改动必重启，配置变更天然被下次启动覆盖；
        /// 进程长期不重启只是晚几天清理，不会留下错误状态。
        /// </summary>
        private void ScheduleArchiveCleanup(ILogSettings settings)
        {
            Task.Run(() =>
            {
                LogCleanResult result = new LogArchiveCleaner().Clean(settings);

                if (result.Error != null)
                {
                    // 清理没跑完 = 日志可能从此不再被删（磁盘只涨不落），必须留痕：
                    // 带堆栈进 Error 通道，别让它安静地停摆
                    Error(result.Error, "日志清理未完成，可能跳过了部分目录");
                }

                if (!result.HasChanges)
                    return;

                // 此刻配置已生效、logger 已建；写失败由 LogInternal 自己兜住（写路径永不抛）。
                // 清单走 Info 而不是另开通道：上屏能看见、落盘可查，一条就够
                Info("日志清理：删除 {0} 个过期文件 / {1} 个空目录，失败 {2} 个 → {3}",
                    result.FilesDeleted, result.DirsDeleted, result.FilesFailed, FormatDeletedNames(result));
            });
        }

        /// <summary>删除清单压成一行：够定位是哪一批，又不至于让一条日志刷出几千字符。</summary>
        private static string FormatDeletedNames(LogCleanResult result)
        {
            if (result.DeletedNames.Count == 0)
                return "(无)";

            string names = string.Join("; ", result.DeletedNames);
            int missing = result.FilesDeleted - result.DeletedNames.Count;
            return missing > 0 ? names + "; …另 " + missing + " 个" : names;
        }

        /// <summary>
        /// 关闭日志系统：flush 异步队列残留（K2 连锁——AsyncTargetWrapper 的 flush 需经
        /// <see cref="LogManager.Shutdown"/> 触发，退出不调用会丢行）。
        /// 在进程退出前（主窗体消息循环结束后）由组合根调用一次。
        /// </summary>
        public void Shutdown()
        {
            try
            {
                LogManager.Shutdown();
            }
            catch
            {
                // 关闭失败不抛——退出路径上没有更外层的兜底
            }
        }

        /// <summary>
        /// 确保所有类别的日志器已创建
        /// </summary>
        private void EnsureLoggersCreated()
        {
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                _loggers.GetOrAdd(category, c =>
                {
                    // 使用带类别的 Logger 名称，便于 NLog 配置规则匹配
                    string loggerName = $"LogDriver.{c}";
                    return LogManager.GetLogger(loggerName);
                });
            }
        }

        #endregion

        #region 通道日志（通道即方法名，级别是记录的数据字段，不参与对外契约）

        /// <summary>记录 Debug 通道日志（落盘，UI 暂不显示）</summary>
        public void Debug(string message, params object[] args)
        {
            LogInternal(LogCategory.Debug, LogLevel.Debug, message, args, null);
        }

        /// <summary>记录 Info 级别日志</summary>
        public void Info(string message, params object[] args)
        {
            LogInternal(LogCategory.Info, LogLevel.Info, message, args, null);
        }

        /// <summary>记录 Warn 级别日志</summary>
        public void Warn(string message, params object[] args)
        {
            LogInternal(LogCategory.Warn, LogLevel.Warn, message, args, null);
        }

        /// <summary>记录 Error 级别日志（message 为空时使用 ex.Message）</summary>
        public void Error(Exception ex, string message = null, params object[] args)
        {
            if (message != null)
            {
                LogInternal(LogCategory.Error, LogLevel.Error, message, args, ex);
            }
            else
            {
                LogInternal(LogCategory.Error, LogLevel.Error, ex.Message, null, ex);
            }
        }

        #endregion

        #region 业务类别日志

        /// <summary>记录 MES 通道日志（记录级别恒 Info，无级别重载）</summary>
        public void MES(string message, params object[] args)
        {
            LogInternal(LogCategory.MES, LogLevel.Info, message, args, null);
        }

        #endregion

        #region 核心日志记录逻辑

        /// <summary>
        /// 核心日志记录：取通道 logger → 构造日志事件 → NLog 落盘 → 触发 UI 事件。
        /// 实体链已摘除（所有信息由调用方转成字符串填入 message）；
        /// message/args 交给 NLog 格式化，模板写错导致的格式化异常由本方法 catch 接住转 emergency
        /// 留痕（fail fast 优于静默降级）。
        /// NLog 6.0 兼容：必须通过 LogEventInfo 构造函数传入所有参数，属性随后不可修改。
        /// 🔴 Message 是**原始模板**（带 {0}），FormattedMessage 才是代入参数后的成品：
        /// 落盘走 ${message}（渲染 FormattedMessage），上屏必须同样取 FormattedMessage，
        /// 否则界面显示模板、文件显示结果，两边对不上（11-04 实测踩到）。
        /// </summary>
        private void LogInternal(
            LogCategory category,
            LogLevel level,
            string message,
            object[] args,
            Exception exception)
        {
            try
            {
                if (!_loggers.TryGetValue(category, out Logger logger))
                {
                    logger = LogManager.GetLogger($"LogDriver.{category}");
                    _loggers[category] = logger;
                }

                NLog.LogLevel nlogLevel = ToNLogLevel(level);

                var logEvent = new LogEventInfo(nlogLevel, logger.Name, null, message, args, exception);
                logger.Log(logEvent);

                if (EnableEvents && level >= MinimumEventLevel)
                {
                    // FormattedMessage 而不是 Message：后者是原始模板，上屏会带 {0}
                    TriggerEvent(category, level, logEvent.FormattedMessage);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"日志记录失败: {ex.Message}");
                WriteToEmergencyLog($"日志记录失败: {ex.Message}\n原始消息: {message}");
            }
        }

        /// <summary>
        /// 自建日志级别映射为 NLog 日志级别
        /// </summary>
        private static NLog.LogLevel ToNLogLevel(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.Debug: return NLog.LogLevel.Debug;
                case LogLevel.Info: return NLog.LogLevel.Info;
                case LogLevel.Warn: return NLog.LogLevel.Warn;
                case LogLevel.Error: return NLog.LogLevel.Error;
                default: return NLog.LogLevel.Info;
            }
        }

        /// <summary>
        /// 触发日志事件（强类型调用，单个订阅者失败不影响其他订阅者）
        /// </summary>
        private void TriggerEvent(
            LogCategory category,
            LogLevel level,
            string logMessage)
        {
            if (LogWritten == null)
                return;

            try
            {
                // 创建事件参数（快照字段：禁读活对象，见 LogForm.FormatLine 硬约束）
                var eventArgs = new LogEventArgs
                {
                    Category = category,
                    Level = level,
                    Message = logMessage,
                    Timestamp = DateTime.Now
                };

                // 线程安全地触发事件
                lock (_eventLock)
                {
                    var handlers = LogWritten.GetInvocationList();
                    if (handlers.Length > 0)
                    {
                        foreach (var handler in handlers)
                        {
                            try
                            {
                                ((EventHandler<LogEventArgs>)handler)(null, eventArgs);
                            }
                            catch
                            {
                                // 单个订阅者失败不影响其他订阅者
                            }
                        }
                    }
                }
            }
            catch
            {
                // 事件触发失败不影响主流程
            }
        }

        #endregion

        #region 工具方法（LogHelp 兼容转发）

        /// <summary>
        /// 获取所有日志类别
        /// </summary>
        public LogCategory[] GetAllCategories()
        {
            return (LogCategory[])Enum.GetValues(typeof(LogCategory));
        }

        /// <summary>
        /// 清空事件订阅（谨慎使用）
        /// </summary>
        public void ClearEventSubscriptions()
        {
            LogWritten = null;
        }

        #endregion

        #region 紧急日志

        /// <summary>
        /// L3 最后防线：绕开 NLog 直接写盘（连最小配置都设不进去时用）。
        /// 与 L2 写不同文件（emergency-fallback.log）——每层只有一个写入者，结构消除句柄互抢；
        /// 加锁 + 限长（超限截断重写，紧急日志只关心最近发生了什么）。
        /// </summary>
        private void WriteToEmergencyLog(string message)
        {
            try
            {
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs");
                string path = Path.Combine(dir, "emergency-fallback.log");
                lock (_emergencyWriteLock)
                {
                    if (!Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }

                    if (File.Exists(path) && new FileInfo(path).Length > EmergencyFileMaxBytes)
                    {
                        File.Delete(path);
                    }

                    File.AppendAllText(path,
                        $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {message}{Environment.NewLine}");
                }
            }
            catch
            {
                // 最后的手段：控制台输出
                Console.WriteLine($"紧急日志写入失败: {message}");
            }
        }

        /// <summary>
        /// L2 降级：主配置加载 / overlay 失败时，设置「最小 File 配置」（单 target + 单 rule，
        /// 写 logs/emergency.log）——让日志还有地方落，且用正常 logger 名（NLog 按名懒匹配，
        /// 主配置修复后自动回到正常通道，S0 实验 4 已证）。设失败时静默，兜底交给 L3。
        /// </summary>
        private void SetupFallbackConfiguration()
        {
            try
            {
                var config = new LoggingConfiguration();

                var emergencyTarget = new FileTarget("emergencyFile")
                {
                    FileName = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "logs", "emergency.log"),
                    Layout = "${longdate}|EMERGENCY|${level:uppercase=true}|${message}${exception:format=ToString}",
                    KeepFileOpen = false,
                    Encoding = Encoding.UTF8,
                    WriteBom = true
                };
                config.AddTarget(emergencyTarget);

                var rule = new LoggingRule("LogDriver.*", NLog.LogLevel.Trace, NLog.LogLevel.Fatal, emergencyTarget)
                {
                    Final = true
                };
                config.LoggingRules.Add(rule);

                LogManager.Configuration = config;
            }
            catch
            {
                // 连 L2 都设不进去 ⇒ 兜底只剩 L3（WriteToEmergencyLog 直接写盘）
            }
        }

        #endregion
    }
}
