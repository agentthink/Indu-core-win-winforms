using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
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
    /// 报警查询 ViewModel（只读页，参照 <see cref="AuditQueryViewModel"/> 先例同构）。
    /// 无删除/修改/清空；处理动作只在弹窗链路。筛选只留 时间范围 + 类型 两维。
    /// 契约无分页维度：Query 一次拉全量（服务端上限保护），本 VM 做内存分页（照流程监控页先例）。
    /// 导出照审计页姿势：Query 全量 → 映射导出行 → CSV。
    /// </summary>
    public partial class AlarmQueryViewModel : ViewModelBase
    {
        private readonly IAlarmService alarmService;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBoxService;
        private readonly ICsvService csvService;
        private readonly IAuditService auditService;
        private readonly IUserSession userSession;

        /// <summary>最近一次查询的全量结果（内存分页数据源）</summary>
        private List<AlarmRecordDto> allRecords = new List<AlarmRecordDto>();

        #region 可观察属性

        /// <summary>当前页报警行（行类型 = AlarmRecordDto）</summary>
        [ObservableProperty]
        private BindingList<AlarmRecordDto> records = new BindingList<AlarmRecordDto>();

        /// <summary>起始时间（分钟粒度，View 日期时间选择器直传完整值）</summary>
        [ObservableProperty]
        private DateTime timeFrom = DateTime.Now.AddDays(-2);

        /// <summary>结束时间（分钟粒度；仓储按 &lt;= 含端点比较）</summary>
        [ObservableProperty]
        private DateTime timeTo = DateTime.Now;

        /// <summary>类型（null = 全部；收可空枚举，文案转换在仓储一次性完成）</summary>
        [ObservableProperty]
        private AlarmType? typeFilter;

        /// <summary>当前页码（1 起，内存分页）</summary>
        [ObservableProperty]
        private int pageIndex = 1;

        /// <summary>每页行数（真相源：View 侧分页控件初始化时按此值同步）</summary>
        [ObservableProperty]
        private int pageSize = 20;

        /// <summary>命中总行数</summary>
        [ObservableProperty]
        private int totalCount;

        /// <summary>总页数（供 View 分页控件）</summary>
        [ObservableProperty]
        private int totalPages;

        /// <summary>是否允许导出（数据出库独立授权 Btn:Alarm_Export）</summary>
        public bool CanExport => authService != null && authService.HasPermission(PermissionCodes.BtnAlarmExport);

        #endregion

        /// <summary>
        /// 弹出保存文件对话框并返回目标路径（取消返回 null/空）。文件对话框归 View（与审计页同惯例）。
        /// </summary>
        public Func<string> AskExportFilePath { get; set; }

        public AlarmQueryViewModel(
            IAlarmService alarmService,
            IAuthService authService,
            IMessageBoxService messageBoxService,
            ICsvService csvService,
            IAuditService auditService,
            IUserSession userSession)
        {
            this.alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBoxService = messageBoxService ?? throw new ArgumentNullException(nameof(messageBoxService));
            this.csvService = csvService ?? throw new ArgumentNullException(nameof(csvService));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        #region 命令 — 查询

        /// <summary>按当前筛选条件（时间 + 类型）查询（回到第 1 页）。</summary>
        [RelayCommand]
        private void Search()
        {
            PageIndex = 1;
            Load();
        }

        /// <summary>
        /// 跳转到指定页——分页控件 PageChanged 的唯一入口。
        /// 页码夹紧到 [1, TotalPages]；同页直接返回（挡住程序性同步 ActivePage 的回环）。
        /// 内存分页，只切当前页切片，不重查。
        /// 🔴 不提供 PrevPage / NextPage：控件的语义是「目标页」，相对步长会与已算好的目标页打架。
        /// </summary>
        [RelayCommand]
        private void GoToPage(int pageIndex)
        {
            int target = Math.Max(1, Math.Min(pageIndex, TotalPages > 0 ? TotalPages : 1));
            if (target == PageIndex) return;

            PageIndex = target;
            FillCurrentPage();
        }

        /// <summary>重查当前筛选（外部改动后刷新用，保持当前页码）</summary>
        [RelayCommand]
        private void Refresh() => Load();

        #endregion

        #region 命令 — 导出

        /// <summary>
        /// 导出当前筛选条件下的全量报警 CSV（Query，服务端上限保护）。
        /// 两处兜底照 Audit 规则：写前删目标文件（底层追加写）、写后回读校验（防「写成功但文件为空」；
        /// Records 已改为写失败抛异常，回读校验降级为兜底）。
        /// 权限门禁 Btn:Alarm_Export。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExport))]
        private void Export()
        {
            if (!authService.HasPermission(PermissionCodes.BtnAlarmExport))
            {
                messageBoxService.ShowError("权限不足");
                return;
            }

            string path = AskExportFilePath?.Invoke();
            if (string.IsNullOrWhiteSpace(path)) return;

            try
            {
                var rows = alarmService.Query(BuildFilter());
                var exportRows = rows.Select(AlarmExportRow.From).ToList();

                // 空结果不写文件：CsvService 对空集合只写 0 字节文件，会与「写失败」混淆
                if (exportRows.Count == 0)
                {
                    messageBoxService.ShowWarning("当前筛选条件下没有可导出的数据");
                    return;
                }

                if (File.Exists(path)) File.Delete(path);

                csvService.Records(exportRows, path);

                var info = new FileInfo(path);
                if (!info.Exists || info.Length == 0)
                    throw new IOException("导出文件未写入成功（目标文件可能正被其它程序占用）");

                messageBoxService.ShowSuccess($"导出成功，共 {rows.Count} 行", false, 500);

                // 数据出库是受控动作（独立权限码 Btn:Alarm_Export），成功即留痕；
                // 权限拒绝 / 取消选文件 / 空结果 / 异常均不记账——动作未发生或未完成
                auditService.Record(new AuditEntry(AuditCategory.Alarm, AuditAction.Export,
                    ExportTarget(path), userSession?.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex)
            {
                messageBoxService.ShowError($"导出失败: {ex.Message}");
            }
        }

        #endregion

        #region 加载（内存分页）

        private void Load()
        {
            try
            {
                allRecords = alarmService.Query(BuildFilter());

                int count = allRecords.Count;
                int pages = PageSize > 0 ? (int)Math.Ceiling(count / (double)PageSize) : 0;

                // 🔴 顺序铁律：必须先回夹 PageIndex、再赋 TotalCount。
                // Pagination.ActivePage setter 在页码被夹取时会触发 PageChanged（控件源码取证：
                // RenderAndNotifyIfClamped），若先赋 TotalCount，"结果页数缩水把当前页顶出界"就会：
                // 控件夹页 → PageChanged → PageIndex 与 ActivePage 不等 → 递归 Refresh → 查询执行两次。
                // 先夹好 VM 页码，后续 TotalCount 同步时控件不再越界，链路一次查询收敛
                //（夹取引发的 PageChanged 由 View 侧"同页幂等防御"挡住）。
                if (PageIndex > pages) PageIndex = pages > 0 ? pages : 1;

                TotalCount = count;
                TotalPages = pages;

                FillCurrentPage();
            }
            catch (Exception ex)
            {
                // 读侧策略与写侧相反：查询失败必须让用户看见，否则空白页会被误判成"没有记录"
                messageBoxService.ShowError($"查询报警记录失败: {ex.Message}");
            }
        }

        private void FillCurrentPage()
        {
            var page = allRecords
                .Skip((PageIndex - 1) * PageSize)
                .Take(PageSize)
                .ToList();

            Records.RaiseListChangedEvents = false;
            Records.Clear();
            foreach (var item in page) Records.Add(item);
            Records.RaiseListChangedEvents = true;
            Records.ResetBindings();
        }

        #endregion

        #region 私有辅助

        /// <summary>
        /// 审计 Target：<c>报警记录 (文件名)</c>。只取文件名，不写全路径。
        /// </summary>
        private static string ExportTarget(string path)
        {
            string fileName = Path.GetFileName(path);
            return string.IsNullOrWhiteSpace(fileName) ? "报警记录" : "报警记录 (" + fileName + ")";
        }

        /// <summary>组装查询条件（时间 + 类型，照审计 BuildFilter）。</summary>
        private AlarmQueryFilter BuildFilter()
        {
            return new AlarmQueryFilter
            {
                TimeFrom = TimeFrom,
                TimeTo = TimeTo,
                Type = TypeFilter,
            };
        }

        #endregion
    }
}
