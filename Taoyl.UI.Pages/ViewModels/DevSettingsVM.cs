using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 系统参数配置 ViewModel，管理 DevConfig 的浏览/编辑状态机和保存/取消逻辑。
    /// </summary>
    public partial class DevSettingsVM : ViewModelBase
    {
        #region 服务依赖

        private readonly IConfigService configService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuthService authService;
        private readonly IUserSession userSession;
        private readonly IAuditService auditService;

        /// <summary>审计目标名（配置节是单例，没有"某一条"的概念——目标名即节名）</summary>
        private const string AuditTarget = "系统参数";

        #endregion

        #region 可观察属性

        [ObservableProperty]
        private DevConfig config;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanCancel))]
        private bool isEditing;

        #endregion

        #region 计算属性

        /// <summary>是否有未保存修改。</summary>
        public bool IsDirty => Config?.IsDirty ?? false;

        /// <summary>浏览态可用。</summary>
        public bool CanEdit  => !IsEditing;

        /// <summary>编辑态可用。</summary>
        public bool CanSave  => IsEditing;

        /// <summary>编辑态可用。</summary>
        public bool CanCancel => IsEditing;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造系统参数 ViewModel，从 ConfigService 加载当前配置。
        /// </summary>
        /// <param name="configService">配置管理服务</param>
        public DevSettingsVM(IConfigService configService, IMessageBoxService messageBoxService,
            IAuthService authService, IUserSession userSession, IAuditService auditService)
        {
            this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            Config = configService.Get<DevConfig>();
        }

        #endregion

        #region 命令

        /// <summary>
        /// 进入编辑态，允许修改配置字段。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void Edit()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamSystem_Edit))
            { messageBoxService.ShowError("权限不足"); return; }
            // 克隆草稿副本：编辑只改草稿，缓存单例（窄接口读的就是它）不被污染，未保存不生效
            Config = configService.Get<DevConfig>().Clone<DevConfig>();
            IsEditing = true;
            // 不留痕（2026-09-20）：进入编辑态此刻数据零变化，真正的账由 Save 记
        }

        /// <summary>
        /// 保存当前配置到文件并退出编辑态。
        /// 内部检查 IsDirty，未修改则直接退出编辑态。
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamSystem_Save))
            { messageBoxService.ShowError("权限不足"); return; }
            if (Config?.IsDirty != true)
            {
                IsEditing = false;
                return;
            }

            try
            {
                var cached = configService.Get<DevConfig>();
                // 明细必须在 CopyTo 之前取：CopyTo 是原地覆盖，之后 cached 即等于草稿、旧值就此丢失
                var changes = auditService.BuildChanges(cached, Config);
                // 草稿提交到缓存单例：窄接口此刻才读到新值，实现"保存才生效"
                Config.CopyTo(cached);
                configService.Save<DevConfig>();
                Config = cached;   // 界面指回缓存单例，回到浏览态
                IsEditing = false;
                OnPropertyChanged(nameof(IsDirty));
                // 留痕须在业务写成功之后（铁律 1：宁漏记不记假账——Save 抛异常走 catch，不产生记录）
                auditService.Record(new AuditEntry(AuditCategory.Config, AuditAction.Save, AuditTarget,
                    userSession.CurrentUser.ToAuditActor()) { Changes = changes });
                messageBoxService.ShowWarning("系统参数已保存，刷卡与自动登出立即生效，其余需重启软件后生效", false, 3000);
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"保存系统参数失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            if (IsDirty && !messageBoxService.ShowConfirm("未保存的修改将丢失，确定取消？"))
                return;
            // 丢弃草稿，缓存单例从头到尾没被动过；不调 Reload（会换实例导致窄接口失效）
            Config = configService.Get<DevConfig>();
            IsEditing = false;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// 将系统参数恢复为默认值（仅填充界面，需点保存才落盘）。
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsEditing))]
        private void Reset()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamSystem_Reset))
            { messageBoxService.ShowError("权限不足"); return; }
            if (!messageBoxService.ShowConfirm("确定要将系统参数恢复为默认值吗？")) return;
            var defaults = new DevConfig();
            defaults.SetDirty();
            // 草稿回默认值，仅填充界面；不调 Replace（会换实例让窄接口指向旧对象）
            Config = defaults;
            OnPropertyChanged(nameof(IsDirty));
            // 留痕：重置只填界面、不落盘（真正后果在随后的 Save），明细留空只记动作本身
            auditService.Record(new AuditEntry(AuditCategory.Config, AuditAction.Reset, AuditTarget,
                userSession.CurrentUser.ToAuditActor()));
        }

        #endregion
    }
}
