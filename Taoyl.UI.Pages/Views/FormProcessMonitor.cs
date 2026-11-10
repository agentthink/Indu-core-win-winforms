using Sunny.UI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 流程监控页面
    /// 
    /// 布局设计：
    /// 外层 tableLayoutPanel1 三行：顶部(状态/控制) | 中部(表格+分页) | 底部(操作按钮)
    /// 
    /// 性能与体验策略：
    /// 1. 异步刷新：后台 PrepareSnapshot，UI 线程 CommitSnapshot，防止阻塞界面
    /// 2. 逐行同步：结构不变时仅 ResetItem，保持滚动条与视觉稳定
    /// 3. CellFormatting 按需着色：仅可见行触发，避免全量遍历
    /// 4. 操作反馈 3 秒自动清除，避免旧消息误导
    /// 5. 按钮状态由 ViewModel 字段属性驱动，WinForms 数据绑定，减少手动同步代码
    /// 6. 列头自然排序：流程名称支持 Flow1, Flow2, Flow10 正确排序
    /// </summary>
    public partial class FormProcessMonitor : UIPage
    {
        #region 依赖注入

        /// <summary>
        /// 流程监控视图模型，由 Autofac 属性注入
        /// </summary>
        public ProcessMonitorViewModel ViewModel { get; set; }

        #endregion

        #region 表格列对象

        /// <summary>
        /// DGV 的 DPI 缩放系数（System aware 下窗体按物理像素布局，字体/行高/列宽需手动补偿）
        /// </summary>
        private float dgvDpiScale = 1F;

        /// <summary>分页条行高基准（照 FormAudit 基准 37；分页按钮全 Dock=Fill，高度吃宿主 TLP 行）</summary>
        private const int PaginationRowHeightBasis = 37;

        /// <summary>
        /// 状态列加粗字体（复用缓存，避免 CellFormatting 高频 new Font 泄漏 GDI 句柄）
        /// </summary>
        private Font statusBoldFont;

        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colTimestamp;
        private DataGridViewTextBoxColumn colCategory;
        private DataGridViewTextBoxColumn colStatus;
        private DataGridViewTextBoxColumn colStep;
        private DataGridViewTextBoxColumn colException;
        private DataGridViewTextBoxColumn colEnabled;
        private DataGridViewTextBoxColumn colPauseOthers;

        #endregion

        #region 定时器

        /// <summary>数据轮询定时器</summary>
        private readonly Timer pollTimer = new Timer();

        /// <summary>操作反馈自动清除定时器（3 秒）</summary>
        private readonly Timer feedbackTimer = new Timer();

        #endregion

        #region 状态标记

        private bool isFirstLoad = true;

        /// <summary>OnLoad 已完成（StartPolling 之后置位）。OnParentChanged 在 Controls.Add 时就会触发，
        /// 那时 Tick 还没绑定——用它挡住构造期/首挂载的空转与误判。</summary>
        private bool initialized;

        /// <summary>当前排序方向，true 表示升序</summary>
        private bool isSortedAscending = true;

        #endregion

        public FormProcessMonitor()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            // UIComboBox 下拉项高默认 25 物理像素、不随 DPI 缩放（高 DPI 下项高夹字），统一走 ConfigFormStyle 单一真相点
            cmbPageSize.ItemHeight = ConfigFormStyle.ComboItemHeight;
        }

        #region 生命周期

        protected override void OnLoad(EventArgs e)
        {
            ValidateInjection();
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);
            
            SetupDgv();
            SetupRefreshControls();
            SetupPagination();
            SetupFeedbackTimer();
            BindEvents();
            BindButtonStates();
            StartPolling();
        }

        /// <summary>
        /// 首帧显示完成（Form.Shown 每实例仅触发一次，与 OnLoad 配对）：WinForms 保证 OnShown 前
        /// 窗口定位/尺寸消息（WM_SIZE/WM_WINDOWPOSCHANGED）已处理完毕——此处恢复禁绘 + 整页重绘，
        /// 终态一次成帧。缓存复用切回（Remove→Add→Show）不重走 Load/Shown，本恢复不重复执行。
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            // 初始按当前语言重算一次动态按钮文本（原在 OnLoad，随首帧移至此）：
            // VM 字段初始值是中文字面量不经过 ToTranslate，且 VM 订阅 LanguageChanged 只在切语言时回调
            // （不触发回放），必须补一次，否则启动时整机初始化等按钮停留在中文直到选中行/切语言才刷新。
            // OnShown 时首帧快照若已提交，SelectedRow 已就绪，算出的是真实选中分支而非「无选中行」分支。
            // 仍在 SetRedraw(true) 之前：文本改动与恢复绘制合并成一帧，不闪。
            ViewModel.RefreshDynamicTexts();

            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        /// <summary>
        /// 校验 ViewModel 注入，失败即抛异常阻止继续加载
        /// </summary>
        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
        }

        #endregion

        #region 表格初始化

        private void SetupDgv()
        {
            dgvProcesses.AutoGenerateColumns = false;
            dgvProcesses.ReadOnly = true;
            dgvProcesses.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvProcesses.MultiSelect = false;
            dgvProcesses.AllowUserToAddRows = false;
            dgvProcesses.AllowUserToDeleteRows = false;
            dgvProcesses.AllowUserToResizeRows = false;
            // System aware 下显式 Font 不随 DPI 缩放，DGV 字体/行高/列宽按 DPI 动态补偿。
            // 系数用 UiDpi.Scale（Program.cs 按 Graphics.DpiX 算好的 GlobalFontScale），不用 DeviceDpi——实测不可靠
            dgvDpiScale = UiDpi.DpiScale;
            dgvProcesses.RowTemplate.Height = (int)Math.Round(44 * dgvDpiScale);
            dgvProcesses.BackgroundColor = Color.White;
            // 控件 Font 保持 12F 物理：UIDataGridView 列头字体跟随控件 Font 且列头绘制走 GlobalFontScale，
            // 物理放大会双重放大（24F×200%=48F 视觉）；12F×200%=24F 视觉，与按钮一致
            dgvProcesses.Font = new Font("微软雅黑", 12F);
            // 状态列加粗字体：单元格绘制不走 GlobalFontScale，需物理放大才是 24F 视觉
            statusBoldFont = new Font("微软雅黑", 12F * dgvDpiScale, FontStyle.Bold);
            dgvProcesses.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvProcesses.ScrollBars = ScrollBars.Both;

            // 单元格字体物理放大（原生绘制不走 GlobalFontScale）；Designer 显式宋体优先于控件 Font，必须一并重建
            dgvProcesses.DefaultCellStyle.Font = new Font("微软雅黑", 12F * dgvDpiScale);
            dgvProcesses.RowsDefaultCellStyle.Font = new Font("微软雅黑", 12F * dgvDpiScale);

            // 列头样式保留，数据行样式由 SunnyUI 主题与 CellFormatting 共同接管
            dgvProcesses.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 40, 170),
                ForeColor = Color.White
            };
            dgvProcesses.EnableHeadersVisualStyles = false;

            dgvProcesses.Columns.Clear();
            colName = AddTextColumn(nameof(ProcessMonitorRow.ProcessName), 140, DataGridViewContentAlignment.MiddleLeft);
            colTimestamp = AddTextColumn(nameof(ProcessMonitorRow.TimestampText), 110);  // 第二列：更新时间
            colCategory = AddTextColumn(nameof(ProcessMonitorRow.Category), 70);
            colStatus = AddTextColumn(nameof(ProcessMonitorRow.StatusText), 80);
            colStep = AddTextColumn(nameof(ProcessMonitorRow.Step), 60);
            colException = AddTextColumn(nameof(ProcessMonitorRow.ExceptionDisplay), 220, DataGridViewContentAlignment.MiddleLeft);
            colEnabled = AddTextColumn(nameof(ProcessMonitorRow.IsEnabledText), 60);
            colPauseOthers = AddTextColumn(nameof(ProcessMonitorRow.CanPauseOthersText), 60);

            dgvProcesses.DataSource = ViewModel.ProcessRows;
            // 列头高度：弃用 AutoSize。AutoSize 仅在初始布局等时机测量，运行时按 DPI 改字体后不会可靠重算，
            // 列头高度停留在默认值导致高 DPI 下被切。改 EnableResizing + 按 DPI 显式补偿，与字体/行高/列宽一致
            dgvProcesses.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvProcesses.ColumnHeadersHeight = (int)Math.Round(36 * dgvDpiScale);

            // 启用列头排序：流程名称自定义自然排序，其余禁用
            foreach (DataGridViewColumn col in dgvProcesses.Columns)
            {
                if (col.Name == colName.Name)
                {
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                }
                else
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }

            dgvProcesses.SelectionChanged += OnSelectionChanged;
            dgvProcesses.CellFormatting += OnCellFormatting;
            dgvProcesses.ColumnHeaderMouseClick += OnColumnHeaderMouseClick;
        }

        private DataGridViewTextBoxColumn AddTextColumn(string dataProperty, int width,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var prop = typeof(ProcessMonitorRow).GetProperty(dataProperty);
            var desc = prop?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast < DescriptionAttribute > ().FirstOrDefault();

            var col = new DataGridViewTextBoxColumn
            {
                Name = $"col{dataProperty}",
                HeaderText = desc?.Description ?? dataProperty,
                DataPropertyName = dataProperty,
                Width = (int)Math.Round(width * dgvDpiScale),
                DefaultCellStyle = { Alignment = align },
                ReadOnly = true
            };
            dgvProcesses.Columns.Add(col);
            return col;
        }

        #endregion

        #region 刷新控制与分页初始化

        /// <summary>
        /// 初始化自动刷新开关、刷新间隔、手动刷新按钮
        /// </summary>
        private void SetupRefreshControls()
        {
            switchAutoRefresh.Active = ViewModel.IsAutoRefresh;
            switchAutoRefresh.ValueChanged += (s, e) =>
            {
                ViewModel.IsAutoRefresh = switchAutoRefresh.Active;
                // 走 SyncPolling 而非直接赋 Enabled：开关打开时若页面不在舞台上（理论窗口：切页瞬间改配置），
                // 直接赋 true 会让后台重新开跑
                SyncPolling();
            };

            upDownInterval.Minimum = 100;
            upDownInterval.Maximum = 10000;
            upDownInterval.Step = 100;
            upDownInterval.Value = ViewModel.RefreshInterval;
            upDownInterval.ValueChanged += (s, e) =>
            {
                ViewModel.RefreshInterval = upDownInterval.Value;
                pollTimer.Interval = Math.Max(100, ViewModel.RefreshInterval);
            };

            btnManulRefresh.Click += async (s, e) => await ExecuteRefreshAsync();
        }

        /// <summary>
        /// 分页控件初始化（照审计/报警页姿势：自建 <see cref="Controls.Pagination"/> + DPI 补偿 + 单向同步）。
        /// 旧 UIPagination 接法（DataBindings + 程序赋值触发 PageChanged）在页大小变化/页数缩水时
        /// 会双重触发刷新，换控件时一并收敛。
        /// </summary>
        private void SetupPagination()
        {
            ApplyPaginationScale();

            cmbPageSize.Items.AddRange(new object[] { "10", "20", "50", "100", "全部" });
            cmbPageSize.SelectedIndex = 1; // 默认 20
            cmbPageSize.SelectedIndexChanged += OnPageSizeChanged;

            // 真相源在 VM：初值同步放在 PageChanged 挂接之前（同值赋值不发事件）
            paginationProcess.PageSize = ViewModel.PageSize;
            SyncPagination();

            // 控件 → VM：只写页码 + 刷新；
            // 回环防御：VM 分页属性变化会 SyncPagination 回写控件，同页直接跳过
            paginationProcess.PageChanged += OnPaginationPageChanged;
        }

        /// <summary>
        /// 分页控件的 DPI 补偿（**缩放策略归宿主**，照 FormAudit 清单）。
        /// 字体属性（PageButtonFont / NavButtonFont）刻意不动：DPI 铁律下字号保持物理值，
        /// SunnyUI 自按 GlobalFontScale 绘制文字，再放大会双重放大。
        /// </summary>
        private void ApplyPaginationScale()
        {
            paginationProcess.PrevNextButtonWidth = UiDpi.Scale(75);
            paginationProcess.PageButtonWidth = UiDpi.Scale(29);
            paginationProcess.PageButtonWidthMedium = UiDpi.Scale(36);
            paginationProcess.PageButtonWidthLong = UiDpi.Scale(52);
            paginationProcess.EllipsisWidth = UiDpi.Scale(29);
            paginationProcess.JumpLabelWidth = UiDpi.Scale(21);
            paginationProcess.JumpBoxWidth = UiDpi.Scale(53);
            paginationProcess.JumpButtonWidth = UiDpi.Scale(61);

            // 分页条按钮全 Dock=Fill，高度吃宿主 TLP 行——行高不放大等于没放大
            var paginationRow = midTableLayoutPanel.RowStyles[1];
            paginationRow.SizeType = SizeType.Absolute;
            paginationRow.Height = UiDpi.Scale(PaginationRowHeightBasis);
        }

        /// <summary>
        /// VM 分页状态 → 控件（单向）。反向（控件 → VM）由 PageChanged 负责，两向分开写而不挂
        /// DataBindings：控件会单方面夹取越界页码并主动发 PageChanged 让本页把 VM 回正
        /// （见 <see cref="Controls.Pagination"/> RenderAndNotifyIfClamped）——
        /// DataBindings 会与这条回正路径打架（审计页已实锤注释）。
        /// </summary>
        private void SyncPagination()
        {
            paginationProcess.TotalCount = ViewModel.TotalCount;
            paginationProcess.PageSize = ViewModel.PageSize;
            paginationProcess.ActivePage = ViewModel.PageIndex;
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            string selected = cmbPageSize.SelectedItem?.ToString();
            int newSize = selected == "全部" ? int.MaxValue : int.Parse(selected);

            // 页大小变化必须回第 1 页重查（控件侧由 SyncPagination 统一同步，不再手动灌属性）
            ViewModel.PageSize = newSize;
            ViewModel.PageIndex = 1;
            SyncPagination();

            _ = ExecuteRefreshAsync(resetScroll: true);
        }

        private void OnPaginationPageChanged(object sender, EventArgs e)
        {
            if (ViewModel.PageIndex == paginationProcess.ActivePage) return;
            ViewModel.PageIndex = paginationProcess.ActivePage;
            _ = ExecuteRefreshAsync(resetScroll: true);
        }

        /// <summary>
        /// 初始化操作反馈自动清除定时器（3 秒单次触发）
        /// </summary>
        private void SetupFeedbackTimer()
        {
            feedbackTimer.Interval = 3000;
            feedbackTimer.Tick += (s, e) =>
            {
                feedbackTimer.Stop();
                if (ViewModel != null)
                    ViewModel.OperationFeedback = string.Empty;
            };
        }

        #endregion

        #region 事件绑定

        private void BindEvents()
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 文本与状态绑定（强类型 nameof，避免字符串硬编码）
            lblFeedback.DataBindings.Add(nameof(lblFeedback.Text), ViewModel, nameof(ProcessMonitorViewModel.OperationFeedback));
            lblSystemState.DataBindings.Add(nameof(lblSystemState.Text), ViewModel, nameof(ProcessMonitorViewModel.SystemState));

            // 控制命令
            btnStart.Click += (s, e) => ViewModel.StartOrResumeSelectedCommand.Execute(null);
            btnPause.Click += (s, e) => ViewModel.PauseSelectedCommand.Execute(null);
            btnReset.Click += (s, e) => ViewModel.ResetSelectedCommand.Execute(null);
            btnInit.Click += (s, e) => ViewModel.InitializeSelectedCommand.Execute(null);

            btnToggleEnabled.Click += (s, e) => ViewModel.ToggleSelectedEnabledCommand.Execute(null);
            btnTogglePauseOthers.Click += (s, e) => ViewModel.ToggleSelectedPauseOthersCommand.Execute(null);

            btnInitAll.Click += (s, e) => ViewModel.InitializeAllCommand.Execute(null);

            // 行号绘制
            dgvProcesses.RowPostPaint += (s, e) => OnRowPostPaint(dgvProcesses, e);
        }

        /// <summary>
        /// 按钮状态与文本统一绑定到 ViewModel 字段属性
        /// </summary>
        private void BindButtonStates()
        {
            btnStart.DataBindings.Add(nameof(btnStart.Enabled), ViewModel, nameof(ProcessMonitorViewModel.CanStartSelected));
            btnStart.DataBindings.Add(nameof(btnStart.Text), ViewModel, nameof(ProcessMonitorViewModel.StartButtonText));

            btnPause.DataBindings.Add(nameof(btnPause.Enabled), ViewModel, nameof(ProcessMonitorViewModel.CanPauseSelected));
            btnReset.DataBindings.Add(nameof(btnReset.Enabled), ViewModel, nameof(ProcessMonitorViewModel.CanResetSelected));
            btnInit.DataBindings.Add(nameof(btnInit.Enabled), ViewModel, nameof(ProcessMonitorViewModel.CanInitSelected));

            btnInitAll.DataBindings.Add(nameof(btnInitAll.Text), ViewModel, nameof(ProcessMonitorViewModel.InitAllButtonText));
            btnInitAll.DataBindings.Add(nameof(btnInitAll.Enabled), ViewModel, nameof(ProcessMonitorViewModel.CanInitAll));

            btnToggleEnabled.DataBindings.Add(nameof(btnToggleEnabled.Text), ViewModel, nameof(ProcessMonitorViewModel.ToggleEnabledButtonText));
            btnTogglePauseOthers.DataBindings.Add(nameof(btnTogglePauseOthers.Text), ViewModel, nameof(ProcessMonitorViewModel.TogglePauseOthersButtonText));

            
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ProcessMonitorViewModel.SystemState):
                    lblSystemState.ForeColor = ViewModel.GetStateColor(ViewModel.SystemState);
                    break;

                case nameof(ProcessMonitorViewModel.SelectedRow):
                    UpdateSelectionInfo();
                    break;

                case nameof(ProcessMonitorViewModel.OperationFeedback):
                    if (!string.IsNullOrEmpty(ViewModel.OperationFeedback))
                    {
                        feedbackTimer.Stop();
                        feedbackTimer.Start();
                    }
                    break;

                case nameof(ProcessMonitorViewModel.TotalCount):
                case nameof(ProcessMonitorViewModel.PageIndex):
                case nameof(ProcessMonitorViewModel.PageSize):
                    SyncPagination();
                    break;
            }
        }

        #endregion

        #region 行选中变化

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (dgvProcesses.SelectedRows.Count == 0 || dgvProcesses.Rows.Count == 0)
            {
                ViewModel.SelectedRow = null;
                return;
            }

            var row = dgvProcesses.SelectedRows[0];
            ViewModel.SelectedRow = row.DataBoundItem as ProcessMonitorRow;
        }

        #endregion

        #region 选中信息更新

        /// <summary>
        /// 根据当前选中行更新 lblSelectedInfo 文本与颜色
        /// </summary>
        private void UpdateSelectionInfo()
        {
            var row = ViewModel.SelectedRow;

            if (row == null)
            {
                lblSelectedInfo.Text = "未选择流程";
                lblSelectedInfo.ForeColor = Color.FromArgb(48, 48, 48);
                return;
            }

            lblSelectedInfo.Text = $"{row.ProcessName}  [{row.StatusText}]";
            lblSelectedInfo.ForeColor = ViewModel.GetStatusColor(row.State);
        }

        #endregion

        #region 列头自然排序

        /// <summary>
        /// 流程名称列头点击：自然排序（Flow1, Flow2, Flow10）
        /// 其他列不支持排序
        /// </summary>
        private void OnColumnHeaderMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.ColumnIndex != colName.Index) return;

            // 切换排序方向
            isSortedAscending = !isSortedAscending;

            var sorted = isSortedAscending
                ? ViewModel.ProcessRows.OrderBy(r => r.ProcessName, new NaturalStringComparer()).ToList()
                : ViewModel.ProcessRows.OrderByDescending(r => r.ProcessName, new NaturalStringComparer()).ToList();

            // 保存当前选中流程名，排序后恢复
            string selectedName = ViewModel.SelectedRow?.ProcessName;

            ViewModel.ProcessRows.RaiseListChangedEvents = false;
            try
            {
                ViewModel.ProcessRows.Clear();
                foreach (var item in sorted)
                    ViewModel.ProcessRows.Add(item);
            }
            finally
            {
                ViewModel.ProcessRows.RaiseListChangedEvents = true;
                ViewModel.ProcessRows.ResetBindings();
            }

            // 恢复选中
            if (!string.IsNullOrEmpty(selectedName))
            {
                var rowToSelect = dgvProcesses.Rows
                    .Cast<DataGridViewRow>()
                    .FirstOrDefault(r => (r.DataBoundItem as ProcessMonitorRow)?.ProcessName == selectedName);
                if (rowToSelect != null)
                    rowToSelect.Selected = true;
            }

            // 显示排序方向箭头
            colName.HeaderCell.SortGlyphDirection = isSortedAscending ? SortOrder.Ascending : SortOrder.Descending;
        }

        #endregion

        #region 轮询刷新

        /// <summary>
        /// 页面是否在舞台上。切页 = mainPanel.Controls.Remove / Controls.Add（内嵌页），
        /// 既不 Close 也不 Dispose ⇒ OnFormClosing 里那条 Stop 永远等不到。
        /// 口径与 FormProductLive 一致：Parent != null &amp;&amp; Visible。
        /// </summary>
        private bool IsOnStage => Parent != null && Visible;

        /// <summary>
        /// 按「在舞台 + 自动刷新开关」同步轮询定时器。
        /// 🔴 页面是容器单例（只创建一次、永不销毁），不按可见性停就会一直在后台每 500ms
        ///    做全量快照 + 重建全部行对象——切走后这些工作全是白烧的 CPU 与 GC 分配。
        /// </summary>
        private void SyncPolling()
        {
            pollTimer.Enabled = IsOnStage && (ViewModel?.IsAutoRefresh ?? false);
        }

        /// <summary>
        /// 切页主挂点（🔴 不是 OnVisibleChanged）。
        /// Controls.Remove 后 Control.Visible 仍返回 true（无 Parent 时读自身标志），
        /// VisibleChanged 不触发——FormProductLive 已实证，本页照同一口径补。
        /// </summary>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (!initialized) return;
            SyncPolling();
        }

        /// <summary>自身显隐（Show/Hide）补充挂点：切页走 ParentChanged，这里兜 Hide。</summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!initialized) return;
            SyncPolling();
        }

        private void StartPolling()
        {
            pollTimer.Interval = Math.Max(100, ViewModel.RefreshInterval);
            pollTimer.Tick += async (s, e) =>
            {
                if (ViewModel.IsAutoRefresh)
                    await ExecuteRefreshAsync();
            };

            _ = ExecuteRefreshAsync();
            initialized = true;
            SyncPolling();
        }

        /// <summary>
        /// 执行一次完整刷新：后台准备数据，UI 线程提交
        /// resetScroll=true 用于翻页或页大小变化，强制回到顶部
        /// </summary>
        private async Task ExecuteRefreshAsync(bool resetScroll = false)
        {
            try
            {
                await Task.Run(() => ViewModel.PrepareSnapshot());
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"后台刷新异常: {ex.Message}");
                return;
            }

            if (IsDisposed || !IsHandleCreated)
                return;

            this.Invoke(new Action(() =>
            {
                ViewModel.CommitSnapshot();

                if (resetScroll && dgvProcesses.Rows.Count > 0)
                    dgvProcesses.FirstDisplayedScrollingRowIndex = 0;

                if (isFirstLoad && ViewModel.ProcessRows.Count > 0)
                {
                    dgvProcesses.ClearSelection();
                    dgvProcesses.Rows[0].Selected = true;
                    isFirstLoad = false;
                }
            }));
        }

        #endregion

        #region 单元格格式化（按需着色，仅可见行触发）

        private void OnCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvProcesses.Rows.Count)
                return;

            if (dgvProcesses.Rows[e.RowIndex].DataBoundItem is not ProcessMonitorRow data)
                return;

            if (e.ColumnIndex == colStatus.Index)
            {
                e.CellStyle.ForeColor = ViewModel.GetStatusColor(data.State);
                e.CellStyle.Font = statusBoldFont;
            }
            else if (e.ColumnIndex == colException.Index)
            {
                if (!string.IsNullOrEmpty(data.ExceptionCode))
                    e.CellStyle.ForeColor = Color.Red;
            }
        }

        #endregion

        #region 行号绘制

        /// <summary>
        /// 绘制行号，异常时静默处理，绝不抛异常避免搞崩 DataGridView 渲染管线
        /// </summary>
        private void OnRowPostPaint(DataGridView dgv, DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                using var brush = new SolidBrush(dgv.RowHeadersDefaultCellStyle.ForeColor);
                string lineNo = (e.RowIndex + 1).ToString();

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using var sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };

                e.Graphics.DrawString(lineNo, e.InheritedRowStyle.Font, brush,
                    new Rectangle(e.RowBounds.Location.X, e.RowBounds.Location.Y,
                        dgv.RowHeadersWidth, e.RowBounds.Height), sf);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加行号时发生错误: {ex.Message}");
            }
        }

        #endregion

        #region 资源释放

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            pollTimer?.Stop();
            feedbackTimer?.Stop();
            statusBoldFont?.Dispose();
            statusBoldFont = null;
            base.OnFormClosing(e);
        }

        #endregion
    }
}