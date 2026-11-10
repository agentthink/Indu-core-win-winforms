using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Services
{
    /// <summary>
    /// 角色服务实现：角色 DTO 查询、保存与删除编排。
    /// 实体↔DTO 转换边界在服务；仓储只做实体进出。
    /// </summary>
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository roleRepo;
        private readonly IUserSession userSession;

        public RoleService(
            IRoleRepository roleRepo,
            IUserSession userSession)
        {
            this.roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        public List<RoleDto> GetAllRoles()
        {
            return roleRepo.GetAll()
                .OrderBy(r => r.Level)
                .Select(r => r.Adapt<RoleDto>())
                .ToList();
        }

        public bool SaveRole(RoleDto role, out string errorMsg)
        {
            errorMsg = null;

            if (role == null)
            {
                errorMsg = "角色信息不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(role.RoleName) || role.Level <= 0)
            {
                errorMsg = "角色名不能为空且等级需大于0";
                return false;
            }

            // 权限保护：不能创建等级高于自己的角色
            if (role.Level > userSession.CurrentLevel)
            {
                errorMsg = "不能创建等级高于当前用户的角色";
                return false;
            }

            var entity = role.Adapt<Role>();
            if (entity.Id == 0)
            {
                // 内置标记只由 RoleType 枚举驱动，新增角色一律非内置
                entity.IsBuiltIn = 0;
                roleRepo.Add(entity);
            }
            else
            {
                var existing = roleRepo.GetById(entity.Id);
                if (existing == null)
                {
                    errorMsg = "角色不存在";
                    return false;
                }

                // 内置角色的 RoleName / Level 只读：以库中原值覆盖，仅应用 Remark。
                // 原值回写 DTO，供 VM 审计取到正确值（否则会把被忽略的输入记成变更）。
                if (existing.IsBuiltIn == 1)
                {
                    entity.RoleName = existing.RoleName;
                    entity.Level = existing.Level;
                    entity.IsBuiltIn = existing.IsBuiltIn;
                    role.RoleName = existing.RoleName;
                    role.Level = existing.Level;
                }

                roleRepo.Update(entity);
            }

            // 回填自增 Id，供调用方随后同步手动权限
            role.Id = entity.Id;
            return true;
        }

        public bool DeleteRole(int roleId, out string errorMsg)
        {
            errorMsg = null;

            var role = roleRepo.GetById(roleId);
            if (role == null)
            {
                errorMsg = "角色不存在";
                return false;
            }

            if (role.IsBuiltIn == 1)
            {
                errorMsg = "内置角色不能删除";
                return false;
            }

            // 阻断：不能删除包含当前登录用户的角色
            int currentUserId = userSession.CurrentUser?.UserId ?? 0;
            if (roleRepo.IsAssignedToUser(roleId, currentUserId))
            {
                errorMsg = "当前登录用户关联此角色，无法删除。请先切换账号或解绑该角色。";
                return false;
            }

            // 单事务解除关联并删角色（sys_role_permission + sys_user_role + sys_role）
            if (!roleRepo.DeleteWithRelations(roleId))
            {
                errorMsg = "删除角色失败";
                return false;
            }

            return true;
        }
    }

}
