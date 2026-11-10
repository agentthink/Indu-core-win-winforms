namespace Taoyl.UI.Pages
{
    partial class FormProcessMonitor
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.bottomTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.downMidTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.gbControl = new Sunny.UI.UIGroupBox();
            this.controlTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnReset = new Sunny.UI.UIButton();
            this.btnPause = new Sunny.UI.UIButton();
            this.btnStart = new Sunny.UI.UIButton();
            this.btnInit = new Sunny.UI.UIButton();
            this.gbConfig = new Sunny.UI.UIGroupBox();
            this.configTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnTogglePauseOthers = new Sunny.UI.UIButton();
            this.btnToggleEnabled = new Sunny.UI.UIButton();
            this.downTopPanel = new Sunny.UI.UIPanel();
            this.uiTableLayoutPanel3 = new Sunny.UI.UITableLayoutPanel();
            this.lblSelectedInfo = new Sunny.UI.UILabel();
            this.lblSelectedDesc = new Sunny.UI.UILabel();
            this.downBottomPanel = new Sunny.UI.UIPanel();
            this.uiTableLayoutPanel4 = new Sunny.UI.UITableLayoutPanel();
            this.lblFeedback = new Sunny.UI.UILabel();
            this.lblFeedbackDesc = new Sunny.UI.UILabel();
            this.mainTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topLeftPanel = new Sunny.UI.UIPanel();
            this.sysStateTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblSystemStateDesc = new Sunny.UI.UILabel();
            this.lblSystemState = new Sunny.UI.UILabel();
            this.topMidPanel = new Sunny.UI.UIPanel();
            this.btnInitAll = new Sunny.UI.UIButton();
            this.topRightPanel = new Sunny.UI.UIPanel();
            this.autoManulTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblIntervalUnit = new Sunny.UI.UILabel();
            this.upDownInterval = new Sunny.UI.UIIntegerUpDown();
            this.switchAutoRefresh = new Sunny.UI.UISwitch();
            this.lblAutoRefresh = new Sunny.UI.UILabel();
            this.lblManulRefresh = new Sunny.UI.UILabel();
            this.btnManulRefresh = new Sunny.UI.UIButton();
            this.cmbPageSize = new Sunny.UI.UIComboBox();
            this.lblInterval = new Sunny.UI.UILabel();
            this.lblPageSize = new Sunny.UI.UILabel();
            this.midTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.dgvProcesses = new Sunny.UI.UIDataGridView();
            this.paginationProcess = new Taoyl.UI.Pages.Controls.Pagination();
            this.bottomTableLayoutPanel.SuspendLayout();
            this.downMidTableLayoutPanel.SuspendLayout();
            this.gbControl.SuspendLayout();
            this.controlTableLayoutPanel.SuspendLayout();
            this.gbConfig.SuspendLayout();
            this.configTableLayoutPanel.SuspendLayout();
            this.downTopPanel.SuspendLayout();
            this.uiTableLayoutPanel3.SuspendLayout();
            this.downBottomPanel.SuspendLayout();
            this.uiTableLayoutPanel4.SuspendLayout();
            this.mainTableLayoutPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.topLeftPanel.SuspendLayout();
            this.sysStateTableLayoutPanel.SuspendLayout();
            this.topMidPanel.SuspendLayout();
            this.topRightPanel.SuspendLayout();
            this.autoManulTableLayoutPanel.SuspendLayout();
            this.midTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvProcesses)).BeginInit();
            this.SuspendLayout();
            // 
            // bottomTableLayoutPanel
            // 
            this.bottomTableLayoutPanel.ColumnCount = 1;
            this.bottomTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomTableLayoutPanel.Controls.Add(this.downMidTableLayoutPanel, 0, 1);
            this.bottomTableLayoutPanel.Controls.Add(this.downTopPanel, 0, 0);
            this.bottomTableLayoutPanel.Controls.Add(this.downBottomPanel, 0, 2);
            this.bottomTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomTableLayoutPanel.Location = new System.Drawing.Point(3, 788);
            this.bottomTableLayoutPanel.Name = "bottomTableLayoutPanel";
            this.bottomTableLayoutPanel.RowCount = 3;
            this.bottomTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.bottomTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.bottomTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.bottomTableLayoutPanel.Size = new System.Drawing.Size(1654, 241);
            this.bottomTableLayoutPanel.TabIndex = 2;
            this.bottomTableLayoutPanel.TagString = null;
            // 
            // downMidTableLayoutPanel
            // 
            this.downMidTableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.downMidTableLayoutPanel.ColumnCount = 2;
            this.downMidTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.downMidTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.downMidTableLayoutPanel.Controls.Add(this.gbControl, 0, 0);
            this.downMidTableLayoutPanel.Controls.Add(this.gbConfig, 1, 0);
            this.downMidTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downMidTableLayoutPanel.Location = new System.Drawing.Point(4, 52);
            this.downMidTableLayoutPanel.Margin = new System.Windows.Forms.Padding(4);
            this.downMidTableLayoutPanel.Name = "downMidTableLayoutPanel";
            this.downMidTableLayoutPanel.RowCount = 1;
            this.downMidTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.downMidTableLayoutPanel.Size = new System.Drawing.Size(1646, 136);
            this.downMidTableLayoutPanel.TabIndex = 1;
            this.downMidTableLayoutPanel.TagString = null;
            // 
            // gbControl
            // 
            this.gbControl.Controls.Add(this.controlTableLayoutPanel);
            this.gbControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbControl.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gbControl.Location = new System.Drawing.Point(0, 0);
            this.gbControl.Margin = new System.Windows.Forms.Padding(0);
            this.gbControl.MinimumSize = new System.Drawing.Size(1, 1);
            this.gbControl.Name = "gbControl";
            this.gbControl.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.gbControl.Size = new System.Drawing.Size(1069, 136);
            this.gbControl.TabIndex = 9;
            this.gbControl.Text = "控制";
            this.gbControl.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // controlTableLayoutPanel
            // 
            this.controlTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.controlTableLayoutPanel.ColumnCount = 4;
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.controlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.controlTableLayoutPanel.Controls.Add(this.btnReset, 2, 0);
            this.controlTableLayoutPanel.Controls.Add(this.btnPause, 1, 0);
            this.controlTableLayoutPanel.Controls.Add(this.btnStart, 0, 0);
            this.controlTableLayoutPanel.Controls.Add(this.btnInit, 3, 0);
            this.controlTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.controlTableLayoutPanel.Location = new System.Drawing.Point(0, 32);
            this.controlTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.controlTableLayoutPanel.Name = "controlTableLayoutPanel";
            this.controlTableLayoutPanel.RowCount = 1;
            this.controlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.controlTableLayoutPanel.Size = new System.Drawing.Size(1069, 104);
            this.controlTableLayoutPanel.TabIndex = 0;
            this.controlTableLayoutPanel.TagString = null;
            // 
            // btnReset
            // 
            this.btnReset.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnReset.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnReset.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnReset.Location = new System.Drawing.Point(584, 20);
            this.btnReset.Margin = new System.Windows.Forms.Padding(50, 20, 50, 20);
            this.btnReset.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnReset.Name = "btnReset";
            this.btnReset.Size = new System.Drawing.Size(167, 64);
            this.btnReset.TabIndex = 10;
            this.btnReset.Text = "复位";
            this.btnReset.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPause
            // 
            this.btnPause.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPause.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPause.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPause.Location = new System.Drawing.Point(317, 20);
            this.btnPause.Margin = new System.Windows.Forms.Padding(50, 20, 50, 20);
            this.btnPause.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPause.Name = "btnPause";
            this.btnPause.Size = new System.Drawing.Size(167, 64);
            this.btnPause.TabIndex = 9;
            this.btnPause.Text = "暂停";
            this.btnPause.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnStart
            // 
            this.btnStart.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnStart.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnStart.Location = new System.Drawing.Point(50, 20);
            this.btnStart.Margin = new System.Windows.Forms.Padding(50, 20, 50, 20);
            this.btnStart.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(167, 64);
            this.btnStart.TabIndex = 5;
            this.btnStart.Text = "启动";
            this.btnStart.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnInit
            // 
            this.btnInit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInit.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnInit.Location = new System.Drawing.Point(851, 20);
            this.btnInit.Margin = new System.Windows.Forms.Padding(50, 20, 50, 20);
            this.btnInit.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnInit.Name = "btnInit";
            this.btnInit.Size = new System.Drawing.Size(168, 64);
            this.btnInit.TabIndex = 6;
            this.btnInit.Text = "初始化";
            this.btnInit.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // gbConfig
            // 
            this.gbConfig.Controls.Add(this.configTableLayoutPanel);
            this.gbConfig.Dock = System.Windows.Forms.DockStyle.Fill;
            this.gbConfig.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.gbConfig.Location = new System.Drawing.Point(1069, 0);
            this.gbConfig.Margin = new System.Windows.Forms.Padding(0);
            this.gbConfig.MinimumSize = new System.Drawing.Size(1, 1);
            this.gbConfig.Name = "gbConfig";
            this.gbConfig.Padding = new System.Windows.Forms.Padding(0, 32, 0, 0);
            this.gbConfig.Size = new System.Drawing.Size(577, 136);
            this.gbConfig.TabIndex = 10;
            this.gbConfig.Text = "配置";
            this.gbConfig.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // configTableLayoutPanel
            // 
            this.configTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.configTableLayoutPanel.ColumnCount = 2;
            this.configTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.configTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.configTableLayoutPanel.Controls.Add(this.btnTogglePauseOthers, 0, 0);
            this.configTableLayoutPanel.Controls.Add(this.btnToggleEnabled, 1, 0);
            this.configTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.configTableLayoutPanel.Location = new System.Drawing.Point(0, 32);
            this.configTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.configTableLayoutPanel.Name = "configTableLayoutPanel";
            this.configTableLayoutPanel.RowCount = 1;
            this.configTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.configTableLayoutPanel.Size = new System.Drawing.Size(577, 104);
            this.configTableLayoutPanel.TabIndex = 0;
            this.configTableLayoutPanel.TagString = null;
            // 
            // btnTogglePauseOthers
            // 
            this.btnTogglePauseOthers.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTogglePauseOthers.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTogglePauseOthers.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTogglePauseOthers.Location = new System.Drawing.Point(60, 20);
            this.btnTogglePauseOthers.Margin = new System.Windows.Forms.Padding(60, 20, 60, 20);
            this.btnTogglePauseOthers.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTogglePauseOthers.Name = "btnTogglePauseOthers";
            this.btnTogglePauseOthers.Size = new System.Drawing.Size(168, 64);
            this.btnTogglePauseOthers.TabIndex = 10;
            this.btnTogglePauseOthers.Text = "异停";
            this.btnTogglePauseOthers.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnToggleEnabled
            // 
            this.btnToggleEnabled.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnToggleEnabled.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnToggleEnabled.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnToggleEnabled.Location = new System.Drawing.Point(348, 20);
            this.btnToggleEnabled.Margin = new System.Windows.Forms.Padding(60, 20, 60, 20);
            this.btnToggleEnabled.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnToggleEnabled.Name = "btnToggleEnabled";
            this.btnToggleEnabled.Size = new System.Drawing.Size(169, 64);
            this.btnToggleEnabled.TabIndex = 9;
            this.btnToggleEnabled.Text = "是否启用";
            this.btnToggleEnabled.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // downTopPanel
            // 
            this.downTopPanel.Controls.Add(this.uiTableLayoutPanel3);
            this.downTopPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downTopPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.downTopPanel.Location = new System.Drawing.Point(4, 0);
            this.downTopPanel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.downTopPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.downTopPanel.Name = "downTopPanel";
            this.downTopPanel.Padding = new System.Windows.Forms.Padding(5);
            this.downTopPanel.Size = new System.Drawing.Size(1646, 48);
            this.downTopPanel.TabIndex = 14;
            this.downTopPanel.Text = null;
            this.downTopPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiTableLayoutPanel3
            // 
            this.uiTableLayoutPanel3.ColumnCount = 2;
            this.uiTableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.uiTableLayoutPanel3.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 92F));
            this.uiTableLayoutPanel3.Controls.Add(this.lblSelectedInfo, 1, 0);
            this.uiTableLayoutPanel3.Controls.Add(this.lblSelectedDesc, 0, 0);
            this.uiTableLayoutPanel3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTableLayoutPanel3.Location = new System.Drawing.Point(5, 5);
            this.uiTableLayoutPanel3.Margin = new System.Windows.Forms.Padding(0);
            this.uiTableLayoutPanel3.Name = "uiTableLayoutPanel3";
            this.uiTableLayoutPanel3.RowCount = 1;
            this.uiTableLayoutPanel3.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTableLayoutPanel3.Size = new System.Drawing.Size(1636, 38);
            this.uiTableLayoutPanel3.TabIndex = 0;
            this.uiTableLayoutPanel3.TagString = null;
            // 
            // lblSelectedInfo
            // 
            this.lblSelectedInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedInfo.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSelectedInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblSelectedInfo.Location = new System.Drawing.Point(132, 2);
            this.lblSelectedInfo.Margin = new System.Windows.Forms.Padding(2);
            this.lblSelectedInfo.Name = "lblSelectedInfo";
            this.lblSelectedInfo.Size = new System.Drawing.Size(1502, 34);
            this.lblSelectedInfo.TabIndex = 12;
            this.lblSelectedInfo.Text = "----";
            this.lblSelectedInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSelectedDesc
            // 
            this.lblSelectedDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSelectedDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSelectedDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblSelectedDesc.Location = new System.Drawing.Point(2, 2);
            this.lblSelectedDesc.Margin = new System.Windows.Forms.Padding(2);
            this.lblSelectedDesc.Name = "lblSelectedDesc";
            this.lblSelectedDesc.Size = new System.Drawing.Size(126, 34);
            this.lblSelectedDesc.TabIndex = 11;
            this.lblSelectedDesc.Text = "当前选中:";
            this.lblSelectedDesc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // downBottomPanel
            // 
            this.downBottomPanel.Controls.Add(this.uiTableLayoutPanel4);
            this.downBottomPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downBottomPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.downBottomPanel.Location = new System.Drawing.Point(4, 196);
            this.downBottomPanel.Margin = new System.Windows.Forms.Padding(4);
            this.downBottomPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.downBottomPanel.Name = "downBottomPanel";
            this.downBottomPanel.Padding = new System.Windows.Forms.Padding(5);
            this.downBottomPanel.Size = new System.Drawing.Size(1646, 41);
            this.downBottomPanel.TabIndex = 15;
            this.downBottomPanel.Text = null;
            this.downBottomPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // uiTableLayoutPanel4
            // 
            this.uiTableLayoutPanel4.ColumnCount = 2;
            this.uiTableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 8F));
            this.uiTableLayoutPanel4.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 92F));
            this.uiTableLayoutPanel4.Controls.Add(this.lblFeedback, 1, 0);
            this.uiTableLayoutPanel4.Controls.Add(this.lblFeedbackDesc, 0, 0);
            this.uiTableLayoutPanel4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTableLayoutPanel4.Location = new System.Drawing.Point(5, 5);
            this.uiTableLayoutPanel4.Margin = new System.Windows.Forms.Padding(0);
            this.uiTableLayoutPanel4.Name = "uiTableLayoutPanel4";
            this.uiTableLayoutPanel4.RowCount = 1;
            this.uiTableLayoutPanel4.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.uiTableLayoutPanel4.Size = new System.Drawing.Size(1636, 31);
            this.uiTableLayoutPanel4.TabIndex = 1;
            this.uiTableLayoutPanel4.TagString = null;
            // 
            // lblFeedback
            // 
            this.lblFeedback.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFeedback.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblFeedback.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblFeedback.Location = new System.Drawing.Point(132, 2);
            this.lblFeedback.Margin = new System.Windows.Forms.Padding(2);
            this.lblFeedback.Name = "lblFeedback";
            this.lblFeedback.Size = new System.Drawing.Size(1502, 27);
            this.lblFeedback.TabIndex = 10;
            this.lblFeedback.Text = "---";
            this.lblFeedback.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblFeedbackDesc
            // 
            this.lblFeedbackDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblFeedbackDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblFeedbackDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblFeedbackDesc.Location = new System.Drawing.Point(2, 2);
            this.lblFeedbackDesc.Margin = new System.Windows.Forms.Padding(2);
            this.lblFeedbackDesc.Name = "lblFeedbackDesc";
            this.lblFeedbackDesc.Size = new System.Drawing.Size(126, 27);
            this.lblFeedbackDesc.TabIndex = 9;
            this.lblFeedbackDesc.Text = "操作反馈:";
            this.lblFeedbackDesc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // mainTableLayoutPanel
            // 
            this.mainTableLayoutPanel.ColumnCount = 1;
            this.mainTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.mainTableLayoutPanel.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.bottomTableLayoutPanel, 0, 2);
            this.mainTableLayoutPanel.Controls.Add(this.midTableLayoutPanel, 0, 1);
            this.mainTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mainTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.RowCount = 3;
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12.38095F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 63.80952F));
            this.mainTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 23.80952F));
            this.mainTableLayoutPanel.Size = new System.Drawing.Size(1660, 1032);
            this.mainTableLayoutPanel.TabIndex = 0;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 3;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.topTableLayoutPanel.Controls.Add(this.topLeftPanel, 0, 0);
            this.topTableLayoutPanel.Controls.Add(this.topMidPanel, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.topRightPanel, 2, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 1;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(1660, 127);
            this.topTableLayoutPanel.TabIndex = 3;
            this.topTableLayoutPanel.TagString = null;
            // 
            // topLeftPanel
            // 
            this.topLeftPanel.Controls.Add(this.sysStateTableLayoutPanel);
            this.topLeftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLeftPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topLeftPanel.Location = new System.Drawing.Point(4, 5);
            this.topLeftPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topLeftPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topLeftPanel.Name = "topLeftPanel";
            this.topLeftPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topLeftPanel.Size = new System.Drawing.Size(324, 117);
            this.topLeftPanel.TabIndex = 0;
            this.topLeftPanel.Text = null;
            this.topLeftPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // sysStateTableLayoutPanel
            // 
            this.sysStateTableLayoutPanel.ColumnCount = 1;
            this.sysStateTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sysStateTableLayoutPanel.Controls.Add(this.lblSystemStateDesc, 0, 0);
            this.sysStateTableLayoutPanel.Controls.Add(this.lblSystemState, 0, 1);
            this.sysStateTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.sysStateTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.sysStateTableLayoutPanel.Name = "sysStateTableLayoutPanel";
            this.sysStateTableLayoutPanel.RowCount = 2;
            this.sysStateTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sysStateTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.sysStateTableLayoutPanel.Size = new System.Drawing.Size(314, 107);
            this.sysStateTableLayoutPanel.TabIndex = 0;
            this.sysStateTableLayoutPanel.TagString = null;
            // 
            // lblSystemStateDesc
            // 
            this.lblSystemStateDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSystemStateDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblSystemStateDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblSystemStateDesc.Location = new System.Drawing.Point(20, 3);
            this.lblSystemStateDesc.Margin = new System.Windows.Forms.Padding(20, 3, 3, 3);
            this.lblSystemStateDesc.Name = "lblSystemStateDesc";
            this.lblSystemStateDesc.Size = new System.Drawing.Size(291, 47);
            this.lblSystemStateDesc.TabIndex = 4;
            this.lblSystemStateDesc.Text = "系统状态:";
            this.lblSystemStateDesc.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblSystemState
            // 
            this.lblSystemState.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblSystemState.Font = new System.Drawing.Font("微软雅黑", 14F, System.Drawing.FontStyle.Bold);
            this.lblSystemState.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblSystemState.Location = new System.Drawing.Point(20, 56);
            this.lblSystemState.Margin = new System.Windows.Forms.Padding(20, 3, 3, 3);
            this.lblSystemState.Name = "lblSystemState";
            this.lblSystemState.Size = new System.Drawing.Size(291, 48);
            this.lblSystemState.TabIndex = 3;
            this.lblSystemState.Text = "--";
            this.lblSystemState.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // topMidPanel
            // 
            this.topMidPanel.Controls.Add(this.btnInitAll);
            this.topMidPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topMidPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topMidPanel.Location = new System.Drawing.Point(336, 5);
            this.topMidPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topMidPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topMidPanel.Name = "topMidPanel";
            this.topMidPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topMidPanel.Size = new System.Drawing.Size(324, 117);
            this.topMidPanel.TabIndex = 1;
            this.topMidPanel.Text = null;
            this.topMidPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnInitAll
            // 
            this.btnInitAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnInitAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnInitAll.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnInitAll.Location = new System.Drawing.Point(5, 5);
            this.btnInitAll.Margin = new System.Windows.Forms.Padding(15);
            this.btnInitAll.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnInitAll.Name = "btnInitAll";
            this.btnInitAll.Size = new System.Drawing.Size(314, 107);
            this.btnInitAll.TabIndex = 0;
            this.btnInitAll.Text = "整机初始化";
            this.btnInitAll.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // topRightPanel
            // 
            this.topRightPanel.Controls.Add(this.autoManulTableLayoutPanel);
            this.topRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topRightPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topRightPanel.Location = new System.Drawing.Point(668, 5);
            this.topRightPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topRightPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topRightPanel.Name = "topRightPanel";
            this.topRightPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topRightPanel.Size = new System.Drawing.Size(988, 117);
            this.topRightPanel.TabIndex = 2;
            this.topRightPanel.Text = null;
            this.topRightPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // autoManulTableLayoutPanel
            // 
            this.autoManulTableLayoutPanel.ColumnCount = 6;
            this.autoManulTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.autoManulTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.autoManulTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.autoManulTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.autoManulTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.autoManulTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.autoManulTableLayoutPanel.Controls.Add(this.lblIntervalUnit, 4, 0);
            this.autoManulTableLayoutPanel.Controls.Add(this.upDownInterval, 3, 0);
            this.autoManulTableLayoutPanel.Controls.Add(this.switchAutoRefresh, 1, 0);
            this.autoManulTableLayoutPanel.Controls.Add(this.lblAutoRefresh, 0, 0);
            this.autoManulTableLayoutPanel.Controls.Add(this.lblManulRefresh, 0, 1);
            this.autoManulTableLayoutPanel.Controls.Add(this.btnManulRefresh, 1, 1);
            this.autoManulTableLayoutPanel.Controls.Add(this.cmbPageSize, 3, 1);
            this.autoManulTableLayoutPanel.Controls.Add(this.lblInterval, 2, 0);
            this.autoManulTableLayoutPanel.Controls.Add(this.lblPageSize, 2, 1);
            this.autoManulTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.autoManulTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.autoManulTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.autoManulTableLayoutPanel.Name = "autoManulTableLayoutPanel";
            this.autoManulTableLayoutPanel.RowCount = 2;
            this.autoManulTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.autoManulTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.autoManulTableLayoutPanel.Size = new System.Drawing.Size(978, 107);
            this.autoManulTableLayoutPanel.TabIndex = 0;
            this.autoManulTableLayoutPanel.TagString = null;
            // 
            // lblIntervalUnit
            // 
            this.lblIntervalUnit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblIntervalUnit.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblIntervalUnit.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblIntervalUnit.Location = new System.Drawing.Point(587, 3);
            this.lblIntervalUnit.Margin = new System.Windows.Forms.Padding(3);
            this.lblIntervalUnit.Name = "lblIntervalUnit";
            this.lblIntervalUnit.Size = new System.Drawing.Size(91, 47);
            this.lblIntervalUnit.TabIndex = 18;
            this.lblIntervalUnit.Text = "ms";
            this.lblIntervalUnit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // upDownInterval
            // 
            this.upDownInterval.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.upDownInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.upDownInterval.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.upDownInterval.Location = new System.Drawing.Point(404, 5);
            this.upDownInterval.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.upDownInterval.MinimumSize = new System.Drawing.Size(1, 16);
            this.upDownInterval.Name = "upDownInterval";
            this.upDownInterval.Padding = new System.Windows.Forms.Padding(5);
            this.upDownInterval.ShowText = false;
            this.upDownInterval.Size = new System.Drawing.Size(165, 43);
            this.upDownInterval.TabIndex = 16;
            this.upDownInterval.Text = "0";
            this.upDownInterval.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // switchAutoRefresh
            // 
            this.switchAutoRefresh.Active = true;
            this.switchAutoRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.switchAutoRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.switchAutoRefresh.Location = new System.Drawing.Point(112, 5);
            this.switchAutoRefresh.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.switchAutoRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.switchAutoRefresh.Name = "switchAutoRefresh";
            this.switchAutoRefresh.Size = new System.Drawing.Size(165, 43);
            this.switchAutoRefresh.SwitchShape = Sunny.UI.UISwitch.UISwitchShape.Square;
            this.switchAutoRefresh.TabIndex = 13;
            this.switchAutoRefresh.Text = "uiSwitch1";
            // 
            // lblAutoRefresh
            // 
            this.lblAutoRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblAutoRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblAutoRefresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblAutoRefresh.Location = new System.Drawing.Point(3, 3);
            this.lblAutoRefresh.Margin = new System.Windows.Forms.Padding(3);
            this.lblAutoRefresh.Name = "lblAutoRefresh";
            this.lblAutoRefresh.Size = new System.Drawing.Size(91, 47);
            this.lblAutoRefresh.TabIndex = 4;
            this.lblAutoRefresh.Text = "自动刷新";
            this.lblAutoRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblManulRefresh
            // 
            this.lblManulRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblManulRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblManulRefresh.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblManulRefresh.Location = new System.Drawing.Point(3, 56);
            this.lblManulRefresh.Margin = new System.Windows.Forms.Padding(3);
            this.lblManulRefresh.Name = "lblManulRefresh";
            this.lblManulRefresh.Size = new System.Drawing.Size(91, 48);
            this.lblManulRefresh.TabIndex = 11;
            this.lblManulRefresh.Text = "手动刷新";
            this.lblManulRefresh.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnManulRefresh
            // 
            this.btnManulRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnManulRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnManulRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnManulRefresh.Location = new System.Drawing.Point(112, 58);
            this.btnManulRefresh.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.btnManulRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnManulRefresh.Name = "btnManulRefresh";
            this.btnManulRefresh.Size = new System.Drawing.Size(165, 44);
            this.btnManulRefresh.TabIndex = 12;
            this.btnManulRefresh.Text = "手动刷新";
            this.btnManulRefresh.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // cmbPageSize
            // 
            this.cmbPageSize.DataSource = null;
            this.cmbPageSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPageSize.DropDownStyle = Sunny.UI.UIDropDownStyle.DropDownList;
            this.cmbPageSize.FillColor = System.Drawing.Color.White;
            this.cmbPageSize.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbPageSize.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbPageSize.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbPageSize.Location = new System.Drawing.Point(404, 58);
            this.cmbPageSize.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbPageSize.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbPageSize.Name = "cmbPageSize";
            this.cmbPageSize.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbPageSize.Size = new System.Drawing.Size(165, 44);
            this.cmbPageSize.SymbolSize = 24;
            this.cmbPageSize.TabIndex = 17;
            this.cmbPageSize.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbPageSize.Watermark = "";
            // 
            // lblInterval
            // 
            this.lblInterval.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblInterval.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblInterval.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblInterval.Location = new System.Drawing.Point(295, 3);
            this.lblInterval.Margin = new System.Windows.Forms.Padding(3);
            this.lblInterval.Name = "lblInterval";
            this.lblInterval.Size = new System.Drawing.Size(91, 47);
            this.lblInterval.TabIndex = 14;
            this.lblInterval.Text = "刷新间隔";
            this.lblInterval.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblPageSize
            // 
            this.lblPageSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageSize.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPageSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblPageSize.Location = new System.Drawing.Point(295, 56);
            this.lblPageSize.Margin = new System.Windows.Forms.Padding(3);
            this.lblPageSize.Name = "lblPageSize";
            this.lblPageSize.Size = new System.Drawing.Size(91, 48);
            this.lblPageSize.TabIndex = 15;
            this.lblPageSize.Text = "每页";
            this.lblPageSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // midTableLayoutPanel
            // 
            this.midTableLayoutPanel.ColumnCount = 1;
            this.midTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.midTableLayoutPanel.Controls.Add(this.paginationProcess, 0, 1);
            this.midTableLayoutPanel.Controls.Add(this.dgvProcesses, 0, 0);
            this.midTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.midTableLayoutPanel.Location = new System.Drawing.Point(0, 127);
            this.midTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.midTableLayoutPanel.Name = "midTableLayoutPanel";
            this.midTableLayoutPanel.Padding = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.midTableLayoutPanel.RowCount = 2;
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 93F));
            this.midTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.midTableLayoutPanel.Size = new System.Drawing.Size(1660, 658);
            this.midTableLayoutPanel.TabIndex = 4;
            this.midTableLayoutPanel.TagString = null;
            // 
            // dgvProcesses
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvProcesses.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvProcesses.BackgroundColor = System.Drawing.Color.White;
            this.dgvProcesses.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dgvProcesses.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvProcesses.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvProcesses.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvProcesses.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvProcesses.EnableHeadersVisualStyles = false;
            this.dgvProcesses.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvProcesses.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvProcesses.Location = new System.Drawing.Point(5, 3);
            this.dgvProcesses.Name = "dgvProcesses";
            this.dgvProcesses.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvProcesses.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvProcesses.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvProcesses.RowTemplate.Height = 23;
            this.dgvProcesses.SelectedIndex = -1;
            this.dgvProcesses.Size = new System.Drawing.Size(1650, 605);
            this.dgvProcesses.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvProcesses.TabIndex = 1;
            // 
            // paginationProcess
            // 
            this.paginationProcess.BackColor = System.Drawing.Color.White;
            this.paginationProcess.Dock = System.Windows.Forms.DockStyle.Fill;
            this.paginationProcess.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.paginationProcess.Location = new System.Drawing.Point(5, 616);
            this.paginationProcess.Margin = new System.Windows.Forms.Padding(3, 5, 3, 5);
            this.paginationProcess.MinimumSize = new System.Drawing.Size(1, 1);
            this.paginationProcess.Name = "paginationProcess";
            this.paginationProcess.NavButtonFont = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.paginationProcess.PageButtonFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.paginationProcess.SelectedBackColor = System.Drawing.Color.White;
            this.paginationProcess.SelectedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.paginationProcess.SelectedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.paginationProcess.Size = new System.Drawing.Size(1650, 37);
            this.paginationProcess.TabIndex = 2;
            // 
            // FormProcessMonitor
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1660, 1032);
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F);
            this.Name = "FormProcessMonitor";
            this.Text = "FormProcessManagement";
            this.TitleFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.bottomTableLayoutPanel.ResumeLayout(false);
            this.downMidTableLayoutPanel.ResumeLayout(false);
            this.gbControl.ResumeLayout(false);
            this.controlTableLayoutPanel.ResumeLayout(false);
            this.gbConfig.ResumeLayout(false);
            this.configTableLayoutPanel.ResumeLayout(false);
            this.downTopPanel.ResumeLayout(false);
            this.uiTableLayoutPanel3.ResumeLayout(false);
            this.downBottomPanel.ResumeLayout(false);
            this.uiTableLayoutPanel4.ResumeLayout(false);
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.topLeftPanel.ResumeLayout(false);
            this.sysStateTableLayoutPanel.ResumeLayout(false);
            this.topMidPanel.ResumeLayout(false);
            this.topRightPanel.ResumeLayout(false);
            this.autoManulTableLayoutPanel.ResumeLayout(false);
            this.midTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvProcesses)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private Sunny.UI.UITableLayoutPanel bottomTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel downMidTableLayoutPanel;
        private Sunny.UI.UIGroupBox gbControl;
        private Sunny.UI.UITableLayoutPanel controlTableLayoutPanel;
        private Sunny.UI.UIButton btnReset;
        private Sunny.UI.UIButton btnPause;
        private Sunny.UI.UIButton btnStart;
        private Sunny.UI.UIButton btnInit;
        private Sunny.UI.UIGroupBox gbConfig;
        private Sunny.UI.UITableLayoutPanel configTableLayoutPanel;
        private Sunny.UI.UIButton btnTogglePauseOthers;
        private Sunny.UI.UIButton btnToggleEnabled;
        private Sunny.UI.UIPanel downTopPanel;
        private Sunny.UI.UIPanel downBottomPanel;
        private System.Windows.Forms.TableLayoutPanel mainTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UIPanel topLeftPanel;
        private Sunny.UI.UITableLayoutPanel sysStateTableLayoutPanel;
        private Sunny.UI.UIPanel topMidPanel;
        private Sunny.UI.UIButton btnInitAll;
        private Sunny.UI.UILabel lblSystemStateDesc;
        private Sunny.UI.UILabel lblSystemState;
        private Sunny.UI.UIPanel topRightPanel;
        private Sunny.UI.UITableLayoutPanel autoManulTableLayoutPanel;
        private Sunny.UI.UILabel lblAutoRefresh;
        private Sunny.UI.UILabel lblManulRefresh;
        private Sunny.UI.UISwitch switchAutoRefresh;
        private Sunny.UI.UIButton btnManulRefresh;
        private Sunny.UI.UILabel lblPageSize;
        private Sunny.UI.UILabel lblInterval;
        private Sunny.UI.UIIntegerUpDown upDownInterval;
        private Sunny.UI.UIComboBox cmbPageSize;
        private Sunny.UI.UILabel lblIntervalUnit;
        private Sunny.UI.UITableLayoutPanel uiTableLayoutPanel3;
        private Sunny.UI.UILabel lblSelectedDesc;
        private Sunny.UI.UILabel lblSelectedInfo;
        private Sunny.UI.UITableLayoutPanel uiTableLayoutPanel4;
        private Sunny.UI.UILabel lblFeedbackDesc;
        private Sunny.UI.UILabel lblFeedback;
        private Sunny.UI.UITableLayoutPanel midTableLayoutPanel;
        private Sunny.UI.UIDataGridView dgvProcesses;
        private Controls.Pagination paginationProcess;
    }
}