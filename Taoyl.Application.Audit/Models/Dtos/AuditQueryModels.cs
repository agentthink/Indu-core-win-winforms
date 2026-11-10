using System;
using System.Collections.Generic;
using System.ComponentModel;
using Taoyl.Application.Audit.Constants;

namespace Taoyl.Application.Audit.Models.Dtos
{
    /// <summary>
    /// 审计五维筛选条件（时间范围 / 页面 / 操作者 / 结果 / 动作；对齐头表索引）。
    /// 全部字段可为空 = 不加该维条件。
    ///
    /// 页面 / 动作 / 结果收**可空枚举**（2026-09-18 改型）：UI 下拉选中项即枚举成员，无需字符串比对；
    /// 转换到 SQL 参数在 AuditQueryService.BuildHeaderQuery 里一次性完成（先用局部变量算出中文文案，
    /// 再进表达式树——静态方法禁入表达式树是本项目既有红线）。
    /// 读回来的 DTO 仍然收 string：展示 / 导出零映射，且天然容忍历史文案与将来新增值。
    ///
    /// 说明：UI 侧筛选已收为二维（时间 + 页面），但本契约**保留五维能力**——
    /// 服务端仍支持按操作者 / 结果 / 动作查询，裁剪它要动实现与 SQL 拼装，是另一件事。
    /// </summary>
    public class AuditQueryFilter
    {
        /// <summary>起始时间（含）；null = 不限</summary>
        public DateTime? TimeFrom { get; set; }

        /// <summary>结束时间（含当天全天）；null = 不限</summary>
        public DateTime? TimeTo { get; set; }

        /// <summary>页面（<see cref="AuditCategory"/> 成员）；null = 全部</summary>
        public AuditCategory? Category { get; set; }

        /// <summary>操作者用户名（精确匹配）；null/空 = 全部</summary>
        public string UserName { get; set; }

        /// <summary>结果两态（<see cref="AuditResult"/> 成员）；null = 全部</summary>
        public AuditResult? Result { get; set; }

        /// <summary>命令级动词（<see cref="AuditAction"/> 成员）；null = 全部</summary>
        public AuditAction? Action { get; set; }

        /// <summary>页码（1 起）</summary>
        public int PageIndex { get; set; } = 1;

        /// <summary>
        /// 每页行数。取值由实现方夹到 [1, 单页上限]——**别指望调用方自觉**：
        /// 本页是服务端分页，超大的每页值等于把长期只增的留档表整表拉进内存（见 AuditQueryService）。
        /// </summary>
        public int PageSize { get; set; } = 200;
    }

    /// <summary>分页查询结果。</summary>
    public class AuditPageResult
    {
        /// <summary>当前页头表行（时间倒序）</summary>
        public List<AuditOperationDto> Items { get; set; } = new List<AuditOperationDto>();

        /// <summary>满足条件的总行数（分页控件用）</summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 头表行（查询页展示用）。
    /// 页面 / 动作 / 结果直接承载落库的中文文案（由 AuditService 写入时的 [Description] 决定），
    /// UI 与 CSV 均**零映射**——读侧之所以坚持 string 而非解析回枚举，正是为了这一条：
    /// 历史文案一旦出了枚举范围（改型前的旧数据 / 将来删掉的成员），枚举解析会失败，而 string 原样展示。
    ///
    /// 列头文案走属性 [Description]：本 DTO 就是查询页直接绑定的模型，界面列头即它的展示元数据，
    /// 放在模型上才能让 View 反射取用而不硬编码中文——与 <c>UserInfo</c>（FormUserManager）、
    /// <c>Role</c>（FormRoleManager）的既有规则一致。<see cref="Entities.AuditOperationEntity"/> 上的同名单特性
    /// 是**落库列注释**（"操作时间" / "角色快照"），语义不同，勿互抄。
    /// </summary>
    public class AuditOperationDto
    {
        public long Id { get; set; }

        [Description("时间")]
        public DateTime Time { get; set; }

        [Description("操作者")]
        public string UserName { get; set; }

        [Description("角色")]
        public string RoleNames { get; set; }

        [Description("等级")]
        public int RoleLevel { get; set; }

        [Description("页面")]
        public string Category { get; set; }

        [Description("动作")]
        public string Action { get; set; }

        [Description("操作对象")]
        public string Target { get; set; }

        [Description("结果")]
        public string Result { get; set; }
    }

    /// <summary>
    /// 明细行（主从钻取的从表；与实体同构但独立声明，供 UI 直接绑定而不接触落库实体）。
    /// 列头文案同样走 [Description]，规则见 <see cref="AuditOperationDto"/>。
    /// </summary>
    public class AuditFieldChangeDto
    {
        public long Id { get; set; }
        public long OperationId { get; set; }

        [Description("字段路径")]
        public string FieldPath { get; set; }

        [Description("变更类型")]
        public string ChangeType { get; set; }

        [Description("旧值")]
        public string OldValue { get; set; }

        [Description("新值")]
        public string NewValue { get; set; }
    }

    /// <summary>
    /// 导出长表行（头信息 + 明细，一行一条明细）。
    /// 动作型操作（无明细）导出为一行、明细列留空——保证"每行自洽"，Excel 自动筛选即可抽查。
    /// </summary>
    public class AuditFlatRowDto
    {
        public DateTime Time { get; set; }
        public string UserName { get; set; }
        public string RoleNames { get; set; }
        public int RoleLevel { get; set; }
        public string Category { get; set; }
        public string Action { get; set; }
        public string Target { get; set; }
        public string Result { get; set; }
        public string FieldPath { get; set; }

        /// <summary>变更类型（落库的中文文案）；空 = 该行是动作型操作、无明细</summary>
        public string ChangeType { get; set; }

        public string OldValue { get; set; }
        public string NewValue { get; set; }
    }
}
