using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 权限树节点 VM 模型（纯数据，无 UI 依赖）
    /// </summary>
    public class PermissionNode
    {
        public int Id { get; set; }
        public string Code { get; set; }
        public string Name { get; set; }
        public bool IsChecked { get; set; }
        public bool IsAutoInherited { get; set; }
        public List<PermissionNode> Children { get; set; } = new List<PermissionNode>();

        /// <summary>父节点引用（级联勾选用，不序列化）</summary>
        [Newtonsoft.Json.JsonIgnore]
        public PermissionNode Parent { get; set; }
    }

    /// <summary>
    /// 角色管理 ViewModel
    /// </summary>
    public partial class RoleManagerViewModel : ViewModelBase
    {
        private readonly IRoleService roleService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuthService authService;
        private readonly IUserSession userSession;
        private readonly IUserManager userManager;
        private readonly IPermissionService permissionService;
        private readonly IAuditService auditService;
        private readonly SynchronizationContext uiContext;

        /// <summary>
        /// 选中/新增角色那一刻的基线快照（基本信息 diff 的 before；选中清空时为 null）。
        ///
        /// 为什么在 OnSelectedRoleChanged 取而不在 SaveRole 取：本页编辑态直接改 SelectedRole 实例
        /// （无草稿副本），且 View 在调 SaveRole 前会先就地写回面板值（FormRoleManager.SyncEditPanelToViewModel）
        /// ——SaveRole 里再取基线取到的已是新值，diff 恒空（改等级不出明细的根因，2026-09-20 Bug A 修复）。
        /// 选中/新增那一刻取快照，永远早于任何写回；模式对齐 RecipeEditVM.editSnapshot。
        /// 类型为不透明句柄——**不可用 ConfigBase.Clone&lt;T&gt;()**（ObjectCreationHandling.Auto 会把
        /// 预置集合追加翻倍），统一走 <see cref="IAuditService.Snapshot"/>（只序列化、不实例化目标类型）。
        /// </summary>
        private AuditBaseline editSnapshot;

        #region 可观察属性

        /// <summary>
        /// 权限树节点（供 View 渲染）
        /// </summary>
        [ObservableProperty]
        private List<PermissionNode> permissionNodes = new List<PermissionNode>();

        /// <summary>
        /// 角色列表（WinForms 最优绑定：BindingList）
        /// </summary>
        [ObservableProperty]
        private BindingList<RoleDto> roles = new BindingList<RoleDto>();

        /// <summary>
        /// 当前选中角色
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditRole))]
        [NotifyPropertyChangedFor(nameof(CanDeleteRole))]
        private RoleDto selectedRole;

        /// <summary>
        /// 已勾选的权限 ID 列表
        /// </summary>
        [ObservableProperty]
        private List<int> selectedPermissionIds = new List<int>();

        [ObservableProperty]
        private string currentUserRoleDisplay = string.Empty;

        /// <summary>
        /// 状态栏文本
        /// </summary>
        [ObservableProperty]
        private string statusText = "就绪";

        #endregion

        #region 选中变化钩子（partial 方法，生成器自动调用）

        partial void OnSelectedRoleChanged(RoleDto value)
        {
            // 选中/新增角色即取基线（AddRole 的 new RoleDto 也走这里——Save 时 diff 出"操作者相对模板填的字段"）；
            // LoadRoles 置空选中 → value = null → Snapshot 返回 null，基线随之收敛，不串账
            editSnapshot = auditService.Snapshot(value);
        }

        #endregion

        #region 权限控制属性（供 View 绑定）

        /// <summary>
        /// 是否允许保存 / 重置当前角色（有选中即可）。
        /// 内置角色同样允许保存——RoleName / Level 由输入框只读 + 服务层强制原值保证不可改，
        /// 但**手动权限分配必须可保存**（保存按钮不能因内置而禁用）。
        /// </summary>
        public bool CanEditRole => SelectedRole != null;

        /// <summary>
        /// 是否允许删除当前角色（内置角色不可删）
        /// </summary>
        public bool CanDeleteRole => SelectedRole != null && SelectedRole.IsBuiltIn == 0;

        #endregion

        public RoleManagerViewModel(
            IRoleService roleService,
            IMessageBoxService messageBoxService,
            IAuthService authService,
            IUserSession userSession,
            IUserManager userManager,
            IPermissionService permissionService,
            IAuditService auditService)
        {
            this.roleService = roleService ?? throw new ArgumentNullException(nameof(roleService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this.uiContext = SynchronizationContext.Current; //捕获UI线程
            userSession.UserChanged += _ => RefreshCurrentUserDisplay();
            RefreshCurrentUserDisplay();
        }

        #region 命令

        [RelayCommand]
        private void LoadRoles()
        {
            try
            {
                SelectedRole = null;

                var list = roleService.GetAllRoles();

                Roles.RaiseListChangedEvents = false;
                Roles.Clear();
                foreach (var r in list) Roles.Add(r);
                Roles.RaiseListChangedEvents = true;
                Roles.ResetBindings();

                UpdateStatus();
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"加载角色数据失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void AddRole()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRoleManager_Add))
            { messageBoxService.ShowError("权限不足"); return; }
            SelectedRole = new RoleDto { Level = 100 }; // 默认操作员等级
            SelectedPermissionIds = new List<int>();
            UpdateStatus();
            // 不留痕（2026-09-20）：新增此刻未落库，真正的账由 SaveRole 落库时记 Add
        }

        [RelayCommand(CanExecute = nameof(CanEditRole))]
        private void SaveRole()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRoleManager_Save))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRole == null) return;

            //  防御：先缓存用户勾选的手动权限，防止后续任何 UI 刷新覆盖
            var manualPermissionIds = SelectedPermissionIds?.ToList() ?? new List<int>();

            // 审计基线之一：手动权限集必须在 SyncManualPermissions 之前取——同步会就地改写，事后再取就只剩新值了。
            // 基本信息基线**不走此处快照**（2026-09-20 Bug A 修复）：View 在调本命令前会先就地写回
            // SelectedRole（FormRoleManager.SyncEditPanelToViewModel），此刻快照取到的已是新值、diff 恒空
            // ——基线改在选中/新增角色那一刻取（OnSelectedRoleChanged → editSnapshot），永远早于写回。
            bool isNew = SelectedRole.Id == 0;
            var beforeManualCodes = isNew ? new List<string>() : GetManualPermissionCodes(SelectedRole.Id);

            // 保存角色基本信息（角色名/等级校验与等级越权校验均在服务内）
            if (!roleService.SaveRole(SelectedRole, out string saveError))
            {
                messageBoxService.ShowError(saveError);
                return;
            }

            // 同步手动权限：只保存用户手动勾选的权限ID
            // 自动继承的权限由 PermissionService 实时计算，不落库、不干预
            permissionService.SyncManualPermissions(SelectedRole.Id, manualPermissionIds);

            messageBoxService.ShowSuccess("保存成功", false, 500);

            // 留痕：基本信息（角色名 / 等级 / 备注）逐字段 + 权限集合差异**逐项**展开
            //（2026-09-20：每个权限点各占一条明细，读者逐条可核；权限值取 Code 不取 int Id，
            // Id 换个库就没意义了）。Id 排除：新增时由 Add 回填，不是操作者填的，记出来只会是"Id 0 → 7"这种干扰行。
            var changes = auditService.BuildChanges(editSnapshot, SelectedRole, nameof(RoleDto.Id));
            changes.AddRange(auditService.BuildSetChanges("手动权限",
                beforeManualCodes, GetManualPermissionCodes(SelectedRole.Id)));

            // 角色不在 AuditAction.Save 注释列举的"整体落盘"四类内，按集合条目用 新建/修改 直接可辨
            auditService.Record(new AuditEntry(AuditCategory.Role,
                isNew ? AuditAction.Add : AuditAction.Update,
                SelectedRole.RoleName, userSession.CurrentUser.ToAuditActor()) { Changes = changes });

            LoadRolesCommand.Execute(null);
        }

        [RelayCommand(CanExecute = nameof(CanDeleteRole))]
        private void DeleteRole()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRoleManager_Delete))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRole == null || SelectedRole.IsBuiltIn == 1) return;

            // 1. 查询关联用户
            var linkedUsers = userManager.GetUsersByRoleId(SelectedRole.Id);

            // 2. 阻断：不能删除包含当前登录用户的角色
            if (linkedUsers.Any(u => u.UserId == userSession.CurrentUser?.UserId))
            {
                messageBoxService.ShowError("当前登录用户关联此角色，无法删除。请先切换账号或注销。");
                return;
            }

            // 3. 弹窗确认
            if (linkedUsers.Count > 0)
            {
                var userNames = string.Join(", ", linkedUsers.Select(u => u.Username));
                var msg = $"角色 \"{SelectedRole.RoleName}\" 关联以下用户：\n\n" +
                          $"{userNames}\n\n" +
                          $"删除后将解除这些用户的该角色（用户保留）。此操作不可恢复！\n是否继续？";
                if (!messageBoxService.ShowConfirm(msg, "危险操作确认")) return;
            }
            else
            {
                if (!messageBoxService.ShowConfirm($"确定删除角色 \"{SelectedRole.RoleName}\" 吗？\n删除后不可恢复。", "删除确认"))
                    return;
            }

            // 4. 执行删除：级联业务编排在 RoleService 内（先用户、清手动权限、后角色）
            try
            {
                if (!roleService.DeleteRole(SelectedRole.Id, out string delError))
                {
                    messageBoxService.ShowError(delError);
                    return;
                }

                messageBoxService.ShowSuccess("删除成功", false, 500);
                // 留痕：Target 须在 LoadRoles 之前取——它内部会把 SelectedRole 置 null
                auditService.Record(new AuditEntry(AuditCategory.Role, AuditAction.Delete,
                    SelectedRole.RoleName, userSession.CurrentUser.ToAuditActor()));
                LoadRolesCommand.Execute(null);
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"删除失败: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanEditRole))]
        private void ResetRolePermissions()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRoleManager_Reset))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRole == null) return;

            // 弹窗确认（VM层直接调用MessageBoxService，符合你现有架构）
            if (!messageBoxService.ShowConfirm(
                $"确定要重置角色「{SelectedRole.RoleName}」的手动权限吗？\n\n" +
                "• 重置后将清除所有手动分配的权限\n" +
                "• 只保留由角色等级自动继承的权限\n" +
                "• 此操作立即生效且不可撤销",
                "权限重置确认"))
                return;

            try
            {
                // 审计基线：必须在清空之前取（随后 SyncManualPermissions 把手动权限全删，旧值就没了）
                var beforeManualCodes = GetManualPermissionCodes(SelectedRole.Id);

                // 清空手动权限（传入空列表 = 全删）
                permissionService.SyncManualPermissions(SelectedRole.Id, new List<int>());

                // 重建权限树（自动继承的节点会重新计算为Checked；尾部自含 SelectedPermissionIds/StatusText 刷新，调用方不重复）
                BuildPermissionTree(SelectedRole.Id);

                messageBoxService.ShowSuccess("权限已重置为自动继承状态", false, 500);

                // 留痕：本页重置当场落库（与配置页"仅填界面"的重置不同），必记且带明细（逐项展开全为 Delete）
                var changes = auditService.BuildSetChanges("手动权限",
                    beforeManualCodes, new List<string>());
                auditService.Record(new AuditEntry(AuditCategory.Role, AuditAction.Reset,
                    SelectedRole.RoleName, userSession.CurrentUser.ToAuditActor()) { Changes = changes });
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"重置失败: {ex.Message}");
            }
        }

        #endregion

        #region 公共方法

        public void BuildPermissionTree(int roleId)
        {
            PermissionNodes = new List<PermissionNode>();
            if (roleId <= 0) return;

            var effectiveCodes = permissionService.GetRoleEffectivePermissionCodes(roleId);
            var manualIds = permissionService.GetRolePermissionIds(roleId).ToHashSet();
            var allPerms = permissionService.GetAllPermissions().Where(p => p.IsActive == 1).ToList();

            // 构建根级菜单
            var rootMenus = allPerms
                .Where(p => p.Type == 1 && string.IsNullOrEmpty(p.ParentCode))
                .OrderBy(p => p.Sort).ThenBy(p => p.Name);

            foreach (var root in rootMenus)
            {
                var node = CreateNode(root, effectiveCodes, manualIds);
                PermissionNodes.Add(node);
                BuildChildNodes(node, root.Code, allPerms, effectiveCodes, manualIds);
            }

            // 未挂载功能权限
            var orphaned = allPerms
                .Where(p => p.Type == 2 && !string.IsNullOrEmpty(p.ParentCode))
                .Where(p => !allPerms.Any(x => x.Code.Equals(p.ParentCode, StringComparison.OrdinalIgnoreCase)))
                .OrderBy(p => p.Name);

            if (orphaned.Any())
            {
                var orphanRoot = new PermissionNode
                {
                    Id = -1,
                    Name = "【未分类功能】",
                    Code = "Orphan",
                    IsAutoInherited = false,
                    IsChecked = false
                };
                foreach (var perm in orphaned)
                    orphanRoot.Children.Add(CreateNode(perm, effectiveCodes, manualIds));
                PermissionNodes.Add(orphanRoot);
            }

            SelectedPermissionIds = CollectManualIds(PermissionNodes);
            UpdateStatus();
        }

        private void BuildChildNodes(PermissionNode parent, string parentCode, List<PermissionDto> allPerms,
    HashSet<string> effectiveCodes, HashSet<int> manualIds)
        {
            var children = allPerms
                .Where(p => p.ParentCode != null &&
                            p.ParentCode.Equals(parentCode, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.Type)
                .ThenBy(p => p.Sort)
                .ThenBy(p => p.Name);

            foreach (var child in children)
            {
                var childNode = CreateNode(child, effectiveCodes, manualIds);
                childNode.Parent = parent;  // ← 建立双向引用
                parent.Children.Add(childNode);
                if (child.Type == 1)
                    BuildChildNodes(childNode, child.Code, allPerms, effectiveCodes, manualIds);
            }
        }

        private PermissionNode CreateNode(PermissionDto perm, HashSet<string> effectiveCodes, HashSet<int> manualIds)
        {
            bool hasPermission = effectiveCodes.Contains(perm.Code);
            bool isManual = manualIds.Contains(perm.Id);
            return new PermissionNode
            {
                Id = perm.Id,
                Code = perm.Code,
                Name = perm.Name,
                IsChecked = hasPermission,
                IsAutoInherited = hasPermission && !isManual
            };
        }

        // ========== 勾选变更处理（由 VM 负责业务逻辑） ==========

        public void OnNodeCheckedChanged(int nodeId, bool isChecked)
        {
            var node = FindNode(PermissionNodes, nodeId);
            if (node == null || node.IsAutoInherited) return;

            node.IsChecked = isChecked;

            // ========== 级联逻辑 ==========
            if (isChecked)
            {
                // 勾选子节点 → 自动勾选所有父节点（否则菜单显示不出来）
                var parent = node.Parent;
                while (parent != null)
                {
                    if (!parent.IsAutoInherited)
                        parent.IsChecked = true;
                    parent = parent.Parent;
                }
                // 勾选父节点 → 级联显式授予整棵子树（与取消方向对称，一键开整页）
                CascadeCheckChildren(node);
            }
            else
            {
                // 取消父节点 → 自动取消所有子节点（否则逻辑矛盾）
                CascadeUncheckChildren(node);
            }
            // ===================================

            SelectedPermissionIds = CollectManualIds(PermissionNodes);
            UpdateStatus();
        }

        private void CascadeUncheckChildren(PermissionNode node)
        {
            if (node == null) return;
            foreach (var child in node.Children)
            {
                if (!child.IsAutoInherited)
                {
                    child.IsChecked = false;
                    CascadeUncheckChildren(child);
                }
            }
        }

        /// <summary>
        /// 勾选级联：显式授予整棵子树（与取消方向对称；继承节点跳过，其授予态由等级实时计算）
        /// </summary>
        private void CascadeCheckChildren(PermissionNode node)
        {
            if (node == null) return;
            foreach (var child in node.Children)
            {
                if (!child.IsAutoInherited)
                {
                    child.IsChecked = true;
                    CascadeCheckChildren(child);
                }
            }
        }

        /// <summary>
        /// 根据Id查找权限节点（供View判断自动继承状态）
        /// </summary>
        public PermissionNode FindPermissionNode(int id)
        {
            return FindNode(PermissionNodes, id);
        }

        private PermissionNode FindNode(List<PermissionNode> nodes, int id)
        {
            foreach (var node in nodes)
            {
                if (node.Id == id) return node;
                var found = FindNode(node.Children, id);
                if (found != null) return found;
            }
            return null;
        }

        private List<int> CollectManualIds(List<PermissionNode> nodes)
        {
            var result = new List<int>();
            foreach (var node in nodes)
            {
                if (node.IsChecked && !node.IsAutoInherited)
                    result.Add(node.Id);
                result.AddRange(CollectManualIds(node.Children));
            }
            return result;
        }

        /// <summary>
        /// 更新状态栏文本
        /// </summary>
        public void UpdateStatus()
        {
            if (SelectedRole == null)
            {
                StatusText = "就绪";
                return;
            }
            StatusText = $"已选择角色「{SelectedRole.RoleName}」，共勾选 {SelectedPermissionIds.Count} 项权限。";
        }
        private void RefreshCurrentUserDisplay()
        {
            if (uiContext != null && uiContext != SynchronizationContext.Current)
            {
                uiContext.Post(_ => DoRefreshCurrentUserDisplay(), null);
                return;
            }
            DoRefreshCurrentUserDisplay();
        }

        private void DoRefreshCurrentUserDisplay()
        {
            var user = userSession.CurrentUser;
            CurrentUserRoleDisplay = user == null || user.MaxRoleLevel == 0
                ? "未登录"
                : $"{user.Username} ({user.RoleNamesDisplay})";
        }

        #endregion

        #region 审计留痕

        /// <summary>
        /// 角色的**手动**权限码集合（审计明细用）。
        /// 只取手动授予部分——自动继承由等级实时计算、不落库，不属于"谁改了什么"。
        /// 取码不取 Id：审计的读者要看得懂，而权限 Id 是自增整数、离开本库就没有意义。
        /// </summary>
        private List<string> GetManualPermissionCodes(int roleId)
        {
            if (roleId <= 0) return new List<string>();

            var manualIds = permissionService.GetRolePermissionIds(roleId);
            if (manualIds == null || manualIds.Count == 0) return new List<string>();

            var idSet = new HashSet<int>(manualIds);
            return permissionService.GetAllPermissions()
                .Where(p => idSet.Contains(p.Id))
                .Select(p => p.Code)
                .ToList();
        }

        #endregion
    }
}