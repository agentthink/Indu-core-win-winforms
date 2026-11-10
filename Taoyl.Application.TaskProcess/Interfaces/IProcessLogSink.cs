using System;

namespace Taoyl.Application.TaskProcess.Interfaces
{
    /// <summary>
    /// 流程基座的日志出口。
    ///
    /// 分工（与故障出口不重叠）：
    /// - 流程故障（E001/E999/E000/业务异常）→ <see cref="IProcessCoordinator.ProcessFaulted"/>（报警落库，需人处理）
    /// - 协调器操作流水与操作失败 → 本端口（追溯用，不需要人处理）
    /// - ProcessBase 自身不记任何日志（状态机没有可追溯的业务信息）
    ///
    /// 基座零 ProjectReference：只定义端口，具体日志库由外层实现（如 LogHelp）。
    /// </summary>
    public interface IProcessLogSink
    {
        void Info(string message);
        void Warn(string message);
        void Error(Exception exception, string message);
    }
}
