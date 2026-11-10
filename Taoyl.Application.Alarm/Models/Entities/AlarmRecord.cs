using System;
using SqlSugar;
using Taoyl.Application.Alarm.Models.Dtos;

namespace Taoyl.Application.Alarm.Models.Entities
{
    /// <summary>
    /// 报警记录（落库实体）。只由 <see cref="Repository.AlarmRepository"/> 使用，出仓储层一律转 <see cref="AlarmRecordDto"/>。
    /// 两阶段填充：Raise 时刻填业务字段（RaiseTime 服务补）；Ack 时刻补处理人员/时间。
    /// 列注释归 <see cref="SugarColumnAttribute.ColumnDescription"/>（UI 列头在 DTO 上）。
    /// </summary>
    [SugarTable("sys_alarm_records", TableDescription = "报警记录")]
    public class AlarmRecord
    {
        // SQLite 的 AUTOINCREMENT 只能用于类型名精确为 INTEGER 的主键；long 默认映射 BIGINT 会建表失败
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER", ColumnDescription = "主键")]
        public long Id { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "产生时间（Raise 时刻）")]
        public DateTime RaiseTime { get; set; }

        [SugarColumn(Length = 20, IsNullable = false, ColumnDescription = "类型（记录/提示/异常，中文文案直存）")]
        public string Type { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "报警代码")]
        public string Code { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "来源（工位/设备/流程等出事主体）")]
        public string Source { get; set; }

        [SugarColumn(Length = 200, IsNullable = false, ColumnDescription = "报警描述")]
        public string Message { get; set; }

        [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "行动指南（给现场人的处置指引，可空）")]
        public string ActionGuide { get; set; }

        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "处理人员（Ack 时刻补）")]
        public string HandleUser { get; set; }

        [SugarColumn(IsNullable = true, ColumnDescription = "处理时间（null=未处理）")]
        public DateTime? HandleTime { get; set; }
    }
}
