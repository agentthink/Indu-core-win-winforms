using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Taoyl.Application.ProcessMonitoring;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Management;
using Taoyl.UI.FormMain.Models;
using Taoyl.UI.FormMain.Services;

namespace Taoyl.UI.FormMain.ViewModels
{
    /// <summary>
    /// 主窗体 ViewModel
    /// 
    /// 按钮职责严格分离：
    /// - 开始：Idle → Running（启动所有流程）
    /// - 暂停：Running → Paused（暂停所有流程）
    /// - 复位：Exception → Paused（仅消除异常，不自动继续）
    /// 
    /// 异常处理流程：
    /// 1. 流程异常 → 系统状态变 Exception
    /// 2. 操作员点击复位 → 异常清除，状态变 Paused
    /// 3. 操作员点击开始 → 状态变 Running，继续运行
    /// 
    /// 开始按钮语义：
    /// - Idle 时：启动
    /// - Paused 时：继续
    /// - Exception 时：禁用（必须先复位）
    /// </summary>
    public partial class MainViewModel : ViewModelBase
    {
        #region 依赖服务
        private readonly IAuthService authService;
        private readonly IUserSession userService;
        private readonly IMonitorService monitorService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IUserActivityMonitor activityMonitor;
        private readonly MenuManager menuManager;
        private readonly IConfigService configService;
        private readonly IAuditService auditService;
        private readonly SynchronizationContext uiContext;
        #endregion

        #region 可观察属性
        [ObservableProperty]
        private string statusText=string.Empty;

        [ObservableProperty]
        private string currentUserName = "未登录";

        [ObservableProperty]
        private string currentRoleName = "访客";

        [ObservableProperty]
        private string currentPageTitle = "主界面";

        [ObservableProperty]
        private string countdownText = string.Empty;

        [ObservableProperty]
        private bool isLoggedIn;

        [ObservableProperty]
        private string currentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        [ObservableProperty]
        private List<MenuItem> menuItems = new();

        [ObservableProperty]
        private MenuItem selectedMenuItem;

        [ObservableProperty]
        private string loginButtonText = "登录";

        [ObservableProperty]
        private System.Drawing.Color countdownForeColor = System.Drawing.Color.FromArgb(204, 212, 238);

        [ObservableProperty]
        private LanguageType currentLanguage = LanguageType.zh_CN;

        /// <summary>系统聚合状态（驱动按钮显示）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanStart))]
        [NotifyPropertyChangedFor(nameof(CanPause))]
        [NotifyPropertyChangedFor(nameof(CanReset))]
        private SystemProcessState systemState = SystemProcessState.Idle;
        #endregion

        #region 计算属性（按钮状态）

        /// <summary>
        /// 开始按钮可用条件：
        /// - 已登录
        /// - Idle：首次启动
        /// - Paused：继续运行
        /// - Exception：禁用（必须先点复位）
        /// </summary>
        public bool CanStart =>  SystemState is SystemProcessState.Idle or SystemProcessState.Paused;

        /// <summary>
        /// 暂停按钮可用条件：已登录且正在运行
        /// </summary>
        public bool CanPause =>  SystemState == SystemProcessState.Running;

        /// <summary>
        /// 复位按钮可用条件：已登录且有流程异常
        /// </summary>
        public bool CanReset =>  SystemState == SystemProcessState.Exception;

        public bool ShowLogout => IsLoggedIn;

        #endregion

        #region 事件
        public event Action ShowLoginDialogRequested;
        public event Action CloseWindowRequested;
        public event Action MinimizeWindowRequested;
        public event Action ToggleMaximizeWindowRequested;
        public event Action<MenuItem> NavigateToPageRequested;
        #endregion

        #region 构造函数
        public MainViewModel(
            IAuthService authService,
            IUserSession userService,
            IMonitorService monitorService,
            IMessageBoxService messageBoxService,
            IUserActivityMonitor activityMonitor,
            MenuManager menuManager,
            IConfigService configService,
            IAuditService auditService)
        {
            uiContext = SynchronizationContext.Current;
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.userService = userService ?? throw new ArgumentNullException(nameof(userService));
            this.monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.activityMonitor = activityMonitor ?? throw new ArgumentNullException(nameof(activityMonitor));
            this.menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
            this.configService = configService ?? throw new ArgumentNullException(nameof(configService));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

            this.userService.UserChanged += OnUserChanged;
            this.activityMonitor.CountdownUpdated += OnCountdownUpdated;
            this.activityMonitor.AutoLogoutRequired += OnAutoLogoutRequired;
        }
        #endregion

        #region 命令

        [RelayCommand]
        private void ShowLogin() => ShowLoginDialogRequested?.Invoke();

        [RelayCommand]
        private void Logout()
        {
            if (!messageBoxService.ShowConfirm("确定要注销当前用户吗？")) return;
            authService.Logout();
            messageBoxService.ShowSuccess("已注销", false, 500);
        }

        /// <summary>
        /// 运行按钮。统一走 StartProcess（StartAll 逐流程分流：待命/停止启动，暂停恢复，运行中忽略），
        /// 混合态（部分待命+部分暂停）无漏网。文案按聚合态区分：Paused 提示"已继续"，其余提示"已启动"。
        /// Running/Exception 态不动作（理论上被 CanStart 门控挡住；命令直调时不动作不留痕——宁漏记不记假账）。
        /// </summary>
        [RelayCommand]
        private void StartProcess()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessStart))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            // acted = 是否真的进了执行分支。置位放在调用**之前**：调用抛异常时也说明动作已发起，该记失败。
            // ⚠️ 无参重载返回 void（带 processName 的重载才有 bool）⇒ 成败只能靠 catch 判定。
            bool acted = false;
            try
            {
                if (SystemState == SystemProcessState.Idle || SystemState == SystemProcessState.Paused)
                {
                    acted = true;
                    monitorService.StartProcess();
                    messageBoxService.ShowSuccess(
                        SystemState == SystemProcessState.Paused ? "生产已继续" : "生产已启动", false, 500);

                    auditService.Record(new AuditEntry(AuditCategory.Main, AuditAction.Start,
                        BulkTarget, ToAuditActor(userService.CurrentUser)));
                }
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"操作失败: {ex.Message}");
                if (acted) auditService.Record(new AuditEntry(AuditCategory.Main, AuditAction.Start,
                    BulkTarget, ToAuditActor(userService.CurrentUser), AuditResult.Fail));
            }
        }

        /// <summary>
        /// 暂停按钮
        /// 仅 Running 时可用
        /// </summary>
        [RelayCommand]
        private void PauseProcess()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessPause))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            try
            {
                monitorService.PauseProcess();
                messageBoxService.ShowInfo("生产已暂停", false, 500);
                auditService.Record(new AuditEntry(AuditCategory.Main, AuditAction.Pause,
                    BulkTarget, ToAuditActor(userService.CurrentUser)));
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"暂停失败: {ex.Message}");
                auditService.Record(new AuditEntry(AuditCategory.Main, AuditAction.Pause,
                    BulkTarget, ToAuditActor(userService.CurrentUser), AuditResult.Fail));
            }
        }

        /// <summary>
        /// 复位按钮
        /// 仅 Exception 时可用
        /// 仅消除异常状态，不自动继续，等待人工点开始后继续
        /// </summary>
        [RelayCommand]
        private void ResetProcess()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessReset))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            try
            {
                monitorService.ResetProcess();
                messageBoxService.ShowInfo("已复位，请点击启动继续", false, 500);
                auditService.Record(new AuditEntry(AuditCategory.Main, AuditAction.ResetProcess,
                    BulkTarget, ToAuditActor(userService.CurrentUser)));
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"复位失败: {ex.Message}");
                auditService.Record(new AuditEntry(AuditCategory.Main, AuditAction.ResetProcess,
                    BulkTarget, ToAuditActor(userService.CurrentUser), AuditResult.Fail));
            }
        }

        [RelayCommand]
        private void CloseWindow()
        {
            if (!messageBoxService.ShowConfirm("确定要退出系统吗？")) return;
            CloseWindowRequested?.Invoke();
        }

        [RelayCommand]
        private void MinimizeWindow() => MinimizeWindowRequested?.Invoke();

        [RelayCommand]
        private void ToggleMaximize() => ToggleMaximizeWindowRequested?.Invoke();

        [RelayCommand]
        private void ShowHelp() => messageBoxService.ShowInfo("系统帮助文档\n版本: 2.0.1");

        [RelayCommand]
        private void SelectMenu(MenuItem menuItem)
        {
            if (menuItem == null) return;
            // 菜单已通过权限码过滤，此处保留二次校验（页面权限码）
            if (menuItem.IsFormNode() && !authService.HasPermission($"Page:{menuItem.Name}"))
            {
                messageBoxService.ShowError("没有权限访问此功能");
                return;
            }

            try
            {
                SelectedMenuItem = menuItem;
                if (!menuItem.IsFormNode()) return;

                CurrentPageTitle = menuItem.DisplayName;
                NavigateToPageRequested?.Invoke(menuItem);
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"导航失败:{ex.Message}");
            }
        }

        [RelayCommand]
        private void ChangeLanguage(LanguageType language)
        {
            CurrentLanguage = language;
            TransManager.Current.CurrentLanguage = language;
            SaveLanguageToConfig(language);
        }
        #endregion

        #region 公共方法

        public void OnViewLoaded()
        {
            CurrentLanguage = LoadLanguageFromConfig();
            TransManager.Current.CurrentLanguage = CurrentLanguage;
            TransManager.Current.UseRecommendedExclusions();
            LoadMenu();
            LoadDefaultPage();
            UpdateUserDisplay(userService.CurrentUser);

            
        }

        public void OnViewClosing()
        {
            activityMonitor.Stop();
        }

        public void UpdateTime() => CurrentTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        public void OnLoginSuccess(string username, string password)
        {
             messageBoxService.ShowSuccess($"欢迎 {userService.CurrentUser.Username}", false, 500);
        }
        #endregion

        #region 状态轮询

        

        /// <summary>
        /// 轮询系统状态，更新 UI 状态机
        /// </summary>
        public void PollSystemState()
        {
            try
            {
                var newSystemState = monitorService.GetSystemState();

                if (newSystemState != SystemState)
                {
                    // 聚合状态显示保留；异常弹窗已撤销（10-10 报警系统上线：弹窗唯一出口 =
                    // AlarmPopupPresenter 分通道弹窗，此处边沿弹窗与报警弹窗重复，只留状态赋值）
                    SystemState = newSystemState;
                }
            }
            catch { /* 轮询异常不抛，避免崩 UI */ }
        }
        #endregion

        #region 审计留痕

        // 页面维度固定 AuditCategory.Main：主界面是个独立页面，但它的三个按钮挂的权限码是
        // Btn:Process_* 而非 Page:*（不经页面导航，没有菜单节点），故只能按"主界面"这个页面名
        // 归属，不能靠页面码推导。投递点统一内联 auditService.Record(new AuditEntry(...))。

        /// <summary>
        /// 批量动作的操作对象名。主界面这三个按钮作用于**全部流程**，没有单点对象可作 Target。
        /// 明细留空——它们是动作型操作，本身不产生字段级变更（对比：流程监控页的启用/异停切换带明细）。
        /// </summary>
        private const string BulkTarget = "全部流程";

        /// <summary>
        /// 会话身份 → 审计身份快照的**适配**（与 UI.Pages 的 AuditActorExtensions 同构）。
        /// 🔴 快照语义：取**当下这一刻**的身份，取完即与 UserDto 脱钩——操作者日后改名 / 换角色，
        /// 历史记录仍反映操作当时的身份。null 或用户名为空白 = 未登录，归一为访客
        /// （归一规则与 AuditEntry 构造内的兜底一致，此处提前归一只是为省一次分配）。
        /// </summary>
        private  AuditActor ToAuditActor(UserDto user)
        {
            if (user == null || string.IsNullOrWhiteSpace(user.Username)) return AuditActor.Guest;

            var roles = user.Roles;
            string roleNames = roles == null || roles.Count == 0
                ? string.Empty
                : string.Join(",", roles.Where(r => r != null).Select(r => r.RoleName));

            return new AuditActor(user.Username, roleNames, user.MaxRoleLevel);
        }

        #endregion

        #region 私有方法

        private void LoadMenu() => MenuItems = menuManager.GetMenuItemsByPermission(userService.CurrentPermissionCodes);

        private void LoadDefaultPage()
        {
            var defaultMenu = menuManager.GetDefaultMenuItem(userService.CurrentPermissionCodes);
            if (defaultMenu == null) return;

            SelectedMenuItem = defaultMenu;
            CurrentPageTitle = defaultMenu.DisplayName;
            NavigateToPageRequested?.Invoke(defaultMenu);
        }

        private void OnUserChanged(UserDto user)
        {
            if (uiContext != null && uiContext != SynchronizationContext.Current)
            {
                uiContext.Post(_ => ExecuteUserChanged(user), null);
                return;
            }
            ExecuteUserChanged(user);
        }

        private void ExecuteUserChanged(UserDto user)
        {
            UpdateUserDisplay(user);
            if (IsLoggedIn)
            {
                activityMonitor.Start();
                OnCountdownUpdated(activityMonitor.GetRemainingSeconds());
            }
            else
            {
                activityMonitor.Stop();
                CountdownText = string.Empty;
            }
            LoadMenu();
            LoadDefaultPage();
        }

        private void UpdateUserDisplay(UserDto user)
        {
            IsLoggedIn = userService.IsLoggedIn;
            CurrentUserName = user?.Username ?? "未登录";
            CurrentRoleName = user?.RoleNamesDisplay ?? "访客";
            LoginButtonText = IsLoggedIn ? "切换用户" : "登录";
        }

        private void OnCountdownUpdated(int remainingSeconds)
        {
            if (!IsLoggedIn)
            {
                CountdownText = string.Empty;
                return;
            }

            CountdownForeColor = remainingSeconds <= 10
                ? System.Drawing.Color.Red
                : System.Drawing.Color.FromArgb(204, 212, 238);

            if (remainingSeconds > 1)
            {
                var time = TimeSpan.FromSeconds(remainingSeconds);
                var formatted = time.TotalHours >= 1
                    ? $"{time.Hours:D2}时:{time.Minutes:D2}分:{time.Seconds:D2}秒"
                    : $"00时:{time.Minutes:D2}分:{time.Seconds:D2}秒";
                CountdownText = $"注销倒计时:{formatted}";
            }
            else
            {
                CountdownText = "即将注销...";
            }
        }

        private void OnAutoLogoutRequired()
        {
            if (!IsLoggedIn) return;
            authService.Logout();
            messageBoxService.ShowWarning("由于长时间无操作，系统已自动注销", false, 500);
        }

        private LanguageType LoadLanguageFromConfig()
        {
            try
            {
                string lang = configService.Get<DevConfig>().UILanguage;
                if (Enum.TryParse(lang, out LanguageType language))
                    return language;
            }
            catch { }
            return LanguageType.zh_CN;
        }

        private void SaveLanguageToConfig(LanguageType language)
        {
            try
            {
                var dev = configService.Get<DevConfig>();
                dev.UILanguage = language.ToString();
                configService.Save<DevConfig>();
            }
            catch { }
        }
        #endregion

        #region 释放

        protected override void OnDispose()
        {
            userService.UserChanged -= OnUserChanged;
            activityMonitor.CountdownUpdated -= OnCountdownUpdated;
            activityMonitor.AutoLogoutRequired -= OnAutoLogoutRequired;
        }
        #endregion
    }
}