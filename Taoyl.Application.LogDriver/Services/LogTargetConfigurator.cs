using NLog.Config;
using NLog.Targets;
using NLog.Targets.Wrappers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver.Services
{
    /// <summary>
    /// 日志落盘目标配置器：NLog.config 载入后、logger 创建前，按 <see cref="ILogSettings"/>
    /// 对全部 FileTarget 做代码 overlay（config 只留结构，「值」由配置中心分发 + 本类覆盖）。
    ///
    /// overlay 边界（v5 §五 K1 如实交代）：
    /// - fileName / MaxArchiveDays / MaxArchiveFiles：必然覆盖（${var} 不递归展开、int 属性装不了字符串）；
    ///   🔴 MaxArchiveDays 覆盖成 0 不是「不保留」：目录分层后 NLog 的清理只扫当前文件所在那一层，
    ///   旧月份轮不到它 ⇒ 保留期整体移交 LogArchiveCleaner（单一删除者）；
    /// - layout：🔴 永不覆盖（锁死 CSV 后无任何赋值场景，CsvLayout 列结构 = XML 唯一真值源；
    ///   赋值会踩 TargetWithLayoutHeaderAndFooter.Layout setter 分支 2 的「切格式静默失效」坑）；
    /// - encoding：无条件应用（与 XML 默认形态一致，幂等）。
    ///
    /// 定位姿势（S0 第 0 号结论，运行时实证）：
    /// &lt;default-wrapper&gt; 会让 wrapper 顶替 target 名（内层 FileTarget 被改名 name_wrapped），
    /// 且 AllTargets 同时给出 wrapper 与其 wrapped ⇒ 必须两段式定位（先按 Target.Name 命中 → 再 Unwrap）+
    /// HashSet 去重；FindTargetByName&lt;FileTarget&gt; 确认必失败，禁用。
    ///
    /// 生效时机：LogConfig 任何改动一律要求进程重启；本类只在 Initialize 的
    /// 「config 载入之后、EnsureLoggers 之前」调用一次。
    /// </summary>
    public static class LogTargetConfigurator
    {
        #region 主流程

        /// <summary>
        /// 按日志设置对配置内全部通道的 FileTarget 做 overlay 与校验。
        /// 任一通道校验失败或定位失败即抛异常（fail fast，由调用方转入降级链路），
        /// 不允许「部分通道生效」的静默半态。
        /// </summary>
        /// <param name="config">已从 NLog.config 解析出的配置对象（尚未赋给 LogManager）</param>
        /// <param name="settings">日志全局设置（LogConfig，经 ILogSettings 中继）</param>
        /// <returns>归一后的日志根路径（供调用方注入 gdc 与幂等比对）</returns>
        public static string Apply(LoggingConfiguration config, ILogSettings settings)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            if (settings == null) throw new ArgumentNullException(nameof(settings));

            string root = ResolveRoot(settings.RootPath);

            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                ILogCategoryPolicy policy = settings.GetPolicy(category);
                ValidatePolicy(policy);

                string targetName = GetTargetName(category);
                FileTarget target = FindFileTarget(config, targetName);
                if (target == null)
                {
                    throw new InvalidOperationException(
                        $"NLog 配置中找不到通道 {category} 的 FileTarget（目标名 {targetName}），NLog.config 结构被改动或缺失。");
                }

                ApplyPolicy(target, root, settings.Encoding, policy);
            }

            return root;
        }

        /// <summary>
        /// 把单通道策略覆盖到 FileTarget（fileName / 保留期 / 防残留清零 / 编码）。
        /// </summary>
        private static void ApplyPolicy(FileTarget target, string root, LogFileEncoding encoding, ILogCategoryPolicy policy)
        {
            // fileName：{root}/{类别}/{yyyy}/{MM}[/{dd}]/{前缀}_{日期段}.csv（布局见 LogArchiveLayout）
            // 🔴 目录分层与「清理责任移交」必须成对改：加层后 NLog 够不着旧月份（见下面 MaxArchiveDays）
            string prefix = ResolvePrefix(policy);
            target.FileName = BuildFileName(root, policy, prefix);

            // 保留期：🔴 MaxArchiveDays 恒 0 —— 不是「不保留」，是把删除权交给 LogArchiveCleaner。
            // 否则 NLog 只清「当前文件所在目录」，旧月份静默堆积；两个删除者并存还会让「谁删的」无从追查。
            // MaxArchiveFiles = -1 让归档器的启用条件整体为 false，写路径零额外开销（硬结论 8）。
            target.MaxArchiveDays = 0;
            target.MaxArchiveFiles = -1;

            // 防残留清零：显式归零 XML 可能残留的归档配置（残留会把 handler 拉回 Legacy 路径，
            // 清理扫 archives/ 目录继续空转，见档案 N1）；
            // ArchiveOldFileOnStartup 必须 false——启动改名会造出 _01 序号文件，
            // 与「一天（或一小时）一个文件」的规则正面冲突，还会让清理器面对同一天的两份文件
            target.ArchiveEvery = FileArchivePeriod.None;
            target.ArchiveFileName = null;
            target.ArchiveAboveSize = -1;
            target.ArchiveOldFileOnStartup = false;

            ApplyEncoding(target, encoding);
        }

        /// <summary>
        /// 目录分层：按天 {root}/{类别}/{yyyy}/{MM}/，按小时再挂一层 {dd}/。
        /// 🔴 格式串全部取自 LogArchiveLayout（与清理器同源）；目录与文件名各渲染一次，但取同一个
        /// 事件时间戳，不会出现「目录进 11 月、文件名还是 1031」的错位。
        /// </summary>
        private static string BuildFileName(string root, ILogCategoryPolicy policy, string prefix)
        {
            string[] periods = LogArchiveLayout.PeriodFormats;
            bool hourly = policy.SplitPeriod == LogSplitPeriod.Hour;

            string directory = Path.Combine(root, policy.Category.ToString(),
                "${date:format=" + periods[0] + "}", "${date:format=" + periods[1] + "}");

            if (hourly)
                directory = Path.Combine(directory, "${date:format=" + periods[2] + "}");

            string stampFormat = hourly ? LogArchiveLayout.HourStampFormat : LogArchiveLayout.DayStampFormat;

            return Path.Combine(directory,
                prefix + LogArchiveLayout.StampSeparator
                       + "${date:format=" + stampFormat + "}" + LogArchiveLayout.CsvExtension);
        }

        #endregion

        #region 定位与解包

        /// <summary>
        /// 两段式按名定位 FileTarget（S0 第 0 号结论定案姿势）。
        /// 第一段：按 Target.Name 命中（default-wrapper 顶名 ⇒ 名字挂在 wrapper 上），命中即下钻；
        /// 第二段兜底：全量下钻后按 FileTarget 名匹配（含 _wrapped 改名形态）。
        /// </summary>
        private static FileTarget FindFileTarget(LoggingConfiguration config, string targetName)
        {
            foreach (Target target in config.AllTargets)
            {
                if (string.Equals(target.Name, targetName, StringComparison.OrdinalIgnoreCase))
                {
                    return UnwrapFileTarget(target);
                }
            }

            HashSet<FileTarget> found = CollectFileTargets(config);
            foreach (FileTarget file in found)
            {
                if (string.Equals(file.Name, targetName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(file.Name, targetName + "_wrapped", StringComparison.OrdinalIgnoreCase))
                {
                    return file;
                }
            }

            return null;
        }

        /// <summary>
        /// 收集配置内全部 FileTarget：递归下钻 wrapper + HashSet 去重
        ///（AllTargets 可能同时给出 wrapper 与其 wrapped，S0 实验 3 已实证）。
        /// </summary>
        private static HashSet<FileTarget> CollectFileTargets(LoggingConfiguration config)
        {
            var found = new HashSet<FileTarget>();
            foreach (Target target in config.AllTargets)
            {
                FileTarget file = UnwrapFileTarget(target);
                if (file != null)
                {
                    found.Add(file);
                }
            }
            return found;
        }

        /// <summary>
        /// 递归解包 NLog 包装目标（async/auto-flush 等），取出底层 FileTarget
        ///（复用 NLogLogService.UnwrapFileTarget 已实测有效的姿势）。
        /// </summary>
        private static FileTarget UnwrapFileTarget(Target target)
        {
            var fileTarget = target as FileTarget;
            if (fileTarget != null)
            {
                return fileTarget;
            }

            var wrapper = target as WrapperTargetBase;
            return wrapper != null ? UnwrapFileTarget(wrapper.WrappedTarget) : null;
        }

        #endregion

        #region 校验与拼接

        /// <summary>
        /// 日志根目录归一：空即拒绝（不再静默回退盘符根目录，fail fast 优于静默兜底）；
        /// 相对路径基于程序目录转绝对（保持 DevConfig.LogPath 时代兼容语义）。
        /// 🔴 internal：LogArchiveCleaner 复用同一份归一逻辑——两处各写一份，迟早走偏。
        /// </summary>
        internal static string ResolveRoot(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath))
            {
                throw new InvalidOperationException(
                    "日志根目录 RootPath 未配置（LogConfig.json 的 RootPath 为空），拒绝启动日志系统。");
            }

            if (Path.IsPathRooted(rootPath))
            {
                return rootPath;
            }

            return Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, rootPath));
        }

        /// <summary>
        /// 保留期下限校验：-1 = 不清理（合法）；0 与小于 -1 都没有意义——
        /// 0 会让清理器的 cutoff 停在今天、把今天之前的全判过期（等于「只留当天」，不是「不清理」）。
        /// 小时模式不限制保留期；
        /// 运维注意：按小时 ⇒ 单日目录 24 个文件、单月目录 ≈ 24 × 当月天数（清理只遍历文件名，毫秒级）。
        /// </summary>
        private static void ValidatePolicy(ILogCategoryPolicy policy)
        {
            if (policy.KeepDays == 0 || policy.KeepDays < -1)
            {
                throw new InvalidOperationException(
                    $"日志通道 {policy.Category} 的保留天数 KeepDays={policy.KeepDays} 非法（-1 = 不清理，或 ≥ 1 天）。");
            }
        }

        /// <summary>
        /// 解析文件名前缀：为空回退通道名；含非法字符即抛。
        /// 前缀必须是「干净的文件名字符」——它同时用作清理器的文件系统过滤（前缀*.csv）与归属判断，
        /// 混入路径分隔符或通配符会把清理范围放大到整个目录。
        /// 🔴 internal：LogArchiveCleaner 复用同一份前缀规则，两处各写一份必然走偏。
        /// </summary>
        internal static string ResolvePrefix(ILogCategoryPolicy policy)
        {
            string prefix = policy.FileNamePrefix;
            if (string.IsNullOrWhiteSpace(prefix))
            {
                return policy.Category.ToString();
            }

            foreach (char c in prefix)
            {
                bool legal = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
                if (!legal)
                {
                    throw new InvalidOperationException(
                        $"日志通道 {policy.Category} 的 FileNamePrefix 含非法字符 '{c}'：仅允许字母/数字/下划线。");
                }
            }

            return prefix;
        }

        /// <summary>
        /// 通道 → NLog target 名（与 NLog.config 骨架的 target 命名一一对应）。
        /// switch 显式列举 5 通道：加类别时编译器穷尽检查强制同步。
        /// </summary>
        private static string GetTargetName(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Debug: return "debugFile";
                case LogCategory.Info: return "infoFile";
                case LogCategory.Warn: return "warnFile";
                case LogCategory.Error: return "errorFile";
                case LogCategory.MES: return "mesFile";
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "未知的日志通道");
            }
        }

        /// <summary>
        /// 应用文件编码（三态，与 NLogLogService.ApplyFileEncoding 同语义；无条件应用 = 幂等）。
        /// </summary>
        private static void ApplyEncoding(FileTarget target, LogFileEncoding encoding)
        {
            switch (encoding)
            {
                case LogFileEncoding.Utf8Bom:
                    target.Encoding = Encoding.UTF8;
                    target.WriteBom = true;
                    break;
                case LogFileEncoding.Utf8:
                    target.Encoding = Encoding.UTF8;
                    target.WriteBom = false;
                    break;
                case LogFileEncoding.Gbk:
                    target.Encoding = Encoding.GetEncoding("GBK");
                    target.WriteBom = false;
                    break;
                default:
                    target.Encoding = Encoding.UTF8;
                    target.WriteBom = true;
                    break;
            }
        }

        #endregion
    }
}
