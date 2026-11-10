using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Sunny.UI;
using Taoyl.Application.MesDriver.Models;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.Models.Mes;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// MES 调试页面（类 Postman：左接口列表 + 右请求配置 / 响应查看）。
    /// 本文件只负责控件与绑定：值字段绑草稿（ViewModel.Editing，路径一段），可用性绑 ViewModel。
    ///
    /// 🔴 绑定只在 OnLoad 建一次：草稿是只读引用、装载走 LoadFrom 原地灌值，永不换实例。
    /// 两段路径（ViewModel.Editing.Xxx）下源→控件的刷新会失效（实测：格式化改了模型、界面不动），
    /// 所以 dataSource 一律传草稿实例本身，dataMember 永远是一段。
    /// 🔴 投影只有三个驱动入口：草稿的 PropertyChanged（BodyType / AuthType / 枚举）、
    /// ViewModel.PropertyChanged（IsReadOnly / SelectedApiName）、OnLoad 末尾的首次投影。
    /// 值字段不要在这里同步——绑定已经管了，再加一遍就是两条真相。
    /// 🔴 面板显隐走事件同步，不走 Visible 绑定（Visible 绑定不可靠：控件未 CreateControl 前不生效，
    /// 而 5 个面板初始 Visible=false 正好踩这条；另见 MS KB 327305）。
    /// 🔴 枚举下拉的选中项也只能显式投影：UIComboBox.SelectedItem 没有变更事件，无法双向绑定。
    /// 🔴 5 个平铺 radio 与 MesBodyType 五值恒等映射，所以不需要防回环标志。
    /// 🔴 形态 → radio / 面板由 bodyTypeMap 派生；编辑器控件不在表里（三个原文框绑同一字段）。
    /// 🔴 DGV 的列一律在代码里建（SetupGrids），不写进 Designer：设计器保存会重生成
    /// InitializeComponent，手写的 Columns 会被吃掉（已实际发生过）。
    /// 🔴 只读走逐列 ReadOnly，不设 DGV 级：后者优先级更高，会连带禁掉新增行的输入。
    /// DataGridViewColumn 不是 Control，没有 DataBindings，列只读只能代码赋值。
    /// 🔴 报文框用 UITextBox，绝不能用 RichEdit 系控件（实测整页卡顿）。
    /// </summary>
    public partial class FormMesDebug : UIPage
    {
        public MesDebugViewModel ViewModel { get; set; }

        /// <summary>
        /// 单元格字体：UIDataGridView 单元格是原生绘制，不走 SunnyUI 的 GlobalFontScale，
        /// 必须按 DPI 物理放大——这是 UiDgvHelper.Setup 对 cellFont 参数的约定（行高与列头高它已自行缩放）。
        /// </summary>
        private readonly Font cellFont = new Font("微软雅黑", 12F * UiDpi.DpiScale);

        /// <summary>
        /// 列头字体：列头绘制走 GlobalFontScale，若再物理放大会双重放大，故保持基准值。
        /// </summary>
        private readonly Font headerFont = new Font("微软雅黑", 12F, FontStyle.Bold);

        /// <summary>
        /// dgv.Font（列头基准字体）：SunnyUI 网格会在此基础上按 GlobalFontScale 缩放，同样必须保持基准值。
        /// 不可沿用 cellFont——那份已按 DPI 物理放大，再被放大一次就是双重放大。
        /// </summary>
        private readonly Font controlFont = new Font("微软雅黑", 12F);

        /// <summary>
        /// 正文形态 → radio + 面板的唯一登记表。面板显隐、radio 选中、DPI 补偿、事件订阅都从这张表派生。
        /// 🔴 编辑器控件**不在这张表里**：三个原文框绑同一个正文属性（见 BindDraftValues），
        /// Form 用表格、None 只有标签——「按形态查控件」的消费者已经不存在了。
        /// </summary>
        private readonly (MesBodyType Type, UIRadioButton Radio, UIPanel Panel)[] bodyTypeMap;

        /// <summary>鉴权方式 ↔ 面板的映射（加一种鉴权方式只加一行，不再四行硬编码）。</summary>
        private readonly (MesAuthType Type, UIPanel Panel)[] authTypeMap;

        private DataGridViewColumn[] queryEditColumns;
        private DataGridViewColumn[] headerEditColumns;
        private DataGridViewColumn[] bodyFormEditColumns;

        public FormMesDebug()
        {
            InitializeComponent();

            // 必须放构造函数（InitializeComponent 之后）：字段初始化器不能引用 this
            // ——引用 rdoBodyNone 这类实例字段是编译错 CS0236，只能在这里赋值
            bodyTypeMap = new (MesBodyType Type, UIRadioButton Radio, UIPanel Panel)[]
            {
                (MesBodyType.None, rdoBodyNone, pnlBodyNone),
                (MesBodyType.Form, rdoBodyForm, pnlBodyForm),
                (MesBodyType.Json, rdoBodyJson, pnlBodyJson),
                (MesBodyType.Text, rdoBodyText, pnlBodyText),
                (MesBodyType.Xml, rdoBodyXml, pnlBodyXml),
            };

            authTypeMap = new[]
            {
                (MesAuthType.None, pnlAuthNone),
                (MesAuthType.Bearer, pnlAuthBearer),
                (MesAuthType.Basic, pnlAuthBasic),
                (MesAuthType.ApiKey, pnlAuthApiKey),
            };

            SetupDpiAdjustments();
        }

        /// <summary>
        /// DPI 补偿（System aware 物理像素布局：Designer 里的尺寸不会随 DPI 放大，须运行期按 UiDpi.Scale 重算）。
        /// 🔴 一律放 .cs 不放 Designer.cs——设计器重开会冲掉手写代码。
        /// </summary>
        private void SetupDpiAdjustments()
        {
            // ItemSize：TabPage 的排版由 TabControl 运行期按 ItemSize 重算，Designer 的 Location 只是设计时快照；
            // 不补偿则高 DPI 下页签高度不足、标签文字被裁
            tabRequest.ItemSize = new Size(UiDpi.Scale(150), UiDpi.Scale(40));
            tabResponse.ItemSize = new Size(UiDpi.Scale(150), UiDpi.Scale(40));

            // ItemHeight：UIComboBox 没有 ItemSize，内层 ListBoxEx 构造期固定 25 物理像素，
            // 不补偿则项高夹字，且下拉总高 = 4 + min(项数,8)×ItemHeight 一并偏矮
            cmbMethod.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbAuthType.ItemHeight = ConfigFormStyle.ComboItemHeight;
            cmbAuthKeyIn.ItemHeight = ConfigFormStyle.ComboItemHeight;

            // RadioButtonSize：圆点是 SunnyUI 自绘，不走字体缩放（200% 下实测不放大），按设计基准显式补偿
            foreach (var entry in bodyTypeMap)
                entry.Radio.RadioButtonSize = UiDpi.Scale(20);
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (ViewModel == null)
            {
                // 容器配置错误必须可见：静默 return 的表现是「所有按钮点了完全没反应」，极难定位。
                // 此刻正是注入可能整体失效的时候，用 UIMessageBox 而不是注入的服务
                UIMessageBox.ShowError("MES 调试页未注入 ViewModel。请检查 MesDebugViewModel 是否已在 FormModule 注册。");
                return;
            }

            // OnLoad 每实例只触发一次：Form.Load 的契约是「首次显示前」，而页面切换走
            // mainPanel.Controls.Remove/Add（实例与句柄都不重建），所以订阅不需要幂等守卫。
            UiDpi.EnableDoubleBuffered(this);
            SetupGrids();          // 建列 + 初始只读态
            NormalizeTabPages();

            // 先订阅后装载：Initialize 灌草稿时的属性通知才会驱动投影，末尾不必再补一遍逐项手工同步
            BindEvents();
            ViewModel.Initialize();

            // 草稿默认值（无正文 / 无鉴权）与空定义的取值相同，SetProperty 会短路不发通知，
            // 所以首次的形态面板、鉴权面板、枚举下拉仍需显式投影一次
            SyncBodyUi();
            SyncAuthUi();
            SyncEnumCombos();
        }

        /// <summary>TabPage 关掉视觉样式背景：开着时内容变化会连带重绘整页背景，表现为两页一起闪。</summary>
        private void NormalizeTabPages()
        {
            foreach (TabPage page in tabRequest.TabPages)
                SetPageStyle(page);

            foreach (TabPage page in tabResponse.TabPages)
                SetPageStyle(page);
        }

        private static void SetPageStyle(TabPage page)
        {
            page.UseVisualStyleBackColor = false;
            page.BackColor = Color.White;
        }

        #region DGV 建列

        private void SetupGrids()
        {
            // 🔴 DGV 级 ReadOnly 优先级高于列级，必须显式 false，否则列上设 ReadOnly=false 也没用
            foreach (DataGridView grid in new[] { dgvQueryParams, dgvHeaders, dgvBodyForm })
            {
                grid.ReadOnly = false;
                grid.AllowUserToAddRows = false;
                grid.AllowUserToDeleteRows = false;
                grid.EditMode = DataGridViewEditMode.EditOnKeystrokeOrF2;
            }

            queryEditColumns = SetupGrid(dgvQueryParams, "参数名", "参数值", ViewModel.Editing.QueryParams);
            headerEditColumns = SetupGrid(dgvHeaders, "头名称", "头值", ViewModel.Editing.Headers);
            bodyFormEditColumns = SetupGrid(dgvBodyForm, "键", "值", ViewModel.Editing.BodyFormItems);

            SetupApiNameGrid();
            ApplyGridEditMode();   // 列已建好，按当前会话态定一次只读
        }

        /// <summary>左栏接口列表：两列，绑 ApiNameItem.Name / ApiNameItem.DisplayName。</summary>
        private void SetupApiNameGrid()
        {
            UiDgvHelper.Setup(dgvApiNames, cellFont, headerFont, controlFont, showRowNumbers: false);
            dgvApiNames.AutoGenerateColumns = false;
            dgvApiNames.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvApiNames.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvApiNames.MultiSelect = false;
            dgvApiNames.ReadOnly = true;
            dgvApiNames.AllowUserToAddRows = false;
            dgvApiNames.AllowUserToDeleteRows = false;
            dgvApiNames.AllowUserToResizeRows = false;
            dgvApiNames.Columns.Clear();

            // 两列：接口名（技术键，Invoke 用的就是它）+ 接口描述（底层 DisplayName，给人看）
            DataGridViewColumn nameColumn = AddTextColumn(
                dgvApiNames, "接口名", nameof(ApiNameItem.Name), fillWeight: 55);
            nameColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

            DataGridViewColumn descColumn = AddTextColumn(
                dgvApiNames, "接口描述", nameof(ApiNameItem.DisplayName), fillWeight: 45);
            descColumn.SortMode = DataGridViewColumnSortMode.NotSortable;

            dgvApiNames.DataSource = ViewModel.ApiNames;
        }

        /// <summary>
        /// 三个键值表同构：两列业务字段 + 一列说明。
        /// 说明只进配置，驱动转 k=v&amp;… 时会丢弃。
        /// </summary>
        private DataGridViewColumn[] SetupGrid(DataGridView grid, string nameHeader, string valueHeader, object source)
        {
            UiDgvHelper.Setup(grid, cellFont, headerFont, controlFont, showRowNumbers: false);
            grid.AutoGenerateColumns = false;
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;
            grid.AllowUserToResizeRows = false;
            grid.Columns.Clear();

            DataGridViewColumn name = AddTextColumn(grid, nameHeader, nameof(MesHeader.Name), width: 240);
            DataGridViewColumn value = AddTextColumn(grid, valueHeader, nameof(MesHeader.Value), fillWeight: 60);
            DataGridViewColumn desc = AddTextColumn(grid, "说明", nameof(MesHeader.Description), fillWeight: 40);

            grid.DataSource = source;

            return new[] { name, value, desc };
        }

        private static DataGridViewColumn AddTextColumn(
            DataGridView grid,
            string headerText,
            string dataProperty,
            int width = 0,
            int fillWeight = 0)
        {
            var column = new DataGridViewTextBoxColumn
            {
                HeaderText = headerText,
                DataPropertyName = dataProperty,
                DefaultCellStyle = new DataGridViewCellStyle
                {
                    Alignment = DataGridViewContentAlignment.MiddleLeft
                }
            };

            if (fillWeight > 0)
            {
                column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                column.FillWeight = fillWeight;
            }
            else
            {
                column.Width = width;
                column.MinimumWidth = 60;
            }

            grid.Columns.Add(column);
            return column;
        }

        private static void SetColumnsEditMode(DataGridViewColumn[] columns, bool editing)
        {
            foreach (DataGridViewColumn column in columns)
                column.ReadOnly = !editing;
        }

        #endregion

        #region 事件绑定

        /// <summary>
        /// 全部接线与绑定，只在 OnLoad 调一次（草稿是只读引用，绑定不需要重建）。
        /// 🔴 值绑定绑草稿（ViewModel.Editing），可用性绑 VM——两者路径都是**一段**。
        /// 「ViewModel.Editing.Xxx」这种两段路径下源→控件的刷新会失效（实测：格式化改了模型、界面不动）。
        /// </summary>
        private void BindEvents()
        {
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;

            // 草稿的 PropertyChanged 直接订阅（标准 INotifyPropertyChanged）：VM 不再为 View 转发一次
            ViewModel.Editing.PropertyChanged += OnEditingPropertyChanged;

            BindToolbar();
            BindBodyRadios();
            BindApiList();
            BindDraftValues();
            BindStates();
            BindStatusArea();
        }

        /// <summary>
        /// 值绑定：绑草稿（ViewModel.Editing），路径一段，一个控件一行——哪个控件绑哪个属性在这里全部可见。
        /// 🔴 三个原文框绑的是**同一个** BodyRawText：形态只决定驱动侧怎么处理这段文本，不决定它存在哪。
        /// 🔴 数值字段用 string 承载（超时 / 次数 / 间隔），理由见 MesApiEditModel 类注释。
        /// </summary>
        private void BindDraftValues()
        {
            txtApiName.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.Name), true, DataSourceUpdateMode.OnPropertyChanged);
            txtDisplayName.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.DisplayName), true, DataSourceUpdateMode.OnPropertyChanged);
            txtUrl.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.Url), true, DataSourceUpdateMode.OnPropertyChanged);
            txtTimeoutMs.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.TimeoutMsText), true, DataSourceUpdateMode.OnPropertyChanged);
            txtMaxAttempts.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.MaxAttemptsText), true, DataSourceUpdateMode.OnPropertyChanged);
            txtBackoffMs.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.BackoffMsText), true, DataSourceUpdateMode.OnPropertyChanged);

            txtAuthToken.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.AuthToken), true, DataSourceUpdateMode.OnPropertyChanged);
            txtAuthUserName.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.AuthUserName), true, DataSourceUpdateMode.OnPropertyChanged);
            txtAuthPassword.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.AuthPassword), true, DataSourceUpdateMode.OnPropertyChanged);
            txtAuthKeyName.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.AuthKeyName), true, DataSourceUpdateMode.OnPropertyChanged);
            txtAuthKeyValue.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.AuthKeyValue), true, DataSourceUpdateMode.OnPropertyChanged);

            // 三个原文面板各一个文本框，绑同一份正文——切形态内容跟着走，不会丢
            txtBodyJson.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.BodyRawText), true, DataSourceUpdateMode.OnPropertyChanged);
            txtBodyText.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.BodyRawText), true, DataSourceUpdateMode.OnPropertyChanged);
            txtBodyXml.DataBindings.Add(nameof(Control.Text), ViewModel.Editing, nameof(MesApiEditModel.BodyRawText), true, DataSourceUpdateMode.OnPropertyChanged);

            // UISwitch.Active 的 setter 会触发 ActiveChanged，WinForms 绑定正是靠「属性名 + Changed」找写回时机，
            // 双向都自动完成，不需要 EventHandler，也不需要单独的 Sync 方法
            swiCertValidation.DataBindings.Add(nameof(UISwitch.Active), ViewModel.Editing, nameof(MesApiEditModel.EnableCertificateValidation), true, DataSourceUpdateMode.OnPropertyChanged);

            // 枚举下拉只能填充 + 事件写回：UIComboBox.SelectedItem 没有变更事件，双向绑定无从触发
            BindEnumCombo(cmbMethod, typeof(MesHttpMethod), v => ViewModel.Editing.Method = (MesHttpMethod)v);
            BindEnumCombo(cmbAuthType, typeof(MesAuthType), v => ViewModel.Editing.AuthType = (MesAuthType)v);
            BindEnumCombo(cmbAuthKeyIn, typeof(MesKeyIn), v => ViewModel.Editing.AuthKeyIn = (MesKeyIn)v);
        }

        /// <summary>
        /// 可用性绑定：绑 ViewModel 的会话态。控件名单逐个写出来——从表里取控件，读的人得跳到表才知道绑了谁。
        /// 🔴 SunnyUI 的 UITextBox 只设 Enabled=false 时外观变灰、但仍能输入，必须同时绑 ReadOnly（已实测）。
        /// 🔴 绑定不支持取反，所以 VM 里为「可编辑」单列了取反属性。
        /// </summary>
        private void BindStates()
        {
            // 单行字段：只读态要「变灰 + 真的输不进去」
            BindEditable(txtApiName, txtDisplayName, txtUrl, txtTimeoutMs, txtMaxAttempts, txtBackoffMs,
                         txtAuthToken, txtAuthUserName, txtAuthPassword, txtAuthKeyName, txtAuthKeyValue);

            // 下拉 / 容器 / 开关没有 ReadOnly，只绑 Enabled
            BindEnabled(cmbMethod, cmbAuthType, cmbAuthKeyIn, tblBodyType, swiCertValidation);

            // 正文框只绑 ReadOnly：浏览态保持白底可读，不置灰（与单行字段的观感不同，刻意为之）
            txtBodyJson.DataBindings.Add(nameof(UITextBox.ReadOnly), ViewModel, nameof(MesDebugViewModel.IsReadOnly));
            txtBodyText.DataBindings.Add(nameof(UITextBox.ReadOnly), ViewModel, nameof(MesDebugViewModel.IsReadOnly));
            txtBodyXml.DataBindings.Add(nameof(UITextBox.ReadOnly), ViewModel, nameof(MesDebugViewModel.IsReadOnly));

            btnFormatBody.DataBindings.Add(nameof(Control.Enabled), ViewModel, nameof(MesDebugViewModel.CanFormatBody));

            // 按钮可用性：与流程监控页同风格，绑 VM 的 CanXxx
            btnEdit.DataBindings.Add(nameof(btnEdit.Enabled), ViewModel, nameof(MesDebugViewModel.CanEdit));
            btnAdd.DataBindings.Add(nameof(btnAdd.Enabled), ViewModel, nameof(MesDebugViewModel.CanAdd));
            btnSave.DataBindings.Add(nameof(btnSave.Enabled), ViewModel, nameof(MesDebugViewModel.CanSave));
            btnCancel.DataBindings.Add(nameof(btnCancel.Enabled), ViewModel, nameof(MesDebugViewModel.CanCancel));
            btnDelete.DataBindings.Add(nameof(btnDelete.Enabled), ViewModel, nameof(MesDebugViewModel.CanDelete));
            btnSend.DataBindings.Add(nameof(btnSend.Enabled), ViewModel, nameof(MesDebugViewModel.CanSend));

            // 与 btnSend 同一个 IsBusy 的两个极性：互斥由构造保证，不靠手动同步
            btnCancelSend.DataBindings.Add(nameof(btnCancelSend.Enabled), ViewModel, nameof(MesDebugViewModel.IsBusy));
        }

        /// <summary>Enabled + ReadOnly 一起绑：单行输入框要「变灰 + 真的输不进去」。</summary>
        private void BindEditable(params UITextBox[] boxes)
        {
            foreach (UITextBox box in boxes)
            {
                box.DataBindings.Add(nameof(Control.Enabled), ViewModel, nameof(MesDebugViewModel.IsEditable));
                box.DataBindings.Add(nameof(UITextBox.ReadOnly), ViewModel, nameof(MesDebugViewModel.IsReadOnly));
            }
        }

        /// <summary>只绑 Enabled：这些控件没有 ReadOnly 属性。</summary>
        private void BindEnabled(params Control[] controls)
        {
            foreach (Control control in controls)
                control.DataBindings.Add(nameof(Control.Enabled), ViewModel, nameof(MesDebugViewModel.IsEditable));
        }

        /// <summary>
        /// 左栏接口列表。用户意图有两个入口：鼠标 CellClick、键盘 KeyUp（换行键）。
        ///
        /// 🔴 用 CellClick / KeyUp 而不是 SelectionChanged：SelectionChanged 在我们自己改选中时也会触发
        /// （回滚、换接口同步、重绑），在里面弹模态框 + 重绑数据源会与 DGV 内部的选中设置过程打架
        /// —— PLC 页 2026-09-14 实测，回调内触碰 Rows/SelectedRows 会因 Rows.Count 与
        /// CurrencyManager.Count 瞬态不一致而抛异常。
        /// 这两个事件只由真实用户操作触发，程序化改选中永不重入 → 不需要防回环标志，也不需要 BeginInvoke。
        /// 🔴 键盘必须用 KeyUp 不能用 KeyDown：KeyDown 时 DGV 还没移动 CurrentRow，拿到的是旧行。
        /// </summary>
        private void BindApiList()
        {
            // DataSource 已在 SetupApiNameGrid 里设好
            dgvApiNames.CellClick += OnApiListClicked;
            dgvApiNames.KeyUp += OnApiListKeyUp;

            // 列表重绑（ReloadApiList → ResetBindings）后 DGV 会自动落首行，而「保存但没改名」时
            // SelectedApiName 不变、不发通知，高亮就会停在第一条。绑定完成后按 VM 重新定位。
            dgvApiNames.DataBindingComplete += (s, e) => SyncApiSelection();
        }

        private void OnApiListClicked(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.RowIndex >= dgvApiNames.Rows.Count)
                return;

            TrySwitchTo(dgvApiNames.Rows[e.RowIndex].DataBoundItem as ApiNameItem);
        }

        /// <summary>键盘换行：只认会改变选中行的键，其余（F2 / 空格 / 回车等）不触发切换。</summary>
        private void OnApiListKeyUp(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Up:
                case Keys.Down:
                case Keys.Home:
                case Keys.End:
                case Keys.PageUp:
                case Keys.PageDown:
                    break;

                default:
                    return;
            }

            TrySwitchTo(dgvApiNames.CurrentRow == null
                ? null
                : dgvApiNames.CurrentRow.DataBoundItem as ApiNameItem);
        }

        /// <summary>鼠标与键盘的唯一汇合点：切换意图 → Guard → 失败回滚高亮。</summary>
        private void TrySwitchTo(ApiNameItem item)
        {
            if (item == null)
                return;

            // 编辑态会弹三选一：先把高亮拨回已提交行，对话框出现时界面显示的就是「当前接口」。
            // 发送中会被 VM 直接拒绝（TrySwitchApi 判 IsBusy），同样要先回滚——否则高亮跳到目标行
            // 而内容一个都没变，比不动更怪。
            // 切换成功时 SelectedApiName 变化会经 case SelectedApiName 再拨到目标行；
            // 纯浏览态不弹框，不必先回滚（否则每次正常切换都会闪一下）。
            if (ViewModel.IsEditing || ViewModel.IsBusy)
                SyncApiSelection();

            ViewModel.TrySwitchApi(item.Name);
        }

        private void SyncApiSelection()
        {
            // 按 DataBoundItem 反查行，不依赖索引——将来即使放开排序也不会错位。
            // 调用点只有三处：SelectedApiName 通知、DataBindingComplete、切换前回滚（鼠标/键盘），
            // 都在 DGV 内部的选中设置流程之外，所以这里改 CurrentCell 是安全的。
            foreach (DataGridViewRow row in dgvApiNames.Rows)
            {
                ApiNameItem item = row.DataBoundItem as ApiNameItem;
                if (item == null || !string.Equals(item.Name, ViewModel.SelectedApiName, StringComparison.Ordinal))
                    continue;

                if (dgvApiNames.CurrentRow == row)
                    return;

                dgvApiNames.CurrentCell = row.Cells[0];
                return;
            }
        }

        private void BindToolbar()
        {
            btnEdit.Click += (s, e) => ViewModel.EditCommand.Execute(null);
            btnAdd.Click += (s, e) => ViewModel.AddCommand.Execute(null);
            btnSave.Click += (s, e) => ViewModel.SaveCommand.Execute(null);
            btnCancel.Click += (s, e) => ViewModel.CancelCommand.Execute(null);
            btnDelete.Click += (s, e) => ViewModel.DeleteCommand.Execute(null);
            btnCancelSend.Click += (s, e) => ViewModel.CancelSendCommand.Execute(null);
            btnFormatBody.Click += (s, e) => ViewModel.FormatBodyCommand.Execute(null);

            // 不 await、不包 async void：SendAsync 内部 try/catch/finally 全兜住，Invoke 本身永不抛。
            // 若将来 SendAsync 改成会抛，这行会静默吞异常——届时必须补 await + catch。
            btnSend.Click += (s, e) => ViewModel.SendCommand.ExecuteAsync(null);
        }

        /// <summary>
        /// 五个形态 radio 的选中变化汇到一处。程序化设 Checked 也会触发，靠「写入同值 → SetProperty 短路」止步，
        /// 所以不需要防回环标志（五值 ↔ 五控件是恒等映射）。
        /// </summary>
        private void BindBodyRadios()
        {
            rdoBodyNone.CheckedChanged += OnBodyTypeRadioChanged;
            rdoBodyForm.CheckedChanged += OnBodyTypeRadioChanged;
            rdoBodyJson.CheckedChanged += OnBodyTypeRadioChanged;
            rdoBodyText.CheckedChanged += OnBodyTypeRadioChanged;
            rdoBodyXml.CheckedChanged += OnBodyTypeRadioChanged;
        }

        private void OnBodyTypeRadioChanged(object sender, EventArgs e)
        {
            var radio = sender as UIRadioButton;

            // 只处理「被选中」的那次：一组里另一个被取消时会再触发一次，忽略掉
            if (radio == null || !radio.Checked)
                return;

            foreach (var entry in bodyTypeMap)
            {
                if (entry.Radio != radio)
                    continue;

                // 写模型即完成：通知会经 OnEditingPropertyChanged 回到 SyncBodyUi，这里不再同步控件
                ViewModel.Editing.BodyType = entry.Type;
                return;
            }
        }

        /// <summary>
        /// 正文形态的全部投影：5 个 radio 的选中态、5 个面板的显隐、格式化按钮的可见性。
        ///
        /// 🔴 这是恒等映射（5 个枚举值 ↔ 5 组控件），所以顺序无关、也不需要防回环标志：
        ///    写控件 → 控件事件回写模型 → 与当前值相等 → SetProperty 短路 → 链条自然停。
        /// 🔴 显隐仍走事件同步不走 Visible 绑定（绑定从不写入，见类头注释）。
        /// 🔴 按钮的显示与否查 MesBodyTypeUi（与启用与否分开：文本形态按钮可见但禁用）。
        /// </summary>
        private void SyncBodyUi()
        {
            MesBodyType type = ViewModel.Editing.BodyType;
            MesBodyTypeUi.UiInfo ui = MesBodyTypeUi.For(type);

            pnlBodyHost.SuspendLayout();

            // btnFormatBody 的父容器是 tblBodyType（不是 pnlBodyHost），它的 Visible 变化
            // 会让容器重排全部单元格——必须单独挂起，否则这次重排落在挂起区之外
            tblBodyType.SuspendLayout();
            try
            {
                bool radioMatched = false;
                foreach (var entry in bodyTypeMap)
                {
                    bool on = entry.Type == type;
                    entry.Radio.Checked = on;
                    entry.Panel.Visible = on;

                    if (on)
                        radioMatched = true;
                }

                // 未登记的形态 = 代码缺陷（Normalize 已保证配置值合法），不是运行状态。
                // 兜底仍显示文本面板：MesBodyTypeUi 已把它回落成文本编辑器，正文不会丢。
                if (!radioMatched)
                {
                    Debug.Assert(false, "正文形态未登记进 bodyTypeMap");
                    pnlBodyText.Visible = true;
                }

                btnFormatBody.Visible = ui.ShowFormatButton;

                // 提示文案随形态变。这里直接写控件而不绑草稿的转发属性——
                // ui 本来就在手上，模型不必为一句界面文案多开一个属性。
                lblBodyHint.Text = ui.Hint;
            }
            finally
            {
                tblBodyType.ResumeLayout();
                pnlBodyHost.ResumeLayout();
            }
        }

        /// <summary>
        /// 授权四页显隐。四个 Panel 都 Dock=Fill 叠在 pnlAuthHost 里，同一时刻只让一个 Visible：
        /// Visible=false 的面板不参与布局计算，剩下那个自然填满。
        /// 🔴 走事件同步不走 Visible 绑定（实测绑定从不写入，见类头注释）。
        /// </summary>
        private void SyncAuthUi()
        {
            MesAuthType type = ViewModel.Editing.AuthType;
            bool matched = false;

            pnlAuthHost.SuspendLayout();
            try
            {
                foreach (var entry in authTypeMap)
                {
                    bool on = entry.Type == type;
                    entry.Panel.Visible = on;

                    if (on)
                        matched = true;
                }

                // 未登记的鉴权类型：四个面板全隐会让用户找不到任何可填的位置，比显示错更糟。
                // 退回「无鉴权」面板——配置侧 Normalize 也把非法类型归 None，两处规则一致。
                if (!matched)
                    pnlAuthNone.Visible = true;
            }
            finally
            {
                pnlAuthHost.ResumeLayout();
            }
        }

        /// <summary>结果区：状态条与报文框只读展示，单向绑 VM（源→控件，永不写回）。</summary>
        private void BindStatusArea()
        {
            txtStatusApi.DataBindings.Add(nameof(Control.Text), ViewModel, nameof(MesDebugViewModel.StatusApiName), true, DataSourceUpdateMode.Never);
            txtStatusCode.DataBindings.Add(nameof(Control.Text), ViewModel, nameof(MesDebugViewModel.StatusHttpCode), true, DataSourceUpdateMode.Never);
            txtStatusElapsed.DataBindings.Add(nameof(Control.Text), ViewModel, nameof(MesDebugViewModel.StatusElapsed), true, DataSourceUpdateMode.Never);
            txtStatusTime.DataBindings.Add(nameof(Control.Text), ViewModel, nameof(MesDebugViewModel.StatusTimeRange), true, DataSourceUpdateMode.Never);

            // Control.Text 的 setter 自带「值相同就跳过」的短路，不需要额外包一层手动赋值
            txtRequest.DataBindings.Add(nameof(Control.Text), ViewModel, nameof(MesDebugViewModel.RequestText), true, DataSourceUpdateMode.Never);
            txtResponse.DataBindings.Add(nameof(Control.Text), ViewModel, nameof(MesDebugViewModel.ResponseText), true, DataSourceUpdateMode.Never);
        }

        #endregion

        #region 通用绑定与属性变更

        /// <summary>
        /// 枚举下拉的填充 + 写回（cmbMethod / cmbAuthType / cmbAuthKeyIn 三处共用）。
        ///
        /// 🔴 用 Items + SelectedItem，不用 DataSource：显示文本就是枚举名，没有 DisplayMember 需求。
        ///    「DataSource 不可靠」这个结论是**错的**——FormProductQuery.cmbOperator 用 DataSource
        ///    正常工作。这里只是选了依赖更少的那条。
        ///
        /// 🔴 不靠下标映射值：SelectedItem 直接给/取枚举值本身。用「数组下标 ↔ Items 下标」那套，
        ///    将来往 Items 插一条「全部」之类的展示项就会静默串值——下标是位置，值才是语义。
        ///
        /// 🔴 用 SelectionChangeCommitted 而不是 SelectedIndexChanged：后者在程序化改选中也触发
        ///    （装载 / 换接口 / 投影都会），那一刻控件值未必稳定，写回会把 null 或旧值灌进模型
        ///    （实测症状：2026-10-29 的备份里 Method 被写成 0）。判来源，不判内容。
        ///    根因未隔离验证（候选：缺 DisplayMember / 用了 DataBindings / Array vs List），
        ///    当前实现同时绕开这三个变量——它是「排除了未知」，不是「证明了因果」。
        ///
        /// 已知边界：键盘方向键在展开列表里选择不写回。要支持就退回「SelectedIndexChanged + 标志位」
        /// （先例：FormProductQuery.fillingCombos、FormRoleManager.suppressCheck）。
        /// </summary>
        private void BindEnumCombo(UIComboBox combo, Type enumType, Action<object> writeBack)
        {
            combo.Items.Clear();
            foreach (object value in Enum.GetValues(enumType))
                combo.Items.Add(value);

            combo.SelectionChangeCommitted += (s, e) =>
            {
                object selected = combo.SelectedItem;
                if (selected != null)
                    writeBack(selected);
            };
        }

        /// <summary>
        /// 三个枚举下拉的「模型 → 控件」投影。
        ///
        /// 🔴 直接赋 SelectedItem（枚举值本身），不换算下标。
        ///    值不在 Items 里时 SelectedItem 会落成 null（下拉显示空），这是可见信号；
        ///    正常情况下不会发生——MesConfigStore.Normalize 已保证枚举值合法。
        /// </summary>
        private void SyncEnumCombos()
        {
            MesApiEditModel editing = ViewModel.Editing;
            if (editing == null)
                return;

            cmbMethod.SelectedItem = editing.Method;
            cmbAuthType.SelectedItem = editing.AuthType;
            cmbAuthKeyIn.SelectedItem = editing.AuthKeyIn;
        }

        /// <summary>
        /// 草稿属性变化 → 控件投影。值类字段不在这里（它们绑在草稿上，属性通知直达控件）；
        /// 这里只处理绑定替不了的三件事：面板显隐、枚举下拉的选中项。
        /// </summary>
        private void OnEditingPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MesApiEditModel.BodyType):
                    SyncBodyUi();
                    break;

                // 枚举下拉的选中项无法绑定（UIComboBox.SelectedItem 没有变更事件），装载时必须显式投影
                case nameof(MesApiEditModel.Method):
                case nameof(MesApiEditModel.AuthKeyIn):
                    SyncEnumCombos();
                    break;

                // 鉴权类型变化要同时切面板与下拉选中项
                case nameof(MesApiEditModel.AuthType):
                    SyncAuthUi();
                    SyncEnumCombos();
                    break;
            }
        }

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(MesDebugViewModel.SelectedApiName):
                    SyncApiSelection();
                    break;

                // 只听 IsReadOnly：会话态变化时它和 IsEditing / IsEditable / IsAdding 一起发出，一个就够。
                // 草稿是只读引用（永不换实例），所以这里没有「换对象」分支：
                // 装载引起的变化全部走草稿的 PropertyChanged，值字段由绑定直达控件。
                case nameof(MesDebugViewModel.IsReadOnly):
                    ApplyGridEditMode();
                    break;
            }
        }

        /// <summary>
        /// DGV 的编辑能力。输入控件的 Enabled / ReadOnly 由 BindStates 绑定，不在这里管。
        /// 只读规则见类头注释（逐列 ReadOnly，不设 DGV 级）。
        /// </summary>
        private void ApplyGridEditMode()
        {
            bool editable = !ViewModel.IsReadOnly;

            // 编辑态才允许增删行；浏览态连末行都不显示
            foreach (DataGridView grid in new[] { dgvQueryParams, dgvHeaders, dgvBodyForm })
            {
                grid.AllowUserToAddRows = editable;
                grid.AllowUserToDeleteRows = editable;
            }

            SetColumnsEditMode(queryEditColumns, editable);
            SetColumnsEditMode(headerEditColumns, editable);
            SetColumnsEditMode(bodyFormEditColumns, editable);
        }

        #endregion
    }
}
