using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Stateless;
using Taoyl.Application.TaskProcess.Interfaces;
using Taoyl.Application.TaskProcess.Models;

namespace Taoyl.Application.TaskProcess.Services
{
    /// <summary>
    /// 流程引擎抽象基类（Stateless 单层五态状态机版）。
    ///
    /// 状态流转清单（单层五态，状态即线程生死）：
    ///   Idle      --Start---------> Running    唯一启动入口，建 looper 线程（待命态）
    ///   Stopped   --Start---------> Running    重启（跑过之后）
    ///   Running   --Pause---------> Paused      步间暂停语义（本步执行完才停）
    ///   Running   --RaiseException-> Exception 步序异常 / 健康检查失败 / 循环异常统一入口
    ///   Paused    --Resume--------> Running     放行门控，步序继续
    ///   Paused    --RaiseException-> Exception
    ///   Exception --Reset----------> Paused     软恢复：步序不归零，清异常，从异常步重试
    ///   Running/Paused/Exception --Stop-->    Stopped  杀线程并等待 looper 退出
    ///   Running/Paused/Exception --Finish-->  Stopped  looper 退出收尾自转
    ///   Stopped   --Initialize----> Idle       重新初始化回待命（两段式第二段）
    ///   Idle      --Initialize----> Idle       重入幂等（清步序清异常）
    ///   Idle/Stopped --Stop-------> 自身        重入幂等（重入触发 OnEntry 即 Cancel，无害）
    ///
    /// 关键不变式与规则：
    /// 1. 线程死则状态必停：looper 退出必经收尾 Finish 自转 → Stopped，
    ///    Complete 自杀路径（无 Stop 调用）状态不滞留 Running（治 L1 僵尸）。
    /// 2. 状态唯一写入口 = 状态机 Fire（stateGate 锁内 CanFire→Fire），任何路径禁止直赋状态字段。
    /// 3. 🔴 进 Idle 唯一路径 = Initialize 触发，且必须两段式：先 StopAsync 杀线程（→ Stopped），
    ///    再 Fire(Initialize)（→ Idle）。禁止活跃态直达 Idle——否则"状态说线程不存在但线程还活着"。
    ///    初始化动作本身也走状态机（Initialize 触发器），不留 StopAsync+静默清字段的暗道。
    /// 4. stateGate 每实例独立锁：Stateless 官方声明非线程安全（README: may not be used
    ///    concurrently），CanFire/Fire/快照/GetPermittedTriggers 一律锁内执行；
    ///    🔴 锁内禁 await——looper 收尾自转要拿同锁，锁内等待必死锁（StopAsync 模式：锁内 Fire + 锁外 await）。
    /// 5. Reset 语义 = 软恢复：步序不归零，清除异常，从异常步重试（钉死规则，勿改"归零"）。
    /// 6. 语义收窄：Start@Paused/Exception、Pause@Paused、Exception 态重复异常 = 拒绝（CanFire=false），
    ///    消费者零影响（UI 已按触发器分流 + 批量操作忽略返回值）。
    /// 7. 故障派发语义：Faulted 事件在**锁外**同步派发，执行在触发故障的线程上
    ///    （looper 或控制调用方）；Coordinator 收到后先交外层落库（报警），再联动暂停其他流程。
    /// 8. 异常单出口：TriggerException 构造 ProcessFault 赋 CurrentException（锁内），
    ///    🔴 锁外派发 Faulted——订阅方会做报警落库（DB I/O），持 stateGate 派发会阻塞一切状态读写。
    ///    Faulted 是 internal：外部只能经 IProcessCoordinator.ProcessFaulted 拿故障，出口唯一。
    ///    基座不认识报警库：落库由外层实现（ProcessFaulted → Router → AlarmService.Raise）。
    ///    子类业务报警不走基座：需要报警的子类自行注入 IAlarmService 直调 Raise（不经状态机门控）。
    /// 9. 构造零依赖：基座不认识报警 / 日志 / 设备 / 上下文；子类需要什么自己注入。
    /// 10. 生命周期终点 = Dispose（协调器在 ClearAllProcesses 里逐个调用，幂等）。
    ///    子类在 OnDispose 里退订事件 / 释放自有资源——**订阅放构造、退订放 OnDispose**，成对。
    ///    🔴 Dispose 前必须已 StopAsync：活线程上释放 = 退了订阅但 looper 还在跑。
    /// </summary>
    public abstract class ProcessBase : IProcess
    {
        #region 状态机核心（字段与锁）

        /// <summary>全部触发器（CanFire 逐个探测用，静态缓存；GetPermittedTriggers 同步版已弃用）</summary>
        private static readonly ProcessTrigger[] AllTriggers =
            (ProcessTrigger[])Enum.GetValues(typeof(ProcessTrigger));

        /// <summary>流程状态（单字段真相源，StateMachine 经委托构造器读写此字段；初始待命）</summary>
        private ProcessState state = ProcessState.Idle;

        /// <summary>
        /// Stateless 状态机实例（转换规则与副作用表达）。
        /// 官方声明非线程安全，全部访问必须经 stateGate 锁（CanFire/Fire/State/GetPermittedTriggers）。
        /// </summary>
        private readonly StateMachine<ProcessState, ProcessTrigger> machine;

        /// <summary>
        /// 状态机锁（每实例独立）。
        /// 🔴 锁内禁 await：looper 收尾自转要拿同锁，锁内等待必死锁。
        /// </summary>
        private readonly object stateGate = new object();

        /// <summary>
        /// 取消令牌源。不 Dispose（无 Timer 的 CTS 无非托管资源，
        /// StartLooper 覆盖引用后旧实例交由 GC 回收）。
        /// </summary>
        private CancellationTokenSource cts;

        /// <summary>暂停门控信号（初始置位；Paused/Exception 态 looper 阻塞其上，Resume 放行）</summary>
        private ManualResetEventSlim pauseSignal = new ManualResetEventSlim(true);

        /// <summary>looper 线程任务（StopAsync 锁外 await 等待退出）</summary>
        private Task looperTask;

        #endregion

        #region 状态属性（IProcess 契约）

        /// <summary>流程状态（锁内取值，与状态机运行时同源）</summary>
        public ProcessState State
        {
            get { lock (stateGate) { return machine.State; } }
        }

        #endregion

        #region 公共数据（步序 / 故障 / 配置 / 标识）

        /// <summary>当前步序号</summary>
        public int CurrentStep { get; protected set; }

        /// <summary>
        /// 当前故障（null 表示无异常）。状态快照的 ExceptionCode / ExceptionMessage 取自此处。
        /// 只含「来源 + 码 + 描述 + 时刻」；报警记录（Id / 处理痕迹）在报警域，由外层落库后自己管。
        /// </summary>
        public ProcessFault CurrentException { get; private set; }

        /// <summary>流程行为配置</summary>
        public ProcessBehaviorConfig BehaviorConfig { get; protected set; }

        /// <summary>流程名称（唯一标识）</summary>
        public string ProcessName { get; protected set; }

        /// <summary>流程类别</summary>
        public ProcessCategory Category { get; protected set; }

        /// <summary>步序间隔等待时长（毫秒）</summary>
        public int WaitTime { get; set; }

        #endregion

        #region 构造与状态机配置

        /// <summary>
        /// 构造函数。初始化默认值并装配状态机转换规则（初始态 = Idle 待命）。
        /// 🔴 零参数：基座不认识报警 / 日志 / 设备 / 上下文。故障经 Faulted 交给基座内的
        ///    Coordinator 汇聚，再由 ProcessFaulted 交外层落库；子类需要什么能力自己注入。
        /// </summary>
        protected ProcessBase()
        {
            ProcessName = "Unnamed";
            Category = ProcessCategory.Production;
            WaitTime = 200;
            CurrentStep = 10;
            BehaviorConfig = new ProcessBehaviorConfig();

            // 状态存自家字段（单字段真相源），StateMachine 经委托构造器读写
            machine = new StateMachine<ProcessState, ProcessTrigger>(() => state, s => state = s);

            ConfigureStateMachine();
        }

        /// <summary>
        /// 配置状态机转换规则（单层五态全图，每态一块看全进出与副作用）。
        /// </summary>
        private void ConfigureStateMachine()
        {
            machine.Configure(ProcessState.Idle)
                .Permit(ProcessTrigger.Start, ProcessState.Running)     // 唯一启动入口（待命态）
                .PermitReentry(ProcessTrigger.Stop)                     // 待命再停 = 幂等（无害）
                .PermitReentry(ProcessTrigger.Initialize)               // 初始化幂等（清步序清异常）
                .OnEntryFrom(ProcessTrigger.Initialize, () =>
                {
                    CurrentStep = 10;
                    CurrentException = null;
                });

            machine.Configure(ProcessState.Stopped)
                .Permit(ProcessTrigger.Start, ProcessState.Running)     // 重启（跑过之后）
                .Permit(ProcessTrigger.Initialize, ProcessState.Idle)   // 重新初始化回待命（两段式第二段）
                .PermitReentry(ProcessTrigger.Stop)                     // 已停再停 = 幂等（重入触发 OnEntry 即 Cancel，无害）
                .OnEntry(() => cts?.Cancel());                          // Stop/Finish 进入即杀令牌

            machine.Configure(ProcessState.Running)
                .Permit(ProcessTrigger.Pause, ProcessState.Paused)
                .Permit(ProcessTrigger.RaiseException, ProcessState.Exception)
                .Permit(ProcessTrigger.Stop, ProcessState.Stopped)
                .Permit(ProcessTrigger.Finish, ProcessState.Stopped)
                .OnEntryFrom(ProcessTrigger.Start, StartLooper)         // 建线程：仅启动路径（Resume 进入不触发）
                .OnEntryFrom(ProcessTrigger.Resume, () => pauseSignal.Set());

            machine.Configure(ProcessState.Paused)
                .Permit(ProcessTrigger.Resume, ProcessState.Running)
                .Permit(ProcessTrigger.RaiseException, ProcessState.Exception)
                .Permit(ProcessTrigger.Stop, ProcessState.Stopped)
                .Permit(ProcessTrigger.Finish, ProcessState.Stopped)
                .OnEntry(() => pauseSignal.Reset())                     // Pause/Reset 共用门控
                .OnEntryFrom(ProcessTrigger.Reset, () => CurrentException = null);  // 仅复位清异常

            machine.Configure(ProcessState.Exception)
                .Permit(ProcessTrigger.Reset, ProcessState.Paused)      // 软恢复：步序不归零，从异常步重试
                .Permit(ProcessTrigger.Stop, ProcessState.Stopped)
                .Permit(ProcessTrigger.Finish, ProcessState.Stopped)
                .OnEntry(() => pauseSignal.Reset());
        }

        #endregion

        #region 公共控制方法（统一形态：锁内 CanFire → Fire）

        /// <summary>
        /// 启动流程。Idle/Stopped → Running（建 looper 线程）。
        /// Paused/Exception 态启动 = 拒绝（语义收窄：暂停走继续、异常走复位，启动不越权）。
        /// </summary>
        public bool Start()
        {
            try
            {
                lock (stateGate)
                {
                    if (!machine.CanFire(ProcessTrigger.Start)) return false;
                    machine.Fire(ProcessTrigger.Start);
                    return true;
                }
            }
            catch (Exception ex)
            {
                // 🔴 锁外派发：looper 创建失败发生在 Fire 回调内（持 stateGate），
                //    报警落库是 DB I/O，持锁派发会阻塞一切状态读写。异常原样上抛（与改造前一致）。
                RaiseFaulted(new ProcessFault
                {
                    Source = ProcessName,
                    Code = "E000",
                    Message = "looper 创建失败：" + ex.Message,
                    OccurredAt = DateTime.Now
                });
                throw;
            }
        }

        /// <summary>
        /// 暂停。Running → Paused（本步执行完才停，步间暂停语义由 looper 门控保证）。
        /// </summary>
        public bool Pause()
        {
            lock (stateGate)
            {
                if (!machine.CanFire(ProcessTrigger.Pause)) return false;
                machine.Fire(ProcessTrigger.Pause);
                return true;
            }
        }

        /// <summary>
        /// 继续。Paused → Running（放行门控，步序继续推进）。
        /// 异常态不允许直接继续，必须先复位。
        /// </summary>
        public bool Resume()
        {
            lock (stateGate)
            {
                if (!machine.CanFire(ProcessTrigger.Resume)) return false;
                machine.Fire(ProcessTrigger.Resume);
                return true;
            }
        }

        /// <summary>
        /// 复位。Exception → Paused。
        /// 软恢复：不杀线程，步序不归零，清除异常，从异常步重试（等待人工继续）。
        /// </summary>
        public bool Reset()
        {
            lock (stateGate)
            {
                if (!machine.CanFire(ProcessTrigger.Reset)) return false;
                machine.Fire(ProcessTrigger.Reset);
                return true;
            }
        }

        /// <summary>
        /// 停止流程。任意活跃态 → Stopped（杀令牌并等待 looper 退出）。
        /// Idle/Stopped 态重复停止 = 幂等（PermitReentry，返回 true）。
        /// 🔴 await 必须锁外：looper 收尾自转要拿 stateGate，锁内等待必死锁。
        /// 🔴 looperTask 引用必须锁内捕获：StartLooper 在锁内覆盖该字段，锁外读会拿到
        /// 他人重启后的新线程引用（停止后立即重启的连点场景），await 错对象假死；
        /// 锁外置 null 同理会偷清新引用 → 已完成 task 的引用留存无害（下次 await 立即返回），
        /// 与 cts 不 Dispose 同规则。
        /// </summary>
        public async Task<bool> StopAsync()
        {
            Task task;
            lock (stateGate)
            {
                if (!machine.CanFire(ProcessTrigger.Stop)) return false;
                machine.Fire(ProcessTrigger.Stop);          // Stopped.OnEntry: cts.Cancel() 杀令牌
                task = looperTask;                          // 锁内捕获（与 StartLooper 的锁内写窗口闭合）
            }

            if (task != null)
            {
                try { await task; }
                catch (OperationCanceledException) { }
            }

            return true;
        }

        /// <summary>
        /// 初始化流程（两段式，走状态机，治 L3）。
        /// 第一段：幂等停止（活跃态杀线程并等待 looper 退出 → Stopped；已停/待命则重入幂等直过）；
        /// 第二段：Fire(Initialize) → Idle（清步序清异常，副作用挂 OnEntryFrom）。
        /// 🔴 两段之间有窗口：若此刻被其他线程 Start 抢先，CanFire(Initialize)=false，放弃初始化（返回 false）。
        /// 由 Coordinator 在首次注册、单流程初始化、全量初始化时调用。
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            // 第一段：幂等停止（消除并发写者）
            await StopAsync();

            // 第二段：初始化回待命（状态机转换，禁止直赋状态）
            lock (stateGate)
            {
                if (!machine.CanFire(ProcessTrigger.Initialize)) return false;
                machine.Fire(ProcessTrigger.Initialize);
                // 业务初始化锁内执行：Fire 后状态即 Idle，锁外执行会被抢先 Start 的 looper
                // 与之并发（"状态说待命就绪而初始化未完成"的收敛缺口，L1 同构）；
                // 必须在 Fire 之后——OnEntryFrom(Initialize) 会清步序清异常，先跑会被覆盖。
                // 🔴 契约：InitProcess 必须轻量（毫秒级），重活留到 Start 后的步序里做。
                InitProcess();
            }

            return true;
        }

        #endregion

        #region 释放（生命周期终点）

        /// <summary>已释放标志（协调器可能重复触发清空，Dispose 必须幂等）</summary>
        private bool disposed;

        /// <summary>
        /// 释放流程（生命周期终点）。由 ProcessCoordinator.ClearAllProcesses 在**停止之后**逐个调用。
        /// 幂等：重复调用只执行一次。子类异常在此隔离——一个流程释放失败不影响同批其它流程。
        /// 🔴 不杀线程：调用前必须已 StopAsync（活线程上释放 = 退了订阅但 looper 还在跑）。
        /// 🔴 释放后实例即废：InitializeProcessAsync 走的是同实例重置，不经过这里。
        /// </summary>
        public void Dispose()
        {
            if (disposed) return;
            disposed = true;

            try
            {
                OnDispose();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format("[{0}] 释放异常：{1}", ProcessName, ex));
            }
        }

        /// <summary>
        /// 子类释放点：退订事件（设备变更 / 总线 / 定时器）、释放自己持有的资源。
        /// 🔴 订阅放构造、退订放这里——放 InitProcess 会随每次 Initialize 重入累积处理器。
        /// 没有需要释放的写空方法体：抽象强制实现，是为了让每个新流程都过一遍这个判断。
        /// </summary>
        protected abstract void OnDispose();

        #endregion

        #region 异常触发（统一入口）

        /// <summary>
        /// 触发异常（统一入口）。Running/Paused → Exception。
        /// 🔴 门控：已停止/待命（迟到期异常）或已处于异常态（重复异常）时，不写状态不上报（治 L4 误联动）。
        /// CurrentException 即本次故障（ProcessFault，供状态快照读取）；
        /// 故障经 Faulted 事件（锁外）交给 Coordinator，再由 ProcessFaulted 交外层落库报警。
        /// </summary>
        private void TriggerException(string code, string message)
        {
            ProcessFault fault;
            lock (stateGate)
            {
                if (!machine.CanFire(ProcessTrigger.RaiseException)) return;
                machine.Fire(ProcessTrigger.RaiseException);
                fault = new ProcessFault
                {
                    Source = ProcessName,
                    Code = code,
                    Message = message,
                    OccurredAt = DateTime.Now
                };
                CurrentException = fault;
            }

            RaiseFaulted(fault);        // 🔴 锁外：订阅方会做报警落库（DB I/O）
        }

        #endregion

        #region 故障出口（基座内事件 → Coordinator 汇聚）

        /// <summary>
        /// 故障事件。internal：只有基座内的 ProcessCoordinator 能订阅——
        /// 外部一律走 IProcessCoordinator.ProcessFaulted，保证「异常单出口」。
        /// </summary>
        internal event EventHandler<ProcessFault> Faulted;

        /// <summary>
        /// 派发故障（🔴 必须在锁外调用）。
        /// 订阅方异常隔离：不允许把订阅方异常回灌 looper（会变成 E999 死循环）。
        /// 无订阅方 = 流程未注册到协调器，故障无人接收（配置错误），走 Trace 兜底不静默。
        /// </summary>
        private void RaiseFaulted(ProcessFault fault)
        {
            var handler = Faulted;
            if (handler == null)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format("[{0}] 未注册到协调器，故障无人接收：[{1}] {2}", ProcessName, fault.Code, fault.Message));
                return;
            }

            try
            {
                handler(this, fault);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Trace.TraceError(
                    string.Format("[{0}] 故障订阅方异常：[{1}] {2}", ProcessName, fault.Code, ex));
            }
        }

        #endregion

        #region 内部执行循环

        /// <summary>
        /// 创建 looper 线程。由 Running 态 OnEntryFrom(Start) 触发（锁内执行）。
        /// StartNew 为非阻塞调度（LongRunning 提示建专用线程），不拖长锁持有时间。
        /// 创建失败时显式回转 Stopped（缓解 Stateless OnEntry 动作失败不回滚），异常原样上抛。
        /// </summary>
        private void StartLooper()
        {
            try
            {
                cts = new CancellationTokenSource();
                pauseSignal.Set();
                CurrentException = null;
                looperTask = Task.Factory.StartNew(
                    () => ExecuteLoop(cts.Token),
                    cts.Token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
            catch
            {
                machine.Fire(ProcessTrigger.Stop);      // 显式回转 Stopped（此处在 Fire(Start) 回调内，状态已更新为 Running）
                throw;                                  // 由 Start 的 catch 统一报 E000（锁外派发）
            }
        }

        /// <summary>
        /// 执行循环。线程入口（LongRunning 专用线程）。
        ///
        /// 循环逻辑：
        /// 1. 检查取消令牌
        /// 2. 按状态决定行为（读 state 字段，Paused/Exception 即阻塞条件）：
        ///    - Running：正常执行步序
        ///    - Paused/Exception：阻塞在门控信号上，等待 Resume（异常需先 Reset）
        /// 3. 处理步序结果，可能触发状态变化
        /// 4. 异常捕获，触发 Exception 状态
        ///
        /// 收尾自转（治 L1）：线程退出时若仍处活跃态（Complete 自杀路径），
        /// Fire(Finish) 收敛到 Stopped；已被 Stop 转入 Stopped 时 CanFire=false 自然跳过。
        /// </summary>
        private void ExecuteLoop(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    // 暂停或异常时：阻塞等待，但定期检查取消令牌
                    if (state == ProcessState.Paused || state == ProcessState.Exception)
                    {
                        pauseSignal.Wait(token);
                        if (token.IsCancellationRequested) break;
                    }

                    // 健康检查
                    if (!CheckHealth())
                    {
                        TriggerException("E001", "健康检查失败");
                        token.WaitHandle.WaitOne(WaitTime);
                        continue;
                    }

                    ProcessStepResult result = ExecuteStep(CurrentStep, token);
                    HandleStepResult(result);

                    // 步序间隔等待
                    token.WaitHandle.WaitOne(WaitTime);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    TriggerException("E999", ex.Message);
                    token.WaitHandle.WaitOne(1000);
                }
            }

            // 收尾自转：线程死则状态必停（治 L1 僵尸）
            lock (stateGate)
            {
                if (machine.CanFire(ProcessTrigger.Finish))
                {
                    machine.Fire(ProcessTrigger.Finish);
                }
            }
        }

        /// <summary>
        /// 处理步序结果，触发状态变化。
        /// Complete 分支仅杀令牌使 looper 退出，状态由 ExecuteLoop 收尾 Finish 自转收敛。
        /// </summary>
        private void HandleStepResult(ProcessStepResult result)
        {
            switch (result.Action)
            {
                case ProcessStepResult.StepAction.Next:
                    CurrentStep += 10;
                    break;

                case ProcessStepResult.StepAction.Stay:
                    // 保持当前步序，什么都不做
                    break;

                case ProcessStepResult.StepAction.Jump:
                    CurrentStep = result.TargetStep.Value;
                    break;

                case ProcessStepResult.StepAction.Exception:
                    TriggerException(result.ExceptionCode, result.Message);
                    break;

                case ProcessStepResult.StepAction.Complete:
                    cts?.Cancel();
                    break;
            }
        }

        #endregion

        #region 健康检查（子类可重写）

        /// <summary>
        /// 健康检查。子类重写以添加自定义检查逻辑。
        /// 返回 false 时，基类会自动触发 Exception 状态。
        /// </summary>
        protected virtual bool CheckHealth()
        {
            return true;
        }

        #endregion

        #region 超时辅助（时间源维持 DateTime.Now，10-08 决定不换源，一行不动）

        private DateTime timeoutStart;

        protected void SetTimeout()
        {
            timeoutStart = DateTime.Now;
        }

        protected bool CheckTimeout(int milliseconds)
        {
            return (DateTime.Now - timeoutStart).TotalMilliseconds > milliseconds;
        }

        #endregion

        #region 抽象方法：子类必须实现

        /// <summary>
        /// 执行指定步序的业务逻辑。
        /// 子类根据当前步序号执行对应操作，返回结果指导状态机流转。
        /// </summary>
        protected abstract ProcessStepResult ExecuteStep(int step, CancellationToken token);

        /// <summary>
        /// 初始化流程。InitializeAsync 第二段锁内调用（Fire 之后），设置流程名称、初始步序等。
        /// 🔴 必须轻量（毫秒级）：执行期间持有 stateGate，阻塞一切状态读写与控制操作。
        /// </summary>
        public virtual void InitProcess() { }

        #endregion

        #region 状态快照

        /// <summary>
        /// 构建流程状态快照（监控、事件广播和 UI 展示）。
        /// 锁内取值：State 与 PermittedTriggers 同一锁窗口求值，保证一致
        /// （UI 按钮判定与状态机运行时同源）。
        /// PermittedTriggers 用 CanFire 逐触发器探测：同步 GetPermittedTriggers 已被
        /// Stateless 5.18+ 弃用（无法评估 async guard），CanFire 未弃用且语义等价。
        /// </summary>
        public ProcessStatus GetStatusSnapshot()
        {
            lock (stateGate)
            {
                return new ProcessStatus
                {
                    ProcessName = ProcessName,
                    Category = Category,
                    State = machine.State,
                    PermittedTriggers = AllTriggers.Where(t => machine.CanFire(t)).ToArray(),
                    Step = CurrentStep,
                    ExceptionCode = CurrentException?.Code,
                    ExceptionMessage = CurrentException?.Message,
                    Timestamp = DateTime.Now
                };
            }
        }

        #endregion
    }
}
