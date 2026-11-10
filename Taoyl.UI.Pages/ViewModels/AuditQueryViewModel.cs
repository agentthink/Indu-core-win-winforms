using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.Audit.Models.Dtos;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.FileUtil;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages.Extensions;
using Taoyl.UI.Pages.Models;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 审计留痕查询 ViewModel（只读页）。
    ///
    /// 只读铁律（方案 §4）：无删除 / 无修改 / 无清空——结构上不给口子。
    /// 本 VM 只暴露 查询 / 钻取 / 导出 三类命令，且导出需独立权限码（能查 ≠ 能导）。
    /// 唯一的写侧行为是导出时投递一条审计——"谁把审计数据导走了"正是最该留痕的那问。
    ///
    /// 分层：VM 只依赖契约 <see cref="IAuditQueryService"/>，不引用审计实现；
    /// 实现与 DI 注册在组合根完成（依赖倒置，与配置/设备域挂点同惯例）。
    /// 导出复用通用 <see cref="ICsvService"/>，不自行造 CSV 轮子。
    /// </summary>
    public partial class AuditQueryViewModel : ViewModelBase
    {
        private readonly IAuditQueryService queryService;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBoxService;
        private readonly ICsvService csvService;
        private readonly IAuditService auditService;
        private readonly IUserSession userSession;

        #region 可观察属性 — 头表（主）

        /// <summary>头表行（WinForms 最优绑定：BindingList）</summary>
        [ObservableProperty]
        private BindingList<AuditOperationDto> operations = new BindingList<AuditOperationDto>();

        /// <summary>当前选中操作，变更时自动触发明细加载</summary>
        [ObservableProperty]
        private AuditOperationDto selectedOperation;

        #endregion

        #region 可观察属性 — 明细表（从）

        /// <summary>当前选中操作的字段级变更明细</summary>
        [ObservableProperty]
        private BindingList<AuditFieldChangeDto> fieldChanges = new BindingList<AuditFieldChangeDto>();

        #endregion

        #region 可观察属性 — 筛选（二维）

        /// <summary>起始时间（View 用日期选择器绑定）</summary>
        [ObservableProperty]
        private DateTime timeFrom = DateTime.Today.AddDays(-7);

        /// <summary>结束时间</summary>
        [ObservableProperty]
        private DateTime timeTo = DateTime.Today;

        /// <summary>
        /// 页面（null = 全部）。收**可空枚举**：与 <see cref="AuditQueryFilter"/> 同类型，
        /// 中间不再有"字符串 ↔ 枚举"的一次转换；View 侧下拉项即枚举成员（显示名取 [Description]），
        /// 选中"全部"时置 null。
        /// </summary>
        [ObservableProperty]
        private AuditCategory? categoryFilter;

        // 筛选条件刻意只留「时间 + 页面」两维，操作者 / 动作 / 结果三维已从 UI 撤掉：
        // 条件越多越没人用，细筛交给「导出 CSV」之后的表格去做。
        // 契约 AuditQueryFilter 上的 UserName / Action / Result **保留不动**（服务端能力不减，
        // 删字段要连带改查询实现与 SQL 拼装，属另一件事），只是本页不再喂值。

        #endregion

        #region 可观察属性 — 分页与状态

        /// <summary>当前页码（1 起）</summary>
        [ObservableProperty]
        private int pageIndex = 1;

        /// <summary>每页行数（真相源：View 侧分页控件在 SetupPagination 里按此值同步）</summary>
        [ObservableProperty]
        private int pageSize = 20;

        /// <summary>命中总行数</summary>
        [ObservableProperty]
        private int totalCount;

        /// <summary>总页数（供 View 分页控件）</summary>
        [ObservableProperty]
        private int totalPages;

        #endregion

        #region 权限控制属性（供 View 绑定）

        /// <summary>是否允许导出（数据出库独立授权）</summary>
        public bool CanExport => authService != null && authService.HasPermission(PermissionCodes.BtnAuditExport);

        #endregion

        #region 委托（View 层实现，VM 零耦合）

        /// <summary>
        /// 调用方：请弹出保存文件对话框并返回目标路径（取消返回 null/空）。
        /// 文件对话框归 View（与 PLC 调试页导入导出同惯例）。
        /// </summary>
        public Func<string> AskExportFilePath { get; set; }

        #endregion

        public AuditQueryViewModel(
            IAuditQueryService queryService,
            IAuthService authService,
            IMessageBoxService messageBoxService,
            ICsvService csvService,
            IAuditService auditService,
            IUserSession userSession)
        {
            this.queryService = queryService ?? throw new ArgumentNullException(nameof(queryService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.csvService = csvService ?? throw new ArgumentNullException(nameof(csvService));
            this.auditService = auditService;
            this.userSession = userSession;
        }

        #region 命令 — 查询

        /// <summary>按当前筛选条件（时间 + 页面）查询（回到第 1 页）。</summary>
        [RelayCommand]
        private void Search()
        {
            PageIndex = 1;
            LoadPage();
        }

        /// <summary>
        /// 跳转到指定页——分页控件 PageChanged 的唯一入口。
        /// 页码夹紧到 [1, TotalPages]；同页直接返回（挡住程序性同步 ActivePage 的回环）。
        /// 🔴 不提供 PrevPage / NextPage：控件的语义是「目标页」，相对步长会与已算好的目标页打架。
        /// </summary>
        [RelayCommand]
        private void GoToPage(int pageIndex)
        {
            int target = Math.Max(1, Math.Min(pageIndex, TotalPages > 0 ? TotalPages : 1));
            if (target == PageIndex) return;

            PageIndex = target;
            LoadPage();
        }

        /// <summary>重载当前页（外部改动后刷新用）</summary>
        [RelayCommand]
        private void Refresh() => LoadPage();

        #endregion

        #region 命令 — 导出

        /// <summary>
        /// 导出当前筛选条件下的展开长表 CSV（全部命中结果，不分页）。
        ///
        /// 编码由底层 CsvService 保证为 UTF-8 **带 BOM**（Excel 打开无 BOM 的 UTF-8 CSV 会把中文识别成乱码）。
        /// 数据出库是受控动作，独立权限码 Btn:Audit_Export（能查 ≠ 能导）。
        ///
        /// 两处兜底（针对底层 CsvService 的既有行为，不改动它——它是流控热路径）：
        /// ①写前删目标文件：CsvService.Records 是"追加写"且文件已存在时不写表头；
        /// ②写后回读校验：兜底（Records 已改为写失败抛异常，此处防「写成功但文件为空」）。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExport))]
        private void Export()
        {
            if (!authService.HasPermission(PermissionCodes.BtnAuditExport))
            {
                // 门禁拒绝不留痕（与其余页面同规则）：命令首行就被拦下、没碰任何数据，
                // 记进审计只会混进一批"什么都没发生"的行（见 AuditResult 注释）
                messageBoxService.ShowError("权限不足");
                return;
            }

            string path = AskExportFilePath?.Invoke();
            if (string.IsNullOrWhiteSpace(path)) return; // 取消选文件 = 动作未发生，不记账

            try
            {
                var rows = queryService.QueryFlat(BuildFilter());
                var exportRows = rows.Select(AuditExportRow.From).ToList();

                // 空结果不写文件、不记账：与「取消选文件」同规则——动作未发生。
                // （CsvService 对空集合只写 0 字节文件，会与「写失败」混淆）
                if (exportRows.Count == 0)
                {
                    messageBoxService.ShowWarning("当前筛选条件下没有可导出的数据");
                    return;
                }

                // ① SaveFileDialog 只提示"是否覆盖"，不会真删文件；而 CsvService.Records 是追加写 ——
                // 不删就会得到"旧数据 + 无表头的新数据"这种脏文件。
                if (File.Exists(path)) File.Delete(path);

                csvService.Records(exportRows, path);

                // ② 回读校验：Records 已改为写失败抛异常（文件被占用时直接抛到 catch），
                // 这里只兜「写成功但文件为空」的极端情况。
                var info = new FileInfo(path);
                if (!info.Exists || info.Length == 0)
                    throw new IOException("导出文件未写入成功（目标文件可能正被其它程序占用）");

                messageBoxService.ShowSuccess($"导出成功，共 {rows.Count} 行", false, 500);

                // 导出审计记录本身也是受审计动作——不然"谁把审计数据导走了"这个最该留痕的问题反而无据可查
                auditService.Record(new AuditEntry(AuditCategory.Audit, AuditAction.Export, ExportTarget(path),
                    userSession?.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"导出失败: {ex.Message}");
                auditService.Record(new AuditEntry(AuditCategory.Audit, AuditAction.Export, ExportTarget(path),
                    userSession?.CurrentUser.ToAuditActor(), AuditResult.Fail));
            }
        }

        #endregion

        #region 明细钻取

        /// <summary>选中操作变更 → 加载其明细（主从钻取）。</summary>
        partial void OnSelectedOperationChanged(AuditOperationDto value)
        {
            LoadFieldChanges(value?.Id ?? 0);
        }

        #endregion

        #region 加载

        private void LoadPage()
        {
            try
            {
                var result = queryService.Query(BuildFilter());

                TotalCount = result.TotalCount;
                TotalPages = PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

                SelectedOperation = null;

                Operations.RaiseListChangedEvents = false; // 批量更新避免闪烁与滚动条重置
                Operations.Clear();
                foreach (var item in result.Items) Operations.Add(item);
                Operations.RaiseListChangedEvents = true;
                Operations.ResetBindings();
            }
            catch (Exception ex)
            {
                // 读侧策略与写侧相反：查询失败必须让用户看见，否则空白页会被误判成"没有记录"
                messageBoxService.ShowError($"查询审计记录失败: {ex.Message}");
            }
        }

        private void LoadFieldChanges(long operationId)
        {
            try
            {
                var list = operationId > 0
                    ? queryService.GetChanges(operationId)
                    : new List<AuditFieldChangeDto>();

                FieldChanges.RaiseListChangedEvents = false;
                FieldChanges.Clear();
                foreach (var item in list) FieldChanges.Add(item);
                FieldChanges.RaiseListChangedEvents = true;
                FieldChanges.ResetBindings();
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"加载变更明细失败: {ex.Message}");
            }
        }

        #endregion

        #region 私有辅助

        /// <summary>
        /// 组装查询条件。只喂本页还留着的两维（时间 + 页面）——
        /// UserName / Action / Result 在契约上仍在（服务端能力不减），只是 UI 不再提供入口。
        /// </summary>
        private AuditQueryFilter BuildFilter()
        {
            return new AuditQueryFilter
            {
                TimeFrom = TimeFrom,
                TimeTo = TimeTo,
                Category = CategoryFilter,
                PageIndex = PageIndex,
                PageSize = PageSize,
            };
        }

        #endregion

        #region 审计留痕

        // 本页只有「导出」一个留痕点（门禁拒绝不留痕）——查询 / 翻页 / 刷新不记：读不改任何状态，
        // 每点一次查都记一条会把审计表自己刷成噪声（与 IAuditQueryService"查询动作本身不记审计"一致）。
        // 页面维度取"审计记录"：本页动作发生在审计页；跨页面的登录态类动作才归 AuditCategory.System。

        /// <summary>审计操作对象：本页唯一的受审计动作是「导出」，对象即审计记录本身</summary>
        private const string AuditTargetName = "审计记录";

        /// <summary>导出对象 = 审计记录（把"导到哪份文件"并进同一格，审计页自查时一眼可辨）</summary>
        private static string ExportTarget(string path)
        {
            string fileName = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(fileName)
                ? AuditTargetName
                : AuditTargetName + " (" + fileName + ")";
        }

        #endregion
    }
}
