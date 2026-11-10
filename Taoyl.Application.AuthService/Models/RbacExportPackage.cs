using System.Collections.Generic;

namespace Taoyl.Application.AuthService.Models
{
    /// <summary>
    /// RBAC跨机迁移导出包（以名称/Code为锚点，完全不依赖自增ID）
    /// </summary>
    public class RbacExportPackage
    {
        public string Version { get; set; } = "1.0";
        public string ExportTime { get; set; }
        /// <summary>Menu.json权限指纹，用于跨机一致性校验</summary>
        public string MenuVersion { get; set; }
        public List<RoleExportItem> Roles { get; set; } = new List<RoleExportItem>();
        public List<UserExportItem> Users { get; set; } = new List<UserExportItem>();
    }

    public class RoleExportItem
    {
        public string RoleName { get; set; }
        public int Level { get; set; }
        public int IsBuiltIn { get; set; }
        public string Remark { get; set; }
        /// <summary>
        /// 手动授予的权限Code列表。自动继承的权限由Level实时计算，无需导出。
        /// </summary>
        public List<string> PermissionCodes { get; set; } = new List<string>();
    }

    public class UserExportItem
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public string UserNumber { get; set; }
        public string CardCode { get; set; }
        public string AddPeople { get; set; }
        public string CreateTime { get; set; }
        /// <summary>关联的角色名称列表（按名称匹配本地角色）</summary>
        public List<string> RoleNames { get; set; } = new List<string>();
    }

}
