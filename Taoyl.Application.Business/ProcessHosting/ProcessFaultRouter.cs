using System;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.TaskProcess.Interfaces;
using Taoyl.Application.TaskProcess.Models;

namespace Taoyl.Application.Business.ProcessHosting
{
    /// <summary>
    /// 流程故障 → 报警落库的适配器（外层实现，基座不认识报警库）。
    ///
    /// 接线：**构造时自动订阅** <c>IProcessCoordinator.ProcessFaulted</c>——本类的存在意义就是接住故障，
    /// 订阅写在构造里，组合根只需 Resolve 本类（与 ProcessRegistry「构造即注册」、
    /// AlarmPopupPresenter「构造即订阅」同构）。之后流程故障（E001 / E999 / E000 / 业务异常）
    /// 与协调器自身故障都经这里变成报警记录，走 AlarmService.Raise → 落库 → 总线广播 → 弹窗/查询页。
    ///
    /// ⚠️ 删掉本类的注册或 Resolve = 报警静默丢失（无编译/运行错误）；
    /// ProcessCoordinator.Register 有接线自检，启动日志会记一条 Error。
    ///
    /// 为什么不是「基座直接调报警」：基座零 ProjectReference，引 Alarm 即破边界；
    /// 也不能让子类各自 Raise——那会漏掉框架级故障（K2）。
    /// </summary>
    public sealed class ProcessFaultRouter
    {
        private readonly IAlarmService alarmService;

        public ProcessFaultRouter(IProcessCoordinator coordinator, IAlarmService alarmService)
        {
            if (coordinator == null) throw new ArgumentNullException(nameof(coordinator));
            this.alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));

            // 构造即接线：故障出口只有一个消费方，订阅关系不需要外部记得
            coordinator.ProcessFaulted += OnFault;
        }

        /// <summary>
        /// 事件处理器（订阅 IProcessCoordinator.ProcessFaulted）。
        /// 跑在触发故障的线程上（流程 looper 或控制调用方）；Raise 内部入队落库，微秒级返回。
        /// </summary>
        public void OnFault(object sender, ProcessFault fault)
        {
            if (fault == null) return;

            alarmService.Raise(new AlarmRecordDto
            {
                Source = fault.Source,
                Type = AlarmEnumText.Of(AlarmType.Exception),
                Code = fault.Code,
                Message = fault.Message
            });
        }
    }
}
