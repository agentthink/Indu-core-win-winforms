using System;
using System.Threading;
using Taoyl.Application.LogDriver;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    /// <summary>
    /// 日志与超时测试流程：步序 10 灌四通道日志样本，步序 20 验基座超时机制，循环。
    ///
    /// 🔴 超时并入本流程而不是单独成流：`SetTimeout` / `CheckTimeout` 是基座能力，
    /// 它不产报警、最终以 Error 日志呈现，单独成流没有归属，塞进别的流程又名不副实。
    /// 🔴 与旧 TestProcess 的差别：旧流程随机触发（一个类同时测日志 + 报警 + 超时），
    /// 拆分后每步只测一件事，故改为**每次必发**——测试要的是可复现，随机只适合压测。
    /// 🔴 默认禁用（IsEnabled = false）：不参与 StartAll 与系统状态聚合，现场「一键启动」不会带上它。
    /// 测的时候在流程监控页选中本行 → 启用 → 启动。该开关只改运行时值、不落盘，重启即恢复禁用。
    /// </summary>
    internal class LogTestProcess : ProcessBase
    {
        private readonly Random random = new Random();

        public LogTestProcess()
        {
            Category = ProcessCategory.Production;

            // 只在本类构造里关：不动基类默认值，其余流程照旧启用
            BehaviorConfig.IsEnabled = false;

            // 日志测试进异常态不该牵连其他流程
            BehaviorConfig.PauseOthersOnException = false;
        }

        public override void InitProcess()
        {
            ProcessName = "测试-日志与超时";
        }

        /// <summary>无外部订阅、无自有资源，无需释放。</summary>
        protected override void OnDispose() { }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            switch (step)
            {
                case 10:
                    // 四通道各一条：核对日志页四个页签、级别过滤与 CSV 落盘列数
                    LogHelp.Info(string.Format("[{0}] 一般信息样本", ProcessName));
                    LogHelp.Warn(string.Format("[{0}] 警告样本：腔体温度接近上限", ProcessName));
                    LogHelp.Error(new InvalidOperationException("条码校验失败（测试样本）"),
                        string.Format("[{0}] 异常样本：条码校验失败", ProcessName));
                    LogHelp.MES(string.Format("[{0}] MES 通道样本：步序[10] 完成，产量+1", ProcessName));
                    return ProcessStepResult.Next();

                case 20:
                    // 基座超时机制：SetTimeout 起表 → CheckTimeout 判超时。
                    // 🔴 超时后必须先 SetTimeout 再判/退出，否则下一拍的基准还停在上一拍的时间戳，
                    // 第一次超时之后每一拍都会立刻判超时（旧 TestProcess 同样是这个写法）。
                    SetTimeout();
                    Thread.Sleep(random.Next(0, 1100));

                    if (CheckTimeout(1200))
                    {
                        SetTimeout();
                        LogHelp.Error(new TimeoutException(string.Format("{0} 采集等待超时", ProcessName)),
                            string.Format("[{0}] 步序[20] 模拟采集数据超时", ProcessName));
                        return ProcessStepResult.Exception("E201", "模拟采集数据超时");
                    }

                    // 未超时才有这一步：1/3 概率灌一条 Warn，制造「警告不停线」的对照样本
                    if (random.Next(0, 3) == 0)
                    {
                        LogHelp.Warn(string.Format("[{0}] 步序[20] 采集耗时偏高但未超时", ProcessName));
                    }
                    return ProcessStepResult.Next();

                default:
                    return ProcessStepResult.Jump(10);
            }
        }
    }
}
