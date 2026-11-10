using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taoyl.Application.TaskProcess.Interfaces;
using Taoyl.Application.TaskProcess.Models;

namespace Taoyl.Application.TaskProcess.Services
{
    /// <summary>
    /// 流程协调器。管理所有流程实例的生命周期。
    ///   聚合状态规则（单层五态版）：
    /// - 线程活（Running/Paused/Exception）才参与聚合；Idle/Stopped/禁用不参与
    /// - 有任意流程 Exception → 整体 Exception（最高优先级）
    /// - 有任意流程 Running → 整体 Running
    /// - 其余（全部 Paused）→ 整体 Paused
    /// - 全部待命/停止或禁用 → 整体 Idle
    ///
    /// 基座内闭环（10-18 定稿）：
    /// 1. 故障汇聚：Register 时订阅每个流程的 ProcessBase.Faulted（同程序集 internal 事件）——
    ///    收到后先 ProcessFaulted 对外（外层 Router → 报警落库），再联动暂停其他流程。
    ///    订阅由基座自己完成，不靠外部纪律（漏挂 = 静默丢报警）。
    /// 2. 基座不引总线：全量重建改为普通委托 ProcessesNeedRebuild，注册器在外层订阅。
    /// 3. 基座不引日志库：运维流水走 IProcessLogSink 端口（构造注入）。
    ///    流程故障不在此记日志——报警已落库，双写是冗余。
    /// </summary>
    public class ProcessCoordinator : IProcessCoordinator
    {
        private readonly ConcurrentDictionary<string, IProcess> processes = new ConcurrentDictionary<string, IProcess>();
        private readonly IProcessLogSink log;

        /// <summary>接线自检只做一次（首次注册时），避免 30 个流程刷 30 条同样的 Error。</summary>
        private bool faultOutletChecked;

        public ProcessCoordinator(IProcessLogSink logSink)
        {
            log = logSink ?? throw new ArgumentNullException(nameof(logSink));
        }

        /// <summary>
        /// 全量重建请求（原 EventBus + AllProcessesNeedInitEvent → 普通委托）。
        /// 由外层注册器订阅，收到后重新从容器 Resolve 全部流程并 Register。
        /// </summary>
        public event Action ProcessesNeedRebuild;

        /// <summary>
        /// 流程故障出口（基座对外唯一）。
        /// 外层订阅后转成报警落库（弹窗 / 查询页）；无订阅方时走 Trace 兜底，不静默丢弃。
        /// </summary>
        public event EventHandler<ProcessFault> ProcessFaulted;

        public IReadOnlyCollection<IProcess> Processes
        {
            get { return processes.Values.ToList().AsReadOnly(); }
        }

        public void Register(IProcess process)
        {
            if (process == null) throw new ArgumentNullException(nameof(process));

            // 首次创建：完整初始化（框架状态 + 业务上下文）
            // 🔴 必须在入表前：ProcessName 由 InitProcess() 赋值（InitializeAsync 内部调用），
            //    初始化前恒为 "Unnamed"，提前查重会让第 2 个流程就撞车、启动即崩。
            process.InitializeAsync().GetAwaiter().GetResult();

            // 查重：同名静默覆盖会顶掉旧实例且不释放（100 流程规模必踩），改为快速失败。
            // TryAdd 是原子的：并发注册同名时只有一个成功，另一个走到异常分支。
            if (!processes.TryAdd(process.ProcessName, process))
                throw new InvalidOperationException(
                    string.Format("流程 [{0}] 已注册，禁止同名覆盖", process.ProcessName));

            // 故障汇聚：注册即接管。非 ProcessBase 实现（未来别处来的 IProcess）没有 Faulted，
            // 需自行保证故障上报——基座不替它兜底。
            var source = process as ProcessBase;
            if (source != null) source.Faulted += OnProcessFaulted;

            log.Info(string.Format("流程 [{0}] 已注册", process.ProcessName));

            // 接线自检：故障出口无订阅方 = 报警链路没接上（漏 Resolve ProcessFaultRouter）。
            // 启动即暴露，不等现场出事才发现"报警不弹"。
            if (!faultOutletChecked)
            {
                faultOutletChecked = true;
                if (ProcessFaulted == null)
                    log.Error(null, "故障出口未接线：ProcessFaulted 无订阅方，流程故障将不会落库报警");
            }
        }

        public IProcess GetProcess(string name)
        {
            return processes.TryGetValue(name, out var p) ? p : null;
        }

        /// <summary>
        /// 获取系统聚合状态（只考虑允许人工暂停的主流程）。
        /// 单层判定：线程活（非 Stopped）才有聚合意义；异常 > 运行 > 暂停。
        /// </summary>
        public SystemProcessState GetSystemState()
        {
            // 只考虑：启用的、允许人工暂停的（即主流程）
            var mainProcesses = processes.Values
                .Where(p => p.BehaviorConfig.IsEnabled && p.BehaviorConfig.AllowManualPause)
                .ToList();

            // 线程活 = Running/Paused/Exception；Idle（待命）/Stopped（停止）/禁用不参与聚合
            var active = mainProcesses
                .Where(p => p.State != ProcessState.Stopped && p.State != ProcessState.Idle)
                .ToList();

            if (active.Count == 0)
                return SystemProcessState.Idle;

            // 有主流程异常 → Exception（最高优先级）
            if (active.Any(p => p.State == ProcessState.Exception))
                return SystemProcessState.Exception;

            // 有主流程正常运行 → Running
            if (active.Any(p => p.State == ProcessState.Running))
                return SystemProcessState.Running;

            // 剩余全部暂停 → Paused
            return SystemProcessState.Paused;
        }

        // ========== 批量控制 ==========

        /// <summary>
        /// 批量启动。逐流程按自身状态分流：Idle/Stopped 启动、Paused 恢复、Running 忽略。
        /// "运行"语义 = 让每条流程跑起来，混合态（部分待命 + 部分暂停）各自归位，无漏网。
        /// 读 State 与 Fire 两次拿锁间的竞态无害：Start/Resume 内部 CanFire 再验证，最坏返回 false。
        /// </summary>
        public void StartAll()
        {
            ForEachEnabled(p =>
            {
                if (p.State == ProcessState.Stopped || p.State == ProcessState.Idle)
                    p.Start();
                else if (p.State == ProcessState.Paused)
                    p.Resume();
                // Running：已在跑，忽略
            });
        }

        public void PauseAll()
        {
            ForEachEnabled(p =>
            {
                if (p.BehaviorConfig.AllowManualPause)
                    p.Pause();
            });
        }

        public void ResumeAll()
        {
            ForEachEnabled(p => p.Resume());
        }

        public void ResetAll()
        {
            ForEachEnabled(p => p.Reset());
        }

        public async Task StopAllAsync()
        {
            var tasks = processes.Values.Select(p => p.StopAsync()).ToArray();
            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// 初始化单个流程（轻量级重置）。
        /// 直接操作目标流程：停止线程、清理状态、调用业务初始化。
        /// 保留原实例，不影响其他流程。
        /// </summary>
        public async Task InitializeProcessAsync(string processName)
        {
            if (!processes.TryGetValue(processName, out var process))
            {
                log.Warn(string.Format("流程 [{0}] 不存在，无法初始化", processName));
                return;
            }

            var ok = await process.InitializeAsync();
            if (ok)
                log.Info(string.Format("流程 [{0}] 已初始化", processName));
            else
                log.Warn(string.Format("流程 [{0}] 初始化放弃（窗口期被抢先启动）", processName));
        }

        /// <summary>
        /// 初始化所有流程（彻底重建）。
        /// 策略：全部停止 → 清空字典 → 触发重建请求（普通委托）→ 注册器重新注册。
        /// 从容器重新 Resolve 新实例，彻底清理残留状态。
        /// </summary>
        public async Task InitializeAllAsync()
        {
            // 1. 全部停止
            await StopAllAsync();

            // 2. 清空现有流程（解绑事件，释放引用，等待GC）
            ClearAllProcesses();

            // 3. 触发重建请求，由外层注册器重新 Init()
            var handler = ProcessesNeedRebuild;
            if (handler == null)
            {
                log.Warn("全量重建失败：无订阅方（注册器未接入）");
                return;
            }

            try { handler(); }
            catch (Exception ex) { log.Error(ex, "全量重建失败（注册器异常）"); }

            log.Info("所有流程已重建");
        }

        /// <summary>
        /// 清空并释放所有流程（生命周期终点：逐个 Dispose 后清表）。
        /// 🔴 调用前必须已 StopAllAsync——活线程上释放会留下「订阅已退、looper 还在跑」的僵尸。
        ///    InitializeAllAsync 已保证这个顺序（先 StopAllAsync 再本方法）。
        /// 🔴 逐个隔离：某个流程 Dispose 抛异常不影响同批其它流程，也不阻止字典清空。
        /// 🔴 保留原实例的单流程初始化（InitializeProcessAsync）不走这里——那是同实例重置。
        /// </summary>
        public void ClearAllProcesses()
        {
            foreach (var p in processes.Values)
            {
                // 兜底提示：走到这里说明调用方漏了 StopAllAsync（Dispose 本身不杀线程）
                if (p.State != ProcessState.Stopped && p.State != ProcessState.Idle)
                {
                    log.Warn(string.Format("流程 [{0}] 在活跃态（{1}）被释放：调用前应先 StopAllAsync",
                        p.ProcessName, p.State));
                }

                try { p.Dispose(); }
                catch (Exception ex) { log.Error(ex, string.Format("[{0}] 释放失败", p.ProcessName)); }
            }

            processes.Clear();
        }

        // ========== 故障汇聚 ==========

        /// <summary>
        /// 流程故障汇聚（跑在触发故障的线程上：流程 looper 或控制调用方）。
        /// 顺序固定：先对外落库（报警），再联动暂停——与改造前「Raise 落库 → 总线回调 → 暂停」等价。
        /// </summary>
        private void OnProcessFaulted(object sender, ProcessFault fault)
        {
            RaiseProcessFaulted(fault);          // ① 对外：报警落库（弹窗 / 查询页）
            PauseOthersOnFault(fault.Source);    // ② 联动暂停
        }

        /// <summary>
        /// 对外派发故障。订阅方异常隔离：报警链路出问题不许回灌流程 looper（会变 E999 死循环）。
        /// </summary>
        private void RaiseProcessFaulted(ProcessFault fault)
        {
            var handler = ProcessFaulted;
            if (handler == null)
            {
                System.Diagnostics.Trace.TraceError(string.Format(
                    "[{0}] 故障无订阅方（报警链路未接线）：[{1}] {2}", fault.Source, fault.Code, fault.Message));
                return;
            }

            try { handler(this, fault); }
            catch (Exception ex) { log.Error(ex, string.Format("故障订阅方异常：[{0}]", fault.Code)); }
        }

        /// <summary>
        /// 联动暂停：某流程异常后，暂停其他允许人工暂停的活跃流程。
        /// 白名单过滤：仅 Running/Paused 需要联动暂停（Exception 态已停等复位，
        /// Idle/Stopped 无线程；白名单式在未来加态时默认安全，防 Pause 被 CanFire 拒绝抛异常刷日志）。
        /// </summary>
        private void PauseOthersOnFault(string source)
        {
            var sender = processes.Values.FirstOrDefault(p => p.ProcessName == source);
            if (sender == null) return;

            if (!sender.BehaviorConfig.PauseOthersOnException) return;

            foreach (var p in processes.Values)
            {
                if (p != sender
                    && (p.State == ProcessState.Running || p.State == ProcessState.Paused)
                    && p.BehaviorConfig.AllowManualPause)
                {
                    try { p.Pause(); }
                    catch (Exception pauseEx)
                    {
                        log.Error(pauseEx, string.Format("暂停流程 [{0}] 失败", p.ProcessName));
                    }
                }
            }
        }

        // ========== 辅助方法 ==========

        private void ForEachEnabled(Action<IProcess> action)
        {
            foreach (var p in processes.Values.Where(p => p.BehaviorConfig.IsEnabled))
            {
                try { action(p); }
                catch (Exception ex) { log.Error(ex, string.Format("[{0}] 批量操作失败", p.ProcessName)); }
            }
        }
    }
}
