namespace Taoyl.Application.Audit.Models
{
    /// <summary>
    /// 操作者身份快照 —— 审计投递请求携带的"谁做的"。
    ///
    /// 存在理由：审计模块独立成项目后，<see cref="AuditEntry"/> 不能再吃
    /// <c>Taoyl.Application.AuthService.Models.Entities.UserInfo</c> ——
    /// 那会让审计项目反向依赖认证业务域，且 UserInfo 有 8 个字段、审计只取其中 3 个。
    /// 本类就是那 3 个字段的最小载体，使依赖方向保持 `消费方 → 审计`，审计不认识任何业务模型。
    ///
    /// 字段语义（与落库头表列一一对应，见 Entities.AuditOperationEntity）：
    /// - <see cref="Name"/> → UserName（列宽 50）
    /// - <see cref="RoleNames"/> → RoleNames（列宽 200，逗号连接）
    /// - <see cref="Level"/> → RoleLevel（最高角色等级，多角色取 max）
    ///
    /// 🔴 三者都是**快照**，不是引用：角色会改名 / 换组，事后回查得不到当时的身份
    /// （与 AuditEntry 的既有约定一致）。
    ///
    /// 🔴 登录失败场景**显式构造**、不借用在线用户的角色 ——
    /// 身份未经验证却挂上别人的角色，那是假账（见 IAuditService 铁律一）。
    /// </summary>
    public sealed class AuditActor
    {
        /// <summary>
        /// 访客哨兵：未登录 / 名字为空时的归一目标（落库为"访客"、角色串空、等级 0）。
        /// </summary>
        public static readonly AuditActor Guest = new AuditActor(null, null, 0);

        /// <summary>
        /// 构造一份身份快照。
        /// </summary>
        /// <param name="name">操作者名快照；null / 空白 = 未登录（投递时归一为"访客"）</param>
        /// <param name="roleNames">角色名快照，逗号连接（无角色传 null 或空串）</param>
        /// <param name="level">生效等级快照 = 最高角色等级</param>
        public AuditActor(string name, string roleNames, int level)
        {
            Name = name;
            RoleNames = roleNames;
            Level = level;
        }

        /// <summary>操作者名快照（null / 空白 = 未登录）</summary>
        public string Name { get; private set; }

        /// <summary>角色名快照，逗号连接（无角色为空串）</summary>
        public string RoleNames { get; private set; }

        /// <summary>生效等级快照 = 最高角色等级（多角色取 max）</summary>
        public int Level { get; private set; }
    }
}
