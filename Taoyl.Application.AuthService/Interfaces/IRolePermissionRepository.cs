using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 角色权限关联仓储接口
    /// </summary>
    public interface IRolePermissionRepository
    {
        /// <summary>
        /// 获取所有关联
        /// </summary>
        List<RolePermission> GetAll();

        /// <summary>
        /// 根据角色ID获取关联
        /// </summary>
        List<RolePermission> GetByRoleId(int roleId);

        /// <summary>
        /// 根据权限ID获取关联
        /// </summary>
        List<RolePermission> GetByPermissionId(int permissionId);

        /// <summary>
        /// 检查关联是否存在
        /// </summary>
        bool Exists(int roleId, int permissionId);

        /// <summary>
        /// 新增关联
        /// </summary>
        void Add(RolePermission rolePermission);

        /// <summary>
        /// 删除关联
        /// </summary>
        void Delete(int id);

        /// <summary>
        /// 根据角色ID删除所有关联
        /// </summary>
        void DeleteByRoleId(int roleId);

        
    }

}
