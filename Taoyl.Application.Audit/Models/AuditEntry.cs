using System.Collections.Generic;
using Taoyl.Application.Audit.Constants;

namespace Taoyl.Application.Audit.Models
{
    /// <summary>
    /// 字段级变更条目（投递用轻量 DTO，与落库实体解耦）。
    /// </summary>
    public class AuditFieldChange
    {
        /// <summary>字段点分路径，如 组名.标签名 / Ip / Port</summary>
        public string FieldPath { get; set; }

        /// <summary>变更类型（<see cref="AuditChangeType"/> 成员）</summary>
        public AuditChangeType ChangeType { get; set; }

        /// <summary>
        /// 旧值（不截断）。
        /// 🔴 约定：留空 = 旧值**不可知**，不等于"原值为空"。
        /// PLC 写入即属此类——写前读到的缓存值既非操作者所见、也非 PLC 真值（读之后可能已被他人改写），记了就是半假账。
        /// </summary>
        public string OldValue { get; set; }

        /// <summary>新值（不截断；删除类留空）</summary>
        public string NewValue { get; set; }
    }

    /// <summary>
    /// 审计投递请求（一次操作 = 一条头记录 + 0..N 条明细）。
    ///
    /// 身份三快照由**调用方随请求带入**，审计服务不反向读取会话：
    /// 持久化服务没有任何理由知道"当前是谁"；让命令层组装身份输入，
    /// 既贴合"记录点 = 操作发生地 = UI 命令层"的落点原则，也让 AuditService 只依赖数据库
    /// ——结构上不再产生依赖环（也无需"某某类永不许持有 IAuditService"这类靠注释维护的禁令）。
    ///
    /// 三个维度收**枚举**（2026-09-18 改型）：页面 / 动词 / 结果三者类型不同，
    /// 过去同为 string 时（category, action, …）两个相邻参数写反**编译器不报错**，
    /// 只会静静落一行永远查不出来的记录；改成枚举后这个盲区被编译期直接堵死。
    /// 落库文案（[Description]）由 AuditService 在写库前统一转换，本类不碰存储格式。
    ///
    /// 身份收 <see cref="AuditActor"/>（2026-09-19 改型）：本类原先额外提供"名字 / 角色串 / 等级"
    /// 三元组重载，供不持有 UserInfo 的调用方使用；审计模块独立成项目后，UserInfo 版整体删除，
    /// 三元组版取而代之成为**唯一**构造 —— 一个构造就没有"两个入口语义是否一致"的疑问。
    /// </summary>
    public class AuditEntry
    {
        /// <summary>
        /// 构造一条审计请求。身份取自 <paramref name="actor"/> 的**快照**（用户名列 + 角色名快照 + 生效等级）。
        /// </summary>
        /// <param name="category">页面（<see cref="AuditCategory"/> 成员）</param>
        /// <param name="action">命令级动词（<see cref="AuditAction"/> 成员）</param>
        /// <param name="target">操作对象业务主键（配置节名 / 设备名 / 配方名 / 组名.标签名 / 用户名 / 命令名 (权限码)）</param>
        /// <param name="actor">
        /// 操作者快照；null 或用户名为空 = 未登录（归一为"访客"）。
        /// 登录失败场景显式传 <c>new AuditActor(尝试的用户名, null, 0)</c>——
        /// 角色列留空（身份未验证，不借用当前在线用户的角色，否则是假账）。
        /// </param>
        /// <param name="result">结果两态（<see cref="AuditResult"/> 成员）</param>
        public AuditEntry(AuditCategory category, AuditAction action, string target,
            AuditActor actor = null, AuditResult result = AuditResult.Success)
        {
            Category = category;
            Action = action;
            Target = target;
            Result = result;

            if (actor == null || string.IsNullOrWhiteSpace(actor.Name))
            {
                ActorName = "访客";
                ActorRoles = string.Empty;
                ActorLevel = 0;
                return;
            }

            ActorName = actor.Name;
            ActorRoles = actor.RoleNames ?? string.Empty;
            ActorLevel = actor.Level;
        }

        /// <summary>页面（<see cref="AuditCategory"/> 成员）</summary>
        public AuditCategory Category { get; private set; }

        /// <summary>命令级动词（<see cref="AuditAction"/> 成员）</summary>
        public AuditAction Action { get; private set; }

        /// <summary>操作对象业务主键</summary>
        public string Target { get; private set; }

        /// <summary>结果两态（<see cref="AuditResult"/> 成员）</summary>
        public AuditResult Result { get; private set; }

        /// <summary>操作者名快照（角色会改名/换组，快照防追溯失真）</summary>
        public string ActorName { get; private set; }

        /// <summary>角色名快照，逗号连接（无角色为空串）</summary>
        public string ActorRoles { get; private set; }

        /// <summary>生效等级快照 = 最高角色等级（多角色取 max，与 PermissionService 的 role.Level &gt;= perm.Level 判定一致）</summary>
        public int ActorLevel { get; private set; }

        /// <summary>字段级变更明细（无变更的动作型操作留空）</summary>
        public List<AuditFieldChange> Changes { get; set; }
    }
}
