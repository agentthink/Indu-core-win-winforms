using Google.Protobuf.WellKnownTypes;
using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Share.Extensions;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 报警查询页（只读）：查（时间 + 类型两维筛选）/ 导（Btn:Alarm_Export）。
    /// 控件由 Designer 拖拽（手工布局），本文件只做控件 ↔ VM 的接线与数据管道。
    /// </summary>
    public partial class FormAlarmQuery : UIPage
    {
        public AlarmQueryViewModel ViewModel { get; set; }

        #region 表格度量（照 FormAudit 基准）

        private float dgvDpiScale = 1F;
        private const int RowHeightBasis = 36;
        private const int ColumnHeaderHeightBasis = 36;
        private const float GridFontSize = 12F;
        private const int PaginationRowHeightBasis = 37;

        private static readonly string[] PageSizeOptions = { "10", "20", "50", "100" };

        private const int DefaultPageSize = 20;

        #endregion

        public FormAlarmQuery()
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

            SetupDgv();
            SetupFilter();
            SetupPageSize();
            SetupPagination();
            BindViewModel();
            BridgeEvents();
            ViewModel.SearchCommand.Execute(null);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        /// <summary>每次显示重载当前筛选（单例切换后看最新；不重置筛选与页码）。</summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && ViewModel != null)
            {
                ViewModel.RefreshCommand.Execute(null);
            }
        }

        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
        }

        #region 表格初始化

        private void SetupDgv()
        {
            dgvDpiScale = UiDpi.DpiScale;
            var grid = dgvRecords;

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
            grid.Font = new Font("微软雅黑", GridFontSize);
            grid.DefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale);
            grid.RowsDefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 243, 255);
            grid.RowTemplate.Height = (int)Math.Round(RowHeightBasis * dgvDpiScale);
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("微软雅黑", GridFontSize, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 40, 170),
                ForeColor = Color.White
            };
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = (int)Math.Round(ColumnHeaderHeightBasis * dgvDpiScale);

            // 列头取模型 [Description]
            AddTextColumn(grid, nameof(AlarmRecordDto.RaiseTime), 150);
            AddTextColumn(grid, nameof(AlarmRecordDto.Type), 70);
            AddTextColumn(grid, nameof(AlarmRecordDto.Code), 90);
            AddTextColumn(grid, nameof(AlarmRecordDto.Source), 110);
            AddTextColumn(grid, nameof(AlarmRecordDto.Message), 300);
            AddTextColumn(grid, nameof(AlarmRecordDto.ActionGuide), 260);
            AddTextColumn(grid, nameof(AlarmRecordDto.HandleUser), 90);
            AddTextColumn(grid, nameof(AlarmRecordDto.HandleTime), 150);

            grid.CellFormatting += OnTypeCellFormatting;
        }

        private DataGridViewTextBoxColumn AddTextColumn(DataGridView grid, string dataProperty, int width)
        {
            var prop = typeof(AlarmRecordDto).GetProperty(dataProperty);
            var desc = prop?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>().FirstOrDefault();

            var col = new DataGridViewTextBoxColumn
            {
                Name = "col" + dataProperty,
                HeaderText = desc?.Description ?? dataProperty,
                DataPropertyName = dataProperty,
                Width = (int)Math.Round(width * dgvDpiScale),
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            grid.Columns.Add(col);
            return col;
        }

        /// <summary>类型列：异常标红。</summary>
        private void OnTypeCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            if (grid == null || e.RowIndex < 0) return;
            if (grid.Columns[e.ColumnIndex].DataPropertyName != nameof(AlarmRecordDto.Type)) return;

            if (string.IsNullOrEmpty(e.Value as string)) return;
            if ((string)e.Value == AlarmEnumText.Of(AlarmType.Exception))
            {
                e.CellStyle.ForeColor = Color.Red;
            }
            if ((string)e.Value == AlarmEnumText.Of(AlarmType.Warning))
            {
                e.CellStyle.ForeColor = Color.Orange;
            }
        }

        #endregion

        #region 筛选与分页初始化

        private void SetupFilter()
        {
            dtpTimeFrom.Value = ViewModel.TimeFrom;
            dtpTimeTo.Value = ViewModel.TimeTo;

            cmbType.ItemHeight = ConfigFormStyle.ComboItemHeight;

            // 类型下拉：第 0 项「全部」（null）+ 三态成员
            var types = System.Enum.GetValues(typeof(AlarmType))
                .Cast<AlarmType>()
                .Select(type => new { Display = type.GetDescription(), Value = (AlarmType?)type })
                .ToList();
            types.Insert(0, new { Display = "全部", Value = (AlarmType?)null });
            cmbType.DataSource = types;
            cmbType.DisplayMember = "Display";
            cmbType.ValueMember = "Value";
        }

        private void SetupPageSize()
        {
            cmbPageSize.Items.Clear();
            cmbPageSize.Items.AddRange(PageSizeOptions);
            cmbPageSize.ItemHeight = ConfigFormStyle.ComboItemHeight;

            int index = Array.IndexOf(PageSizeOptions, ViewModel.PageSize.ToString(CultureInfo.InvariantCulture));
            cmbPageSize.SelectedIndex = index < 0
                ? Array.IndexOf(PageSizeOptions, DefaultPageSize.ToString(CultureInfo.InvariantCulture))
                : index;
        }

        private void SetupPagination()
        {
            ApplyPaginationScale();

            pagination.PageSize = ViewModel.PageSize;
            SyncPagination();
        }

        /// <summary>分页控件 DPI 补偿（照 FormAudit 清单）。</summary>
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

            btnExport.DataBindings.Add("Enabled", ViewModel, nameof(AlarmQueryViewModel.CanExport), true,
                DataSourceUpdateMode.OnPropertyChanged);

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AlarmQueryViewModel.TotalCount)
                || e.PropertyName == nameof(AlarmQueryViewModel.PageIndex)
                || e.PropertyName == nameof(AlarmQueryViewModel.PageSize))
            {
                SyncPagination();
            }
        }

        /// <summary>控件 → VM 单向桥接；全部具名方法，便于断点与回查。</summary>
        private void BridgeEvents()
        {
            btnSearch.Click += OnSearchClick;
            btnRefresh.Click += OnRefreshClick;
            btnExport.Click += OnExportClick;

            // 分页：控件 → VM 单一入口 GoToPage（页码夹紧与同页幂等都在 VM）
            pagination.PageChanged += OnPaginationPageChanged;

            // 每页条数变化必须回第 1 页重查
            cmbPageSize.SelectedIndexChanged += OnPageSizeChanged;

            // 「全部」归一为 null
            cmbType.SelectedIndexChanged += OnTypeFilterChanged;

            dtpTimeFrom.ValueChanged += (s, e) => ViewModel.TimeFrom = dtpTimeFrom.Value.Date;
            dtpTimeTo.ValueChanged += (s, e) => ViewModel.TimeTo = dtpTimeTo.Value.Date;
            ViewModel.AskExportFilePath = AskExportFilePath;
        }

        private void OnSearchClick(object sender, EventArgs e)
        {
            ViewModel.SearchCommand.Execute(null);
        }

        private void OnRefreshClick(object sender, EventArgs e)
        {
            ViewModel.RefreshCommand.Execute(null);
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
            int size;
            int newSize = int.TryParse(cmbPageSize.SelectedItem as string, out size) && size > 0
                ? size
                : DefaultPageSize;
            if (newSize == ViewModel.PageSize) return;

            ViewModel.PageSize = newSize;
            ViewModel.PageIndex = 1;
            ViewModel.RefreshCommand.Execute(null);
        }

        private void OnTypeFilterChanged(object sender, EventArgs e)
        {
            ViewModel.TypeFilter = cmbType.SelectedValue is AlarmType type ? type : (AlarmType?)null;
        }


        #endregion

        #region 文件对话框（导出）

        private string AskExportFilePath()
        {
            using (var dlg = new SaveFileDialog())
            {
                dlg.Title = "导出报警记录";
                dlg.Filter = "CSV文件|*.csv";
                dlg.FileName = $"Alarm_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
                return dlg.ShowDialog() == DialogResult.OK ? dlg.FileName : null;
            }
        }

        #endregion
    }
}
