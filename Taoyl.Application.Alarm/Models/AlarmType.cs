using System.ComponentModel;

namespace Taoyl.Application.Alarm.Models
{
    /// <summary>
    /// 报警类型三态：提示 = 自关弹窗（错过看查询页，不暂停业务）；警告 = 常驻待确认（预警防错过，不暂停业务）；
    /// 异常 = 常驻待确认 + 流程联动暂停。严重度色阶：提示=黄 / 警告=橙 / 异常=红。
    /// [Description] 文案即存储契约（列宽 20）：2026-10-12 命名定稿（原「记录/提示」框架期作废），
    /// 投产起不得改，只可新增成员。
    /// </summary>
    public enum AlarmType
    {
        /// <summary>提示：自关弹窗（空闲自动收窗，清空不 Ack；手动确认可 Ack 留痕）</summary>
        [Description("提示")] Notice = 1,

        /// <summary>警告：常驻待确认弹窗（预警防错过，确认 Ack 留痕）</summary>
        [Description("警告")] Warning = 2,

        /// <summary>异常：常驻待确认弹窗 + 流程联动暂停（确认 Ack 留痕）</summary>
        [Description("异常")] Exception = 3,
    }
}
