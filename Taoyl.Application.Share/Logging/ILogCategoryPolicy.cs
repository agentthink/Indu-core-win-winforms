namespace Taoyl.Application.Share.Logging
{
    /// <summary>
    /// 单个日志通道的落盘策略契约（每类别独立：目录 / 前缀 / 切分粒度 / 保留期）。
    /// Configuration 侧的 LogCategoryPolicyConfig 实现它，LogDriver 侧的
    /// LogTargetConfigurator 消费它——两侧不直接互相引用（Share 中继，IAutoLogoutSettings 先例）。
    /// </summary>
    public interface ILogCategoryPolicy
    {
        /// <summary>本策略对应的日志通道。</summary>
        LogCategory Category { get; }

        /// <summary>文件切分粒度（默认 Day）。</summary>
        LogSplitPeriod SplitPeriod { get; }

        /// <summary>保留天数：-1 = 不清理；≥ 1 = 超过即被 LogArchiveCleaner 按文件名日期清理。</summary>
        int KeepDays { get; }

        /// <summary>
        /// 文件名前缀（仅允许字母/数字/下划线，防清理通配符退化为扫全目录）；
        /// 为空时回退为通道名（Category.ToString()）。
        /// </summary>
        string FileNamePrefix { get; }
    }
}
