namespace Taoyl.UI.FormMain
{
    partial class FormMain
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mainTableLayoutPanel1 = new Sunny.UI.UITableLayoutPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topLeftTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.pic_Logo = new System.Windows.Forms.PictureBox();
            this.lblTitle = new Sunny.UI.UILabel();
            this.topMidTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblTime = new Sunny.UI.UILabel();
            this.lblCountdown = new Sunny.UI.UILabel();
            this.topRightTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnClosed = new Sunny.UI.UISymbolLabel();
            this.btnMax = new Sunny.UI.UISymbolLabel();
            this.btnMin = new Sunny.UI.UISymbolLabel();
            this.cmbLanguage = new Sunny.UI.UIComboBox();
            this.btnHelp = new Sunny.UI.UIButton();
            this.downTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.mainPanel = new Sunny.UI.UIPanel();
            this.downLeftTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.userPanel = new Sunny.UI.UIPanel();
            this.userInfoTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnLogout = new Sunny.UI.UIButton();
            this.lblRole = new Sunny.UI.UILabel();
            this.btnLogin = new Sunny.UI.UIButton();
            this.lblUsername = new Sunny.UI.UILabel();
            this.userAvatar = new Sunny.UI.UIAvatar();
            this.navMenu = new Sunny.UI.UINavMenu();
            this.controlTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnReset = new Sunny.UI.UIButton();
            this.btnPause = new Sunny.UI.UIButton();
            this.btnStart = new Sunny.UI.UIButton();
            this.mainTableLayoutPanel1.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.topLeftTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pic_Logo)).BeginInit();
            this.topMidTableLayoutPanel.SuspendLayout();
            this.topRightTableLayoutPanel.SuspendLayout();
            this.downTableLayoutPanel.SuspendLayout();
            this.downLeftTableLayoutPanel.SuspendLayout();
            this.userPanel.SuspendLayout();
            this.userInfoTableLayoutPanel.SuspendLayout();
            this.controlTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel1
            // 
            this.mainTableLayoutPanel1.ColumnCount = 1;
            this.mainTableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel1.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel1.Controls.Add(this.downTableLayoutPanel, 0, 1);
            this.mainTableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel1.Name = "mainTableLayoutPanel1";
            this.mainTableLayoutPanel1.RowCount = 2;
            this.mainTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 4.5F));
            this.mainTableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95.5F));
            this.mainTableLayoutPanel1.Size = new System.Drawing.Size(1556, 980);
            this.mainTableLayoutPanel1.TabIndex = 0;
            this.mainTableLayoutPanel1.TagString = null;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.topTableLayoutPanel.ColumnCount = 3;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.topTableLayoutPanel.Controls.Add(this.topLeftTableLayoutPanel, 0, 0);
            this.topTableLayoutPanel.Controls.Add(this.topMidTableLayoutPanel, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.topRightTableLayoutPanel, 2, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.MinimumSize = new System.Drawing.Size(0, 48);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 1;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(1556, 48);
            this.topTableLayoutPanel.TabIndex = 0;
            this.topTableLayoutPanel.TagString = null;
            // 
            // topLeftTableLayoutPanel
            // 
            this.topLeftTableLayoutPanel.ColumnCount = 3;
            this.topLeftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.topLeftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.topLeftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.topLeftTableLayoutPanel.Controls.Add(this.pic_Logo, 0, 0);
            this.topLeftTableLayoutPanel.Controls.Add(this.lblTitle, 2, 0);
            this.topLeftTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLeftTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.topLeftTableLayoutPanel.Name = "topLeftTableLayoutPanel";
            this.topLeftTableLayoutPanel.RowCount = 1;
            this.topLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topLeftTableLayoutPanel.Size = new System.Drawing.Size(460, 42);
            this.topLeftTableLayoutPanel.TabIndex = 0;
            this.topLeftTableLayoutPanel.TagString = null;
            // 
            // pic_Logo
            // 
            this.pic_Logo.BackColor = System.Drawing.Color.Transparent;
            this.pic_Logo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pic_Logo.Image = global::Taoyl.UI.FormMain.Properties.Resources.AppLogo;
            this.pic_Logo.Location = new System.Drawing.Point(3, 0);
            this.pic_Logo.Margin = new System.Windows.Forms.Padding(3, 0, 3, 0);
            this.pic_Logo.Name = "pic_Logo";
            this.pic_Logo.Size = new System.Drawing.Size(178, 42);
            this.pic_Logo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pic_Logo.TabIndex = 1;
            this.pic_Logo.TabStop = false;
            // 
            // lblTitle
            // 
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(210, 3);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(3);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(247, 36);
            this.lblTitle.TabIndex = 2;
            this.lblTitle.Text = "主界面";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // topMidTableLayoutPanel
            // 
            this.topMidTableLayoutPanel.ColumnCount = 2;
            this.topMidTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topMidTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topMidTableLayoutPanel.Controls.Add(this.lblTime, 1, 0);
            this.topMidTableLayoutPanel.Controls.Add(this.lblCountdown, 0, 0);
            this.topMidTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topMidTableLayoutPanel.Location = new System.Drawing.Point(469, 3);
            this.topMidTableLayoutPanel.Name = "topMidTableLayoutPanel";
            this.topMidTableLayoutPanel.RowCount = 1;
            this.topMidTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topMidTableLayoutPanel.Size = new System.Drawing.Size(616, 42);
            this.topMidTableLayoutPanel.TabIndex = 1;
            this.topMidTableLayoutPanel.TagString = null;
            // 
            // lblTime
            // 
            this.lblTime.BackColor = System.Drawing.Color.Transparent;
            this.lblTime.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTime.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.lblTime.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(212)))), ((int)(((byte)(238)))));
            this.lblTime.Location = new System.Drawing.Point(311, 3);
            this.lblTime.Margin = new System.Windows.Forms.Padding(3);
            this.lblTime.Name = "lblTime";
            this.lblTime.Size = new System.Drawing.Size(302, 36);
            this.lblTime.TabIndex = 4;
            this.lblTime.Text = "实时时间";
            this.lblTime.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblCountdown
            // 
            this.lblCountdown.BackColor = System.Drawing.Color.Transparent;
            this.lblCountdown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCountdown.Font = new System.Drawing.Font("微软雅黑", 10F);
            this.lblCountdown.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(204)))), ((int)(((byte)(212)))), ((int)(((byte)(238)))));
            this.lblCountdown.Location = new System.Drawing.Point(3, 3);
            this.lblCountdown.Margin = new System.Windows.Forms.Padding(3);
            this.lblCountdown.Name = "lblCountdown";
            this.lblCountdown.Size = new System.Drawing.Size(302, 36);
            this.lblCountdown.TabIndex = 3;
            this.lblCountdown.Text = "注销倒计时";
            this.lblCountdown.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // topRightTableLayoutPanel
            // 
            this.topRightTableLayoutPanel.ColumnCount = 8;
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 21F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.topRightTableLayoutPanel.Controls.Add(this.btnClosed, 7, 0);
            this.topRightTableLayoutPanel.Controls.Add(this.btnMax, 6, 0);
            this.topRightTableLayoutPanel.Controls.Add(this.btnMin, 5, 0);
            this.topRightTableLayoutPanel.Controls.Add(this.cmbLanguage, 1, 0);
            this.topRightTableLayoutPanel.Controls.Add(this.btnHelp, 3, 0);
            this.topRightTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topRightTableLayoutPanel.Location = new System.Drawing.Point(1091, 3);
            this.topRightTableLayoutPanel.Name = "topRightTableLayoutPanel";
            this.topRightTableLayoutPanel.RowCount = 1;
            this.topRightTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topRightTableLayoutPanel.Size = new System.Drawing.Size(462, 42);
            this.topRightTableLayoutPanel.TabIndex = 2;
            this.topRightTableLayoutPanel.TagString = null;
            // 
            // btnClosed
            // 
            this.btnClosed.BackColor = System.Drawing.Color.Transparent;
            this.btnClosed.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnClosed.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnClosed.Location = new System.Drawing.Point(425, 3);
            this.btnClosed.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnClosed.Name = "btnClosed";
            this.btnClosed.Size = new System.Drawing.Size(34, 36);
            this.btnClosed.Symbol = 558829;
            this.btnClosed.SymbolColor = System.Drawing.Color.White;
            this.btnClosed.SymbolSize = 40;
            this.btnClosed.TabIndex = 12;
            // 
            // btnMax
            // 
            this.btnMax.BackColor = System.Drawing.Color.Transparent;
            this.btnMax.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMax.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnMax.Location = new System.Drawing.Point(389, 5);
            this.btnMax.Margin = new System.Windows.Forms.Padding(3, 5, 3, 3);
            this.btnMax.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnMax.Name = "btnMax";
            this.btnMax.Size = new System.Drawing.Size(30, 34);
            this.btnMax.Symbol = 61590;
            this.btnMax.SymbolColor = System.Drawing.Color.White;
            this.btnMax.SymbolSize = 35;
            this.btnMax.TabIndex = 13;
            // 
            // btnMin
            // 
            this.btnMin.BackColor = System.Drawing.Color.Transparent;
            this.btnMin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnMin.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnMin.Location = new System.Drawing.Point(353, 3);
            this.btnMin.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnMin.Name = "btnMin";
            this.btnMin.Size = new System.Drawing.Size(30, 36);
            this.btnMin.Symbol = 361544;
            this.btnMin.SymbolColor = System.Drawing.Color.White;
            this.btnMin.SymbolSize = 35;
            this.btnMin.TabIndex = 14;
            // 
            // cmbLanguage
            // 
            this.cmbLanguage.BackColor = System.Drawing.Color.Transparent;
            this.cmbLanguage.DataSource = null;
            this.cmbLanguage.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbLanguage.DropDownStyle = Sunny.UI.UIDropDownStyle.DropDownList;
            this.cmbLanguage.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.cmbLanguage.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbLanguage.ForeColor = System.Drawing.Color.White;
            this.cmbLanguage.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbLanguage.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbLanguage.Location = new System.Drawing.Point(50, 5);
            this.cmbLanguage.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.cmbLanguage.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbLanguage.Name = "cmbLanguage";
            this.cmbLanguage.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbLanguage.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(94)))), ((int)(((byte)(191)))));
            this.cmbLanguage.Size = new System.Drawing.Size(107, 32);
            this.cmbLanguage.Style = Sunny.UI.UIStyle.Custom;
            this.cmbLanguage.SymbolSize = 24;
            this.cmbLanguage.TabIndex = 15;
            this.cmbLanguage.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbLanguage.Watermark = "";
            // 
            // btnHelp
            // 
            this.btnHelp.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.btnHelp.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnHelp.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnHelp.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.btnHelp.FillColor2 = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.btnHelp.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnHelp.Location = new System.Drawing.Point(210, 3);
            this.btnHelp.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnHelp.Name = "btnHelp";
            this.btnHelp.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.btnHelp.Size = new System.Drawing.Size(91, 36);
            this.btnHelp.Style = Sunny.UI.UIStyle.Custom;
            this.btnHelp.TabIndex = 16;
            this.btnHelp.Text = "帮助";
            this.btnHelp.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // downTableLayoutPanel
            // 
            this.downTableLayoutPanel.ColumnCount = 2;
            this.downTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16.5F));
            this.downTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 83.5F));
            this.downTableLayoutPanel.Controls.Add(this.mainPanel, 1, 0);
            this.downTableLayoutPanel.Controls.Add(this.downLeftTableLayoutPanel, 0, 0);
            this.downTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downTableLayoutPanel.Location = new System.Drawing.Point(0, 44);
            this.downTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.downTableLayoutPanel.Name = "downTableLayoutPanel";
            this.downTableLayoutPanel.RowCount = 1;
            this.downTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.downTableLayoutPanel.Size = new System.Drawing.Size(1556, 936);
            this.downTableLayoutPanel.TabIndex = 1;
            this.downTableLayoutPanel.TagString = null;
            // 
            // mainPanel
            // 
            this.mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainPanel.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(251)))));
            this.mainPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.mainPanel.Location = new System.Drawing.Point(256, 0);
            this.mainPanel.Margin = new System.Windows.Forms.Padding(0);
            this.mainPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.mainPanel.Name = "mainPanel";
            this.mainPanel.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(240)))), ((int)(((byte)(245)))), ((int)(((byte)(251)))));
            this.mainPanel.Size = new System.Drawing.Size(1300, 936);
            this.mainPanel.TabIndex = 3;
            this.mainPanel.Text = null;
            this.mainPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // downLeftTableLayoutPanel
            // 
            this.downLeftTableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(25)))), ((int)(((byte)(100)))));
            this.downLeftTableLayoutPanel.ColumnCount = 1;
            this.downLeftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.downLeftTableLayoutPanel.Controls.Add(this.userPanel, 0, 1);
            this.downLeftTableLayoutPanel.Controls.Add(this.navMenu, 0, 0);
            this.downLeftTableLayoutPanel.Controls.Add(this.controlTableLayoutPanel, 0, 2);
            this.downLeftTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downLeftTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.downLeftTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.downLeftTableLayoutPanel.Name = "downLeftTableLayoutPanel";
            this.downLeftTableLayoutPanel.RowCount = 3;
            this.downLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 74F));
            this.downLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 18F));
            this.downLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.downLeftTableLayoutPanel.Size = new System.Drawing.Size(256, 936);
            this.downLeftTableLayoutPanel.TabIndex = 0;
            this.downLeftTableLayoutPanel.TagString = null;
            // 
            // userPanel
            // 
            this.userPanel.BackColor = System.Drawing.Color.Transparent;
            this.userPanel.Controls.Add(this.userInfoTableLayoutPanel);
            this.userPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userPanel.FillColor = System.Drawing.Color.Transparent;
            this.userPanel.FillColor2 = System.Drawing.Color.Transparent;
            this.userPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.userPanel.ForeColor = System.Drawing.Color.White;
            this.userPanel.Location = new System.Drawing.Point(0, 692);
            this.userPanel.Margin = new System.Windows.Forms.Padding(0);
            this.userPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.userPanel.Name = "userPanel";
            this.userPanel.Padding = new System.Windows.Forms.Padding(15);
            this.userPanel.Radius = 0;
            this.userPanel.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(25)))), ((int)(((byte)(100)))));
            this.userPanel.Size = new System.Drawing.Size(256, 168);
            this.userPanel.TabIndex = 4;
            this.userPanel.Text = null;
            this.userPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // userInfoTableLayoutPanel
            // 
            this.userInfoTableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(255)))));
            this.userInfoTableLayoutPanel.ColumnCount = 2;
            this.userInfoTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.userInfoTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.userInfoTableLayoutPanel.Controls.Add(this.btnLogout, 1, 2);
            this.userInfoTableLayoutPanel.Controls.Add(this.lblRole, 1, 1);
            this.userInfoTableLayoutPanel.Controls.Add(this.btnLogin, 0, 2);
            this.userInfoTableLayoutPanel.Controls.Add(this.lblUsername, 1, 0);
            this.userInfoTableLayoutPanel.Controls.Add(this.userAvatar, 0, 0);
            this.userInfoTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userInfoTableLayoutPanel.Location = new System.Drawing.Point(15, 15);
            this.userInfoTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.userInfoTableLayoutPanel.Name = "userInfoTableLayoutPanel";
            this.userInfoTableLayoutPanel.RowCount = 3;
            this.userInfoTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.userInfoTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.userInfoTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.userInfoTableLayoutPanel.Size = new System.Drawing.Size(226, 138);
            this.userInfoTableLayoutPanel.TabIndex = 2;
            this.userInfoTableLayoutPanel.TagString = null;
            // 
            // btnLogout
            // 
            this.btnLogout.BackColor = System.Drawing.Color.Transparent;
            this.btnLogout.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLogout.FillColor = System.Drawing.Color.Transparent;
            this.btnLogout.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnLogout.ForeColor = System.Drawing.Color.Black;
            this.btnLogout.Location = new System.Drawing.Point(133, 88);
            this.btnLogout.Margin = new System.Windows.Forms.Padding(20);
            this.btnLogout.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(138)))), ((int)(((byte)(255)))));
            this.btnLogout.Size = new System.Drawing.Size(73, 30);
            this.btnLogout.Style = Sunny.UI.UIStyle.Custom;
            this.btnLogout.TabIndex = 1;
            this.btnLogout.Text = "注销";
            this.btnLogout.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // lblRole
            // 
            this.lblRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRole.Font = new System.Drawing.Font("微软雅黑", 10.5F);
            this.lblRole.ForeColor = System.Drawing.Color.White;
            this.lblRole.Location = new System.Drawing.Point(116, 37);
            this.lblRole.Margin = new System.Windows.Forms.Padding(3);
            this.lblRole.Name = "lblRole";
            this.lblRole.Size = new System.Drawing.Size(107, 28);
            this.lblRole.TabIndex = 2;
            this.lblRole.Text = "操作员";
            this.lblRole.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnLogin
            // 
            this.btnLogin.BackColor = System.Drawing.Color.Transparent;
            this.btnLogin.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnLogin.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnLogin.FillColor = System.Drawing.Color.Transparent;
            this.btnLogin.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnLogin.ForeColor = System.Drawing.Color.Black;
            this.btnLogin.Location = new System.Drawing.Point(20, 88);
            this.btnLogin.Margin = new System.Windows.Forms.Padding(20);
            this.btnLogin.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnLogin.Name = "btnLogin";
            this.btnLogin.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(138)))), ((int)(((byte)(255)))));
            this.btnLogin.Size = new System.Drawing.Size(73, 30);
            this.btnLogin.Style = Sunny.UI.UIStyle.Custom;
            this.btnLogin.TabIndex = 0;
            this.btnLogin.Text = "登录";
            this.btnLogin.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // lblUsername
            // 
            this.lblUsername.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblUsername.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblUsername.ForeColor = System.Drawing.Color.White;
            this.lblUsername.Location = new System.Drawing.Point(116, 3);
            this.lblUsername.Margin = new System.Windows.Forms.Padding(3);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(107, 28);
            this.lblUsername.TabIndex = 1;
            this.lblUsername.Text = "未登录";
            this.lblUsername.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // userAvatar
            // 
            this.userAvatar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(255)))));
            this.userAvatar.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.userAvatar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.userAvatar.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.userAvatar.Icon = Sunny.UI.UIAvatar.UIIcon.Image;
            this.userAvatar.Image = global::Taoyl.UI.FormMain.Properties.Resources.默认头像;
            this.userAvatar.Location = new System.Drawing.Point(35, 10);
            this.userAvatar.Margin = new System.Windows.Forms.Padding(35, 10, 35, 13);
            this.userAvatar.MinimumSize = new System.Drawing.Size(1, 1);
            this.userAvatar.Name = "userAvatar";
            this.userInfoTableLayoutPanel.SetRowSpan(this.userAvatar, 2);
            this.userAvatar.Size = new System.Drawing.Size(43, 45);
            this.userAvatar.TabIndex = 3;
            this.userAvatar.Text = "uiAvatar1";
            // 
            // navMenu
            // 
            this.navMenu.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(25)))), ((int)(((byte)(100)))));
            this.navMenu.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.navMenu.Dock = System.Windows.Forms.DockStyle.Fill;
            this.navMenu.DrawMode = System.Windows.Forms.TreeViewDrawMode.OwnerDrawAll;
            this.navMenu.FillColor = System.Drawing.Color.Transparent;
            this.navMenu.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.navMenu.ForeColor = System.Drawing.Color.White;
            this.navMenu.FullRowSelect = true;
            this.navMenu.HotTracking = true;
            this.navMenu.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(40)))), ((int)(((byte)(170)))));
            this.navMenu.ItemHeight = 48;
            this.navMenu.LineColor = System.Drawing.Color.Red;
            this.navMenu.Location = new System.Drawing.Point(3, 3);
            this.navMenu.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.navMenu.Name = "navMenu";
            this.navMenu.SelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(255)))));
            this.navMenu.SelectedForeColor = System.Drawing.Color.White;
            this.navMenu.ShowLines = false;
            this.navMenu.ShowPlusMinus = false;
            this.navMenu.ShowRootLines = false;
            this.navMenu.Size = new System.Drawing.Size(250, 686);
            this.navMenu.Style = Sunny.UI.UIStyle.Custom;
            this.navMenu.TabIndex = 1;
            this.navMenu.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // controlTableLayoutPanel
            // 
            this.controlTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.controlTableLayoutPanel.ColumnCount = 3;
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.controlTableLayoutPanel.Controls.Add(this.btnReset, 2, 0);
            this.controlTableLayoutPanel.Controls.Add(this.btnPause, 1, 0);
            this.controlTableLayoutPanel.Controls.Add(this.btnStart, 0, 0);
            this.controlTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlTableLayoutPanel.Location = new System.Drawing.Point(3, 863);
            this.controlTableLayoutPanel.Name = "controlTableLayoutPanel";
            this.controlTableLayoutPanel.RowCount = 1;
            this.controlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.controlTableLayoutPanel.Size = new System.Drawing.Size(250, 70);
            this.controlTableLayoutPanel.TabIndex = 5;
            this.controlTableLayoutPanel.TagString = null;
            // 
            // btnReset
            // 
            this.btnReset.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(255)))));
            this.btnReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReset.FillColor = System.Drawing.Color.Transparent;
            this.btnReset.FillColor2 = System.Drawing.Color.Transparent;
            this.btnReset.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.btnReset.ForeHoverColor = System.Drawing.Color.GreenYellow;
            this.btnReset.ForePressColor = System.Drawing.Color.DarkGreen;
            this.btnReset.Location = new System.Drawing.Point(170, 6);
            this.btnReset.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnReset.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(76, 58);
            this.btnReset.Style = Sunny.UI.UIStyle.Custom;
            this.btnReset.TabIndex = 7;
            this.btnReset.Text = "复位";
            this.btnReset.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPause
            // 
            this.btnPause.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(255)))));
            this.btnPause.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPause.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPause.FillColor = System.Drawing.Color.Transparent;
            this.btnPause.FillColor2 = System.Drawing.Color.Transparent;
            this.btnPause.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.btnPause.ForeHoverColor = System.Drawing.Color.GreenYellow;
            this.btnPause.ForePressColor = System.Drawing.Color.DarkGreen;
            this.btnPause.Location = new System.Drawing.Point(87, 6);
            this.btnPause.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnPause.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(75, 58);
            this.btnPause.Style = Sunny.UI.UIStyle.Custom;
            this.btnPause.TabIndex = 6;
            this.btnPause.Text = "暂停";
            this.btnPause.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnStart
            // 
            this.btnStart.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(99)))), ((int)(((byte)(255)))));
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.FillColor = System.Drawing.Color.Transparent;
            this.btnStart.FillColor2 = System.Drawing.Color.Transparent;
            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.btnStart.ForeHoverColor = System.Drawing.Color.GreenYellow;
            this.btnStart.ForePressColor = System.Drawing.Color.DarkGreen;
            this.btnStart.Location = new System.Drawing.Point(4, 6);
            this.btnStart.Margin = new System.Windows.Forms.Padding(4, 6, 4, 6);
            this.btnStart.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(75, 58);
            this.btnStart.Style = Sunny.UI.UIStyle.Custom;
            this.btnStart.TabIndex = 3;
            this.btnStart.Text = "启动";
            this.btnStart.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // FormMain
            // 
            this.AllowShowTitle = false;
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1556, 980);
            this.Controls.Add(this.mainTableLayoutPanel1);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MinimumSize = new System.Drawing.Size(1280, 720);
            this.Name = "FormMain";
            this.Padding = new System.Windows.Forms.Padding(0);
            this.ShowTitle = false;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.mainTableLayoutPanel1.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.topLeftTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pic_Logo)).EndInit();
            this.topMidTableLayoutPanel.ResumeLayout(false);
            this.topRightTableLayoutPanel.ResumeLayout(false);
            this.downTableLayoutPanel.ResumeLayout(false);
            this.downLeftTableLayoutPanel.ResumeLayout(false);
            this.userPanel.ResumeLayout(false);
            this.userInfoTableLayoutPanel.ResumeLayout(false);
            this.controlTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel1;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel topLeftTableLayoutPanel;
        private System.Windows.Forms.PictureBox pic_Logo;
        private Sunny.UI.UILabel lblTitle;
        private Sunny.UI.UITableLayoutPanel topMidTableLayoutPanel;
        private Sunny.UI.UILabel lblCountdown;
        private Sunny.UI.UILabel lblTime;
        private Sunny.UI.UITableLayoutPanel topRightTableLayoutPanel;
        private Sunny.UI.UISymbolLabel btnClosed;
        private Sunny.UI.UISymbolLabel btnMax;
        private Sunny.UI.UISymbolLabel btnMin;
        private Sunny.UI.UIComboBox cmbLanguage;
        private Sunny.UI.UIButton btnHelp;
        private Sunny.UI.UITableLayoutPanel downTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel downLeftTableLayoutPanel;
        private Sunny.UI.UINavMenu navMenu;
        private Sunny.UI.UIPanel mainPanel;
        private Sunny.UI.UIPanel userPanel;
        private Sunny.UI.UITableLayoutPanel controlTableLayoutPanel;
        private Sunny.UI.UIButton btnReset;
        private Sunny.UI.UIButton btnPause;
        private Sunny.UI.UIButton btnStart;
        private Sunny.UI.UITableLayoutPanel userInfoTableLayoutPanel;
        private Sunny.UI.UIButton btnLogout;
        private Sunny.UI.UILabel lblRole;
        private Sunny.UI.UIButton btnLogin;
        private Sunny.UI.UILabel lblUsername;
        private Sunny.UI.UIAvatar userAvatar;
    }
}
