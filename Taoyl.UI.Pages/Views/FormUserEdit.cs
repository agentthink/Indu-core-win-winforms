using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    public partial class FormUserEdit : UIForm
    {
        #region 依赖注入

        private readonly UserEditViewModel viewModel;
        private readonly IMessageBoxService messageBoxService;

        #endregion

        public FormUserEdit(UserEditViewModel viewModel, IMessageBoxService messageBoxService)
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            this.viewModel = viewModel;
            this.messageBoxService = messageBoxService;

            ApplyDpiCompensation();
        }

        /// <summary>
        /// System aware 下 UIForm 弹窗不自动缩放（UIStyles.ZoomScale 默认 false；UIStyles.SetDPIScale 调用时机在窗体注册前，白调）：
        /// ClientSize/标题栏/布局 Margin 全是物理像素设计值，必须手动按 DPI 放大，否则 200% 屏下小窗 + 磅制文字物理翻倍 → 内容挤爆、标题被切
        /// </summary>
        private void ApplyDpiCompensation()
        {
            // 窗体与标题栏整体放大（UIForm 标题栏自绘，TitleHeight setter 不更新 Padding，
            // Dock=Fill 的内容区靠 Padding.Top 避让标题栏，必须手动同步）
            int titleHeight = UiDpi.Scale(35);
            ClientSize = new Size(UiDpi.Scale(350), UiDpi.Scale(500));
            TitleHeight = titleHeight;
            Padding = new Padding(0, titleHeight, 0, 0);

            // 头像：Margin 是物理像素不随 TLP 放大，200% 下单元格宽翻倍而 Margin 不变 → 头像横向压扁成椭圆，必须同步放大
            userAvatar.Margin = new Padding(UiDpi.Scale(120), UiDpi.Scale(3), UiDpi.Scale(120), UiDpi.Scale(3));
            // UIAvatar 实际绘制尺寸 = AvatarSize（默认 60 物理硬编码），与控件区域无关，图片按它重采样绘制；
            // 只放大控件区域而 AvatarSize 不放大 → 头像视觉反而缩小。必须按 DPI 放大
            userAvatar.AvatarSize = UiDpi.Scale(60);

            // 按钮区：Percent 行高随 ClientSize 线性放大，但 Margin 是物理像素不放大 → 按钮 Dock=Fill 相对字号偏高，
            // Margin 按 DPI 放大保持视觉比例（100% 下 48px 按钮 / 16px 字 = 3:1）
            var btnMargin = new Padding(UiDpi.Scale(22), UiDpi.Scale(18), UiDpi.Scale(22), UiDpi.Scale(18));
            btnConfirm.Margin = btnMargin;
            btnCancel.Margin = btnMargin;
        }

        #region 生命周期

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UiDpi.EnableDoubleBuffered(this);
            BridgeEvents();
            LoadDataToUI();
        }

        #endregion

        #region 绑定与事件

        private void BridgeEvents()
        {
            btnConfirm.Click += OnConfirmClick;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;
        }

        #endregion

        #region 数据加载与保存

        private void LoadDataToUI()
        {
            // 密码框统一隐藏
            txtPassword.PasswordChar = '*';

            if (viewModel.IsNewUser)
            {
                txtUsername.Text = string.Empty;
                txtPassword.Text = string.Empty;
                txtUserNumber.Text = string.Empty;
                txtCardCode.Text = string.Empty;
                this.Text = "新增用户";
                txtPassword.Watermark = "必填";
            }
            else
            {
                txtUsername.Text = viewModel.OriginalUser.Username;
                txtPassword.Text = string.Empty; // 编辑时绝不回显原密码
                txtUserNumber.Text = viewModel.OriginalUser.UserNumber ?? string.Empty;
                txtCardCode.Text = viewModel.OriginalUser.CardCode ?? string.Empty;

                this.Text = $"修改用户 - {viewModel.OriginalUser.Username}";
                txtPassword.Watermark = "留空则不修改密码";
            }

            // 加载角色多选树
            LoadRoleTree();

            // 焦点定位
            txtUsername.Focus();
        }

        /// <summary>
        /// 初始化 UIComboTreeView 角色树（平级节点，无层级关系）
        /// </summary>
        private void LoadRoleTree()
        {
            // 注意：cmbRoleLevel 在 Designer 中已改为 UIComboTreeView
            var tree = cmbRoleLevel.TreeView;
            tree.Nodes.Clear();
            tree.CheckBoxes = true;

            foreach (var role in viewModel.AvailableRoles)
            {
                var node = tree.Nodes.Add(role.RoleName);
                node.Tag = role.Id;
                node.Checked = viewModel.SelectedRoleIds.Contains(role.Id);
            }

            // 勾选变化时更新显示文本
            //tree.AfterCheck
            tree.AfterCheck += (s, e) =>
            {
                cmbRoleLevel.Text = GetSelectedRoleNames();
            };

            cmbRoleLevel.Text = GetSelectedRoleNames();
        }

        private string GetSelectedRoleNames()
        {
            var tree = cmbRoleLevel.TreeView;
            var selectedNames = new List<string>();
            foreach (TreeNode node in tree.Nodes)
            {
                if (node.Checked)
                    selectedNames.Add(node.Text);
            }
            return selectedNames.Count > 0
                ? string.Join(", ", selectedNames)
                : "请选择角色";
        }

        private List<int> GetSelectedRoleIds()
        {
            var tree = cmbRoleLevel.TreeView;
            var ids = new List<int>();
            foreach (TreeNode node in tree.Nodes)
            {
                if (node.Checked && node.Tag is int id)
                    ids.Add(id);
            }
            return ids;
        }

        private void OnConfirmClick(object sender, EventArgs e)
        {
            // 收集数据
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Text;
            string userNumber = txtUserNumber.Text.Trim();
            string cardCode = txtCardCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(username))
            {
                messageBoxService.ShowWarning("用户名不能为空，请输入用户名。");
                txtUsername.Focus();
                return;
            }

            // 新增时密码必填
            if (viewModel.IsNewUser && string.IsNullOrWhiteSpace(password))
            {
                messageBoxService.ShowWarning("新增用户时密码不能为空，请输入密码。");
                txtPassword.Focus();
                return;
            }

            // 收集选中的角色
            var roleIds = GetSelectedRoleIds();
            if (roleIds.Count == 0)
            {
                messageBoxService.ShowWarning("请至少选择一个角色");
                return;
            }

            // 构建 UserDto（Password 由密码框写入：新增必填，编辑留空 = 保留原密码）
            var user = new UserDto
            {
                UserId = viewModel.IsNewUser ? 0 : viewModel.OriginalUser.UserId,
                Username = username,
                UserNumber = userNumber,
                CardCode = cardCode,
                Password = password
            };

            // 调用 viewModel 保存（含角色分配）
            if (viewModel.SaveUser(user, roleIds, out string errorMsg))
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                messageBoxService.ShowError("保存失败: " + errorMsg);
            }
        }

        #endregion
    }
}