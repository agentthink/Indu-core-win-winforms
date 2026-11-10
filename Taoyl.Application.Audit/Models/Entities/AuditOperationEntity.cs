using System;
using SqlSugar;

namespace Taoyl.Application.Audit.Models.Entities
{
    /// <summary>
    /// 审计操作头表（一次操作一行）。查询页抽查姿势：按 Time / Category / UserName / Result 筛选。
    /// 无 Reason 列（将来 21 CFR 合规需要时再加列）。
    /// </summary>
    [SugarTable("sys_audit_operations", TableDescription = "审计操作头表")]
    public class AuditOperationEntity
    {
        // SQLite 的 AUTOINCREMENT 只能用于类型名精确为 INTEGER 的主键；long 默认映射 BIGINT 会建表失败
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDataType = "INTEGER", ColumnDescription = "主键")]
        public long Id { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "操作时间")]
        public DateTime Time { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "操作者（未登录=访客）")]
        public string UserName { get; set; }

        [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "角色名快照，逗号连接")]
        public string RoleNames { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "生效等级快照")]
        public int RoleLevel { get; set; }

        [SugarColumn(Length = 20, IsNullable = false, ColumnDescription = "页面（AuditCategory 常量，中文直存）")]
        public string Category { get; set; }

        [SugarColumn(Length = 20, IsNullable = false, ColumnDescription = "命令级动作（AuditAction 常量，中文直存）")]
        public string Action { get; set; }

        [SugarColumn(Length = 100, IsNullable = true, ColumnDescription = "操作对象业务主键")]
        public string Target { get; set; }

        [SugarColumn(Length = 20, IsNullable = false, ColumnDescription = "结果两态（AuditResult 常量，中文直存）")]
        public string Result { get; set; }
    }
}
