using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 用户角色关联仓储接口
    /// </summary>
    public interface IUserRoleRepository
    {
        /// <summary>
        /// 获取所有关联
        /// </summary>
        List<UserRole> GetAll();

        /// <summary>
        /// 根据用户ID获取关联
        /// </summary>
        List<UserRole> GetByUserId(int userId);

        /// <summary>
        /// 根据角色ID获取关联
        /// </summary>
        List<UserRole> GetByRoleId(int roleId);

        /// <summary>
        /// 新增关联
        /// </summary>
        void Add(UserRole userRole);

        /// <summary>
        /// 删除关联
        /// </summary>
        void Delete(int id);

        /// <summary>
        /// 根据用户ID删除所有关联
        /// </summary>
        void DeleteByUserId(int userId);

        
    }

}
