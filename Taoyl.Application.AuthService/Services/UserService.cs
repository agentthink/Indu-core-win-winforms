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
    /// 用户服务实现（RBAC改造版）
    /// 职责：用户数据管理 + 会话状态持有
    /// 权限计算完全委托给 IPermissionService
    /// 实体↔DTO 的唯一转换边界：对外只出 DTO，实体不出服务方法栈
    /// </summary>
    public class UserService : IUserSession, IUserManager
    {
        private readonly IUserRepository repository;
        private readonly IUserRoleRepository userRoleRepo;
        private readonly IRoleRepository roleRepo;
        private readonly IPermissionService permissionService;
        private UserDto currentUser;
        private HashSet<string> currentPermissionCodes = new HashSet<string>();
        private readonly object sessionLock = new object();

        public UserService(
            IUserRepository userRepository,
            IUserRoleRepository userRoleRepo,
            IRoleRepository roleRepo,
            IPermissionService permissionService)
        {
            repository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
            this.userRoleRepo = userRoleRepo ?? throw new ArgumentNullException(nameof(userRoleRepo));
            this.roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
            this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            // 启动时初始化为访客会话，确保未登录状态菜单不空白
            currentUser = UserDto.CreateGuest();
            InitializeGuestSession();
        }

        #region 会话状态

        public UserDto CurrentUser
        {
            get { lock (sessionLock) { return currentUser; } }
        }

        public bool IsLoggedIn
        {
            get { lock (sessionLock) { return currentUser?.MaxRoleLevel > 0; } }
        }

        public int CurrentLevel
        {
            get { lock (sessionLock) { return currentUser?.MaxRoleLevel ?? 0; } }
        }

        public HashSet<string> CurrentPermissionCodes
        {
            get { lock (sessionLock) { return currentPermissionCodes; } }
        }

        public event Action<UserDto> UserChanged;

        public void SetCurrentUser(UserDto user, HashSet<string> permissionCodes)
        {
            if (user == null) throw new ArgumentNullException(nameof(user));

            lock (sessionLock)
            {
                currentUser = Clone(user);
                currentPermissionCodes = permissionCodes ?? new HashSet<string>();
            }

            UserChanged?.Invoke(currentUser);
        }

        public void SetCurrentUser(UserDto user)
        {
            SetCurrentUser(user, new HashSet<string>());
        }

        public void ClearCurrentUser()
        {
            lock (sessionLock)
            {
                currentUser = UserDto.CreateGuest();
                InitializeGuestSession();
            }

            UserChanged?.Invoke(currentUser);
        }

        public void ReloadCurrentUser()
        {
            lock (sessionLock)
            {
                if (currentUser == null) return;

                var fresh = ToDto(repository.GetById(currentUser.UserId));
                if (fresh == null) return;

                currentUser = Clone(fresh);
                currentPermissionCodes = permissionService.GetUserPermissionCodes(fresh.UserId);
                UserChanged?.Invoke(currentUser);
            }
        }

        /// <summary>
        /// 初始化访客会话：实时计算访客角色的有效权限
        /// </summary>
        private void InitializeGuestSession()
        {
            var guestRole = roleRepo.GetByName(RoleType.Guest.GetDescription());
            currentPermissionCodes = guestRole != null
                ? permissionService.GetRoleEffectivePermissionCodes(guestRole.Id)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        }

        #endregion

        #region 用户数据管理

        /// <summary>
        /// 唯一的 UserDto 装配点：实体→DTO 并填充角色。所有用户读取出口统一经过此方法。
        /// </summary>
        private UserDto ToDto(UserInfo entity)
        {
            if (entity == null) return null;

            var dto = entity.Adapt<UserDto>();
            dto.Roles = LoadRoleDtos(entity.UserId);
            return dto;
        }

        /// <summary>
        /// 按用户查询角色 DTO（Roles 集合的唯一查询装配点，供 ToDto 与 GetUserRoles 共用）。
        /// </summary>
        private List<RoleDto> LoadRoleDtos(int userId)
        {
            var userRoles = userRoleRepo.GetByUserId(userId);
            var roles = new List<RoleDto>();
            foreach (var ur in userRoles)
            {
                var role = roleRepo.GetById(ur.RoleId);
                if (role != null) roles.Add(role.Adapt<RoleDto>());
            }
            return roles;
        }

        public List<UserDto> GetAllUsers()
        {
            return repository.GetAll().Select(ToDto).Where(d => d != null).ToList();
        }

        public UserDto GetByUsername(string username) => ToDto(repository.GetByUsername(username));

        public UserDto GetByCardCode(string cardCode) => ToDto(repository.GetByCardCode(cardCode));

        public bool AddUser(UserDto user, out string errorMsg)
        {
            errorMsg = null;

            if (user == null)
            {
                errorMsg = "用户信息不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                errorMsg = "用户名不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.Password))
            {
                errorMsg = "密码不能为空";
                return false;
            }

            if (repository.GetByUsername(user.Username) != null)
            {
                errorMsg = "用户名已存在";
                return false;
            }

            if (repository.CardCodeExists(user.CardCode))
            {
                errorMsg = "卡号已被绑定";
                return false;
            }

            // CreateTime / AddPeople 由服务维护，不信任 DTO 传入值
            user.CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            user.AddPeople = CurrentUser?.Username ?? "系统";

            var entity = user.Adapt<UserInfo>();
            repository.Add(entity);

            // 回写仓储生成的自增 ID，后续 AssignRolesToUser 必须拿到真实 ID
            user.UserId = entity.UserId;
            return true;
        }

        public bool UpdateUser(UserDto user, out string errorMsg)
        {
            errorMsg = null;

            if (user == null)
            {
                errorMsg = "用户信息不能为空";
                return false;
            }

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                errorMsg = "用户名不能为空";
                return false;
            }

            var existingByName = repository.GetByUsername(user.Username);
            if (existingByName != null && existingByName.UserId != user.UserId)
            {
                errorMsg = "用户名已被其他用户使用";
                return false;
            }

            if (repository.CardCodeExists(user.CardCode, user.UserId))
            {
                errorMsg = "卡号已被绑定";
                return false;
            }

            var entity = repository.GetById(user.UserId);
            if (entity == null)
            {
                errorMsg = "用户不存在";
                return false;
            }

            // 等级保护：不能编辑等级高于自己的用户（对齐 UI CanEdit 的 <= currentLevel 语义）
            if (GetUserMaxRoleLevel(user.UserId) > CurrentLevel)
            {
                errorMsg = "不能编辑等级高于自己的用户";
                return false;
            }

            // 只覆盖可编辑资料列；CreateTime / LastLoginTime / AddPeople 保持原值
            entity.Username = user.Username;
            entity.UserNumber = user.UserNumber;
            entity.CardCode = user.CardCode;

            // 空密码 = 保留原密码，绝不把 DTO 默认值覆盖进实体
            bool updatePassword = !string.IsNullOrWhiteSpace(user.Password);
            if (updatePassword)
                entity.Password = user.Password;

            repository.UpdateEditableFields(entity, updatePassword);
            return true;
        }

        public bool TouchLastLogin(int userId, out string errorMsg)
        {
            errorMsg = null;
            var now = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            if (!repository.UpdateLastLoginTime(userId, now))
            {
                errorMsg = "更新最后登录时间失败";
                return false;
            }
            return true;
        }

        public bool DeleteUser(int userId, out string errorMsg)
        {
            errorMsg = null;
            lock (sessionLock)
            {
                if (currentUser != null && currentUser.UserId == userId)
                {
                    errorMsg = "不能删除当前登录用户";
                    return false;
                }
            }
            var userToDelete = repository.GetById(userId);
            if (userToDelete == null)
            {
                errorMsg = "用户不存在";
                return false;
            }

            // 等级保护：不能删除等级高于自己的用户（对齐 UI CanDelete 的 <= currentLevel 语义）
            int targetLevel = GetUserMaxRoleLevel(userId);
            if (targetLevel > CurrentLevel)
            {
                errorMsg = "不能删除等级高于自己的用户";
                return false;
            }

            // 防锁死③：目标是管理员级且删除后系统将无管理员 ⇒ 拒绝
            if (IsAdminLevel(targetLevel) && !HasAnyAdminLevelUser(userId))
            {
                errorMsg = "不能移除系统中的最后一个管理员账户";
                return false;
            }

            userRoleRepo.DeleteByUserId(userId);

            repository.Delete(userId);
            return true;
        }


        #endregion

        #region 角色分配

        public bool AssignRolesToUser(int userId, List<int> roleIds, out string errorMsg)
        {
            errorMsg = null;

            // 等级保护：两条**独立**判据，校验对象不同，勿合并——
            //   ① 被分配的「角色」不能高于自己：防越权提升（自己 500 不能把别人配成 700）
            //   ② 目标「用户」不能高于自己  ：防越权修改（本方法是整集合替换，能剥掉高等级用户的角色）
            int currentLevel = CurrentLevel;
            bool newSetHasAdmin = false;
            foreach (var roleId in roleIds)
            {
                var role = roleRepo.GetById(roleId);
                if (role != null && role.Level > currentLevel)
                {
                    errorMsg = $"不能分配等级高于当前用户的角色: {role.RoleName}";
                    return false;
                }
                if (role != null && IsAdminLevel(role.Level))
                    newSetHasAdmin = true;
            }

            int targetLevel = GetUserMaxRoleLevel(userId);
            if (targetLevel > currentLevel)
            {
                errorMsg = "不能修改等级高于自己的用户的角色";
                return false;
            }

            // 防锁死②：目标当前为管理员级且新角色集合不含管理员级 ⇒ 若系统将无管理员则拒绝
            if (IsAdminLevel(targetLevel) && !newSetHasAdmin && !HasAnyAdminLevelUser(userId))
            {
                errorMsg = "不能移除系统中的最后一个管理员账户";
                return false;
            }

            userRoleRepo.DeleteByUserId(userId);
            foreach (var roleId in roleIds)
            {
                userRoleRepo.Add(new UserRole { UserId = userId, RoleId = roleId });
            }

            return true;
        }

        public List<RoleDto> GetUserRoles(int userId)
        {
            return LoadRoleDtos(userId);
        }

        public List<UserDto> GetUsersByRoleId(int roleId)
        {
            var userIds = userRoleRepo.GetByRoleId(roleId).Select(ur => ur.UserId).ToList();
            var allUsers = repository.GetAll();
            return allUsers.Where(u => userIds.Contains(u.UserId)).Select(ToDto).Where(d => d != null).ToList();
        }

        #endregion

        #region 辅助

        private static UserDto Clone(UserDto src)
        {
            if (src == null) return null;
            return new UserDto
            {
                UserId = src.UserId,
                Username = src.Username,
                Password = src.Password,
                UserNumber = src.UserNumber,
                CardCode = src.CardCode,
                LastLoginTime = src.LastLoginTime,
                AddPeople = src.AddPeople,
                CreateTime = src.CreateTime,
                Roles = src.Roles?.Select(r => new RoleDto
                {
                    Id = r.Id,
                    RoleName = r.RoleName,
                    Level = r.Level,
                    IsBuiltIn = r.IsBuiltIn,
                    Remark = r.Remark
                }).ToList() ?? new List<RoleDto>()
            };
        }

        /// <summary>
        /// 目标用户的最高角色等级（无角色 = 0，即游客规则）。
        /// </summary>
        private int GetUserMaxRoleLevel(int userId)
        {
            var roles = LoadRoleDtos(userId);
            return roles.Count > 0 ? roles.Max(r => r.Level) : 0;
        }

        private static bool IsAdminLevel(int level) => level >= (int)RoleType.Administrator;

        /// <summary>
        /// 是否存在管理员级用户（任一角色 Level ≥ Administrator）。
        /// excludeUserId 用于判定"移除该用户后系统是否仍有管理员"。
        /// </summary>
        private bool HasAnyAdminLevelUser(int? excludeUserId = null)
        {
            var roleLevels = roleRepo.GetAll().ToDictionary(r => r.Id, r => r.Level);
            foreach (var ur in userRoleRepo.GetAll())
            {
                if (excludeUserId.HasValue && ur.UserId == excludeUserId.Value) continue;
                if (roleLevels.TryGetValue(ur.RoleId, out int level) && IsAdminLevel(level))
                    return true;
            }
            return false;
        }

        #endregion
    }

}
