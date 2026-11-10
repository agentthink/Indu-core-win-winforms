using SqlSugar;

namespace Taoyl.Application.AuthService.Models.Entities
{
    /// <summary>
    /// 用户角色关联实体
    /// </summary>
    [SugarTable("sys_user_role", TableDescription = "用户角色关联表")]
    public class UserRole
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RoleId { get; set; }
    }

}
