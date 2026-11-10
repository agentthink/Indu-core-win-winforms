using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models.Dtos;
using Taoyl.Application.Share.Extensions;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 审计留痕查询页（只读页）。
    ///
    /// 只读铁律（方案 §4）：本页**不提供**任何删除 / 修改 / 清空入口——防篡改第一道墙在 UI 层也照样立着，
    /// 能做的只有三件事：查（时间 + 页面二维筛选、分页）、钻（主从明细）、导（受独立权限码管控，且导出本身要留痕）。
    ///
    /// 分层：View 只做「控件 ↔ VM」的哑接线与两个文件对话框（导入导出归 View 是项目惯例）；
    /// 全部业务判断（权限、筛选、分页、审计投递）在 <see cref="AuditQueryViewModel"/> 内完成。
    ///
    /// 实现规则与 FormProcessMonitor / 用户管理 / 角色管理对齐（三处曾被有意统一）：
    /// DGV 度量抱在实例字段上按 DPI 补偿、列头文案反射模型的 [Description]、下拉项高统一取
    /// <see cref="ConfigFormStyle.ComboItemHeight"/>——**不硬编码中文列头，也不散落裸物理像素**。
    ///
    /// 筛选条件**刻意只有两个**（时间区间 + 页面）：条件越多越没人用；按人 / 按动作 / 按结果细筛
    /// 交给「导出 CSV」之后在表格里做——那才是擅长干这个的地方。
    /// </summary>
    public partial class FormAudit : UIPage
    {
        #region 依赖注入

        public AuditQueryViewModel ViewModel { get; set; }

        #endregion

        #region 表格度量

        /// <summary>
        /// DGV 的 DPI 缩放系数（System aware 下窗体按物理像素布局，字体/行高/列宽需手动补偿）。
        /// 系数取 Program.cs 入口按 Graphics.DpiX 算好的 GlobalFontScale，**不要用 Control.DeviceDpi**
        /// ——实测返回 96 不可靠（与 FormProcessMonitor 同一条）。
        ///
        /// 为什么做成实例字段而不是每处直接 UiDpi.Scale(...)：列宽要按系数加权，
        /// 且这是"本页两个 DGV 的度量"——抱在实例上，下面的列构造 / 网格配置 / 格式化才自然是实例方法
        /// （FormProcessMonitor 的形态即如此，页内不再出现只为躲开实例状态的 static 方法）。
        /// </summary>
        private float dgvDpiScale = 1F;

        /// <summary>数据行高 100% 设计基准值</summary>
        private const int RowHeightBasis = 36;

        /// <summary>列头高 100% 设计基准值</summary>
        private const int ColumnHeaderHeightBasis = 36;

        /// <summary>网格内文字字号（物理字号：列头由 SunnyUI 按 GlobalFontScale 绘制，物理放大会双重放大）</summary>
        private const float GridFontSize = 12F;

        /// <summary>
        /// 分页条所在行的高度（100% 设计基准值）。
        /// 分页条内的按钮全是 Dock=Fill，**高度吃的是这一行** ⇒ 行高必须与其余度量同步按 DPI 放大，
        /// 否则高 DPI 下就是"按钮只横着长、竖着不动"。值 = 按钮 29 + 上下 Margin 各 4，
        /// 与 DGV 的行高基准（36）视觉齐平。
        /// </summary>
        private const int PaginationRowHeightBasis = 37;

        #endregion

        public FormAudit()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);

            cmbCategory.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbPageSize.ItemHeight = ConfigFormStyle.ComboItemHeight;
            if (UiDpi.DpiScale >= 1.5F)
            {
                dtpTimeFrom.SizeMultiple = 2;
                dtpTimeTo.SizeMultiple = 2;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            ValidateInjection();
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);
            SetupDgv();
            SetupFilter();
            SetupPageSize();
            SetupPagination();
            BindViewModel();
            BridgeEvents();
            ViewModel.SearchCommand.Execute(null);
        }

        /// <summary>
        /// 首帧显示完成（Form.Shown 每实例仅触发一次，与 OnLoad 配对）：WinForms 保证 OnShown 前
        /// 窗口定位/尺寸消息（WM_SIZE/WM_WINDOWPOSCHANGED）已处理完毕——此处恢复禁绘 + 整页重绘，
        /// 终态一次成帧。缓存复用切回（Remove→Add→Show）不重走 Load/Shown，本恢复不重复执行。
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        /// <summary>
        /// 每次显示时重载当前页（支持单例切换后看到最新留痕；不重置筛选与页码）。
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && ViewModel != null)
            {
                ViewModel.RefreshCommand.Execute(null);
            }
        }

        /// <summary>
        /// 校验 ViewModel 注入，失败即抛异常阻止继续加载。
        /// </summary>
        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
        }

        #region 表格初始化

        private void SetupDgv()
        {
            dgvDpiScale = UiDpi.DpiScale;

            SetupReadOnlyGrid(dgvOperations);
            SetupReadOnlyGrid(dgvChanges);

            dgvOperations.Columns.Clear();
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.Time), 160);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.UserName), 100);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.RoleNames), 130);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.RoleLevel), 60);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.Category), 80);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.Action), 90);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.Target), 260);
            AddTextColumn(dgvOperations, typeof(AuditOperationDto), nameof(AuditOperationDto.Result), 70);

            // 明细列宽按内容特性分配：旧/新值最大（配置项与地址表明细可能很长）
            dgvChanges.Columns.Clear();
            AddTextColumn(dgvChanges, typeof(AuditFieldChangeDto), nameof(AuditFieldChangeDto.FieldPath), 200);
            AddTextColumn(dgvChanges, typeof(AuditFieldChangeDto), nameof(AuditFieldChangeDto.ChangeType), 80);
            AddTextColumn(dgvChanges, typeof(AuditFieldChangeDto), nameof(AuditFieldChangeDto.OldValue), 260);
            AddTextColumn(dgvChanges, typeof(AuditFieldChangeDto), nameof(AuditFieldChangeDto.NewValue), 260);

            // 结果列着色：审计页的用法是"翻页扫一眼有没有异常"，成功与失败同色会白翻
            dgvOperations.CellFormatting += OnOperationCellFormatting;
        }

        /// <summary>
        /// 只读网格统一配置（DPI 三件套：单元格字体原生绘制物理放大、行高/列头按系数、列头字体保持物理字号）。
        /// </summary>
        private void SetupReadOnlyGrid(UIDataGridView grid)
        {
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.BackgroundColor = Color.White;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            grid.ScrollBars = ScrollBars.Both;

            // 控件 Font 保持物理字号：UIDataGridView 列头字体跟随控件 Font 且列头绘制走 GlobalFontScale，
            // 物理放大会双重放大（与 FormProcessMonitor 的 DGV 同一条规则）
            grid.Font = new Font("微软雅黑", GridFontSize);

            // 单元格字体物理放大（原生绘制不走 GlobalFontScale）；Designer 里的显式宋体优先于控件 Font，必须一并重建
            grid.DefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale);
            grid.RowsDefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale);

            grid.RowTemplate.Height = (int)Math.Round(RowHeightBasis * dgvDpiScale);

            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("微软雅黑", GridFontSize, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 40, 170),
                ForeColor = Color.White
            };
            grid.EnableHeadersVisualStyles = false;

            // 列头高度：弃用 AutoSize（运行时按 DPI 改字体后不重算必切），EnableResizing + 按 DPI 显式补偿
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = (int)Math.Round(ColumnHeaderHeightBasis * dgvDpiScale);
        }

        /// <summary>
        /// 加一个只读文本列。列头文案反射模型的 [Description]（无特性时回退属性名），
        /// 与 FormProcessMonitor / 用户管理 / 角色管理同规则，**页内不硬编码中文列头**。
        /// Fill 模式下 Width 即占比权重，故直接以内容预期长度当权重用。
        /// </summary>
        private DataGridViewTextBoxColumn AddTextColumn(UIDataGridView grid, Type rowType, string dataProperty,
            int width, DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var prop = rowType.GetProperty(dataProperty);
            var desc = prop?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>().FirstOrDefault();

            var col = new DataGridViewTextBoxColumn
            {
                Name = "col" + dataProperty,
                HeaderText = desc?.Description ?? dataProperty,
                DataPropertyName = dataProperty,
                Width = (int)Math.Round(width * dgvDpiScale),
                DefaultCellStyle = { Alignment = align },
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            grid.Columns.Add(col);
            return col;
        }

        /// <summary>
        /// 结果列着色：失败标红（其余走主题默认色）。
        /// </summary>
        private void OnOperationCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].DataPropertyName != nameof(AuditOperationDto.Result)) return;

            string result = e.Value as string;

            // 判据取"不等于成功"而不是"等于失败"：读侧收到的是落库文案（string，见 AuditOperationDto），
            // 将来若新增结果态，这里不必回来改；空值保持默认色
            if (string.IsNullOrEmpty(result) || result == AuditResult.Success.GetDescription()) return;

            e.CellStyle.ForeColor = Color.Red;
        }

        #endregion

        #region 筛选控件初始化

        /// <summary>
        /// 日期初值取 VM（真相源），「页面」下拉的数据源取 <see cref="AuditCategory"/> 的全部成员。
        /// 必须在 BridgeEvents 之前调用——先设 SelectedIndex 会触发 SelectedIndexChanged。
        /// </summary>
        private void SetupFilter()
        {
            dtpTimeFrom.Value = ViewModel.TimeFrom;
            dtpTimeTo.Value = ViewModel.TimeTo;

            FillCategoryCombo();
        }

        /// <summary>
        /// 填「页面」下拉：第 0 项「全部」（值为 null）+ 枚举全成员（显示名 = [Description]）。
        ///
        /// 范式与 <see cref="Taoyl.UI.FormMain.FormMain"/> 顶栏的语言下拉完全一致：匿名类型承载
        /// 「显示名 + 值」，`DisplayMember` / `ValueMember` 交给控件——**不自己包一个"选项类"**。
        /// 下拉项就只是一张"文案 ↔ 值"的对照表，够用即止；包装类还得多写一个 `ToString` 重写，
        /// 净收益为零。
        ///
        /// **不做"文案 → 枚举"的反查**：文案是落库契约（见 AuditConstants），一旦微调，反查静默失效。
        /// 「全部」的 Value 为 null，与查询契约的空值语义（该维度不参与筛选）直接对齐。
        /// </summary>
        private void FillCategoryCombo()
        {
            // 与 FormMain 顶栏语言下拉同一范式：匿名类型承载「显示名 + 值」，控件按
            // DisplayMember / ValueMember 取用。⚠️ 哨兵项与枚举项必须是**同一个匿名类型**
            // （属性名 / 类型 / 顺序全一致，故 Value 显式写成 AuditCategory?），否则 Insert 编译不过。
            var items = Enum.GetValues(typeof(AuditCategory))
                .Cast<AuditCategory>()
                .Select(category => new { Display = category.GetDescription(), Value = (AuditCategory?)category })
                .ToList();

            items.Insert(0, new { Display = "全部", Value = (AuditCategory?)null });

            cmbCategory.DataSource = items;
            cmbCategory.DisplayMember = "Display";
            cmbCategory.ValueMember = "Value";
        }

        /// <summary>取「页面」下拉当前选中值（「全部」→ null，与查询契约的空值语义一致）。</summary>
        private AuditCategory? SelectedCategory()
        {
            return cmbCategory.SelectedValue is AuditCategory category ? category : (AuditCategory?)null;
        }

        #endregion

        #region 分页控件接线

        /// <summary>
        /// 每页条数下拉初始化。
        ///
        /// 🔴 **刻意不给"全部"档**：本页是服务端分页（SQL Skip/Take），"全部"等于把长期只增的留档表
        /// 一次性拉进内存；看全量有专门的出口——「导出」（独立权限码 Btn:Audit_Export、导出动作本身留痕，
        /// 且服务端另有 5 万头表 / 20 万展开行双上限）。流程监控页之所以有"全部"，是因为那页是内存分页，
        /// 代价完全不同——不要把两页的档位当成同一回事照抄。
        /// 必须排在 BridgeEvents 之前：先设 SelectedIndex 会触发 SelectedIndexChanged。
        /// </summary>
        private void SetupPageSize()
        {
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(PageSizeOptions);
            cmbPageSize.SelectedIndex = IndexOfPageSize(ViewModel.PageSize);
        }

        /// <summary>
        /// 分页控件初始化：先按 DPI 通知尺寸，再把真相源（VM）的每页条数推给控件。
        /// 必须在 BridgeEvents 之前调用——本方法内会写 ActivePage，若 PageChanged 此时已挂上，
        /// 属性联动会多跑一次查询（<see cref="Controls.Pagination"/> 对"同值赋值"不发事件，
        /// 已挡掉绝大部分噪音，但顺序仍按老规矩守着）。
        /// </summary>
        private void SetupPagination()
        {
            ApplyPaginationScale();

            // 真相源在 VM：控件属性的初值只是设计期预览，运行时以 VM 为准
            pagination.PageSize = ViewModel.PageSize;
            SyncPagination();
        }

        /// <summary>
        /// 分页控件的 DPI 补偿（**缩放策略归宿主**，见 <see cref="Controls.Pagination"/> 类注释）。
        ///
        /// 为什么在页面里做而不是控件内部自助：控件是通用件，它不该假设宿主用哪套 DPI 方案
        /// （今天是 UiDpi.Scale，明天可能换成宿主自己的缩放表）。尺寸属性的语义是"我要多宽"，
        /// 至于这个数怎么来的，是宿主的事——控件里一旦内建缩放，换宿主就得改控件。
        ///
        /// 两件事：
        /// ①8 个尺寸属性按系数放大（属性默认值即 100% 设计基准）；
        /// ②分页行高按同一系数放大 —— 分页条里的按钮全是 Dock=Fill，**高度吃的是行高**，
        ///   行高不放大就等于没放大（Designer 里的 37 是基准值，不是运行值）。
        ///
        /// ⚠️ 字体的两个属性（PageButtonFont / NavButtonFont）刻意不动：DPI 铁律下字号保持物理值，
        /// SunnyUI 自己会按 GlobalFontScale 绘制文字，这里再放大会双重放大。
        /// </summary>
        private void ApplyPaginationScale()
        {
            pagination.PrevNextButtonWidth = UiDpi.Scale(75);
            pagination.PageButtonWidth = UiDpi.Scale(29);
            pagination.PageButtonWidthMedium = UiDpi.Scale(36);
            pagination.PageButtonWidthLong = UiDpi.Scale(52);
            pagination.EllipsisWidth = UiDpi.Scale(29);
            pagination.JumpLabelWidth = UiDpi.Scale(21);
            pagination.JumpBoxWidth = UiDpi.Scale(53);
            pagination.JumpButtonWidth = UiDpi.Scale(61);

            var paginationRow = masterShowTableLayoutPanel.RowStyles[1];
            paginationRow.SizeType = SizeType.Absolute;
            paginationRow.Height = UiDpi.Scale(PaginationRowHeightBasis);
        }

        /// <summary>
        /// VM 分页状态 → 控件（单向）。
        /// 反向（控件 → VM）由 PageChanged 事件负责，两向分开写而不是挂 DataBindings：
        /// 控件会**单方面夹取**越界页码，并在夹取后主动发 PageChanged 让本页把 VM 回正
        /// （见 Pagination.RenderAndNotifyIfClamped）——用 DataBindings 反而会与这条回正路径打架。
        /// </summary>
        private void SyncPagination()
        {
            pagination.TotalCount = ViewModel.TotalCount;
            pagination.PageSize = ViewModel.PageSize;
            pagination.ActivePage = ViewModel.PageIndex;
        }

        #endregion

        #region 绑定与事件

        private void BindViewModel()
        {
            // 数据源绑定：BindingList 与 DataGridView 原生配合最佳（主从两表同源同绑）
            dgvOperations.DataSource = ViewModel.Operations;
            dgvChanges.DataSource = ViewModel.FieldChanges;

            // 导出按钮权限：由 VM 的 CanExport 独立驱动（能查 ≠ 能导），本页唯一受权限码管控的按钮
            btnExport.DataBindings.Add("Enabled", ViewModel, nameof(AuditQueryViewModel.CanExport), true,
                DataSourceUpdateMode.OnPropertyChanged);

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 只跟与分页相关的三个属性。TotalPages 刻意不跟：总页数由控件按 TotalCount/PageSize 自算，
        /// 多喂一个就会出现"控件算的"与"VM 算的"两份真相（本页无删除功能，总条数只增不减，
        /// 页码不可能越上界，故也不需要靠 TotalPages 做回夹）。
        /// </summary>
        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AuditQueryViewModel.TotalCount)
                || e.PropertyName == nameof(AuditQueryViewModel.PageIndex)
                || e.PropertyName == nameof(AuditQueryViewModel.PageSize))
            {
                SyncPagination();
            }
        }

        private void BridgeEvents()
        {
            // 按钮点击 → 命令
            btnSearch.Click += (s, e) => ViewModel.SearchCommand.Execute(null);
            btnRefresh.Click += (s, e) => ViewModel.RefreshCommand.Execute(null);
            btnExport.Click += (s, e) => ViewModel.ExportCommand.Execute(null);

            // 分页：控件 → VM 单一入口 GoToPage（页码夹紧与同页幂等都在 VM，
            // 避免与控件已算好的目标页重复翻页）
            pagination.PageChanged += (s, e) => ViewModel.GoToPageCommand.Execute(pagination.ActivePage);

            // 每页条数：档位变了必须回第 1 页重查——服务端分页的固有约束，不是可选项
            // （停在第 5 页时把每页从 20 改成 100，第 5 页在新分页下已不存在，会查出空白页）。
            // 顺序有意为之：先改 VM 的两个属性（真相源），控件由 OnViewModelPropertyChanged 单向同步过去。
            cmbPageSize.SelectedIndexChanged += (s, e) =>
            {
                int newSize = PageSizeOf(cmbPageSize);
                if (newSize == ViewModel.PageSize) return;

                ViewModel.PageSize = newSize;
                ViewModel.PageIndex = 1;
                ViewModel.RefreshCommand.Execute(null);
            };

            // 筛选：控件 → VM 属性（「全部」归一为 null，与查询契约的空值语义一致）
            cmbCategory.SelectedIndexChanged += (s, e) => ViewModel.CategoryFilter = SelectedCategory();

            // 日期取 .Date：审计时间戳精确到秒，用户选的是"哪一天"，把时分秒带进来会凭空截掉当天尾部记录
            dtpTimeFrom.ValueChanged += (s, e) => ViewModel.TimeFrom = dtpTimeFrom.Value.Date;
            dtpTimeTo.ValueChanged += (s, e) => ViewModel.TimeTo = dtpTimeTo.Value.Date;

            // 主表选中 → 从表钻取（VM 的 OnSelectedOperationChanged 自动加载明细）
            dgvOperations.SelectionChanged += (s, e) =>
            {
                if (dgvOperations.Rows.Count == 0 || dgvOperations.SelectedRows.Count == 0)
                {
                    ViewModel.SelectedOperation = null;
                    return;
                }

                ViewModel.SelectedOperation = dgvOperations.SelectedRows[0].DataBoundItem as AuditOperationDto;
            };

            // VM 委托：View 层实现文件对话框，VM 零耦合（与 PLC 调试页导入导出同惯例）
            ViewModel.AskExportFilePath = AskExportFilePath;
        }

        #endregion

        #region 文件对话框（导出）

        private string AskExportFilePath()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "导出审计记录";
                dlg.Filter = "CSV文件|*.csv";
                dlg.FileName = $"Audit_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
            }
        }

        #endregion

        #region 私有辅助

        /// <summary>每页条数档位（顺序即下拉顺序）。**刻意没有"全部"**——理由见 <see cref="SetupPageSize"/>。</summary>
        private static readonly string[] PageSizeOptions = { "10", "20", "50", "100" };

        /// <summary>每页条数不在档位内时的兜底值（= "20"，与 ViewModel.PageSize 的初值一致）。</summary>
        private const int DefaultPageSize = 20;

        /// <summary>取每页条数下拉的选中值（非法项回落到 <see cref="DefaultPageSize"/>，避免异常态把查询带偏）。</summary>
        private static int PageSizeOf(UIComboBox combo)
        {
            int size;
            return int.TryParse(combo.SelectedItem as string, out size) && size > 0
                ? size
                : DefaultPageSize;
        }

        /// <summary>VM 的每页条数 → 下拉档位下标（不在档位内则退回默认档，否则下拉会显示空白）。</summary>
        private static int IndexOfPageSize(int pageSize)
        {
            int index = Array.IndexOf(PageSizeOptions, pageSize.ToString(CultureInfo.InvariantCulture));
            return index < 0
                ? Array.IndexOf(PageSizeOptions, DefaultPageSize.ToString(CultureInfo.InvariantCulture))
                : index;
        }

        #endregion
    }
}
