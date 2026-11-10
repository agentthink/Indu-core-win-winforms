using System;
using Taoyl.Application.LogDriver;
using Taoyl.Application.TaskProcess.Interfaces;

namespace Taoyl.Application.Business.ProcessHosting
{
    /// <summary>
    /// 流程基座日志端口的实现（外层）。
    ///
    /// 基座零 ProjectReference，不认识 LogDriver，只认 <see cref="IProcessLogSink"/>；
    /// 由组合根把本类注册进容器注入 ProcessCoordinator。
    /// 这里只承接「协调器运维流水」——流程故障不写日志（报警已落库，双写冗余）。
    /// </summary>
    public sealed class LogHelpProcessLogSink : IProcessLogSink
    {
        public void Info(string message)
        {
            LogHelp.Info(message);
        }

        public void Warn(string message)
        {
            LogHelp.Warn(message);
        }

        public void Error(Exception exception, string message)
        {
            LogHelp.Error(exception, message);
        }
    }
}
