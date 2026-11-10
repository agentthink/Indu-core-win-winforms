using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Dtos;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 用户管理接口，提供用户数据的增删改查、角色分配等功能。
    /// 用户域只出 DTO；角色 CRUD 见 IRoleService；RBAC 导入导出见 IRbacMigrationService。
    /// </summary>
    public interface IUserManager
    {
        List<UserDto> GetAllUsers();
        UserDto GetByUsername(string username);
        UserDto GetByCardCode(string cardCode);

        /// <summary>新增用户；Password 必须非空。</summary>
        bool AddUser(UserDto user, out string errorMsg);

        /// <summary>更新用户；Password 为空 = 保留原密码，非空 = 更新密码。</summary>
        bool UpdateUser(UserDto user, out string errorMsg);

        bool DeleteUser(int userId, out string errorMsg);
        bool AssignRolesToUser(int userId, List<int> roleIds, out string errorMsg);
        List<RoleDto> GetUserRoles(int userId);
        List<UserDto> GetUsersByRoleId(int roleId);

        /// <summary>只更新最后登录时间（登录链专用，不走用户编辑校验）。</summary>
        bool TouchLastLogin(int userId, out string errorMsg);
    }

}
