using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 角色仓储接口
    /// </summary>
    public interface IRoleRepository
    {
        /// <summary>
        /// 获取所有角色
        /// </summary>
        List<Role> GetAll();

        /// <summary>
        /// 根据ID获取角色
        /// </summary>
        Role GetById(int id);

        /// <summary>
        /// 根据名称获取角色
        /// </summary>
        Role GetByName(string roleName);

        /// <summary>
        /// 新增角色
        /// </summary>
        void Add(Role role);

        /// <summary>
        /// 更新角色
        /// </summary>
        void Update(Role role);

        /// <summary>
        /// 删除角色
        /// </summary>
        void Delete(int id);

        /// <summary>
        /// 删除角色并解除其全部关联（sys_role_permission + sys_user_role + sys_role，单事务）。
        /// </summary>
        bool DeleteWithRelations(int roleId);

        /// <summary>
        /// 该角色是否已关联指定用户（角色删除防自锁校验用）。
        /// </summary>
        bool IsAssignedToUser(int roleId, int userId);

        
    }

}
