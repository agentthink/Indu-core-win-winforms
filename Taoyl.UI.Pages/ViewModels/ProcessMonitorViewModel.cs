using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using Taoyl.Application.ProcessMonitoring;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.Application.TranslateDriver.Interfaces;
using Taoyl.Application.TranslateDriver.Management;
using Taoyl.Application.TranslateDriver.TranslateExtensions;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 流程监控页面 ViewModel
    /// 
    /// 核心设计：
    /// 1. 数据层：全量缓存 + 内存分页，避免翻页重复查服务
    /// 2. 同步层：结构不变时逐行 ResetItem，保持滚动条；仅增删时全量重建
    /// 3. 选中层：刷新后按流程名重新定位 SelectedRow，确保按钮状态实时同步
    /// 4. 命令层：所有操作按钮带 CanExecute，统一权限检查，操作前二次确认防呆
    /// 5. 线程安全：PrepareSnapshot 与 CommitSnapshot 分离，后台准备数据，UI 线程提交
    /// 6. 状态同步：SelectedRow / IsInitializingAll 变化时通过 partial 方法同步派生字段
    /// 7. 颜色解析：状态颜色逻辑内聚于 VM，便于后续主题切换
    /// </summary>
    public partial class ProcessMonitorViewModel : ObservableObject
    {
        private readonly IMonitorService monitorService;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBox;
        private readonly ITranslationLanguageService translationService;
        private readonly IUserSession userSession;
        private readonly IAuditService auditService;

        /// <summary>全量数据缓存，分页源</summary>
        private List<ProcessMonitorRow> allRowsCache = new List<ProcessMonitorRow>();

        // ---- 后台刷新预准备数据（避免跨线程操作 ObservableProperty）----
        private List<ProcessMonitorRow> preparedRows = new List<ProcessMonitorRow>();
        private SystemProcessState preparedSystemState;
        private int preparedTotalCount;
        private int preparedPageIndex;

        #region 可观察属性

        [ObservableProperty]
        private BindingList<ProcessMonitorRow> processRows = new BindingList<ProcessMonitorRow>();

        [ObservableProperty]
        private ProcessMonitorRow selectedRow;

        [ObservableProperty]
        private SystemProcessState systemState;

        [ObservableProperty]
        private string operationFeedback = string.Empty;

        [ObservableProperty]
        private bool isInitializingAll;

        // ---- 按钮状态字段（由 SelectedRow / IsInitializingAll 变化时同步更新）----
        [ObservableProperty]
        private bool canStartSelected = false;

        [ObservableProperty]
        private bool canPauseSelected = false;

        [ObservableProperty]
        private bool canResetSelected = false;

        [ObservableProperty]
        private bool canInitSelected = false;

        [ObservableProperty]
        private bool canInitAll = true;

        // ---- 按钮文本字段（由 SelectedRow / IsInitializingAll 变化时同步更新）----
        [ObservableProperty]
        private string startButtonText = "启动";

        [ObservableProperty]
        private string initAllButtonText = "整机初始化";

        [ObservableProperty]
        private string toggleEnabledButtonText = "启用:--";

        [ObservableProperty]
        private string togglePauseOthersButtonText = "异停:--";

        // ---- 刷新控制 ----
        [ObservableProperty]
        private bool isAutoRefresh = true;

        [ObservableProperty]
        private int refreshInterval = 500;

        // ---- 分页 ----
        [ObservableProperty]
        private int pageIndex = 1;

        [ObservableProperty]
        private int pageSize = 20;

        [ObservableProperty]
        private int totalCount;

        #endregion

        #region 属性变更钩子（partial 方法）

        /// <summary>
        /// SelectedRow 变化后同步按钮状态与文本
        /// 由 CommunityToolkit.Mvvm 源码生成器在 SelectedRow setter 中自动调用
        /// </summary>
        partial void OnSelectedRowChanged(ProcessMonitorRow value)
        {
            CanStartSelected = value?.CanStart ?? false;
            CanPauseSelected = value?.CanPause ?? false;
            CanResetSelected = value?.CanReset ?? false;
            CanInitSelected = value?.CanInitialize ?? false;

            UpdateSelectedTexts(value);

            // 通知命令 CanExecute 重评估
            StartOrResumeSelectedCommand.NotifyCanExecuteChanged();
            PauseSelectedCommand.NotifyCanExecuteChanged();
            ResetSelectedCommand.NotifyCanExecuteChanged();
            InitializeSelectedCommand.NotifyCanExecuteChanged();
        }

        /// <summary>
        /// 按钮文本的输入签名：文本只依赖这四项（有无选中行 / 状态组 / IsEnabled / CanPauseOthersOnException）。
        /// 四项未变 ⇒ ToTranslate 必然返回同一结果（AutoRegisterOnMiss=false 时读路径无写副作用），直接跳过求值。
        /// 比的是「输入」不是「输出」：比输出必须先求值，日志已打出，治不了本。
        /// 🔴 文本若新增依赖（如把步序、流程名拼进文案），必须同步扩此签名，否则守卫会挡住本该发生的更新。
        /// </summary>
        private (bool HasRow, bool IsStartGroup, bool IsEnabled, bool CanPauseOthers)? lastTextSignature;

        /// <summary>提取按钮文本的输入签名（UpdateSelectedTexts 的唯一依赖集）</summary>
        private static (bool HasRow, bool IsStartGroup, bool IsEnabled, bool CanPauseOthers) GetTextSignature(ProcessMonitorRow value)
            => (value != null,
                value != null && (value.State == ProcessState.Idle || value.State == ProcessState.Stopped),
                value != null && value.IsEnabled,
                value != null && value.CanPauseOthersOnException);

        /// <summary>
        /// 动态按钮文本（绑定 VM，切语言时会被 TranslateForm 覆盖后再被此赋值覆盖回原文）。
        /// 修复：赋值走 ToTranslate（Text 资源，语义 key），语言切换后 View 调 RefreshDynamicTexts 重算。
        /// 注意：对应 Text 类型翻译资源需在 FrmResourceConfig 文本页补充（ProcessStart/ProcessContinue 等），
        /// 未补充时回退原文（中文），与修复前行为一致。
        /// </summary>
        /// <param name="force">true=忽略签名强制重算。语言切换必须走这条：签名四项没变，但译文变了。</param>
        private void UpdateSelectedTexts(ProcessMonitorRow value, bool force = false)
        {
            // 轮询每 500ms 重建行对象 → SelectedRow 换引用 → 本方法每 tick 被调 3 次；
            // 输入没变时跳过求值，日志（TranslateExtensions 未命中分支）才不会每 tick 刷 3 行
            var signature = GetTextSignature(value);
            if (!force && lastTextSignature.HasValue && lastTextSignature.Value == signature) return;
            lastTextSignature = signature;

            StartButtonText = value == null
                ? "启动".ToTranslate<ProcessMonitorViewModel>("ProcessStart")
                : (value.State == ProcessState.Idle || value.State == ProcessState.Stopped)
                    ? "启动".ToTranslate<ProcessMonitorViewModel>("ProcessStart")
                    : "继续".ToTranslate<ProcessMonitorViewModel>("ProcessContinue");

            ToggleEnabledButtonText = value == null
                ? "启用:--".ToTranslate<ProcessMonitorViewModel>("ProcessEnabledUnknown")
                : value.IsEnabled
                    ? "启用:是".ToTranslate<ProcessMonitorViewModel>("ProcessEnabledOn")
                    : "启用:否".ToTranslate<ProcessMonitorViewModel>("ProcessEnabledOff");

            TogglePauseOthersButtonText = value == null
                ? "异停:--".ToTranslate<ProcessMonitorViewModel>("ProcessPauseOthersUnknown")
                : value.CanPauseOthersOnException
                    ? "异停:是".ToTranslate<ProcessMonitorViewModel>("ProcessPauseOthersOn")
                    : "异停:否".ToTranslate<ProcessMonitorViewModel>("ProcessPauseOthersOff");
        }

        /// <summary>
        /// 语言切换后重新计算动态按钮文本（VM 订阅 ITranslationLanguageService.LanguageChanged 调用，
        /// View 在 OnShown 初始调用一次；触发属性变化 → DataBinding 刷新按钮 Text 为当前语言）
        /// </summary>
        public void RefreshDynamicTexts()
        {
            // force：切语言时签名四项一个都没变，但译文变了，必须绕过守卫重算
            UpdateSelectedTexts(SelectedRow, force: true);
            InitAllButtonText = IsInitializingAll
                ? "初始化中...".ToTranslate<ProcessMonitorViewModel>("ProcessInitializing")
                : "整机初始化".ToTranslate<ProcessMonitorViewModel>("ProcessInitAll");
        }

        /// <summary>
        /// 语言切换回调：重算全部动态按钮文本（VM 订阅，SingleInstance 常驻不退订；
        /// 事件在 UI 线程触发，属性赋值与绑定更新同线程安全）
        /// </summary>
        private void OnLanguageChanged(object sender, LanguageChangedEventArgs e)
        {
            RefreshDynamicTexts();
        }

        /// <summary>
        /// IsInitializingAll 变化后同步整机初始化按钮状态与文本
        /// 由 CommunityToolkit.Mvvm 源码生成器在 IsInitializingAll setter 中自动调用
        /// </summary>
        partial void OnIsInitializingAllChanged(bool value)
        {
            CanInitAll = !value;
            InitAllButtonText = value
                ? "初始化中...".ToTranslate<ProcessMonitorViewModel>("ProcessInitializing")
                : "整机初始化".ToTranslate<ProcessMonitorViewModel>("ProcessInitAll");
        }

        #endregion

        #region 颜色解析（供 View 层使用）

        /// <summary>
        /// 根据流程状态获取显示颜色（单层四态）
        /// </summary>
        public Color GetStatusColor(ProcessState state)
        {
            if (state == ProcessState.Exception) return Color.Red;
            if (state == ProcessState.Paused) return Color.Orange;
            if (state == ProcessState.Running) return Color.LimeGreen;
            return Color.Gray;
        }

        /// <summary>
        /// 根据系统级状态获取显示颜色
        /// </summary>
        public Color GetStateColor(SystemProcessState state) => state switch
        {
            SystemProcessState.Running => Color.LimeGreen,
            SystemProcessState.Paused => Color.Orange,
            SystemProcessState.Exception => Color.Red,
            _ => Color.Gray
        };

        #endregion

        public ProcessMonitorViewModel(
            IMonitorService monitorService,
            IAuthService authService,
            IMessageBoxService messageBox,
            ITranslationLanguageService translationService,
            IUserSession userSession,
            IAuditService auditService)
        {
            this.monitorService = monitorService ?? throw new ArgumentNullException(nameof(monitorService));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
            this.translationService = translationService ?? throw new ArgumentNullException(nameof(translationService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));

            // 语言切换事件由 VM 自行订阅：动态文本状态在 VM，View 只绑定；
            // ProcessMonitorViewModel 注册为 SingleInstance 常驻，订阅无需退订
            translationService.LanguageChanged += OnLanguageChanged;
        }

        #region 数据刷新（后台准备 + UI 提交）

        /// <summary>
        /// 在后台线程拉取快照、计算分页，不触碰任何 ObservableProperty
        /// 由 View 层通过 Task.Run 调用
        /// </summary>
        public void PrepareSnapshot()
        {
            try
            {
                preparedSystemState = monitorService.GetSystemState();

                var statuses = monitorService.GetAllProcessStatus();
                allRowsCache = statuses.Select(s => new ProcessMonitorRow
                {
                    ProcessName = s.ProcessName,
                    Category = s.Category.ToString(),
                    State = s.State,
                    PermittedTriggers = s.PermittedTriggers,
                    Step = s.Step,
                    ExceptionCode = s.ExceptionCode,
                    ExceptionMessage = s.ExceptionMessage,
                    IsEnabled = GetProcessEnabled(s.ProcessName),
                    CanPauseOthersOnException = GetProcessPauseOthersConfig(s.ProcessName),
                    Timestamp = s.Timestamp
                }).ToList();

                preparedTotalCount = allRowsCache.Count;

                int targetPage = PageIndex;
                int maxPage = Math.Max(1, (int)Math.Ceiling(preparedTotalCount / (double)PageSize));
                if (targetPage > maxPage) targetPage = maxPage;
                if (targetPage < 1) targetPage = 1;
                preparedPageIndex = targetPage;

                preparedRows = allRowsCache
                    .Skip((targetPage - 1) * PageSize)
                    .Take(PageSize)
                    .ToList();
            }
            catch
            {
                // 轮询异常静默处理，避免崩 UI；提交时会呈现空表
                preparedSystemState = default;
                preparedRows = new List< ProcessMonitorRow > ();
                preparedTotalCount = 0;
                preparedPageIndex = 1;
            }
        }

        /// <summary>
        /// 在 UI 线程提交预准备的数据，同步 BindingList 并恢复选中
        /// </summary>
        public void CommitSnapshot()
        {
            // 🔴 顺序铁律：TotalCount 必须先于 PageIndex 赋值。
            // View 侧 SyncPagination 会把两个属性实时推给分页控件（Controls.Pagination），
            // 控件 ClampPage 用的是它当前持有的 TotalCount 算出的总页数：
            // 本页是轮询页，存在"数据增多"场景（新页码可大于旧总页数）——若先赋 PageIndex，
            // 控件会按旧总页数误夹页码（7→3 回落）并触发 PageChanged 多跑一轮刷新。
            // 先让控件拿到新总数，回夹后的页码才不会被误夹（夹取引发的 PageChanged 由 View 同页幂等防御挡住）。
            // （报警查询页 Load 的"先夹 PageIndex 再赋 TotalCount"字面相反但本质相同：
            //   该页是纯缩水场景，VM 夹后值恒 ≤ 旧页数，不存在误夹窗口；两处都是
            //   "让控件在正确的总页数上下文中接收页码"。）
            TotalCount = preparedTotalCount;

            if (PageIndex != preparedPageIndex)
                PageIndex = preparedPageIndex;

            SystemState = preparedSystemState;
            SyncProcessRows(preparedRows);

            // 引用同步：从新的 ProcessRows 中找回同名流程，更新 SelectedRow
            if (SelectedRow != null)
            {
                string nameToRestore = SelectedRow.ProcessName;
                var newSelected = ProcessRows.FirstOrDefault(r => r.ProcessName == nameToRestore);
                if (newSelected != null && !ReferenceEquals(SelectedRow, newSelected))
                    SelectedRow = newSelected;
            }
        }

        /// <summary>
        /// 同步 BindingList：结构不变时逐行替换 + ResetItem，保持滚动条；
        /// 仅流程增删时才全量重建（工业场景极少触发）
        /// </summary>
        private void SyncProcessRows(List<ProcessMonitorRow> newData)
        {
            if (newData == null || newData.Count == 0)
            {
                ProcessRows.Clear();
                return;
            }

            bool structureChanged = newData.Count != ProcessRows.Count;
            if (!structureChanged)
            {
                var newNames = new HashSet<string>(newData.Select(n => n.ProcessName));
                var oldNames = new HashSet<string>(ProcessRows.Select(p => p.ProcessName));
                structureChanged = !newNames.SetEquals(oldNames);
            }

            if (structureChanged)
            {
                ProcessRows.RaiseListChangedEvents = false;
                try
                {
                    ProcessRows.Clear();
                    foreach (var item in newData)
                        ProcessRows.Add(item);
                }
                finally
                {
                    ProcessRows.RaiseListChangedEvents = true;
                    ProcessRows.ResetBindings();
                }
                return;
            }

            var newDict = newData.ToDictionary(r => r.ProcessName);
            var changedIndices = new List<int>();

            ProcessRows.RaiseListChangedEvents = false;
            try
            {
                for (int i = 0; i < ProcessRows.Count; i++)
                {
                    var oldItem = ProcessRows[i];
                    if (newDict.TryGetValue(oldItem.ProcessName, out var newItem))
                    {
                        if (!ReferenceEquals(oldItem, newItem))
                        {
                            ProcessRows[i] = newItem;
                            changedIndices.Add(i);
                        }
                    }
                }
            }
            finally
            {
                ProcessRows.RaiseListChangedEvents = true;
            }

            foreach (var idx in changedIndices.OrderBy(x => x))
                ProcessRows.ResetItem(idx);
        }

        #endregion

        #region 确认对话框

        /// <summary>
        /// 通用操作确认，返回 true 表示用户点击确认
        /// </summary>
        private bool ConfirmOperation(string actionName, string targetName)
        {
            return messageBox.ShowAsk($"确认{actionName}流程 [{targetName}]？");
        }

        /// <summary>
        /// 整机初始化确认（影响范围大，单独强调）
        /// </summary>
        private bool ConfirmInitializeAll()
        {
            return messageBox.ShowAsk("整机初始化将重置所有流程状态，是否继续？");
        }

        #endregion

        #region 控制命令

        [RelayCommand(CanExecute = nameof(CanStartSelected))]
        private void StartOrResumeSelected()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMonitorStart))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (SelectedRow == null) return;
            // 待命/停止 = 可"启动"；其余（暂停/异常）= "继续"（异常态按钮本被 CanStart 禁用，仅文案兜底）
            bool fromStopState = SelectedRow.State == ProcessState.Idle || SelectedRow.State == ProcessState.Stopped;
            if (!ConfirmOperation(fromStopState ? "启动" : "继续", SelectedRow.ProcessName)) return;

            // ok = 业务层返回的成败。带 processName 的重载返回 bool（批量无参重载是 void，见 IMonitorService）——
            // 原先忽略返回值，会把"返回 false 的业务失败"反馈成"已启动"、审计也记成成功（假账）。
            bool ok = false;
            try
            {
                if (fromStopState)
                {
                    ok = monitorService.StartProcess(SelectedRow.ProcessName);
                    OperationFeedback = ok
                        ? $"[{SelectedRow.ProcessName}] 已启动"
                        : $"[{SelectedRow.ProcessName}] 启动失败";
                }
                else
                {
                    ok = monitorService.ResumeProcess(SelectedRow.ProcessName);
                    OperationFeedback = ok
                        ? $"[{SelectedRow.ProcessName}] 已继续"
                        : $"[{SelectedRow.ProcessName}] 继续失败";
                }
            }
            catch (Exception ex)
            {
                OperationFeedback = $"启动失败: {ex.Message}";
            }

            // 投递放在 try/catch 之外：成功、业务返回 false、抛异常三条路径在这里合流，一处覆盖
            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.Start, SelectedRow.ProcessName,
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
        }

        [RelayCommand(CanExecute = nameof(CanPauseSelected))]
        private void PauseSelected()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMonitorPause))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (SelectedRow == null) return;
            if (!ConfirmOperation("暂停", SelectedRow.ProcessName)) return;

            bool ok = false;
            try
            {
                ok = monitorService.PauseProcess(SelectedRow.ProcessName);
                OperationFeedback = ok
                    ? $"[{SelectedRow.ProcessName}] 已暂停"
                    : $"[{SelectedRow.ProcessName}] 暂停失败";
            }
            catch (Exception ex)
            {
                OperationFeedback = $"暂停失败: {ex.Message}";
            }

            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.Pause, SelectedRow.ProcessName,
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
        }

        [RelayCommand(CanExecute = nameof(CanResetSelected))]
        private void ResetSelected()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMonitorReset))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (SelectedRow == null) return;
            if (!ConfirmOperation("复位", SelectedRow.ProcessName)) return;

            bool ok = false;
            try
            {
                ok = monitorService.ResetProcess(SelectedRow.ProcessName);
                OperationFeedback = ok
                    ? $"[{SelectedRow.ProcessName}] 已复位，请点击启动继续"
                    : $"[{SelectedRow.ProcessName}] 复位失败";
            }
            catch (Exception ex)
            {
                OperationFeedback = $"复位失败: {ex.Message}";
            }

            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.ResetProcess, SelectedRow.ProcessName,
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
        }

        [RelayCommand(CanExecute = nameof(CanInitSelected))]
        private async Task InitializeSelectedAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMointorInit))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (SelectedRow == null) return;
            if (!ConfirmOperation("初始化", SelectedRow.ProcessName)) return;

            // 目标名在 await 前捕获：等待期间轮询刷新会重建行对象并重定位 SelectedRow，
            // await 之后再读 SelectedRow.ProcessName 可能已指向另一行（审计 Target 必须锁死本次操作对象）
            string target = SelectedRow.ProcessName;
            bool ok = false;
            try
            {
                await monitorService.InitializeProcessAsync(target);
                ok = true;
                OperationFeedback = $"[{target}] 已初始化";
            }
            catch (Exception ex)
            {
                OperationFeedback = $"初始化失败: {ex.Message}";
            }

            // InitializeProcessAsync 返回 Task（非 Task&lt;bool&gt;）⇒ 没有业务返回值可判，只能靠是否抛异常
            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.Initialize, target,
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
        }

        [RelayCommand]
        private async Task InitializeAllAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMonitorInitAll))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (!ConfirmInitializeAll()) return;

            IsInitializingAll = true;
            bool ok = false;
            try
            {
                await monitorService.InitializeProcessAsync();
                ok = true;
                OperationFeedback = "整机初始化完成";
            }
            catch (Exception ex)
            {
                OperationFeedback = $"整机初始化失败: {ex.Message}";
            }
            finally
            {
                IsInitializingAll = false;
            }

            // Target 用"整机"与单流程初始化区分（两者共用 AuditAction.Initialize）；
            // 返回 Task（非 Task&lt;bool&gt;）⇒ 只能靠是否抛异常判成败
            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.Initialize, "整机",
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
        }

        #endregion

        #region 配置命令

        [RelayCommand]
        private void ToggleSelectedEnabled()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMonitorToggleEnabled))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (SelectedRow == null) return;

            string action = SelectedRow.IsEnabled ? "禁用" : "启用";
            if (!ConfirmOperation(action, SelectedRow.ProcessName)) return;

            bool oldValue = SelectedRow.IsEnabled;
            bool newValue = !oldValue;
            bool ok = monitorService.SetProcessEnabled(SelectedRow.ProcessName, newValue);
            if (ok)
            {
                var idx = ProcessRows.IndexOf(SelectedRow);
                if (idx >= 0)
                {
                    SelectedRow.IsEnabled = newValue;
                    ProcessRows.ResetItem(idx);
                }
                OperationFeedback = $"[{SelectedRow.ProcessName}] 启用: {(newValue ? "是" : "否")}";
            }
            else
            {
                OperationFeedback = $"[{SelectedRow.ProcessName}] 启用设置失败";
            }

            // 明细只在真的改了运行时配置时落：失败时状态未变，写了 NewValue 就是"记了没发生的事"
            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.ToggleEnabled,
                SelectedRow.ProcessName, userSession.CurrentUser.ToAuditActor(),
                ok ? AuditResult.Success : AuditResult.Fail)
            {
                Changes = ok
                    ? new List<AuditFieldChange>
                    {
                        new AuditFieldChange
                        {
                            FieldPath = "启用",
                            ChangeType = AuditChangeType.Update,
                            OldValue = oldValue ? "是" : "否",
                            NewValue = newValue ? "是" : "否"
                        }
                    }
                    : null
            });
        }

        [RelayCommand]
        private void ToggleSelectedPauseOthers()
        {
            if (!authService.HasPermission(PermissionCodes.BtnProcessMonitorTogglePauseOthers))
            {
                messageBox.ShowError("权限不足");
                return;
            }
            if (SelectedRow == null) return;

            string action = SelectedRow.CanPauseOthersOnException ? "关闭异停" : "开启异停";
            if (!ConfirmOperation(action, SelectedRow.ProcessName)) return;

            bool oldValue = SelectedRow.CanPauseOthersOnException;
            bool newValue = !oldValue;
            bool ok = monitorService.SetProcessPauseOthersOnException(SelectedRow.ProcessName, newValue);
            if (ok)
            {
                var idx = ProcessRows.IndexOf(SelectedRow);
                if (idx >= 0)
                {
                    SelectedRow.CanPauseOthersOnException = newValue;
                    ProcessRows.ResetItem(idx);
                }
                OperationFeedback = $"[{SelectedRow.ProcessName}] 异停: {(newValue ? "是" : "否")}";
            }
            else
            {
                OperationFeedback = $"[{SelectedRow.ProcessName}] 异停设置失败";
            }

            // 同启用开关：明细只在数据真的变了时落
            auditService.Record(new AuditEntry(AuditCategory.Process, AuditAction.TogglePauseOthers,
                SelectedRow.ProcessName, userSession.CurrentUser.ToAuditActor(),
                ok ? AuditResult.Success : AuditResult.Fail)
            {
                Changes = ok
                    ? new List<AuditFieldChange>
                    {
                        new AuditFieldChange
                        {
                            FieldPath = "异停",
                            ChangeType = AuditChangeType.Update,
                            OldValue = oldValue ? "是" : "否",
                            NewValue = newValue ? "是" : "否"
                        }
                    }
                    : null
            });
        }

        #endregion

        #region 辅助

        private bool GetProcessEnabled(string processName)
        {
            return monitorService.GetProcessConfig(processName)?.IsEnabled ?? true;
        }

        private bool GetProcessPauseOthersConfig(string processName)
        {
            return monitorService.GetProcessConfig(processName)?.PauseOthersOnException ?? true;
        }

        #endregion
    }

    #region 流程监控行

    /// <summary>
    /// 流程监控行数据模型
    /// 纯 POCO，无属性变更通知，由 View 定时轮询刷新整表
    /// </summary>
    public class ProcessMonitorRow
    {
        [Description("流程名称")]
        public string ProcessName { get; set; }

        [Description("快照时间")]
        public string TimestampText => Timestamp.ToString("HH:mm:ss.fff");

        [Description("类别")]
        public string Category { get; set; }

        [Description("状态")]
        public string StatusText => GetStatusText();

        [Description("步序")]
        public int Step { get; set; }

        [Description("异常信息")]
        public string ExceptionDisplay => GetExceptionDisplay();

        [Description("启用")]
        public string IsEnabledText => IsEnabled ? "是" : "否";

        [Description("异停")]
        public string CanPauseOthersText => CanPauseOthersOnException ? "是" : "否";

        public ProcessState State { get; set; }
        public IReadOnlyCollection<ProcessTrigger> PermittedTriggers { get; set; }
        public string ExceptionCode { get; set; }
        public string ExceptionMessage { get; set; }
        public bool IsEnabled { get; set; }
        public bool CanPauseOthersOnException { get; set; }
        public DateTime Timestamp { get; set; }

        /// <summary>"启动/继续"两义合一：按状态机运行时求值的合法触发器判定（与状态机同源）</summary>
        public bool CanStart => PermittedTriggers.Contains(ProcessTrigger.Start)
            || PermittedTriggers.Contains(ProcessTrigger.Resume);

        /// <summary>可暂停：仅 Running 态（状态机此刻允许 Pause）</summary>
        public bool CanPause => PermittedTriggers.Contains(ProcessTrigger.Pause);

        /// <summary>可复位：仅 Exception 态（状态机此刻允许 Reset）</summary>
        public bool CanReset => PermittedTriggers.Contains(ProcessTrigger.Reset);

        public bool CanInitialize => true;

        /// <summary>启动按钮文本：状态机此刻允许 Start（待命/停止态）即"启动"，与运行时同源</summary>
        public string StartButtonText => PermittedTriggers.Contains(ProcessTrigger.Start) ? "启动" : "继续";

        private string GetStatusText()
        {
            return State switch
            {
                ProcessState.Idle => "待机",
                ProcessState.Running => "运行中",
                ProcessState.Paused => "已暂停",
                ProcessState.Exception => "异常",
                ProcessState.Stopped => "已停止",
                _ => "未知"
            };
        }

        private string GetExceptionDisplay()
        {
            return !string.IsNullOrEmpty(ExceptionCode)
                ? $"[{ExceptionCode}] {ExceptionMessage}"
                : "-";
        }
    }

    #endregion
}