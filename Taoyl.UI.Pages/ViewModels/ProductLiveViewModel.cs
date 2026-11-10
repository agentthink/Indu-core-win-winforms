using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Models;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>产品表下拉项（Key = 表名，Title = 显示名）。</summary>
    public class ProductTableOption
    {
        /// <summary>表名（= IProductDataSource.Key）。</summary>
        public string Key { get; set; }

        /// <summary>显示名（= IProductDataSource.Title）。</summary>
        public string Title { get; set; }

        public override string ToString() => Title;
    }

    /// <summary>
    /// 产品实时数据 ViewModel（只读页，照 FormAlarmQuery/审计页骨架）。
    /// 依赖 = IEnumerable&lt;IProductDataSource&gt; 集合注入（DeviceManager 同范式：注册多个、一次全给）
    /// + IMessageBoxService。VM 零表知识：表切换/列/排序全部经契约元数据。
    /// 排序三态 = 排序字段下拉（首项「不排序」承担取消语义）+ 升/降方向下拉。
    /// 绑 IList（元素 = 该表 DTO），行随刷新整体换源（View 侧保滚动位置）。
    /// </summary>
    public partial class ProductLiveViewModel : ViewModelBase
    {
        /// <summary>排序下拉首项：承担「取消排序」语义（Name=null → 服务回退时间列倒序）。</summary>
        private static readonly ProductColumn NoSortColumn = new ProductColumn { Name = null, Header = "不排序" };

        private readonly IReadOnlyDictionary<string, IProductDataSource> sources;
        private readonly IMessageBoxService messageBoxService;
        private bool suppressSortRefresh;
        private IReadOnlyList<ProductColumn> currentColumns = Array.Empty<ProductColumn>();

        public ProductLiveViewModel(IEnumerable<IProductDataSource> allSources, IMessageBoxService messageBoxService)
        {
            var list = (allSources ?? throw new ArgumentNullException(nameof(allSources))).ToList();
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            sources = list.ToDictionary(s => s.Key);
            Tables = list.Select(s => new ProductTableOption { Key = s.Key, Title = s.Title }).ToList();
            // 构造期零副作用：不选表不查库——初始选表由 View OnLoad 绑定完毕后显式赋值触发
        }

        #region 可观察属性

        /// <summary>产品表下拉项（构造期一次性填好）。</summary>
        [ObservableProperty]
        private List<ProductTableOption> tables = new List<ProductTableOption>();

        /// <summary>当前选表；切换即重查（变更钩子）。</summary>
        [ObservableProperty]
        private ProductTableOption selectedTable;

        /// <summary>排序字段下拉（首项 = 不排序）；随选表重建。</summary>
        [ObservableProperty]
        private List<ProductColumn> sortFields = new List<ProductColumn>();

        /// <summary>选中排序列（null/Name=null = 取消排序）。</summary>
        [ObservableProperty]
        private ProductColumn selectedSortColumn;

        /// <summary>排序方向（false = 降序默认）。</summary>
        [ObservableProperty]
        private bool isSortAscending = false;

        /// <summary>显示数量档位。</summary>
        [ObservableProperty]
        private List<int> takeOptions = new List<int> { 100, 300, 500, 1000 };

        /// <summary>显示数量（默认 500，仓储收口 1000）。</summary>
        [ObservableProperty]
        private int takeCount = ProductDataLimits.DefaultLiveTake;

        /// <summary>自动刷新开关（默认开）。</summary>
        [ObservableProperty]
        private bool isAutoRefresh = true;

        /// <summary>刷新间隔（秒，默认 15）。</summary>
        [ObservableProperty]
        private int refreshInterval = 15;

        /// <summary>当前行（IList，元素 = 该表 DTO）；每次刷新整体换源。</summary>
        [ObservableProperty]
        private IList rows;

        /// <summary>当前表列元数据（View 建列用；切表时更新）。</summary>
        public IReadOnlyList<ProductColumn> CurrentColumns => currentColumns;

        #endregion

        #region 属性变更钩子

        partial void OnSelectedTableChanged(ProductTableOption value)
        {
            if (value == null) return;

            var source = sources[value.Key];

            // 换表先清排序选择（旧列不再属于新表），期间不触发排序变更重查
            suppressSortRefresh = true;
            SelectedSortColumn = NoSortColumn;
            suppressSortRefresh = false;

            var fields = new List<ProductColumn> { NoSortColumn };
            fields.AddRange(source.Columns);
            SortFields = fields;
            currentColumns = source.Columns;

            Refresh();
        }

        partial void OnSelectedSortColumnChanged(ProductColumn value)
        {
            if (suppressSortRefresh) return;
            Refresh();
        }

        partial void OnIsSortAscendingChanged(bool value)
        {
            if (suppressSortRefresh) return;
            if (SelectedSortColumn != null && SelectedSortColumn.Name != null) Refresh();
        }

        partial void OnTakeCountChanged(int value)
        {
            Refresh();
        }

        #endregion

        #region 命令

        /// <summary>按当前选表/排序/数量取最新数据（读侧失败必须让用户看见，照报警读侧规则）。</summary>
        [RelayCommand]
        private void Refresh()
        {
            if (SelectedTable == null) return;

            try
            {
                var source = sources[SelectedTable.Key];
                var sortField = SelectedSortColumn != null ? SelectedSortColumn.Name : null;
                Rows = source.QueryLatest(TakeCount, sortField, !IsSortAscending);
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError("查询产品数据失败: " + ex.Message);
            }
        }

        #endregion
    }
}
