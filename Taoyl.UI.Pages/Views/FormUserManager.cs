using Autofac;
using Sunny.UI;
using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    public partial class FormUserManager : UIPage
    {
        #region 依赖注入
        public IComponentContext ComponentContext { get; set; }
        public UserManagerViewModel ViewModel { get; set; }
        public IMessageBoxService MessageBoxService { get; set; }
        #endregion

        #region 表格列对象

        private DataGridViewTextBoxColumn colUserId;
        private DataGridViewTextBoxColumn colUsername;
        private DataGridViewTextBoxColumn colUserNumber;
        private DataGridViewTextBoxColumn colCardCode;
        private DataGridViewTextBoxColumn colRoleLevel;
        private DataGridViewTextBoxColumn colRoleName;
        private DataGridViewTextBoxColumn colLastLoginTime;
        private DataGridViewTextBoxColumn colAddPeople;
        private DataGridViewTextBoxColumn colCreateTime;

        #endregion

        public FormUserManager()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
        }

        protected override void OnLoad(EventArgs e)
        {
            ValidateInjection();
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);
            UiDpi.EnableDoubleBuffered(this);
            SetupDgv();
            BindViewModel();
            BridgeEvents();
            ViewModel.LoadUsersCommand.Execute(null);
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

        /// <summary>
        /// 每次显示时刷新数据（支持单例切换后数据更新）
        /// </summary>
        protected override void OnVisibleChanged(EventArgs e)
        {
            base.OnVisibleChanged(e);
            if (this.Visible && ViewModel != null)
            {
                ViewModel.LoadUsersCommand.Execute(null);
            }
        }

        /// <summary>
        /// 校验 ViewModel 注入，失败即抛异常阻止继续加载
        /// </summary>
        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("ViewModel 未注入，请检查 Autofac 配置");
            if (MessageBoxService == null)
                throw new InvalidOperationException("MessageBoxService 未注入，请检查 Autofac 配置");
        }

        #region 表格初始化

        private void SetupDgv()
        {
            dgvUsers.AutoGenerateColumns = false;
            dgvUsers.ReadOnly = true;
            dgvUsers.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvUsers.MultiSelect = false;
            dgvUsers.AllowUserToAddRows = false;
            dgvUsers.AllowUserToDeleteRows = false;
            dgvUsers.AllowUserToResizeRows = false;
            dgvUsers.RowTemplate.Height = UiDpi.Scale(44);
            dgvUsers.BackgroundColor = Color.White;
            dgvUsers.Font = new Font("微软雅黑", 12F);
            dgvUsers.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvUsers.ScrollBars = ScrollBars.Both;

            // DPI 三件套（System aware 下显式 Font/尺寸不随 DPI 缩放，需手动补偿）：
            // 单元格原生绘制物理放大；列头（UIDataGridView）走 GlobalFontScale 保持 12F；行高/列头高度按系数
            float dpiScale = UiDpi.DpiScale;
            dgvUsers.DefaultCellStyle.Font = new Font("微软雅黑", 12F * dpiScale);
            dgvUsers.RowsDefaultCellStyle.Font = new Font("微软雅黑", 12F * dpiScale);

            // 列头样式保留，数据行样式由 SunnyUI 主题与 CellFormatting 共同接管
            dgvUsers.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("微软雅黑", 12F, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 40, 170),
                ForeColor = Color.White
            };
            dgvUsers.EnableHeadersVisualStyles = false;

            dgvUsers.Columns.Clear();
            colUserId = AddTextColumn(nameof(UserDto.UserId), 20, DataGridViewContentAlignment.MiddleLeft);
            colUsername = AddTextColumn(nameof(UserDto.Username), 60);
            colUserNumber = AddTextColumn(nameof(UserDto.UserNumber), 60);
            colCardCode = AddTextColumn(nameof(UserDto.CardCode), 60);
            colRoleLevel = AddTextColumn(nameof(UserDto.MaxRoleLevel), 20);
            colRoleName = AddTextColumn(nameof(UserDto.RoleNamesDisplay), 60);
            colLastLoginTime = AddTextColumn(nameof(UserDto.LastLoginTime), 100);
            colAddPeople = AddTextColumn(nameof(UserDto.AddPeople), 60);
            colCreateTime = AddTextColumn(nameof(UserDto.CreateTime), 100);

            // 列头高度：弃用 AutoSize（运行时按 DPI 改字体后不重算必切），EnableResizing + 按 DPI 手算
            dgvUsers.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgvUsers.ColumnHeadersHeight = UiDpi.Scale(36);

            // 启用列头排序：colUserId自定义自然排序，其余禁用
            foreach (DataGridViewColumn col in dgvUsers.Columns)
            {
                if (col.Name == colUserId.Name)
                {
                    col.SortMode = DataGridViewColumnSortMode.Programmatic;
                }
                else
                {
                    col.SortMode = DataGridViewColumnSortMode.NotSortable;
                }
            }

            dgvUsers.SelectionChanged += OnSelectionChanged;
        }

        private DataGridViewTextBoxColumn AddTextColumn(string dataProperty, int width,
            DataGridViewContentAlignment align = DataGridViewContentAlignment.MiddleCenter)
        {
            var prop = typeof(UserDto).GetProperty(dataProperty);
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
            dgvUsers.Columns.Add(col);
            return col;
        }

        #endregion

        #region 行选中变化

        private void OnSelectionChanged(object sender, EventArgs e)
        {
            if (dgvUsers.SelectedRows.Count == 0 || dgvUsers.Rows.Count == 0)
            {
                ViewModel.SelectedUser = null;
                return;
            }

            var row = dgvUsers.SelectedRows[0];
            ViewModel.SelectedUser = row.DataBoundItem as UserDto;
            // CanEditUser / CanDeleteUser 由 SelectedUser 的 setter 自动联动
        }

        #endregion

        #region 绑定与事件

        private void BindViewModel()
        {
            // 数据源绑定：BindingList 与 DataGridView 原生配合最佳
            dgvUsers.DataSource = ViewModel.Users;

            // 按钮权限绑定：完全由 VM 的 CanEditUser / CanDeleteUser 驱动，零手动更新
            btnEdit.DataBindings.Add("Enabled", ViewModel, nameof(UserManagerViewModel.CanEditUser), true,
                DataSourceUpdateMode.OnPropertyChanged);
            btnDelete.DataBindings.Add("Enabled", ViewModel, nameof(UserManagerViewModel.CanDeleteUser), true,
                DataSourceUpdateMode.OnPropertyChanged);

        }

        private void BridgeEvents()
        {
            // 按钮点击 → 命令
            btnAdd.Click += (s, e) => ViewModel.AddUserCommand.Execute(null);
            btnEdit.Click += (s, e) => ViewModel.EditUserCommand.Execute(null);
            btnDelete.Click += (s, e) => ViewModel.DeleteUserCommand.Execute(null);

            // 导入导出：View 负责弹窗选文件，拿到路径后传给 VM 命令执行
            btnImport.Click += (s, e) =>
            {
                using (var dlg = new OpenFileDialog())
                {
                    dlg.Title = "选择导入文件";
                    dlg.Filter = "JSON文件|*.json|CSV文件|*.csv|所有文件|*.*";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        ViewModel.ImportUsersCommand.Execute(dlg.FileName);
                    }
                }
            };

            btnExport.Click += (s, e) =>
            {
                using (var dlg = new SaveFileDialog())
                {
                    dlg.Title = "导出用户数据";
                    dlg.Filter = "JSON文件|*.json";
                    dlg.FileName = $"Users_{DateTime.Now:yyyyMMdd_HHmmss}.json";
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        ViewModel.ExportUsersCommand.Execute(dlg.FileName);
                    }
                }
            };

            // 双击编辑
            dgvUsers.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    ViewModel.EditUserCommand.Execute(null);
                }
            };

            // VM 委托：View 层实现对话框创建，VM 零耦合
            ViewModel.ShowEditDialog = user =>
            {
                var vm = ComponentContext.Resolve<UserEditViewModel>();
                vm.Initialize(user, user == null);

                using (var editForm = new FormUserEdit(vm, MessageBoxService))
                {
                    return editForm.ShowDialog() == DialogResult.OK;
                }
            };
        }

        #endregion
    }
}