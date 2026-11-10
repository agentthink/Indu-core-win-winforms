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
    /// 产品实时数据页（只读）：选表 / 显示数量 / 字段下拉三态排序（升/降/不排序）/
    /// 自动刷新开关 + 间隔 + 手动刷新。菜单节点 ProductLive（Level=100）。
    /// 绑 IList（元素 = 该表 DTO），列由 ProductColumn 元数据构建——页面零表知识，换表只重建列一次。
    /// 控件由 Designer 布局，本文件只做控件 ↔ VM 接线与数据管道（照 FormAlarmQuery 惯例）。
    /// 🔴 新增产品表本页零改动（服务注册 As&lt;IProductDataSource&gt; 即出现在下拉里）。
    /// </summary>
    public partial class FormProductLive : UIPage
    {
        public ProductLiveViewModel ViewModel { get; set; }

        /// <summary>填下拉期间屏蔽事件回环（照报警页 cmbType 惯例）。</summary>
        private bool fillingCombos;

        /// <summary>当前已建列的表键（切表才重建列，刷新不重建——免闪烁/免列宽重置）。</summary>
        private string boundTableKey;

        /// <summary>OnLoad 完成后才让父容器变化影响定时器（构造期 Parent 尚未确定）。</summary>
        private bool initialized;

        public FormProductLive()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            ValidateInjection();
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);

            ProductGridHelper.Setup(dgvRecords, UiDpi.DpiScale);
            SetupFilter();
            BindViewModel();
            // 绑定完毕再赋初始选表：一条链路触发建列元数据 + 首查 + 行通知（ctor 零副作用，无兜底重查）
            ViewModel.SelectedTable = ViewModel.Tables.Count > 0 ? ViewModel.Tables[0] : null;
            FillSortCombos();
            RebuildColumns(ViewModel.CurrentColumns);
            BridgeEvents();

            initialized = true;
            UpdatePollTimer();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        /// <summary>
        /// 页面切换走 mainPanel.Controls.Remove/Add，不是 Visible 变更——移除后 Control.Visible
        /// 仍返回 true（无 Parent 时读自身标志），OnVisibleChanged 因此不触发。起停表与切回刷新挂在这里。
        /// </summary>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (!initialized) return;

            UpdatePollTimer();
            if (IsOnStage)
            {
                ViewModel?.RefreshCommand.Execute(null);
            }
        }

        /// <summary>真正显示 = 挂在主面板上且自身可见。</summary>
        private bool IsOnStage => Parent != null && Visible;

        /// <summary>只有真正显示时才让定时器跑——页面不在台上查库失败会弹用户看不懂的错误框。</summary>
        private void UpdatePollTimer()
        {
            if (ViewModel == null) return;
            pollTimer.Enabled = IsOnStage && ViewModel.IsAutoRefresh;
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
        /// 按列元数据建列（只在切表时调用一次；刷新只换数据源）。
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

        #region 筛选控件初始化

        private void SetupFilter()
        {
            cmbTable.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbTable.DataSource = ViewModel.Tables;
            cmbTable.DisplayMember = nameof(ProductTableOption.Title);
            cmbTable.ValueMember = nameof(ProductTableOption.Key);

            cmbTake.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbTake.DataSource = ViewModel.TakeOptions;
            cmbTake.SelectedIndex = ViewModel.TakeOptions.IndexOf(ViewModel.TakeCount);

            switchAutoRefresh.Active = ViewModel.IsAutoRefresh;

            upDownInterval.Minimum = 5;
            upDownInterval.Maximum = 3600;
            upDownInterval.Step = 5;
            upDownInterval.Value = ViewModel.RefreshInterval;

            // 排序方向两项固定，只建一次（切表只改选中项，不重建 Items）
            cmbSortDir.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbSortDir.Items.Clear();
            cmbSortDir.Items.Add("升序");
            cmbSortDir.Items.Add("降序");
            cmbSortDir.SelectedIndex = ViewModel.IsSortAscending ? 0 : 1;

            // 定时器间隔跟随 VM；起停由 UpdatePollTimer 统一管（OnLoad 末尾调用）
            pollTimer.Interval = Math.Max(1, ViewModel.RefreshInterval) * 1000;
        }

        /// <summary>填排序字段下拉（随选表重建；首项 = 不排序）；方向选中项随之复位。</summary>
        private void FillSortCombos()
        {
            fillingCombos = true;
            try
            {
                cmbSortField.ItemHeight = ConfigFormStyle.ComboItemHeight;
                cmbSortField.DataSource = ViewModel.SortFields;
                cmbSortField.DisplayMember = nameof(ProductColumn.Header);
                cmbSortField.SelectedIndex = ViewModel.SortFields.IndexOf(ViewModel.SelectedSortColumn);

                cmbSortDir.SelectedIndex = ViewModel.IsSortAscending ? 0 : 1;
            }
            finally
            {
                fillingCombos = false;
            }
        }

        #endregion

        #region 绑定与事件

        private void BindViewModel()
        {
            dgvRecords.DataSource = ViewModel.Rows;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ProductLiveViewModel.Rows))
            {
                ProductGridHelper.RebindPreservingScroll(dgvRecords, ViewModel.Rows, true);
            }
        }

        /// <summary>控件 → VM 单向桥接；全部具名方法，便于断点与回查。</summary>
        private void BridgeEvents()
        {
            cmbTable.SelectedIndexChanged += OnTableChanged;
            cmbSortField.SelectedIndexChanged += OnSortFieldChanged;
            cmbSortDir.SelectedIndexChanged += OnSortDirChanged;
            cmbTake.SelectedIndexChanged += OnTakeChanged;
            // 🔴 UISwitch / UIIntegerUpDown 的 ValueChanged 是自定义委托（非 EventHandler），具名方法签名对不上——照流程监控页用 lambda
            switchAutoRefresh.ValueChanged += (s, e) =>
            {
                ViewModel.IsAutoRefresh = switchAutoRefresh.Active;
                UpdatePollTimer();
            };
            upDownInterval.ValueChanged += (s, e) =>
            {
                ViewModel.RefreshInterval = (int)upDownInterval.Value;
                pollTimer.Interval = Math.Max(1, ViewModel.RefreshInterval) * 1000;
            };
            btnManualRefresh.Click += OnManualRefreshClick;
            pollTimer.Tick += OnPollTick;
        }

        private void OnTableChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;

            var key = cmbTable.SelectedValue as string;
            var option = ViewModel.Tables.FirstOrDefault(t => t.Key == key);
            if (option == null || option.Key == (ViewModel.SelectedTable != null ? ViewModel.SelectedTable.Key : null)) return;

            ViewModel.SelectedTable = option;      // VM 变更钩子已自动重查
            FillSortCombos();                      // 排序下拉随新表重建（首项 = 不排序）
            RebuildColumns(ViewModel.CurrentColumns);
        }

        private void OnSortFieldChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;
            ViewModel.SelectedSortColumn = cmbSortField.SelectedItem as ProductColumn;
        }

        private void OnSortDirChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;
            ViewModel.IsSortAscending = cmbSortDir.SelectedIndex == 0;
        }

        private void OnTakeChanged(object sender, EventArgs e)
        {
            if (fillingCombos) return;
            if (cmbTake.SelectedItem is int count)
            {
                ViewModel.TakeCount = count;
            }
        }

        private void OnManualRefreshClick(object sender, EventArgs e)
        {
            ViewModel.RefreshCommand.Execute(null);
        }

        private void OnPollTick(object sender, EventArgs e)
        {
            ViewModel.RefreshCommand.Execute(null);
        }

        #endregion
    }
}
