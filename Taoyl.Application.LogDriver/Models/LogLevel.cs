namespace Taoyl.Application.LogDriver.Models
{
    /// <summary>
    /// 日志级别枚举（自建，公共 API 不暴露 NLog 类型；内部与 NLog.LogLevel 映射）。
    /// 级别是记录的数据字段（CSV 的 Level 列），通道即方法名 ⇒
    /// 级别不参与对外契约，仅存在于 LogEventArgs.Level 与事件链内部。
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3
    }
}
