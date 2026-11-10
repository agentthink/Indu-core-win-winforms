using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Taoyl.Application.AuthService.Models;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.Application.AuthService.Models.Dtos
{
    /// <summary>
    /// 用户 DTO（服务接口 / UI / 会话统一载体）。
    /// Password 为通用成员，供登录、新增、改密、导出使用；用户列表不显示它。
    /// </summary>
    public class UserDto
    {
        [Description("用户ID")]
        public int UserId { get; set; }

        [Description("用户名")]
        public string Username { get; set; }

        [Description("用户工号")]
        public string UserNumber { get; set; }

        [Description("卡号")]
        public string CardCode { get; set; }

        [Description("最后登录时间")]
        public string LastLoginTime { get; set; }

        [Description("添加人")]
        public string AddPeople { get; set; }

        [Description("密码")]
        public string Password { get; set; }

        [Description("创建时间")]
        public string CreateTime { get; set; }

        // 角色集合由 UserService 单点装配；三个聚合成员不参与通用快照，避免角色信息重复记账。
        [JsonIgnore]
        public List<RoleDto> Roles { get; set; } = new List<RoleDto>();

        [JsonIgnore]
        [Description("角色等级")]
        public int MaxRoleLevel => Roles?.Any() == true ? Roles.Max(r => r.Level) : 0;

        [JsonIgnore]
        [Description("角色")]
        public string RoleNamesDisplay => Roles?.Any() == true
            ? string.Join(",", Roles.Select(r => r.RoleName))
            : RoleType.Guest.GetDescription();

        public static UserDto CreateGuest()
        {
            return new UserDto
            {
                Username = "未登录",
                Roles = new List<RoleDto>
                {
                    new RoleDto
                    {
                        RoleName = RoleType.Guest.GetDescription(),
                        Level = (int)RoleType.Guest
                    }
                }
            };
        }
    }

    /// <summary>
    /// 角色 DTO
    /// </summary>
    public class RoleDto
    {
        public int Id { get; set; }

        [Description("角色")]
        public string RoleName { get; set; }

        [Description("角色等级")]
        public int Level { get; set; }

        [Description("是否内置")]
        public int IsBuiltIn { get; set; }

        [Description("备注")]
        public string Remark { get; set; }
    }

    /// <summary>
    /// 权限 DTO
    /// </summary>
    public class PermissionDto
    {
        public int Id { get; set; }

        public string Code { get; set; }

        public string Name { get; set; }

        public int Type { get; set; }

        public string ParentCode { get; set; }

        public int Sort { get; set; }

        public int? Level { get; set; }

        public string FormClass { get; set; }

        public string DllName { get; set; }

        public int IsActive { get; set; }
    }

    /// <summary>
    /// 用户角色关联 DTO（契约完备性 DTO，业务内部仍使用实体关系行）
    /// </summary>
    public class UserRoleDto
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public int RoleId { get; set; }
    }

    /// <summary>
    /// 角色权限关联 DTO（契约完备性 DTO，业务内部仍使用实体关系行）
    /// </summary>
    public class RolePermissionDto
    {
        public int Id { get; set; }

        public int RoleId { get; set; }

        public int PermissionId { get; set; }
    }

}
