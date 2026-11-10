using System;
using System.Collections.Generic;

namespace Taoyl.Application.TaskProcess.Models
{
    /// <summary>
    /// 流程状态（单层五态）。
    /// 线程生死：Idle/Stopped = 线程不存在；Running/Paused/Exception = 线程存活（仅行为差异）。
    /// Idle = 待命（构造后/初始化后，从未启动或已重新初始化）；Stopped = 跑过之后（人工停止/完成自转）。
    /// 🔴 进 Idle 唯一路径 = Initialize 触发，且必须两段式：先 StopAsync 杀线程（→ Stopped），
    /// 再 Fire(Initialize)（→ Idle）——禁止活跃态直达 Idle，否则"状态说线程不存在但线程还活着"。
    /// </summary>
    public enum ProcessState
    {
        /// <summary>线程不存在，跑过之后（人工停止 / 完成自转；可 Start 重启或 Initialize 回待命）</summary>
        Stopped,

        /// <summary>线程存活，正常执行步序</summary>
        Running,

        /// <summary>线程存活，阻塞在暂停门控，等待 Resume</summary>
        Paused,

        /// <summary>线程存活，阻塞等待人工 Reset（软恢复：步序不归零，从异常步重试）</summary>
        Exception,

        /// <summary>线程不存在，待命（构造后 / 初始化完成后；可 Start 启动或 Initialize 重入清场）</summary>
        Idle
    }

    /// <summary>
    /// 流程状态机触发器（控制方法的参数化流转语义）。
    /// 触发器与状态转换白名单由 TaskProcess/Services/ProcessBase 的 Stateless 配置定义，
    /// 本枚举只承载契约，规则不在此处（TaskProcess 零依赖基座）。
    /// </summary>
    public enum ProcessTrigger
    {
        /// <summary>启动（Idle/Stopped → Running，唯一启动入口，建线程）</summary>
        Start,

        /// <summary>暂停（Running → Paused，步间暂停语义）</summary>
        Pause,

        /// <summary>继续（Paused → Running，放行门控）</summary>
        Resume,

        /// <summary>复位（Exception → Paused，软恢复：步序不归零，从异常步重试）</summary>
        Reset,

        /// <summary>停止（任意态 → Stopped，杀线程）</summary>
        Stop,

        /// <summary>完成（活跃态 → Stopped，looper 退出收尾自转）</summary>
        Finish,

        /// <summary>触发异常（Running/Paused → Exception）</summary>
        RaiseException,

        /// <summary>初始化（Stopped → Idle，两段式第二段；Idle 重入幂等清场。🔴 必须先 StopAsync）</summary>
        Initialize
    }

    /// <summary>
    /// 流程类别
    /// </summary>
    public enum ProcessCategory
    {
        /// <summary>生产流程</summary>
        Production,

        /// <summary>安全流程</summary>
        Safety,

        /// <summary>辅助流程（如心跳）</summary>
        Auxiliary
    }

    /// <summary>
    /// 系统级聚合状态（UI 层使用）
    /// </summary>
    public enum SystemProcessState
    {
        /// <summary>全部停止/待命或禁用</summary>
        Idle,

        /// <summary>有流程在正常运行</summary>
        Running,

        /// <summary>有流程暂停（无异常）</summary>
        Paused,

        /// <summary>有流程异常（最高优先级显示）</summary>
        Exception
    }

    /// <summary>
    /// 流程行为配置（极简版）
    /// 每个流程实例可独立配置，也可使用全局默认值
    /// </summary>
    public class ProcessBehaviorConfig
    {
        /// <summary>异常时是否暂停其他流程（默认true=安全模式）</summary>
        public bool PauseOthersOnException { get; set; }

        /// <summary>是否允许人工暂停（辅助流程如心跳可设为false）</summary>
        public bool AllowManualPause { get; set; }

        /// <summary>是否启用（禁用后不参与系统状态聚合）</summary>
        public bool IsEnabled { get; set; }

        /// <summary>构造函数设置默认值</summary>
        public ProcessBehaviorConfig()
        {
            PauseOthersOnException = true;
            AllowManualPause = true;
            IsEnabled = true;
        }
    }

    /// <summary>
    /// 流程状态快照，用于监控、事件广播和 UI 展示
    /// </summary>
    public class ProcessStatus
    {
        /// <summary>流程名称（唯一标识）</summary>
        public string ProcessName { get; set; }

        /// <summary>流程类别</summary>
        public ProcessCategory Category { get; set; }

        /// <summary>流程状态（单层五态，线程生死即状态）</summary>
        public ProcessState State { get; set; }

        /// <summary>
        /// 当前合法触发器集合（状态机运行时求值，锁内取值）。
        /// UI 按钮判定数据源：按钮可用性由"状态机此刻允许什么触发"派生，与运行时同源，
        /// 未来规则扩展（如 PermitIf 条件转换）自动反映。
        /// </summary>
        public IReadOnlyCollection<ProcessTrigger> PermittedTriggers { get; set; }

        /// <summary>当前步序</summary>
        public int Step { get; set; }

        /// <summary>当前异常故障码（null 表示无异常；快照来源 = ProcessFault.Code）</summary>
        public string ExceptionCode { get; set; }

        /// <summary>当前异常描述（null 表示无异常；快照来源 = ProcessFault.Message）</summary>
        public string ExceptionMessage { get; set; }

        /// <summary>快照时间</summary>
        public DateTime Timestamp { get; set; }
    }
}
