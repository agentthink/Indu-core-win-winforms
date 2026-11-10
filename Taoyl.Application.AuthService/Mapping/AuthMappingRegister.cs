using Mapster;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Mapping
{
    /// <summary>
    /// 认证授权模块映射登记（Mapster IRegister）。由组合根 GlobalSettings.Scan(...) 统一装配。
    /// 本文件是全模块唯一的 TypeAdapterConfig 声明点；任何差异（Ignore / Map）都必须登记在此，
    /// 否则守卫脚本（data_access_guard.py 映射核对）报违规。
    /// </summary>
    public class AuthMappingRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 读侧：Roles 由 UserService.ToDto 单点装配，不走实体映射，故显式 Ignore；
            // 两个只读计算成员无 setter，天然不参与映射。
            // 映射豁免: Roles
            config.NewConfig<UserInfo, UserDto>()
                  .Ignore(dest => dest.Roles);

            // 写侧：DTO 的 Roles 与两个只读计算成员在实体无对应成员，不会写回。
            config.NewConfig<UserDto, UserInfo>();

            // 角色 / 权限 / 两关系表：同名同类型全同构，双向自动匹配。
            config.NewConfig<Role, RoleDto>();
            config.NewConfig<RoleDto, Role>();

            config.NewConfig<Permission, PermissionDto>();
            config.NewConfig<PermissionDto, Permission>();

            config.NewConfig<UserRole, UserRoleDto>();
            config.NewConfig<UserRoleDto, UserRole>();

            config.NewConfig<RolePermission, RolePermissionDto>();
            config.NewConfig<RolePermissionDto, RolePermission>();
        }
    }
}
