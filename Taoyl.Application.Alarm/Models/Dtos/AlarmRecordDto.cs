using System;
using System.ComponentModel;
using Taoyl.Application.Alarm.Models.Entities;

namespace Taoyl.Application.Alarm.Models.Dtos
{
    /// <summary>
    /// 报警记录 DTO：服务接口 / 总线广播 / 流程域快照 / UI 绑定共用（实体不出仓储层）。
    /// 与实体 <see cref="AlarmRecord"/> 全字段同构（9/9），映射登记为空配置。
    /// [Description] = UI 中文列头（勿与实体的落库列注释混同——后者归 SugarColumn.ColumnDescription）。
    /// </summary>
    public class AlarmRecordDto
    {
        [Description("主键")]
        public long Id { get; set; }

        [Description("产生时间")]
        public DateTime RaiseTime { get; set; }

        [Description("类型")]
        public string Type { get; set; }

        [Description("报警代码")]
        public string Code { get; set; }

        [Description("来源")]
        public string Source { get; set; }

        [Description("报警描述")]
        public string Message { get; set; }

        [Description("行动指南")]
        public string ActionGuide { get; set; }

        [Description("处理人员")]
        public string HandleUser { get; set; }

        [Description("处理时间")]
        public DateTime? HandleTime { get; set; }
    }
}
