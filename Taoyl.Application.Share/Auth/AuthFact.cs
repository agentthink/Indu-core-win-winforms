using System.Collections.Generic;

namespace Taoyl.Application.Share.Auth
{
    /// <summary>
    /// 认证事实类型 —— 登录域向外广播的事实种类。
    /// </summary>
    public enum AuthFactKind
    {
        /// <summary>登录成功（密码登录与刷卡登录共用）</summary>
        LoginSucceeded = 1,

        /// <summary>登录失败（用户名或密码错 / 卡号未绑定 / 登录状态更新失败）</summary>
        LoginFailed = 2,

        /// <summary>注销（手动登出与超时登出共用，不区分）</summary>
        Logout = 3,
    }

    /// <summary>
    /// 认证事实 —— 登录域向外广播的只读快照（配合 IAuthService.AuthFactRecorded 事件）。
    ///
    /// 定位：本类放在 Share 层（零项目依赖），使登录域（Taoyl.Application.AuthService）
    /// 与其消费方（如审计）**互不认识**，只共同认识本契约 ——
    /// 登录域结构上不再持有任何审计类型（过去直接 new AuditEntry 的硬耦合就此切断）。
    ///
    /// 铁律一：身份字段必须在**事实发生时点**采集。
    ///   登出时消费方收到事实后，会话随后即被 ClearCurrentUser 清空；
    ///   消费方**绝不可**回头读 IUserSession —— 会读到空或下一位用户，那是假账。
    ///
    /// 铁律二：登录失败时 RoleNames 为空集合、MaxRoleLevel 为 0 是**诚实的** ——
    ///   身份确实未经验证，不得借用当前在线用户的角色。
    ///
    /// 说明：刻意不带时间戳。消费方为同步直写（事件在业务方法内同步触发），
    ///   接手时刻即事实发生时刻；将来若出现异步或延迟投递需求，再补 OccurredAt。
    /// </summary>
    public class AuthFact
    {
        /// <summary>事实类型</summary>
        public AuthFactKind Kind { get; set; }

        /// <summary>本次尝试 / 生效的用户名；登录失败且未提供用户名时为"未知用户"</summary>
        public string Username { get; set; }

        /// <summary>角色名快照（登录失败时为空集合）</summary>
        public List<string> RoleNames { get; set; } = new List<string>();

        /// <summary>最高角色等级快照（登录失败时为 0）</summary>
        public int MaxRoleLevel { get; set; }
    }
}
