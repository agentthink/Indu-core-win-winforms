namespace Taoyl.UI.Pages
{
    partial class FormProductLive
    {
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
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topRightPanel = new Sunny.UI.UIPanel();
            this.refreshTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.upDownInterval = new Sunny.UI.UIIntegerUpDown();
            this.cmbTake = new Sunny.UI.UIComboBox();
            this.lblInterval = new Sunny.UI.UILabel();
            this.switchAutoRefresh = new Sunny.UI.UISwitch();
            this.lblAutoRefresh = new Sunny.UI.UILabel();
            this.lblManualRefresh = new Sunny.UI.UILabel();
            this.btnManualRefresh = new Sunny.UI.UIButton();
            this.lblTake = new Sunny.UI.UILabel();
            this.topMidPanel = new Sunny.UI.UIPanel();
            this.topLeftPanel = new Sunny.UI.UIPanel();
            this.selectTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.cmbTable = new Sunny.UI.UIComboBox();
            this.lblTable = new Sunny.UI.UILabel();
            this.lblSortField = new Sunny.UI.UILabel();
            this.cmbSortField = new Sunny.UI.UIComboBox();
            this.cmbSortDir = new Sunny.UI.UIComboBox();
            this.dgvRecords = new Sunny.UI.UIDataGridView();
            this.pollTimer = new System.Windows.Forms.Timer(this.components);
            this.rootTableLayoutPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.topRightPanel.SuspendLayout();
            this.refreshTableLayoutPanel.SuspendLayout();
            this.topLeftPanel.SuspendLayout();
            this.selectTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecords)).BeginInit();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.dgvRecords, 0, 1);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 2;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 88F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(1660, 1032);
            this.rootTableLayoutPanel.TabIndex = 0;
            this.rootTableLayoutPanel.TagString = null;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 3;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 29F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 36F));
            this.topTableLayoutPanel.Controls.Add(this.topRightPanel, 2, 0);
            this.topTableLayoutPanel.Controls.Add(this.topMidPanel, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.topLeftPanel, 0, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 1;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(1660, 123);
            this.topTableLayoutPanel.TabIndex = 6;
            this.topTableLayoutPanel.TagString = null;
            // 
            // topRightPanel
            // 
            this.topRightPanel.Controls.Add(this.refreshTableLayoutPanel);
            this.topRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topRightPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topRightPanel.Location = new System.Drawing.Point(1066, 5);
            this.topRightPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topRightPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topRightPanel.Name = "topRightPanel";
            this.topRightPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topRightPanel.Size = new System.Drawing.Size(590, 113);
            this.topRightPanel.TabIndex = 2;
            this.topRightPanel.Text = null;
            this.topRightPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // refreshTableLayoutPanel
            // 
            this.refreshTableLayoutPanel.ColumnCount = 4;
            this.refreshTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.refreshTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.refreshTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 16F));
            this.refreshTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 34F));
            this.refreshTableLayoutPanel.Controls.Add(this.upDownInterval, 3, 0);
            this.refreshTableLayoutPanel.Controls.Add(this.cmbTake, 3, 1);
            this.refreshTableLayoutPanel.Controls.Add(this.lblInterval, 2, 0);
            this.refreshTableLayoutPanel.Controls.Add(this.switchAutoRefresh, 1, 0);
            this.refreshTableLayoutPanel.Controls.Add(this.lblAutoRefresh, 0, 0);
            this.refreshTableLayoutPanel.Controls.Add(this.lblManualRefresh, 0, 1);
            this.refreshTableLayoutPanel.Controls.Add(this.btnManualRefresh, 1, 1);
            this.refreshTableLayoutPanel.Controls.Add(this.lblTake, 2, 1);
            this.refreshTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.refreshTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.refreshTableLayoutPanel.Name = "refreshTableLayoutPanel";
            this.refreshTableLayoutPanel.RowCount = 2;
            this.refreshTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.refreshTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.refreshTableLayoutPanel.Size = new System.Drawing.Size(580, 103);
            this.refreshTableLayoutPanel.TabIndex = 0;
            this.refreshTableLayoutPanel.TagString = null;
            // 
            // upDownInterval
            // 
            this.upDownInterval.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.upDownInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.upDownInterval.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.upDownInterval.Location = new System.Drawing.Point(396, 5);
            this.upDownInterval.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.upDownInterval.MinimumSize = new System.Drawing.Size(1, 16);
            this.upDownInterval.Name = "upDownInterval";
            this.upDownInterval.Padding = new System.Windows.Forms.Padding(5);
            this.upDownInterval.ShowText = false;
            this.upDownInterval.Size = new System.Drawing.Size(169, 41);
            this.upDownInterval.TabIndex = 21;
            this.upDownInterval.Text = "30";
            this.upDownInterval.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.upDownInterval.Value = 30;
            // 
            // cmbTake
            // 
            this.cmbTake.DataSource = null;
            this.cmbTake.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbTake.FillColor = System.Drawing.Color.White;
            this.cmbTake.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbTake.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbTake.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbTake.Location = new System.Drawing.Point(396, 56);
            this.cmbTake.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbTake.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbTake.Name = "cmbTake";
            this.cmbTake.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbTake.Size = new System.Drawing.Size(169, 42);
            this.cmbTake.SymbolSize = 24;
            this.cmbTake.TabIndex = 22;
            this.cmbTake.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbTake.Watermark = "";
            // 
            // lblInterval
            // 
            this.lblInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInterval.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblInterval.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblInterval.Location = new System.Drawing.Point(292, 3);
            this.lblInterval.Margin = new System.Windows.Forms.Padding(3);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(86, 45);
            this.lblInterval.TabIndex = 19;
            this.lblInterval.Text = "间隔(秒)";
            this.lblInterval.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // switchAutoRefresh
            // 
            this.switchAutoRefresh.Active = true;
            this.switchAutoRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.switchAutoRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.switchAutoRefresh.Location = new System.Drawing.Point(107, 5);
            this.switchAutoRefresh.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.switchAutoRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.switchAutoRefresh.Name = "switchAutoRefresh";
            this.switchAutoRefresh.Size = new System.Drawing.Size(167, 41);
            this.switchAutoRefresh.SwitchShape = Sunny.UI.UISwitch.UISwitchShape.Square;
            this.switchAutoRefresh.TabIndex = 17;
            this.switchAutoRefresh.Text = "switchAutoRefresh";
            // 
            // lblAutoRefresh
            // 
            this.lblAutoRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblAutoRefresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblAutoRefresh.Location = new System.Drawing.Point(3, 3);
            this.lblAutoRefresh.Margin = new System.Windows.Forms.Padding(3);
            this.lblAutoRefresh.Name = "lblAutoRefresh";
            this.lblAutoRefresh.Size = new System.Drawing.Size(86, 45);
            this.lblAutoRefresh.TabIndex = 15;
            this.lblAutoRefresh.Text = "自动刷新";
            this.lblAutoRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblManualRefresh
            // 
            this.lblManualRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblManualRefresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblManualRefresh.Location = new System.Drawing.Point(3, 54);
            this.lblManualRefresh.Margin = new System.Windows.Forms.Padding(3);
            this.lblManualRefresh.Name = "lblManualRefresh";
            this.lblManualRefresh.Size = new System.Drawing.Size(86, 46);
            this.lblManualRefresh.TabIndex = 16;
            this.lblManualRefresh.Text = "手动刷新";
            this.lblManualRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnManualRefresh
            // 
            this.btnManualRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManualRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManualRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnManualRefresh.Location = new System.Drawing.Point(107, 56);
            this.btnManualRefresh.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.btnManualRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnManualRefresh.Name = "btnManualRefresh";
            this.btnManualRefresh.Size = new System.Drawing.Size(167, 42);
            this.btnManualRefresh.TabIndex = 18;
            this.btnManualRefresh.Text = "手动刷新";
            this.btnManualRefresh.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // lblTake
            // 
            this.lblTake.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTake.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTake.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblTake.Location = new System.Drawing.Point(292, 54);
            this.lblTake.Margin = new System.Windows.Forms.Padding(3);
            this.lblTake.Name = "lblTake";
            this.lblTake.Size = new System.Drawing.Size(86, 46);
            this.lblTake.TabIndex = 20;
            this.lblTake.Text = "显示数量";
            this.lblTake.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topMidPanel
            // 
            this.topMidPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topMidPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topMidPanel.Location = new System.Drawing.Point(585, 5);
            this.topMidPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topMidPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topMidPanel.Name = "topMidPanel";
            this.topMidPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topMidPanel.Size = new System.Drawing.Size(473, 113);
            this.topMidPanel.TabIndex = 1;
            this.topMidPanel.Text = null;
            this.topMidPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topLeftPanel
            // 
            this.topLeftPanel.Controls.Add(this.selectTableLayoutPanel);
            this.topLeftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLeftPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topLeftPanel.Location = new System.Drawing.Point(4, 5);
            this.topLeftPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topLeftPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topLeftPanel.Name = "topLeftPanel";
            this.topLeftPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topLeftPanel.Size = new System.Drawing.Size(573, 113);
            this.topLeftPanel.TabIndex = 0;
            this.topLeftPanel.Text = null;
            this.topLeftPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // selectTableLayoutPanel
            // 
            this.selectTableLayoutPanel.ColumnCount = 3;
            this.selectTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 18.5F));
            this.selectTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.selectTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 31.5F));
            this.selectTableLayoutPanel.Controls.Add(this.cmbTable, 1, 0);
            this.selectTableLayoutPanel.Controls.Add(this.lblTable, 0, 0);
            this.selectTableLayoutPanel.Controls.Add(this.lblSortField, 0, 1);
            this.selectTableLayoutPanel.Controls.Add(this.cmbSortField, 1, 1);
            this.selectTableLayoutPanel.Controls.Add(this.cmbSortDir, 2, 1);
            this.selectTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.selectTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.selectTableLayoutPanel.Name = "selectTableLayoutPanel";
            this.selectTableLayoutPanel.RowCount = 2;
            this.selectTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.selectTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.selectTableLayoutPanel.Size = new System.Drawing.Size(563, 103);
            this.selectTableLayoutPanel.TabIndex = 0;
            this.selectTableLayoutPanel.TagString = null;
            // 
            // cmbTable
            // 
            this.cmbTable.DataSource = null;
            this.cmbTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbTable.FillColor = System.Drawing.Color.White;
            this.cmbTable.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbTable.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbTable.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbTable.Location = new System.Drawing.Point(119, 5);
            this.cmbTable.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbTable.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbTable.Name = "cmbTable";
            this.cmbTable.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbTable.Size = new System.Drawing.Size(251, 41);
            this.cmbTable.SymbolSize = 24;
            this.cmbTable.TabIndex = 3;
            this.cmbTable.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbTable.Watermark = "";
            // 
            // lblTable
            // 
            this.lblTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTable.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTable.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblTable.Location = new System.Drawing.Point(3, 3);
            this.lblTable.Margin = new System.Windows.Forms.Padding(3);
            this.lblTable.Name = "lblTable";
            this.lblTable.Size = new System.Drawing.Size(98, 45);
            this.lblTable.TabIndex = 2;
            this.lblTable.Text = "产品表";
            this.lblTable.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblSortField
            // 
            this.lblSortField.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSortField.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblSortField.Location = new System.Drawing.Point(3, 54);
            this.lblSortField.Margin = new System.Windows.Forms.Padding(3);
            this.lblSortField.Name = "lblSortField";
            this.lblSortField.Size = new System.Drawing.Size(88, 46);
            this.lblSortField.TabIndex = 6;
            this.lblSortField.Text = "排序字段";
            this.lblSortField.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cmbSortField
            // 
            this.cmbSortField.DataSource = null;
            this.cmbSortField.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbSortField.FillColor = System.Drawing.Color.White;
            this.cmbSortField.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbSortField.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbSortField.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbSortField.Location = new System.Drawing.Point(119, 56);
            this.cmbSortField.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbSortField.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbSortField.Name = "cmbSortField";
            this.cmbSortField.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbSortField.Size = new System.Drawing.Size(251, 42);
            this.cmbSortField.SymbolSize = 24;
            this.cmbSortField.TabIndex = 7;
            this.cmbSortField.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbSortField.Watermark = "";
            // 
            // cmbSortDir
            // 
            this.cmbSortDir.DataSource = null;
            this.cmbSortDir.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbSortDir.FillColor = System.Drawing.Color.White;
            this.cmbSortDir.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbSortDir.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbSortDir.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbSortDir.Location = new System.Drawing.Point(390, 56);
            this.cmbSortDir.Margin = new System.Windows.Forms.Padding(5, 5, 15, 5);
            this.cmbSortDir.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbSortDir.Name = "cmbSortDir";
            this.cmbSortDir.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbSortDir.Size = new System.Drawing.Size(158, 42);
            this.cmbSortDir.SymbolSize = 24;
            this.cmbSortDir.TabIndex = 9;
            this.cmbSortDir.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbSortDir.Watermark = "";
            // 
            // dgvRecords
            // 
            this.dgvRecords.AllowUserToResizeRows = false;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvRecords.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle9;
            this.dgvRecords.BackgroundColor = System.Drawing.Color.White;
            this.dgvRecords.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle10.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle10.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle10.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle10.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle10.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecords.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle10;
            this.dgvRecords.ColumnHeadersHeight = 32;
            this.dgvRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRecords.EnableHeadersVisualStyles = false;
            this.dgvRecords.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvRecords.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvRecords.Location = new System.Drawing.Point(3, 126);
            this.dgvRecords.Name = "dgvRecords";
            this.dgvRecords.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle11.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle11.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle11.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle11.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle11.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle11.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecords.RowHeadersDefaultCellStyle = dataGridViewCellStyle11;
            this.dgvRecords.RowHeadersVisible = false;
            dataGridViewCellStyle12.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle12.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvRecords.RowsDefaultCellStyle = dataGridViewCellStyle12;
            this.dgvRecords.SelectedIndex = -1;
            this.dgvRecords.Size = new System.Drawing.Size(1654, 903);
            this.dgvRecords.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvRecords.TabIndex = 2;
            // 
            // pollTimer
            // 
            this.pollTimer.Interval = 30000;
            // 
            // FormProductLive
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1660, 1032);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FormProductLive";
            this.Text = "产品实时数据";
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.topRightPanel.ResumeLayout(false);
            this.refreshTableLayoutPanel.ResumeLayout(false);
            this.topLeftPanel.ResumeLayout(false);
            this.selectTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecords)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.Timer pollTimer;
        private Sunny.UI.UIDataGridView dgvRecords;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UIPanel topRightPanel;
        private Sunny.UI.UITableLayoutPanel refreshTableLayoutPanel;
        private Sunny.UI.UIIntegerUpDown upDownInterval;
        private Sunny.UI.UIComboBox cmbTake;
        private Sunny.UI.UILabel lblInterval;
        private Sunny.UI.UISwitch switchAutoRefresh;
        private Sunny.UI.UILabel lblAutoRefresh;
        private Sunny.UI.UILabel lblManualRefresh;
        private Sunny.UI.UIButton btnManualRefresh;
        private Sunny.UI.UILabel lblTake;
        private Sunny.UI.UIPanel topMidPanel;
        private Sunny.UI.UIPanel topLeftPanel;
        private Sunny.UI.UITableLayoutPanel selectTableLayoutPanel;
        private Sunny.UI.UIComboBox cmbTable;
        private Sunny.UI.UILabel lblTable;
        private Sunny.UI.UILabel lblSortField;
        private Sunny.UI.UIComboBox cmbSortField;
        private Sunny.UI.UIComboBox cmbSortDir;
    }
}
