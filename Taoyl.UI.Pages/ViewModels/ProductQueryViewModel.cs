using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniExcelLibs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Models;
using Taoyl.Application.Product.Query;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>操作符下拉项（Display 进下拉，Value 进 spec）。</summary>
    public class OperatorOption
    {
        public QueryOperator Value { get; set; }

        public string Display { get; set; }

        public override string ToString() => Display;
    }

    /// <summary>
    /// 产品数据查询 ViewModel（只读页，骨架照报警查询页，分页改服务端）。
    /// 依赖 = IEnumerable&lt;IProductDataSource&gt; 集合注入 + IAuthService（导出门禁 Btn:Product_Export）
    /// + IMessageBoxService。筛选 = 单条件（字段+操作符+关键字，首项「不筛选」承担取消语义）
    /// + 时间范围勾选（作用于该表 TimeField 列，含当天全天）。服务端分页（PageIndex/PageSize 直传契约）。
    /// 导出 = 按当前筛选全量（≤ MaxQueryTake）→ ProductSchema.ToDataTable → MiniExcel xlsx。
    /// 筛选项变更不自动重查（照报警页：点「查询」才动）；切表只重置筛选与结果，同样不查库。
    /// </summary>
    public partial class ProductQueryViewModel : ViewModelBase
    {
        /// <summary>筛选字段下拉首项：承担「不筛选」语义（Name=null）。</summary>
        private static readonly ProductColumn NoFilterColumn = new ProductColumn { Name = null, Header = "不筛选" };

        private static readonly OperatorOption[] AllOperators =
        {
            new OperatorOption { Value = QueryOperator.Like, Display = "Like" },
            new OperatorOption { Value = QueryOperator.Equal, Display = "=" },
            new OperatorOption { Value = QueryOperator.NotEqual, Display = "<>" },
            new OperatorOption { Value = QueryOperator.LessThan, Display = "<" },
            new OperatorOption { Value = QueryOperator.LessThanOrEqual, Display = "<=" },
            new OperatorOption { Value = QueryOperator.GreaterThan, Display = ">" },
            new OperatorOption { Value = QueryOperator.GreaterThanOrEqual, Display = ">=" },
        };

        private readonly IReadOnlyDictionary<string, IProductDataSource> sources;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBoxService;
        private readonly IAuditService auditService;
        private readonly IUserSession userSession;
        private bool suppressFilterRebuild;
        private IReadOnlyList<ProductColumn> currentColumns = Array.Empty<ProductColumn>();

        public ProductQueryViewModel(
            IEnumerable<IProductDataSource> allSources,
            IAuthService authService,
            IMessageBoxService messageBoxService,
            IAuditService auditService,
            IUserSession userSession)
        {
            var list = (allSources ?? throw new ArgumentNullException(nameof(allSources))).ToList();
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            sources = list.ToDictionary(s => s.Key);
            Tables = list.Select(s => new ProductTableOption { Key = s.Key, Title = s.Title }).ToList();
            // 构造期零副作用：不选表不查库——初始选表由 View OnLoad 绑定完毕后显式赋值触发
        }

        #region 可观察属性

        /// <summary>产品表下拉项（构造期一次性填好）。</summary>
        [ObservableProperty]
        private List<ProductTableOption> tables = new List<ProductTableOption>();

        /// <summary>当前选表；切换即清筛选并重查（变更钩子）。</summary>
        [ObservableProperty]
        private ProductTableOption selectedTable;

        /// <summary>筛选字段下拉（首项 = 不筛选）；随选表重建。</summary>
        [ObservableProperty]
        private List<ProductColumn> filterFields = new List<ProductColumn>();

        /// <summary>选中筛选列（null/Name=null = 不筛选）。</summary>
        [ObservableProperty]
        private ProductColumn selectedFilterColumn;

        /// <summary>
        /// 操作符下拉（固定 7 个，不随字段类型重建）。
        /// 非文本字段不支持 Like——选中 Like 时由字段变更钩子纠正为 =。
        /// </summary>
        public OperatorOption[] OperatorOptions => AllOperators;

        /// <summary>选中操作符。</summary>
        [ObservableProperty]
        private OperatorOption selectedOperator;

        /// <summary>关键字。</summary>
        [ObservableProperty]
        private string keyword = string.Empty;

        /// <summary>时间筛选勾选（false = 时间两维不进 spec）。</summary>
        [ObservableProperty]
        private bool enableTimeFilter;

        /// <summary>起始时间（含，日期 0 点起）。</summary>
        [ObservableProperty]
        private DateTime timeFrom = DateTime.Now.AddDays(-2);

        /// <summary>结束时间（含，取当天 23:59:59，见 BuildSpec）。</summary>
        [ObservableProperty]
        private DateTime timeTo = DateTime.Now;

        /// <summary>当前页（1 起，服务端分页）。</summary>
        [ObservableProperty]
        private int pageIndex = 1;

        /// <summary>每页行数（真相源：View 侧分页控件初始化时按此值同步）。</summary>
        [ObservableProperty]
        private int pageSize = 20;

        /// <summary>命中总行数。</summary>
        [ObservableProperty]
        private int totalCount;

        /// <summary>总页数（供 View 分页控件）。</summary>
        [ObservableProperty]
        private int totalPages;

        /// <summary>当前页行（IList，元素 = 该表 DTO）；每次查询整体换源。</summary>
        [ObservableProperty]
        private IList records;

        /// <summary>当前表列元数据（View 建列 / 导出列头共用）。</summary>
        public IReadOnlyList<ProductColumn> CurrentColumns => currentColumns;

        /// <summary>是否允许导出（数据出库独立授权 Btn:Product_Export）。</summary>
        public bool CanExport => authService.HasPermission(PermissionCodes.BtnProductExport);

        /// <summary>弹出保存文件对话框并返回目标路径（取消返回 null/空）。文件对话框归 View（照报警页惯例）。</summary>
        public Func<string> AskExportFilePath { get; set; }

        #endregion

        #region 属性变更钩子

        partial void OnSelectedTableChanged(ProductTableOption value)
        {
            if (value == null) return;

            var source = sources[value.Key];

            // 换表先清筛选选择，期间不触发筛选列变更钩子
            suppressFilterRebuild = true;
            SelectedFilterColumn = NoFilterColumn;
            suppressFilterRebuild = false;

            var fields = new List<ProductColumn> { NoFilterColumn };
            fields.AddRange(source.Columns);
            FilterFields = fields;
            currentColumns = source.Columns;

            // 操作符列表固定；切表把选中项重置为等于，关键字清零，回第 1 页
            SelectedOperator = AllOperators.First(o => o.Value == QueryOperator.Equal);
            Keyword = string.Empty;
            PageIndex = 1;

            // 换表只重置筛选与结果，不自动查询——查询页由用户点「查询」触发
            Records = null;
            TotalCount = 0;
            TotalPages = 0;
        }

        partial void OnSelectedFilterColumnChanged(ProductColumn value)
        {
            if (suppressFilterRebuild) return;
            EnsureOperatorSupported();
        }

        partial void OnSelectedOperatorChanged(OperatorOption value)
        {
            EnsureOperatorSupported();
        }

        /// <summary>
        /// 非文本字段不支持 Like：当前选中 Like 时纠正为等于。
        /// 字段切换与手选操作符两条路径共用；纠正结果为等于，不再满足条件即停，幂等不递归。
        /// </summary>
        private void EnsureOperatorSupported()
        {
            if (SelectedOperator == null
                || SelectedOperator.Value != QueryOperator.Like
                || IsTextField(SelectedFilterColumn))
            {
                return;
            }

            SelectedOperator = AllOperators.First(o => o.Value == QueryOperator.Equal);
        }

        /// <summary>可 Like 的字段：真实字段（Name 非空）且声明类型为 string。</summary>
        private static bool IsTextField(ProductColumn column)
        {
            return column != null && column.Name != null && column.Type == typeof(string);
        }

        #endregion

        #region 命令 — 查询

        /// <summary>按当前筛选查询（回第 1 页）。</summary>
        [RelayCommand]
        private void Search()
        {
            PageIndex = 1;
            Load();
        }

        /// <summary>
        /// 跳转到指定页——分页控件 PageChanged 的唯一入口。
        /// 页码夹紧到 [1, TotalPages]；同页且已有结果时不重查（挡住程序性同步 ActivePage 的回环）。
        /// 🔴 不提供 PrevPage / NextPage：控件的语义是「目标页」，相对步长会与已算好的目标页打架。
        /// </summary>
        [RelayCommand]
        private void GoToPage(int pageIndex)
        {
            int target = Math.Max(1, Math.Min(pageIndex, TotalPages > 0 ? TotalPages : 1));
            if (target == PageIndex && Records != null) return;

            PageIndex = target;
            Load();
        }

        /// <summary>重查当前筛选与页码（每页条数变更后用）。</summary>
        [RelayCommand]
        private void Refresh() => Load();

        #endregion

        #region 命令 — 导出

        /// <summary>
        /// 按当前筛选全量导出 xlsx（QueryPage(1, MaxQueryTake)，服务端上限保护；权限门禁 Btn:Product_Export）。
        /// 兜底照报警页：空结果不写文件、写前删目标文件、写后长度校验。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExport))]
        private void Export()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProductExport))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            string path = AskExportFilePath != null ? AskExportFilePath() : null;
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                var source = sources[SelectedTable.Key];
                int total;
                var rows = source.QueryPage(BuildSpec(), 1, ProductDataLimits.MaxQueryTake, out total);

                // 空结果不写文件：与「写失败」混淆，照报警页规则先拦
                if (rows.Count == 0)
                {
                    messageBoxService.ShowWarning("当前筛选条件下没有可导出的数据");
                    return;
                }

                // 命中超过导出上限时明示截断——数据出库不能让用户误以为拿到全量
                if (total > rows.Count)
                {
                    messageBoxService.ShowWarning(
                        $"命中 {total} 行，超过单次导出上限 {ProductDataLimits.MaxQueryTake}，本次仅导出前 {rows.Count} 行");
                }

                if (File.Exists(path)) File.Delete(path);

                MiniExcel.SaveAs(path, ProductSchema.ToDataTable(rows, source.Columns));

                var info = new FileInfo(path);
                if (!info.Exists || info.Length == 0)
                    throw new IOException("导出文件未写入成功（目标文件可能正被其它程序占用）");

                messageBoxService.ShowSuccess($"导出成功，共 {rows.Count} 行", false, 500);

                // 数据出库是受控动作（独立权限码 Btn:Product_Export），成功即留痕；
                // 权限拒绝 / 取消选文件 / 空结果 / 异常均不记账——动作未发生或未完成
                auditService.Record(new AuditEntry(AuditCategory.Product, AuditAction.Export,
                    ExportTarget(path, rows.Count, total), userSession?.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError("导出失败: " + ex.Message);
            }
        }

        #endregion

        #region 私有辅助

        /// <summary>
        /// 审计 Target：<c>产品数据[表名] (文件名)</c>；命中超导出上限时追加 <c>(截断 实导/命中)</c>——
        /// 「实际导走多少行」正是数据出库审计的核心事实。只取文件名，不写全路径。
        /// </summary>
        private string ExportTarget(string path, int exported, int total)
        {
            string table = SelectedTable?.Title;
            string target = string.IsNullOrWhiteSpace(table) ? "产品数据" : "产品数据[" + table + "]";

            string fileName = Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(fileName))
                target += " (" + fileName + ")";

            if (total > exported)
                target += "(截断 " + exported + "/" + total + ")";

            return target;
        }

        /// <summary>
        /// 组装查询条件。时间结束取当天 23:59:59（含当天全天）；
        /// 单条件三者齐备才生效（字段选了但关键字空 = 不筛，避免全表扫一堆误触）。
        /// </summary>
        private ProductQuerySpec BuildSpec()
        {
            var hasFilter = SelectedFilterColumn != null && SelectedFilterColumn.Name != null
                            && SelectedOperator != null
                            && !string.IsNullOrWhiteSpace(Keyword);

            return new ProductQuerySpec
            {
                FieldName = hasFilter ? SelectedFilterColumn.Name : null,
                Operator = hasFilter ? SelectedOperator.Value : (QueryOperator?)null,
                Keyword = hasFilter ? Keyword : null,
                TimeFrom = EnableTimeFilter ? TimeFrom.Date : (DateTime?)null,
                TimeTo = EnableTimeFilter ? TimeTo.Date.AddDays(1).AddSeconds(-1) : (DateTime?)null,
            };
        }

        /// <summary>
        /// 服务端分页加载。🔴 顺序铁律（照报警页）：先回夹 PageIndex，再赋 TotalCount——
        /// Pagination 控件在页码被夹时会触发 PageChanged，反序会造成双重查询。
        /// </summary>
        private void Load()
        {
            if (SelectedTable == null) return;

            try
            {
                var source = sources[SelectedTable.Key];
                int total;
                var rows = source.QueryPage(BuildSpec(), PageIndex, PageSize, out total);

                int pages = PageSize > 0 ? (int)Math.Ceiling(total / (double)PageSize) : 0;
                if (PageIndex > pages) PageIndex = pages > 0 ? pages : 1;

                TotalCount = total;
                TotalPages = pages;
                Records = rows;
            }
            catch (Exception ex)
            {
                // 读侧策略与写侧相反：查询失败必须让用户看见，否则空白页会被误判成"没有记录"
                messageBoxService.ShowError("查询产品数据失败: " + ex.Message);
            }
        }

        #endregion
    }
}
