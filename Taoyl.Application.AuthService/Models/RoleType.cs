using System.ComponentModel;

namespace Taoyl.Application.AuthService.Models
{
    /// <summary>
    /// 权限角色枚举（百进制）
    /// </summary>
    public enum RoleType
    {
        /// <summary>
        /// 未登录
        /// </summary>
        [Description("游客")] Guest = 0,

        /// <summary>
        /// 操作员
        /// </summary>
        [Description("操作员")] Operator = 100,

        /// <summary>
        /// 技师
        /// </summary>
        [Description("技师")] Technician = 300,

        /// <summary>
        /// 工程师（ME/PE合并）
        /// </summary>
        [Description("工程师")] Engineer = 500,

        /// <summary>
        /// 管理员
        /// </summary>
        [Description("管理员")] Administrator = 700
    }

}
