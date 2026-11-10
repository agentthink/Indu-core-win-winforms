using SqlSugar;

namespace Taoyl.Application.Audit.Models.Entities
{
    /// <summary>
    /// 审计字段变更明细表（一条操作 0..N 行）。追查姿势：按 OperationId 主从钻取 / 导出展开长表。
    /// OldValue / NewValue 用 TEXT 不截断。
    /// </summary>
    [SugarTable("sys_audit_field_changes", TableDescription = "审计字段变更明细表")]
    public class AuditFieldChangeEntity
    {
        // SQLite 的 AUTOINCREMENT 只能用于类型名精确为 INTEGER 的主键；long 默认映射 BIGINT 会建表失败
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER", ColumnDescription = "主键")]
        public long Id { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "指向头表 Id")]
        public long OperationId { get; set; }

        [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "点分路径")]
        public string FieldPath { get; set; }

        [SugarColumn(Length = 20, IsNullable = false, ColumnDescription = "变更类型（AuditChangeType 常量，中文直存）")]
        public string ChangeType { get; set; }

        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "旧值（不截断；空 = 旧值不可知，非空值）")]
        public string OldValue { get; set; }

        [SugarColumn(ColumnDataType = "TEXT", IsNullable = true, ColumnDescription = "新值（不截断）")]
        public string NewValue { get; set; }
    }
}
