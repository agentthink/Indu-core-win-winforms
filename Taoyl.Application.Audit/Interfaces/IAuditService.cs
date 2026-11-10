using System.Collections.Generic;
using Taoyl.Application.Audit.Models;

namespace Taoyl.Application.Audit.Interfaces
{
    /// <summary>
    /// 审计投递契约（写侧，挂点唯一入口）。
    ///
    /// 落点原则（R2 定稿）：**审计 = 操作留痕，不是数据变更追踪。**
    /// 记录点 = 操作发生地 = UI 命令层；后端服务类（ConfigService / RecipeService /
    /// DeviceConfigStore / UserService）**不认识本契约**，结构上不可能产生依赖环。
    ///
    /// 唯一例外：登录域（Login / LoginWithCard / Logout）的身份快照由 AuthService 在正确时点采集 ——
    /// 刷卡登录由键盘钩子直达 AuthService，UI 层结构上不可观测；且登录成功须在 SetCurrentUser 之后、
    /// 登出须在 ClearCurrentUser 之前取身份（早一步身份未就绪、晚一步会话已清空）。
    /// AuthService **不直接投递本契约**，而是通过 AuthFactRecorded 事件广播 AuthFact 快照
    /// （Share 层零依赖契约），消费方在 UI 层接线 —— 见 FormMain.OnAuthFactRecorded。
    ///
    /// 铁律（实现方须遵守）：
    /// 1. 宁漏记不记假账——调用方须在业务写成功后投递，本方法自身绝不抛异常。
    /// 2. 审计永不高于业务——投递失败只降级告警，不阻断、不弹窗、不重试、不回填。
    /// 3. 同步直写——挂点全在操作完成路径，方法返回即代表记录已落地或已降级。
    /// </summary>
    public interface IAuditService
    {
        /// <summary>
        /// 投递一条审计记录。实现方内部消化所有异常，永不上抛。
        /// </summary>
        /// <param name="entry">审计请求（操作者身份快照已由调用方在构造时带入，实现方不做会话采集）</param>
        void Record(AuditEntry entry);

        /// <summary>
        /// 取模型基线快照，供后续 <see cref="BuildChanges"/> 比对。
        ///
        /// 只序列化、**不实例化目标类型** —— 禁用 ConfigBase.Clone&lt;T&gt;：
        /// 其 ObjectCreationHandling.Auto 会复用集合并追加，预置集合的模型克隆会翻倍
        /// （MachineARecipeConfig 曾因此刷出 42 条假"删除"）。
        /// </summary>
        /// <param name="model">待取基线的模型实例</param>
        AuditBaseline Snapshot(object model);

        /// <summary>
        /// 比对前后两态，产出字段级明细。
        ///
        /// 采集实现（反射式 / 声明式清单）属本契约内部事务，
        /// 调用方只负责在**原地覆盖之前**取好基线 —— 这是业务时序，不是实现细节。
        /// </summary>
        /// <param name="before">旧状态：模型实例，或 <see cref="Snapshot"/> 产出的 <see cref="AuditBaseline"/></param>
        /// <param name="after">新状态（模型实例）</param>
        /// <param name="except">不参与比对的顶层字段名，如程序盖的章 ModifiedBy / ModifiedTime / LastLoginTime</param>
        List<AuditFieldChange> BuildChanges(object before, object after, params string[] except);

        /// <summary>
        /// 比对两个集合，**逐项**展开差异明细：每个新增 / 移除的元素各占一条
        /// （新增 → <see cref="Constants.AuditChangeType.Add"/> 旧值留空；移除 → <see cref="Constants.AuditChangeType.Delete"/> 新值留空）。
        /// 用于 [JsonIgnore] 集合（角色列表、权限码集合等）—— 它们走 JSON 会被整个丢掉，是通用 diff 的盲区。
        /// 集合顺序不同但成员相同 → 视为无变化（输出按 Ordinal 排序保证稳定）。
        /// </summary>
        /// <param name="fieldPath">展示用字段名，如"角色""手动权限"</param>
        /// <param name="before">旧集合（null = 空集）</param>
        /// <param name="after">新集合（null = 空集）</param>
        /// <returns>无变化返回**空列表**（不返回 null——与 <see cref="BuildChanges"/> 的约定一致，调用方直接 AddRange）</returns>
        List<AuditFieldChange> BuildSetChanges(string fieldPath, IEnumerable<string> before, IEnumerable<string> after);
    }
}
