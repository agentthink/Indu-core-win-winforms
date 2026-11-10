using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.IO;
using System.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.Application.Share.Logging;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 日志参数配置 ViewModel，管理 LogConfig 的浏览/编辑状态机和保存/取消/重置逻辑。
    /// 交互与系统参数/基础参数一致（同款状态机照抄）；差异点：
    /// ① 保存前 UI 校验（规则与 LogTargetConfigurator 启动校验逐条对齐，见 ValidateDraft）；
    /// ② 保存提示「重启生效」——LogConfig 任何改动一律进程重启生效（无热生效）。
    /// </summary>
    public partial class LogSettingsVM : ViewModelBase
    {
        #region 服务依赖

        private readonly IConfigService configService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuthService authService;
        private readonly IUserSession userSession;
        private readonly IAuditService auditService;

        /// <summary>审计目标名（配置节是单例，没有"某一条"的概念——目标名即节名）</summary>
        private const string AuditTarget = "日志参数";

        #endregion

        #region 可观察属性

        [ObservableProperty]
        private LogConfig config;

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
        /// 构造日志参数 ViewModel，从 ConfigService 加载当前配置。
        /// </summary>
        /// <param name="configService">配置管理服务</param>
        public LogSettingsVM(IConfigService configService, IMessageBoxService messageBoxService,
            IAuthService authService, IUserSession userSession, IAuditService auditService)
        {
            this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            Config = configService.Get<LogConfig>();
        }

        #endregion

        #region 命令

        /// <summary>
        /// 进入编辑态，允许修改配置字段。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEdit))]
        private void Edit()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamLog_Edit))
            { messageBoxService.ShowError("权限不足"); return; }
            // 克隆草稿副本：编辑只改草稿，缓存单例（窄接口读的就是它）不被污染，未保存不生效
            Config = configService.Get<LogConfig>().Clone<LogConfig>();
            IsEditing = true;
            // 不留痕（2026-09-20）：进入编辑态此刻数据零变化，真正的账由 Save 记
        }

        /// <summary>
        /// 校验并保存当前配置到文件并退出编辑态。
        /// 内部检查 IsDirty，未修改则直接退出编辑态。
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamLog_Save))
            { messageBoxService.ShowError("权限不足"); return; }
            if (Config?.IsDirty != true)
            {
                IsEditing = false;
                return;
            }

            // 保存前校验：非法值一旦落盘，重启即启动失败（LogTargetConfigurator 启动校验 fail fast），
            // 界面是最后一道闸，必须拦在写盘之前
            string error = ValidateDraft();
            if (error != null)
            {
                messageBoxService.ShowError(error);
                return;
            }

            try
            {
                var cached = configService.Get<LogConfig>();
                // 明细必须在 CopyTo 之前取：CopyTo 是原地覆盖，之后 cached 即等于草稿、旧值就此丢失
                var changes = auditService.BuildChanges(cached, Config);
                // 草稿提交到缓存单例：窄接口此刻才读到新值，实现"保存才生效"
                Config.CopyTo(cached);
                configService.Save<LogConfig>();
                Config = cached;   // 界面指回缓存单例，回到浏览态
                IsEditing = false;
                OnPropertyChanged(nameof(IsDirty));
                // 留痕须在业务写成功之后（铁律 1：宁漏记不记假账——Save 抛异常走 catch，不产生记录）
                auditService.Record(new AuditEntry(AuditCategory.Config, AuditAction.Save, AuditTarget,
                    userSession.CurrentUser.ToAuditActor()) { Changes = changes });
                messageBoxService.ShowWarning("日志参数已保存，重启软件后生效", false, 3000);
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"保存日志参数失败: {ex.Message}");
            }
        }

        [RelayCommand]
        private void Cancel()
        {
            if (IsDirty && !messageBoxService.ShowConfirm("未保存的修改将丢失，确定取消？"))
                return;
            // 丢弃草稿，缓存单例从头到尾没被动过；不调 Reload（会换实例导致窄接口失效）
            Config = configService.Get<LogConfig>();
            IsEditing = false;
            OnPropertyChanged(nameof(IsDirty));
        }

        /// <summary>
        /// 将日志参数恢复为默认值（仅填充界面，需点保存才落盘）。
        /// </summary>
        [RelayCommand(CanExecute = nameof(IsEditing))]
        private void Reset()
        {
            if (!authService.HasPermission(PermissionCodes.BtnParamLog_Reset))
            { messageBoxService.ShowError("权限不足"); return; }
            if (!messageBoxService.ShowConfirm("确定要将日志参数恢复为默认值吗？")) return;
            var defaults = new LogConfig();
            defaults.SetDirty();
            // 草稿回默认值，仅填充界面；不调 Replace（会换实例让窄接口指向旧对象）
            Config = defaults;
            OnPropertyChanged(nameof(IsDirty));
            // 留痕：重置只填界面、不落盘（真正后果在随后的 Save），明细留空只记动作本身
            auditService.Record(new AuditEntry(AuditCategory.Config, AuditAction.Reset, AuditTarget,
                userSession.CurrentUser.ToAuditActor()));
        }

        #endregion

        #region 保存前校验

        /// <summary>
        /// 保存前校验草稿，返回首个错误描述（null = 通过）。三条规则与
        /// LogTargetConfigurator 的启动校验逐条对齐（RootPath 非空 / KeepDays 为 -1 或 ≥ 1 /
        /// 前缀仅字母数字下划线）——两处独立维护、注释互指，不抽公共方法
        /// （UI 校验是体验层提前拦截，启动校验是 fail fast；单点消费不值得加基础设施）。
        /// 通道遍历走 Enum.GetValues：新增日志通道时自动跟随（与 LogForm 建环同款姿势）。
        /// </summary>
        private string ValidateDraft()
        {
            if (string.IsNullOrWhiteSpace(Config.RootPath))
                return "日志根目录不能为空";

            // 字符集黑名单：GetInvalidPathChars（" < > | + 控制符）+ * ?（目录段同样非法）。
            // 刻意不走 Path.GetFullPath 试转——4.6.2 起其对非法字符的检查存在 AppContext 开关歧义，
            // 黑名单是确定性 API，与启动侧行为解耦（启动侧另有 fail fast / L2 降级兜底）
            foreach (char c in Config.RootPath)
            {
                if (Array.IndexOf(Path.GetInvalidPathChars(), c) >= 0 || c == '*' || c == '?')
                    return $"日志根目录含非法字符 \"{c}\"";
            }

            foreach (var category in Enum.GetValues(typeof(LogCategory)).Cast<LogCategory>())
            {
                var policy = Config.GetPolicy(category);
                // -1 = 不清理是合法值；0 与小于 -1 无意义（0 会让清理器把今天之前的全删）
                if (policy.KeepDays == 0 || policy.KeepDays < -1)
                    return $"日志通道 {category} 的保留天数非法（-1 = 不清理，或至少 1 天）";

                // 前缀为空 = 回退通道名（与启动侧规则一致，放行）；非空逐字符校验
                string prefix = policy.FileNamePrefix;
                if (string.IsNullOrEmpty(prefix)) continue;
                foreach (char c in prefix)
                {
                    bool legal = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z')
                        || (c >= '0' && c <= '9') || c == '_';
                    if (!legal)
                        return $"日志通道 {category} 的文件名前缀含非法字符 \"{c}\"（仅允许字母/数字/下划线）";
                }
            }
            return null;
        }

        #endregion
    }
}
