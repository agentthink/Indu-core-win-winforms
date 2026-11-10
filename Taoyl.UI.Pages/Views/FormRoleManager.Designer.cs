namespace Taoyl.UI.Pages
{
    partial class FormRoleManager
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle76 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle77 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle78 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle79 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle80 = new System.Windows.Forms.DataGridViewCellStyle();
            this.mainTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.midtableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.dgvRoles = new Sunny.UI.UIDataGridView();
            this.rightPanel = new Sunny.UI.UITableLayoutPanel();
            this.editPanel = new Sunny.UI.UIPanel();
            this.treePermissions = new Sunny.UI.UITreeView();
            this.bottomPanel = new Sunny.UI.UIPanel();
            this.lblStatus = new Sunny.UI.UITextBox();
            this.topPanel = new Sunny.UI.UIPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.pnlUserDesc = new Sunny.UI.UIPanel();
            this.lblCurrentUserRole = new Sunny.UI.UILabel();
            this.lblCurrentUserDesc = new Sunny.UI.UILabel();
            this.pnlReset = new Sunny.UI.UIPanel();
            this.btnReset = new Sunny.UI.UIButton();
            this.pnlRefresh = new Sunny.UI.UIPanel();
            this.btnRefresh = new Sunny.UI.UIButton();
            this.pnlDel = new Sunny.UI.UIPanel();
            this.btnDelete = new Sunny.UI.UIButton();
            this.pnlSave = new Sunny.UI.UIPanel();
            this.btnSave = new Sunny.UI.UIButton();
            this.pnlAdd = new Sunny.UI.UIPanel();
            this.btnAdd = new Sunny.UI.UIButton();
            this.editTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblRoleNameDesc = new Sunny.UI.UILabel();
            this.lblLevelDesc = new Sunny.UI.UILabel();
            this.lblTipsDesc = new Sunny.UI.UILabel();
            this.txtRoleName = new Sunny.UI.UITextBox();
            this.numLevel = new Sunny.UI.UIIntegerUpDown();
            this.lblRemarkDesc = new Sunny.UI.UILabel();
            this.txtRemark = new Sunny.UI.UITextBox();
            this.txtTips = new Sunny.UI.UITextBox();
            this.chkIsBuiltIn = new Sunny.UI.UICheckBox();
            this.mainTableLayoutPanel.SuspendLayout();
            this.midtableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRoles)).BeginInit();
            this.rightPanel.SuspendLayout();
            this.editPanel.SuspendLayout();
            this.bottomPanel.SuspendLayout();
            this.topPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.pnlUserDesc.SuspendLayout();
            this.pnlReset.SuspendLayout();
            this.pnlRefresh.SuspendLayout();
            this.pnlDel.SuspendLayout();
            this.pnlSave.SuspendLayout();
            this.pnlAdd.SuspendLayout();
            this.editTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 1;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Controls.Add(this.midtableLayoutPanel, 0, 1);
            this.mainTableLayoutPanel.Controls.Add(this.bottomPanel, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.topPanel, 0, 0);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 3;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 83F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(1660, 1032);
            this.mainTableLayoutPanel.TabIndex = 0;
            this.mainTableLayoutPanel.TagString = null;
            // 
            // midtableLayoutPanel
            // 
            this.midtableLayoutPanel.ColumnCount = 2;
            this.midtableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.midtableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.midtableLayoutPanel.Controls.Add(this.dgvRoles, 0, 0);
            this.midtableLayoutPanel.Controls.Add(this.rightPanel, 1, 0);
            this.midtableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.midtableLayoutPanel.Location = new System.Drawing.Point(3, 126);
            this.midtableLayoutPanel.Name = "midtableLayoutPanel";
            this.midtableLayoutPanel.Padding = new System.Windows.Forms.Padding(3);
            this.midtableLayoutPanel.RowCount = 1;
            this.midtableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.midtableLayoutPanel.Size = new System.Drawing.Size(1654, 850);
            this.midtableLayoutPanel.TabIndex = 3;
            this.midtableLayoutPanel.TagString = null;
            // 
            // dgvRoles
            // 
            dataGridViewCellStyle76.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvRoles.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle76;
            this.dgvRoles.BackgroundColor = System.Drawing.Color.White;
            this.dgvRoles.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle77.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle77.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle77.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle77.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle77.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle77.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle77.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRoles.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle77;
            this.dgvRoles.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle78.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle78.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle78.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle78.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle78.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle78.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle78.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvRoles.DefaultCellStyle = dataGridViewCellStyle78;
            this.dgvRoles.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRoles.EnableHeadersVisualStyles = false;
            this.dgvRoles.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvRoles.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvRoles.Location = new System.Drawing.Point(6, 9);
            this.dgvRoles.Margin = new System.Windows.Forms.Padding(3, 6, 3, 6);
            this.dgvRoles.MultiSelect = false;
            this.dgvRoles.Name = "dgvRoles";
            this.dgvRoles.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle79.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle79.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle79.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle79.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle79.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle79.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle79.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRoles.RowHeadersDefaultCellStyle = dataGridViewCellStyle79;
            dataGridViewCellStyle80.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle80.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvRoles.RowsDefaultCellStyle = dataGridViewCellStyle80;
            this.dgvRoles.RowTemplate.Height = 23;
            this.dgvRoles.SelectedIndex = -1;
            this.dgvRoles.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dgvRoles.Size = new System.Drawing.Size(488, 832);
            this.dgvRoles.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvRoles.TabIndex = 0;
            // 
            // rightPanel
            // 
            this.rightPanel.ColumnCount = 1;
            this.rightPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rightPanel.Controls.Add(this.editPanel, 0, 0);
            this.rightPanel.Controls.Add(this.treePermissions, 0, 1);
            this.rightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rightPanel.Location = new System.Drawing.Point(500, 6);
            this.rightPanel.Name = "rightPanel";
            this.rightPanel.RowCount = 2;
            this.rightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.rightPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.rightPanel.Size = new System.Drawing.Size(1148, 838);
            this.rightPanel.TabIndex = 1;
            this.rightPanel.TagString = null;
            // 
            // editPanel
            // 
            this.editPanel.Controls.Add(this.editTableLayoutPanel);
            this.editPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.editPanel.Location = new System.Drawing.Point(4, 5);
            this.editPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.editPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.editPanel.Name = "editPanel";
            this.editPanel.Size = new System.Drawing.Size(1140, 241);
            this.editPanel.TabIndex = 0;
            this.editPanel.Text = null;
            this.editPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // treePermissions
            // 
            this.treePermissions.CheckBoxes = true;
            this.treePermissions.Dock = System.Windows.Forms.DockStyle.Fill;
            this.treePermissions.FillColor = System.Drawing.Color.White;
            this.treePermissions.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.treePermissions.Location = new System.Drawing.Point(4, 256);
            this.treePermissions.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.treePermissions.MinimumSize = new System.Drawing.Size(1, 1);
            this.treePermissions.Name = "treePermissions";
            this.treePermissions.ScrollBarStyleInherited = false;
            this.treePermissions.ShowNodeToolTips = true;
            this.treePermissions.ShowText = false;
            this.treePermissions.Size = new System.Drawing.Size(1140, 577);
            this.treePermissions.TabIndex = 1;
            this.treePermissions.Text = "uiTreeView1";
            this.treePermissions.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // bottomPanel
            // 
            this.bottomPanel.Controls.Add(this.lblStatus);
            this.bottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bottomPanel.Location = new System.Drawing.Point(4, 984);
            this.bottomPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.bottomPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.bottomPanel.Name = "bottomPanel";
            this.bottomPanel.Size = new System.Drawing.Size(1652, 43);
            this.bottomPanel.TabIndex = 4;
            this.bottomPanel.Text = null;
            this.bottomPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblStatus
            // 
            this.lblStatus.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.lblStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblStatus.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblStatus.Location = new System.Drawing.Point(0, 0);
            this.lblStatus.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.lblStatus.MinimumSize = new System.Drawing.Size(1, 16);
            this.lblStatus.Name = "lblStatus";
            this.lblStatus.Padding = new System.Windows.Forms.Padding(5);
            this.lblStatus.ShowText = false;
            this.lblStatus.Size = new System.Drawing.Size(1652, 43);
            this.lblStatus.TabIndex = 6;
            this.lblStatus.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblStatus.Watermark = "";
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.topTableLayoutPanel);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topPanel.Location = new System.Drawing.Point(6, 6);
            this.topPanel.Margin = new System.Windows.Forms.Padding(6);
            this.topPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topPanel.Name = "topPanel";
            this.topPanel.Size = new System.Drawing.Size(1648, 111);
            this.topPanel.TabIndex = 5;
            this.topPanel.Text = null;
            this.topPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 6;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.topTableLayoutPanel.Controls.Add(this.pnlUserDesc, 5, 0);
            this.topTableLayoutPanel.Controls.Add(this.pnlReset, 4, 0);
            this.topTableLayoutPanel.Controls.Add(this.pnlRefresh, 3, 0);
            this.topTableLayoutPanel.Controls.Add(this.pnlDel, 2, 0);
            this.topTableLayoutPanel.Controls.Add(this.pnlSave, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.pnlAdd, 0, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 1;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(1648, 111);
            this.topTableLayoutPanel.TabIndex = 0;
            this.topTableLayoutPanel.TagString = null;
            // 
            // pnlUserDesc
            // 
            this.pnlUserDesc.Controls.Add(this.lblCurrentUserRole);
            this.pnlUserDesc.Controls.Add(this.lblCurrentUserDesc);
            this.pnlUserDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlUserDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pnlUserDesc.Location = new System.Drawing.Point(1239, 5);
            this.pnlUserDesc.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlUserDesc.MinimumSize = new System.Drawing.Size(1, 1);
            this.pnlUserDesc.Name = "pnlUserDesc";
            this.pnlUserDesc.Padding = new System.Windows.Forms.Padding(5);
            this.pnlUserDesc.Size = new System.Drawing.Size(405, 101);
            this.pnlUserDesc.TabIndex = 1;
            this.pnlUserDesc.Text = null;
            this.pnlUserDesc.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCurrentUserRole
            // 
            this.lblCurrentUserRole.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCurrentUserRole.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCurrentUserRole.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblCurrentUserRole.Location = new System.Drawing.Point(144, 5);
            this.lblCurrentUserRole.Name = "lblCurrentUserRole";
            this.lblCurrentUserRole.Size = new System.Drawing.Size(256, 91);
            this.lblCurrentUserRole.TabIndex = 13;
            this.lblCurrentUserRole.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblCurrentUserDesc
            // 
            this.lblCurrentUserDesc.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblCurrentUserDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCurrentUserDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblCurrentUserDesc.Location = new System.Drawing.Point(5, 5);
            this.lblCurrentUserDesc.Name = "lblCurrentUserDesc";
            this.lblCurrentUserDesc.Size = new System.Drawing.Size(139, 91);
            this.lblCurrentUserDesc.TabIndex = 12;
            this.lblCurrentUserDesc.Text = "当前用户角色:";
            this.lblCurrentUserDesc.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // pnlReset
            // 
            this.pnlReset.Controls.Add(this.btnReset);
            this.pnlReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlReset.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pnlReset.Location = new System.Drawing.Point(992, 5);
            this.pnlReset.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlReset.MinimumSize = new System.Drawing.Size(1, 1);
            this.pnlReset.Name = "pnlReset";
            this.pnlReset.Padding = new System.Windows.Forms.Padding(5);
            this.pnlReset.Size = new System.Drawing.Size(239, 101);
            this.pnlReset.TabIndex = 4;
            this.pnlReset.Text = null;
            this.pnlReset.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnReset
            // 
            this.btnReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReset.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnReset.Location = new System.Drawing.Point(5, 5);
            this.btnReset.Margin = new System.Windows.Forms.Padding(15);
            this.btnReset.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(229, 91);
            this.btnReset.TabIndex = 12;
            this.btnReset.Text = "重置";
            this.btnReset.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // pnlRefresh
            // 
            this.pnlRefresh.Controls.Add(this.btnRefresh);
            this.pnlRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pnlRefresh.Location = new System.Drawing.Point(745, 5);
            this.pnlRefresh.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.pnlRefresh.Name = "pnlRefresh";
            this.pnlRefresh.Padding = new System.Windows.Forms.Padding(5);
            this.pnlRefresh.Size = new System.Drawing.Size(239, 101);
            this.pnlRefresh.TabIndex = 3;
            this.pnlRefresh.Text = null;
            this.pnlRefresh.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnRefresh
            // 
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnRefresh.Location = new System.Drawing.Point(5, 5);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(15);
            this.btnRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(229, 91);
            this.btnRefresh.TabIndex = 9;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // pnlDel
            // 
            this.pnlDel.Controls.Add(this.btnDelete);
            this.pnlDel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlDel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pnlDel.Location = new System.Drawing.Point(498, 5);
            this.pnlDel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlDel.MinimumSize = new System.Drawing.Size(1, 1);
            this.pnlDel.Name = "pnlDel";
            this.pnlDel.Padding = new System.Windows.Forms.Padding(5);
            this.pnlDel.Size = new System.Drawing.Size(239, 101);
            this.pnlDel.TabIndex = 2;
            this.pnlDel.Text = null;
            this.pnlDel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnDelete
            // 
            this.btnDelete.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnDelete.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnDelete.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnDelete.Location = new System.Drawing.Point(5, 5);
            this.btnDelete.Margin = new System.Windows.Forms.Padding(15);
            this.btnDelete.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(229, 91);
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "删除";
            this.btnDelete.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // pnlSave
            // 
            this.pnlSave.Controls.Add(this.btnSave);
            this.pnlSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlSave.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pnlSave.Location = new System.Drawing.Point(251, 5);
            this.pnlSave.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlSave.MinimumSize = new System.Drawing.Size(1, 1);
            this.pnlSave.Name = "pnlSave";
            this.pnlSave.Padding = new System.Windows.Forms.Padding(5);
            this.pnlSave.Size = new System.Drawing.Size(239, 101);
            this.pnlSave.TabIndex = 1;
            this.pnlSave.Text = null;
            this.pnlSave.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnSave
            // 
            this.btnSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSave.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSave.Location = new System.Drawing.Point(5, 5);
            this.btnSave.Margin = new System.Windows.Forms.Padding(15);
            this.btnSave.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(229, 91);
            this.btnSave.TabIndex = 7;
            this.btnSave.Text = "保存";
            this.btnSave.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // pnlAdd
            // 
            this.pnlAdd.Controls.Add(this.btnAdd);
            this.pnlAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlAdd.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pnlAdd.Location = new System.Drawing.Point(4, 5);
            this.pnlAdd.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.pnlAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.pnlAdd.Name = "pnlAdd";
            this.pnlAdd.Padding = new System.Windows.Forms.Padding(5);
            this.pnlAdd.Size = new System.Drawing.Size(239, 101);
            this.pnlAdd.TabIndex = 0;
            this.pnlAdd.Text = null;
            this.pnlAdd.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnAdd
            // 
            this.btnAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnAdd.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnAdd.Location = new System.Drawing.Point(5, 5);
            this.btnAdd.Margin = new System.Windows.Forms.Padding(15);
            this.btnAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(229, 91);
            this.btnAdd.TabIndex = 6;
            this.btnAdd.Text = "新增";
            this.btnAdd.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // editTableLayoutPanel
            // 
            this.editTableLayoutPanel.ColumnCount = 4;
            this.editTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.editTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 23F));
            this.editTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.editTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.editTableLayoutPanel.Controls.Add(this.chkIsBuiltIn, 2, 0);
            this.editTableLayoutPanel.Controls.Add(this.txtRoleName, 1, 0);
            this.editTableLayoutPanel.Controls.Add(this.lblRoleNameDesc, 0, 0);
            this.editTableLayoutPanel.Controls.Add(this.lblLevelDesc, 0, 1);
            this.editTableLayoutPanel.Controls.Add(this.lblTipsDesc, 0, 3);
            this.editTableLayoutPanel.Controls.Add(this.numLevel, 1, 1);
            this.editTableLayoutPanel.Controls.Add(this.lblRemarkDesc, 2, 1);
            this.editTableLayoutPanel.Controls.Add(this.txtRemark, 3, 1);
            this.editTableLayoutPanel.Controls.Add(this.txtTips, 1, 3);
            this.editTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.editTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.editTableLayoutPanel.Name = "editTableLayoutPanel";
            this.editTableLayoutPanel.RowCount = 4;
            this.editTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.editTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.editTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.editTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.editTableLayoutPanel.Size = new System.Drawing.Size(1140, 241);
            this.editTableLayoutPanel.TabIndex = 0;
            this.editTableLayoutPanel.TagString = null;
            // 
            // lblRoleNameDesc
            // 
            this.lblRoleNameDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRoleNameDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblRoleNameDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblRoleNameDesc.Location = new System.Drawing.Point(3, 3);
            this.lblRoleNameDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblRoleNameDesc.Name = "lblRoleNameDesc";
            this.lblRoleNameDesc.Size = new System.Drawing.Size(130, 54);
            this.lblRoleNameDesc.TabIndex = 2;
            this.lblRoleNameDesc.Text = "角色名称:";
            this.lblRoleNameDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblLevelDesc
            // 
            this.lblLevelDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblLevelDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblLevelDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblLevelDesc.Location = new System.Drawing.Point(3, 63);
            this.lblLevelDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblLevelDesc.Name = "lblLevelDesc";
            this.lblLevelDesc.Size = new System.Drawing.Size(130, 54);
            this.lblLevelDesc.TabIndex = 3;
            this.lblLevelDesc.Text = "角色等级:";
            this.lblLevelDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTipsDesc
            // 
            this.lblTipsDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTipsDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTipsDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblTipsDesc.Location = new System.Drawing.Point(3, 183);
            this.lblTipsDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblTipsDesc.Name = "lblTipsDesc";
            this.lblTipsDesc.Size = new System.Drawing.Size(130, 55);
            this.lblTipsDesc.TabIndex = 7;
            this.lblTipsDesc.Text = "提示: ";
            this.lblTipsDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtRoleName
            // 
            this.txtRoleName.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtRoleName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRoleName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtRoleName.Location = new System.Drawing.Point(156, 5);
            this.txtRoleName.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.txtRoleName.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtRoleName.Name = "txtRoleName";
            this.txtRoleName.Padding = new System.Windows.Forms.Padding(5);
            this.txtRoleName.ShowText = false;
            this.txtRoleName.Size = new System.Drawing.Size(222, 50);
            this.txtRoleName.TabIndex = 8;
            this.txtRoleName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtRoleName.Watermark = "";
            // 
            // numLevel
            // 
            this.numLevel.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.numLevel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.numLevel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.numLevel.Location = new System.Drawing.Point(156, 65);
            this.numLevel.Margin = new System.Windows.Forms.Padding(20, 5, 20, 5);
            this.numLevel.Maximum = 1000D;
            this.numLevel.Minimum = 0D;
            this.numLevel.MinimumSize = new System.Drawing.Size(1, 16);
            this.numLevel.Name = "numLevel";
            this.numLevel.Padding = new System.Windows.Forms.Padding(5);
            this.numLevel.ShowText = false;
            this.numLevel.Size = new System.Drawing.Size(222, 50);
            this.numLevel.Step = 10;
            this.numLevel.TabIndex = 9;
            this.numLevel.Text = "0";
            this.numLevel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblRemarkDesc
            // 
            this.lblRemarkDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRemarkDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblRemarkDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblRemarkDesc.Location = new System.Drawing.Point(401, 63);
            this.lblRemarkDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblRemarkDesc.Name = "lblRemarkDesc";
            this.lblRemarkDesc.Size = new System.Drawing.Size(222, 54);
            this.lblRemarkDesc.TabIndex = 11;
            this.lblRemarkDesc.Text = "备注:";
            this.lblRemarkDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtRemark
            // 
            this.txtRemark.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtRemark.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRemark.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtRemark.Location = new System.Drawing.Point(630, 65);
            this.txtRemark.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtRemark.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtRemark.Name = "txtRemark";
            this.txtRemark.Padding = new System.Windows.Forms.Padding(5);
            this.txtRemark.ShowText = false;
            this.txtRemark.Size = new System.Drawing.Size(506, 50);
            this.txtRemark.TabIndex = 12;
            this.txtRemark.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtRemark.Watermark = "";
            // 
            // txtTips
            // 
            this.editTableLayoutPanel.SetColumnSpan(this.txtTips, 3);
            this.txtTips.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtTips.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtTips.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtTips.Location = new System.Drawing.Point(140, 185);
            this.txtTips.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtTips.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtTips.Name = "txtTips";
            this.txtTips.Padding = new System.Windows.Forms.Padding(5);
            this.txtTips.ShowText = false;
            this.txtTips.Size = new System.Drawing.Size(996, 51);
            this.txtTips.TabIndex = 13;
            this.txtTips.Text = "只能编辑等级不高于当前用户的角色";
            this.txtTips.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtTips.Watermark = "";
            // 
            // chkIsBuiltIn
            // 
            this.chkIsBuiltIn.CheckBoxSize = 26;
            this.chkIsBuiltIn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkIsBuiltIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkIsBuiltIn.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chkIsBuiltIn.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.chkIsBuiltIn.ImageInterval = 30;
            this.chkIsBuiltIn.Location = new System.Drawing.Point(401, 3);
            this.chkIsBuiltIn.MinimumSize = new System.Drawing.Size(1, 1);
            this.chkIsBuiltIn.Name = "chkIsBuiltIn";
            this.chkIsBuiltIn.Size = new System.Drawing.Size(222, 54);
            this.chkIsBuiltIn.TabIndex = 14;
            this.chkIsBuiltIn.Text = "内置角色";
            // 
            // FormRoleManager
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1660, 1032);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FormRoleManager";
            this.Text = "";
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.midtableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRoles)).EndInit();
            this.rightPanel.ResumeLayout(false);
            this.editPanel.ResumeLayout(false);
            this.bottomPanel.ResumeLayout(false);
            this.topPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.pnlUserDesc.ResumeLayout(false);
            this.pnlReset.ResumeLayout(false);
            this.pnlRefresh.ResumeLayout(false);
            this.pnlDel.ResumeLayout(false);
            this.pnlSave.ResumeLayout(false);
            this.pnlAdd.ResumeLayout(false);
            this.editTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel;
        private Sunny.UI.UIPanel pnlUserDesc;
        private Sunny.UI.UIButton btnReset;
        private Sunny.UI.UIButton btnRefresh;
        private Sunny.UI.UIButton btnDelete;
        private Sunny.UI.UIButton btnSave;
        private Sunny.UI.UIButton btnAdd;
        private Sunny.UI.UITableLayoutPanel midtableLayoutPanel;
        private Sunny.UI.UIDataGridView dgvRoles;
        private Sunny.UI.UITableLayoutPanel rightPanel;
        private Sunny.UI.UIPanel editPanel;
        private Sunny.UI.UITreeView treePermissions;
        private Sunny.UI.UITextBox lblStatus;
        private Sunny.UI.UIPanel bottomPanel;
        private Sunny.UI.UIPanel topPanel;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UILabel lblCurrentUserRole;
        private Sunny.UI.UILabel lblCurrentUserDesc;
        private Sunny.UI.UIPanel pnlReset;
        private Sunny.UI.UIPanel pnlRefresh;
        private Sunny.UI.UIPanel pnlDel;
        private Sunny.UI.UIPanel pnlSave;
        private Sunny.UI.UIPanel pnlAdd;
        private Sunny.UI.UITableLayoutPanel editTableLayoutPanel;
        private Sunny.UI.UILabel lblRoleNameDesc;
        private Sunny.UI.UILabel lblLevelDesc;
        private Sunny.UI.UILabel lblTipsDesc;
        private Sunny.UI.UITextBox txtRoleName;
        private Sunny.UI.UIIntegerUpDown numLevel;
        private Sunny.UI.UILabel lblRemarkDesc;
        private Sunny.UI.UITextBox txtRemark;
        private Sunny.UI.UITextBox txtTips;
        private Sunny.UI.UICheckBox chkIsBuiltIn;
    }
}