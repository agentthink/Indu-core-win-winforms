using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 多语言资源配置页面
    ///
    /// 布局设计：roottableLayoutPanel(顶部操作区/下方内容区) + downtableLayoutPanel(左树/右Tab+双网格)
    /// 本页只保留 View 职责：控件事件转发到 VM、TreeView 挂载与选中、DGV 绑定与行号绘制、文件对话框。
    /// 业务逻辑（树构建/合并/导入导出/提取/保存）全部在 ResourceConfigViewModel。
    /// </summary>
    public partial class FrmResourceConfig : UIPage
    {
        #region 依赖注入

        /// <summary>
        /// 资源配置视图模型，由 Autofac 属性注入
        /// </summary>
        public ResourceConfigViewModel ViewModel { get; set; }

        /// <summary>
        /// 消息框服务（Autofac 属性注入；View 层弹窗统一走服务，与 VM 一致）
        /// </summary>
        public IMessageBoxService MessageBoxService { get; set; }

        #endregion

        #region DPI 字体（缓存复用，OnFormClosing 释放）

        /// <summary>DGV 缩放系数（System aware 下按物理像素布局，需手动补偿）</summary>
        private float dgvDpiScale = 1F;

        private Font cellFont;       // 单元格字体（原生绘制，物理放大 12F × scale）
        private Font headerFont;     // 列头字体（SunnyUI 绘制走 GlobalFontScale，保持 12F 物理）
        private Font controlFont;    // 控件 Font（列头基准字体，保持 12F 物理）

        /// <summary>网格重绑防抖标记（同一帧多次表属性变化合并为一次重绑）</summary>
        private bool gridRefreshPending;

        /// <summary>
        /// 程序化选择标志：树重建恢复选中/初始化自动选中时置 true，
        /// 抑制 BeforeSelect 的未保存确认弹窗（仅用户鼠标点击切换类节点才弹）
        /// </summary>
        private bool suppressSelectCheck;

        #endregion

        public FrmResourceConfig()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            DpiZoomControl();
        }

        private void DpiZoomControl()
        {
            // UITreeView 节点高度硬编码 28 物理像素，高 DPI 下文字被裁切；必须 handle 创建前按 DPI 放大（同 FormRoleManager）
            menuTreeView.ItemHeight = UiDpi.Scale(28);
            resouceTabControl.ItemSize = new Size(UiDpi.Scale(150), UiDpi.Scale(40));
        }

        #region 生命周期

        protected override async void OnLoad(EventArgs e)
        {
            ValidateInjection();
            base.OnLoad(e);

            SetupDgvs();
            BindEvents();
            BindData();

            // 初始化：后台读 Menu.json + 扫描目录 → 构建树 → 挂载 → 选中第一个有效节点
            await ViewModel.InitializeAsync();
            RebindTree();
            SelectFirstValidNode();
        }
        
        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            cellFont?.Dispose();
            headerFont?.Dispose();
            controlFont?.Dispose();
            cellFont = null;
            headerFont = null;
            controlFont = null;
            base.OnFormClosing(e);
        }

        #endregion

        #region DGV 初始化（DPI 三件套）

        private void SetupDgvs()
        {
            dgvDpiScale = UiDpi.DpiScale;

            // 字体按 FormProcessMonitor 惯例：单元格原生绘制物理放大，列头走 GlobalFontScale 保持 12F
            cellFont = new Font("微软雅黑", 12F * dgvDpiScale);
            headerFont = new Font("微软雅黑", 12F, FontStyle.Bold);
            controlFont = new Font("微软雅黑", 12F);

            // 列结构由数据驱动自动生成（固定属性列 + 语言列由 ResourceMapper.ToDataTable 输出）
            dgv_UI.AutoGenerateColumns = true;
            dgv_Text.AutoGenerateColumns = true;

            // DPI 三件套 + 列头样式 + 行号统一走 UiDgvHelper（FormMain.Helpers）
            UiDgvHelper.Setup(dgv_UI, cellFont, headerFont, controlFont);
            UiDgvHelper.Setup(dgv_Text, cellFont, headerFont, controlFont);
        }

        #endregion

        #region 事件绑定

        private void BindEvents()
        {
            // 加载挂在 AfterSelect：SelectedNode 真正切换完成后才触发（e.Node == SelectedNode 必然成立）。
            // 不用 NodeMouseClick + e.Node != SelectedNode 防呆——BeforeSelect 弹模态确认框会打乱
            // NodeMouseClick 与 SelectedNode 切换的时序，导致确认后防呆误伤、点击无反应（第十八轮回归）
            menuTreeView.BeforeSelect += TreeView_BeforeSelect;
            menuTreeView.AfterSelect += TreeView_AfterSelect;

            btn_save.Click += (s, e) => ViewModel.SaveCommand.Execute(null);
            btn_search.Click += (s, e) => ViewModel.SearchCommand.Execute(null);
            btn_export.Click += Btn_export_Click;
            btn_loadIn.Click += Btn_loadIn_Click;
            btn_getUiRes.Click += (s, e) => _ = ViewModel.ExtractUiCommand.ExecuteAsync(null);
            btn_getTextRes.Click += (s, e) => new FrmResourceScanner().ShowDialog();

            resouceTabControl.SelectedIndexChanged += (s, e) => RefreshGridBinding();

            // 网格编辑（用户改单元格）置位未保存标记：IsModified 仅靠提取/保存更新，
            // 不监听编辑则切类确认框只在提取后生效一次；走 VM.MarkCurrentClassDirty 同步 classDirtyMap
            dgv_UI.CellValueChanged += (s, e) => ViewModel.MarkCurrentClassDirty();
            dgv_Text.CellValueChanged += (s, e) => ViewModel.MarkCurrentClassDirty();

            ViewModel.TreeStructureChanged += OnTreeStructureChanged;
            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        /// <summary>
        /// 文本框与 ViewModel 显式同步（SunnyUI UITextBox 数据绑定不可靠，用 TextChanged/PropertyChanged 双向同步）
        /// </summary>
        private void BindData()
        {
            txt_moduleName.TextChanged += (s, e) => ViewModel.ModuleName = txt_moduleName.Text;
            txt_className.TextChanged += (s, e) => ViewModel.ClassName = txt_className.Text;
            txt_resourceName.TextChanged += (s, e) => ViewModel.SearchKeyword = txt_resourceName.Text;
        }

        private void OnViewModelPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(ResourceConfigViewModel.ModuleName):
                    if (txt_moduleName.Text != ViewModel.ModuleName)
                        txt_moduleName.Text = ViewModel.ModuleName;
                    break;

                case nameof(ResourceConfigViewModel.ClassName):
                    if (txt_className.Text != ViewModel.ClassName)
                        txt_className.Text = ViewModel.ClassName;
                    break;

                // 注：IsModified 无需 View 响应——UIPage 无标题栏，标题加 * 不显示（已移除）；
                // 未保存提醒由切类确认框承担（TreeView 点击时读 IsModified）

                case nameof(ResourceConfigViewModel.UiGridTable):
                case nameof(ResourceConfigViewModel.TextGridTable):
                    // 资源表重建（提取/加载/导入）后自动刷新当前网格；
                    // 同一帧多次触发（SelectClassAsync 同时更新两个表）合并为一次重绑
                    if (!gridRefreshPending)
                    {
                        gridRefreshPending = true;
                        BeginInvoke(new Action(() =>
                        {
                            gridRefreshPending = false;
                            // 窗体可能已关闭（防抖回调排队期间），句柄销毁后访问 DGV 会抛异常
                            if (IsDisposed || !IsHandleCreated) return;
                            RefreshGridBinding();
                        }));
                    }
                    break;
            }
        }

        #endregion

        #region 树操作

        /// <summary>
        /// 树结构变更（导入/运行时新增类）后重挂整树并恢复选中
        /// 事件可能从后台线程触发，先切回 UI 线程
        /// </summary>
        private void OnTreeStructureChanged(object sender, EventArgs e)
        {
            if (menuTreeView.InvokeRequired)
            {
                menuTreeView.BeginInvoke(new Action(RebindTree));
                return;
            }
            RebindTree();
        }

        private void RebindTree()
        {
            string selectedKey = menuTreeView.SelectedNode?.Name;

            menuTreeView.BeginUpdate();
            try
            {
                menuTreeView.Nodes.Clear();
                RenderNodes(menuTreeView.Nodes, ViewModel.RootNodes);
                // 树默认展开所有节点（类资源树层级浅，展开便于直接点选）
                menuTreeView.ExpandAll();
            }
            finally
            {
                menuTreeView.EndUpdate();
            }

            if (!string.IsNullOrEmpty(selectedKey))
            {
                var node = FindNodeByName(menuTreeView.Nodes, selectedKey);
                if (node != null)
                {
                    // 程序化恢复选中：抑制 BeforeSelect 未保存确认（避免树重建恢复选中时误弹）
                    suppressSelectCheck = true;
                    try
                    {
                        menuTreeView.SelectedNode = node;
                        node.EnsureVisible();
                    }
                    finally
                    {
                        suppressSelectCheck = false;
                    }
                }
            }
        }

        /// <summary>
        /// 递归渲染节点：VM 数据模型 → TreeNode（参照 FormRoleManager.RenderNodes，纯渲染不触发业务逻辑）
        /// 颜色区分来源：配置=黑色，扫描=蓝色
        /// </summary>
        private static void RenderNodes(TreeNodeCollection parent, IEnumerable<ResourceTreeNode> nodes)
        {
            foreach (var vmNode in nodes)
            {
                var uiNode = new TreeNode(vmNode.DisplayName)
                {
                    Name = vmNode.Key,
                    Tag = vmNode,
                    ForeColor = vmNode.FromConfig ? Color.Black : Color.Blue
                };
                parent.Add(uiNode);
                RenderNodes(uiNode.Nodes, vmNode.Children);
            }
        }

        private static TreeNode FindNodeByName(TreeNodeCollection nodes, string name)
        {
            foreach (TreeNode node in nodes)
            {
                if (node.Name == name) return node;
                if (node.Nodes.Count > 0)
                {
                    var found = FindNodeByName(node.Nodes, name);
                    if (found != null) return found;
                }
            }
            return null;
        }

        /// <summary>
        /// 节点选中完成后加载该类资源（AfterSelect 在 SelectedNode 切换后触发，
        /// 取消场景 SelectedNode 不变不触发，天然与树高亮一致）。
        /// 程序化选择（树重建恢复选中/初始化选中）经 suppressSelectCheck 抑制——此处不加载，
        /// 初始化由 SelectFirstValidNode 手动加载，RebindTree 恢复选中不重载（保护网格未保存编辑）。
        /// </summary>
        private async void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (suppressSelectCheck) return;

            if (e.Node?.Tag is ResourceTreeNode item && item.IsClassNode)
                await LoadClassNode(item);
        }

        /// <summary>加载指定类资源到网格（禁用网格 + 等待光标 + SelectClassAsync）</summary>
        private async Task LoadClassNode(ResourceTreeNode item)
        {
            // 显示加载状态（禁用网格，避免用户误操作）
            dgv_UI.Enabled = false;
            dgv_Text.Enabled = false;
            Cursor = Cursors.WaitCursor;

            try
            {
                await ViewModel.SelectClassAsync(item.Namespace, item.ClassName);
            }
            finally
            {
                // 网格数据由 UiGridTable/TextGridTable 属性变化自动刷新（防抖合并），这里只恢复 UI 状态
                dgv_UI.Enabled = true;
                dgv_Text.Enabled = true;
                Cursor = Cursors.Default;
            }
        }

        /// <summary>
        /// 选择发生前拦截：当前类有未保存修改且用户点击其他类节点时弹确认，
        /// 取消则阻止选中切换（树高亮不变、网格不变，状态一致）。
        /// 确认则丢弃当前类的未保存修改（清 dirty 标记，切回不再反复提示；
        /// 提取/导入的缓存条目本身仍在，切回仍可见可保存）。
        /// 程序化选择（树重建恢复选中/初始化自动选中）经 suppressSelectCheck 跳过
        /// </summary>
        private void TreeView_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            if (suppressSelectCheck) return;

            if (e.Node?.Tag is ResourceTreeNode item && item.IsClassNode && ViewModel.IsModified)
            {
                // 消息服务不可用时放行（不弹窗不拦截不清理），避免"点不动"的静默卡死
                if (MessageBoxService == null) return;

                if (MessageBoxService.ShowAsk("当前类有未保存的修改，切换后修改将丢失，是否继续？"))
                    ViewModel.DiscardCurrentClassEdits();  // 确认丢弃：清 dirty
                else
                    e.Cancel = true;
            }
        }

        private async void SelectFirstValidNode()
        {
            var firstValid = FindFirstValidNode(ViewModel.RootNodes);
            if (firstValid != null)
            {
                var uiNode = FindNodeByName(menuTreeView.Nodes, firstValid.Key);
                if (uiNode != null)
                {
                    // 初始化自动选中：抑制 BeforeSelect 未保存确认与 AfterSelect 加载（此时本就不该弹）
                    suppressSelectCheck = true;
                    try
                    {
                        menuTreeView.SelectedNode = uiNode;
                    }
                    finally
                    {
                        suppressSelectCheck = false;
                    }
                    // AfterSelect 被抑制，这里手动加载第一个有效节点
                    await LoadClassNode(firstValid);
                }
            }
        }

        private static ResourceTreeNode FindFirstValidNode(IEnumerable<ResourceTreeNode> nodes)
        {
            foreach (var node in nodes)
            {
                if (node.IsClassNode)
                    return node;
                if (node.Children.Count > 0)
                {
                    var result = FindFirstValidNode(node.Children);
                    if (result != null) return result;
                }
            }
            return null;
        }

        #endregion

        #region 网格刷新

        /// <summary>
        /// 按当前 Tab 绑定对应 DataTable（切换 Tab 或加载类后调用）
        /// </summary>
        private void RefreshGridBinding()
        {
            var type = resouceTabControl.SelectedTab == tp_UI ? ResourceType.UI : ResourceType.Text;
            ViewModel.SetActiveGrid(type);

            var dgv = type == ResourceType.UI ? dgv_UI : dgv_Text;
            var table = ViewModel.GetGridTable(type);

            // 暂停布局 + 关闭自动列宽计算，大幅提升绑定速度
            dgv.SuspendLayout();
            var originalAutoSizeMode = dgv.AutoSizeColumnsMode;
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            dgv.DataSource = null;
            dgv.DataSource = table;

            // 归属列只读：Namespace/ClassName 决定资源文件路径，防呆不允许编辑（保存时会强制归属当前类）
            if (dgv.Columns.Contains("命名空间"))
                dgv.Columns["命名空间"].ReadOnly = true;
            if (dgv.Columns.Contains("类名"))
                dgv.Columns["类名"].ReadOnly = true;

            dgv.AutoSizeColumnsMode = originalAutoSizeMode;
            dgv.ResumeLayout();
        }

        #endregion

        #region 文件对话框

        private void Btn_export_Click(object sender, EventArgs e)
        {
            if (!ViewModel.HasDataToExport())
            {
                // 弹窗统一走注入的 IMessageBoxService（与 VM 一致，不用 SunnyUI UIMessageTip）
                MessageBoxService?.ShowWarning("没有可导出的数据");
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "Excel文件|*.xlsx";
                sfd.FileName = $"{ViewModel.ModuleName}_{ViewModel.ClassName}_{ViewModel.ActiveGridType}.xlsx";

                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    ViewModel.ExportCommand.Execute(sfd.FileName);
                }
            }
        }

        private void Btn_loadIn_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "Excel文件|*.xlsx";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    _ = ViewModel.ImportCommand.ExecuteAsync(ofd.FileName);
                }
            }
        }

        #endregion
    }
}
