using Taoyl.Application.Audit.Models.Dtos;
using CsvHelper.Configuration.Attributes;

namespace Taoyl.UI.Pages.Models
{
    /// <summary>
    /// 审计展开长表的导出行模型（只为 CSV 中文表头而存在）。
    ///
    /// 为什么不直接给 <see cref="AuditFlatRowDto"/> 加 CsvHelper 的 [Name] 特性：
    /// 那会把 CsvHelper 拖进审计契约（Taoyl.Application.AuthService）的依赖面——
    /// 展示层的导出格式不该污染契约层。故在 UI 层做一次浅映射。
    ///
    /// 值本身已是中文（审计常量直接用中文承载），无需任何"枚举→文案"映射。
    /// </summary>
    public class AuditExportRow
    {
        [Name("时间")]
        public string Time { get; set; }

        [Name("操作者")]
        public string UserName { get; set; }

        [Name("角色")]
        public string RoleNames { get; set; }

        [Name("等级")]
        public int RoleLevel { get; set; }

        [Name("页面")]
        public string Category { get; set; }

        [Name("动作")]
        public string Action { get; set; }

        [Name("操作对象")]
        public string Target { get; set; }

        [Name("结果")]
        public string Result { get; set; }

        [Name("字段路径")]
        public string FieldPath { get; set; }

        /// <summary>空 = 该行是动作型操作（无明细）</summary>
        [Name("变更类型")]
        public string ChangeType { get; set; }

        [Name("旧值")]
        public string OldValue { get; set; }

        [Name("新值")]
        public string NewValue { get; set; }

        /// <summary>从审计契约 DTO 浅映射一行。</summary>
        public static AuditExportRow From(AuditFlatRowDto r)
        {
            return new AuditExportRow
            {
                Time = r.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                UserName = r.UserName,
                RoleNames = r.RoleNames,
                RoleLevel = r.RoleLevel,
                Category = r.Category,
                Action = r.Action,
                Target = r.Target,
                Result = r.Result,
                FieldPath = r.FieldPath,
                ChangeType = r.ChangeType,
                OldValue = r.OldValue,
                NewValue = r.NewValue
            };
        }
    }
}
