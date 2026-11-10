using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.Application.AuthService.Services
{
    /// <summary>
    /// 运行时权限服务 — 权限层唯一真相源
    /// 有效权限 = 等级继承（实时计算） ∪ 手动授予（查表）
    /// 管理员短路全量，访客按Level=0实时计算
    /// </summary>
    public class PermissionService : IPermissionService
    {
        private readonly IUserRoleRepository userRoleRepo;
        private readonly IRolePermissionRepository rolePermissionRepo;
        private readonly IPermissionRepository permissionRepo;
        private readonly IRoleRepository roleRepo;

        public PermissionService(
            IUserRoleRepository userRoleRepo,
            IRolePermissionRepository rolePermissionRepo,
            IPermissionRepository permissionRepo,
            IRoleRepository roleRepo)
        {
            this.userRoleRepo = userRoleRepo ?? throw new ArgumentNullException(nameof(userRoleRepo));
            this.rolePermissionRepo = rolePermissionRepo ?? throw new ArgumentNullException(nameof(rolePermissionRepo));
            this.permissionRepo = permissionRepo ?? throw new ArgumentNullException(nameof(permissionRepo));
            this.roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
        }

        /// <summary>
        /// 用户有效权限码 = 其所有角色有效权限的并集
        /// </summary>
        public HashSet<string> GetUserPermissionCodes(int userId)
        {
            // 无角色 ≡ 游客：未登录（userId≤0）与「已登录但未分配任何角色」同规则回落访客权限，
            // 避免出现"未登录有权限、登录后反而没有"的倒挂
            var userRoles = userId > 0 ? userRoleRepo.GetByUserId(userId) : null;
            if (userRoles == null || userRoles.Count == 0)
            {
                var guestRole = roleRepo.GetByName(RoleType.Guest.GetDescription());
                return guestRole != null
                    ? GetRoleEffectivePermissionCodes(guestRole.Id)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }

            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var ur in userRoles)
                result.UnionWith(GetRoleEffectivePermissionCodes(ur.RoleId));

            return result;
        }

        /// <summary>
        /// 角色有效权限 = 自动继承（等级达标） ∪ 手动授予（RolePermission表）
        /// </summary>
        public HashSet<string> GetRoleEffectivePermissionCodes(int roleId)
        {
            var role = roleRepo.GetById(roleId);
            if (role == null)
                return new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 管理员短路：直接返回全部激活权限
            if (role.Level >= (int)RoleType.Administrator)
            {
                return new HashSet<string>(
                    permissionRepo.GetAll().Where(p => p.IsActive == 1).Select(p => p.Code),
                    StringComparer.OrdinalIgnoreCase);
            }

            var allActive = permissionRepo.GetAll().Where(p => p.IsActive == 1).ToList();
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // 1. 自动继承：角色等级 >= 权限所需等级
            foreach (var perm in allActive)
            {
                if (perm.Level.HasValue && role.Level >= perm.Level.Value)
                    result.Add(perm.Code);
            }

            // 2. 手动授予（补充或覆盖）
            var manualIds = rolePermissionRepo.GetByRoleId(roleId).Select(rp => rp.PermissionId);
            foreach (var id in manualIds)
            {
                var perm = permissionRepo.GetById(id);
                if (perm != null && perm.IsActive == 1)
                    result.Add(perm.Code);
            }

            return result;
        }

        public bool HasPermission(int userId, string permissionCode)
        {
            return GetUserPermissionCodes(userId).Contains(permissionCode);
        }

        public List<int> GetRolePermissionIds(int roleId)
        {
            return rolePermissionRepo.GetByRoleId(roleId)
                .Select(rp => rp.PermissionId)
                .ToList();
        }

        public List<PermissionDto> GetAllPermissions()
        {
            return permissionRepo.GetAll().Select(p => p.Adapt<PermissionDto>()).ToList();
        }

        public void GrantPermission(int roleId, int permissionId)
        {
            if (!rolePermissionRepo.Exists(roleId, permissionId))
            {
                rolePermissionRepo.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = permissionId
                });
            }
        }

        public void RevokePermission(int roleId, int permissionId)
        {
            var existing = rolePermissionRepo.GetByRoleId(roleId)
                .FirstOrDefault(rp => rp.PermissionId == permissionId);
            if (existing != null)
            {
                rolePermissionRepo.Delete(existing.Id);
            }
        }

        /// <summary>
        /// 批量同步手动权限：以 desiredPermissionIds 为准，差集增删
        /// </summary>
        public void SyncManualPermissions(int roleId, List<int> desiredPermissionIds)
        {
            var currentIds = rolePermissionRepo.GetByRoleId(roleId)
                .Select(rp => rp.PermissionId).ToList();

            var toRemove = currentIds.Except(desiredPermissionIds).ToList();
            var toAdd = desiredPermissionIds.Except(currentIds).ToList();

            foreach (var id in toRemove)
            {
                var rp = rolePermissionRepo.GetByRoleId(roleId)
                    .FirstOrDefault(x => x.PermissionId == id);
                if (rp != null) rolePermissionRepo.Delete(rp.Id);
            }

            foreach (var id in toAdd)
            {
                rolePermissionRepo.Add(new RolePermission
                {
                    RoleId = roleId,
                    PermissionId = id
                });
            }

        }
    }

}
