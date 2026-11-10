using System;
using Taoyl.Application.LogDriver.Models;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver.Interfaces
{
    /// <summary>
    /// 日志服务契约（对外唯一日志入口）。
    /// 通道即方法名（Debug/Info/Warn/Error/MES），不暴露级别参数——
    /// 通道决定落盘位置，级别是记录的数据字段（CSV 的 Level 列），由服务侧内部填充。
    /// 实体链已摘除：所有信息由调用方转成字符串填入 message。
    /// 公共 API 不暴露任何 NLog 类型；实现为 <see cref="Services.NLogLogService"/> 共享单例。
    /// </summary>
    public interface ILogService
    {
        #region 通道日志（通道即方法名）

        /// <summary>记录 Debug 通道日志（落盘，UI 暂不显示）</summary>
        void Debug(string message, params object[] args);

        /// <summary>记录 Info 通道日志</summary>
        void Info(string message, params object[] args);

        /// <summary>记录 Warn 通道日志</summary>
        void Warn(string message, params object[] args);

        /// <summary>记录 Error 通道日志（message 为空时使用 ex.Message）</summary>
        void Error(Exception ex, string message = null, params object[] args);

        /// <summary>记录 MES 通道日志（记录级别恒 Info，无级别重载）</summary>
        void MES(string message, params object[] args);

        #endregion

        #region 事件与配置

        /// <summary>
        /// 日志写入事件（单一事件处理所有通道，供 UI 层订阅显示）
        /// </summary>
        event EventHandler<LogEventArgs> LogWritten;

        /// <summary>
        /// 是否启用事件触发（默认为 true）
        /// </summary>
        bool EnableEvents { get; set; }

        /// <summary>
        /// 事件触发最小级别（默认 Info 及以上；只影响 UI 事件链，落盘不受影响）
        /// </summary>
        LogLevel MinimumEventLevel { get; set; }

        /// <summary>
        /// 初始化日志系统（NLog.config 载入 + <see cref="ILogSettings"/> overlay）。
        /// 日志策略由配置中心分发（LogConfig 经 Share 中继传入）；
        /// 🔴 LogConfig 任何改动一律要求进程重启生效（不做热生效）。
        /// </summary>
        void Initialize(ILogSettings logSettings);

        /// <summary>
        /// 关闭日志系统：flush 异步队列残留（K2 连锁——退出不 flush 会丢行）。
        /// 必须在进程退出前（主窗体消息循环结束后）显式调用一次。
        /// </summary>
        void Shutdown();

        #endregion
    }
}
