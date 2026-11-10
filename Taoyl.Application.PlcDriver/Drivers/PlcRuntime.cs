using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.IO;
using Taoyl.Application.PlcDriver.Logging;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Cache;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.PlcDriver.Protocols;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Taoyl.Application.PlcDriver.Config;

namespace Taoyl.Application.PlcDriver.Drivers
{
    /// <summary>
    /// PLC驱动运行时基类（原 PlcBase 演进，第二轮重构瘦身）
    /// 职责：业务API薄壳 + 装配三组件（PlcConnectionState/IoExecutor/IoWorker）+ Connect/Close（缓存模式=IoWorker 生命周期内在部分）
    /// 地址表不持有：集中归 ConfigManager（两层CD），配置加载阶段已就绪，每次经 IPlcConfigManager.GetTagTable 现取（改即生效，无 Reload）
    /// 执行统一走 IoExecutor.ExecuteSession（直接读写=true 同步返 / 缓存写=入队 IoWorker 会话=false）
    /// 读写结果统一 ResponseResult：失败返回 Fail（携带 ErrorCode/Message），不抛异常，调用方自行判断
    /// 连接状态与重连全在 PlcConnectionState（防抖N=3 + 退避 3s×2^n cap 90s，触发仅 IoWorker 循环）
    /// </summary>
    public abstract class PlcRuntime : IPlc, IDisposable
    {
        #region 成员变量
        protected readonly PlcConfig _plcConfig;
        protected readonly IPlcTransport _transport;
        protected readonly IPlcProtocolAccess _protocolAccess;
        protected readonly IPlcConfigManager _configManager;
        protected readonly IPlcLogger _logger;

        // IO 引擎三组件（装配：构造内创建，地址表不缓存）
        private readonly PlcConnectionState _connectionState;
        private readonly IoExecutor _ioExecutor;
        private IoWorker _ioWorker;
        private CancellationTokenSource _ioCts;
        private readonly int _pollingTime;

        private volatile bool _disposed = false;
        private readonly object _connectLock = new object();

        /// <summary>IoWorker 启停互斥锁（防并发启动双 IoWorker）</summary>
        private readonly object _cacheLock = new object();
        #endregion

        #region IPlc接口属性
        /// <summary>
        /// PLC名称（构造时定格，地址表 GetTagTable 键/日志文件名）
        /// </summary>
        public string Name => _plcConfig.Name;

        /// <summary>
        /// 协议类型（取配置原始串，与 Devices.json 的 protocol 字段同源，即 PlcProtocol 成员名；
        /// 供外部诊断/日志识别实例协议）
        /// </summary>
        public string ProtocolType => _plcConfig.Protocol;

        /// <summary>
        /// 是否已连接（PlcConnectionState 维护）
        /// </summary>
        public bool IsConnected => _connectionState.IsConnected;

        /// <summary>
        /// 缓存读写队列深度（透传 IoExecutor，调试界面/监控用）
        /// </summary>
        public int QueueDepth => _ioExecutor.QueueDepth;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数（3参：去 transport 参数，从 protocolAccess.Communication 取）
        /// 地址表由配置加载阶段（ConfigManager.LoadJsonConfigForPlc）顺带加载，此处不解析
        /// </summary>
        /// <param name="plcConfig">PLC配置</param>
        /// <param name="protocolAccess">协议层访问（策略字典 + Communication 传输原语）</param>
        /// <param name="configManager">配置管理器（地址表唯一持有）</param>
        protected PlcRuntime(PlcConfig plcConfig, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager)
        {
            _plcConfig = plcConfig ?? throw new ArgumentNullException(nameof(plcConfig));
            _protocolAccess = protocolAccess ?? throw new ArgumentNullException(nameof(protocolAccess));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));

            // 传输原语从协议层取（冗余参数消除）
            _transport = _protocolAccess.Communication ?? throw new ArgumentNullException(nameof(protocolAccess));

            _logger = CreateLogger();
            _pollingTime = int.TryParse(plcConfig.PollingTime, out var time) ? Math.Max(20, time) : 100;

            // 装配三组件（连接状态机 + IO执行器；IoWorker 由 Connect 首次触发 EnsureIoWorkerRunning 创建；重连参数经 _plcConfig 配置）
            _connectionState = new PlcConnectionState(_transport, _logger, _plcConfig);
            _ioExecutor = new IoExecutor(_plcConfig.Name, _protocolAccess, _configManager, _connectionState, _logger);

            // 注意：不在构造函数中启动 IO 工作器，首次 Connect（成败不论）时启动——成功=轮询刷新，失败=自动重连守护接管
            _logger.WriteInfo($"PLC实例初始化完成: {_plcConfig.Name}, 轮询间隔={_pollingTime}ms");
        }
        #endregion

        #region 日志管理
        /// <summary>
        /// 创建PLC实例级日志（默认INFO级别）
        /// 目录解析与 ConfigManager 同规则：配置 LogPath 绝对路径优先；未配置/相对默认挂程序所在盘根（Logs/PLC/{Name}）
        /// 注意：PlcLogger 收目录（HSL 在目录内生成 Logs_yyyyMMdd_HH.txt），此处不再拼 PLC_{Name}.log 文件名
        /// </summary>
        protected virtual IPlcLogger CreateLogger()
        {
            var logDirectory = PlcLogger.ResolveDirectory(_plcConfig.LogPath, Path.Combine("Logs", "PLC", _plcConfig.Name));
            Directory.CreateDirectory(logDirectory);

            var logger = new PlcLogger(logDirectory);

            logger.WriteInfo("═══════════════════════════════════════");
            logger.WriteInfo($"PLC实例启动: {_plcConfig.Name}");
            logger.WriteInfo($"连接端点: {_plcConfig.EndpointText}");
            logger.WriteInfo($"协议类型: {_plcConfig.Protocol}");
            logger.WriteInfo($"日志路径: {logDirectory}");
            logger.WriteInfo("═══════════════════════════════════════");

            return logger;
        }
        #endregion

        #region 直接读写（ExecuteSession(req, true) 同步返，失败返回 Fail 不抛异常）
        /// <summary>
        /// 执行通用IO请求（薄壳：固定同步直读 ExecuteSession(request, true)，供调试界面等统一读写入口）
        /// 读成功值由策略闭包写入 tag.TypedValue（请求携带 GroupName/TagName 时），UI 重绘即见；
        /// 写成功值由 Commit 回写容器并补三件套
        /// </summary>
        public ResponseResult ExecuteIo(IoRequest request)
        {
            try
            {
                return _ioExecutor.ExecuteSession(request, true);
            }
            catch (Exception ex)
            {
                _logger.WriteException($"执行IO请求失败: {request?.Address}", ex);
                return ResponseResult.Fail($"执行IO请求异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取单个标签值（泛型，立即读取）
        /// </summary>
        public ResponseResult<T> ReadTag<T>(string groupName, string tagName)
        {
            try
            {
                var tag = FindTag(groupName, tagName);
                if (tag == null)
                    return ResponseResult<T>.Fail($"标签不存在: {groupName}.{tagName}");

                var request = new IoRequest(IoRequestKind.Read, tag.Address, tag.DataType, tag.Length, null, groupName, tagName);
                var result = _ioExecutor.ExecuteSession(request, true);

                if (!result.IsSuccess)
                    return ResponseResult<T>.Fail(result.Message, result.ErrorCode);

                if (result is ResponseResult<T> typed)
                    return typed;

                return ResponseResult<T>.Fail($"返回类型与请求类型不匹配: {tag.DataType}");
            }
            catch (Exception ex)
            {
                _logger.WriteException($"读取标签失败: {groupName}.{tagName}", ex);
                return ResponseResult<T>.Fail($"读取标签异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取整个标签组（立即读取）
        /// Success=执行完成（组内失败标签以 Message/默认值占位）；Fail=组不存在或执行异常
        /// </summary>
        public ResponseResult<Dictionary<string, object>> ReadGroup(string groupName)
        {
            try
            {
                var tagTable = _configManager.GetTagTable(_plcConfig.Name);
                if (tagTable == null || !tagTable.TryGetValue(groupName, out var tagGroup))
                    return ResponseResult<Dictionary<string, object>>.Fail($"标签组不存在: {groupName}");

                var result = new Dictionary<string, object>();
                foreach (var kvp in tagGroup.Tags)
                {
                    try
                    {
                        var tag = kvp.Value;

                        var request = new IoRequest(IoRequestKind.Read, tag.Address, tag.DataType, tag.Length, null, groupName, kvp.Key);
                        var readResult = _ioExecutor.ExecuteSession(request, true);

                        if (readResult.IsSuccess)
                        {
                            // 值已由策略闭包写入 tag.TypedValue（ExecuteSession 内），此处仅取 object 视图装返回字典
                            var value = readResult is IResponseResultContent content ? content.ContentObject : null;
                            result[kvp.Key] = value;
                        }
                        else
                        {
                            result[kvp.Key] = readResult.Message;
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.WriteException($"读取标签失败: {kvp.Key}", ex);
                        result[kvp.Key] = GetDefaultValue(kvp.Value.DataType);
                    }
                }
                return ResponseResult<Dictionary<string, object>>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.WriteException($"读取组失败: {groupName}", ex);
                return ResponseResult<Dictionary<string, object>>.Fail($"读取组异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 写入单个标签值（立即写入）
        /// </summary>
        public ResponseResult WriteTag(string groupName, string tagName, object value)
        {
            try
            {
                var tag = FindTag(groupName, tagName);
                if (tag == null)
                    return ResponseResult.Fail($"标签不存在: {groupName}.{tagName}");

                var request = new IoRequest(IoRequestKind.Write, tag.Address, tag.DataType, tag.Length, value, groupName, tagName);
                var result = _ioExecutor.ExecuteSession(request, true);   // Commit 已负责写成功回写容器

                return result;
            }
            catch (Exception ex)
            {
                _logger.WriteException($"写入标签失败: {groupName}.{tagName}", ex);
                return ResponseResult.Fail($"写入标签异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 写入整个标签组（立即写入，任一标签失败即 Fail）
        /// </summary>
        public ResponseResult WriteGroup(string groupName, Dictionary<string, object> values)
        {
            try
            {
                var tagTable = _configManager.GetTagTable(_plcConfig.Name);
                if (tagTable == null || !tagTable.TryGetValue(groupName, out var tagGroup))
                    return ResponseResult.Fail($"标签组不存在: {groupName}");

                foreach (var kvp in values)
                {
                    if (!tagGroup.Tags.TryGetValue(kvp.Key, out var tag))
                        return ResponseResult.Fail($"标签不存在: {groupName}.{kvp.Key}");

                    var request = new IoRequest(IoRequestKind.Write, tag.Address, tag.DataType, tag.Length, kvp.Value, groupName, kvp.Key);
                    var result = _ioExecutor.ExecuteSession(request, true);   // Commit 已负责写成功回写容器

                    if (!result.IsSuccess)
                    {
                        return ResponseResult.Fail($"写入标签[{kvp.Key}]失败: {result.Message}", result.ErrorCode);
                    }
                }
                return ResponseResult.Success();
            }
            catch (Exception ex)
            {
                _logger.WriteException($"写入组失败: {groupName}", ex);
                return ResponseResult.Fail($"写入组异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 原始读取（动态地址，不依赖标签配置）
        /// </summary>
        public ResponseResult<T> ReadRaw<T>(string address, PlcDataType dataType)
        {
            try
            {
                var request = new IoRequest(IoRequestKind.Read, address, dataType, 1, null, null, null);
                var result = _ioExecutor.ExecuteSession(request, true);

                if (!result.IsSuccess)
                    return ResponseResult<T>.Fail(result.Message, result.ErrorCode);

                if (result is ResponseResult<T> typed)
                {
                    // Content 已是 T（策略按 DataType 路由返回对应泛型，类型不匹配早已 Fail），无需跨类型转换
                    return typed;
                }

                return ResponseResult<T>.Fail($"返回类型与请求类型不匹配: {dataType}");
            }
            catch (Exception ex)
            {
                _logger.WriteException($"原始读取异常: {address}", ex);
                return ResponseResult<T>.Fail($"原始读取异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 原始写入（动态地址，不依赖标签配置）
        /// </summary>
        public ResponseResult WriteRaw(string address, object value, PlcDataType dataType)
        {
            try
            {
                var request = new IoRequest(IoRequestKind.Write, address, dataType, 1, value, null, null);
                return _ioExecutor.ExecuteSession(request, true);
            }
            catch (Exception ex)
            {
                _logger.WriteException($"原始写入异常: {address}", ex);
                return ResponseResult.Fail($"原始写入异常: {ex.Message}");
            }
        }
        #endregion

        #region 缓存读写（表访问，地址表现取）
        /// <summary>
        /// 从缓存读取单个标签值（泛型；deepCopy=true 数组深拷贝保护，默认）
        /// 优先走强类型容器 TypedValueHolder&lt;T&gt;（TryGet 零拆箱）；容器类型不匹配（罕见，如 DataType 外部变更未重建）兜底 object 转换
        /// Fail=标签不存在/值未刷新/类型转换失败
        /// </summary>
        public ResponseResult<T> ReadTagCached<T>(string groupName, string tagName, bool deepCopy = true)
        {
            try
            {
                var tag = FindTag(groupName, tagName);
                if (tag == null)
                    return ResponseResult<T>.Fail($"标签不存在: {groupName}.{tagName}");

                // 强类型容器优先（零拆箱；hasValue=false=未刷新）
                if (tag.TypedValue is TypedValueHolder<T> holder)
                {
                    if (!holder.TryGet(out var value))
                        return ResponseResult<T>.Fail($"标签值尚未刷新: {groupName}.{tagName}");

                    // 数组深拷贝保护（防业务层修改污染缓存；Clone 为引用复制非装箱；deepCopy=false 时零拷贝由调用方负责）
                    if (deepCopy && value is Array arr)
                    {
                        return ResponseResult<T>.Success((T)arr.Clone());
                    }
                    return ResponseResult<T>.Success(value);
                }

                // 兜底：容器类型不匹配（罕见）走旧 object 逻辑
                if (tag.Value == null)
                    return ResponseResult<T>.Fail($"标签值尚未刷新: {groupName}.{tagName}");
                if (deepCopy && typeof(T).IsArray && tag.Value is Array arr2)
                {
                    return ResponseResult<T>.Success((T)arr2.Clone());
                }
                if (tag.Value is T typedValue)
                {
                    return ResponseResult<T>.Success(typedValue);
                }
                return ResponseResult<T>.Success((T)Convert.ChangeType(tag.Value, typeof(T)));
            }
            catch (Exception ex)
            {
                _logger.WriteException($"读取缓存失败: {groupName}.{tagName}", ex);
                return ResponseResult<T>.Fail($"读取缓存异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 尝试从缓存读取单个标签值（无结果对象开销，适合高频读取；数组不深拷贝由调用方负责）
        /// 优先走强类型容器（TryGet 零拆箱零分配）；容器类型不匹配兜底 object 转换
        /// </summary>
        public bool TryReadTagCached<T>(string groupName, string tagName, out T value)
        {
            value = default;
            try
            {
                var tag = FindTag(groupName, tagName);
                if (tag == null)
                    return false;

                // 强类型容器优先（零拆箱；hasValue=false 返回 false=未刷新）
                if (tag.TypedValue is TypedValueHolder<T> holder)
                {
                    return holder.TryGet(out value);
                }

                // 兜底：容器类型不匹配走旧 object 逻辑
                if (tag.Value == null)
                    return false;

                if (tag.Value is T typedValue)
                {
                    value = typedValue;
                    return true;
                }

                value = (T)Convert.ChangeType(tag.Value, typeof(T));
                return true;
            }
            catch (Exception)
            {
                value = default;
                return false;
            }
        }

        /// <summary>
        /// 从缓存读取整个标签组（返回深拷贝：数组 Clone，其余值类型/string 不可变直接引用）
        /// </summary>
        public ResponseResult<Dictionary<string, object>> ReadGroupCached(string groupName)
        {
            try
            {
                var tagTable = _configManager.GetTagTable(_plcConfig.Name);
                if (tagTable != null && tagTable.TryGetValue(groupName, out var tagGroup))
                {
                    var result = new Dictionary<string, object>();
                    foreach (var kvp in tagGroup.Tags)
                    {
                        result[kvp.Key] = DeepCopyValue(kvp.Value.Value);
                    }
                    return ResponseResult<Dictionary<string, object>>.Success(result);
                }
                return ResponseResult<Dictionary<string, object>>.Fail($"标签组不存在: {groupName}");
            }
            catch (Exception ex)
            {
                _logger.WriteException($"读取组缓存失败: {groupName}", ex);
                return ResponseResult<Dictionary<string, object>>.Fail($"读取组缓存异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 写入单个标签值到缓存（入队异步写PLC，IoWorker 会话执行）
        /// </summary>
        public ResponseResult WriteTagCached<T>(string groupName, string tagName, T value)
        {
            try
            {
                var tag = FindTag(groupName, tagName);
                if (tag == null)
                    return ResponseResult.Fail($"标签不存在: {groupName}.{tagName}");

                var request = new IoRequest(IoRequestKind.Write, tag.Address, tag.DataType, tag.Length, value, groupName, tagName);
                if (!_ioExecutor.TryEnqueue(request))
                {
                    return ResponseResult.Fail($"写入队列失败（重复或队列满）: {groupName}.{tagName}");
                }
                return ResponseResult.Success();
            }
            catch (Exception ex)
            {
                _logger.WriteException($"写入缓存失败: {groupName}.{tagName}", ex);
                return ResponseResult.Fail($"写入缓存异常: {ex.Message}");
            }
        }

        #endregion

        #region 纯表访问（IPlcReadWrite 接口实现：本地表值，不走队列不通讯）
        /// <summary>
        /// 纯表读：获取标签值（不走队列不通讯）
        /// 优先走强类型容器（零拆箱）；未刷新返回 default（等价旧 Value==null → Success(default) 语义）；
        /// 数组返回克隆副本（与 ReadTagCached 默认 deepCopy 对齐，防业务/UI 修改污染缓存）
        /// </summary>
        public ResponseResult<T> GetValue<T>(string groupName, string tagName)
        {
            var tag = FindTag(groupName, tagName);
            if (tag == null)
                return ResponseResult<T>.Fail($"标签不存在: {groupName}.{tagName}");

            try
            {
                // 强类型容器优先（未刷新 TryGet false → value=default）
                if (tag.TypedValue is TypedValueHolder<T> holder)
                {
                    if (holder.TryGet(out var holderValue) && holderValue is Array arr)
                    {
                        // 数组返回克隆副本（防调用方修改数组元素污染缓存）
                        return ResponseResult<T>.Success((T)arr.Clone());
                    }
                    return ResponseResult<T>.Success(holderValue);
                }

                // 兜底：容器类型不匹配走旧 object 逻辑
                if (tag.Value == null)
                    return ResponseResult<T>.Success(default);

                T value = tag.Value is T typed ? typed : (T)Convert.ChangeType(tag.Value, typeof(T));
                return ResponseResult<T>.Success(value);
            }
            catch (Exception ex)
            {
                return ResponseResult<T>.Fail($"值类型转换失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 纯表写：更新标签值（不走队列不通讯，由业务层自行决定何时写PLC）
        /// 成功后同步 LastOperation 三件套（Time/Result/Msg，不 +RWTimes——纯表写无 IO 不计数）；
        /// 注意：LastOperationTime 同时是轮询到期基准，SetValue 后 RefreshFreq&gt;0 标签的下一轮读顺延一个周期（值刚手动更新，属合理）
        /// </summary>
        public ResponseResult SetValue(string groupName, string tagName, object value)
        {
            var tag = FindTag(groupName, tagName);
            if (tag == null)
                return ResponseResult.Fail($"标签不存在: {groupName}.{tagName}");

            tag.TypedValue.SetObject(value);   // 纯表写统一经容器（object 低频通道）

            tag.LastOperationTime = DateTime.Now;
            tag.LastOperationResult = true;
            tag.LastOperationMsg = string.Empty;

            return ResponseResult.Success();
        }
        #endregion

        #region 连接管理（IPlc接口实现）
        /// <summary>
        /// 连接PLC（连接尝试后无论成败启动 IoWorker——成功=轮询刷新，失败=自动重连守护接管，重连触发仅 IoWorker 循环）
        /// </summary>
        public bool Connect()
        {
            lock (_connectLock)
            {
                try
                {
                    _logger.WriteInfo($"正在连接PLC: {_plcConfig.EndpointText}");
                    var result = _connectionState.Connect();

                    if (_connectionState.IsConnected)
                    {
                        _logger.WriteInfo($"连接成功: {_plcConfig.EndpointText}");
                    }
                    else
                    {
                        _logger.WriteError($"连接失败: {result.Message}");
                    }
                    return _connectionState.IsConnected;
                }
                catch (Exception ex)
                {
                    _logger.WriteException($"连接异常: {ex.Message}", ex);
                    _connectionState.UpdateConnectionFromResult(ResponseResult.Fail($"连接异常: {ex.Message}"));
                    return false;
                }
                finally
                {
                    // 成败不论都确保 IoWorker 运行（幂等，见 EnsureIoWorkerRunning）
                    EnsureIoWorkerRunning();
                }
            }
        }

        /// <summary>
        /// 断开PLC连接（停 IoWorker → 清空待执行队列[会话终结语义，防下次 Connect 补发过期写] → 关通讯 → 状态归位）
        /// 再次 Connect 会启动全新 IoWorker（旧已停并置 null），非并存
        /// </summary>
        public void Close()
        {
            lock (_connectLock)
            {
                try
                {
                    _logger.WriteInfo($"正在关闭PLC连接: {_plcConfig.Name}");

                    // 停止 IO 工作器（避免重连竞态）
                    StopIoWorker();

                    // 清空本会话残留队列（缓存读/写请求），队列可复用
                    _ioExecutor.ClearQueue();

                    _transport.Close();

                    // 状态归位（禁止自动重连 + 计数清零）
                    _connectionState.Reset();

                    _logger.WriteInfo($"连接已关闭: {_plcConfig.EndpointText}");
                }
                catch (Exception ex)
                {
                    _logger.WriteException($"关闭连接异常: {ex.Message}", ex);
                }
            }
        }
        #endregion

        #region 缓存控制（IoWorker 启停：连接生命周期内在部分——Connect 首次触发启动，Close/Dispose 停止；外部零入口）

        /// <summary>
        /// 确保 IO 工作器运行（幂等：实例级仅一个 IoWorker，重复/并发调用零副作用）
        /// 生命周期：Connect（成败不论，finally）首次触发启动——成功=轮询刷新，失败=自动重连守护（IoWorker 循环内退避接管）；
        /// 之后常驻至 Close/Dispose 停止；断线不重建（IoWorker 内部循环切重连分支）
        /// </summary>
        private void EnsureIoWorkerRunning()
        {
            lock (_cacheLock)
            {
                if (_ioWorker != null)
                    return;

                try
                {
                    _ioCts = new CancellationTokenSource();
                    _ioWorker = new IoWorker(_ioExecutor, _connectionState, _pollingTime, _logger);
                    _ioWorker.Start(_ioCts.Token);
                    _logger.WriteInfo("缓存模式已启用（IoWorker 启动）");
                }
                catch (Exception ex)
                {
                    // 启动失败（罕见）：清引用允许下次 Connect 重试，不覆盖 Connect 返回值
                    _logger.WriteException("IO工作器启动失败", ex);
                    _ioCts = null;
                    _ioWorker = null;
                }
            }
        }

        /// <summary>
        /// 停止 IO 工作器（幂等；触发点唯一：Close/Dispose）
        /// 未能在等待时限内退出的残留线程：Cancel 已置 → 完成当前会话后必然退出；transport.Close 打断通讯加速返回；
        /// 期间日志写入由 PlcLogger 释放自防御兜底（静默丢弃），不阻塞等待（防 UI 线程 Dispose 卡顿）
        /// </summary>
        private void StopIoWorker()
        {
            lock (_cacheLock)
            {
                if (_ioWorker == null)
                    return;

                try
                {
                    _ioCts?.Cancel();
                    _ioWorker.Stop(2000); // 最多等待2秒，不等完
                    _logger.WriteInfo("缓存模式已禁用（IoWorker 停止）");
                }
                finally
                {
                    _ioWorker = null;
                    _ioCts = null;
                }
            }
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 按组名+标签名查找标签项（地址表现取，CD查找ns级可忽略）
        /// </summary>
        private TagItem FindTag(string groupName, string tagName)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(tagName))
                return null;

            var tagTable = _configManager.GetTagTable(_plcConfig.Name);
            if (tagTable == null)
                return null;

            if (tagTable.TryGetValue(groupName, out var group))
            {
                if (group.Tags.TryGetValue(tagName, out var tag))
                {
                    return tag;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取数据类型默认值（数组类型返回各自类型的空数组，与 DataType 匹配；default 理论不可达，留防御）
        /// </summary>
        private object GetDefaultValue(PlcDataType type)
        {
            switch (type)
            {
                case PlcDataType.Byte: return (byte)0;
                case PlcDataType.Bool: return false;
                case PlcDataType.Short: return (short)0;
                case PlcDataType.UShort: return (ushort)0;
                case PlcDataType.Int: return 0;
                case PlcDataType.UInt: return 0u;
                case PlcDataType.Long: return 0L;
                case PlcDataType.ULong: return 0UL;
                case PlcDataType.Float: return 0f;
                case PlcDataType.Double: return 0d;
                case PlcDataType.String: return string.Empty;
                case PlcDataType.BoolArray: return new bool[0];
                case PlcDataType.ByteArray: return new byte[0];
                case PlcDataType.ShortArray: return new short[0];
                case PlcDataType.UShortArray: return new ushort[0];
                case PlcDataType.IntArray: return new int[0];
                case PlcDataType.UIntArray: return new uint[0];
                case PlcDataType.LongArray: return new long[0];
                case PlcDataType.ULongArray: return new ulong[0];
                case PlcDataType.FloatArray: return new float[0];
                case PlcDataType.DoubleArray: return new double[0];
                default: return null;
            }
        }

        /// <summary>
        /// 值深拷贝：数组 Clone（防止业务层修改污染缓存）；其余（值类型装箱/string 不可变）直接返回
        /// </summary>
        private static object DeepCopyValue(object value)
        {
            if (value is Array arr)
            {
                return arr.Clone();
            }
            return value;
        }
        #endregion

        #region 资源释放
        /// <summary>
        /// 释放PLC实例资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                try
                {
                    _logger.WriteInfo($"开始释放PLC实例: {_plcConfig.Name}");

                    _disposed = true;

                    // 停止 IO 工作器（Cancel+尽力等；残留线程由 transport.Close 打断、PlcLogger 自防御兜底）
                    StopIoWorker();

                    _transport.Close();

                    // 标记队列完成（优雅关闭）
                    _ioExecutor.CompleteAdding();

                    _logger.WriteInfo($"PLC实例已释放: {_plcConfig.Name}");
                    _logger.WriteInfo("═══════════════════════════════════════");
                    _logger.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.WriteException($"释放资源异常: {ex.Message}", ex);
                }
            }
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~PlcRuntime()
        {
            Dispose(false);
        }
        #endregion
    }
}
