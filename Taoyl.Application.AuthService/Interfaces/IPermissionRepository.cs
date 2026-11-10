using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 权限仓储接口
    /// </summary>
    public interface IPermissionRepository
    {
        /// <summary>
        /// 获取所有权限
        /// </summary>
        List<Permission> GetAll();

        /// <summary>
        /// 根据ID获取权限
        /// </summary>
        Permission GetById(int id);

        /// <summary>
        /// 根据编码获取权限
        /// </summary>
        Permission GetByCode(string code);

        /// <summary>
        /// 根据类型获取权限
        /// </summary>
        List<Permission> GetByType(int type);

        /// <summary>
        /// 获取所有启用的菜单权限
        /// </summary>
        List<Permission> GetActiveMenus();

        /// <summary>
        /// 获取所有启用的功能权限
        /// </summary>
        List<Permission> GetActiveFeatures();

        /// <summary>
        /// 新增权限
        /// </summary>
        void Add(Permission permission);

        /// <summary>
        /// 更新权限
        /// </summary>
        void Update(Permission permission);

        /// <summary>
        /// 删除权限（逻辑删除：标记IsActive=0）
        /// </summary>
        void Delete(int id);

        
    }

}
