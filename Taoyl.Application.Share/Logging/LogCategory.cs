namespace Taoyl.Application.Share.Logging
{
    /// <summary>
    /// 日志通道枚举（5 成员）。
    /// 级别与通道分离——通道决定「落哪个目录、哪个文件」，级别是记录的数据字段（CSV 的 Level 列）。
    /// 本枚举为跨层公共词汇（Share 零依赖基座），由 Configuration 的 LogConfig 与
    /// LogDriver 的 LogTargetConfigurator 共同使用。
    /// </summary>
    public enum LogCategory
    {
        /// <summary>调试通道（落盘但 UI 暂不显示）。</summary>
        Debug = 0,

        /// <summary>信息通道。</summary>
        Info = 1,

        /// <summary>警告通道。</summary>
        Warn = 2,

        /// <summary>错误通道。</summary>
        Error = 3,

        /// <summary>MES 通道（记录级别恒为 Info，不提供级别重载）。</summary>
        MES = 4,
    }
}
