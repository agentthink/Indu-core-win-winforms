using Taoyl.Application.Alarm.Models;

namespace Taoyl.Application.Alarm.Models.Dtos
{
    /// <summary>
    /// 报警筛选条件（时间范围 / 类型）。全部字段可为空 = 不加该维条件。
    /// </summary>
    public class AlarmQueryFilter
    {
        /// <summary>起始时间（含）；null = 不限</summary>
        public System.DateTime? TimeFrom { get; set; }

        /// <summary>结束时间（分钟粒度，含端点，仓储按 &lt;= 比较）；null = 不限</summary>
        public System.DateTime? TimeTo { get; set; }

        /// <summary>类型（AlarmType 成员）；null = 全部</summary>
        public AlarmType? Type { get; set; }
    }
}
