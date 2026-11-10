using Taoyl.Application.PlcDriver.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Taoyl.Application.PlcDriver.IO
{
    /// <summary>
    /// IO工作器（单线程调度循环）
    /// 循环：生成（IoExecutor.GeneratePollRequests）→ 入队 → 出队 → ExecuteSession(req, false) → 断连重连分支
    /// 职责边界：只管调度，执行/提交/状态更新全在 IoExecutor，重连直持 PlcConnectionState（省委托）
    /// </summary>
    public class IoWorker
    {
        #region 成员变量
        private readonly IoExecutor _executor;
        private readonly PlcConnectionState _connectionState;
        private readonly int _pollingTime;
        private readonly IPlcLogger _logger;
        private Task _task;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="executor">IO执行器（生成/入队/出队/会话执行）</param>
        /// <param name="connectionState">连接状态机（断连检测与重连触发）</param>
        /// <param name="pollingTime">轮询间隔（毫秒）</param>
        /// <param name="logger">日志</param>
        public IoWorker(IoExecutor executor, PlcConnectionState connectionState, int pollingTime, IPlcLogger logger)
        {
            _executor = executor ?? throw new ArgumentNullException(nameof(executor));
            _connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
            _pollingTime = pollingTime;
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }
        #endregion

        #region 启停
        /// <summary>
        /// 启动工作线程（LongRunning Task）
        /// </summary>
        public void Start(CancellationToken cancellationToken)
        {
            if (_task != null && !_task.IsCompleted)
                return;

            _task = Task.Factory.StartNew(
                () => RunAsync(cancellationToken),
                cancellationToken,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default
            );
            _logger.WriteDebug("IO工作器已启动");
        }

        /// <summary>
        /// 请求停止并等待工作线程退出
        /// </summary>
        /// <param name="waitMilliseconds">最大等待毫秒数</param>
        public void Stop(int waitMilliseconds)
        {
            if (_task == null || _task.IsCompleted)
                return;

            try
            {
                Task.WaitAny(_task, Task.Delay(waitMilliseconds));
            }
            catch (AggregateException)
            {
                // 任务取消时抛出的异常可以忽略
            }
        }
        #endregion

        #region 主循环
        /// <summary>
        /// 主循环：生成→入队→出队→ExecuteSession→断连重连
        /// </summary>
        private async Task RunAsync(CancellationToken cancellationToken)
        {
            _logger.WriteDebug($"IO工作器开始运行，间隔={_pollingTime}ms");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    if (!_connectionState.IsConnected)
                    {
                        // 断连状态：按退避节奏尝试重连（仅 IoWorker 触发重连）
                        if (_connectionState.ShouldAttemptReconnect())
                        {
                            await _connectionState.TryReconnectAsync(cancellationToken);
                        }
                        await Task.Delay(_pollingTime, cancellationToken);
                        continue;
                    }

                    // 连接正常：遍历地址表生成到期缓存读请求
                    var pollRequests = _executor.GeneratePollRequests();
                    if (pollRequests != null)
                    {
                        foreach (var req in pollRequests)
                        {
                            _executor.TryEnqueue(req);
                        }
                    }

                    // 直接读写期间暂停出队，避免与直接读写抢通讯
                    if (!_executor.IsDirectBusy && _executor.TryDequeue(out var request, 5))
                    {
                        // 会话执行（内部锁内 Execute + 锁外状态更新/提交）
                        _executor.ExecuteSession(request, false);
                    }
                    else
                    {
                        // 无请求或直接读写中，等待下一轮
                        await Task.Delay(_pollingTime, cancellationToken);
                    }
                }
                catch (OperationCanceledException)
                {
                    _logger.WriteDebug("IO工作器被取消");
                    break;
                }
                catch (Exception ex)
                {
                    _logger.WriteException("IO工作器异常", ex);
                    await Task.Delay(_pollingTime, cancellationToken);
                }
            }

            _logger.WriteDebug("IO工作器结束运行");
        }
        #endregion
    }
}
