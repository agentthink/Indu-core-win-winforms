using Autofac;
using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.Share.Auth;
using Taoyl.Application.Share.Extensions;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Interfaces;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using MenuItem = Taoyl.UI.FormMain.Models.MenuItem;

namespace Taoyl.UI.FormMain
{
    /// <summary>
    /// 主窗体 — 纯View角色
    /// 调整：btnStart 三态（开始/暂停/继续），新增 btnReset 独立复位
    /// </summary>
    public partial class FormMain : UIForm
    {
        #region 依赖注入（Autofac属性注入）
        public MainViewModel ViewModel { get; set; }
        public MenuManager MenuManager { get; set; }
        public INavigationService NavigationService { get; set; }
        public IAuthService AuthService { get; set; }
        public IAuditService AuditService { get; set; }
        public IComponentContext ComponentContext { get; set; }
        public IMessageBoxService MessageBox { get; set; }
        public ILogService LogService { get; set; }
        public ITranslationService TranslationService { get; set; }
        #endregion

        #region 私有字段
        private readonly Timer statePollTimer = new Timer();            //启动状态轮询。每 500ms 检查一次系统状态
        private readonly Timer statusTimer = new Timer();
        private readonly ImageList menuIcons = new ImageList();
        private Form currentForm;
        #endregion

        #region 构造函数
        public FormMain()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            DpiZoomControl();
            ApplyNavMenuColors();
            
            AddMousePressMove(topTableLayoutPanel, topLeftTableLayoutPanel, topMidTableLayoutPanel, topRightTableLayoutPanel,
                pic_Logo, lblTitle, lblCountdown, lblTime);

            // 启动隐藏：让内容构建与首帧绘制在不可见期间完成，OnShown 后再恢复可见，
            // 用户第一眼看到的就是完整窗体（代价：启动慢约一帧）
            Opacity = 0;
            Icon = Properties.Resources.app;
        }
        private void DpiZoomControl()
        {
            // UINavMenu 菜单项高度是物理像素（Designer ItemHeight=48），高 DPI 下文字被放大而项高不变会显紧密。
            // 必须在 handle 创建前设置（TreeView.ItemHeight 在 handle 已创建后设置触发 RecreateHandle，SuspendLayout 内会被抑制）
            navMenu.ItemHeight = UiDpi.Scale(48);
            // UIComboBox 下拉列表项高默认 25 物理像素，System aware 下不随 DPI 缩放 ⇒ 走 ConfigFormStyle 单一真相点
            cmbLanguage.ItemHeight = ConfigFormStyle.ComboItemHeight;
        }

        /// <summary>
        /// 侧栏菜单配色：底色 + 滚动条，都跟随菜单色板。
        ///
        /// 🔴 UINavMenu **不是** UITreeView 的子类（它继承原生 TreeView），滚动条是自己叠上去的
        /// 一个 UIScrollBar：系统滚动条被 ShowScrollBar(隐藏) 掉、只借用它的位置与范围，视觉全部自绘。
        /// 所以配色属性是它独有的一套，**不要用 UITreeView 的 ScrollBarBackColor / ScrollBarRectColor**
        /// （本类没有那两个属性，会编译错）：
        ///   轨道 = ScrollFillColor；滑块三态 = ScrollBarColor / ScrollBarHoverColor / ScrollBarPressColor。
        ///
        /// 默认轨道 (56,56,56) 深灰、滑块 Silver——在深蓝菜单底上是一条灰带 + 一块亮灰，
        /// 这就是「滚动条配色与菜单冲突」的来源：冲突源在轨道，不在滑块。
        /// 设色后 menuStyle 自动切 UIMenuStyle.Custom，不再跟随主题（本项目固定自定义配色）。
        ///
        /// 🔴 FillColor 必须不透明（Designer 里是 Transparent）：
        /// UINavMenu.WndProc 拦截 WM_ERASEBKGND 后直接 return，背景擦除这条路径被它自己掐断，
        /// 每行底色只剩 OnDrawNode 的 FillRectangle(fillColor) 负责，而透明画刷是空操作。
        /// 两者叠加 = 重建菜单（切用户 / 权限变化）时旧节点的文字与高亮没人擦、新节点叠上去成重影——
        /// BeginUpdate/EndUpdate 只压得住中间态，压不住「终态也没擦底」。改为不透明后双缓冲才真正生效。
        /// </summary>
        private void ApplyNavMenuColors()
        {
            // 放最前：让下面显式设的滚动条配色最后生效
            navMenu.FillColor = navMenu.BackColor;

            navMenu.ScrollBarHandleWidth = 8;
            navMenu.ScrollFillColor = Color.FromArgb(0, 25, 100);       // 轨道 = BackColor，灰带消失
            navMenu.ScrollBarColor = Color.FromArgb(0, 100, 255);       // 滑块 = SelectedColor
            navMenu.ScrollBarHoverColor = Color.FromArgb(0, 140, 255);  // 悬停稍提亮
            navMenu.ScrollBarPressColor = Color.FromArgb(0, 80, 210);   // 按下稍压暗
        }
        #endregion

        #region 顶栏拖动
        private Point mouseDownPoint;

        /// <summary>
        /// 为顶栏可拖动区控件注册窗口拖动（左段 logo/标题 + 中段倒计时/时间；右侧按钮区不注册，保持可点击）
        /// </summary>
        private new void AddMousePressMove(params Control[] controls)
        {
            foreach (var control in controls)
            {
                control.MouseDown += OnTopPanelMouseDown;
                control.MouseMove += OnTopPanelMouseMove;
            }
        }

        private void OnTopPanelMouseDown(object sender, MouseEventArgs e)
        {
            mouseDownPoint = new Point(e.X, e.Y);
        }

        private void OnTopPanelMouseMove(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                Location = new Point(
                    Location.X + e.X - mouseDownPoint.X,
                    Location.Y + e.Y - mouseDownPoint.Y);
            }
        }
        #endregion

        #region 窗体生命周期
        protected async override void OnLoad(EventArgs e)
        {
            ValidateInjection();
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);

            // 抑制布局：初始化/建菜单/加载子窗体期间不逐次重绘，配合双缓冲消除打开闪烁
            SuspendLayout();
            try
            {
                UiDpi.EnableDoubleBuffered(this);
                InitializeResources();
                BindViewModel();
                BridgeEvents();
                ViewModel.OnViewLoaded();
                StartClock();
                ValidatePageRegistrations();
            }
            finally
            {
                ResumeLayout(false);
            }

            // 异步连接设备，不卡UI
            await InitializeDevicesAsync();
        }
        
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
            // 首帧绘制/合成已完成（内容就绪），恢复可见——WS_EX_COMPOSITED 的合成延迟发生在隐藏期间，用户无感
            BeginInvoke(new Action(() => Opacity = 1));
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ViewModel.PropertyChanged -= OnVmPropertyChanged;
            ViewModel.ShowLoginDialogRequested -= OnShowLoginDialog;
            ViewModel.CloseWindowRequested -= Close;
            ViewModel.MinimizeWindowRequested -= ToggleMinimize;
            ViewModel.ToggleMaximizeWindowRequested -= ToggleMaximize;
            ViewModel.NavigateToPageRequested -= OnNavigateToPage;
            NavigationService.Navigated -= OnNavigated;

            statePollTimer?.Stop();
            statusTimer?.Stop();
            ViewModel.OnViewClosing();
            ViewModel.Dispose();
            base.OnFormClosing(e);
        }
        #endregion

        #region 初始化
        private void ValidateInjection()
        {
            if (ViewModel == null) throw new InvalidOperationException("ViewModel未注入，请检查Autofac配置");
            if (MenuManager == null) throw new InvalidOperationException("MenuManager未注入，请检查Autofac配置");
            if (NavigationService == null) throw new InvalidOperationException("NavigationService未注入，请检查Autofac配置");
            if (AuthService == null) throw new InvalidOperationException("AuthService未注入，请检查Autofac配置");

            // 审计依赖必须一并校验：登录事实→审计的订阅（OnAuthFactRecorded）整段吞异常
            // （审计不得影响登录，故那是刻意的），万一注入失败只会表现为 NRE 被静默吃掉——
            // 登录照常、审计一条不落空，且无人知晓。宁可启动即炸，也不要"看起来在记、实际没记"。
            if (AuditService == null) throw new InvalidOperationException("AuditService未注入，请检查Autofac配置");

            if (ComponentContext == null) throw new InvalidOperationException("IComponentContext未注入，请检查Autofac配置");
        }

        /// <summary>
        /// 页面注册自检：Menu.json 全部页面节点逐一校验 Named&lt;Form&gt; 注册，
        /// 缺失即弹窗列出——把「点开才炸」前移到「起来就报」（不阻断启动）
        /// </summary>
        private void ValidatePageRegistrations()
        {
            var missingPages = MenuManager.GetUnregisteredPages();
            if (missingPages.Count > 0)
                MessageBox.ShowWarning("以下页面未注册，将无法打开：\n" + string.Join("\n", missingPages));
        }

        private void InitializeResources()
        {
            // 窗体标题走 Menu.json 的 MenuName（现场改名只改配置重启，不必重编译）；
            // 任务管理器主行的名字来自 exe 的 AssemblyTitle（编译期烘焙进版本资源），改配置不会变。
            // 标题栏 lblTitle 不在这里设——它绑定 ViewModel.CurrentPageTitle，赋值会被绑定覆盖
            string productName = MenuManager.MenuName;
            if (!string.IsNullOrWhiteSpace(productName))
            {
                Text = productName;
            }

            menuIcons.ColorDepth = ColorDepth.Depth32Bit;
            // 菜单图标按 DPI 放大（200% 屏 24→48），与放大后的菜单字体保持协调；图标资源为 24px 位图，放大后清晰度有限，后续可换高清资源
            menuIcons.ImageSize = new Size(UiDpi.Scale(24), UiDpi.Scale(24));
            menuIcons.Images.Add("default", CreateDefaultIcon());
            navMenu.ImageList = menuIcons;
            pic_Logo.Image = Properties.Resources.AppLogo;
            userAvatar.Image = Properties.Resources.默认头像;
        }

        private async Task InitializeDevicesAsync()
        {
            try
            {
                ViewModel.StatusText = "正在连接设备...";
                var deviceManager = ComponentContext.Resolve<IDeviceManager>();
                bool connected = await deviceManager.ConnectAllAsync();

                ViewModel.StatusText = connected
                    ? "设备连接成功"
                    : "部分设备连接失败，请检查配置";

                if (!connected)
                {
                    MessageBox.ShowWarning(ViewModel.StatusText);
                }
            }
            catch (Exception ex)
            {
                ViewModel.StatusText = $"设备初始化异常: {ex.Message}";
                MessageBox.ShowError($"设备初始化失败: {ex.Message}");
            }
        }

        private void StartClock()
        {
            statusTimer.Interval = 1000;
            statusTimer.Tick += (s, e) => ViewModel.UpdateTime();
            statusTimer.Start();
            statePollTimer.Interval = 500;
            statePollTimer.Tick += (s, e) => ViewModel.PollSystemState();
            statePollTimer.Start();
        }
        #endregion

        #region 绑定映射
        private void BindViewModel()
        {
            // === 状态栏文本绑定 ===
            lblUsername.DataBindings.Add("Text", ViewModel, nameof(MainViewModel.CurrentUserName));
            lblRole.DataBindings.Add("Text", ViewModel, nameof(MainViewModel.CurrentRoleName));
            // 标题栏只显示当前页名：品牌名由左侧 Logo 图承担，再拼一遍产品名是重复
            lblTitle.DataBindings.Add("Text", ViewModel, nameof(MainViewModel.CurrentPageTitle));
            lblCountdown.DataBindings.Add("Text", ViewModel, nameof(MainViewModel.CountdownText));
            lblTime.DataBindings.Add("Text", ViewModel, nameof(MainViewModel.CurrentTime));

            // === 按钮状态绑定 ===
            btnStart.DataBindings.Add("Enabled", ViewModel, nameof(MainViewModel.CanStart));
            btnPause.DataBindings.Add("Enabled", ViewModel, nameof(MainViewModel.CanPause));
            btnReset.DataBindings.Add("Enabled", ViewModel, nameof(MainViewModel.CanReset));

            btnLogout.DataBindings.Add("Visible", ViewModel, nameof(MainViewModel.ShowLogout));

            

            // === 语言下拉框 ===
            var languages = Enum.GetValues(typeof(LanguageType))
                .Cast<LanguageType>()
                .Select(lt => new { Display = lt.GetDescription(), Value = lt })
                .ToList();

            cmbLanguage.DataSource = languages;
            cmbLanguage.DisplayMember = "Display";
            cmbLanguage.ValueMember = "Value";
            cmbLanguage.SelectedValue = ViewModel.CurrentLanguage;
            cmbLanguage.SelectedIndexChanged += (s, e) =>
            {
                if (cmbLanguage.SelectedValue is LanguageType lang)
                    ViewModel.ChangeLanguageCommand.Execute(lang);
            };
        }
        #endregion

        #region 事件桥接
        private void BridgeEvents()
        {
            // === VM请求 → View动作 ===
            ViewModel.PropertyChanged += OnVmPropertyChanged;
            ViewModel.ShowLoginDialogRequested += OnShowLoginDialog;
            ViewModel.CloseWindowRequested += Close;
            ViewModel.MinimizeWindowRequested += ToggleMinimize;
            ViewModel.ToggleMaximizeWindowRequested += ToggleMaximize;
            ViewModel.NavigateToPageRequested += OnNavigateToPage;

            // === 导航服务事件 → View动作 ===
            NavigationService.Navigated += OnNavigated;

            // === 认证事实 → 审计留痕（登录域 UI 不可观测，快照只能在 AuthService 内采集，故在此接线）===
            AuthService.AuthFactRecorded += OnAuthFactRecorded;

            // === 控件事件 → VM命令 ===
            btnClosed.Click += (s, e) => ViewModel.CloseWindowCommand.Execute(null);
            btnMin.Click += (s, e) => ViewModel.MinimizeWindowCommand.Execute(null);
            btnMax.Click += (s, e) => ViewModel.ToggleMaximizeCommand.Execute(null);
            btnLogin.Click += (s, e) => ViewModel.ShowLoginCommand.Execute(null);
            btnLogout.Click += (s, e) => ViewModel.LogoutCommand.Execute(null);

            // 三按钮独立绑定
            btnStart.Click += (s, e) => ViewModel.StartProcessCommand.Execute(null);
            btnPause.Click += (s, e) => ViewModel.PauseProcessCommand.Execute(null);
            btnReset.Click += (s, e) => ViewModel.ResetProcessCommand.Execute(null);

            btnHelp.Click += (s, e) => ViewModel.ShowHelpCommand.Execute(null);

            // === 菜单点击 → VM命令 ===
            navMenu.NodeMouseClick += OnMenuNodeMouseClick;
        }
        #endregion

        #region 事件处理

        /// <summary>
        /// 认证事实 → 审计记录（登录域留痕的**唯一**消费点）。
        ///
        /// 为何在 View 层接线：刷卡登录经键盘钩子直达 AuthService，UI 层结构上不可观测；
        /// 且身份快照只能在 AuthService 内部取（登录成功须在 SetCurrentUser 之后、
        /// 登出须在 ClearCurrentUser 之前）。AuthService 只广播 AuthFact
        /// （Share 层零依赖契约，它不认识任何审计类型），本层负责翻译成审计记录。
        ///
        /// 审计是旁路：落库失败、磁盘满、锁等待都不得影响登录流程，
        /// 故整体 try-catch 吞掉（AuditService 内部已吞，此处为第二道保险，防将来换实现）。
        /// </summary>
        private void OnAuthFactRecorded(AuthFact fact)
        {
            if (fact == null) return;

            try
            {
                var action = fact.Kind == AuthFactKind.LoginSucceeded ? AuditAction.Login
                           : fact.Kind == AuthFactKind.LoginFailed ? AuditAction.LoginFailed
                                                                   : AuditAction.Logout;
                var result = fact.Kind == AuthFactKind.LoginFailed ? AuditResult.Fail : AuditResult.Success;

                AuditService.Record(new AuditEntry(
                    AuditCategory.System,
                    action,
                    fact.Username,
                    new AuditActor(fact.Username,
                                   string.Join(",", fact.RoleNames ?? new List<string>()),
                                   fact.MaxRoleLevel),
                    result));
            }
            catch { }
        }

        private void OnVmPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MainViewModel.LoginButtonText):
                    btnLogin.Text = ViewModel.LoginButtonText;
                    break;
                case nameof(MainViewModel.CountdownForeColor):
                    lblCountdown.ForeColor = ViewModel.CountdownForeColor;
                    break;
                case nameof(MainViewModel.MenuItems):
                    BuildMenuTree(ViewModel.MenuItems, null);
                    ValidateMenuIcons();
                    // 菜单重建（切用户/权限变化）会用 Menu.json 中文重建节点，立即重新翻译恢复当前语言
                    TranslationService?.TranslateForm(this);
                    break;

                case nameof(MainViewModel.CurrentLanguage):
                    // 启动恢复语言（OnViewLoaded）/切换语言后同步下拉框选中项，避免显示与实际语言不一致
                    if (!(cmbLanguage.SelectedValue is LanguageType sel) || sel != ViewModel.CurrentLanguage)
                        cmbLanguage.SelectedValue = ViewModel.CurrentLanguage;
                    break;
            }
        }

        private void OnShowLoginDialog()
        {
            using var loginForm = ComponentContext.Resolve<FormLogin>();

            bool loginSuccess = false;
            loginForm.ViewModel.LoginCompleted += success => loginSuccess = success;

            if (loginForm.ShowDialog() == DialogResult.OK && loginSuccess)
            {
                ViewModel.OnLoginSuccess(loginForm.GetUsername(), loginForm.GetPassword());
            }
        }

        private void OnNavigateToPage(MenuItem menuItem) => NavigationService.NavigateTo(menuItem.Name);

        private void OnNavigated(object sender, NavigationEventArgs e)
        {
            try
            {
                if (currentForm != null)
                {
                    mainPanel.Controls.Remove(currentForm);
                }

                var form = e.Form;
                form.TopLevel = false;
                form.FormBorderStyle = FormBorderStyle.None;

                // 屏幕外装载：OnLoad 由 Show() 触发，把它挪到可视区外跑完，装载中间态一帧都不露面。
                // 🔴 Dock 必须放在装载之后设，否则会覆盖 Location，页面立刻回到可视区。
                // 🔴 不用 form.Opacity 过渡：嵌入 Form 的 TopLevel=false，WinForms 的
                //    AllowTransparency / UpdateLayered 都硬检查 TopLevel，会被静默忽略（已实测）。
                // 防闪靠各页面自己的控件级双缓冲，不用 WS_EX_COMPOSITED——后者会把大文本控件的
                // 重绘放大成整窗重新合成，MES 响应页曾因此周期性白闪。
                form.Location = new Point(-32000, -32000);
                form.Size = mainPanel.ClientSize;

                mainPanel.SuspendLayout();
                form.SuspendLayout();
                try
                {
                    // 页面首次呈现即按当前语言翻译：懒加载页面在切语言后才创建（创建时无翻译），
                    // 切语言只覆盖已打开窗体（RefreshAllOpenForms），此处补上"呈现前翻译"闭合链路
                    TranslationService?.TranslateForm(form);

                    mainPanel.Controls.Add(form);
                    form.Show();
                }
                finally
                {
                    form.ResumeLayout(true);
                    mainPanel.ResumeLayout(true);
                }

                // 装载全部完成，铺满并拉回可视区
                form.Dock = DockStyle.Fill;
                currentForm = form;
            }
            catch (Exception ex)
            {
                MessageBox.ShowError($"加载窗体失败: {ex.Message}");
            }
        }

        private void OnMenuNodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node?.Tag is MenuItem menuItem)
                ViewModel.SelectMenuCommand.Execute(menuItem);
        }
        #endregion

        #region 菜单渲染
        private void BuildMenuTree(List<MenuItem> menus, TreeNode parentNode)
        {
            bool isTopLevel = parentNode == null;

            // 顶层重建（切用户/权限变化）时抑制逐项重绘：先 Clear 再逐个 Add 会触发多次绘制，
            // 构建中间态出现菜单项重影/相互遮挡，BeginUpdate/EndUpdate 合并为一次性重绘
            if (isTopLevel)
                navMenu.BeginUpdate();

            try
            {
                if (isTopLevel)
                {
                    navMenu.Nodes.Clear();
                }

                foreach (var menu in menus)
                {
                    int iconIndex = GetIconIndex(menu.Icon);

                    var node = new TreeNode(menu.DisplayName)
                    {
                        // Name = Menu.json 稳定标识（如 TranslateViewer）：作为翻译/提取的 key（TreeViewTranslator 依赖 Name）
                        Name = menu.Name,
                        Tag = menu,
                        ImageIndex = iconIndex,
                        SelectedImageIndex = iconIndex
                    };

                    if (parentNode == null)
                        navMenu.Nodes.Add(node);
                    else
                        parentNode.Nodes.Add(node);

                    if (menu.Children?.Count > 0)
                        BuildMenuTree(menu.Children, node);
                }

                if (isTopLevel)
                {
                    foreach (TreeNode node in navMenu.Nodes)
                        node.Expand();
                }
            }
            finally
            {
                if (isTopLevel)
                    navMenu.EndUpdate();
            }
        }

        private int GetIconIndex(string iconName)
        {
            if (string.IsNullOrEmpty(iconName))
                return 0;

            string key = iconName.ToLower();
            if (menuIcons.Images.ContainsKey(key))
                return menuIcons.Images.IndexOfKey(key);

            var icon = LoadIconFromResources(iconName);
            if (icon != null)
            {
                menuIcons.Images.Add(key, icon);
                return menuIcons.Images.Count - 1;
            }

            return 0;
        }

        private Image LoadIconFromResources(string iconName)
        {
            try
            {
                return Properties.Resources.ResourceManager.GetObject(iconName) as Image;
            }
            catch
            {
                return null;
            }
        }

        private Image CreateDefaultIcon()
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            {
                g.FillRectangle(Brushes.LightGray, 0, 0, 24, 24);
            }
            return bmp;
        }
        #endregion

        #region 菜单图标校验

        /// <summary>
        /// 校验菜单图标资源是否齐全。
        /// 图标名来自 Menu.json，运行时按名字动态取资源（ResourceManager.GetObject），缺失时回落默认图标：
        /// 现场看到灰块无法区分"配置写错"与"本就无图标"，故汇总告警一条（仅留痕，不阻断启动）。
        /// 取全量菜单而非当前用户可见菜单，避免同一份配置因权限变化而"今天报、明天不报"。
        /// </summary>
        private void ValidateMenuIcons()
        {
            if (MenuManager == null)
                return;

            // ResourceManager.GetObject 返回的是 ResourceSet 缓存的同一实例，重复取不会重复分配，无需另做缓存
            var missingNames = MenuManager.GetAllMenuItems()
                .Select(m => m.Icon)
                .Where(n => !string.IsNullOrEmpty(n))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(n => LoadIconFromResources(n) == null)
                .ToList();

            if (missingNames.Count == 0)
                return;

            string message = $"菜单图标资源缺失 {missingNames.Count} 个：{string.Join("、", missingNames)}"
                             + "（已回落默认图标，请检查 Menu.json 的 Icon 字段与 Resources.resx）";
            LogService?.Warn(message);
            Console.WriteLine(message);
        }

        #endregion

        #region 辅助方法
        private void ToggleMaximize()
        {
            WindowState = WindowState == FormWindowState.Maximized
                ? FormWindowState.Normal
                : FormWindowState.Maximized;
        }

        private void ToggleMinimize() => WindowState = FormWindowState.Minimized;
        #endregion
    }
}