using System.Linq;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Models.Dtos;

namespace Taoyl.UI.Pages.Extensions
{
    /// <summary>
    /// 会话身份 → 审计身份快照的**适配**（UI 命令层专用）。
    ///
    /// 为什么这层适配必须留在 UI 侧：审计模块（Taoyl.Application.Audit）独立成项目后
    /// **不认识 <see cref="UserDto"/>** —— 那会让审计反向依赖认证业务域，
    /// 而它其实只取其中 3 个字段（用户名 / 角色名 / 最高等级）。
    /// 于是转换只能发生在"同时认识两边"的地方，也就是这里：
    /// UI 命令层既持有 IUserSession（业务身份），又投递 AuditEntry（审计身份），
    /// 由它做一次显式翻译，依赖方向才保持单向（消费方 → 审计）。
    ///
    /// 🔴 快照语义：调用点取的是**当下这一刻**的身份，取完即与 UserDto 脱钩。
    /// 操作者日后改名 / 换角色，历史记录仍反映操作当时的身份。
    /// 这与原 AuditEntry 用户版构造的行为逐字一致——本次迁移只换载体，不改语义。
    ///
    /// 放本命名空间（而非新建 Extensions 目录）是为了让 ViewModels 下的调用点零 using 改动；
    /// 6 个 VM 都用它，收敛一处胜过各写各的。
    /// </summary>
    internal static class AuditActorExtensions
    {
        /// <summary>
        /// 取用户身份的审计快照；null 或用户名为空白 = 未登录，归一为访客
        /// （归一规则与 AuditEntry 构造内的兜底一致，此处提前归一只是为省一次分配）。
        /// </summary>
        public static AuditActor ToAuditActor(this UserDto user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username)) return AuditActor.Guest;

            var roles = user.Roles;
            string roleNames = roles == null || roles.Count == 0
                ? string.Empty
                : string.Join(",", roles.Where(r => r != null).Select(r => r.RoleName));

            return new AuditActor(user.Username, roleNames, user.MaxRoleLevel);
        }
    }
}
