using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models.Params;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    public enum RecipeEditState { Browsing, Editing, Creating }

    public partial class RecipeEditVM<TRecipe> : ViewModelBase where TRecipe : RecipeBase, new()
    {
        private readonly IRecipeService<TRecipe> recipeService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuthService authService;
        private readonly IUserSession userSession;
        private readonly Action<TRecipe> recipeSwitchedHandler;
        private readonly IAuditService auditService;

        /// <summary>
        /// 进入编辑态时的配方快照（明细 diff 的基线；新建 / 浏览态为 null）。
        ///
        /// 必须快照：本页编辑态直接改 SelectedRecipe 实例（无草稿副本），到 Save 那刻已无处取旧值。
        /// 类型为不透明句柄——**不可用 ConfigBase.Clone&lt;T&gt;()**：它走裸 JsonConvert
        /// （ObjectCreationHandling.Auto），会把 RecipeBase 构造函数预置的 6 个参数**追加**到克隆体
        /// （快照变 12 项），基线一错，明细就刷出 40+ 条假"删除"。
        /// 统一走 <see cref="IAuditService.Snapshot"/>（只序列化、不实例化目标类型）。
        /// </summary>
        private AuditBaseline editSnapshot;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(SelectedName))]
        [NotifyPropertyChangedFor(nameof(SelectedModifiedBy))]
        [NotifyPropertyChangedFor(nameof(SelectedModifiedTime))]
        [NotifyPropertyChangedFor(nameof(SelectedComment))]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        [NotifyPropertyChangedFor(nameof(CanExport))]
        [NotifyPropertyChangedFor(nameof(CanSwitch))]
        private TRecipe selectedRecipe;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsBrowsing))]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(IsCreating))]
        [NotifyPropertyChangedFor(nameof(CanNew))]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanCancel))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        [NotifyPropertyChangedFor(nameof(CanImport))]
        [NotifyPropertyChangedFor(nameof(CanExport))]
        [NotifyPropertyChangedFor(nameof(CanSwitch))]
        private RecipeEditState state = RecipeEditState.Browsing;

        public bool IsBrowsing => State == RecipeEditState.Browsing;
        public bool IsEditing  => State == RecipeEditState.Editing;
        public bool IsCreating => State == RecipeEditState.Creating;

        public BindingList<TRecipe> RecipeList { get; } = new BindingList<TRecipe>();
        public BindingList<RecipeParameter> SelectedParams { get; } = new BindingList<RecipeParameter>();
        public TRecipe CurrentRecipe => recipeService.CurrentRecipe;

        public bool CanNew    => IsBrowsing;
        public bool CanEdit   => IsBrowsing && SelectedRecipe != null;
        public bool CanSave   => IsEditing || IsCreating;
        public bool CanCancel => IsEditing || IsCreating;
        public bool CanDelete => IsBrowsing && SelectedRecipe != null
            && SelectedRecipe.RecipeName != recipeService.CurrentRecipe?.RecipeName;
        public bool CanImport => IsBrowsing;
        public bool CanExport => IsBrowsing && SelectedRecipe != null;
        public bool CanSwitch => IsBrowsing && SelectedRecipe != null
            && SelectedRecipe.RecipeName != recipeService.CurrentRecipe?.RecipeName;

        // ── 元数据平面属性 ──
        public string SelectedName
        {
            get => SelectedRecipe?.RecipeName ?? "";
            set { if (SelectedRecipe != null) SelectedRecipe.RecipeName = value; }
        }
        public string SelectedModifiedBy
        {
            get => SelectedRecipe?.ModifiedBy ?? "";
            set { if (SelectedRecipe != null) SelectedRecipe.ModifiedBy = value; }
        }
        public string SelectedModifiedTime => SelectedRecipe?.ModifiedTime.ToString("yyyy-MM-dd HH:mm");
        public string SelectedComment
        {
            get => SelectedRecipe?.Comment ?? "";
            set { if (SelectedRecipe != null) SelectedRecipe.Comment = value; }
        }
        public string SelectedCurrentName => CurrentRecipe?.RecipeName ?? "—";

        public RecipeEditVM(IRecipeService<TRecipe> recipeService, IMessageBoxService messageBoxService,
            IAuthService authService, IUserSession userSession, IAuditService auditService)
        {
            this.recipeService = recipeService ?? throw new ArgumentNullException(nameof(recipeService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            recipeSwitchedHandler = _ => Refresh();
            recipeService.OnRecipeSwitched += recipeSwitchedHandler;
            SyncRecipeList();
            if (RecipeList.Count > 0)
                SelectedRecipe = CurrentRecipe ?? RecipeList[0];
        }

        // ── 命令 ──

        [RelayCommand(CanExecute = nameof(CanNew))]
        private void New()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_New))
            { messageBoxService.ShowError("权限不足"); return; }
            string name = "新配方_" + DateTime.Now.ToString("yyyyMMddHHmmss");
            try
            {
                var recipe = recipeService.CreateNewRecipe(name);
                recipe.ModifiedBy = userSession.CurrentUser?.Username ?? "";
                SyncRecipeList();
                SelectedRecipe = recipe;
                State = RecipeEditState.Creating;
                editSnapshot = null; // 新建无旧值基线：Save 时明细留空（Target 的配方名已表达"新建了哪个"）
                // 不留痕（2026-09-20）：新建此刻只进内存不落盘，真正的账由 Save 记（Creating 态记 Add）
            }
            catch (Exception ex) { messageBoxService.ShowError($"新建失败:\n{ex}"); }
        }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void Edit()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_Edit))
            { messageBoxService.ShowError("权限不足"); return; }
            // 快照须在切状态之前取：本页编辑态直接改 SelectedRecipe 实例，不快照则 Save 时无旧值可比
            editSnapshot = auditService.Snapshot(SelectedRecipe);
            State = RecipeEditState.Editing;
            // 不留痕（2026-09-20）：进入编辑态此刻数据零变化，真正的账由 Save 记
        }

        [RelayCommand(CanExecute = nameof(CanSave))]
        private void Save()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_Save))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRecipe == null) return;
            // 新建/编辑在**落盘时**区分动词（对齐角色页 isNew 模式）：Creating 态保存 = 新增一条配方，
            // 记 Add 而非 Save——"新增"的账在这里落，而不是在进编辑态时（后者不落盘，记了就是假账）
            bool isCreating = State == RecipeEditState.Creating;
            try
            {
                SelectedRecipe.ModifiedBy   = userSession.CurrentUser?.Username ?? "";
                SelectedRecipe.ModifiedTime = DateTime.Now;

                // 明细须在落盘前组装，且排除本方法刚写入的两个元数据字段——
                // 那是程序盖的章（与审计自身的操作者 / 时间冗余），不是操作者改的业务参数
                var changes = editSnapshot != null
                    ? auditService.BuildChanges(editSnapshot, SelectedRecipe,
                        nameof(RecipeBase.ModifiedBy), nameof(RecipeBase.ModifiedTime))
                    : null;

                recipeService.SaveRecipe(SelectedRecipe);
                State = RecipeEditState.Browsing;
                editSnapshot = null; // 基线随会话收敛，防下一轮误当旧值
                OnPropertyChanged(nameof(SelectedModifiedBy));
                OnPropertyChanged(nameof(SelectedModifiedTime));
                RecipeList.ResetBindings(); // 配方名可能已变更，强制 UI 刷新
                // 留痕须在业务写成功之后（铁律 1：宁漏记不记假账——Save 抛异常走 catch，不产生记录）
                auditService.Record(new AuditEntry(AuditCategory.Recipe,
                    isCreating ? AuditAction.Add : AuditAction.Save,
                    RecipeTarget(SelectedRecipe), userSession.CurrentUser.ToAuditActor())
                { Changes = changes });
            }
            catch (Exception ex) { messageBoxService.ShowError($"保存失败: {ex.Message}"); }
        }

        [RelayCommand(CanExecute = nameof(CanCancel))]
        private void Cancel()
        {
            if (State == RecipeEditState.Creating && SelectedRecipe != null)
                recipeService.DeleteRecipe(SelectedRecipe.RecipeName);
            // 放弃编辑：基线一并作废（Cancel 本身不留痕——存在切实例/关窗体等无命令的退出路径，
            // 记了覆盖不全，反而会让"有 Edit 无 Cancel"被误读成"没放弃"；宁漏记不记假账）
            editSnapshot = null;
            SelectedRecipe = recipeService.CurrentRecipe;
            SyncRecipeList();
            State = RecipeEditState.Browsing;
        }

        [RelayCommand(CanExecute = nameof(CanDelete))]
        private void Delete()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_Delete))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRecipe == null) return;
            if (!messageBoxService.ShowConfirm($"确定删除配方「{SelectedRecipe.RecipeName}」？"))
                return;
            try
            {
                string name = SelectedRecipe.RecipeName; // 删前取：随后 SelectedRecipe 置 null
                recipeService.DeleteRecipe(name);
                SelectedRecipe = null;
                Refresh();
                auditService.Record(new AuditEntry(AuditCategory.Recipe, AuditAction.Delete, name,
                    userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex) { messageBoxService.ShowError($"删除失败: {ex.Message}"); }
        }

        [RelayCommand(CanExecute = nameof(CanImport))]
        private void Import(string filePath)
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_Import))
            { messageBoxService.ShowError("权限不足"); return; }
            try
            {
                recipeService.ImportRecipe(filePath);
                SyncRecipeList();
                SelectedRecipe = recipeService.Recipes.LastOrDefault();
                messageBoxService.ShowInfo("导入成功");
                auditService.Record(new AuditEntry(AuditCategory.Recipe, AuditAction.Import, FileTarget(filePath),
                    userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex) { messageBoxService.ShowError($"导入失败: {ex.Message}"); }
        }

        [RelayCommand(CanExecute = nameof(CanExport))]
        private void Export(string filePath)
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_Export))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRecipe == null) return;
            try
            {
                recipeService.ExportRecipe(SelectedRecipe.RecipeName, filePath);
                messageBoxService.ShowInfo($"已导出到 {filePath}");
                auditService.Record(new AuditEntry(AuditCategory.Recipe, AuditAction.Export, FileTarget(filePath),
                    userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex) { messageBoxService.ShowError($"导出失败: {ex.Message}"); }
        }

        [RelayCommand(CanExecute = nameof(CanSwitch))]
        private void Switch()
        {
            if (!authService.HasPermission(PermissionCodes.BtnRecipe_Switch))
            { messageBoxService.ShowError("权限不足"); return; }
            if (SelectedRecipe == null) return;
            if (!messageBoxService.ShowConfirm($"确定要切换到配方「{SelectedRecipe.RecipeName}」吗？")) return;
            try
            {
                recipeService.SwitchRecipe(SelectedRecipe.RecipeName);
                // 留痕：切换配方 = 改现场生效对象，切错直接报废，属高危动作
                auditService.Record(new AuditEntry(AuditCategory.Recipe, AuditAction.Switch,
                    SelectedRecipe.RecipeName, userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex) { messageBoxService.ShowError($"切换失败: {ex.Message}"); }
        }

        // ── 事件 ──
        public event Action SelectionChanged;
        public event Action StateOrListChanged;

        partial void OnSelectedRecipeChanged(TRecipe value)
        {
            // 选中对象变了，编辑基线即作废（防下一轮 Save 把旧对象的快照当成新对象的旧值）
            editSnapshot = null;
            SyncSelectedParams();
            SelectionChanged?.Invoke();
        }
        partial void OnStateChanged(RecipeEditState value) => StateOrListChanged?.Invoke();

        private void Refresh()
        {
            SyncRecipeList();
            OnPropertyChanged(nameof(CurrentRecipe));
            OnPropertyChanged(nameof(SelectedCurrentName));
            OnPropertyChanged(nameof(CanDelete));
            OnPropertyChanged(nameof(CanSwitch));
            StateOrListChanged?.Invoke();
        }

        private void SyncRecipeList()
        {
            RecipeList.RaiseListChangedEvents = false;
            RecipeList.Clear();
            foreach (var r in recipeService.Recipes) RecipeList.Add(r);
            RecipeList.RaiseListChangedEvents = true;
            RecipeList.ResetBindings();
        }

        private void SyncSelectedParams()
        {
            SelectedParams.RaiseListChangedEvents = false;
            SelectedParams.Clear();
            if (SelectedRecipe?.Parameters != null)
                foreach (var p in SelectedRecipe.Parameters) SelectedParams.Add(p);
            SelectedParams.RaiseListChangedEvents = true;
            SelectedParams.ResetBindings();
        }

        protected override void OnDispose()
        {
            recipeService.OnRecipeSwitched -= recipeSwitchedHandler;
            SelectionChanged = null;
            StateOrListChanged = null;
        }

        #region 审计留痕

        /// <summary>配方类操作的目标名。</summary>
        private static string RecipeTarget(TRecipe recipe)
        {
            return recipe?.RecipeName ?? "(未命名)";
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
