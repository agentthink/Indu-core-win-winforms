using System;
using Taoyl.Application.LogDriver.Models;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver
{
    /// <summary>
    /// 日志事件参数（4 个快照字段；构造后业务侧可能继续改写活对象，读路径禁读非快照成员——
    /// 见 LogForm.FormatLine 硬约束）。
    /// </summary>
    public class LogEventArgs : EventArgs
    {
        /// <summary>
        /// 日志类别
        /// </summary>
        public LogCategory Category { get; set; }

        /// <summary>
        /// 消息（NLog 格式化后的最终文本，与文件记录一致）
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        public DateTime Timestamp { get; set; }

        /// <summary>
        /// 日志级别（自建枚举，公共 API 不暴露 NLog 类型）
        /// </summary>
        public LogLevel Level { get; set; }
    }
}
