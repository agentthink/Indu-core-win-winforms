using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Dtos;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 运行时权限服务
    /// 负责权限码查询、角色权限关联查询
    /// 不涉及UI层模型，避免循环依赖
    /// </summary>
    public interface IPermissionService
    {
        /// <summary>
        /// 获取用户有效权限码（实时计算：自动继承 ∪ 手动授予）
        /// </summary>
        HashSet<string> GetUserPermissionCodes(int userId);

        /// <summary>
        /// 获取角色有效权限码（实时计算）
        /// </summary>
        HashSet<string> GetRoleEffectivePermissionCodes(int roleId);

        /// <summary>
        /// 检查用户是否拥有指定权限
        /// </summary>
        bool HasPermission(int userId, string permissionCode);

        /// <summary>
        /// 获取角色的权限ID集合（仅手动授予部分）
        /// </summary>
        List<int> GetRolePermissionIds(int roleId);

        /// <summary>
        /// 获取全量权限（含失效项）。UI 权限树自行过滤 IsActive == 1；
        /// 审计权限码对照需用全量，避免失效但仍被手动关联的权限码静默丢失。
        /// </summary>
        List<PermissionDto> GetAllPermissions();

        /// <summary>
        /// 手动授予权限给角色
        /// </summary>
        void GrantPermission(int roleId, int permissionId);

        /// <summary>
        /// 手动撤销角色的权限
        /// </summary>
        void RevokePermission(int roleId, int permissionId);

        /// <summary>
        /// 同步角色的手动权限（批量覆盖，用于权限树保存）
        /// </summary>
        void SyncManualPermissions(int roleId, List<int> desiredPermissionIds);
    }

}
