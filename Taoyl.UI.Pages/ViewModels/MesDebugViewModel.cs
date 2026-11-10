using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.MesDriver.Interfaces;
using Taoyl.Application.MesDriver.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.Models.Mes;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 右栏会话态。新增与编辑都要能改草稿，但「保存时传不传旧名」不同——
    /// 用一个额外的 bool 位区分会造出「浏览态 + 新增中」这种不存在却可达的组合。
    /// </summary>
    public enum MesDebugSession
    {
        /// <summary>浏览态：右栏只读展示当前选中接口，发送可用</summary>
        Browse,

        /// <summary>新增态：右栏是还没进配置的空草稿</summary>
        Add,

        /// <summary>编辑态：右栏是已存在接口的草稿，可改；发送禁用（发的必须是已保存的配置）</summary>
        Edit
    }

    /// <summary>
    /// MES 调试页 ViewModel。分工：本文件管页面级状态与命令；MesApiEditModel 是右栏字段的 UI 镜像；
    /// MesDebugViewModel.Edit.cs 管编辑会话（切接口 Guard / 保存取消 / 校验 / 审计）。
    ///
    /// 🔴 发送只能发已保存的配置：走 Invoke(SelectedApiName) 不带 body。若允许编辑态发送，
    /// 发的是未保存内容，而日志记的 ApiName 指向磁盘上的旧配置，事后按日志复现不出来
    /// ——所以 CanSend 含 !IsEditing，发送中切换接口也被 TrySwitchApi 挡掉。
    /// 🔴 权限门禁判在**命令体首行内联**，不放 CanExecute——CanXxx 只表达业务可用性
    /// （有无选中 / 会话态 / 是否发送中），权限档位调整不必重算全部 CanXxx。
    /// </summary>
    public partial class MesDebugViewModel : ObservableObject
    {
        #region 依赖注入

        private readonly IMesConfigManager configManager;
        private readonly IMesClient mesClient;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBox;
        private readonly IAuditService auditService;
        private readonly IUserSession userSession;

        #endregion

        #region 右栏编辑模型（浏览态与编辑态共用，永不为 null）

        /// <summary>
        /// 右栏草稿。**只读引用**：装载走 <see cref="MesApiEditModel.LoadFrom"/> 原地灌值，不换实例。
        /// 🔴 界面绑定直接持有这个对象（不是「ViewModel.Editing.Xxx」这种两段路径），换实例会让所有绑定
        /// 静默指向旧对象——用只读属性把这类不报错的错位挡在编译期。
        /// </summary>
        public MesApiEditModel Editing { get; } = new MesApiEditModel();

        /// <summary>
        /// 草稿属性变化时只补一件事：<see cref="CanFormatBody"/> 的派生通知
        /// （它组合了「草稿形态」与「会话态」，跨对象派生，注解挂不上）。
        /// View 要的那份通知直接订阅 <see cref="Editing"/> 的 PropertyChanged，不在本类转发一次。
        /// </summary>
        private void OnDraftPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(MesApiEditModel.BodyType))
                OnPropertyChanged(nameof(CanFormatBody));
        }

        #endregion

        public MesDebugViewModel(
            IMesConfigManager configManager,
            IMesClient mesClient,
            IAuthService authService,
            IMessageBoxService messageBox,
            IAuditService auditService,
            IUserSession userSession)
        {
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.mesClient = mesClient ?? throw new ArgumentNullException(nameof(mesClient));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));

            // 草稿是只读引用（永不换实例），订阅一次终身有效，不需要退订记账
            Editing.PropertyChanged += OnDraftPropertyChanged;
        }

        #region 列表与选中

        /// <summary>左栏接口名列表（按 Mes.json 中的顺序）。</summary>
        public BindingList<ApiNameItem> ApiNames { get; } = new BindingList<ApiNameItem>();

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendCommand))]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        [NotifyPropertyChangedFor(nameof(CanSend))]
        private string selectedApiName;

        /// <summary>左栏选中变化：浏览态直接装载显示；编辑态的切换由 TrySwitchApi 先做 Guard 再设值。</summary>
        partial void OnSelectedApiNameChanged(string value)
        {
            if (Session == MesDebugSession.Browse)
                LoadDisplay(value);
        }

        /// <summary>
        /// 从配置管理器重新读取接口名列表。
        /// 保留当前选中项（若它还在）；否则选第一条；一条都没有就清空右栏。
        /// preferKeep：调用方已知「接下来该选谁」时直接指定（保存改名场景），
        /// 不传则维持原行为（读当前 SelectedApiName）。
        /// </summary>
        public void ReloadApiList(string preferKeep = null)
        {
            string keep = preferKeep ?? SelectedApiName;

            // 批量更新：关掉逐项通知再灌数据，最后只发一次 Reset。
            // 否则列表控件每 Add 一条就重绘一次，列表长了就是肉眼可见的抖动。
            ApiNames.RaiseListChangedEvents = false;
            try
            {
                ApiNames.Clear();

                // 直接遍历快照，不用 GetApiNames()：左栏要显示接口描述，逐个 GetApi 会深拷贝 N 次。
                // Current 是发布后冻结的快照引用，取一次全程用一份（与驱动 FindApi 同规则）。
                MesConfig snapshot = configManager.Current;
                if (snapshot != null && snapshot.Apis != null)
                {
                    foreach (MesApiDefinition api in snapshot.Apis)
                    {
                        if (api != null)
                            ApiNames.Add(new ApiNameItem(api.Name, api.DisplayName));
                    }
                }
            }
            finally
            {
                ApiNames.RaiseListChangedEvents = true;
                ApiNames.ResetBindings();
            }

            if (!string.IsNullOrEmpty(keep) && ContainsApiName(keep))
            {
                SelectedApiName = keep;
            }
            else if (ApiNames.Count > 0)
            {
                SelectedApiName = ApiNames[0].Name;
            }
            else
            {
                SelectedApiName = null;
                LoadDisplay(null);
            }
        }

        private bool ContainsApiName(string name)
        {
            return ApiNames.Any(item => string.Equals(item.Name, name, StringComparison.Ordinal));
        }

        /// <summary>页面首次装载入口（View 的 OnLoad 调用）。</summary>
        public void Initialize()
        {
            ReloadApiList();
        }

        #endregion

        #region 会话状态

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsEditing))]
        [NotifyPropertyChangedFor(nameof(IsAdding))]
        [NotifyPropertyChangedFor(nameof(IsReadOnly))]
        [NotifyPropertyChangedFor(nameof(IsEditable))]
        [NotifyPropertyChangedFor(nameof(CanFormatBody))]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanAdd))]
        [NotifyPropertyChangedFor(nameof(CanSave))]
        [NotifyPropertyChangedFor(nameof(CanCancel))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        [NotifyPropertyChangedFor(nameof(CanSend))]
        [NotifyCanExecuteChangedFor(nameof(SendCommand))]
        private MesDebugSession session;

        /// <summary>是否处于编辑态（新增也算：右栏都在改草稿）。</summary>
        public bool IsEditing => Session != MesDebugSession.Browse;

        /// <summary>是否为「新增」——只决定保存时传不传旧名。</summary>
        public bool IsAdding => Session == MesDebugSession.Add;

        /// <summary>右栏字段是否只读（绑定到 TextBox 的 ReadOnly）。</summary>
        public bool IsReadOnly => !IsEditing;

        /// <summary>右栏字段是否可编辑（绑定到控件的 Enabled；IsReadOnly 的取反，绑定不支持取反所以单列一个）。</summary>
        public bool IsEditable => !IsReadOnly;

        /// <summary>
        /// 格式化按钮是否可用：该形态可格式化且处于可编辑态（是否显示由 View 按形态表决定）。
        /// 🔴 直接查形态表——「能不能格式化」只在 MesBodyTypeUi 一处定义，草稿侧不再转发一次。
        /// </summary>
        public bool CanFormatBody => IsEditable && MesBodyTypeUi.For(Editing.BodyType).CanFormat;

        #endregion

        #region 结果区

        /// <summary>请求文本（可粘进 Postman 复现）。</summary>
        [ObservableProperty]
        private string requestText;

        /// <summary>响应正文（原样，不解析）。</summary>
        [ObservableProperty]
        private string responseText;

        /// <summary>状态条 · 接口名。</summary>
        [ObservableProperty]
        private string statusApiName = "-";

        /// <summary>状态条 · 状态码（无响应时显示「无响应」）。</summary>
        [ObservableProperty]
        private string statusHttpCode = "-";

        /// <summary>状态条 · 耗时。</summary>
        [ObservableProperty]
        private string statusElapsed = "-";

        /// <summary>状态条 · 起止时间。</summary>
        [ObservableProperty]
        private string statusTimeRange = "-";

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(SendCommand))]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        [NotifyPropertyChangedFor(nameof(CanAdd))]
        [NotifyPropertyChangedFor(nameof(CanDelete))]
        [NotifyPropertyChangedFor(nameof(CanSend))]
        private bool isBusy;

        #endregion

        #region 按钮可用性（View 用 DataBindings 绑到各按钮的 Enabled）

        /// <summary>业务可用性，不含权限判断（权限在命令体首行内联，见各命令）。</summary>

        /// <summary>浏览态 + 已选中 + 不在发送中。发送中锁住操作面——否则能删掉正在飞的接口。</summary>
        public bool CanEdit => !IsEditing && !IsBusy && !string.IsNullOrEmpty(SelectedApiName);

        /// <summary>浏览态且不在发送中（新增不依赖是否选中）。</summary>
        public bool CanAdd => !IsEditing && !IsBusy;

        /// <summary>只有编辑态能保存。</summary>
        public bool CanSave => IsEditing;

        /// <summary>只有编辑态能取消。</summary>
        public bool CanCancel => IsEditing;

        /// <summary>浏览态 + 已选中 + 不在发送中。</summary>
        public bool CanDelete => !IsEditing && !IsBusy && !string.IsNullOrEmpty(SelectedApiName);

        /// <summary>浏览态 + 已选中 + 不在发送中。</summary>
        public bool CanSend => !IsEditing && !IsBusy && !string.IsNullOrEmpty(SelectedApiName);

        #endregion

        #region 发送

        /// <summary>
        /// 进行中的发送。只在 SendAsync 生命周期内非空——它与 IsBusy 同生共死，
        /// 所以 CancelSend 判空即可，不需要额外的「取消中」状态位。
        /// </summary>
        private CancellationTokenSource sendCts;

        /// <summary>
        /// 取消进行中的发送。无权限门禁——取消自己发起的请求不构成操作面。
        /// 🔴 只置取消信号，不碰 IsBusy：IsBusy 统一由 SendAsync 的 finally 收尾。
        /// 这样「发送 / 取消」两个按钮永远由同一个状态的两个极性派生，构造上不可能同时可用。
        /// </summary>
        [RelayCommand]
        private void CancelSend()
        {
            if (!IsBusy || sendCts == null)
                return;

            StatusHttpCode = "取消中…";   // 立刻给文字反馈，比让按钮变灰更明确
            sendCts.Cancel();
        }

        /// <summary>
        /// 发送一次。走 Invoke(apiName) 不带 body——驱动用 Mes.json 里的 Body 原文发出，
        /// 与流程调用完全同一条代码路径（差别只是流程会额外传 body 覆盖层）。
        /// 🔴 UI 线程不能同步等：Invoke 是同步阻塞的（最长 TimeoutMs），
        /// 必须丢到线程池，否则界面卡死最长 15 秒。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanSend))]
        private async Task SendAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnMesDebugSend))
            {
                messageBox.ShowError("权限不足");
                return;
            }

            string target = SelectedApiName;
            if (string.IsNullOrEmpty(target))
                return;

            IsBusy = true;
            StatusApiName = target;
            StatusHttpCode = "发送中…";
            StatusElapsed = "-";
            StatusTimeRange = "-";

            // 局部变量把令牌固定住：Task.Run 的 lambda 在工作线程上执行，
            // 不依赖字段在那一刻是否还是原值
            var cts = new CancellationTokenSource();
            sendCts = cts;

            // 提到 try 外：finally 要据此判断「到底拿没拿到结果」
            MesResult result = null;

            try
            {
                result = await Task.Run(() => mesClient.Invoke(target, null, cts.Token));

                RequestText = result.Request;
                ResponseText = result.Response;
                ApplyStatus(result);
            }
            catch (Exception ex)
            {
                // Invoke 自身永不抛；这里兜的是 Task.Run 层面的意外（OOM 之类）
                messageBox.ShowError("发送失败：" + ex.Message);
                StatusHttpCode = "异常";
            }
            finally
            {
                // 🔴 只在「根本没拿到结果」时才补「已取消」：拿到结果说明请求已经走完，
                // 不能因为用户晚一拍点了取消就把成功状态改掉——那会出现
                // 「状态条写着已取消、响应框里却是成功报文」的自相矛盾。
                if (result == null && cts.IsCancellationRequested)
                    StatusHttpCode = "已取消";

                // CancelSend 与本方法同在 UI 线程串行执行（await 后回到同步上下文），
                // Dispose 不会与 Cancel 撞车
                cts.Dispose();
                sendCts = null;
                IsBusy = false;
            }
        }

        private void ApplyStatus(MesResult result)
        {
            StatusApiName = result.ApiName;
            // 有响应就显示状态码；没响应则由驱动给的失败分类说明原因（超时 / 连接失败 / 配置错误…），
            // 不再一律写「无响应」逼人去读 Response 里的文案
            StatusHttpCode = result.HttpStatus.HasValue
                ? result.HttpStatus.Value.ToString()
                : MesFailureText.Of(result.Failure);
            // 重试过才显示次数：1 次（未重试）与旧观感一致，不额外加字
            StatusElapsed = result.Attempts > 1
                ? result.Attempts + "次 · " + result.ElapsedMs + "ms"
                : result.ElapsedMs + "ms";
            StatusTimeRange = string.Format("{0:HH:mm:ss.fff} → {1:HH:mm:ss.fff}",
                result.StartTime, result.EndTime);
        }

        #endregion

        #region 正文格式化

        /// <summary>
        /// 格式化当前形态的正文。只改编辑器内容，不动存储的 Body 原文。
        /// 🔴 正文只有一份（BodyRawText），Json / Text / Xml 共用，所以这里不需要按形态挑字段。
        /// 🔴 两条前置不满足时**必须给出提示**：静默 return 会让"点了没反应"无法区分是
        /// 形态不支持、正文为空，还是格式化结果与原值相同。
        /// </summary>
        [RelayCommand]
        private void FormatBody()
        {
            MesBodyTypeUi.UiInfo ui = MesBodyTypeUi.For(Editing.BodyType);
            if (!ui.CanFormat)
            {
                messageBox.ShowWarning("当前形态不支持文本格式化：" + Editing.BodyType
                    + "（只有 JSON 与 XML 可以）");
                return;
            }

            string text = Editing.BodyRawText;
            if (string.IsNullOrWhiteSpace(text))
            {
                messageBox.ShowWarning("正文是空的，没有可格式化的内容。");
                return;
            }

            try
            {
                string formatted = ui.Format(text);

                // 三档提示构成完整诊断：形态不支持 / 编辑器为空 / 结果无变化。
                // 四种"点了没反应"的可能只剩一种没提示——写回成功但控件没刷新（View 端投影缺失）。
                if (string.Equals(formatted, text, StringComparison.Ordinal))
                {
                    messageBox.ShowInfo("格式化结果与原内容相同，无需改动。");
                    return;
                }

                Editing.BodyRawText = formatted;
            }
            catch (Exception ex)
            {
                // 委托内部已带上形态名（"正文不是合法 JSON/XML：…"）
                messageBox.ShowError(ex.Message);
            }
        }

        #endregion

        #region 浏览态装载

        /// <summary>把指定接口的当前配置灌进右栏草稿（原地装载，不换实例）。</summary>
        private void LoadDisplay(string name)
        {
            Editing.LoadFrom(string.IsNullOrEmpty(name) ? null : configManager.GetApi(name));
        }

        #endregion
    }
}
