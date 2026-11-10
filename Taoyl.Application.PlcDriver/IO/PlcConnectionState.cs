using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Logging;
using Taoyl.Application.PlcDriver.Models.Config;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Taoyl.Application.PlcDriver.IO
{
    /// <summary>
    /// PLC连接状态枚举（三态轻状态机）
    /// </summary>
    public enum PlcConnectionStatus
    {
        /// <summary>未连接</summary>
        Disconnected,

        /// <summary>连接中（Connect/TryReconnectAsync 进行中）</summary>
        Connecting,

        /// <summary>已连接</summary>
        Connected
    }

    /// <summary>
    /// PLC连接状态机（手写轻状态机，不引第三方库）
    /// 职责：维护连接状态三态 + 被动判定（Execute 后 UpdateConnectionFromResult）+ 防抖（连续N=3异常才断）+ 退避重连（可配置：见构造函数）
    /// 判定=每次真正读写后固有步骤（所有来源一致）；重连触发=仅 IoWorker 循环
    /// 边界：不持有地址表、不执行读写，只维护连接相关状态
    /// </summary>
    public class PlcConnectionState
    {
        #region 成员变量
        private readonly IPlcTransport _transport;
        private readonly IPlcLogger _logger;

        private readonly object _lock = new object();

        // 连接状态（volatile：跨线程可见）
        private volatile bool _isConnected = false;
        private volatile PlcConnectionStatus _status = PlcConnectionStatus.Disconnected;
        private volatile bool _allowReconnection = false;

        // 防抖：连续异常计数（连续N次异常才判定断开，成功清零）
        private int _connectionErrorCount = 0;
        private const int CONNECTION_FAIL_THRESHOLD = 3;

        // 重连管理（指数退避：首试立即，后续 base×2^n，间隔封顶；次数由 RetryConnect 决定，默认无限直到 Close/Reset 终止）
        private int _reconnectAttemptCount = 0;
        private DateTime _lastReconnectTime = DateTime.MinValue;
        private readonly int _reconnectBaseDelay;   // 基础间隔 ms（ProtocolParams.ReconnectBaseDelay，默认 3000）
        private readonly int _reconnectMaxDelay;    // 间隔封顶 ms（ProtocolParams.ReconnectMaxDelay，默认 90000）
        private readonly int _reconnectLimit;       // 自动重连次数上限（PlcConfig.RetryConnect；<=0=无限，默认 0=无限）
        private const int MAX_RECONNECT_EXPONENT = 6;    // 指数封顶：3000×2^6=192s 必超封顶，防 attemptCount 长期增长致指数溢出
        #endregion

        #region 公开属性
        /// <summary>
        /// 是否已连接（volatile，业务层读取）
        /// </summary>
        public bool IsConnected => _isConnected;

        /// <summary>
        /// 连接状态三态
        /// </summary>
        public PlcConnectionStatus Status => _status;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="transport">传输层原语（状态判定依赖库错误语义，由 Transport.CheckConnectionStatus 提供）</param>
        /// <param name="logger">实例级日志</param>
        /// <param name="plcConfig">PLC配置（重连参数可配：ProtocolParams.ReconnectBaseDelay/ReconnectMaxDelay + RetryConnect；未配置回退默认，默认=3000/90000/无限）</param>
        public PlcConnectionState(IPlcTransport transport, IPlcLogger logger, PlcConfig plcConfig)
        {
            _transport = transport ?? throw new ArgumentNullException(nameof(transport));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            if (plcConfig == null)
                throw new ArgumentNullException(nameof(plcConfig));

            // 重连参数构造时一次性解析（构造后不可变，无需每次 ShouldAttemptReconnect 现读）
            _reconnectBaseDelay = GetIntParam(plcConfig.GetProtocolParam("ReconnectBaseDelay", null), 3000);
            _reconnectMaxDelay = GetIntParam(plcConfig.GetProtocolParam("ReconnectMaxDelay", null), 90000);
            // RetryConnect：<=0=无限（默认，Normalize 已兜底空值→"0"）；>0=最多自动重连 N 次，达上限停止等上层 Connect
            _reconnectLimit = int.TryParse(plcConfig.RetryConnect, out var limit) ? Math.Max(limit, 0) : 0;
        }

        /// <summary>
        /// 解析整型参数（未配置/解析失败回退默认值）
        /// </summary>
        private static int GetIntParam(string value, int defaultValue)
        {
            return int.TryParse(value, out var result) ? result : defaultValue;
        }
        #endregion

        #region 状态判定（被动）
        /// <summary>
        /// 根据操作结果更新连接状态（防抖：连续N次异常才判定断开，成功清零）
        /// 判定=每次真正读写后固有步骤，所有来源（轮询/缓存写/直接读写）一致
        /// 注意：断开后不再计数（防抖只用于"已连接→断开"判定，已断开计数无意义）
        /// </summary>
        /// <param name="result">操作结果</param>
        public void UpdateConnectionFromResult(ResponseResult result)
        {
            if (result == null) return;

            bool ok = _transport.CheckConnectionStatus(result);

            lock (_lock)
            {
                if (ok)
                {
                    // 成功：清零连续异常计数
                    _connectionErrorCount = 0;
                    if (!_isConnected)
                    {
                        _isConnected = true;
                        _status = PlcConnectionStatus.Connected;
                        _logger.WriteInfo($"连接状态: 已连接,操作结果信息:{result}");
                    }
                }
                else if (_isConnected)
                {
                    // 失败且当前已连接：连续异常计数，达到阈值才判定断开（防抖，避免异常波动误判）
                    _connectionErrorCount++;
                    if (_connectionErrorCount >= CONNECTION_FAIL_THRESHOLD)
                    {
                        _isConnected = false;
                        _status = PlcConnectionStatus.Disconnected;
                        _logger.WriteWarn($"连接状态: 已断开,操作结果信息:{result}");
                    }
                }
            }
        }
        #endregion

        #region 连接管理
        /// <summary>
        /// 连接PLC（一次干净尝试，成功清重连计数；失败由 IoWorker 循环按退避重试）
        /// _lock 串行化：与 TryReconnectAsync 互斥，避免业务 Connect 与 IoWorker 重连并发调 transport.Connect/Close
        /// </summary>
        public ResponseResult Connect()
        {
            lock (_lock)
            {
                _allowReconnection = true;
                _status = PlcConnectionStatus.Connecting;

                var result = _transport.Connect();
                UpdateConnectionFromResult(result);

                if (_isConnected)
                {
                    Interlocked.Exchange(ref _reconnectAttemptCount, 0);
                }
                else
                {
                    // 首次连接失败：_isConnected 本就 false，UpdateConnectionFromResult 不会置 Disconnected，这里显式回落
                    _status = PlcConnectionStatus.Disconnected;
                }
                return result;
            }
        }

        /// <summary>
        /// 重置连接状态（Close 时调用：禁止自动重连、状态归位、计数清零）
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _allowReconnection = false;
                _isConnected = false;
                _status = PlcConnectionStatus.Disconnected;
                _connectionErrorCount = 0;
                _reconnectAttemptCount = 0;
            }
        }
        #endregion

        #region 重连（指数退避 base×2^n，间隔封顶可配置，次数默认无限）
        /// <summary>
        /// 是否应该尝试重连（指数退避判断，仅 IoWorker 循环调用）
        /// 节奏：首试立即 → base → base×2 → base×4 …（封顶）；RetryConnect&gt;0 时达到次数上限返回 false（停止自动重连，等上层再 Connect）
        /// </summary>
        public bool ShouldAttemptReconnect()
        {
            if (!_allowReconnection) return false;

            int attemptCount = Interlocked.CompareExchange(ref _reconnectAttemptCount, 0, 0);
            if (_reconnectLimit > 0 && attemptCount >= _reconnectLimit)
                return false; // 达到自动重连次数上限：停止（静默），上层 Connect() 成功/Reset 清计数后重新开始

            if (attemptCount == 0)
                return true; // 第一次重连立即尝试

            int exponent = Math.Min(attemptCount - 1, MAX_RECONNECT_EXPONENT);
            // 每次×2（指数退避）：base × 2^exponent；2 的整数次幂在 double 中精确（IEEE754 指数即 2 的幂存储），无浮点误差
            long delayMs = _reconnectBaseDelay * (long)Math.Pow(2, exponent);
            if (delayMs > _reconnectMaxDelay)
                delayMs = _reconnectMaxDelay;

            return (DateTime.Now - _lastReconnectTime).TotalMilliseconds >= delayMs;
        }

        /// <summary>
        /// 执行自动重连（Close→Connect，成功清计数；取消令牌可打断；仅 IoWorker 循环触发）
        /// _lock 串行化：与 Connect/Reset 互斥，避免重连与业务 Connect/Close 并发调 transport
        /// 注意：锁内无 await（Close/Connect 均同步），Monitor 可重入，UpdateConnectionFromResult 内层 lock 不冲突
        /// </summary>
        public async Task TryReconnectAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested) return;

            lock (_lock)
            {
                try
                {
                    int currentAttempt = Interlocked.Increment(ref _reconnectAttemptCount);
                    _lastReconnectTime = DateTime.Now;
                    _status = PlcConnectionStatus.Connecting;

                    _logger.WriteInfo($"尝试第{currentAttempt}次重连...");

                    _transport.Close();

                    var result = _transport.Connect();
                    UpdateConnectionFromResult(result);

                    if (_isConnected)
                    {
                        _logger.WriteInfo($"第{currentAttempt}次重连,成功");
                        Interlocked.Exchange(ref _reconnectAttemptCount, 0);
                    }
                    else
                    {
                        _status = PlcConnectionStatus.Disconnected;
                    }
                }
                catch (Exception ex)
                {
                    _logger.WriteException("重连异常", ex);
                }
            }
        }
        #endregion
    }
}
