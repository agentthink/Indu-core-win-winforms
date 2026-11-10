using System;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.LogDriver.Models;
using Taoyl.Application.LogDriver.Services;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver
{
    /// <summary>
    /// 日志帮助类（静态兼容门面，转发到 <see cref="NLogLogService"/> 共享单例）
    /// 供业务层静态直调使用（免构造注入）；UI 层建议通过 Autofac 注入 <see cref="ILogService"/>。
    /// 所有转发均指向同一实例，保证事件链路不分裂。
    /// </summary>
    public static class LogHelp
    {
        // 共享单例（与 Autofac 注册的 ILogService、LogForm 订阅为同一实例）
        private static readonly ILogService _service = NLogLogService.Instance;

        #region 公共属性与事件

        /// <summary>
        /// 日志写入事件（单一事件处理所有类别）
        /// </summary>
        public static event EventHandler<LogEventArgs> LogWritten
        {
            add { _service.LogWritten += value; }
            remove { _service.LogWritten -= value; }
        }

        /// <summary>
        /// 是否启用事件触发（默认为 true）
        /// </summary>
        public static bool EnableEvents
        {
            get { return _service.EnableEvents; }
            set { _service.EnableEvents = value; }
        }

        /// <summary>
        /// 事件触发最小级别（默认 Info 及以上）
        /// </summary>
        public static LogLevel MinimumEventLevel
        {
            get { return _service.MinimumEventLevel; }
            set { _service.MinimumEventLevel = value; }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 初始化日志系统（NLog.config 载入 + ILogSettings 落盘策略 overlay）。
        /// 🔴 LogConfig 任何改动一律要求进程重启生效（不做热生效）。
        /// </summary>
        public static void Initialize(ILogSettings logSettings)
        {
            _service.Initialize(logSettings);
        }

        #endregion

        #region 公共日志方法（原有级别）

        /// <summary>记录 Debug 通道日志（落盘，UI 暂不显示）</summary>
        public static void Debug(string message, params object[] args)
        {
            _service.Debug(message, args);
        }

        /// <summary>记录 Info 通道日志</summary>
        public static void Info(string message, params object[] args)
        {
            _service.Info(message, args);
        }

        /// <summary>记录 Warn 级别日志</summary>
        public static void Warn(string message, params object[] args)
        {
            _service.Warn(message, args);
        }

        /// <summary>记录 Error 通道日志（message 为空时使用 ex.Message）</summary>
        public static void Error(Exception ex, string message = null, params object[] args)
        {
            _service.Error(ex, message, args);
        }

        #endregion

        #region 业务通道日志（MES）

        /// <summary>记录 MES 通道日志（无实体，记录级别恒 Info）</summary>
        public static void MES(string message, params object[] args)
        {
            _service.MES(message, args);
        }

        

        #endregion

        #region 工具方法

        /// <summary>
        /// 获取所有日志类别
        /// </summary>
        public static LogCategory[] GetAllCategories()
        {
            return NLogLogService.Instance.GetAllCategories();
        }

        /// <summary>
        /// 清空事件订阅（谨慎使用）
        /// </summary>
        public static void ClearEventSubscriptions()
        {
            NLogLogService.Instance.ClearEventSubscriptions();
        }

        #endregion
    }
}
