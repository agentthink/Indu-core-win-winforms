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
    public partial class BaseSettingsVM : ViewModelBase
    {
        private readonly IConfigService configService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuthService authService;
        private readonly IUserSession userSession;
        private readonly IAuditService auditService;

        /// <summary>审计目标名（配置节是单例，没有"某一条"的概念——目标名即节名）</summary>
        private const string AuditTarget = "基础参数";

        [ObservableProperty]
        private BaseConfig config;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanCancel))]
        private bool isEditing;

        public bool IsDirty  => Config?.IsDirty ?? false;
        public bool CanEdit   => !IsEditing;
        public bool CanSave   => IsEditing;
        public bool CanCancel => IsEditing;

        public BaseSettingsVM(IConfigService configService, IMessageBoxService messageBoxService,
            IAuthService authService, IUserSession userSession, IAuditService auditService)
        {
            this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            Config = configService.Get<BaseConfig>();
        }

        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void Edit()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamBase_Edit))
            { messageBoxService.ShowError("权限不足"); return; }
            // 克隆草稿副本：编辑只改草稿，缓存单例不被污染，未保存不生效
            Config = configService.Get<BaseConfig>().Clone<BaseConfig>();
            IsEditing = true;
            // 不留痕（2026-09-20）：进入编辑态此刻数据零变化，真正的账由 Save 记
        }

        [RelayCommand]
        private void Save()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamBase_Save))
            { messageBoxService.ShowError("权限不足"); return; }
            if (Config?.IsDirty != true) { IsEditing = false; return; }
            try
            {
                var cached = configService.Get<BaseConfig>();
                // 明细必须在 CopyTo 之前取：CopyTo 是原地覆盖，之后 cached 即等于草稿、旧值就此丢失
                var changes = auditService.BuildChanges(cached, Config);
                // 草稿提交到缓存单例：保存才生效
                Config.CopyTo(cached);
                configService.Save<BaseConfig>();
                Config = cached;   // 界面指回缓存单例，回到浏览态
                IsEditing = false;
                OnPropertyChanged(nameof(IsDirty));
                // 留痕须在业务写成功之后（铁律 1：宁漏记不记假账——Save 抛异常走 catch，不产生记录）
                auditService.Record(new AuditEntry(AuditCategory.Config, AuditAction.Save, AuditTarget,
                    userSession.CurrentUser.ToAuditActor()) { Changes = changes });
                messageBoxService.ShowWarning("基础参数已保存，重启软件后生效", false, 3000);
            }
            catch (Exception ex) { messageBoxService.ShowError($"保存基础参数失败: {ex.Message}"); }
        }

        [RelayCommand]
        private void Cancel()
        {
            if (IsDirty && !messageBoxService.ShowConfirm("未保存的修改将丢失，确定取消？"))
                return;
            // 丢弃草稿，缓存单例从头到尾没被动过；不调 Reload（会换实例）
            Config = configService.Get<BaseConfig>();
            IsEditing = false;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// 将基础参数恢复为默认值（仅填充界面，需点保存才落盘）。
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsEditing))]
        private void Reset()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamBase_Reset))
            { messageBoxService.ShowError("权限不足"); return; }
            if (!messageBoxService.ShowConfirm("确定要将基础参数恢复为默认值吗？")) return;
            var defaults = new BaseConfig();
            defaults.SetDirty();
            // 草稿回默认值，仅填充界面；不调 Replace（会换实例）
            Config = defaults;
            OnPropertyChanged(nameof(IsDirty));
            // 留痕：重置只填界面、不落盘（真正后果在随后的 Save），明细留空只记动作本身
            auditService.Record(new AuditEntry(AuditCategory.Config, AuditAction.Reset, AuditTarget,
                userSession.CurrentUser.ToAuditActor()));
        }
    }
}
