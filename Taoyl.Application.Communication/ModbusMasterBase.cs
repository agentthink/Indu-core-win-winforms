using NModbus;
using NModbus.Message;
using NModbus.Logging;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// Modbus 主站抽象基类。收敛 TCP/RTU 公共逻辑：
    /// - 信号量同步
    /// - 锁外重试（避免阻塞所有线程）
    /// - 消息间延迟控制（串口设备喘息）
    /// - 连接状态机（Disconnected/Connecting/Connected/Reconnecting/Closing）
    /// - 单 Task 心跳 + 重连循环（指数退避 + 随机抖动）
    /// - 连接代次隔离（旧连接异常不影响新连接）
    /// - 所有强类型读写方法
    /// - 自定义报文发送
    /// - 原始报文日志（异步写文件）
    /// 子类只需实现：连接创建（同步+异步）、连接状态判断、原生 Master 获取、断开清理。
    /// </summary>
    public abstract class ModbusMasterBase : IReadWriteDevice
    {
        #region 子类必须实现的抽象成员

        /// <summary>当前物理连接是否建立</summary>
        protected abstract bool IsConnectedCore { get; }

        /// <summary>获取 NModbus 原生主站实例（连接成功后必须非空）</summary>
        protected abstract IModbusMaster NativeMaster { get; }

        /// <summary>同步建立物理连接（信号量内调用）</summary>
        protected abstract OperateResult ConnectCore();

        /// <summary>异步建立物理连接（信号量内调用，真异步路径）</summary>
        protected abstract Task<OperateResult> ConnectCoreAsync();

        /// <summary>断开物理连接并清理资源（信号量内调用）</summary>
        protected abstract void DisconnectCore();

        #endregion

        protected readonly ConnectionParams parameters;
        protected readonly SemaphoreSlim semaphore = new SemaphoreSlim(1, 1);
        protected bool disposed;
        private DateTime lastMessageTime = DateTime.MinValue;
        private static readonly Random jitterRandom = new Random();

        #region 连接状态机

        /// <summary>当前连接状态（原子读写）</summary>
        private int state = (int)ConnectionState.Disconnected;

        /// <summary>当前连接状态</summary>
        public ConnectionState State => (ConnectionState)Interlocked.CompareExchange(ref state, 0, 0);

        /// <summary>
        /// 当前是否处于可读写状态。
        /// 未启用心跳：仅判断物理连接；
        /// 启用心跳：物理连接正常且心跳未超时。
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (!IsConnectedCore) return false;
                if (!parameters.EnableHeartbeat) return true;
                return (DateTime.UtcNow - lastHeartbeatAckUtc).TotalMilliseconds <= parameters.HeartbeatTimeoutMs;
            }
        }

        /// <summary>连接状态变更事件（在后台线程触发，UI 订阅需 Invoke/BeginInvoke）</summary>
        public event Action<ConnectionState> StateChanged;

        /// <summary>通讯异常事件</summary>
        public event Action<Exception> CommunicationError;

        /// <summary>连接代次，每次建立新连接时递增，用于隔离旧连接的迟到异常</summary>
        private long connectionGeneration;

        /// <summary>当前重连退避时间（毫秒）</summary>
        private int currentBackoffMs;

        /// <summary>重连尝试计数</summary>
        private int reconnectAttemptCount;

        /// <summary>上次心跳应答时间（UTC）</summary>
        private DateTime lastHeartbeatAckUtc = DateTime.MinValue;

        /// <summary>上次重连尝试时间（UTC）</summary>
        private DateTime lastReconnectAttempt = DateTime.MinValue;

        /// <summary>心跳/重连循环任务</summary>
        private Task heartbeatTask;

        /// <summary>生命周期取消令牌源</summary>
        private CancellationTokenSource lifetimeCts = new CancellationTokenSource();

        /// <summary>重连同步锁，确保同一时刻只有一个重连流程</summary>
        private readonly object reconnectSync = new object();

        private readonly object heartbeatLock = new object();

        #endregion

        #region 日志

        private FileModbusLogger logger;

        /// <summary>
        /// 当前使用的日志记录器，可能为 null。
        /// </summary>
        protected FileModbusLogger Logger => logger;

        #endregion

        protected ModbusMasterBase(ConnectionParams parameters)
        {
            this.parameters = parameters ?? throw new ArgumentNullException(nameof(parameters));

            // 根据 LogPathTemplate 初始化日志器
            if (!string.IsNullOrEmpty(parameters.LogPathTemplate))
            {
                logger = new FileModbusLogger(parameters.LogPathTemplate);
            }
        }

        #region 状态机辅助方法

        /// <summary>原子更新状态并触发事件</summary>
        private void UpdateState(ConnectionState newState)
        {
            var oldState = (ConnectionState)Interlocked.Exchange(ref state, (int)newState);
            if (oldState != newState)
            {
                var handler = StateChanged;
                if (handler != null)
                {
                    // 始终异步触发，确保不阻塞任何锁
                    Task.Run(() => handler(newState));
                }
            }

        }

        /// <summary>触发通讯异常事件</summary>
        private void RaiseCommunicationError(Exception exception)
        {
            CommunicationError?.Invoke(exception);
        }

        /// <summary>检查对象是否已释放</summary>
        private void ThrowIfDisposed()
        {
            if (disposed) throw new ObjectDisposedException(GetType().Name);
        }

        #endregion

        #region 连接管理

        public OperateResult Connect()
        {
            ThrowIfDisposed();
            semaphore.Wait();
            try
            {
                return ConnectInternal(false, CancellationToken.None).GetAwaiter().GetResult();
            }
            catch (Exception ex) { return OperateResult.Fail(ex.Message); }
            finally { semaphore.Release(); }
        }

        public async Task<OperateResult> ConnectAsync()
        {
            ThrowIfDisposed();
            await semaphore.WaitAsync().ConfigureAwait(false);
            try
            {
                return await ConnectInternal(true, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex) { return OperateResult.Fail(ex.Message); }
            finally { semaphore.Release(); }
        }

        /// <summary>连接内部实现（同步/异步统一入口）</summary>
        private async Task<OperateResult> ConnectInternal(bool async, CancellationToken cancellationToken)
        {
            if (State == ConnectionState.Connected || State == ConnectionState.Connecting)
                return OperateResult.Success();

            UpdateState(ConnectionState.Connecting);

            try
            {
                DisconnectCore();

                var result = async
                    ? await ConnectCoreAsync().ConfigureAwait(false)
                    : ConnectCore();

                if (!result.IsSuccess)
                {
                    UpdateState(ConnectionState.Disconnected);
                    return result;
                }

                // 新连接代次
                Interlocked.Increment(ref connectionGeneration);
                lastHeartbeatAckUtc = DateTime.UtcNow;
                currentBackoffMs = parameters.ReconnectDelayMs;
                reconnectAttemptCount = 0;

                UpdateState(ConnectionState.Connected);

                // 启动心跳/重连循环（单 Task）
                StartHeartbeatLoop();

                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                UpdateState(ConnectionState.Disconnected);
                return OperateResult.Fail(ex.Message);
            }
        }

        public OperateResult Disconnect()
        {
            ThrowIfDisposed();
            semaphore.Wait();
            try
            {
                return DisconnectInternal();
            }
            catch (Exception ex) { return OperateResult.Fail(ex.Message); }
            finally { semaphore.Release(); }
        }

        /// <summary>断开连接内部实现</summary>
        private OperateResult DisconnectInternal()
        {
            // 1. 进入 Closing 状态，阻止后续重连触发
            UpdateState(ConnectionState.Closing);

            // 2. 取消令牌，让心跳循环的 Task.Delay / WaitAsync 快速醒来
            lifetimeCts?.Cancel();

            // 递增连接代次，使所有旧后台任务立即失效（双保险）
            Interlocked.Increment(ref connectionGeneration);

            // 3. 立即强制关闭物理连接（踢掉所有卡住的 IO）
            DisconnectCore();

            // 4. 重置退避参数（为下次全新连接做准备）
            currentBackoffMs = parameters.ReconnectDelayMs;
            reconnectAttemptCount = 0;

            // 5. 【核心修复】原子性地解除对旧心跳任务的引用
            //    注意：这里完全不需要 lock，因为持有 semaphore 大锁，且用 Interlocked 保证原子性
            Interlocked.Exchange(ref heartbeatTask, null);

            // 6. 状态机切回 Disconnected
            UpdateState(ConnectionState.Disconnected);

            return OperateResult.Success();
        }

        #endregion

        #region 心跳与重连循环（单 Task）

        /// <summary>启动心跳/重连循环任务</summary>
        private void StartHeartbeatLoop()
        {
            lock (heartbeatLock)
            {
                if (heartbeatTask != null && !heartbeatTask.IsCompleted) return;

                var generation = Interlocked.Read(ref connectionGeneration);
                heartbeatTask = Task.Run(() => HeartbeatLoopAsync(generation, lifetimeCts.Token));
            }
        }

        /// <summary>
        /// 心跳与重连循环（单 Task）。
        /// 状态 = Connected 时执行心跳检测；
        /// 状态 = Reconnecting 时执行退避重连；
        /// 其他状态退出循环。
        /// </summary>
        private async Task HeartbeatLoopAsync(long generation, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                // 代次检查：旧连接的任务直接退出
                if (generation != Interlocked.Read(ref connectionGeneration))
                    return;

                // 动态延迟：默认心跳周期，重连状态用退避时间
                int delayMs = parameters.HeartbeatIntervalMs;

                try
                {
                    switch (State)
                    {
                        case ConnectionState.Connected:
                            await TickHeartbeatAsync(generation, token).ConfigureAwait(false);
                            if (State == ConnectionState.Reconnecting)
                            {
                                await TickReconnectAsync(generation, token).ConfigureAwait(false);
                                // 如果重连失败，使用退避时间作为下次检查间隔
                                delayMs = Math.Min(currentBackoffMs, parameters.HeartbeatIntervalMs);
                                // 如果重连成功，状态已变回 Connected，delayMs 会在下次循环恢复
                            }
                            break;

                        case ConnectionState.Reconnecting:
                            await TickReconnectAsync(generation, token).ConfigureAwait(false);
                            break;

                        case ConnectionState.Disconnected:
                        case ConnectionState.Connecting:
                        case ConnectionState.Closing:
                            return; // 不相关状态，退出循环
                    }
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
                catch (Exception ex)
                {
                    RaiseCommunicationError(ex);
                }

                // 等待下一个心跳周期
                try
                {
                    await Task.Delay(parameters.HeartbeatIntervalMs, token).ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (token.IsCancellationRequested)
                {
                    return;
                }
            }
        }

        /// <summary>心跳周期：发送伪心跳并检查响应</summary>
        private async Task TickHeartbeatAsync(long generation, CancellationToken token)
        {
            // 未配置心跳：仅检查物理连接
            if (!parameters.EnableHeartbeat)
            {
                if (!IsConnectedCore)
                    EnterReconnecting(generation);
                return;
            }

            // 尝试获取锁，等待最多 500ms，避免心跳占用锁过久影响业务
            if (!await semaphore.WaitAsync(500, token).ConfigureAwait(false))
                return; // 业务繁忙，本次心跳跳过

            // 发送伪心跳（读固定寄存器）
            bool heartbeatSuccess = false;
            try
            {
                if (NativeMaster != null)
                {
                    //await semaphore.WaitAsync(token).ConfigureAwait(false); // 锁内执行心跳，避免与读写冲突

                    await NativeMaster.ReadHoldingRegistersAsync(
                        parameters.SlaveId,
                        parameters.HeartbeatAddress,
                        parameters.HeartbeatLength).ConfigureAwait(false);
                    heartbeatSuccess = true;  // 无异常即成功
                }
            }
            catch (Exception)
            {
                heartbeatSuccess = false;  // 任何异常都视为失败
            }
            finally
            {
                semaphore.Release();
            }

            if (heartbeatSuccess)
            {
                lastHeartbeatAckUtc = DateTime.UtcNow;
                return;
            }

            // 心跳失败，检查是否已超时
            var silence = (DateTime.UtcNow - lastHeartbeatAckUtc).TotalMilliseconds;
            if (silence > parameters.HeartbeatTimeoutMs)
            {
                EnterReconnecting(generation);
            }
        }

        /// <summary>进入重连状态</summary>
        private void EnterReconnecting(long generation)
        {
            // 如果未启用自动重连，直接切到 Disconnected 并不执行重连
            if (!parameters.AutoReconnect)
            {
                UpdateState(ConnectionState.Disconnected);
                return;
            }

            if (generation != Interlocked.Read(ref connectionGeneration)) return;
            if (State != ConnectionState.Connected) return;

            lock (reconnectSync)
            {
                if (State != ConnectionState.Connected) return;

                UpdateState(ConnectionState.Reconnecting);
                currentBackoffMs = parameters.ReconnectDelayMs;
                reconnectAttemptCount = 0;
                lastReconnectAttempt = DateTime.MinValue; // 立即允许第一次重试
            }
        }

        /// <summary>重连周期：按退避策略执行一次重连尝试</summary>
        private async Task TickReconnectAsync(long generation, CancellationToken token)
        {
            // 检查退避等待
            var elapsed = (long)(DateTime.UtcNow - lastReconnectAttempt).TotalMilliseconds;
            if (elapsed < currentBackoffMs)
                return; // 还没等到时间，下次周期再来

            // 等待信号量，确保与读写操作互斥
            await semaphore.WaitAsync(token).ConfigureAwait(false);

            try
            {
                // 双重检查：状态可能被用户 Connect/Disconnect 改变了
                if (State != ConnectionState.Reconnecting ||
                    generation != Interlocked.Read(ref connectionGeneration))
                    return;

                lastReconnectAttempt = DateTime.UtcNow;
                reconnectAttemptCount++;

                // 检查最大重试次数
                if (parameters.MaxReconnectRetries > 0 &&
                    reconnectAttemptCount > parameters.MaxReconnectRetries)
                {
                    UpdateState(ConnectionState.Disconnected);
                    RaiseCommunicationError(new InvalidOperationException(
                        $"重连失败：已达到最大重试次数 {parameters.MaxReconnectRetries}。"));
                    return;
                }

                try
                {
                    DisconnectCore();

                    var result = await ConnectCoreAsync().ConfigureAwait(false);

                    if (result.IsSuccess)
                    {
                        // 重连成功 连接代次递增，重置心跳时间和退避参数
                        Interlocked.Increment(ref connectionGeneration);
                        lastHeartbeatAckUtc = DateTime.UtcNow;
                        currentBackoffMs = parameters.ReconnectDelayMs;
                        reconnectAttemptCount = 0;
                        UpdateState(ConnectionState.Connected);
                        return;
                    }

                    // 失败：计算下次退避（指数退避 + 随机抖动）
                    var nextBackoff = currentBackoffMs * 2;
                    var jitter = jitterRandom.Next(0, 1000); // 0~1000ms 随机抖动
                    currentBackoffMs = Math.Min(nextBackoff, parameters.MaxReconnectDelayMs) + jitter;

                    RaiseCommunicationError(new InvalidOperationException(
                        $"第 {reconnectAttemptCount} 次重连失败，{result.ErrorMessage}。下次 {currentBackoffMs}ms 后重试。"));

                }
                catch (Exception ex)
                {
                    // 计算下次退避
                    var nextBackoff = currentBackoffMs * 2;
                    var jitter = jitterRandom.Next(0, 1000);
                    currentBackoffMs = Math.Min(nextBackoff, parameters.MaxReconnectDelayMs) + jitter;

                    RaiseCommunicationError(new InvalidOperationException(
                        $"第 {reconnectAttemptCount} 次重连异常：{ex.Message}。下次 {currentBackoffMs}ms 后重试。", ex));

                }
            }
            finally
            {
                semaphore.Release();
            }
        }

        #endregion

        #region 核心：锁外重试 + 消息间延迟 + 动态超时

        /// <summary>
        /// 同步执行：重试循环在锁外，锁内只做一次操作 + 消息间延迟。
        /// 避免设备离线时长时间占用信号量，导致其他线程饿死。
        /// </summary>
        protected OperateResult<T> PerformWithRetry<T>(Func<T> action, int? timeout = null)
        {
            for (int i = 0; i <= parameters.MaxRetries; i++)
            {
                var result = PerformOnce(action, timeout);
                if (result.IsSuccess) return result;
                if (i == parameters.MaxRetries) return result;
                Thread.Sleep(parameters.ReadRetryIntervalMs);
            }
            return OperateResult<T>.Fail("重试耗尽");
        }

        protected OperateResult PerformWithRetry(Action action, int? timeout = null)
        {
            for (int i = 0; i <= parameters.MaxRetries; i++)
            {
                var result = PerformOnce(action, timeout);
                if (result.IsSuccess) return result;
                if (i == parameters.MaxRetries) return result;
                Thread.Sleep(parameters.ReadRetryIntervalMs);
            }
            return OperateResult.Fail("重试耗尽");
        }

        /// <summary>
        /// 异步执行：重试循环在锁外，锁内只做一次操作 + 消息间延迟。
        /// </summary>
        protected async Task<OperateResult<T>> PerformWithRetryAsync<T>(Func<Task<T>> action, int? timeout = null)
        {
            for (int i = 0; i <= parameters.MaxRetries; i++)
            {
                var result = await PerformOnceAsync(action, timeout).ConfigureAwait(false);
                if (result.IsSuccess) return result;
                if (i == parameters.MaxRetries) return result;
                await Task.Delay(parameters.ReadRetryIntervalMs).ConfigureAwait(false);
            }
            return OperateResult<T>.Fail("重试耗尽");
        }

        protected async Task<OperateResult> PerformWithRetryAsync(Func<Task> action, int? timeout = null)
        {
            for (int i = 0; i <= parameters.MaxRetries; i++)
            {
                var result = await PerformOnceAsync(action, timeout).ConfigureAwait(false);
                if (result.IsSuccess) return result;
                if (i == parameters.MaxRetries) return result;
                await Task.Delay(parameters.ReadRetryIntervalMs).ConfigureAwait(false);
            }
            return OperateResult.Fail("重试耗尽");
        }

        /// <summary>
        /// 同步执行单次操作（信号量保护 + 动态超时 + 消息延迟 + 连接检查）。
        /// </summary>
        private OperateResult<T> PerformOnce<T>(Func<T> action, int? timeout = null)
        {
            try
            {
                EnsureConnected();   // 锁外检查连接，快速失败
            }
            catch (Exception ex) { return OperateResult<T>.Fail(ex.Message); }

            semaphore.Wait();
            int origRead = -1, origWrite = -1;
            var transport = NativeMaster?.Transport;

            try
            {
                // 动态修改本次超时
                if (transport != null && timeout.HasValue)
                {
                    origRead = transport.ReadTimeout;
                    origWrite = transport.WriteTimeout;
                    transport.ReadTimeout = timeout.Value;
                    transport.WriteTimeout = timeout.Value;
                }

                ApplyMessageDelay();
                T value = action();
                lastMessageTime = DateTime.Now;
                return OperateResult<T>.Success(value);
            }
            catch (Exception ex)
            {
                return OperateResult<T>.Fail(ex.Message);
            }
            finally
            {
                // 恢复原始超时
                if (transport != null && origRead != -1)
                {
                    transport.ReadTimeout = origRead;
                    transport.WriteTimeout = origWrite;
                }
                semaphore.Release();
            }
        }

        private OperateResult PerformOnce(Action action, int? timeout = null)
        {
            try
            {
                EnsureConnected();   // 锁外检查连接，快速失败
            }
            catch (Exception ex) { return OperateResult.Fail(ex.Message); }

            semaphore.Wait();
            int origRead = -1, origWrite = -1;
            var transport = NativeMaster?.Transport;

            try
            {
                if (transport != null && timeout.HasValue)
                {
                    origRead = transport.ReadTimeout;
                    origWrite = transport.WriteTimeout;
                    transport.ReadTimeout = timeout.Value;
                    transport.WriteTimeout = timeout.Value;
                }

                ApplyMessageDelay();
                action();
                lastMessageTime = DateTime.Now;
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                return OperateResult.Fail(ex.Message);
            }
            finally
            {
                if (transport != null && origRead != -1)
                {
                    transport.ReadTimeout = origRead;
                    transport.WriteTimeout = origWrite;
                }
                semaphore.Release();
            }
        }

        /// <summary>
        /// 异步执行单次操作（信号量保护 + 动态超时 + 消息延迟 + 连接检查）。
        /// </summary>
        private async Task<OperateResult<T>> PerformOnceAsync<T>(Func<Task<T>> action, int? timeout = null)
        {
            try
            {
                await EnsureConnectedAsync().ConfigureAwait(false);  // 锁外检查连接，快速失败
            }
            catch (Exception ex) { return OperateResult<T>.Fail(ex.Message); }

            await semaphore.WaitAsync().ConfigureAwait(false);
            int origRead = -1, origWrite = -1;
            var transport = NativeMaster?.Transport;

            try
            {
                if (transport != null && timeout.HasValue)
                {
                    origRead = transport.ReadTimeout;
                    origWrite = transport.WriteTimeout;
                    transport.ReadTimeout = timeout.Value;
                    transport.WriteTimeout = timeout.Value;
                }
                await ApplyMessageDelayAsync().ConfigureAwait(false);
                T value = await action().ConfigureAwait(false);
                lastMessageTime = DateTime.Now;
                return OperateResult<T>.Success(value);
            }
            catch (Exception ex)
            {
                return OperateResult<T>.Fail(ex.Message);
            }
            finally
            {
                if (transport != null && origRead != -1)
                {
                    transport.ReadTimeout = origRead;
                    transport.WriteTimeout = origWrite;
                }
                semaphore.Release();
            }
        }

        private async Task<OperateResult> PerformOnceAsync(Func<Task> action, int? timeout = null)
        {
            try
            {
                await EnsureConnectedAsync().ConfigureAwait(false);  // 锁外检查连接，快速失败
            }
            catch (Exception ex) { return OperateResult.Fail(ex.Message); }

            await semaphore.WaitAsync().ConfigureAwait(false);
            int origRead = -1, origWrite = -1;
            var transport = NativeMaster?.Transport;

            try
            {
                if (transport != null && timeout.HasValue)
                {
                    origRead = transport.ReadTimeout;
                    origWrite = transport.WriteTimeout;
                    transport.ReadTimeout = timeout.Value;
                    transport.WriteTimeout = timeout.Value;
                }
                await ApplyMessageDelayAsync().ConfigureAwait(false);
                await action().ConfigureAwait(false);
                lastMessageTime = DateTime.Now;
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                return OperateResult.Fail(ex.Message);
            }
            finally
            {
                if (transport != null && origRead != -1)
                {
                    transport.ReadTimeout = origRead;
                    transport.WriteTimeout = origWrite;
                }
                semaphore.Release();
            }
        }

        /// <summary>消息间延迟控制（某些串口设备需要喘息时间）</summary>
        private void ApplyMessageDelay()
        {
            if (parameters.DelayBetweenMessages <= 0) return;
            int elapsed = (int)(DateTime.Now - lastMessageTime).TotalMilliseconds;
            if (elapsed < parameters.DelayBetweenMessages)
                Thread.Sleep(parameters.DelayBetweenMessages - elapsed);
        }

        private async Task ApplyMessageDelayAsync()
        {
            if (parameters.DelayBetweenMessages <= 0) return;
            int elapsed = (int)(DateTime.UtcNow - lastMessageTime).TotalMilliseconds;
            if (elapsed < parameters.DelayBetweenMessages)
                await Task.Delay(parameters.DelayBetweenMessages - elapsed).ConfigureAwait(false);
        }

        /// <summary>
        /// 锁内检查物理连接，断线时若启用自动重连则快速失败。
        /// 注意：此处不检查心跳，避免读写操作被心跳阻塞。
        /// </summary>
        private void EnsureConnected()
        {
            if (IsConnectedCore) return;

            if (!parameters.AutoReconnect)
                throw new InvalidOperationException("设备未连接，且自动重连已禁用。");

            // 快速失败：不在锁内执行重连，由后台心跳循环处理
            throw new InvalidOperationException("设备未连接，正在自动重连中。");
        }

        /// <summary>异步锁内检查物理连接</summary>
        private async Task EnsureConnectedAsync()
        {
            if (IsConnectedCore) return;

            if (!parameters.AutoReconnect)
                throw new InvalidOperationException("设备未连接，且自动重连已禁用。");

            throw new InvalidOperationException("设备未连接，正在自动重连中。");
        }

        #endregion

        #region 原生报文批量读写

        /// <summary>
        /// 发送自定义 Modbus 报文（支持非标准功能码）。
        /// </summary>
        public OperateResult<TResponse> ExecuteCustom<TResponse>(
            IModbusMessage request,
            int? timeout = null)
            where TResponse : IModbusMessage, new()
        {
            return PerformWithRetry(() => NativeMaster.ExecuteCustomMessage<TResponse>(request), timeout);
        }
        #endregion

        #region 原生报文批量读写（全部增加 timeout 参数）

        public OperateResult<ushort[]> ReadHoldingRegisters(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, startAddress, numberOfPoints), timeout);

        public async Task<OperateResult<ushort[]>> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, startAddress, numberOfPoints), timeout).ConfigureAwait(false);

        public OperateResult<ushort[]> ReadInputRegisters(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.ReadInputRegisters(parameters.SlaveId, startAddress, numberOfPoints), timeout);

        public async Task<OperateResult<ushort[]>> ReadInputRegistersAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.ReadInputRegistersAsync(parameters.SlaveId, startAddress, numberOfPoints), timeout).ConfigureAwait(false);

        public OperateResult<bool[]> ReadCoils(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.ReadCoils(parameters.SlaveId, startAddress, numberOfPoints), timeout);

        public async Task<OperateResult<bool[]>> ReadCoilsAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.ReadCoilsAsync(parameters.SlaveId, startAddress, numberOfPoints), timeout).ConfigureAwait(false);

        public OperateResult<bool[]> ReadDiscreteInputs(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.ReadInputs(parameters.SlaveId, startAddress, numberOfPoints), timeout);

        public async Task<OperateResult<bool[]>> ReadDiscreteInputsAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.ReadInputsAsync(parameters.SlaveId, startAddress, numberOfPoints), timeout).ConfigureAwait(false);

        public OperateResult WriteMultipleRegisters(ushort startAddress, ushort[] data, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, startAddress, data), timeout);

        public async Task<OperateResult> WriteMultipleRegistersAsync(ushort startAddress, ushort[] data, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, startAddress, data), timeout).ConfigureAwait(false);

        public OperateResult WriteCoils(ushort startAddress, bool[] data, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.WriteMultipleCoils(parameters.SlaveId, startAddress, data), timeout);

        public async Task<OperateResult> WriteCoilsAsync(ushort startAddress, bool[] data, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.WriteMultipleCoilsAsync(parameters.SlaveId, startAddress, data), timeout).ConfigureAwait(false);

        #endregion

        #region 强类型读取 — Bool

        public OperateResult<bool> ReadCoil(ushort address, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadCoils(parameters.SlaveId, address, 1), timeout);
            if (!result.IsSuccess) return OperateResult<bool>.Fail(result.ErrorMessage);
            return OperateResult<bool>.Success(result.Data[0]);
        }

        public async Task<OperateResult<bool>> ReadCoilAsync(ushort address, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadCoilsAsync(parameters.SlaveId, address, 1), timeout).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<bool>.Fail(result.ErrorMessage);
            return OperateResult<bool>.Success(result.Data[0]);
        }

        #endregion

        #region 强类型读取 — Int16

        public OperateResult<short> ReadInt16(ushort address, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, 1), timeout);
            if (!result.IsSuccess) return OperateResult<short>.Fail(result.ErrorMessage);
            return OperateResult<short>.Success(ModbusUtils.ConvertToInt16(result.Data[0], parameters.DataFormat));
        }

        public async Task<OperateResult<short>> ReadInt16Async(ushort address, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, 1), timeout).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<short>.Fail(result.ErrorMessage);
            return OperateResult<short>.Success(ModbusUtils.ConvertToInt16(result.Data[0], parameters.DataFormat));
        }

        public OperateResult<short[]> ReadInt16Array(ushort address, ushort length, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, length), timeout);
            if (!result.IsSuccess) return OperateResult<short[]>.Fail(result.ErrorMessage);
            short[] values = new short[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToInt16(result.Data[i], parameters.DataFormat);
            return OperateResult<short[]>.Success(values);
        }

        public async Task<OperateResult<short[]>> ReadInt16ArrayAsync(ushort address, ushort length, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, length), timeout).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<short[]>.Fail(result.ErrorMessage);
            short[] values = new short[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToInt16(result.Data[i], parameters.DataFormat);
            return OperateResult<short[]>.Success(values);
        }

        #endregion

        #region 强类型读取 — UInt16

        public OperateResult<ushort> ReadUInt16(ushort address, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, 1));
            if (!result.IsSuccess) return OperateResult<ushort>.Fail(result.ErrorMessage);
            return OperateResult<ushort>.Success(ModbusUtils.ConvertToUInt16(result.Data[0], parameters.DataFormat));
        }

        public async Task<OperateResult<ushort>> ReadUInt16Async(ushort address, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, 1)).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<ushort>.Fail(result.ErrorMessage);
            return OperateResult<ushort>.Success(ModbusUtils.ConvertToUInt16(result.Data[0], parameters.DataFormat));
        }

        public OperateResult<ushort[]> ReadUInt16Array(ushort address, ushort length, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, length));
            if (!result.IsSuccess) return OperateResult<ushort[]>.Fail(result.ErrorMessage);
            ushort[] values = new ushort[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToUInt16(result.Data[i], parameters.DataFormat);
            return OperateResult<ushort[]>.Success(values);
        }

        public async Task<OperateResult<ushort[]>> ReadUInt16ArrayAsync(ushort address, ushort length, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, length)).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<ushort[]>.Fail(result.ErrorMessage);
            ushort[] values = new ushort[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToUInt16(result.Data[i], parameters.DataFormat);
            return OperateResult<ushort[]>.Success(values);
        }

        #endregion

        #region 强类型读取 — Int32

        public OperateResult<int> ReadInt32(ushort address, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, 2));
            if (!result.IsSuccess) return OperateResult<int>.Fail(result.ErrorMessage);
            return OperateResult<int>.Success(ModbusUtils.ConvertToInt32(result.Data, 0, parameters.DataFormat));
        }

        public async Task<OperateResult<int>> ReadInt32Async(ushort address, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, 2)).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<int>.Fail(result.ErrorMessage);
            return OperateResult<int>.Success(ModbusUtils.ConvertToInt32(result.Data, 0, parameters.DataFormat));
        }

        public OperateResult<int[]> ReadInt32Array(ushort address, ushort length, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, (ushort)(length * 2)));
            if (!result.IsSuccess) return OperateResult<int[]>.Fail(result.ErrorMessage);
            int[] values = new int[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToInt32(result.Data, i * 2, parameters.DataFormat);
            return OperateResult<int[]>.Success(values);
        }

        public async Task<OperateResult<int[]>> ReadInt32ArrayAsync(ushort address, ushort length, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, (ushort)(length * 2))).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<int[]>.Fail(result.ErrorMessage);
            int[] values = new int[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToInt32(result.Data, i * 2, parameters.DataFormat);
            return OperateResult<int[]>.Success(values);
        }

        #endregion

        #region 强类型读取 — Float

        public OperateResult<float> ReadFloat(ushort address, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, 2));
            if (!result.IsSuccess) return OperateResult<float>.Fail(result.ErrorMessage);
            return OperateResult<float>.Success(ModbusUtils.ConvertToFloat(result.Data, 0, parameters.DataFormat));
        }

        public async Task<OperateResult<float>> ReadFloatAsync(ushort address, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, 2)).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<float>.Fail(result.ErrorMessage);
            return OperateResult<float>.Success(ModbusUtils.ConvertToFloat(result.Data, 0, parameters.DataFormat));
        }

        public OperateResult<float[]> ReadFloatArray(ushort address, ushort length, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, (ushort)(length * 2)));
            if (!result.IsSuccess) return OperateResult<float[]>.Fail(result.ErrorMessage);
            float[] values = new float[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToFloat(result.Data, i * 2, parameters.DataFormat);
            return OperateResult<float[]>.Success(values);
        }

        public async Task<OperateResult<float[]>> ReadFloatArrayAsync(ushort address, ushort length, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, (ushort)(length * 2))).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<float[]>.Fail(result.ErrorMessage);
            float[] values = new float[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToFloat(result.Data, i * 2, parameters.DataFormat);
            return OperateResult<float[]>.Success(values);
        }

        #endregion

        #region 强类型读取 — Double

        public OperateResult<double> ReadDouble(ushort address, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, 4));
            if (!result.IsSuccess) return OperateResult<double>.Fail(result.ErrorMessage);
            return OperateResult<double>.Success(ModbusUtils.ConvertToDouble(result.Data, 0, parameters.DataFormat));
        }

        public async Task<OperateResult<double>> ReadDoubleAsync(ushort address, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, 4)).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<double>.Fail(result.ErrorMessage);
            return OperateResult<double>.Success(ModbusUtils.ConvertToDouble(result.Data, 0, parameters.DataFormat));
        }

        public OperateResult<double[]> ReadDoubleArray(ushort address, ushort length, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, (ushort)(length * 4)));
            if (!result.IsSuccess) return OperateResult<double[]>.Fail(result.ErrorMessage);
            double[] values = new double[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToDouble(result.Data, i * 4, parameters.DataFormat);
            return OperateResult<double[]>.Success(values);
        }

        public async Task<OperateResult<double[]>> ReadDoubleArrayAsync(ushort address, ushort length, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, (ushort)(length * 4))).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<double[]>.Fail(result.ErrorMessage);
            double[] values = new double[length];
            for (int i = 0; i < length; i++)
                values[i] = ModbusUtils.ConvertToDouble(result.Data, i * 4, parameters.DataFormat);
            return OperateResult<double[]>.Success(values);
        }

        #endregion

        #region 强类型读取 — String

        public OperateResult<string> ReadString(ushort address, ushort length, int? timeout = null)
        {
            var result = PerformWithRetry(() => NativeMaster.ReadHoldingRegisters(parameters.SlaveId, address, length));
            if (!result.IsSuccess) return OperateResult<string>.Fail(result.ErrorMessage);
            return OperateResult<string>.Success(ModbusUtils.ConvertToString(result.Data, 0, length, parameters.DataFormat, parameters.TextEncoding));
        }

        public async Task<OperateResult<string>> ReadStringAsync(ushort address, ushort length, int? timeout = null)
        {
            var result = await PerformWithRetryAsync(() => NativeMaster.ReadHoldingRegistersAsync(parameters.SlaveId, address, length)).ConfigureAwait(false);
            if (!result.IsSuccess) return OperateResult<string>.Fail(result.ErrorMessage);
            return OperateResult<string>.Success(ModbusUtils.ConvertToString(result.Data, 0, length, parameters.DataFormat, parameters.TextEncoding));
        }

        #endregion

        #region 强类型写入 — Bool

        public OperateResult WriteCoil(ushort address, bool value, int? timeout = null)
            => PerformWithRetry(() => NativeMaster.WriteSingleCoil(parameters.SlaveId, address, value), timeout);

        public async Task<OperateResult> WriteCoilAsync(ushort address, bool value, int? timeout = null)
            => await PerformWithRetryAsync(() => NativeMaster.WriteSingleCoilAsync(parameters.SlaveId, address, value), timeout).ConfigureAwait(false);

        #endregion

        // 注意：此处省略了 Int16, UInt16, Int32, Float, Double, String 等所有写入方法，它们均需同样添加 timeout 参数。
        // 实际代码中请严格按照上述模式全部补全，确保接口完整实现。
        #region 强类型写入 — Int16

        public OperateResult WriteInt16(ushort address, short value, int? timeout = null)
        {
            ushort reg = ModbusUtils.ConvertFromInt16(value, parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteSingleRegister(parameters.SlaveId, address, reg));
        }

        public async Task<OperateResult> WriteInt16Async(ushort address, short value, int? timeout = null)
        {
            ushort reg = ModbusUtils.ConvertFromInt16(value, parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteSingleRegisterAsync(parameters.SlaveId, address, reg)).ConfigureAwait(false);
        }

        public OperateResult WriteInt16Array(ushort address, short[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length];
            for (int i = 0; i < values.Length; i++)
                regs[i] = ModbusUtils.ConvertFromInt16(values[i], parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteInt16ArrayAsync(ushort address, short[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length];
            for (int i = 0; i < values.Length; i++)
                regs[i] = ModbusUtils.ConvertFromInt16(values[i], parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        #endregion

        #region 强类型写入 — UInt16

        public OperateResult WriteUInt16(ushort address, ushort value, int? timeout = null)
        {
            ushort reg = ModbusUtils.ConvertFromUInt16(value, parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteSingleRegister(parameters.SlaveId, address, reg));
        }

        public async Task<OperateResult> WriteUInt16Async(ushort address, ushort value, int? timeout = null)
        {
            ushort reg = ModbusUtils.ConvertFromUInt16(value, parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteSingleRegisterAsync(parameters.SlaveId, address, reg)).ConfigureAwait(false);
        }

        public OperateResult WriteUInt16Array(ushort address, ushort[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length];
            for (int i = 0; i < values.Length; i++)
                regs[i] = ModbusUtils.ConvertFromUInt16(values[i], parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteUInt16ArrayAsync(ushort address, ushort[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length];
            for (int i = 0; i < values.Length; i++)
                regs[i] = ModbusUtils.ConvertFromUInt16(values[i], parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        #endregion

        #region 强类型写入 — Int32

        public OperateResult WriteInt32(ushort address, int value, int? timeout = null)
        {
            ushort[] regs = ModbusUtils.ConvertFromInt32(value, parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteInt32Async(ushort address, int value, int? timeout = null)
        {
            ushort[] regs = ModbusUtils.ConvertFromInt32(value, parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        public OperateResult WriteInt32Array(ushort address, int[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                ushort[] pair = ModbusUtils.ConvertFromInt32(values[i], parameters.DataFormat);
                regs[i * 2] = pair[0];
                regs[i * 2 + 1] = pair[1];
            }
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteInt32ArrayAsync(ushort address, int[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                ushort[] pair = ModbusUtils.ConvertFromInt32(values[i], parameters.DataFormat);
                regs[i * 2] = pair[0];
                regs[i * 2 + 1] = pair[1];
            }
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        #endregion

        #region 强类型写入 — Float

        public OperateResult WriteFloat(ushort address, float value, int? timeout = null)
        {
            ushort[] regs = ModbusUtils.ConvertFromFloat(value, parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteFloatAsync(ushort address, float value, int? timeout = null)
        {
            ushort[] regs = ModbusUtils.ConvertFromFloat(value, parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        public OperateResult WriteFloatArray(ushort address, float[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                ushort[] pair = ModbusUtils.ConvertFromFloat(values[i], parameters.DataFormat);
                regs[i * 2] = pair[0];
                regs[i * 2 + 1] = pair[1];
            }
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteFloatArrayAsync(ushort address, float[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length * 2];
            for (int i = 0; i < values.Length; i++)
            {
                ushort[] pair = ModbusUtils.ConvertFromFloat(values[i], parameters.DataFormat);
                regs[i * 2] = pair[0];
                regs[i * 2 + 1] = pair[1];
            }
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        #endregion

        #region 强类型写入 — Double

        public OperateResult WriteDouble(ushort address, double value, int? timeout = null)
        {
            ushort[] regs = ModbusUtils.ConvertFromDouble(value, parameters.DataFormat);
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteDoubleAsync(ushort address, double value, int? timeout = null)
        {
            ushort[] regs = ModbusUtils.ConvertFromDouble(value, parameters.DataFormat);
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        public OperateResult WriteDoubleArray(ushort address, double[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length * 4];
            for (int i = 0; i < values.Length; i++)
            {
                ushort[] quad = ModbusUtils.ConvertFromDouble(values[i], parameters.DataFormat);
                regs[i * 4] = quad[0];
                regs[i * 4 + 1] = quad[1];
                regs[i * 4 + 2] = quad[2];
                regs[i * 4 + 3] = quad[3];
            }
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs));
        }

        public async Task<OperateResult> WriteDoubleArrayAsync(ushort address, double[] values, int? timeout = null)
        {
            if (values == null || values.Length == 0)
                return OperateResult.Fail("数组为空");
            ushort[] regs = new ushort[values.Length * 4];
            for (int i = 0; i < values.Length; i++)
            {
                ushort[] quad = ModbusUtils.ConvertFromDouble(values[i], parameters.DataFormat);
                regs[i * 4] = quad[0];
                regs[i * 4 + 1] = quad[1];
                regs[i * 4 + 2] = quad[2];
                regs[i * 4 + 3] = quad[3];
            }
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs)).ConfigureAwait(false);
        }

        #endregion

        #region 强类型写入 — String

        public OperateResult WriteString(ushort address, string value, int? timeout = null)
            => WriteString(address, value, 0, timeout);

        public async Task<OperateResult> WriteStringAsync(ushort address, string value, int? timeout = null)
            => await WriteStringAsync(address, value, 0, timeout).ConfigureAwait(false);

        public OperateResult WriteString(ushort address, string value, ushort registerCount, int? timeout = null)
        {

            byte[] bytes = parameters.TextEncoding.GetBytes(value ?? string.Empty);

            int actualRegCount;
            if (registerCount > 0)
            {
                actualRegCount = registerCount;
            }
            else
            {
                // 自动计算：内容字节 + 1字节'\0'终止符，向上取整到寄存器个数
                // 公式：(bytes.Length + 1 + 1) / 2
                //  第一个 +1：给 '\0' 留 1 个字节
                //  第二个 +1：配合整数除法实现向上取整（ceil）
                actualRegCount = (bytes.Length + 1 + 1) / 2;
            }

            if (actualRegCount > 255)
                return OperateResult.Fail("字符串超出最大寄存器限制");

            ushort[] regs = ModbusUtils.ConvertFromString(value, actualRegCount, parameters.DataFormat, parameters.TextEncoding);
            return PerformWithRetry(() => NativeMaster.WriteMultipleRegisters(parameters.SlaveId, address, regs), timeout);
        }

        public async Task<OperateResult> WriteStringAsync(ushort address, string value, ushort registerCount, int? timeout = null)
        {

            byte[] bytes = parameters.TextEncoding.GetBytes(value ?? string.Empty);
            int actualRegCount;
            if (registerCount > 0)
            {
                actualRegCount = registerCount;
            }
            else
            {
                actualRegCount = (bytes.Length + 1 + 1) / 2;
            }

            if (actualRegCount > 255)
                return OperateResult.Fail("字符串超出最大寄存器限制");

            ushort[] regs = ModbusUtils.ConvertFromString(value, actualRegCount, parameters.DataFormat, parameters.TextEncoding);
            return await PerformWithRetryAsync(() => NativeMaster.WriteMultipleRegistersAsync(parameters.SlaveId, address, regs), timeout).ConfigureAwait(false);
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed) return;
            if (disposing)
            {
                // 1. 通知后台任务取消
                lifetimeCts?.Cancel();

                // 2. 非阻塞尝试获取锁（0 毫秒超时），绝不等待 UI
                bool lockTaken = false;
                try
                {
                    lockTaken = semaphore.Wait(0);

                    // 3. 无论是否拿到锁，都强制断开物理连接
                    //    拿到锁：安全断开；拿不到：暴力踢掉，业务线程会在异常中释放锁。
                    DisconnectCore();
                    UpdateState(ConnectionState.Disconnected);
                }
                finally
                {
                    if (lockTaken) semaphore.Release();
                }

                // 4. 清空任务引用（辅助 GC，且绝对不阻塞）
                Interlocked.Exchange(ref heartbeatTask, null);

                // 5. 释放托管资源
                semaphore?.Dispose();
                lifetimeCts?.Dispose();
                logger?.Dispose();
                logger = null;
            }
            disposed = true;
        }

        #endregion
    }
}