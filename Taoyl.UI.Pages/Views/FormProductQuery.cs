using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Product.Models;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.Controls;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 产品数据查询页（只读）：选表 / 单条件筛选（字段+操作符+关键字）/ 时间范围勾选 /
    /// 服务端分页 / 导出 xlsx（Btn:Product_Export 独立授权）。菜单节点 ProductQuery（Level=100）。
    /// 绑 IList（元素 = 该表 DTO），列由 ProductColumn 元数据构建——页面零表知识，换表只重建列一次。
    /// 控件由 Designer 布局，本文件只做控件 ↔ VM 接线与数据管道（照 FormAlarmQuery 惯例）。
    /// 🔴 新增产品表本页零改动（服务注册 As&lt;IProductDataSource&gt; 即出现在下拉里）。
    /// </summary>
    public partial class FormProductQuery : UIPage
    {
        public ProductQueryViewModel ViewModel { get; set; }

        private static readonly string[] PageSizeOptions = { "10", "20", "50", "100" };

        /// <summary>填下拉期间屏蔽事件回环（照报警页惯例）。</summary>
        private bool fillingCombos;

        /// <summary>当前已建列的表键（切表才重建列，查询不重建）。</summary>
        private string boundTableKey;

        public FormProductQuery()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
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

            ProductGridHelper.Setup(dgvRecords, UiDpi.DpiScale);
            SetupFilter();
            SetupPageSize();
            SetupPagination();
            BindViewModel();
            // 绑定完毕再赋初始选表：只重建筛选元数据与列，不查库（查询由用户点「查询」触发）
            ViewModel.SelectedTable = ViewModel.Tables.Count > 0 ? ViewModel.Tables[0] : null;
            FillFilterCombos();
            RebuildColumns(ViewModel.CurrentColumns);
            BridgeEvents();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        /// <summary>
        /// Ctrl+C / Ctrl+Insert 复制当前单元格（FullRowSelect 下原生会复制整行）；
        /// Ctrl+Shift+C 放行，保留原生整行复制入口。判定与复制见 UiDgvHelper.TryCopyFocusedCell。
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (UiDgvHelper.TryCopyFocusedCell(ref msg, keyData, dgvRecords))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
        }

        #region 建列（换表时一次）

        /// <summary>
        /// 按列元数据建列（只在切表时调用一次；查询只换数据源）。
        /// 列样式细节全在 ProductGridHelper.BuildColumn。
        /// </summary>
        private void RebuildColumns(IReadOnlyList<ProductColumn> columns)
        {
            if (ViewModel.SelectedTable == null || ViewModel.SelectedTable.Key == boundTableKey) return;

            dgvRecords.SuspendLayout();
            dgvRecords.Columns.Clear();
            foreach (var column in columns)
            {
                dgvRecords.Columns.Add(ProductGridHelper.BuildColumn(column, UiDpi.DpiScale));
            }
            boundTableKey = ViewModel.SelectedTable.Key;
            dgvRecords.ResumeLayout();
        }

        #endregion

        #region 筛选与分页初始化

        private void SetupFilter()
        {
            cmbTable.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbTable.DataSource = ViewModel.Tables;
            cmbTable.DisplayMember = nameof(ProductTableOption.Title);
            cmbTable.ValueMember = nameof(ProductTableOption.Key);

            txtKeyword.Text = ViewModel.Keyword;

            chkTimeEnable.Checked = ViewModel.EnableTimeFilter;
            dtpTimeFrom.Value = ViewModel.TimeFrom;
            dtpTimeTo.Value = ViewModel.TimeTo;
            dtpTimeFrom.Enabled = ViewModel.EnableTimeFilter;
            dtpTimeTo.Enabled = ViewModel.EnableTimeFilter;
        }

        /// <summary>填筛选下拉（随选表重建；首项 = 不筛选）。操作符列表固定，选中项由 SyncOperatorSelection 同步。</summary>
        private void FillFilterCombos()
        {
            fillingCombos = true;
            try
            {
                cmbField.ItemHeight = ConfigFormStyle.ComboItemHeight;
                cmbField.DataSource = ViewModel.FilterFields;
                cmbField.DisplayMember = nameof(ProductColumn.Header);
                cmbField.SelectedIndex = ViewModel.FilterFields.IndexOf(ViewModel.SelectedFilterColumn);

                cmbOperator.ItemHeight = ConfigFormStyle.ComboItemHeight;
                cmbOperator.DataSource = ViewModel.OperatorOptions;
                cmbOperator.DisplayMember = nameof(OperatorOption.Display);
                cmbOperator.SelectedItem = ViewModel.SelectedOperator;
            }
            finally
            {
                fillingCombos = false;
            }
        }

        /// <summary>同步操作符选中项（VM 侧纠正非法组合时调用）。</summary>
        private void SyncOperatorSelection()
        {
            fillingCombos = true;
            try
            {
                cmbOperator.SelectedItem = ViewModel.SelectedOperator;
            }
            finally
            {
                fillingCombos = false;
            }
        }

        private void SetupPageSize()
        {
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(PageSizeOptions);
            cmbPageSize.ItemHeight = ConfigFormStyle.ComboItemHeight;

            int index = Array.IndexOf(PageSizeOptions, ViewModel.PageSize.ToString());
            cmbPageSize.SelectedIndex = index < 0
                ? Array.IndexOf(PageSizeOptions, ProductDataLimits.DefaultQueryPageSize.ToString())
                : index;
        }

        private void SetupPagination()
        {
            ApplyPaginationScale();

            pagination.PageSize = ViewModel.PageSize;
            SyncPagination();
        }

        /// <summary>分页控件 DPI 补偿（照 FormAlarmQuery 清单）。</summary>
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
        }

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
            dgvRecords.DataSource = ViewModel.Records;

            btnExport.DataBindings.Add("Enabled", ViewModel, nameof(ProductQueryViewModel.CanExport), true,
                DataSourceUpdateMode.OnPropertyChanged);

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductQueryViewModel.TotalCount)
                || e.PropertyName == nameof(ProductQueryViewModel.PageIndex)
                || e.PropertyName == nameof(ProductQueryViewModel.PageSize))
            {
                SyncPagination();
            }
            else if (e.PropertyName == nameof(ProductQueryViewModel.Records))
            {
                ProductGridHelper.RebindPreservingScroll(dgvRecords, ViewModel.Records);
            }
            else if (e.PropertyName == nameof(ProductQueryViewModel.SelectedOperator))
            {
                // 非文本字段选中 Like 时 VM 会纠正为 = ——把纠正结果同步回下拉
                SyncOperatorSelection();
            }
        }

        /// <summary>控件 → VM 单向桥接；全部具名方法，便于断点与回查。</summary>
        private void BridgeEvents()
        {
            btnSearch.Click += OnSearchClick;
            btnExport.Click += OnExportClick;
            pagination.PageChanged += OnPaginationPageChanged;
            cmbPageSize.SelectedIndexChanged += OnPageSizeChanged;
            txtKeyword.TextChanged += OnKeywordChanged;
            // 🔴 UICheckBox.CheckedChanged 委托签名存疑（仓内只有 lambda 先例），用 lambda 挂接
            chkTimeEnable.CheckedChanged += (s, e) =>
            {
                ViewModel.EnableTimeFilter = chkTimeEnable.Checked;
                dtpTimeFrom.Enabled = chkTimeEnable.Checked;
                dtpTimeTo.Enabled = chkTimeEnable.Checked;
            };
            dtpTimeFrom.ValueChanged += (s, e) => ViewModel.TimeFrom = dtpTimeFrom.Value.Date;
            dtpTimeTo.ValueChanged += (s, e) => ViewModel.TimeTo = dtpTimeTo.Value.Date;
            ViewModel.AskExportFilePath = AskExportFilePath;

            // 表/字段/操作符三下拉走组合框事件，控件值变化时不能抢在 VM 前面（guard fillingCombos）
            cmbTable.SelectedIndexChanged += OnTableChanged;
            cmbField.SelectedIndexChanged += OnFilterFieldChanged;
            cmbOperator.SelectedIndexChanged += OnOperatorChanged;
        }

        private void OnTableChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;

            var key = cmbTable.SelectedValue as string;
            var option = ViewModel.Tables.FirstOrDefault(t => t.Key == key);
            if (option == null || option.Key == (ViewModel.SelectedTable != null ? ViewModel.SelectedTable.Key : null)) return;

            ViewModel.SelectedTable = option;      // VM 变更钩子已清筛选并清空结果
            FillFilterCombos();
            RebuildColumns(ViewModel.CurrentColumns);
        }

        private void OnFilterFieldChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;
            ViewModel.SelectedFilterColumn = cmbField.SelectedItem as ProductColumn;
        }

        private void OnOperatorChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;
            ViewModel.SelectedOperator = cmbOperator.SelectedItem as OperatorOption;
        }

        private void OnKeywordChanged(object sender, EventArgs e)
        {
            ViewModel.Keyword = txtKeyword.Text;
        }

        private void OnSearchClick(object sender, EventArgs e)
        {
            ViewModel.SearchCommand.Execute(null);
        }

        private void OnExportClick(object sender, EventArgs e)
        {
            ViewModel.ExportCommand.Execute(null);
        }

        private void OnPaginationPageChanged(object sender, EventArgs e)
        {
            // 页码夹紧 + 同页幂等都在 VM 的 GoToPage 里，View 只负责报目标页
            ViewModel.GoToPageCommand.Execute(pagination.ActivePage);
        }

        private void OnPageSizeChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;

            int size;
            int newSize = int.TryParse(cmbPageSize.SelectedItem as string, out size) && size > 0
                ? size
                : ProductDataLimits.DefaultQueryPageSize;
            if (newSize == ViewModel.PageSize) return;

            ViewModel.PageSize = newSize;
            ViewModel.PageIndex = 1;
            ViewModel.RefreshCommand.Execute(null);
        }

        #endregion

        #region 文件对话框（导出）

        private string AskExportFilePath()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "导出产品数据";
                dlg.Filter = "Excel 文件|*.xlsx";
                dlg.FileName = (ViewModel.SelectedTable != null ? ViewModel.SelectedTable.Key : "ProductData")
                               + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".xlsx";
                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
            }
        }

        #endregion
    }
}
