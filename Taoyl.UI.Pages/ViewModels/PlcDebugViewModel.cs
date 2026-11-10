using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using Taoyl.Application.Audit.Interfaces;          // IAuditService（审计留痕投递）
using Taoyl.Application.AuthService.Interfaces;    // IAuthService / IUserSession
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.PlcDriver.Config;          // IPlcConfigManager
using Taoyl.Application.PlcDriver.Models.Config;   // PlcConfig（ConfigDraft/快照字段）
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.Models;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// PLC 调试页编辑会话状态机（2026-09-08 枚举化：替代原 IsEditing/IsConfigEditing 双互斥 bool——
    /// 两会话互斥由枚举单值天然保证，状态流转收敛为 Session 单点赋值）
    /// </summary>
    public enum PlcDebugSession
    {
        /// <summary>浏览态：无任何编辑会话（地址表只读 + 配置区只读，连接/读写/导出随时可用）</summary>
        None,

        /// <summary>地址表编辑会话（信息列可编辑；子态=组级/子级操作域，见 IsGroupScope）</summary>
        TagTableEdit,

        /// <summary>连接配置编辑会话（配置区草稿可编辑渲染；子态 IsPlcAdding=新增标志）</summary>
        ConfigEdit,
    }

    /// <summary>
    /// PLC 调试页面 ViewModel（纯运行时调试器：地址表工作集模型 + 连接配置运行时管理）
    ///
    /// 职责边界（防翻案锚点，2026-09-07 架构重构更新）：
    /// - 运行时设备配置管理已启用（推翻旧"停机改文件"锚点）：连接配置「保存」= 走 IDeviceOrchestration 编排口
    ///   （AddDevice 新增 / UpdateDevice 保存生效 / DeleteDevice 删除——内部=校验/查重/缓存同步 + 配置落盘 + 实例重建）；
    ///   保存/删除后实例重建，旧引用失效 → 列表重取（重新 GetDevice 惯例）。本页宽口注入 IDeviceOrchestration，
    ///   实例获取/连接（IDeviceManager 域）与配置读写（IDeviceConfigManager 域）均经同一入口，无需自判"问谁"。
    /// - 连接/读写测试/地址表导出 为调试操作不随编辑会话门禁；保存重建后连接状态=断开（重建不含连接，
    ///   连接由「连接」按钮显式执行——补丁二决策）；地址表 xlsx 文件不在删除/改名时清理（现场资产）。
    /// - 数据流三条通道，纽带 = 实例 Name，每次现取：
    ///   ① 配置信息区 = 浏览态 IPlcConfigManager.GetPlcConfig(name) 只读渲染；编辑态 = 草稿 ConfigDraft 可编辑渲染
    ///   （深拷贝副本隔离：字段直绑草稿，保存才经编排提交；取消=弃副本零副作用）
    ///   ② 地址表 = GetTagTable(name) → 组下拉 + 当前组地址表工作集（TagRows：编辑缓冲，点保存才提交组表 + 落盘）
    ///   ③ 连接/读写 = orchestration.GetDevice(name).GetRawDev&lt;IPlc&gt;()；读写统一 ExecuteIo（值自动回填容器）
    ///
    /// 反馈契约（R3）：失败/警告弹窗，成功静默（保存附一句重连提示——重建后连接态=断开，R3 例外属必要告知）
    ///
    /// 编辑会话状态机（2026-09-08 枚举化）：Session ∈ { None, TagTableEdit, ConfigEdit }——地址表编辑与连接配置编辑
    /// 互斥由枚举单值保证（原双 bool IsEditing/IsConfigEditing 收敛）；IsEditing/IsConfigEditing 保留为只读派生便捷属性。
    /// 会话子态：ConfigEdit 内 IsPlcAdding（新增/编辑标志）；TagTableEdit 内（后续批）组级/子级操作域 RadioButton。
    /// （Guard：进入任一会话先收尾另一会话，脏→确认；切实例强制退配置会话 + 清草稿）
    ///
    /// 工作集模型（编辑事务隔离）：界面增删改全部落在 TagItemRow 副本（不污染驱动正在用的组表），
    /// 「保存」= 预检（重名/必填整单拒存）→ ApplyEdits（先删后增/先挂后摘/属性回写/DataType 容器重建）→ 落盘（.bak+覆盖）；
    /// 行编辑 CellValueChanged 置脏（WriteValue 点动写缓冲除外）；1s 节拍 RefreshTagRowValues 只刷展示列不碰编辑列，
    /// 变化行 ResetItem 行级通知（编辑中行豁免）；脏行明黄/新增行淡青由 View CellFormatting 按行状态上色。
    /// 未保存切组/切实例/导入 = ConfirmDiscardPending 确认（先保存/放弃/留下）；删除 = 工作集移除 + 删除清单（切走=自动撤销）。
    /// 已知边界：ApplyEdits 回写与 IoWorker 并发 = CD 弱一致遍历 + 单点原子查找 + 实例永不销毁，最坏差一拍自愈（IoWorker 外部零启停定稿）
    ///
    /// partial 按域分文件（组织拆分，行为零变）：
    /// - 本文件 = 状态声明 + 页面导航装载（壳：可观察属性/CanXxx/选中钩子/实例列表/现取三块）；
    /// - PlcDebugViewModel.Config.cs = 连接配置域（设备管理：新增/编辑/保存/取消/删除/重建 + 会话 Guard + 草稿事务）；
    /// - PlcDebugViewModel.TagTable.cs = 地址表域（工作集节拍与通知 / 行操作 / 刷新频率 / 保存事务）；
    /// - PlcDebugViewModel.Io.cs = 运行时交互（连接断开 / 导入导出 / 读写测试）。
    /// </summary>
    public partial class PlcDebugViewModel : ObservableObject
    {
        #region 依赖注入

        /// <summary>设备编排门面（宽口）：实例获取/连接 + 配置读写 + 增删改业务用例（AddDevice/UpdateDevice/DeleteDevice）单入口。
        /// 窄口隔离：本页是"设备管理 UI"，全能力走编排口；进程层/配置中心等只需单域能力的调用方另注窄口。</summary>
        private readonly IDeviceOrchestration orchestration;
        private readonly IPlcConfigManager configManager;
        private readonly IAuthService authService;
        private readonly IMessageBoxService messageBox;
        private readonly IUserSession userSession;
        private readonly IAuditService auditService;

        #endregion

        #region 连接防抖

        /// <summary>连接/断开执行中（防重复点击；变化即时转发连接两按钮使能，无需等 1s 节拍）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConnect))]
        [NotifyPropertyChangedFor(nameof(CanDisconnect))]
        private bool connectionBusy;

        #endregion

        #region 可观察属性

        /// <summary>PLC实例列表（行=DeviceRow 持 IDevice；连接状态列绑 ConnectionText，VM RefreshDeviceRows 节拍刷新）</summary>
        [ObservableProperty]
        private BindingList<DeviceRow> devices = new BindingList<DeviceRow>();

        /// <summary>当前选中实例行（变化转发全部实例级按钮使能）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanConnect))]
        [NotifyPropertyChangedFor(nameof(CanDisconnect))]
        [NotifyPropertyChangedFor(nameof(CanImport))]
        [NotifyPropertyChangedFor(nameof(CanExport))]
        [NotifyPropertyChangedFor(nameof(CanSaveTag))]
        [NotifyPropertyChangedFor(nameof(CanEditTag))]
        [NotifyPropertyChangedFor(nameof(CanAddRow))]
        [NotifyPropertyChangedFor(nameof(CanSetAll))]
        [NotifyPropertyChangedFor(nameof(CanEditPlc))]
        [NotifyPropertyChangedFor(nameof(CanDeletePlc))]
        [NotifyPropertyChangedFor(nameof(CanRebuild))]
        private DeviceRow selectedDevice;

        /// <summary>当前选中组名（真实组表键；由组 DGV 选中行 SourceGroupName 派生——编辑态改名未提交前标签表仍对旧键）。
        /// 变化驱动标签工作集重建（RebuildTagRows）；null=无当前组</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditTag))]
        [NotifyPropertyChangedFor(nameof(CanAddRow))]
        [NotifyPropertyChangedFor(nameof(CanSetAll))]
        private string selectedGroupName;

        /// <summary>
        /// 编辑会话状态机（2026-09-08 枚举化：原 IsEditing/IsConfigEditing 互斥 bool 收敛为单值枚举——
        /// 互斥由类型保证，赋值 Session 天然单会话，Guard 只负责"脏确认/收尾另一会话"，不负责"防并存"）：
        /// - None（浏览态）：地址表信息列只读、配置区只读，仅 读写测试/连接/导出/点编辑/新增设备 可用；
        /// - TagTableEdit（地址表编辑会话）：行级编辑/增删移/设频率/导入/保存/取消 可用（变更类操作统一须先进编辑会话）；
        /// - ConfigEdit（连接配置编辑会话）：配置区草稿可编辑渲染（IsPlcAdding 子态区分新增/编辑）。
        /// 退出路径（v3 钉死）：保存成功/取消/切实例 = None；导入成功保持 TagTableEdit（不落盘，落盘经保存）；
        /// 切组保持当前值（跨组会话，脏拦截在 View）。View 订阅变化驱动 DGV 列只读切换 + 配置区双态渲染。
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanEditTag))]
        [NotifyPropertyChangedFor(nameof(CanSaveTag))]
        [NotifyPropertyChangedFor(nameof(CanCancelTag))]
        [NotifyPropertyChangedFor(nameof(CanImport))]
        [NotifyPropertyChangedFor(nameof(CanAddRow))]
        [NotifyPropertyChangedFor(nameof(CanSetAll))]
        [NotifyPropertyChangedFor(nameof(CanSetCurrent))]
        [NotifyPropertyChangedFor(nameof(CanRemoveRow))]
        [NotifyPropertyChangedFor(nameof(CanMoveRow))]
        [NotifyPropertyChangedFor(nameof(CanAddPlc))]
        [NotifyPropertyChangedFor(nameof(CanEditPlc))]
        [NotifyPropertyChangedFor(nameof(CanSavePlc))]
        [NotifyPropertyChangedFor(nameof(CanCancelPlc))]
        [NotifyPropertyChangedFor(nameof(CanDeletePlc))]
        [NotifyPropertyChangedFor(nameof(CanRebuild))]
        private PlcDebugSession session = PlcDebugSession.None;

        /// <summary>地址表编辑会话激活中（Session 只读派生便捷属性——读点兼容旧 IsEditing；禁止赋值，写会话一律走 Session=）</summary>
        public bool IsEditing => Session == PlcDebugSession.TagTableEdit;

        /// <summary>连接配置编辑会话激活中（Session 只读派生便捷属性——读点兼容旧 IsConfigEditing；禁止赋值，写会话一律走 Session=）</summary>
        public bool IsConfigEditing => Session == PlcDebugSession.ConfigEdit;

        /// <summary>新增草稿子态（true=新增无实例草稿，保存走编排口 AddDevice；false=编辑已有实例，保存走 UpdateDevice）。
        /// 恒伴随 Session=ConfigEdit（IsPlcAdding 仅是保存路径/旧名来源的标志位）；
        /// 转发 CanDeletePlc——删除仅编辑子态可用（新增草稿无实体可删，2026-09-10）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanDeletePlc))]
        private bool isPlcAdding;

        /// <summary>连接配置编辑草稿（深拷贝副本：进会话现取缓存配置 Clone，改字段直绑草稿；保存提交/取消弃副本零副作用）</summary>
        [ObservableProperty]
        private PlcConfig configDraft;

        /// <summary>设备管理操作执行中（保存/删除/重建编排防重入；编排内实例释放/重建可能阻塞至旧实例 IO 超时，须 Task.Run 异步执行）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanSavePlc))]
        [NotifyPropertyChangedFor(nameof(CanDeletePlc))]
        [NotifyPropertyChangedFor(nameof(CanRebuild))]
        private bool configBusy;

        /// <summary>进配置会话时的快照（脏检测：草稿 vs 快照逐字段 ContentEquals——取消/切实例拦截依据）</summary>
        private PlcConfig configSnapshot;

        /// <summary>组 DGV 工作集（TagGroupRow 编辑缓冲：保存才提交组表；切实例/放弃重建。全组列表非"当前组"隔离——组与标签双轨工作集）</summary>
        [ObservableProperty]
        private BindingList<TagGroupRow> groupRows = new BindingList<TagGroupRow>();

        /// <summary>当前选中组行（组 DGV 选中行；变化转发组级按钮使能）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRemoveRow))]
        [NotifyPropertyChangedFor(nameof(CanMoveRow))]
        private TagGroupRow selectedGroupRow;

        /// <summary>
        /// 组 DGV 选中闸门（程序化重建 vs 用户切组）：LoadGroupRows 起始置位——重绑后组 DGV 内部自动落首行
        /// 非用户切组意图，View 的 SelectionChanged 据此跳过 ConfirmDiscardTagScope 脏确认、直接同步 VM
        /// （保留"切实例自动选首组"语义）；用户点击组行（CellMouseDown）/键盘（KeyDown）调 UnlockGroupSelection 解锁。
        /// ⚠️ 置位必须在重绑之前：原实现放 View.DataBindingComplete 置位，该事件在 DGV 处理完 Reset（含自动选中）
        /// 之后才触发 → 闸门来不及拦住那次自动选中（2026-09-10 取消路径多弹一次"是否先保存"的根因）。
        /// 闸门归属 VM 的理由：只有 VM 知道"是否程序化重建"，View 只知道"是否用户交互"（解锁）。
        /// </summary>
        public bool GroupSelectionLocked { get; private set; } = true;

        /// <summary>用户显式选择组行（View 组 DGV CellMouseDown/KeyDown 调）→ 解锁组选中闸门，恢复切组脏确认</summary>
        public void UnlockGroupSelection()
        {
            GroupSelectionLocked = false;
        }

        /// <summary>
        /// 设备 DGV 选中闸门（程序化重载 vs 用户点选）：LoadDevices 重绑前置位——ResetBindings 会让设备 DGV
        /// 自动落首行（旧选中行已随 Clear 消失），而该行往往不是 VM 要选的目标行（如删除后要选的是"邻行"），
        /// 故 View 的 SelectionChanged 据此跳过写 SelectedDevice，由调用方在重载后显式赋值唯一收敛。
        /// 不加闸门的后果：中间轮把 Devices[0] 写进 SelectedDevice → 配置区先重绘一次错误行（残留"请选中实例"观感 + 闪烁），
        /// 再被目标行重绘覆盖。用户点击设备行（CellMouseDown）/键盘（KeyDown）→ UnlockPlcSelection 解锁。
        /// 闸门归属 VM 的理由同 GroupSelectionLocked：只有 VM 知道"是否程序化重载"。
        /// </summary>
        public bool PlcSelectionLocked { get; private set; }

        /// <summary>用户显式选择设备行（View 设备 DGV CellMouseDown/KeyDown 调）→ 解锁设备选中闸门</summary>
        public void UnlockPlcSelection()
        {
            PlcSelectionLocked = false;
        }

        /// <summary>操作域（radio 组级/子级）：true=组级（增删移按钮作用于组 DGV 选中行）；false=子级（作用于标签行）。
        /// 纯派生 UI 态：读写测试/设频率始终子级（CanReadWrite/CanSetCurrent 前置 !IsGroupScope）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanAddRow))]
        [NotifyPropertyChangedFor(nameof(CanRemoveRow))]
        [NotifyPropertyChangedFor(nameof(CanMoveRow))]
        [NotifyPropertyChangedFor(nameof(CanReadWrite))]
        [NotifyPropertyChangedFor(nameof(CanSetCurrent))]
        [NotifyPropertyChangedFor(nameof(CanSetAll))]
        private bool isGroupScope;

        /// <summary>当前组地址表工作集（TagItemRow 编辑缓冲：保存才提交组表；切组/切实例重建）</summary>
        [ObservableProperty]
        private BindingList<TagItemRow> tagRows = new BindingList<TagItemRow>();

        /// <summary>当前选中工作行（变化转发标签级按钮使能；未保存草稿行禁读/写/设频率）</summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(CanRemoveRow))]
        [NotifyPropertyChangedFor(nameof(CanMoveRow))]
        [NotifyPropertyChangedFor(nameof(CanReadWrite))]
        [NotifyPropertyChangedFor(nameof(CanSetCurrent))]
        private TagItemRow selectedRow;

        /// <summary>配置信息区扁平行（只读渲染）</summary>
        [ObservableProperty]
        private List<ConfigFlatRow> configRows = new List<ConfigFlatRow>();

        /// <summary>当前选中实例 IO 队列深度（运行指标 = IoWorker 待处理请求数；1s 节拍 RefreshPlcQueueDepth 现取编排口，
        /// 切实例钩子即时刷新——无选中/取不到=0。PLC 专属运行量不入 DeviceRow（通用设备行零 PLC 依赖），View 顶栏 label 绑定本值）</summary>
        [ObservableProperty]
        private int currentQueueDepth;

        // ---- 按钮使能：只读计算属性（数据驱动——本地依赖 setter 转发，连接状态由 1s 节拍感知）----
        // 依赖三类：
        // ① 本地可观察状态（SelectedDevice/SelectedGroupName/SelectedRow/ConnectionBusy/Session）：字段 [NotifyPropertyChangedFor] 注解转发，变化即通知；
        // ② 连接状态 IsConnected：驱动后台真值、无事件源（PlcDriver 定稿不感知 UI），只能由 View 1s 节拍调 RefreshConnectionButtons 感知。
        // ③ 行脏态（SelectedRow.IsNew/IsDirty）：草稿行禁读/写/设频率，RaiseDirtyRelated 集中通知。
        // 故无 RefreshTick 全量重算；下方表达式 + 通知源 = 按钮态"数据驱动 + 连接节拍 + 脏态联动"。

        /// <summary>可连接：选中实例且未连接且不在连接/断开执行中</summary>
        public bool CanConnect => SelectedDevice != null && !IsSelectedConnected() && !ConnectionBusy;

        /// <summary>可断开：选中实例且已连接且不在连接/断开执行中</summary>
        public bool CanDisconnect => SelectedDevice != null && IsSelectedConnected() && !ConnectionBusy;

        /// <summary>可导入地址表：地址表编辑会话内（导入=整体替换地址表属变更操作，统一门禁）</summary>
        public bool CanImport => SelectedDevice != null && Session == PlcDebugSession.TagTableEdit;

        /// <summary>可导出地址表：选中实例即可（纯读零副作用，改前备份/取表随时可用；导出源=已提交内存态不含草稿）</summary>
        public bool CanExport => SelectedDevice != null;

        /// <summary>可保存落盘：地址表编辑会话内（无编辑无变更无需保存；导入覆盖内存后的固化也走保存按钮）</summary>
        public bool CanSaveTag => SelectedDevice != null && Session == PlcDebugSession.TagTableEdit;

        /// <summary>可进入编辑会话：选中实例、不在任何编辑会话（组/标签双工作集共用一个 TagTableEdit 会话；无需选中组——组列表编辑不需要当前组）</summary>
        public bool CanEditTag => SelectedDevice != null && Session == PlcDebugSession.None;

        /// <summary>可取消编辑会话：仅地址表编辑态内（有脏先确认放弃）</summary>
        public bool CanCancelTag => Session == PlcDebugSession.TagTableEdit;

        // ---- 地址表域按钮使能（radio 组级/子级双轨：CanAddRow/CanRemoveRow/CanMoveRow 按 IsGroupScope 分流；读/写/设频率仅子级）----
        // 组级：操作对象=组 DGV 选中行（SelectedGroupRow）；子级：操作对象=标签 DGV 选中行（SelectedRow）。
        // 编辑/取消/保存跨域共享（TagTableEdit 会话统一管理组+标签双工作集），radio 只切"增删移"操作域。

        /// <summary>可添加行（组级=可加组；子级=可加标签）：地址表编辑会话内</summary>
        public bool CanAddRow => SelectedDevice != null && Session == PlcDebugSession.TagTableEdit
            && (IsGroupScope || !string.IsNullOrEmpty(SelectedGroupName));

        /// <summary>可删除行（组级=选中组行；子级=选中标签行）：地址表编辑会话内且各自域有选中</summary>
        public bool CanRemoveRow => Session == PlcDebugSession.TagTableEdit
            && (IsGroupScope ? SelectedGroupRow != null : SelectedRow != null);

        /// <summary>可上移/下移（组级=调整组顺序；子级=调整标签顺序）：地址表编辑会话内且各自域有选中</summary>
        public bool CanMoveRow => Session == PlcDebugSession.TagTableEdit
            && (IsGroupScope ? SelectedGroupRow != null : SelectedRow != null);

        /// <summary>可读取/写入：仅子级域且选中非草稿行（IsNew/IsDirty=未保存标签驱动不认，语义禁——决策 A）；读写为调试操作不随编辑会话门禁</summary>
        public bool CanReadWrite => !IsGroupScope && SelectedRow != null && !SelectedRow.IsNew && !SelectedRow.IsDirty;

        /// <summary>可设当前标签刷新频率：仅子级域且与读写同条件（草稿行禁）</summary>
        public bool CanSetCurrent => !IsGroupScope && SelectedRow != null && !SelectedRow.IsNew && !SelectedRow.IsDirty && Session == PlcDebugSession.TagTableEdit;

        /// <summary>可设全组刷新频率：仅子级域（组级无频率概念；设频率=批量改标签字段属编辑操作）</summary>
        public bool CanSetAll => !IsGroupScope && SelectedDevice != null && !string.IsNullOrEmpty(SelectedGroupName) && Session == PlcDebugSession.TagTableEdit;

        // ---- 设备管理（连接配置域）按钮使能：新增/编辑/重建 = 浏览态（None）可（会话入口 + 调试操作）；
        //      保存/取消 = 配置会话内（ConfigEdit）可；删除 = 配置会话内且编辑子态（非新增草稿）可（2026-09-10 收紧）；
        //      连接/断开不随门禁（调试操作）----

        /// <summary>可新增 PLC：不在任何编辑会话（浏览态）——新增=进入配置会话的入口之一，禁则列表空时无法创建设备</summary>
        public bool CanAddPlc => Session == PlcDebugSession.None;

        /// <summary>可编辑连接配置：有选中实例且不在任何编辑会话</summary>
        public bool CanEditPlc => SelectedDevice != null && Session == PlcDebugSession.None;

        /// <summary>可保存配置：连接配置会话内（编辑/新增子态均可；无会话禁）</summary>
        public bool CanSavePlc => Session == PlcDebugSession.ConfigEdit && !ConfigBusy;

        /// <summary>可取消配置会话：连接配置会话内</summary>
        public bool CanCancelPlc => Session == PlcDebugSession.ConfigEdit;

        /// <summary>
        /// 可删除 PLC：连接配置编辑会话内、编辑已有实例子态（非新增草稿）、非编排执行中。
        /// 门禁归属编辑态（2026-09-10 收紧，原为浏览态可删）：删除 = "从配置移除已存在设备"的破坏性操作，浏览态禁；
        /// 新增子态（IsPlcAdding）禁——草稿未落盘无实体可删，丢弃草稿走「取消」；
        /// 编辑子态删除对象 = 正在编辑的实例（EditPlc 不切列表选中，SelectedDevice 即被编辑设备）。
        /// </summary>
        public bool CanDeletePlc => SelectedDevice != null && Session == PlcDebugSession.ConfigEdit && !IsPlcAdding && !ConfigBusy;

        /// <summary>可重建实例：有选中实例、不在任何编辑会话且不在编排执行中（重建=现取缓存配置手动重置实例，不落盘不改配置）</summary>
        public bool CanRebuild => SelectedDevice != null && Session == PlcDebugSession.None && !ConfigBusy;

        /// <summary>未保存修改总数（标签脏/新增/删除清单 + 组脏/新增/删除清单），保存按钮计数提示</summary>
        public int PendingCount => TagRows.Count(r => r.IsNew || r.IsDirty) + _removedTagNames.Count
            + GroupRows.Count(g => g.IsNew || g.IsDirty) + _removedGroupNames.Count;

        /// <summary>标签工作集未保存数（切组拦截专用——组工作集跨组不随切组丢，仅标签随当前组重建）</summary>
        public int PendingTagCount => TagRows.Count(r => r.IsNew || r.IsDirty) + _removedTagNames.Count;

        /// <summary>是否有未保存修改（切实例/导入/退出拦截依据——含组工作集）</summary>
        public bool HasPendingChanges => PendingCount > 0;

        /// <summary>标签工作集是否有未保存修改（切组拦截依据；组编辑跨组保留不拦）</summary>
        public bool HasPendingTagChanges => PendingTagCount > 0;

        /// <summary>保存按钮文案（有未保存修改时带计数，如"保存(3)"）</summary>
        public string SaveButtonText => PendingCount > 0 ? $"保存({PendingCount})" : "保存";

        #endregion

        #region 工作集事务状态

        /// <summary>已删除存量标签行的组表键（保存统一 RemoveTag；切组/放弃清空 = 自动撤销删除）</summary>
        private readonly List<string> _removedTagNames = new List<string>();

        /// <summary>已删除存量组的 CD 键（保存统一 RemoveTagGroup；切实例/放弃清空 = 自动撤销删除）</summary>
        private readonly List<string> _removedGroupNames = new List<string>();

        /// <summary>当前正在编辑的行（1s 快照豁免：ResetItem 会经 CurrencyManager 干扰未提交单元格）</summary>
        private TagItemRow _editingRow;

        /// <summary>当前正在编辑的组行（预留：组 DGV 暂无展示列节拍无消费者——将来组级动态列节拍豁免用，勿删，详见 TagTable.cs NotifyGroupRowEditing）</summary>
        private TagGroupRow _editingGroupRow;

        #endregion

        #region 构造

        public PlcDebugViewModel(
            IDeviceOrchestration orchestration,
            IPlcConfigManager configManager,
            IAuthService authService,
            IMessageBoxService messageBox,
            IUserSession userSession,
            IAuditService auditService)
        {
            this.orchestration = orchestration ?? throw new ArgumentNullException(nameof(orchestration));
            this.configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            this.messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
        }

        #endregion

        // 权限门禁说明：各命令体首行**内联** if (!authService.HasPermission(码)) { 弹窗; return; }
        // （全项目同一写法，见 BaseSettingsVM / DevSettingsVM / RoleManagerViewModel）。
        // 判据刻意落在**命令层而非 CanExecute**（与 ProcessMonitorViewModel 同惯例）：CanXxx 只表达业务可用性
        // （有无选中 / 会话态 / 连线态），不掺权限——权限档位调整无需逐条重算全部 CanXxx，
        // 也不会让用户对"按钮为何变灰"无从判断。
        // 每个命令一个独立权限码（见 PermissionCodes.BtnPLCDebug*）：原先按动作域归并的 6 码已拆开，
        // 归并会把权限粒度绑死在实现分组上，现场没法给"能改配置但不许删设备"这类分工留口子。
        // ⚠️ DGV 编辑管线回调（CellValueChanged→NotifyRowEdited / RenameTag / RenameGroup / OnTagIdEdited）
        // **不设权限**：它们只在 Session==TagTableEdit 内可达，而进会话必经「进入地址表编辑」命令（已设门禁）——
        // 卡入口 = 结构保证；在回调里撒权限 = 纪律保证（且是噪音）。
        // 门禁被拒**不留痕**：结果维度只有成功 / 失败两态，被拒不是"一次没成功的操作"而是门禁裁决，
        // 记进结果列会让这一列同时承担两件事（现场约定：对齐威纶通等 HMI 操作记录，只记成功 / 失败）。
        #region 审计留痕

        /// <summary>
        /// 审计目标名兜底：设备类动作传设备名、文件类动作传纯文件名
        /// （全路径会被 Target 列的 100 字上限截断成半截脏数据——AuditService.Truncate）。
        /// 投递点统一内联 <c>auditService.Record(new AuditEntry(AuditCategory.Device, ...))</c>
        /// （2026-09-20：删除 private Audit / AuditDevice 转发样板，挂点直写完整语义）。
        /// </summary>
        private static string AuditTargetName(string text)
        {
            return string.IsNullOrWhiteSpace(text) ? "(未命名)" : text;
        }

        #endregion

        #region 选中变化钩子（partial 方法，生成器自动调用）

        partial void OnSelectedDeviceChanged(DeviceRow value)
        {
            // 切实例（View 已先 配置脏/地址表脏 拦截放行）= 离开当前设备编辑上下文，两会话强制结束回浏览态
            // 连接配置会话：草稿与快照一并弃（进配置会话不跨实例，切实例即放弃——View 拦截已确认）
            IsPlcAdding = false;
            ConfigDraft = null;
            configSnapshot = null;
            // 地址表会话（v3 惯例）——两会话收敛为 Session=None 单点退出（枚举化）
            Session = PlcDebugSession.None;
            // 清工作集顺序（防二次弹窗）：先清删除清单/编辑豁免 → 清当前组选中（SelectedGroupRow=null →
            // 钩子 SelectedGroupName=null → RebuildTagRows 清标签工作集）→ 最后清组列表
            // （GroupRows.Clear 触发组 DGV SelectionChanged 时 SelectedGroupRow 已 null → 处理器短路，无脏确认弹窗）
            _removedTagNames.Clear();
            _removedGroupNames.Clear();
            _editingRow = null;
            _editingGroupRow = null;
            SelectedGroupRow = null;
            GroupRows.Clear();
            SelectedGroupName = null;
            ConfigRows = new List<ConfigFlatRow>();

            if (value != null)
            {
                LoadPlcConfigRows(value.Name);
                LoadGroupRows(value.Name);
            }
            RefreshPlcQueueDepth(); // 切设备即时现取队列深度（无选中内部置 0），防 1s 节拍滞后误显上一台的值
        }

        /// <summary>
        /// 组 DGV 选中行变化：派生当前组名（真实组表键 = SourceGroupName——编辑态改名未提交不改当前组），
        /// 驱动标签工作集重建（组 DGV 行选中即选组，替代已删的 cmbGroupName 下拉）。
        /// 新增组行（IsNew，SourceGroupName=null）不可作当前组（无真实组表键）。
        /// </summary>
        partial void OnSelectedGroupRowChanged(TagGroupRow value)
        {
            SelectedGroupName = value != null && !value.IsNew
                ? value.SourceGroupName
                : null;
        }

        partial void OnSelectedGroupNameChanged(string value)
        {
            SelectedRow = null;
            RebuildTagRows(); // 切组 = 丢弃工作集重新快照（未保存确认已在 View 选择拦截/Import 内处理）
        }

        #endregion

        #region 状态派生与连接节拍（View 1s Timer 调：连接按钮态 + 设备行状态）

        /// <summary>
        /// 连接状态节拍：IsConnected 为驱动后台真值、无事件源（PlcDriver 定稿不感知 UI），
        /// 只能由 View 1s 节拍轮询感知。通知收窄为显式两属性（2026-09-10：原 OnPropertyChanged(string.Empty)
        /// 空串=21 个绑定全量重读，而依赖连接真值的仅 CanConnect/CanDisconnect，其余全由本地 setter [NotifyPropertyChangedFor] 转发）。
        /// ⚠️ 将来新增"依赖连接真值"的绑定属性时，须在此补 OnPropertyChanged——勿回退空串全量通知。
        /// </summary>
        public void RefreshConnectionButtons()
        {
            OnPropertyChanged(nameof(CanConnect));
            OnPropertyChanged(nameof(CanDisconnect));
        }

        /// <summary>
        /// 现取选中实例的连接状态（每次从编排口取最新实例，防重建/移除后旧引用失效）
        /// </summary>
        private bool IsSelectedConnected()
        {
            if (SelectedDevice == null) return false;
            var dev = orchestration.GetDevice(SelectedDevice.Name);
            return dev != null && dev.IsConnected;
        }

        /// <summary>
        /// 设备列表连接状态节拍（View pollTimer 1s 调）：逐行现取编排口最新连接状态，
        /// 状态翻转才写行 + ResetItem 行级刷新——替代原 dgvPLCList.Invalidate 全表强刷 + CellFormatting 画时现取。
        /// 现取防重建/移除后旧 DeviceRow 引用失效；连接状态列文本经 ConnectionText 绑定，颜色由 View 按行状态上色。
        /// </summary>
        public void RefreshDeviceRows()
        {
            for (int i = 0; i < Devices.Count; i++)
            {
                var row = Devices[i];
                var dev = orchestration.GetDevice(row.Name);
                bool connected = dev != null && dev.IsConnected;
                if (connected == row.IsConnected) continue;
                row.IsConnected = connected;
                Devices.ResetItem(i); // 行级刷新（DGV 重读 ConnectionText；CellFormatting 同步上色）
            }
        }

        #endregion

        #region 实例列表

        /// <summary>
        /// 加载 PLC 实例列表（页面 OnLoad 调一次；实例生命周期收敛后无运行时增删，无需轮询重建）。
        /// 重绑前置 PlcSelectionLocked：ResetBindings 触发设备 DGV 自动落首行，该行非 VM 目标行
        /// （如删除后要选邻行）→ 拦住这轮 SelectionChanged 写 SelectedDevice，由调用方重载后显式赋值收敛。
        /// </summary>
        public void LoadDevices()
        {
            PlcSelectionLocked = true; // 程序化重载：重绑后 DGV 自动落首行非用户点选意图（须在 ResetBindings 之前置位）
            Devices.RaiseListChangedEvents = false;
            try
            {
                Devices.Clear();
                var plcs = orchestration.GetDevicesByType(DeviceTypeNames.Plc);
                foreach (var dev in plcs)
                    Devices.Add(new DeviceRow(dev));
            }
            finally
            {
                Devices.RaiseListChangedEvents = true;
                Devices.ResetBindings();
            }
        }

        #endregion

        #region 现取三块

        /// <summary>
        /// 现取选中实例的 PlcConfig → Flatten 扁平行（供 View 渲染只读配置信息区）
        /// </summary>
        private void LoadPlcConfigRows(string plcName)
        {
            try
            {
                var cfg = configManager.GetPlcConfig(plcName);
                ConfigRows = cfg != null
                    ? ConfigFlatRow.Flatten(cfg)
                    : new List<ConfigFlatRow>();
            }
            catch (Exception ex)
            {
                ConfigRows = new List<ConfigFlatRow>();
                messageBox.ShowError($"加载配置失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 现取选中实例的地址表 → 组工作集（TagGroupRow 快照，按 GroupID 升序、同 ID 组名 Ordinal 兜底——展示稳定）。
        /// 切实例/保存成功/取消/导入后调用（浏览态组 DGV 展示源；编辑态改组名列解锁，工作集复用）。
        /// </summary>
        private void LoadGroupRows(string plcName)
        {
            GroupSelectionLocked = true; // 程序化重建：重绑后 DGV 自动落首行非用户切组意图（须在 ResetBindings 之前置位）
            GroupRows.RaiseListChangedEvents = false;
            try
            {
                GroupRows.Clear();
                var table = configManager.GetTagTable(plcName);
                if (table == null) return;
                var groups = table
                    .OrderBy(kv => kv.Value.GroupID)
                    .ThenBy(kv => kv.Key, StringComparer.Ordinal)
                    .ToList();
                foreach (var kv in groups)
                    GroupRows.Add(TagGroupRow.FromSource(kv.Value, kv.Key));
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"加载地址表失败: {ex.Message}");
            }
            finally
            {
                GroupRows.RaiseListChangedEvents = true;
                GroupRows.ResetBindings();
            }
        }

        /// <summary>
        /// 重建工作集（切组/切实例/取消/保存后）：整组快照——丢弃旧工作集（含删除清单/编辑豁免），
        /// 从组表现取重建（编辑列拷贝 + Source/SourceTagName 锚定 + 展示列初快照），按 TagID 升序。
        /// ⚠️ 本方法不管理 Session（v3 会话态由各路径显式：保存成功/取消/切实例置 None；导入成功保持 TagTableEdit；切组保持当前值）。
        /// </summary>
        private void RebuildTagRows()
        {
            TagRows.RaiseListChangedEvents = false;
            try
            {
                TagRows.Clear();
                _removedTagNames.Clear();
                _editingRow = null;
                if (SelectedDevice == null || string.IsNullOrEmpty(SelectedGroupName)) return;

                var table = configManager.GetTagTable(SelectedDevice.Name);
                if (table == null) return;

                if (table.TryGetValue(SelectedGroupName, out var group) && group?.Tags != null)
                {
                    var items = group.Tags.Values
                        .OrderBy(t => t.TagID)
                        .ThenBy(t => t.TagName, StringComparer.Ordinal)
                        .ToList();
                    foreach (var item in items)
                        TagRows.Add(TagItemRow.FromSource(item));
                }
            }
            finally
            {
                TagRows.RaiseListChangedEvents = true;
                TagRows.ResetBindings();
                RaiseDirtyRelated(); // 工作集重建后脏态归零，刷新保存按钮计数/草稿禁按钮
            }
        }

        #endregion
    }
}
