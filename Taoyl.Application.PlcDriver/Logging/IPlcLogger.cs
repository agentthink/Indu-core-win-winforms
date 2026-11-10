using System;

namespace Taoyl.Application.PlcDriver.Logging
{
    /// <summary>
    /// PLC实例级日志接口
    /// 职责：抽象驱动日志能力，公共 API 不暴露 HSL 日志类型
    /// 实例级：每个 PLC 实例（PlcRuntime）与配置管理器（ConfigManager）各自独立日志实例，非全局单例
    /// 继承 IDisposable：释放底层 HSL 日志资源（PlcLogger 已实现）
    /// </summary>
    public interface IPlcLogger : IDisposable
    {
        /// <summary>
        /// 记录调试日志
        /// </summary>
        void WriteDebug(string message);

        /// <summary>
        /// 记录信息日志
        /// </summary>
        void WriteInfo(string message);

        /// <summary>
        /// 记录警告日志
        /// </summary>
        void WriteWarn(string message);

        /// <summary>
        /// 记录错误日志
        /// </summary>
        void WriteError(string message);

        /// <summary>
        /// 记录异常日志
        /// </summary>
        /// <param name="message">异常描述</param>
        /// <param name="ex">异常对象</param>
        void WriteException(string message, Exception ex);
    }
}
