using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Dtos;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 角色服务：角色查询、新增/更新、删除及级联业务编排。
    /// 从 IUserManager 拆出，避免角色 CRUD 混入用户域；UI 只经此接口操作角色。
    /// </summary>
    public interface IRoleService
    {
        /// <summary>按等级升序返回全部角色。</summary>
        List<RoleDto> GetAllRoles();

        /// <summary>新增或更新角色（Id=0 为新增）；含等级越权校验。成功时回填自增 Id。</summary>
        bool SaveRole(RoleDto role, out string errorMsg);

        /// <summary>
        /// 删除角色：内置角色禁止删除；关联当前登录用户时阻断；
        /// 否则级联删除关联用户、清空手动权限，最后删除角色实体。
        /// </summary>
        bool DeleteRole(int roleId, out string errorMsg);
    }

}
