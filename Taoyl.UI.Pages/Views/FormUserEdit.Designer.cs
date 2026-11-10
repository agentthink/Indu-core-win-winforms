namespace Taoyl.UI.Pages
{
    partial class FormUserEdit
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.userAvatar = new Sunny.UI.UIAvatar();
            this.lblDesc = new Sunny.UI.UILabel();
            this.midTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblUsername = new Sunny.UI.UILabel();
            this.lblRoleLevel = new Sunny.UI.UILabel();
            this.lblCardCode = new Sunny.UI.UILabel();
            this.lblUserNumber = new Sunny.UI.UILabel();
            this.lblPassword = new Sunny.UI.UILabel();
            this.txtUsername = new Sunny.UI.UITextBox();
            this.txtPassword = new Sunny.UI.UITextBox();
            this.txtUserNumber = new Sunny.UI.UITextBox();
            this.txtCardCode = new Sunny.UI.UITextBox();
            this.cmbRoleLevel = new Sunny.UI.UIComboTreeView();
            this.downTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnCancel = new Sunny.UI.UISymbolButton();
            this.btnConfirm = new Sunny.UI.UISymbolButton();
            this.mainTableLayoutPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.midTableLayoutPanel.SuspendLayout();
            this.downTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 1;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.midTableLayoutPanel, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.downTableLayoutPanel, 0, 2);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 35);
            this.mainTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 3;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 57F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(350, 465);
            this.mainTableLayoutPanel.TabIndex = 0;
            this.mainTableLayoutPanel.TagString = null;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 1;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Controls.Add(this.lblDesc, 0, 1);
            this.topTableLayoutPanel.Controls.Add(this.userAvatar, 0, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 2;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 75F));
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(350, 116);
            this.topTableLayoutPanel.TabIndex = 0;
            this.topTableLayoutPanel.TagString = null;
            // 
            // userAvatar
            // 
            this.userAvatar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userAvatar.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.userAvatar.Icon = Sunny.UI.UIAvatar.UIIcon.Image;
            this.userAvatar.Image = global::Taoyl.UI.Pages.Properties.Resources.默认头像;
            this.userAvatar.Location = new System.Drawing.Point(120, 3);
            this.userAvatar.Margin = new System.Windows.Forms.Padding(120, 3, 120, 3);
            this.userAvatar.MinimumSize = new System.Drawing.Size(1, 1);
            this.userAvatar.Name = "userAvatar";
            this.userAvatar.Size = new System.Drawing.Size(110, 81);
            this.userAvatar.TabIndex = 2;
            this.userAvatar.Text = "uiAvatar1";
            // 
            // lblDesc
            // 
            this.lblDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblDesc.Location = new System.Drawing.Point(3, 90);
            this.lblDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblDesc.Name = "lblDesc";
            this.lblDesc.Size = new System.Drawing.Size(344, 23);
            this.lblDesc.TabIndex = 3;
            this.lblDesc.Text = "用户注册/修改";
            this.lblDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // midTableLayoutPanel
            // 
            this.midTableLayoutPanel.ColumnCount = 2;
            this.midTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.midTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.midTableLayoutPanel.Controls.Add(this.lblPassword, 0, 2);
            this.midTableLayoutPanel.Controls.Add(this.lblUsername, 0, 1);
            this.midTableLayoutPanel.Controls.Add(this.lblRoleLevel, 0, 5);
            this.midTableLayoutPanel.Controls.Add(this.lblCardCode, 0, 4);
            this.midTableLayoutPanel.Controls.Add(this.lblUserNumber, 0, 3);
            this.midTableLayoutPanel.Controls.Add(this.txtUsername, 1, 1);
            this.midTableLayoutPanel.Controls.Add(this.txtPassword, 1, 2);
            this.midTableLayoutPanel.Controls.Add(this.txtUserNumber, 1, 3);
            this.midTableLayoutPanel.Controls.Add(this.txtCardCode, 1, 4);
            this.midTableLayoutPanel.Controls.Add(this.cmbRoleLevel, 1, 5);
            this.midTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.midTableLayoutPanel.Location = new System.Drawing.Point(0, 116);
            this.midTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.midTableLayoutPanel.Name = "midTableLayoutPanel";
            this.midTableLayoutPanel.RowCount = 6;
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.midTableLayoutPanel.Size = new System.Drawing.Size(350, 265);
            this.midTableLayoutPanel.TabIndex = 1;
            this.midTableLayoutPanel.TagString = null;
            // 
            // lblUsername
            // 
            this.lblUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUsername.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.lblUsername.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblUsername.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblUsername.Location = new System.Drawing.Point(3, 29);
            this.lblUsername.Margin = new System.Windows.Forms.Padding(3);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(116, 41);
            this.lblUsername.TabIndex = 4;
            this.lblUsername.Text = "用户名:";
            this.lblUsername.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblRoleLevel
            // 
            this.lblRoleLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoleLevel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.lblRoleLevel.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblRoleLevel.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblRoleLevel.Location = new System.Drawing.Point(3, 217);
            this.lblRoleLevel.Margin = new System.Windows.Forms.Padding(3);
            this.lblRoleLevel.Name = "lblRoleLevel";
            this.lblRoleLevel.Size = new System.Drawing.Size(116, 45);
            this.lblRoleLevel.TabIndex = 11;
            this.lblRoleLevel.Text = "角色:";
            this.lblRoleLevel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCardCode
            // 
            this.lblCardCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCardCode.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.lblCardCode.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblCardCode.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblCardCode.Location = new System.Drawing.Point(3, 170);
            this.lblCardCode.Margin = new System.Windows.Forms.Padding(3);
            this.lblCardCode.Name = "lblCardCode";
            this.lblCardCode.Size = new System.Drawing.Size(116, 41);
            this.lblCardCode.TabIndex = 10;
            this.lblCardCode.Text = "卡号:";
            this.lblCardCode.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblUserNumber
            // 
            this.lblUserNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUserNumber.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.lblUserNumber.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblUserNumber.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblUserNumber.Location = new System.Drawing.Point(3, 123);
            this.lblUserNumber.Margin = new System.Windows.Forms.Padding(3);
            this.lblUserNumber.Name = "lblUserNumber";
            this.lblUserNumber.Size = new System.Drawing.Size(116, 41);
            this.lblUserNumber.TabIndex = 9;
            this.lblUserNumber.Text = "工号:";
            this.lblUserNumber.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblPassword
            // 
            this.lblPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPassword.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.lblPassword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblPassword.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblPassword.Location = new System.Drawing.Point(3, 76);
            this.lblPassword.Margin = new System.Windows.Forms.Padding(3);
            this.lblPassword.Name = "lblPassword";
            this.lblPassword.Size = new System.Drawing.Size(116, 41);
            this.lblPassword.TabIndex = 8;
            this.lblPassword.Text = "密码:";
            this.lblPassword.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // txtUsername
            // 
            this.txtUsername.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUsername.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtUsername.Location = new System.Drawing.Point(137, 31);
            this.txtUsername.Margin = new System.Windows.Forms.Padding(15, 5, 20, 5);
            this.txtUsername.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtUsername.Name = "txtUsername";
            this.txtUsername.Padding = new System.Windows.Forms.Padding(5);
            this.txtUsername.ShowText = false;
            this.txtUsername.Size = new System.Drawing.Size(193, 37);
            this.txtUsername.TabIndex = 12;
            this.txtUsername.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtUsername.Watermark = "";
            // 
            // txtPassword
            // 
            this.txtPassword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtPassword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtPassword.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.txtPassword.Location = new System.Drawing.Point(137, 78);
            this.txtPassword.Margin = new System.Windows.Forms.Padding(15, 5, 20, 5);
            this.txtPassword.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtPassword.Name = "txtPassword";
            this.txtPassword.Padding = new System.Windows.Forms.Padding(5);
            this.txtPassword.ShowText = false;
            this.txtPassword.Size = new System.Drawing.Size(193, 37);
            this.txtPassword.TabIndex = 13;
            this.txtPassword.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtPassword.Watermark = "";
            // 
            // txtUserNumber
            // 
            this.txtUserNumber.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtUserNumber.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtUserNumber.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.txtUserNumber.Location = new System.Drawing.Point(137, 125);
            this.txtUserNumber.Margin = new System.Windows.Forms.Padding(15, 5, 20, 5);
            this.txtUserNumber.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtUserNumber.Name = "txtUserNumber";
            this.txtUserNumber.Padding = new System.Windows.Forms.Padding(5);
            this.txtUserNumber.ShowText = false;
            this.txtUserNumber.Size = new System.Drawing.Size(193, 37);
            this.txtUserNumber.TabIndex = 14;
            this.txtUserNumber.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtUserNumber.Watermark = "";
            // 
            // txtCardCode
            // 
            this.txtCardCode.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtCardCode.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtCardCode.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.txtCardCode.Location = new System.Drawing.Point(137, 172);
            this.txtCardCode.Margin = new System.Windows.Forms.Padding(15, 5, 20, 5);
            this.txtCardCode.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtCardCode.Name = "txtCardCode";
            this.txtCardCode.Padding = new System.Windows.Forms.Padding(5);
            this.txtCardCode.ShowText = false;
            this.txtCardCode.Size = new System.Drawing.Size(193, 37);
            this.txtCardCode.TabIndex = 15;
            this.txtCardCode.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtCardCode.Watermark = "";
            // 
            // cmbRoleLevel
            // 
            this.cmbRoleLevel.CheckBoxes = true;
            this.cmbRoleLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbRoleLevel.DropDownStyle = Sunny.UI.UIDropDownStyle.DropDownList;
            this.cmbRoleLevel.FillColor = System.Drawing.Color.White;
            this.cmbRoleLevel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbRoleLevel.Location = new System.Drawing.Point(137, 219);
            this.cmbRoleLevel.Margin = new System.Windows.Forms.Padding(15, 5, 20, 5);
            this.cmbRoleLevel.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbRoleLevel.Name = "cmbRoleLevel";
            this.cmbRoleLevel.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbRoleLevel.Size = new System.Drawing.Size(193, 41);
            this.cmbRoleLevel.SymbolSize = 24;
            this.cmbRoleLevel.TabIndex = 16;
            this.cmbRoleLevel.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbRoleLevel.Watermark = "";
            // 
            // downTableLayoutPanel
            // 
            this.downTableLayoutPanel.ColumnCount = 2;
            this.downTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.downTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.downTableLayoutPanel.Controls.Add(this.btnCancel, 0, 0);
            this.downTableLayoutPanel.Controls.Add(this.btnConfirm, 1, 0);
            this.downTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downTableLayoutPanel.Location = new System.Drawing.Point(0, 381);
            this.downTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.downTableLayoutPanel.Name = "downTableLayoutPanel";
            this.downTableLayoutPanel.RowCount = 1;
            this.downTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.downTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.downTableLayoutPanel.Size = new System.Drawing.Size(350, 84);
            this.downTableLayoutPanel.TabIndex = 2;
            this.downTableLayoutPanel.TagString = null;
            // 
            // btnCancel
            // 
            this.btnCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnCancel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold);
            this.btnCancel.Location = new System.Drawing.Point(22, 18);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(22, 18, 22, 18);
            this.btnCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Radius = 20;
            this.btnCancel.Size = new System.Drawing.Size(131, 48);
            this.btnCancel.Symbol = 557676;
            this.btnCancel.TabIndex = 16;
            this.btnCancel.Text = "取消";
            this.btnCancel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnConfirm
            // 
            this.btnConfirm.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnConfirm.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnConfirm.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnConfirm.Location = new System.Drawing.Point(197, 18);
            this.btnConfirm.Margin = new System.Windows.Forms.Padding(22, 18, 22, 18);
            this.btnConfirm.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnConfirm.Name = "btnConfirm";
            this.btnConfirm.Radius = 20;
            this.btnConfirm.Size = new System.Drawing.Size(131, 48);
            this.btnConfirm.TabIndex = 15;
            this.btnConfirm.Text = "确认";
            this.btnConfirm.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // FormUserEdit
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(350, 500);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FormUserEdit";
            this.ShowIcon = false;
            this.Text = "用户注册/修改";
            this.TitleFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.midTableLayoutPanel.ResumeLayout(false);
            this.downTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UIAvatar userAvatar;
        private Sunny.UI.UILabel lblDesc;
        private Sunny.UI.UITableLayoutPanel midTableLayoutPanel;
        private Sunny.UI.UILabel lblUsername;
        private Sunny.UI.UILabel lblRoleLevel;
        private Sunny.UI.UILabel lblCardCode;
        private Sunny.UI.UILabel lblUserNumber;
        private Sunny.UI.UILabel lblPassword;
        private Sunny.UI.UITextBox txtUsername;
        private Sunny.UI.UITextBox txtPassword;
        private Sunny.UI.UITextBox txtUserNumber;
        private Sunny.UI.UITextBox txtCardCode;
        private Sunny.UI.UIComboTreeView cmbRoleLevel;
        private Sunny.UI.UITableLayoutPanel downTableLayoutPanel;
        private Sunny.UI.UISymbolButton btnCancel;
        private Sunny.UI.UISymbolButton btnConfirm;
    }
}