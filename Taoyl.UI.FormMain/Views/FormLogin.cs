using Sunny.UI;
using System;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.FormMain.ViewModels;

namespace Taoyl.UI.FormMain
{
    /// <summary>
    /// 登录对话框
    /// 负责登录界面渲染和ViewModel绑定映射
    /// </summary>
    public partial class FormLogin : UIForm
    {
        #region 依赖注入
        public LoginViewModel ViewModel { get; set; }
        #endregion

        #region 构造函数
        public FormLogin()
        {
            InitializeComponent();
            ApplyDpiCompensation();
        }
        #endregion

        /// <summary>
        /// System aware 下 UIForm 弹窗不自动缩放（ZoomScale 默认 false / SetDPIScale 时机太早白调）：
        /// 窗体尺寸与标题栏都是物理像素设计值，必须手动按 DPI 放大（同 FormUserEdit 范式）
        /// </summary>
        private void ApplyDpiCompensation()
        {
            int titleHeight = UiDpi.Scale(35);
            Size = new Size(UiDpi.Scale(400), UiDpi.Scale(300));
            TitleHeight = titleHeight;
            // TitleHeight setter 不更新 Padding，Dock=Fill 的内容区靠 Padding.Top 避让标题栏，必须手动同步
            Padding = new Padding(0, titleHeight, 0, 0);
        }

        #region 窗体生命周期
        protected override void OnLoad(EventArgs e)
        {
            if (ViewModel == null)
                throw new InvalidOperationException("LoginViewModel未注入，请检查Autofac配置");

            base.OnLoad(e);
            UiDpi.EnableDoubleBuffered(this);
            InitializeUI();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            ViewModel.LoginCompleted -= OnLoginCompleted;
            ViewModel.Dispose();
            base.OnFormClosing(e);
        }
        #endregion

        #region 初始化
        private void InitializeComponent()
        {
            this.rootTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.uiLabel1 = new Sunny.UI.UILabel();
            this.txtUsername = new Sunny.UI.UITextBox();
            this.uiLabel2 = new Sunny.UI.UILabel();
            this.txtPassword = new Sunny.UI.UITextBox();
            this.downTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnLogin = new Sunny.UI.UIButton();
            this.btnCancel = new Sunny.UI.UIButton();
            this.rootTableLayoutPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.downTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootTableLayoutPanel.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.downTableLayoutPanel, 0, 1);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 35);
            this.rootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 2;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(400, 265);
            this.rootTableLayoutPanel.TabIndex = 0;
            this.rootTableLayoutPanel.TagString = null;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 2;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.topTableLayoutPanel.Controls.Add(this.txtPassword, 1, 1);
            this.topTableLayoutPanel.Controls.Add(this.uiLabel2, 0, 1);
            this.topTableLayoutPanel.Controls.Add(this.txtUsername, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.uiLabel1, 0, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 2;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(400, 159);
            this.topTableLayoutPanel.TabIndex = 0;
            this.topTableLayoutPanel.TagString = null;
            // 
            // uiLabel1
            // 
            this.uiLabel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLabel1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel1.Location = new System.Drawing.Point(3, 3);
            this.uiLabel1.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.uiLabel1.Name = "uiLabel1";
            this.uiLabel1.Size = new System.Drawing.Size(137, 73);
            this.uiLabel1.TabIndex = 1;
            this.uiLabel1.Text = "用户名:";
            this.uiLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtUsername
            // 
            this.txtUsername.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUsername.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtUsername.Location = new System.Drawing.Point(163, 20);
            this.txtUsername.Margin = new System.Windows.Forms.Padding(3, 20, 20, 20);
            this.txtUsername.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Padding = new System.Windows.Forms.Padding(5);
            this.txtUsername.ShowText = false;
            this.txtUsername.Size = new System.Drawing.Size(217, 39);
            this.txtUsername.TabIndex = 3;
            this.txtUsername.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtUsername.Watermark = "";
            // 
            // uiLabel2
            // 
            this.uiLabel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiLabel2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiLabel2.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLabel2.Location = new System.Drawing.Point(3, 82);
            this.uiLabel2.Margin = new System.Windows.Forms.Padding(3, 3, 20, 3);
            this.uiLabel2.Name = "uiLabel2";
            this.uiLabel2.Size = new System.Drawing.Size(137, 74);
            this.uiLabel2.TabIndex = 4;
            this.uiLabel2.Text = "密码:";
            this.uiLabel2.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtPassword
            // 
            this.txtPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtPassword.Location = new System.Drawing.Point(163, 99);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(3, 20, 20, 20);
            this.txtPassword.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Padding = new System.Windows.Forms.Padding(5);
            this.txtPassword.ShowText = false;
            this.txtPassword.Size = new System.Drawing.Size(217, 40);
            this.txtPassword.TabIndex = 5;
            this.txtPassword.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtPassword.Watermark = "";
            // 
            // downTableLayoutPanel
            // 
            this.downTableLayoutPanel.ColumnCount = 2;
            this.downTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.downTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.downTableLayoutPanel.Controls.Add(this.btnLogin, 0, 0);
            this.downTableLayoutPanel.Controls.Add(this.btnCancel, 1, 0);
            this.downTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downTableLayoutPanel.Location = new System.Drawing.Point(0, 159);
            this.downTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.downTableLayoutPanel.Name = "downTableLayoutPanel";
            this.downTableLayoutPanel.RowCount = 1;
            this.downTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.downTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.downTableLayoutPanel.Size = new System.Drawing.Size(400, 106);
            this.downTableLayoutPanel.TabIndex = 1;
            this.downTableLayoutPanel.TagString = null;
            // 
            // btnLogin
            // 
            this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLogin.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnLogin.Location = new System.Drawing.Point(40, 30);
            this.btnLogin.Margin = new System.Windows.Forms.Padding(40, 30, 40, 30);
            this.btnLogin.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.Size = new System.Drawing.Size(120, 46);
            this.btnLogin.TabIndex = 6;
            this.btnLogin.Text = "登录";
            this.btnLogin.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnCancel
            // 
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnCancel.Location = new System.Drawing.Point(240, 30);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(40, 30, 40, 30);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(120, 46);
            this.btnCancel.TabIndex = 7;
            this.btnCancel.Text = "取消";
            this.btnCancel.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // FormLogin
            // 
            this.ClientSize = new System.Drawing.Size(400, 300);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormLogin";
            this.Style = Sunny.UI.UIStyle.Custom;
            this.Text = "用户登录";
            this.TitleColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 400, 300);
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.downTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        /// <summary>
        /// 基于 Designer 布局绑定功能：密码掩码、登录/取消/回车事件、ViewModel 生命周期、初始焦点
        /// </summary>
        private void InitializeUI()
        {
            // 密码框统一隐藏（Designer 未设 PasswordChar）
            txtPassword.PasswordChar = '*';


            btnLogin.Click += (s, e) => DoLogin();
            btnCancel.Click += (s, e) =>
            {
                txtUsername.Text = string.Empty;
                txtPassword.Text = string.Empty;
                ViewModel.CancelCommand.Execute(null);
            };
            txtPassword.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Enter)
                {
                    DoLogin();
                    e.Handled = true;
                }
            };

            ViewModel.LoginCompleted += OnLoginCompleted;
            ViewModel.LoginCancel += OnLoginLoginCancel;

            txtUsername.Focus();
        }

        /// <summary>
        /// 收集输入并触发登录（按钮点击与回车共用）
        /// </summary>
        private void DoLogin()
        {
            ViewModel.UserName = txtUsername.Text.Trim();
            ViewModel.Password = txtPassword.Text;
            ViewModel.LoginCommand.Execute(null);
        }
        #endregion

        #region 事件处理
        private void OnLoginCompleted(bool success)
        {
            if (success)
            {
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show(this, "用户名或密码错误", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                txtPassword.SelectAll();
                txtPassword.Focus();
            }
        }

        private void OnLoginLoginCancel(bool cancel)
        {
            if (cancel)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }
        #endregion

        #region 公共方法
        public string GetUsername() => txtUsername.Text.Trim();
        public string GetPassword() => txtPassword.Text;
        #endregion
    }
}