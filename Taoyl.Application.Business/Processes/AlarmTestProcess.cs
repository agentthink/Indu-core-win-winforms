using System;
using System.Threading;
using Taoyl.Application.Business.ProcessHosting;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    /// <summary>
    /// 报警测试流程：步序 10 发三态报警，步序 20 走 Exception 单出口（进异常态 + 联动暂停），循环。
    ///
    /// 🔴 PauseOthersOnException 保持默认 true：本流程的职责就是验证「异常联动暂停其他 Running 流程」，
    /// 关掉等于不测。**启动前先停掉生产流程**，否则它会把人一起按停。
    /// 🔴 每次必发（旧 TestProcess 是随机触发）：拆分后每步只测一件事，可复现优先于随机。
    /// 🔴 默认禁用（IsEnabled = false）：测的时候在流程监控页选中 → 启用 → 启动；重启恢复禁用。
    /// </summary>
    internal class AlarmTestProcess : ProcessBase
    {
        private readonly IProcessContext context;

        public AlarmTestProcess(IProcessContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Category = ProcessCategory.Production;

            // 只在本类构造里关：不动基类默认值，其余流程照旧启用
            BehaviorConfig.IsEnabled = false;
        }

        public override void InitProcess()
        {
            ProcessName = "测试-报警";
        }

        /// <summary>context 及其服务是容器单例，不归流程释放；本类无订阅。</summary>
        protected override void OnDispose() { }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            switch (step)
            {
                case 10:
                    // 三态各一条，用来核对手写文案是否与 AlarmEnumText 一致：
                    // Notice 自关 / Warning 常驻待确认 / Exception 模态逐条
                    Raise(AlarmType.Notice, "N101", "模拟提示：批次参数留痕", null);
                    Raise(AlarmType.Warning, "W101", "模拟警告：腔体温度接近上限", "关注散热风扇转速，无需停机");
                    Raise(AlarmType.Exception, "E301", "模拟业务异常：真空度超下限", "检查腔体密封圈");
                    return ProcessStepResult.Next();

                case 20:
                    // Exception 单出口：先落一条报警，再让状态机进异常态并联动暂停其他 Running 流程。
                    // 两条一起发是为了确认「报警落库」与「状态机联动」是两条独立路径、都通。
                    Raise(AlarmType.Exception, "E302", "模拟流程异常：加热器电流过流", "检查加热器与固态继电器");
                    return ProcessStepResult.Exception("E302", "模拟流程异常：加热器电流过流");

                default:
                    return ProcessStepResult.Jump(10);
            }
        }

        /// <summary>Type 经 AlarmEnumText 取文案（手写中文=脱离契约），RaiseTime 由服务统一补。</summary>
        private void Raise(AlarmType type, string code, string message, string actionGuide)
        {
            context.AlarmService.Raise(new AlarmRecordDto
            {
                Type = AlarmEnumText.Of(type),
                Code = code,
                Source = ProcessName,
                Message = message,
                ActionGuide = actionGuide
            });
        }
    }
}
