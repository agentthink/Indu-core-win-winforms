using SqlSugar;

namespace Taoyl.Application.AuthService.Models.Entities
{
    /// <summary>
    /// 角色权限关联实体（手动授予部分，自动继承不落库）
    /// </summary>
    [SugarTable("sys_role_permission", TableDescription = "角色权限关联表")]
    public class RolePermission
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public int RoleId { get; set; }

        public int PermissionId { get; set; }

        // Source 字段已废弃，自动继承改为实时计算，不再持久化
    }

}
