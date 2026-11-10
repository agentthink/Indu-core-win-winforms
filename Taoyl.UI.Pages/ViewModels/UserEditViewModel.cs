using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Linq;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 用户编辑对话框 ViewModel（RBAC 多角色版）
    /// 负责编辑窗体的数据验证和保存逻辑
    /// </summary>
    public partial class UserEditViewModel : ViewModelBase
    {
        private readonly IUserSession userSession;
        private readonly IUserManager userManager;
        private readonly IMessageBoxService messageBoxService;
        private readonly IRoleService roleService;

        [ObservableProperty]
        private UserDto originalUser;

        [ObservableProperty]
        private bool isNewUser;

        /// <summary>
        /// 当前登录用户可分配的角色列表（等级不高于当前用户）
        /// </summary>
        [ObservableProperty]
        private List<RoleDto> availableRoles = new List<RoleDto>();

        /// <summary>
        /// 已选中的角色 ID 列表
        /// </summary>
        [ObservableProperty]
        private List<int> selectedRoleIds = new List<int>();

        /// <summary>
        /// 当前登录用户的权限等级（供 UI 过滤用）
        /// </summary>
        public int CurrentLoginLevel => userSession.CurrentLevel;

        public UserEditViewModel(
            IUserSession userSession,
            IUserManager userManager,
            IMessageBoxService messageBoxService,
            IRoleService roleService)
        {
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
        }

        /// <summary>
        /// 初始化运行时状态（替代构造函数传参）
        /// </summary>
        public void Initialize(UserDto editingUser, bool isNew)
        {
            IsNewUser = isNew;
            OriginalUser = editingUser ?? new UserDto();

            // 加载可用角色（排除高于当前用户等级的）
            int currentLevel = userSession.CurrentLevel;
            AvailableRoles = roleService.GetAllRoles()
                .Where(r => r.Level <= currentLevel)
                .OrderBy(r => r.Level)
                .ToList();

            // 加载用户已有角色
            if (!isNew && editingUser != null)
            {
                SelectedRoleIds = userManager.GetUserRoles(editingUser.UserId)
                    .Select(r => r.Id)
                    .ToList();
            }
            else
            {
                SelectedRoleIds = new List<int>();
            }
        }

        /// <summary>
        /// 保存用户（由 View 调用）。密码经 user.Password 传入：新增必填，编辑留空表示保留原密码。
        /// </summary>
        public bool SaveUser(UserDto user, List<int> roleIds, out string errorMsg)
        {
            errorMsg = null;

            if (string.IsNullOrWhiteSpace(user.Username))
            {
                errorMsg = "用户名不能为空";
                return false;
            }

            // 校验：不能分配高于自己等级的角色
            int currentLevel = userSession.CurrentLevel;
            foreach (var roleId in roleIds)
            {
                var role = AvailableRoles.FirstOrDefault(r => r.Id == roleId);
                if (role != null && role.Level > currentLevel)
                {
                    errorMsg = $"不能分配等级高于当前用户的角色: {role.RoleName}";
                    return false;
                }
            }

            // 新增时密码必填（编辑留空 = 保留原密码）
            if (IsNewUser && string.IsNullOrWhiteSpace(user.Password))
            {
                errorMsg = "新增用户时密码不能为空";
                return false;
            }

            // ========== 执行保存（非原子，需明确阶段错误） ==========
            bool success = IsNewUser
                ? userManager.AddUser(user, out errorMsg)
                : userManager.UpdateUser(user, out errorMsg);
            if (!success) return false;

            // 分配角色（使用服务回填的 UserId）
            if (!userManager.AssignRolesToUser(user.UserId, roleIds, out string roleError))
            {
                errorMsg += $"成功,但角色分配失败: {roleError}";
                return false;
            }
            // 如果编辑的是当前登录用户，统一刷新一次（只触发一次 UserChanged）
            if (user.UserId == userSession.CurrentUser?.UserId)
            {
                userSession.ReloadCurrentUser();
            }

            return true;

        }
    }
}
