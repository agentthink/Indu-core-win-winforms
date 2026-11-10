using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    public partial class FormRoleManager : UIPage
    {
        #region 依赖注入
        public RoleManagerViewModel ViewModel { get; set; }
        #endregion

        #region 权限树渲染状态
        /// <summary>
        /// 程序化写 TreeNode.Checked 的抑制标志：Checked 赋值会触发 AfterCheck(Unknown)，
        /// 置位期间事件处理直接返回，保证渲染/回写不落业务数据；用户点击（ByMouse）不受影响。
        /// </summary>
        private bool suppressCheck;
        #endregion

        public FormRoleManager()
        {
            InitializeComponent();

            // UITreeView 节点高度硬编码 28 物理像素，而节点文字按磅制 Font 绘制（DrawString 磅转像素随 DPI 翻倍，
            // 200% 屏下 12 磅 = 32px 物理 > 28px → 文字被行高裁切畸形）。按 DPI 放大行高。
            // 必须 handle 创建前设置（handle 已创建后设置触发 RecreateHandle），同 navMenu 结论。
            treePermissions.ItemHeight = UiDpi.Scale(28);

            // 关闭 SunnyUI 内建父子勾选联动（TreeNodeStateSync 默认 true）。反编译实证（SunnyUI v3.9.1）：
            // 其 OnDrawNode 无 ByMouse 门禁——每绘制一个子节点就按「子节点是否全勾」重算父节点 Checked，
            // 把「等级继承」的父勾直接抹掉（ToolTip 仍提示已继承，复选框却未勾）；联动改写还会把结果经
            // AfterCheck(Unknown) 当用户操作写进 VM（数据污染）。父子级联语义统一由 RoleManagerViewModel 负责。
            treePermissions.TreeNodeStateSync = false;
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var cp = base.CreateParams;
                cp.ExStyle |= 0x02000000;   // WS_EX_COMPOSITED：整棵控件树先组合再一次性显示，消除嵌套容器分层
                return cp;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UiDpi.SetRedraw(this, false);
            SuspendLayout();
            try
            {
                UiDpi.EnableDoubleBuffered(this);
                SetupDgv();
                BindViewModel();
                BridgeEvents();
            }
            finally
            {
                ResumeLayout(false);
            }

            // DPI 补偿必须在 ResumeLayout(false) 之后：
            // SuspendLayout 期间 ColumnHeadersHeightSizeMode 切换被抑制，紧接着的 ColumnHeadersHeight 会被当作 AutoSize 忽略
            ApplyDgvDpiCompensation();

            // 延迟一帧执行数据加载，避免与窗体首次绘制抢同一条消息循环
            BeginInvoke(new Action(() =>
            {
                ViewModel.LoadRolesCommand.Execute(null);
            }));
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

        #region 表格初始化

        private void SetupDgv()
        {
            dgvRoles.SuspendLayout();
            try
            {
                dgvRoles.AutoGenerateColumns = false;
                dgvRoles.ReadOnly = true;
                dgvRoles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                dgvRoles.MultiSelect = false;
                dgvRoles.AllowUserToAddRows = false;
                dgvRoles.AllowUserToDeleteRows = false;
                dgvRoles.AllowUserToResizeRows = false;
                dgvRoles.BackgroundColor = Color.White;
                dgvRoles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
                dgvRoles.ScrollBars = ScrollBars.Both;

                dgvRoles.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
                {
                    Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                    Alignment = DataGridViewContentAlignment.MiddleCenter,
                    BackColor = Color.FromArgb(0, 40, 170),
                    ForeColor = Color.White
                };
                dgvRoles.EnableHeadersVisualStyles = false;

                dgvRoles.Columns.Clear();
                AddTextColumn(nameof(RoleDto.RoleName), 120);
                AddTextColumn(nameof(RoleDto.Level), 80);
                AddTextColumn(nameof(RoleDto.IsBuiltIn), 60);

                dgvRoles.CellFormatting += OnDgvCellFormatting;
                dgvRoles.SelectionChanged += OnRoleSelectionChanged;
            }
            finally
            {
                dgvRoles.ResumeLayout();
            }
        }

        /// <summary>
        /// DGV DPI 补偿（三件套：字体/行高/列头），必须在 ResumeLayout(false) 之后调用
        /// </summary>
        private void ApplyDgvDpiCompensation()
        {
            // 系数统一 UiDpi.DpiScale（Program.cs 入口按 Graphics.DpiX 算好 GlobalFontScale，勿用 DeviceDpi——实测返回 96 不可靠）
            float dpiScale = UiDpi.DpiScale;

            // 控件 Font 保持 12F 物理：UIDataGridView 列头字体跟随控件 Font 且列头绘制走 GlobalFontScale，
            // 物理放大会双重放大（24F×200%=48F 视觉太大）；12F×200%=24F 视觉，与按钮一致
            dgvRoles.Font = new Font("微软雅黑", 12F);
            // 单元格字体物理放大（DataGridView 原生绘制不走 GlobalFontScale，12F×scale 才是 24F 视觉）
            // Designer 显式 Font（宋体）优先于控件 Font，必须一并重建
            dgvRoles.DefaultCellStyle.Font = new Font("微软雅黑", 12F * dpiScale);
            dgvRoles.RowsDefaultCellStyle.Font = new Font("微软雅黑", 12F * dpiScale);

            // 行高基线 44（100% 设计值）
            dgvRoles.RowTemplate.Height = (int)Math.Round(44 * dpiScale);
            // 列头高度：弃用 AutoSize（运行时改字体后不重算必切），按 DPI 手算
            dgvRoles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvRoles.ColumnHeadersHeight = (int)Math.Round(36 * dpiScale);

            dgvRoles.Invalidate();
        }

        private DataGridViewTextBoxColumn AddTextColumn(string dataProperty, int width,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var prop = typeof(RoleDto).GetProperty(dataProperty);
            var desc = prop?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>().FirstOrDefault();

            var col = new DataGridViewTextBoxColumn
            {
                Name = $"col{dataProperty}",
                HeaderText = desc?.Description ?? dataProperty,
                DataPropertyName = dataProperty,
                Width = width,
                DefaultCellStyle = { Alignment = align },
                ReadOnly = true
            };
            dgvRoles.Columns.Add(col);
            return col;
        }

        private void OnDgvCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.Value == null) return;

            if (dgvRoles.Columns[e.ColumnIndex].DataPropertyName == nameof(RoleDto.IsBuiltIn))
            {
                e.Value = (int)e.Value == 1 ? "是" : "否";
                e.FormattingApplied = true;
            }
        }

        #endregion

        #region 绑定与事件

        private void BindViewModel()
        {
            dgvRoles.DataSource = ViewModel.Roles;

            btnSave.DataBindings.Add("Enabled", ViewModel, nameof(RoleManagerViewModel.CanEditRole), true,
                DataSourceUpdateMode.OnPropertyChanged);
            btnDelete.DataBindings.Add("Enabled", ViewModel, nameof(RoleManagerViewModel.CanDeleteRole), true,
                DataSourceUpdateMode.OnPropertyChanged);

            lblCurrentUserRole.DataBindings.Add("Text", ViewModel, nameof(RoleManagerViewModel.CurrentUserRoleDisplay));
            lblStatus.DataBindings.Add("Text", ViewModel, nameof(RoleManagerViewModel.StatusText));

            ViewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private void BridgeEvents()
        {
            btnAdd.Click += (s, e) => ViewModel.AddRoleCommand.Execute(null);
            btnSave.Click += (s, e) =>
            {
                SyncEditPanelToViewModel();
                ViewModel.SaveRoleCommand.Execute(null);
            };
            btnReset.Click += (s, e) =>
            {
                ViewModel.ResetRolePermissionsCommand.Execute(null);
                RenderTree(); // VM 已在 ResetRolePermissions 内重建权限树，此处只重渲染
            };
            btnDelete.Click += (s, e) => ViewModel.DeleteRoleCommand.Execute(null);
            btnRefresh.Click += (s, e) => ViewModel.LoadRolesCommand.Execute(null);

            // BeforeCheck：拦截不参与手动授权的节点（纯展示容器/等级继承），状态不变、无闪烁；
            // 程序化 Checked 赋值不经过 BeforeCheck，渲染/回写天然不受拦截影响
            treePermissions.BeforeCheck += OnPermissionTreeBeforeCheck;
            treePermissions.AfterCheck += OnPermissionTreeAfterCheck;
            // 行号绘制
            dgvRoles.RowPostPaint += (s, e) => OnRowPostPaint(dgvRoles, e);
        }

        #endregion

        #region ViewModel 属性变更同步

        private void OnViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RoleManagerViewModel.SelectedRole))
            {
                SyncEditPanel(ViewModel.SelectedRole);
                // 选中角色（含置空）都重建权限树数据后重渲染；RenderTree 对空选中仅禁用树控件
                ViewModel.BuildPermissionTree(ViewModel.SelectedRole?.Id ?? 0);
                RenderTree();
            }
        }

        #endregion

        #region 角色选中变化

        private void OnRoleSelectionChanged(object sender, EventArgs e)
        {
            if (dgvRoles.SelectedRows.Count == 0 || dgvRoles.Rows.Count == 0)
            {
                ViewModel.SelectedRole = null;
                return;
            }

            var row = dgvRoles.SelectedRows[0];
            if (row.DataBoundItem is RoleDto role)
            {
                ViewModel.SelectedRole = role;
            }
        }

        #endregion

        #region 编辑区同步

        private void SyncEditPanel(RoleDto role)
        {
            if (role == null)
            {
                txtRoleName.Text = string.Empty;
                numLevel.Value = 100;
                chkIsBuiltIn.Checked = false;
                chkIsBuiltIn.Enabled = false;
                txtRemark.Text = string.Empty;
                txtRoleName.ReadOnly = false;
                numLevel.Enabled = true;
                return;
            }

            txtRoleName.Text = role.RoleName ?? string.Empty;
            numLevel.Value = role.Level;
            chkIsBuiltIn.Checked = role.IsBuiltIn == 1;
            chkIsBuiltIn.Enabled = false;
            txtRemark.Text = role.Remark ?? string.Empty;

            // 内置角色只读 RoleName / Level（Remark 可改）；与 chkIsBuiltIn.Enabled = false 同款处理
            bool builtIn = role.IsBuiltIn == 1;
            txtRoleName.ReadOnly = builtIn;
            numLevel.Enabled = !builtIn;
        }

        private void SyncEditPanelToViewModel()
        {
            if (ViewModel.SelectedRole == null) return;

            ViewModel.SelectedRole.RoleName = txtRoleName.Text.Trim();
            ViewModel.SelectedRole.Level = (int)numLevel.Value;
            ViewModel.SelectedRole.Remark = txtRemark.Text.Trim();
        }

        #endregion

        #region 权限树（父子展示分组 + 等级继承灰勾；勾选级联语义在 VM）

        /// <summary>
        /// 渲染权限树（唯一渲染入口）：suppressCheck 全程抑制，Checked 赋值不落业务数据；
        /// VM 数据重建（BuildPermissionTree）由调用方负责，本方法只消费 ViewModel.PermissionNodes
        /// </summary>
        private void RenderTree()
        {
            treePermissions.BeginUpdate();
            suppressCheck = true;
            try
            {
                treePermissions.Nodes.Clear();

                if (ViewModel.SelectedRole == null)
                {
                    treePermissions.Enabled = false;
                    return;
                }

                treePermissions.Enabled = true;
                RenderNodes(treePermissions.Nodes, ViewModel.PermissionNodes);
                treePermissions.ExpandAll();
            }
            finally
            {
                suppressCheck = false;
                treePermissions.EndUpdate();
            }
        }

        /// <summary>
        /// 递归渲染节点：纯渲染，不触发业务逻辑
        /// </summary>
        private void RenderNodes(TreeNodeCollection parent, List<PermissionNode> nodes)
        {
            foreach (var vmNode in nodes)
            {
                var uiNode = new TreeNode($"{vmNode.Name}  [{vmNode.Code}]")
                {
                    Name = vmNode.Id.ToString(),
                    Checked = vmNode.IsChecked,
                    ForeColor = vmNode.IsAutoInherited ? Color.Gray : Color.Black,
                    ToolTipText = vmNode.IsAutoInherited
                        ? "由角色等级自动继承（不可取消）"
                        : (vmNode.IsChecked ? "手动分配" : "无权限")
                };
                parent.Add(uiNode);
                RenderNodes(uiNode.Nodes, vmNode.Children);
            }
        }

        /// <summary>
        /// BeforeCheck：拦截不参与手动授权的节点。
        /// VM 中不存在的节点（如「未分类功能」容器）= 纯展示分组，不可操作；等级继承节点（灰勾）由角色
        /// 等级实时计算，手动改无效（ToolTip 已说明）。e.Cancel 后原生不翻转、不触发 AfterCheck；
        /// 程序化 Checked 赋值不经过本事件，渲染/回写不受影响。
        /// </summary>
        private void OnPermissionTreeBeforeCheck(object sender, TreeViewCancelEventArgs e)
        {
            PermissionNode vmNode = null;
            if (int.TryParse(e.Node?.Name, out int id))
                vmNode = ViewModel.FindPermissionNode(id);

            if (vmNode == null || vmNode.IsAutoInherited)
                e.Cancel = true;
        }

        /// <summary>
        /// AfterCheck：仅用户点击（ByMouse）可达——继承/容器节点已被 BeforeCheck 拦截，程序化赋值
        /// （Unknown）被 suppressCheck 拦截。父子级联全部在 VM 内完成后，按 VM 权威状态一次性回写 UI。
        /// </summary>
        private void OnPermissionTreeAfterCheck(object sender, TreeViewEventArgs e)
        {
            if (suppressCheck) return;
            if (!int.TryParse(e.Node?.Name, out int id)) return;

            var vmNode = ViewModel.FindPermissionNode(id);
            if (vmNode == null) return; // 防御：容器节点理论上已被 BeforeCheck 拦截

            ViewModel.OnNodeCheckedChanged(id, e.Node.Checked);
            SyncFromVm();
        }

        /// <summary>
        /// 按 VM 权威状态回写整树勾选态（勾/取消的父子级联都在 VM 内完成后一次性同步）；
        /// 抑制期间 Checked 赋值触发的 AfterCheck(Unknown) 直接返回
        /// </summary>
        private void SyncFromVm()
        {
            suppressCheck = true;
            try
            {
                ApplyChecked(treePermissions.Nodes);
            }
            finally
            {
                suppressCheck = false;
            }
        }

        /// <summary>
        /// 递归回写：UI 节点按 Name（=权限 Id）现取 VM 节点比对勾选态（同「长期持引用必须现取」原则，不挂对象引用）
        /// </summary>
        private void ApplyChecked(TreeNodeCollection uiNodes)
        {
            foreach (TreeNode uiNode in uiNodes)
            {
                if (int.TryParse(uiNode.Name, out int id))
                {
                    var vmNode = ViewModel.FindPermissionNode(id);
                    if (vmNode != null && uiNode.Checked != vmNode.IsChecked)
                        uiNode.Checked = vmNode.IsChecked; // AfterCheck(Unknown) 被 suppressCheck 拦截
                }
                ApplyChecked(uiNode.Nodes);
            }
        }
        #endregion

        #region 行号绘制
        /// <summary>
        /// 绘制行号，异常时静默处理，绝不抛异常避免搞崩 DataGridView 渲染管线
        /// </summary>
        private void OnRowPostPaint(DataGridView dgv, DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                using var brush = new SolidBrush(dgv.RowHeadersDefaultCellStyle.ForeColor);
                string lineNo = (e.RowIndex + 1).ToString();

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                // using 释放 StringFormat，避免每行绘制泄漏 GDI+ 对象（高频触发，跑几小时句柄耗尽）
                using var sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };

                e.Graphics.DrawString(lineNo, e.InheritedRowStyle.Font, brush,
                    new Rectangle(e.RowBounds.Location.X, e.RowBounds.Location.Y,
                        dgv.RowHeadersWidth, e.RowBounds.Height), sf);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加行号时发生错误: {ex.Message}");
            }
        }
        #endregion
    }
}