using System;

namespace Taoyl.Application.TaskProcess.Models
{
    /// <summary>
    /// 流程故障（基座对外唯一的故障载荷）。
    ///
    /// 来源两处：
    /// 1. ProcessBase.TriggerException —— 状态机进入 Exception 时构造（E001 健康检查 / E999 循环异常 / E000 基座内部错误 / 业务自定义码）；
    /// 2. ProcessCoordinator —— 协调器自身操作失败（E901 暂停流程失败 / E902 批量操作失败）。
    ///
    /// 继承 EventArgs：直接作为事件载荷使用，不再单独造 *EventArgs 类。
    /// 载荷只含「来源 + 码 + 描述 + 时刻」，不含异常实例与流程实例——外层拿不到基座内部对象。
    /// </summary>
    public sealed class ProcessFault : EventArgs
    {
        /// <summary>故障来源（流程名；协调器自身故障时为协调器标识）</summary>
        public string Source { get; set; }

        /// <summary>故障码</summary>
        public string Code { get; set; }

        /// <summary>故障描述</summary>
        public string Message { get; set; }

        /// <summary>发生时刻（本地时间，与状态快照同一时间源 DateTime.Now）</summary>
        public DateTime OccurredAt { get; set; }
    }
}
