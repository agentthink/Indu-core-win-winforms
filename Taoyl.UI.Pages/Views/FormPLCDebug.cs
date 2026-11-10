using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.Share.Extensions;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.Models;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// PLC 调试页面（纯运行时调试器，工作集模型 + 编辑会话门禁）
    ///
    /// 布局：两列。左=PLC通讯列表（设备常驻列 + 组名/刷新频率顶栏，导入/导出在顶栏右端）；右=UITabControl 双页签：
    /// 「连接配置」页 = 顶栏 plcControlTableLayoutPanel 设备管理按钮（新增/编辑/保存/取消/删除/连接/断开/重建）+
    /// 内容区 pnlConfigContent1 双态渲染（浏览态=现取缓存只读展示；编辑会话态=草稿可编辑渲染，均含 ProtocolParams
    /// 字典展开，整行自适应显示）；「地址表」页 = 组名▼/刷新频率 + DGV（TagItemRow 工作集直绑）+ 行操作/读写/编辑会话按钮列。
    ///
    /// 会话门禁双域互斥（编辑会话全局唯一，VM Session 枚举化：None/TagTableEdit/ConfigEdit 单值保证）：
    /// - 地址表域（v3）：浏览态信息列只读（读写测试/WriteValue/连接/导出随时可用），点「编辑」进编辑态才可改结构；
    ///   「保存」= 提交+落盘并退编辑态；「取消」= 丢弃工作集退编辑态。列只读切换由 VM Session→TagTableEdit 驱动。
    /// - 连接配置域（设备管理 6 键）：新增/编辑 → 进配置会话（内容区切草稿可编辑渲染，VM Session→ConfigEdit 驱动）；
    ///   保存 = 校验 → 驱动缓存覆盖 → Devices.json 落盘(.bak) → 实例重建即时生效 → 退会话（重建后连接态=断开，需重连）；
    ///   取消 = 弃草稿退会话（盘/缓存/实例零变化）；删除 = 配置会话内且编辑子态才可（浏览态/新增草稿禁，2026-09-10 收紧）；
    ///   重建 = 浏览态可（调试操作）。切实例/进配置会话前先确认对方会话脏丢弃。
    ///
    /// View 职责（照 FrmResourceConfig/FormProcessMonitor 惯例）：注入 VM、DGV 建列与 DPI 补偿、
    /// 控件事件转发到 VM 命令、文件对话框、Timer 1s 轮询刷新（连接状态节拍 + 地址表展示列快照行级通知）。
    /// 事件绑定按域拆 5 组子方法（BindEvents 骨架 → BindListSelectionEvents / BindPlcConfigEvents / BindTagTableTopEvents /
    /// BindRowActionEvents / BindDgvCellEvents），长 lambda 一律提命名处理器（OnXxx，与既有 OnCellFormattingXxx 同风格）。
    /// 业务逻辑全部在 PlcDebugViewModel。
    ///
    /// 关键机制：
    /// - OnLoad WM_SETREDRAW 禁绘屏障：整树递归禁绘至装载完成，首帧一次成页（中间态零渲染）；
    /// - 展示列（值/读写次数/最近时间/结果/信息）绑定 TagItemRow 预格式化文本，VM 快照 + 行级 ResetItem 驱动，CellFormatting 只上色零取数；
    /// - 切实例/切组先确认双域脏（配置脏 ConfirmConfigDiscard + 地址表脏 ConfirmDiscardPending），取消回滚选择（ReferenceEquals/同值短路防重入）；
    /// - 保存/取消先 EndEdit 提交当前编辑格（防丢最后一格）；配置区文本框写回走 DataBindings 失焦验证（点按钮即失焦，先写回后 Click）。
    /// </summary>
    public partial class FormPLCDebug : UIPage
    {
        #region 依赖注入

        /// <summary>
        /// PLC 调试视图模型，由 Autofac 属性注入
        /// </summary>
        public PlcDebugViewModel ViewModel { get; set; }

        #endregion

        #region 定时器与标记

        /// <summary>1s 轮询定时器：连接状态节拍 + 设备列表连接列重绘 + 地址表展示列快照行级通知</summary>
        private readonly Timer pollTimer = new Timer();

        /// <summary>首次加载默认选中第一实例</summary>
        private bool isFirstLoad = true;

        /// <summary>OnLoad 已完成（StartPolling 之后置位）。OnParentChanged 在 Controls.Add 时就会触发，
        /// 那时 Tick 还没绑定——用它挡住构造期/首挂载的空转与误判。</summary>
        private bool initialized;

        /// <summary>TagName 列编辑开始前的旧名（改名即时校验/回滚定位用；键迁移移交保存 ApplyEdits）</summary>
        private string editStartTagName;

        /// <summary>组 DGV GroupName 列编辑开始前的旧名（组改名即时校验/回滚定位用；CD Key 迁移移交保存 ApplyGroupEdits）</summary>
        private string editStartGroupName;

        /// <summary>上一次会话态（Session 变化方向判断：涉及 ConfigEdit 进出才重建配置区；TagTableEdit 进出只切列只读）</summary>
        private PlcDebugSession lastSession = PlcDebugSession.None;

        /// <summary>
        /// 标签 DGV"自动选中"闸门（拍板"切组只展示不自动选中"的可靠落地）：
        /// true = 锁定期——标签工作集整集重绑后的自动落首行（ResetBindings 触发时机与 DataBindingComplete
        /// 清选存在竞争，DGV 内部补格可能晚于清选，形成"切组后下一秒自动选中首行 + radio 跳子级"）一律弹回，
        /// 直到用户主动点击标签行（CellMouseDown）或用键盘（KeyDown）才解锁放行。
        /// 行级操作（删除后邻行自动选中/上下移重选）不经 Reset、且前置必有用户点选解锁，不受影响。
        /// </summary>
        private bool tagSelectLocked = true;

        #endregion

        #region DPI 字体与列引用

        private Font cellFont;
        private Font headerFont;
        private Font controlFont;

        // 实例列表
        private DataGridViewTextBoxColumn colPlcStatus;

        // 地址表信息列（编辑会话门禁列组 tagEditColumns 成员；事件/自查/格式化需要 Index）
        private DataGridViewTextBoxColumn colTagId;
        private DataGridViewTextBoxColumn colTagName;
        private DataGridViewTextBoxColumn colTagAddress;
        private DataGridViewTextBoxColumn colTagLength;
        private DataGridViewTextBoxColumn colTagFreq;
        private DataGridViewTextBoxColumn colTagDesc;
        private DataGridViewComboBoxColumn colTagDataType;
        private DataGridViewTextBoxColumn colTagOpResult;
        private DataGridViewTextBoxColumn colTagOpMsg;

        // 组 DGV 列（组级操作：组序号只读展示/组名/组描述可编辑——编辑会话门禁列组 groupEditColumns 成员）
        private DataGridViewTextBoxColumn colGroupName;
        private DataGridViewTextBoxColumn colGroupDesc;

        /// <summary>地址表信息列集合（序号/名/地址/类型/长度/频率——随编辑会话门禁切换只读；WriteValue 列不入组恒可编辑）</summary>
        private DataGridViewColumn[] tagEditColumns;

        /// <summary>组 DGV 编辑列集合（组名/组描述——随编辑会话门禁切换只读；组序号列恒只读排序展示）</summary>
        private DataGridViewColumn[] groupEditColumns;

        #endregion

        #region 绘制颜色常量（CellFormatting 收口绘制统一取色，调色只改此处）

        private static readonly Color ColorDirtyBack = Color.FromArgb(255, 245, 157); // 未保存修改行底（柔和明黄）
        private static readonly Color ColorNewBack = Color.FromArgb(220, 240, 255);   // 未保存新增行底（淡青）
        private static readonly Color ColorOkFore = Color.FromArgb(0, 150, 0);        // 已连接/结果成功（绿）
        private static readonly Color ColorFailFore = Color.Red;                      // 结果失败/错误信息（红）
        private static readonly Color ColorDisconnectedFore = Color.FromArgb(130, 130, 130); // 断开（灰）

        #endregion

        #region 构造

        public FormPLCDebug()
        {
            InitializeComponent();
            // 双缓冲提前到构造（控件树已建），覆盖首帧显示前的重绘闪烁
            UiDpi.EnableDoubleBuffered(this);
        }

        #endregion

        #region 生命周期
        protected override void OnLoad(EventArgs e)
        {
            ValidateInjection();

            UiDpi.SetRedraw(this, false);
            try
            {
                base.OnLoad(e);

                SuspendLayout();
                try
                {
                    BindEvents();
                    BindButtonStates();
                }
                finally
                {
                    ResumeLayout(false);
                }

                // DPI 补偿（System aware 物理像素布局，样式修正放 .cs 防 Designer 重开冲掉）
                SetupDpiAdjustments();
                // DGV 初始化放 ResumeLayout 后：UiDgvHelper 内列头高 EnableResizing 切换被 Suspend 抑制（DPI 惯例）
                SetupDgvFonts();
                SetupPlcListDgv();
                SetupGroupDgv();
                SetupTagDgv();

                StartPolling();

                // 数据装载 + 首实例选中联动（同步链内完成，无中间帧可画）
                SuspendLayout();
                try
                {
                    ViewModel.LoadDevices();
                    SelectFirstPlcIfAny();
                }
                finally
                {
                    ResumeLayout(true);
                }
            }
            catch
            {
                // 异常路径防御：装载中途抛异常也立即恢复禁绘再重抛（防页面保持禁绘黑屏/卡死）
                UiDpi.SetRedraw(this, true);
                Invalidate(true);
                throw;
            }
        }

        /// <summary>
        /// 首帧显示完成（Form.Shown 每实例仅触发一次，与 OnLoad 配对）：WinForms 保证 OnShown 前
        /// 窗口定位/尺寸消息（WM_SIZE/WM_WINDOWPOSCHANGED）已处理完毕——此处恢复禁绘 + 整页重绘，
        /// 终态一次成帧。缓存复用切回（Remove→Add→Show）不重走 Load/Shown，本恢复不重复执行。
        /// </summary>
        protected override void OnShown(EventArgs e)
        {                                                  
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }

        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            pollTimer?.Stop();
            cellFont?.Dispose();
            headerFont?.Dispose();
            controlFont?.Dispose();
            cellFont = null;
            headerFont = null;
            controlFont = null;
            base.OnFormClosing(e);
        }

        /// <summary>
        /// Ctrl+C / Ctrl+Insert 复制当前单元格文本（所见即所复制）。
        /// 三个网格（设备列表 / 组 / 地址表）均接白名单，规避 FullRowSelect 下原生 Ctrl+C 复制整行；
        /// Ctrl+Shift+C 不拦，保留原生整行复制入口。判定与复制实现见 UiDgvHelper.TryCopyFocusedCell。
        /// </summary>
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (UiDgvHelper.TryCopyFocusedCell(ref msg, keyData, dgvPLCList, dgvGroupList, dgvTagList))
                return true;
            return base.ProcessCmdKey(ref msg, keyData);
        }

        #endregion

        #region DPI 补偿（System aware 物理像素布局：字号/控件尺寸显式放大）

        /// <summary>
        /// 顶栏非 DGV 控件的 DPI 补偿（DGV 字体/列宽走 SetupDgvFonts/AddTextColumn 的 UiDpi.Scale）：
        /// uiTabControl1.ItemSize：页签物理放大（TabPage 排版由 TabControl 运行期按 ItemSize 重算，Designer 的 Location 仅设计时快照）；
        /// ItemHeight 显式放大（UIComboBox 下拉项高，照 FormMain.cmbLanguage 惯例 UiDpi.Scale(25)）。
        /// 样式修正放 .cs 不放 Designer.cs——设计器重开会冲掉手动代码。
        /// </summary>
        private void SetupDpiAdjustments()
        {
            uiTabControl1.ItemSize = new Size(UiDpi.Scale(150), UiDpi.Scale(50));

            // 地址表页组/标签上下分栏：水平 SplitContainer 物理像素补偿（Designer 100% 基准值：SplitterDistance 300/分栏条 5/Panel Min 25）
            TagTableSplitContainer.SplitterDistance = UiDpi.Scale(300);
            TagTableSplitContainer.SplitterWidth = UiDpi.Scale(5);
            TagTableSplitContainer.Panel1MinSize = UiDpi.Scale(25);
            TagTableSplitContainer.Panel2MinSize = UiDpi.Scale(25);

            // 组级/子级 RadioButton：SunnyUI 自绘圆点不走字体缩放（200% 回归机实测圆点不放大），按 Designer 基准显式补偿
            groupItemRadioButton.RadioButtonSize = UiDpi.Scale(20);
            tagItemRadioButton.RadioButtonSize = UiDpi.Scale(20);
        }

        #endregion

        #region DGV 初始化（DPI 三件套 + 列头样式统一走 UiDgvHelper）

        private void SetupDgvFonts()
        {
            // 字体按 FrmResourceConfig 惯例：UIDataGridView 单元格原生绘制物理放大（12F × scale），
            // 列头绘制走 GlobalFontScale 保持 12F（物理放大会双重放大）
            cellFont = new Font("微软雅黑", 12F * UiDpi.DpiScale);
            headerFont = new Font("微软雅黑", 12F, FontStyle.Bold);
            controlFont = new Font("微软雅黑", 12F);
        }

        /// <summary>取行类型属性的 [Description] 作列头（TagItemRow/DeviceRow 反射统一表头来源）</summary>
        private static string HeaderOf(Type rowType, string propName)
        {
            var attr = rowType.GetProperty(propName)?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? propName;
        }

        /// <summary>
        /// 统一文本列（实例列表/地址表两 DGV 共用）：
        /// 全部列带 DataPropertyName（可编辑列直绑 TagItemRow 副本，只读展示列绑预格式化文本）；
        /// 宽度两模式：fillWeight &gt; 0 = Fill 百分比权重（dgvPLCList，AutoSizeColumnsMode.Fill）；
        /// 否则 width = 固定宽 + MinimumWidth（dgvTagList，AutoSizeColumnsMode.None）。
        /// </summary>
        private DataGridViewTextBoxColumn AddTextColumn(DataGridView dgv, string headerText, string dataProperty,
            bool readOnly, bool left, int width = 0, int fillWeight = 0)
        {
            var col = new DataGridViewTextBoxColumn
            {
                HeaderText = headerText,
                DataPropertyName = dataProperty,
                ReadOnly = readOnly,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = left ? DataGridViewContentAlignment.MiddleLeft : DataGridViewContentAlignment.MiddleCenter,
                },
            };
            if (fillWeight > 0)
                col.FillWeight = fillWeight;
            else
            {
                col.Width = UiDpi.Scale(width);
                col.MinimumWidth = UiDpi.Scale(30);
            }
            dgv.Columns.Add(col);
            return col;
        }

        #endregion

        #region 实例列表 DGV

        private void SetupPlcListDgv()
        {
            UiDgvHelper.Setup(dgvPLCList, cellFont, headerFont, controlFont, showRowNumbers: false);
            dgvPLCList.AutoGenerateColumns = false;
            dgvPLCList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvPLCList.ReadOnly = true;
            dgvPLCList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPLCList.MultiSelect = false;
            dgvPLCList.BackgroundColor = Color.White;
            dgvPLCList.Columns.Clear();

            AddTextColumn(dgvPLCList,
                HeaderOf(typeof(DeviceRow), nameof(DeviceRow.Name)), nameof(DeviceRow.Name),
                readOnly: true, left: true, fillWeight: 60);
            // 连接状态列：绑 ConnectionText（VM RefreshDeviceRows 节拍写 IsConnected → ResetItem 行级刷新），
            // 颜色由 OnCellFormattingPlc 按行状态上色——CellFormatting 不再现取 IDevice（与地址表行状态同模式）
            colPlcStatus = AddTextColumn(dgvPLCList,
                HeaderOf(typeof(DeviceRow), nameof(DeviceRow.ConnectionText)), nameof(DeviceRow.ConnectionText),
                readOnly: true, left: false, fillWeight: 40);

            dgvPLCList.DataSource = ViewModel.Devices;
        }

        #endregion

        #region 组 DGV（组工作集：组序号只读展示 + 组名/组描述编辑——随 TagTableEdit 会话解锁）

        private void SetupGroupDgv()
        {
            UiDgvHelper.Setup(dgvGroupList, cellFont, headerFont, controlFont, showRowNumbers: false);
            dgvGroupList.AutoGenerateColumns = false;
            dgvGroupList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvGroupList.ScrollBars = ScrollBars.Both;
            dgvGroupList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGroupList.MultiSelect = false;
            dgvGroupList.AllowUserToAddRows = false;
            dgvGroupList.AllowUserToDeleteRows = false;
            dgvGroupList.AllowUserToResizeRows = false;
            dgvGroupList.BackgroundColor = Color.White;
            dgvGroupList.Columns.Clear();

            // 组序号（只读排序展示——组间调序走上/下移按钮，手编序号不开放）；组名/组描述（编辑会话门禁）
            AddTextColumn(dgvGroupList, HeaderOf(typeof(TagGroupRow), nameof(TagGroupRow.GroupID)), nameof(TagGroupRow.GroupID), readOnly: true, left: false, width: 70);
            colGroupName = AddTextColumn(dgvGroupList, HeaderOf(typeof(TagGroupRow), nameof(TagGroupRow.GroupName)), nameof(TagGroupRow.GroupName), readOnly: false, left: true, width: 180);
            colGroupDesc = AddTextColumn(dgvGroupList, HeaderOf(typeof(TagGroupRow), nameof(TagGroupRow.GroupDesc)), nameof(TagGroupRow.GroupDesc), readOnly: false, left: true, width: 260);
            AddTextColumn(dgvGroupList, HeaderOf(typeof(TagGroupRow), nameof(TagGroupRow.ChildCount)), nameof(TagGroupRow.ChildCount), readOnly: true, left: false, width: 70);

            groupEditColumns = new DataGridViewColumn[] { colGroupName, colGroupDesc };

            dgvGroupList.DataSource = ViewModel.GroupRows;

            // 初始浏览态（Session=None 无变化通知，显式初始化列只读）
            SetColumnsEditMode(groupEditColumns, editing: false);
        }

        /// <summary>
        /// 编辑会话列级只读切换统一入口（组 DGV 编辑列 / 标签 DGV 信息列共用——原 SetGroupEditMode/SetTagEditMode 逐字同构合并）：
        /// ReadOnly = !editing；WriteValue 列不在 tagEditColumns（恒可编辑不随门禁）。cols=null 防御（Setup*Dgv 完成前不可达，理论不触发）。
        /// </summary>
        private static void SetColumnsEditMode(DataGridViewColumn[] cols, bool editing)
        {
            if (cols == null) return;
            foreach (var col in cols)
                col.ReadOnly = !editing;
        }

        #endregion

        #region 地址表 DGV（工作集 12 列：可编辑列直绑 TagItemRow 副本；展示列绑定预格式化文本；行状态色 CellFormatting 收口）

        private void SetupTagDgv()
        {
            UiDgvHelper.Setup(dgvTagList, cellFont, headerFont, controlFont, showRowNumbers: false);
            dgvTagList.AutoGenerateColumns = false;
            dgvTagList.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvTagList.ScrollBars = ScrollBars.Both;
            dgvTagList.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTagList.MultiSelect = false;
            dgvTagList.AllowUserToAddRows = false;
            dgvTagList.AllowUserToDeleteRows = false;
            dgvTagList.AllowUserToResizeRows = false;
            dgvTagList.BackgroundColor = Color.White;
            dgvTagList.Columns.Clear();

            // 可编辑绑定列（写回 TagItemRow 副本，CellValueChanged 置脏；保存才提交组表——编辑事务隔离）
            // 信息列（序号/名/地址/长度/频率/类型）入 tagEditColumns 会话门禁组；WriteValue 为调试写输入缓冲，恒可编辑不入组
            colTagId = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.TagID)), nameof(TagItemRow.TagID), readOnly: false, left: false, width: 60);
            colTagName = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.TagName)), nameof(TagItemRow.TagName), readOnly: false, left: true, width: 140);
            colTagDesc = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.TagDesc)), nameof(TagItemRow.TagDesc), readOnly: false, left: true, width: 150);
            colTagAddress = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.Address)), nameof(TagItemRow.Address), readOnly: false, left: true, width: 150);

            
            var dataType = Enum.GetValues(typeof(PlcDataType))
                .Cast<PlcDataType>()
                .Select(t => new { Display = t.GetDescription(), Value = t })
                .ToList();

            
            colTagDataType = new DataGridViewComboBoxColumn
            {
                HeaderText = HeaderOf(typeof(TagItemRow), nameof(TagItemRow.DataType)),
                DataPropertyName = nameof(TagItemRow.DataType),
                Width = UiDpi.Scale(120),            // 容纳短名（最长 Double[] 在 96dpi 约 65px）+ 显示态下拉箭头
                MinimumWidth = UiDpi.Scale(60),
                DropDownWidth = UiDpi.Scale(120),    // 与列宽同宽（弹层宽 = max(列宽, 本值)）
                MaxDropDownItems = 12,               // 可见项数 8 → 12，21 项由滚三屏降到两屏
                FlatStyle = FlatStyle.Flat,
            };
            colTagDataType.DataSource = dataType;
            colTagDataType.DisplayMember = "Display";
            colTagDataType.ValueMember = "Value";
            dgvTagList.Columns.Add(colTagDataType);

            colTagLength = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.Length)), nameof(TagItemRow.Length), readOnly: false, left: false, width: 60);
            colTagFreq = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.RefreshFreq)), nameof(TagItemRow.RefreshFreq), readOnly: false, left: false, width: 90);

            // 展示列（只读绑定预格式化文本：VM 1s 节拍 RefreshTagRowValues 快照写，仅变化行 ResetItem 行级通知）
            AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.ValueText)), nameof(TagItemRow.ValueText), readOnly: true, left: false, width: 120);

            // WriteValue = 点动写输入缓冲：不入 tagEditColumns（恒可编辑，浏览态也可填/写，不随会话门禁）
            AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.WriteValue)), nameof(TagItemRow.WriteValue), readOnly: false, left: true, width: 130);

            

            tagEditColumns = new DataGridViewColumn[]
            {
                colTagId, colTagName, colTagAddress, colTagDataType, colTagLength, colTagFreq, colTagDesc,
            };

            
            AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.RWTimesText)), nameof(TagItemRow.RWTimesText), readOnly: true, left: false, width: 80);
            AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.OpTimeText)), nameof(TagItemRow.OpTimeText), readOnly: true, left: false, width: 130);
            colTagOpResult = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.OpResultText)), nameof(TagItemRow.OpResultText), readOnly: true, left: false, width: 80);
            colTagOpMsg = AddTextColumn(dgvTagList, HeaderOf(typeof(TagItemRow), nameof(TagItemRow.OpMsgText)), nameof(TagItemRow.OpMsgText), readOnly: true, left: true, width: 200);

            dgvTagList.DataSource = ViewModel.TagRows;

            // 初始浏览态（IsEditing 默认 false 无变化通知，显式初始化列只读）
            SetColumnsEditMode(tagEditColumns, editing: false);
        }

        #endregion

        #region 事件绑定

        /// <summary>
        /// 事件绑定总入口（按域拆 5 组，见各子方法）：列表选中联动 / 连接配置键 / 地址表顶栏 / 行操作与读写 / DGV 单元格。
        /// 长 lambda（三处 DGV 选中 + 导入导出对话框）提为命名处理器（见本 region 末尾），与既有 OnCellFormattingXxx 同风格。
        /// </summary>
        private void BindEvents()
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            BindListSelectionEvents();  // ① 三 DGV 选中 + radio 互斥 + 用户解锁闸门
            BindPlcConfigEvents();      // ② 连接配置页：设备管理 8 键 + 导入/导出
            BindTagTableTopEvents();    // ③ 地址表顶栏：刷新频率双钮
            BindRowActionEvents();      // ④ 行操作 + 读写 + 编辑会话三键
            BindDgvCellEvents();        // ⑤ 单元格格式化 + 标签/组编辑管线
        }

        /// <summary>① 列表选中与操作域联动：三 DGV SelectionChanged（含重绑闸门/脏确认拦截）+ radio 显式互斥 + 用户解锁闸门</summary>
        private void BindListSelectionEvents()
        {
            // ---- 列表选中 → VM ----
            dgvPLCList.SelectionChanged += OnPlcListSelectionChanged;
            dgvTagList.SelectionChanged += OnTagListSelectionChanged;

            // ---- 设备 DGV 用户主动选择解锁（闸门配套）：鼠标左键点数据行 / 键盘任意键 = 显式点选，解除重载锁定 ----
            dgvPLCList.CellMouseDown += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.Button == MouseButtons.Left)
                    ViewModel.UnlockPlcSelection();
            };
            dgvPLCList.KeyDown += (s, e) => ViewModel.UnlockPlcSelection();

            // ---- 标签 DGV 用户主动选择解锁（闸门配套）：鼠标左键点数据行 / 键盘任意键 = 显式操作，解除重绑锁定 ----
            dgvTagList.CellMouseDown += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.Button == MouseButtons.Left)
                    tagSelectLocked = false;
            };
            dgvTagList.KeyDown += (s, e) => tagSelectLocked = false;

            // ---- 组 DGV 用户主动选择解锁（闸门配套，对称标签侧）：闸门由 VM 在 LoadGroupRows 起始置位（重绑前），
            //      此处鼠标左键点组行 / 键盘操作 = 显式切组意图 → 解锁 ----
            dgvGroupList.CellMouseDown += (s, e) =>
            {
                if (e.RowIndex >= 0 && e.Button == MouseButtons.Left)
                    ViewModel.UnlockGroupSelection();
            };
            dgvGroupList.KeyDown += (s, e) => ViewModel.UnlockGroupSelection();

            // ---- 组 DGV 选中（选组=组 DGV 行；替代已删的 cmbGroupName 下拉）----
            dgvGroupList.SelectionChanged += OnGroupListSelectionChanged;

            // ---- 操作域 radio（组级/子级双向）：radio → VM.IsGroupScope；区域选中 → radio 已在各 DGV SelectionChanged 处理 ----
            // 显式互斥（防 SunnyUI UIRadioButton 容器互斥失效）：checked=true 时清另一并写本域值（清另一触发其 CheckedChanged → checked=false 短路不递归）
            groupItemRadioButton.CheckedChanged += (s, e) => OnScopeRadioChecked(groupItemRadioButton, tagItemRadioButton, groupScope: true);
            tagItemRadioButton.CheckedChanged += (s, e) => OnScopeRadioChecked(tagItemRadioButton, groupItemRadioButton, groupScope: false);
            tagItemRadioButton.Checked = true; // 初始默认子级操作域（浏览态读写为主）；触发 IsGroupScope=false
        }

        /// <summary>② 「连接配置」页顶栏：设备管理 8 键（连接/断开/增/编/存/取/删/重建）+ 导入/导出对话框</summary>
        private void BindPlcConfigEvents()
        {
            // ---- 设备管理 6 键 + 连接/断开（对话框/确认在 VM）----
            // 新增/编辑 = 进配置会话（VM 先收尾地址表会话——会话互斥）；保存/取消 = 会话提交/放弃（草稿事务在 VM）；
            // 删除 = 编辑会话内编辑子态才可（浏览态/新增草稿禁——破坏性操作，2026-09-10 收紧）；重建 = 浏览态可（调试操作）；
            // 删除/重建 = 确认后异步编排（Task.Run：实例释放/重建可能阻塞至旧实例 IO 超时，禁 UI 线程直调）；
            // 连接/断开 = 调试操作不随会话门禁（保存/重建后实例已换新，连接按钮显式执行——补丁二决策）
            btnPlcConn.Click += async (s, e) => await ViewModel.ConnectCommand.ExecuteAsync(null);
            btnPlcDisConn.Click += async (s, e) => await ViewModel.DisconnectCommand.ExecuteAsync(null);
            btnPlcAdd.Click += (s, e) => ViewModel.AddPlcCommand.Execute(null);
            btnPlcEdit.Click += (s, e) => ViewModel.EditPlcCommand.Execute(null);
            btnPlcSave.Click += async (s, e) => await ViewModel.SavePlcCommand.ExecuteAsync(null);
            btnPlcCancel.Click += (s, e) => ViewModel.CancelPlcCommand.Execute(null);
            btnPlcDel.Click += async (s, e) => await ViewModel.DeletePlcCommand.ExecuteAsync(null);
            btnPlcRebuild.Click += async (s, e) => await ViewModel.RebuildPlcCommand.ExecuteAsync(null);
            btnPlcLoadIn.Click += OnPlcLoadInClick;
            btnPlcExport.Click += OnPlcExportClick;
        }

        /// <summary>③ 地址表顶栏：刷新频率双钮（文本解析与语义在 VM）</summary>
        private void BindTagTableTopEvents()
        {
            btnSetCurrent.Click += (s, e) => ViewModel.SetCurrentFreqCommand.Execute(txtRefreshFreq.Text);
            btnSetAll.Click += (s, e) => ViewModel.SetAllFreqCommand.Execute(txtRefreshFreq.Text);
        }

        /// <summary>④ 地址表行操作 + 读写 + 编辑会话三键（编辑/取消/保存）</summary>
        private void BindRowActionEvents()
        {
            btnTagAdd.Click += (s, e) => ViewModel.AddRowCommand.Execute(null);
            btnTagDel.Click += (s, e) => ViewModel.RemoveRowCommand.Execute(null);
            btnTagUpper.Click += (s, e) => ViewModel.MoveUpCommand.Execute(null);
            btnTagDown.Click += (s, e) => ViewModel.MoveDownCommand.Execute(null);
            btnTagRead.Click += async (s, e) => await ViewModel.ReadCommand.ExecuteAsync(null);
            btnTagWrite.Click += async (s, e) => await ViewModel.WriteCommand.ExecuteAsync(null);
            // 编辑会话三键语义（参照 FormParamSetting）：
            // 编辑=浏览态进入编辑会话（浏览态恒无脏，VM 直接置 Session=TagTableEdit → 列解锁）；
            // 取消=丢弃工作集退浏览态（有脏 VM 先确认；⚠️ 不回滚导入的内存覆盖）；
            // 保存=提交+实例级落盘退编辑态（导入覆盖内存后的固化也走此钮）。
            // 三键点击均先 EndEdit 提交当前编辑格（半编辑格 CellValueChanged 未触发，不提交会丢/误判）
            btnTagEdit.Click += (s, e) =>
            {
                dgvTagList.EndEdit();
                ViewModel.EditTagCommand.Execute(null);
            };
            btnTagCancel.Click += (s, e) =>
            {
                dgvTagList.EndEdit();
                ViewModel.CancelTagEditCommand.Execute(null);
            };
            btnTagSave.Click += (s, e) =>
            {
                dgvTagList.EndEdit();
                ViewModel.SaveTagConfigCommand.Execute(null);
            };
        }

        /// <summary>⑤ DGV 单元格：格式化上色 ×3 + 标签/组双编辑管线（值变更/进入/结束/重绑清选）</summary>
        private void BindDgvCellEvents()
        {
            // ---- 单元格格式化：设备列表连接列上色（文本已绑 ConnectionText）；地址表行状态上色（展示列已绑定）----
            dgvPLCList.CellFormatting += OnCellFormattingPlc;
            dgvTagList.CellFormatting += OnCellFormattingTag;
            dgvGroupList.CellFormatting += OnCellFormattingGroup;

            // ---- 编辑提交：属性列置脏 + 快照豁免登记；序号/TagName 提交后延迟处理（编辑管线内不可重绑，17:30 教训）----
            dgvTagList.CellValueChanged += OnTagCellValueChanged;
            dgvTagList.CellBeginEdit += OnTagCellBeginEdit;
            dgvTagList.CellEndEdit += OnTagCellEndEdit;

            // ---- 标签工作集重绑后清选（"选中=操作目标"须显式；切组/保存/取消/切实例重快照后只展示不自动选中，
            //      防"自动落首行 + 随手读写"误触现场标签——2026-09-09 拍板）----
            dgvTagList.DataBindingComplete += OnTagListDataBindingComplete;

            // ---- 组 DGV 编辑：组名/组描述 列置脏（组序号列只读不触发）；GroupName 编辑结束 → 组改名即时校验 ----
            dgvGroupList.CellValueChanged += OnGroupCellValueChanged;
            dgvGroupList.CellBeginEdit += OnGroupCellBeginEdit;
            dgvGroupList.CellEndEdit += OnGroupCellEndEdit;
        }

        // ---- 长 lambda 提命名（供上方绑定方法引用；与既有 OnCellFormattingXxx 等命名处理器同风格）----

        /// <summary>
        /// 设备列表选中 → VM：先确认双域未保存修改——配置会话脏（ConfirmConfigDiscard，有配置会话及脏先确认放弃）+
        /// 地址表脏（ConfirmDiscardPending：保存并继续/放弃/留下）——任一取消则回滚 DGV 选中；
        /// 放行后由 VM OnSelectedDeviceChanged 钩子统一退两会话清草稿。
        /// 回滚/同值事件经 ReferenceEquals 短路，防弹窗风暴与重入（含 DGV 切行的 null 中间轮：首轮拦截一次弹窗，放行后第二轮无脏直接过）。
        /// </summary>
        private void OnPlcListSelectionChanged(object sender, EventArgs e)
        {
            // 重绑期（PlcSelectionLocked）：程序化重载的 DGV 自动落首行非用户点选意图 →
            // 不写 SelectedDevice，由调用方（ReloadAndSelectDevice/ReloadAndSelectAdjacent）重载后显式赋值唯一收敛。
            // 不拦的后果：中间轮把 Devices[0] 写进 SelectedDevice → 配置区先重绘错误行再被目标行覆盖（残留 + 闪烁）
            // ⚠️ 闸门判断置于最前且此处**绝不回写 DGV 选中**（2026-09-14 实测）：本回调由 DGV 自身 SelectionChanged 触发，
            // 此刻往往正在 ResetBindings 的 ClearInternal 窗口内（栈：ResetBindings → OnClearingRows → ClearSelection
            // → OnSelectionChanged），回调内再触碰 SelectedRows/Rows 会因 Rows.Count 与 CurrencyManager.Count
            // 瞬态不一致而抛 IndexOutOfRangeException。重载期 DGV 选中无所谓——调用方重载收尾会 SyncPlcDgvSelection 校正。
            if (ViewModel.PlcSelectionLocked) return;

            DeviceRow target = dgvPLCList.SelectedRows.Count > 0
                ? RowAt<DeviceRow>(dgvPLCList, dgvPLCList.SelectedRows[0].Index)
                : null;
            if (ReferenceEquals(target, ViewModel.SelectedDevice)) return; // 回滚或同值事件，短路

            if (!ViewModel.ConfirmConfigDiscard() || !ViewModel.ConfirmDiscardPending())
            {
                RestorePlcSelection(ViewModel.SelectedDevice); // 留在原实例
                return;
            }
            ViewModel.SelectedDevice = target;
        }

        /// <summary>
        /// 标签 DGV 选中 → VM：闸门（tagSelectLocked）整集重绑（切组/取消/保存/切实例/导入）后的 DGV 内部自动补格
        /// （自动落首行）一律弹回，不写 SelectedRow/不跳 radio——"选中=操作目标须显式"（2026-09-09 拍板）；
        /// 用户主动点选/键盘已解锁放行。
        /// </summary>
        private void OnTagListSelectionChanged(object sender, EventArgs e)
        {
            if (tagSelectLocked && dgvTagList.SelectedRows.Count > 0)
            {
                dgvTagList.ClearSelection();
                dgvTagList.CurrentCell = null;
                return; // 弹回引发下一次 SelectionChanged(空) → 走下方置 null 分支（VM SelectedRow 归 null 一致性）
            }
            ViewModel.SelectedRow = dgvTagList.SelectedRows.Count > 0
                ? RowAt<TagItemRow>(dgvTagList, dgvTagList.SelectedRows[0].Index)
                : null;
            if (dgvTagList.SelectedRows.Count > 0)
                tagItemRadioButton.Checked = true; // 标签 DGV 选中 → 子级操作域（radio 双向联动）
        }

        /// <summary>
        /// 组 DGV 选中 → VM（选组=组 DGV 行，替代已删的 cmbGroupName 下拉）：切组拦截=标签脏专用
        /// （ConfirmDiscardTagScope——组工作集跨组不随切组丢），回滚=把 DGV 拨回当前组行，选中组行 → 组级操作域。
        /// 重绑闸门（VM.GroupSelectionLocked）：程序化重绑（LoadGroupRows→ResetBindings）的 DGV 自动选中跳过确认直接同步，
        /// 防误触发"用户切组"语义（保存/取消路径重绑时标签有脏会误弹"是否先保存"）。
        /// </summary>
        private void OnGroupListSelectionChanged(object sender, EventArgs e)
        {
            TagGroupRow target = dgvGroupList.SelectedRows.Count > 0
                ? RowAt<TagGroupRow>(dgvGroupList, dgvGroupList.SelectedRows[0].Index)
                : null;
            if (ReferenceEquals(target, ViewModel.SelectedGroupRow)) return; // 回滚或同值事件，短路

            // 重绑期：程序化自动选中非用户切组意图 → 跳过脏确认直接同步 VM（保留"切实例自动选首组"）
            if (!ViewModel.GroupSelectionLocked && !ViewModel.ConfirmDiscardTagScope())
            {
                RestoreGroupSelection(ViewModel.SelectedGroupRow); // 留在原组：把组 DGV 拨回当前选中行
                return;
            }
            ViewModel.SelectedGroupRow = target;
            if (target != null)
                groupItemRadioButton.Checked = true; // 组 DGV 选中 → 组级操作域（radio 双向联动）
        }

        /// <summary>导入地址表（覆盖当前内存地址表；固化由保存钮走实例级落盘）</summary>
        private void OnPlcLoadInClick(object sender, EventArgs e)
        {
            if (ViewModel.SelectedDevice == null) return;
            using (var dlg = new OpenFileDialog
            {
                Title = "导入地址表（覆盖当前内存地址表）",
                Filter = "Excel 文件|*.xlsx",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    ViewModel.ImportCommand.Execute(dlg.FileName);
            }
        }

        /// <summary>导出地址表（当前内存地址表 → xlsx）</summary>
        private void OnPlcExportClick(object sender, EventArgs e)
        {
            if (ViewModel.SelectedDevice == null) return;
            using (var dlg = new SaveFileDialog
            {
                Title = "导出地址表",
                Filter = "Excel 文件|*.xlsx",
                FileName = ViewModel.SelectedDevice.Name + ".xlsx",
            })
            {
                if (dlg.ShowDialog() == DialogResult.OK)
                    ViewModel.ExportCommand.Execute(dlg.FileName);
            }
        }

        /// <summary>radio → 操作域：checked=true 时清另一（互斥）并写 VM.IsGroupScope（原两 lambda 对称逻辑合并）</summary>
        private void OnScopeRadioChecked(UIRadioButton self, UIRadioButton other, bool groupScope)
        {
            if (!self.Checked) return;
            if (other.Checked)
                other.Checked = false;
            ViewModel.IsGroupScope = groupScope;
        }

        /// <summary>组行编辑提交：非组序号列改动（组名/组描述）→ VM 置组行脏（组序号列只读不触发本事件）</summary>
        private void OnGroupCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = RowAt<TagGroupRow>(dgvGroupList, e.RowIndex);
            if (row == null) return;
            ViewModel.NotifyGroupRowEdited(row);
            dgvGroupList.Invalidate(); // 行色即时反馈（CellFormatting 读 IsDirty 上色；管线内禁 ResetItem）
        }

        /// <summary>组行进入编辑：豁免登记（预留——将来组级展示列节拍用，见 VM NotifyGroupRowEditing 预留声明）；GroupName 列记录旧名</summary>
        private void OnGroupCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            editStartGroupName = null;
            if (e.RowIndex < 0) return;
            var row = RowAt<TagGroupRow>(dgvGroupList, e.RowIndex);
            if (row == null) return;
            ViewModel.NotifyGroupRowEditing(row);
            if (e.ColumnIndex == colGroupName.Index)
                editStartGroupName = row.GroupName;
        }

        /// <summary>组行编辑结束：解除豁免；GroupName 列改动 → 组改名即时校验（延迟到编辑管线后，防 CurrencyManager 瞬态错位）</summary>
        private void OnGroupCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            ViewModel.NotifyGroupRowEditing(null);
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex != colGroupName.Index) return; // 组描述列直接置脏已够，无键迁移

            var row = RowAt<TagGroupRow>(dgvGroupList, e.RowIndex);
            if (row == null) return;

            string oldName = editStartGroupName;
            editStartGroupName = null;
            if (string.IsNullOrEmpty(oldName)) return;

            string newName = row.GroupName;
            if (string.Equals(oldName, newName, StringComparison.Ordinal)) return;
            BeginInvoke(new Action(() => ViewModel.RenameGroup(row, oldName)));
        }

        /// <summary>
        /// 编辑提交：
        /// 1) WriteValue 列 = 点动写输入缓冲（非地址表配置字段）——编辑不置脏（置脏会禁读写按钮，锁死写入入口）；
        /// 2) 其余属性列改动 → VM 置行脏 + 通知（保存按钮计数/草稿行禁读/写/设频率）。
        /// DataType 容器重建已移交保存 ApplyEdits（编辑期零污染驱动真值表——工作集隔离）。
        /// </summary>
        /// <summary>
        /// 标签工作集整集重绑完成（RebuildTagRows 尾部 ResetBindings → ListChangedType.Reset）→ 统一回到"只展示"基态：
        /// 切组/取消/保存/切实例/导入后标签 DGV 不自动选中（2026-09-09 拍板"选中=操作目标须显式"，
        /// 防 ResetBindings 自动落首行 + 浏览态可读写 → 误触现场标签）。
        /// ⚠️ 必须按 ListChangedType 过滤：DataBindingComplete 不止 Reset 触发——行级刷新（RefreshTagRowValues
        /// 每秒 ResetItem → ItemChanged）与行增删（ItemAdded/ItemDeleted）同样触发本事件。无条件清选会把
        /// 用户选中行/编辑格每秒清掉 → DGV 反复清选重绘 + SelectedRow 归 null → 按钮 Enabled 翻转，
        /// 表现为"标签表一直闪、读/写按钮闪、编辑态操作按钮全闪"（09-09 F5 回归，根因即无条件清选）。
        /// 清选同时上锁 tagSelectLocked：DGV 内部自动补格若晚于本事件（ResetBindings 触发时机竞争，
        /// 表现为"切组下一秒自动落首行 + radio 跳子级"）由 SelectionChanged 闸门弹回；用户点选/键盘解锁。
        /// </summary>
        private void OnTagListDataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (e.ListChangedType != ListChangedType.Reset) return; // 行级 ItemChanged/增删 不清选，手动选中与编辑格不受节拍干扰
            tagSelectLocked = true; // 回到"只展示不自动选中"基态，待用户显式操作解锁
            dgvTagList.ClearSelection();
            dgvTagList.CurrentCell = null;
        }

        /// <summary>任意属性列提交：置脏 + 行色即时反馈（明黄）。</summary>
        private void OnTagCellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = RowAt<TagItemRow>(dgvTagList, e.RowIndex);
            if (row == null) return;

            if (dgvTagList.Columns[e.ColumnIndex].DataPropertyName == nameof(TagItemRow.WriteValue))
                return; // 写缓冲不置脏

            ViewModel.NotifyRowEdited(row);
            // 行色即时反馈（明黄）：CellValueChanged 在编辑提交管线内禁 ResetItem（CurrencyManager 雷区），
            // Invalidate 仅标记重绘无绑定交互，安全——CellFormatting 重绘时读 IsDirty 上色
            dgvTagList.Invalidate();
        }

        /// <summary>任意列进入编辑：登记快照豁免行（1s 快照跳过该行，防 ResetItem 干扰未提交单元格）；TagName 列记录旧名</summary>
        private void OnTagCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            editStartTagName = null;
            if (e.RowIndex < 0) return;

            var row = RowAt<TagItemRow>(dgvTagList, e.RowIndex);
            if (row == null) return;
            ViewModel.NotifyRowEditing(row);

            if (e.ColumnIndex == colTagName.Index)
                editStartTagName = row.TagName;
        }

        /// <summary>
        /// 编辑结束：解除快照豁免；序号/TagName 列改动延迟到编辑管线后处理（管线内动集合会致 CurrencyManager 行索引瞬态错位——17:30 教训）
        /// 序号列 → 置脏 + 按新序号重排（行对象稳定，传对象非行号防错位）；
        /// TagName 列 → 改名只置脏 + 即时重名回滚（键迁移统一移交保存 ApplyEdits）
        /// </summary>
        private void OnTagCellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            ViewModel.NotifyRowEditing(null); // 编辑结束，恢复 1s 快照

            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (e.ColumnIndex != colTagId.Index && e.ColumnIndex != colTagName.Index) return;

            var row = RowAt<TagItemRow>(dgvTagList, e.RowIndex);
            if (row == null) return;

            if (e.ColumnIndex == colTagId.Index)
            {
                BeginInvoke(new Action(() => ViewModel.OnTagIdEdited(row)));
                return;
            }

            string oldName = editStartTagName;
            editStartTagName = null;
            if (string.IsNullOrEmpty(oldName)) return;

            // TagName 已由直绑写回新值；仅值变化才处理
            string newName = row.TagName;
            if (string.Equals(oldName, newName, StringComparison.Ordinal)) return;

            BeginInvoke(new Action(() => ViewModel.RenameTag(row, oldName)));
        }

        /// <summary>安全取 DGV 行绑定对象：编辑提交/ResetBindings 间隙行索引与行集合瞬态错位（CurrencyManager 越界），取不到返回 null 静默跳过。
        /// 三 DGV（实例列表/组/标签）共用泛型——原 RowRowAt/RowDeviceAt/RowGroupAt 逐字同构合并</summary>
        private static T RowAt<T>(DataGridView dgv, int rowIndex) where T : class
        {
            if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return null;
            try { return dgv.Rows[rowIndex].DataBoundItem as T; }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// 按绑定对象定位行并选中（Restore/Sync 三处共用定位内层）；找到置 Selected=true 返回 true，找不到 false。
        /// ⚠️ 瞬态防护（同 RowAt 惯例，2026-09-14 删除实例实测踩雷）：Rows[i] 索引器走 CurrencyManager.get_Item(i)，
        /// 而 Rows.Count 与 CurrencyManager.Count 在 ResetBindings/ClearInternal 进行中不一致 ——
        /// DGV 已完成 OnClearingRows 清行、CurrencyManager 尚未同步时，取 Rows[i] 抛 IndexOutOfRangeException
        /// （栈：ClearSelection → OnSelectionChanged → 本方法）。与 RowAt 同源防护，取不到当"该行无效"跳过，
        /// 循环不中断（后续索引可能已同步有效），最终找不到由调用方走 ClearDgvSelection 保持高亮一致。
        /// </summary>
        private static bool TrySelectRowByItem(DataGridView dgv, object item)
        {
            for (int i = 0; i < dgv.Rows.Count; i++)
            {
                var bound = RowBoundItemAt(dgv, i);
                if (bound == null) continue; // 瞬态错位：跳过该索引（收尾 null 语义 = 统一哨兵，循环内不提前 return）
                if (ReferenceEquals(bound, item))
                {
                    // 选中动作同样在瞬态窗口内可能抛（Selected setter 走 CurrencyManager）：兜住返回 false，
                    // 由调用方走 ClearDgvSelection 分支，不把异常抛进 DGV 的事件回调链
                    try { dgv.Rows[i].Selected = true; }
                    catch (Exception) { return false; }
                    return true;
                }
            }
            return false;
        }

        /// <summary>安全取某行绑定对象原始引用（瞬态错位返回 null，不抛）：同 RowAt 防护，只是不做泛型转换（本方法按引用相等比对）</summary>
        private static object RowBoundItemAt(DataGridView dgv, int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgv.Rows.Count) return null;
            try { return dgv.Rows[rowIndex].DataBoundItem; }
            catch (Exception) { return null; }
        }

        /// <summary>
        /// 清 DGV 选中（Restore/Sync 的 null/找不到分支共用；ClearSelection 无选中时零副作用）。
        /// ⚠️ 瞬态防护（同 RowAt/TrySelectRowByItem 惯例）：SelectedRows.Count 与 ClearSelection 在
        /// ResetBindings/ClearInternal 窗口内同样会触碰 CurrencyManager 抛错；捕获后静默返回——
        /// 清选是"保持一致"的收尾动作，瞬态期抛错无意义（DGV 本就在重建选中）。
        /// </summary>
        private static void ClearDgvSelection(DataGridView dgv)
        {
            try
            {
                if (dgv.SelectedRows.Count > 0)
                    dgv.ClearSelection();
            }
            catch (Exception) { /* 瞬态错位：DGV 正在重建行，清选无意义，静默跳过 */ }
        }

        /// <summary>取消切实例：DGV 选中回滚到原实例（触发 SelectionChanged 时 target==SelectedDevice → 处理器短路，无二次弹窗）。dev=null 防御直返</summary>
        private void RestorePlcSelection(DeviceRow dev)
        {
            if (dev == null) return;
            TrySelectRowByItem(dgvPLCList, dev);
        }

        /// <summary>取消切组：组 DGV 选中回滚到原组行（触发 SelectionChanged 时 target==SelectedGroupRow → 处理器短路，无二次弹窗）。
        /// 原选中不存在（null/已删/重载失效）→ 清 DGV 选中保持一致（VM 侧 SelectedGroupRow 本为 null）</summary>
        private void RestoreGroupSelection(TagGroupRow group)
        {
            if (group == null)
            {
                ClearDgvSelection(dgvGroupList);
                return;
            }
            if (!TrySelectRowByItem(dgvGroupList, group))
                ClearDgvSelection(dgvGroupList); // 原组行不在列表（被删/重载）→ 清选中防高亮悬空
        }

        /// <summary>按钮使能统一绑定 VM 派生状态（地址表域 + 连接配置域全部走绑定，无手工置灰）</summary>
        private void BindButtonStates()
        {
            // ---- 地址表域 ----
            btnPlcConn.DataBindings.Add(nameof(btnPlcConn.Enabled), ViewModel, nameof(PlcDebugViewModel.CanConnect));
            btnPlcDisConn.DataBindings.Add(nameof(btnPlcDisConn.Enabled), ViewModel, nameof(PlcDebugViewModel.CanDisconnect));
            btnPlcLoadIn.DataBindings.Add(nameof(btnPlcLoadIn.Enabled), ViewModel, nameof(PlcDebugViewModel.CanImport));
            btnPlcExport.DataBindings.Add(nameof(btnPlcExport.Enabled), ViewModel, nameof(PlcDebugViewModel.CanExport));

            btnSetCurrent.DataBindings.Add(nameof(btnSetCurrent.Enabled), ViewModel, nameof(PlcDebugViewModel.CanSetCurrent));
            btnSetAll.DataBindings.Add(nameof(btnSetAll.Enabled), ViewModel, nameof(PlcDebugViewModel.CanSetAll));

            btnTagAdd.DataBindings.Add(nameof(btnTagAdd.Enabled), ViewModel, nameof(PlcDebugViewModel.CanAddRow));
            btnTagDel.DataBindings.Add(nameof(btnTagDel.Enabled), ViewModel, nameof(PlcDebugViewModel.CanRemoveRow));
            btnTagUpper.DataBindings.Add(nameof(btnTagUpper.Enabled), ViewModel, nameof(PlcDebugViewModel.CanMoveRow));
            btnTagDown.DataBindings.Add(nameof(btnTagDown.Enabled), ViewModel, nameof(PlcDebugViewModel.CanMoveRow));
            btnTagRead.DataBindings.Add(nameof(btnTagRead.Enabled), ViewModel, nameof(PlcDebugViewModel.CanReadWrite));
            btnTagWrite.DataBindings.Add(nameof(btnTagWrite.Enabled), ViewModel, nameof(PlcDebugViewModel.CanReadWrite));
            btnTagEdit.DataBindings.Add(nameof(btnTagEdit.Enabled), ViewModel, nameof(PlcDebugViewModel.CanEditTag));
            btnTagCancel.DataBindings.Add(nameof(btnTagCancel.Enabled), ViewModel, nameof(PlcDebugViewModel.CanCancelTag));
            btnTagSave.DataBindings.Add(nameof(btnTagSave.Enabled), ViewModel, nameof(PlcDebugViewModel.CanSaveTag));
            // 未保存修改计数提示（如"保存(3)"；动态后缀仅数字，不涉翻译文案）
            btnTagSave.DataBindings.Add(nameof(btnTagSave.Text), ViewModel, nameof(PlcDebugViewModel.SaveButtonText));

            // ---- 连接配置域（设备管理 6 键；连接/断开不随会话门禁，绑定已在地址表域上方）----
            btnPlcAdd.DataBindings.Add(nameof(btnPlcAdd.Enabled), ViewModel, nameof(PlcDebugViewModel.CanAddPlc));
            btnPlcEdit.DataBindings.Add(nameof(btnPlcEdit.Enabled), ViewModel, nameof(PlcDebugViewModel.CanEditPlc));
            btnPlcSave.DataBindings.Add(nameof(btnPlcSave.Enabled), ViewModel, nameof(PlcDebugViewModel.CanSavePlc));
            btnPlcCancel.DataBindings.Add(nameof(btnPlcCancel.Enabled), ViewModel, nameof(PlcDebugViewModel.CanCancelPlc));
            btnPlcDel.DataBindings.Add(nameof(btnPlcDel.Enabled), ViewModel, nameof(PlcDebugViewModel.CanDeletePlc));
            btnPlcRebuild.DataBindings.Add(nameof(btnPlcRebuild.Enabled), ViewModel, nameof(PlcDebugViewModel.CanRebuild));

            // 当前实例 IO 队列深度：值 label 绑 VM 节拍现取属性（desc 前缀 label"队列深度:"为 Designer 静态文案，不绑）
            lblQueueDepth.DataBindings.Add(nameof(lblQueueDepth.Text), ViewModel, nameof(PlcDebugViewModel.CurrentQueueDepth));
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PlcDebugViewModel.SelectedDevice))
            {
                SyncPlcDgvSelection(); // VM 内部选行（删除后选邻行）→ DGV 选中跟随，保证"选中行 ↔ 配置区"一致
                RefreshConfigArea();   // 切实例：浏览态配置区整体重建（钩子已复位会话，ConfigRows=现取缓存扁平）
            }
            else if (e.PropertyName == nameof(PlcDebugViewModel.SelectedGroupRow))
                SyncGroupDgvSelection(); // VM 内部选组（导入/切实例置空/保存后恢复）→ 组 DGV 选中跟随，保证"无选中=无标签展示"一致
            else if (e.PropertyName == nameof(PlcDebugViewModel.Session))
                OnSessionChanged(); // 会话状态机（枚举化）：TagTableEdit 切列只读 / ConfigEdit 进出重建配置区
        }

        /// <summary>
        /// VM.SelectedDevice 变化 → 设备 DGV 选中行跟随（null=无选中清空）。
        /// 场景：删除成功后 VM 显式计算邻行（ReloadAndSelectAdjacent），而 DGV 经 ResetBindings 已自行落到别的行
        /// → 不跟随则"高亮行 ≠ 配置区实例"。
        /// 自动联动经 OnPlcListSelectionChanged 的 ReferenceEquals 短路（target==SelectedDevice）不重复写 VM、不弹窗。
        /// </summary>
        private void SyncPlcDgvSelection()
        {
            var cur = ViewModel.SelectedDevice;
            if (cur == null)
            {
                ClearDgvSelection(dgvPLCList);
                return;
            }
            // 定位并选中（已选中行 Selected=true 同值不触发 SelectionChanged；VM 内部置空/恢复走此跟随）
            if (!TrySelectRowByItem(dgvPLCList, cur))
                ClearDgvSelection(dgvPLCList); // 目标行不在列表（被删/重载失效）→ 清选中防高亮悬空
        }

        /// <summary>VM.SelectedGroupRow 变化 → 组 DGV 选中行跟随（null=无选中清空；自动联动经 ReferenceEquals 短路不弹窗）</summary>
        private void SyncGroupDgvSelection()
        {
            var cur = ViewModel.SelectedGroupRow;
            if (cur == null)
            {
                ClearDgvSelection(dgvGroupList);
                return;
            }
            // 定位并选中（已选中行 Selected=true 同值不触发 SelectionChanged；VM 内部置空/恢复走此跟随）
            TrySelectRowByItem(dgvGroupList, cur);
        }

        /// <summary>
        /// 会话状态变化分派（2026-09-08 枚举化，替代原 IsEditing/IsConfigEditing 双事件订阅）：
        /// - TagTableEdit 进出：地址表信息列只读切换（SetColumnsEditMode），配置区不变不重建；
        /// - ConfigEdit 进出：配置区双态渲染切换（浏览态只读 ↔ 会话态草稿可编辑）——VM 已先行切好 ConfigRows 渲染源（行先换、会话位后置）；
        /// 方向判断靠 lastSession（仅涉及 ConfigEdit 的切换才重建配置区，TagTableEdit↔None 不重建）。
        /// </summary>
        private void OnSessionChanged()
        {
            var cur = ViewModel.Session;
            bool tagEditing = cur == PlcDebugSession.TagTableEdit;
            SetColumnsEditMode(tagEditColumns, tagEditing);  // 进地址表会话解锁标签信息列
            SetColumnsEditMode(groupEditColumns, tagEditing); // 进地址表会话解锁组名/组描述列（组序号恒只读）
            bool configTouched = cur == PlcDebugSession.ConfigEdit || lastSession == PlcDebugSession.ConfigEdit;
            lastSession = cur;
            if (configTouched)
                RefreshConfigArea();
        }

        #endregion

        #region 配置信息区双态渲染（Designer 手建 pnlConfigContent1，只填内容）

        /// <summary>
        /// 用 VM.ConfigRows 渲染配置信息区（含 ProtocolParams 字典键值行展开），整区双态：
        /// 浏览态 = 现取缓存配置 Flatten（只读直显，SetReadOnly 整区锁）；配置会话态（Session==ConfigEdit）=
        /// 草稿 FlattenEditable（Protocol 行渲下拉，字段/字典行可编辑写回草稿——DataBindings/TextChanged，取消弃副本零副作用）。
        /// 渲染目标 = Designer 手建原生 Panel pnlConfigContent1（AutoScroll=true，原生滚动可靠，UIPanel 自绘不可靠问题已随类型更换消除）。
        /// 整行显示模式（valueWidth=0）：值列 Percent 自适应宿主宽——tabConfig 页全宽，参数整行显示（同 FormParamSetting 调用式）。
        /// 刷新驱动：SelectedDevice 变化 / Session 涉及 ConfigEdit 切换（VM 先行切好渲染源，见 OnViewModelPropertyChanged）。
        /// </summary>
        private void RefreshConfigArea()
        {
            // 换实例重建前复位滚动偏移，防旧滚动位置残留到新内容
            pnlConfigContent1.AutoScrollPosition = new Point(0, 0);

            pnlConfigContent1.SuspendLayout();
            try
            {
                // 摘除即释放——不能只 Clear（句柄不随摘除销毁，只等 GC 会走终结器线程 Dispose，见 UiControlHelper 头注）；
                // 紧随其后的 Render 内部同样走该清理（此时 Controls 已空，幂等）
                UiControlHelper.DisposeAndClear(pnlConfigContent1);

                if (ViewModel.ConfigRows.Count == 0)
                {
                    pnlConfigContent1.Controls.Add(new UILabel
                    {
                        Text = "— 选中实例后显示配置信息 —",
                        Dock = DockStyle.Fill,
                        TextAlign = ContentAlignment.MiddleCenter,
                        ForeColor = Color.FromArgb(150, 150, 150),
                        Font = new Font("微软雅黑", 12F),
                    });
                    return;
                }

                // 整行自适应模式（valueWidth=0）：渲染根 Dock=Fill，值列 Percent 随宿主宽伸缩（长值不截断，Panel AutoScroll 纵向承载）
                ConfigFormRenderer.Render(pnlConfigContent1, ViewModel.ConfigRows);
                // 编辑会话态解锁（草稿可编辑：字段直绑/字典写回）；浏览态整区只读（现取缓存零写入口——SetReadOnly 覆盖所有 UITextBox/下拉/开关）
                ConfigFormRenderer.SetReadOnly(pnlConfigContent1, readOnly: !ViewModel.IsConfigEditing);
                // 渲染产物是运行时新控件树，补一次递归双缓冲（抑制内容逐行出现时的重绘闪烁）
                UiDpi.EnableDoubleBuffered(pnlConfigContent1);
            }
            finally
            {
                pnlConfigContent1.ResumeLayout(true);
            }
        }

        #endregion

        #region 选中初始化

        private void SelectFirstPlcIfAny()
        {
            if (!isFirstLoad) return;
            isFirstLoad = false;
            ViewModel.UnlockPlcSelection(); // 首装载完成：解除重载闸门，后续用户点选正常生效
            if (dgvPLCList.Rows.Count == 0) return;
            // 只补选不 ClearSelection：清空选中会先触发 SelectedDevice=null（整页清空联动）再重选（整页重装），
            // 若发生在页面可见后 = "按钮态来回切换/配置区多次刷新"的闪烁；DGV 绑定填充时已自动选中首行则跳过（联动已随装载完成）
            if (dgvPLCList.SelectedRows.Count == 0)
                dgvPLCList.Rows[0].Selected = true;
        }

        #endregion

        #region 轮询刷新（1s：连接按钮态 RefreshConnectionButtons + 设备行 RefreshDeviceRows + 队列深度 RefreshPlcQueueDepth + 地址表展示列 RefreshTagRowValues，全部行级/通知式）

        /// <summary>
        /// 页面是否在舞台上。切页 = mainPanel.Controls.Remove / Controls.Add（内嵌页），
        /// 既不 Close 也不 Dispose ⇒ OnFormClosing 里那条 Stop 永远等不到。
        /// 口径与 FormProductLive 一致：Parent != null &amp;&amp; Visible。
        /// </summary>
        private bool IsOnStage => Parent != null && Visible;

        /// <summary>
        /// 按可见性同步轮询定时器。
        /// 🔴 页面是容器单例（只创建一次、永不销毁），不按可见性停就会一直在后台每秒
        ///    现取编排口 + 地址表快照（RefreshPlcQueueDepth / RefreshTagRowValues），
        ///    切走后这些设备访问与快照全是白做的。
        /// </summary>
        private void SyncPolling()
        {
            pollTimer.Enabled = IsOnStage;
        }

        /// <summary>
        /// 切页主挂点（🔴 不是 OnVisibleChanged）。
        /// Controls.Remove 后 Control.Visible 仍返回 true（无 Parent 时读自身标志），
        /// VisibleChanged 不触发——FormProductLive 已实证，本页照同一口径补。
        /// </summary>
        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            if (!initialized) return;
            SyncPolling();
        }

        /// <summary>自身显隐（Show/Hide）补充挂点：切页走 ParentChanged，这里兜 Hide。</summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (!initialized) return;
            SyncPolling();
        }

        private void StartPolling()
        {
            pollTimer.Interval = 1000;
            pollTimer.Tick += (s, e) =>
            {
                ViewModel.RefreshConnectionButtons(); // 连接/断开按钮使能（IsConnected 驱动无事件源 → 全绑重读现取）
                ViewModel.RefreshDeviceRows();        // 设备列表连接状态行（状态翻转 ResetItem，无变化零重绘）
                ViewModel.RefreshPlcQueueDepth();     // 地址表顶栏队列深度 label（现取编排口，值变才通知）
                ViewModel.RefreshTagRowValues();      // 地址表展示列快照：仅变化行 ResetItem 行级通知
            };
            initialized = true;
            SyncPolling();
        }

        #endregion

        #region 单元格格式化（设备列表连接列按行状态上色——文本已绑 ConnectionText；地址表仅行状态上色，展示列文本已绑定）

        /// <summary>
        /// 设备列表连接状态列上色：文本已绑 DeviceRow.ConnectionText（VM 节拍刷新），此处只按行状态取色，
        /// 不再现取 IDevice——行状态与文本同拍，杜绝文本/颜色差一拍的错配。
        /// </summary>
        private void OnCellFormattingPlc(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = RowAt<DeviceRow>(dgvPLCList, e.RowIndex);
            if (row == null) return;

            if (e.ColumnIndex == colPlcStatus.Index)
                e.CellStyle.ForeColor = row.IsConnected ? ColorOkFore : ColorDisconnectedFore;
        }

        /// <summary>
        /// 地址表行状态绘制收口（展示列已绑定预格式化文本，此处零取值零格式化）：
        /// 1) 草稿行底纹——IsDirty 明黄（未保存修改）/ IsNew 淡青（未保存新增）；保存/放弃清态后下次重绘自然回白，无需手动恢复；
        /// 2) 最近结果列红绿（成功绿/失败红）；最近信息列非空即红（成功时驱动清空 Msg，非空=失败信息）
        /// </summary>
        private void OnCellFormattingTag(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = RowAt<TagItemRow>(dgvTagList, e.RowIndex);
            if (row == null) return;

            // 草稿行底纹（行状态绘制）
            if (row.IsDirty)
                e.CellStyle.BackColor = ColorDirtyBack; // 柔和明黄（纯 Yellow 太刺眼）
            else if (row.IsNew)
                e.CellStyle.BackColor = ColorNewBack; // 淡青（区分草稿行）

            // 结果/信息列红绿前景（结果文本与 TagItemRow 拼串同源常量）
            if (e.ColumnIndex == colTagOpResult.Index)
            {
                if (string.Equals(row.OpResultText, TagItemRow.OpResultFailText, StringComparison.Ordinal))
                    e.CellStyle.ForeColor = ColorFailFore;
                else if (string.Equals(row.OpResultText, TagItemRow.OpResultSuccessText, StringComparison.Ordinal))
                    e.CellStyle.ForeColor = ColorOkFore;
            }
            else if (e.ColumnIndex == colTagOpMsg.Index)
            {
                if (!string.IsNullOrEmpty(row.OpMsgText))
                    e.CellStyle.ForeColor = ColorFailFore; // 成功时驱动清空 LastOperationMsg，非空即失败信息
            }
        }

        /// <summary>组 DGV 行状态绘制收口：草稿组行底纹（IsDirty 明黄 / IsNew 淡青），语义同标签行</summary>
        private void OnCellFormattingGroup(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var row = RowAt<TagGroupRow>(dgvGroupList, e.RowIndex);
            if (row == null) return;

            if (row.IsDirty)
                e.CellStyle.BackColor = ColorDirtyBack;
            else if (row.IsNew)
                e.CellStyle.BackColor = ColorNewBack;
        }

        #endregion
    }
}
