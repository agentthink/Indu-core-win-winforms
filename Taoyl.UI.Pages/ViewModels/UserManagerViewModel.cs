using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.Share.Auth;
using Taoyl.Application.Share.Paths;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 用户管理 ViewModel
    /// </summary>
    public partial class UserManagerViewModel : ViewModelBase
    {
        private readonly IUserManager userManager;
        private readonly IRbacMigrationService rbacMigrationService;
        private readonly IUserSession userSession;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuditService auditService;
        private readonly SynchronizationContext uiContext;

        #region 可观察属性

        /// <summary>
        /// 用户列表（WinForms 最优绑定：BindingList）
        /// </summary>
        [ObservableProperty]
        private BindingList<UserDto> users = new BindingList<UserDto>();

        /// <summary>
        /// 当前选中用户
        /// 变更时自动联动 CanEditUser / CanDeleteUser
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditUser))]
        [NotifyPropertyChangedFor(nameof(CanDeleteUser))]
        private UserDto selectedUser;

        #endregion

        #region 权限控制属性（供 View 绑定）

        /// <summary>
        /// 是否允许编辑当前选中用户
        /// 规则：可编辑权限低于或等于自己的用户，或编辑自己（保存时拦截调高等级）
        /// </summary>
        public bool CanEditUser => CanEdit();

        /// <summary>
        /// 是否允许删除当前选中用户
        /// 规则：只能删除权限严格低于或等于自己的用户（不能删自己）
        /// </summary>
        public bool CanDeleteUser => CanDelete();

        #endregion

        #region 委托（View 层实现，VM 零耦合）

        /// <summary>
        /// 调用方：请显示用户编辑对话框，返回是否保存成功
        /// </summary>
        public Func<UserDto, bool> ShowEditDialog { get; set; }

        #endregion

        public UserManagerViewModel(
            IUserManager userManager,
            IRbacMigrationService rbacMigrationService,
            IUserSession userSession,
            IAuthService authService,
            IMessageBoxService messageBoxService,
            IAuditService auditService)
        {
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.rbacMigrationService = rbacMigrationService ?? throw new ArgumentNullException(nameof(rbacMigrationService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this.uiContext = SynchronizationContext.Current; // 捕获 UI 线程上下文
            LoadUsers();
        }

        #region 命令

        [RelayCommand]
        private void LoadUsers()
        {
            try
            {
                SelectedUser = null;

                var list = userManager.GetAllUsers();


                // 封送到 UI 线程更新 BindingList
                void UpdateList()
                {
                    SelectedUser = null;
                    Users.RaiseListChangedEvents = false;      // 保持 BindingList 实例不变，批量更新避免闪烁与滚动条重置
                    Users.Clear();
                    foreach (var user in list) Users.Add(user);
                    Users.RaiseListChangedEvents = true;
                    Users.ResetBindings();       // 触发 ListChangedType.Reset，DataGridView 平滑刷新
                }

                if (uiContext != null && uiContext != SynchronizationContext.Current)
                    uiContext.Post(_ => UpdateList(), null);
                else
                    UpdateList();

            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"加载用户数据失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void AddUser()
        {
            if (!authService.HasPermission(PermissionCodes.BtnUserManagerAdd))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            // 委托只回 bool（View 层对话框不向 VM 暴露"填了什么"），故用前后差集反推新建出来的那一行
            var beforeIds = new HashSet<int>(userManager.GetAllUsers().Select(u => u.UserId));

            if (ShowEditDialog?.Invoke(null) != true) return;

            LoadUsers();

            var created = userManager.GetAllUsers().FirstOrDefault(u => !beforeIds.Contains(u.UserId));
            if (created != null)
                auditService.Record(new AuditEntry(AuditCategory.User, AuditAction.Add, created.Username,
                    userSession.CurrentUser.ToAuditActor()));
        }

        [RelayCommand(CanExecute = nameof(CanEditUser))]
        private void EditUser()
        {
            if (!authService.HasPermission(PermissionCodes.BtnUserManagerEdit))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }
            if (SelectedUser == null) return;

            // 必须先克隆快照：保存走 View 层对话框并直接落库，不克隆则返回后 SelectedUser 已是新值、旧值无从取
            var before = CloneUser(SelectedUser);
            // 角色另取字符串快照：UserDto.Roles 带 [JsonIgnore]，走 JSON 快照会被整个丢掉
            // ⇒ 改角色是本页最该留痕的动作，却恰恰是通用 diff 的盲区（现场验证时只有一条空明细，原因在此）
            var beforeRoles = RoleNamesOf(SelectedUser);

            if (ShowEditDialog?.Invoke(SelectedUser) != true) return;

            LoadUsers();

            // 主键不变，按 UserId 回查保存后的值；Target 取**旧用户名**（同一人的历史记录连成一条线，改名本身进明细）
            var after = userManager.GetAllUsers().FirstOrDefault(u => u.UserId == before.UserId);
            if (after == null) return;

            // 最后登录时间是登录域自己写的，不是本次编辑的意图——排除，避免每次改用户都带一条无关明细
            var changes = auditService.BuildChanges(before, after, nameof(UserDto.LastLoginTime));

            // 角色差异单独**逐项**展开（2026-09-20：每个角色各占一条明细，读者逐条可核）
            changes.AddRange(auditService.BuildSetChanges("角色", beforeRoles, RoleNamesOf(after)));

            auditService.Record(new AuditEntry(AuditCategory.User, AuditAction.Update,
                before.Username, userSession.CurrentUser.ToAuditActor()) { Changes = changes });
        }

        [RelayCommand(CanExecute = nameof(CanDeleteUser))]
        private void DeleteUser()
        {
            if (!authService.HasPermission(PermissionCodes.BtnUserManagerDelete))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }
            if (SelectedUser == null) return;

            var userToDelete = SelectedUser;
            if (!messageBoxService.ShowConfirm($"确定要删除用户 \"{userToDelete.Username}\" 吗？\n此操作不可撤销！"))
                return;

            try
            {
                SelectedUser = null; // 提前置空，避免删除后选中状态异常

                if (userManager.DeleteUser(userToDelete.UserId, out string errorMsg))
                {
                    messageBoxService.ShowSuccess($"用户 \"{userToDelete.Username}\" 已删除", false, 500);
                    LoadUsers();
                    // 留痕：删除成功才投递（失败走 else 分支，不产生记录）
                    auditService.Record(new AuditEntry(AuditCategory.User, AuditAction.Delete, userToDelete.Username,
                        userSession.CurrentUser.ToAuditActor()));
                }
                else
                {
                    messageBoxService.ShowError($"删除失败: {errorMsg}");
                }
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"删除用户时发生异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 导入用户（由 View 弹窗后传入文件路径执行）
        /// </summary>
        [RelayCommand]
        private void ImportUsers(string filePath)
        {
            if (!authService.HasPermission(PermissionCodes.BtnUserManagerImport))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            if (string.IsNullOrWhiteSpace(filePath)) return;

            if (!messageBoxService.ShowConfirm(
        $"确定要导入配置吗？\n\n" +
        $"• 此操作将覆盖本机所有用户、角色及权限配置\n" +
        $"• 原有配置已自动备份至 {ConfigPathName.AuthBackupDir} 目录\n" +
        $"• 导入完成后需重新登录\n\n" +
        $"是否继续？",
        "危险操作确认"))
                return;

            bool success = rbacMigrationService.ImportUsers(filePath, out string errorMsg);
            if (success)
            {
                messageBoxService.ShowSuccess("导入成功，请重新登录\n"+ errorMsg);
                // 留痕：导入 = 覆盖本机全部用户/角色/权限（Target 取文件名，全路径会被 Target 列截断）
                auditService.Record(new AuditEntry(AuditCategory.User, AuditAction.Import, FileTarget(filePath),
                    userSession.CurrentUser.ToAuditActor()));
            }
            else
            {
                messageBoxService.ShowError(errorMsg);
            }
        }

        /// <summary>
        /// 导出用户（由 View 弹窗后传入文件路径执行）
        /// </summary>
        [RelayCommand]
        private void ExportUsers(string filePath)
        {
            if (!authService.HasPermission(PermissionCodes.BtnUserManagerExport))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }
            if (string.IsNullOrWhiteSpace(filePath)) return;

            bool success = rbacMigrationService.ExportUsers(filePath);
            if (success)
            {
                messageBoxService.ShowSuccess("导出成功", false, 500);
                // 留痕：数据出库是受控动作
                auditService.Record(new AuditEntry(AuditCategory.User, AuditAction.Export, FileTarget(filePath),
                    userSession.CurrentUser.ToAuditActor()));
            }
            else
                messageBoxService.ShowError("导出失败，请检查文件路径");
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 编辑完成后刷新
        /// </summary>
        public void RefreshAfterEdit()
        {
            LoadUsers();
        }


        #endregion

        #region 私有权限计算

        private bool CanEdit()
        {
            if (SelectedUser == null) return false;

            int currentLevel = userSession.CurrentLevel;
            int currentUserId = (int)(userSession.CurrentUser?.UserId);

            // 可编辑权限小于等于自己的用户
            if (SelectedUser.MaxRoleLevel <= currentLevel) return true;

            // 可编辑自己（等级调高由 SaveUser 拦截）
            if (SelectedUser.UserId == currentUserId) return true;

            return false;
        }

        private bool CanDelete()
        {
            if (SelectedUser == null) return false;

            int currentLevel = userSession.CurrentLevel;
            int currentUserId = (int)(userSession.CurrentUser?.UserId);

            // 只能删除权限小于等于自己的，且不能删除自己
            return SelectedUser.MaxRoleLevel <= currentLevel
                && SelectedUser.UserId != currentUserId;
        }

        #endregion

        #region 审计留痕

        /// <summary>
        /// 用户字段快照（编辑前基线用）。
        /// 没走配置模型的 Clone：UserDto 不继承 ConfigBase。
        /// ⚠️ Roles 标了 [JsonIgnore]，本方法**会丢掉角色**——角色必须另由 <see cref="RoleNamesOf"/> 单独取，
        /// 且两者都要在对话框弹出**之前**取完（对话框直接改 SelectedUser 并落库，之后再取已是新值）。
        /// </summary>
        private static UserDto CloneUser(UserDto user)
        {
            var json = JsonConvert.SerializeObject(user);
            return JsonConvert.DeserializeObject<UserDto>(json);
        }

        /// <summary>
        /// 角色名集合快照（取 RoleName 不取 Id：Id 是库内自增，名字才是给人看的审计事实）。
        ///
        /// 存在的唯一理由：把"改了角色"从 [JsonIgnore] 的盲区里捞回来——
        /// 用户管理页最该留痕的动作就是角色分配，此前它恰恰是零明细。
        /// </summary>
        private static List<string> RoleNamesOf(UserDto user)
        {
            if (user?.Roles == null) return new List<string>();

            return user.Roles
                       .Select(r => r.RoleName)
                       .Where(name => !string.IsNullOrWhiteSpace(name))
                       .ToList();
        }

        /// <summary>
        /// 文件类操作（导入 / 导出）的目标名：取纯文件名。
        /// 不取全路径——Target 列有 100 字上限（AuditService.Truncate），全路径极易被截断成半截脏数据。
        /// </summary>
        private static string FileTarget(string filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? "(未指定)" : Path.GetFileName(filePath);
        }

        #endregion
    }
}