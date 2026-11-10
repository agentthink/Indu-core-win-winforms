using System.ComponentModel;

namespace Taoyl.Application.Share.Logging
{
    /// <summary>
    /// 日志文件切分粒度。
    /// 驱动 fileName 的日期 token：Day 生成 yyyyMMdd，Hour 生成 yyyyMMddHH
    /// （均不含冒号，无 layout 转义风险；NLog 按渲染结果变化开新文件）。
    /// [Description] 供配置界面下拉显示中文（ConfigFormRenderer.MakeCombo 走 GetDescription）。
    /// </summary>
    public enum LogSplitPeriod
    {
        /// <summary>按天切分（默认：单目录文件数 ≈ KeepDays，180 天 = 180 文件，可接受）。</summary>
        [Description("按天")]
        Day = 0,

        /// <summary>
        /// 按小时切分（临时排障用：单目录文件数 ≈ 24 × KeepDays，不限制保留期——
        /// 保留期与单目录文件数解耦；运维注意人工翻阅体验）。
        /// </summary>
        [Description("按小时")]
        Hour = 1,
    }
}
