namespace Taoyl.UI.Pages
{
    partial class FormPLCDebug
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle5 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle6 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle7 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle8 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle9 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle10 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle11 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle12 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle13 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle14 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle15 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.leftTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.leftTopTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblPlcList = new Sunny.UI.UILabel();
            this.dgvPLCList = new Sunny.UI.UIDataGridView();
            this.uiTabControl1 = new Sunny.UI.UITabControl();
            this.tabConfig = new System.Windows.Forms.TabPage();
            this.plcConfigTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.pnlConfigContent1 = new System.Windows.Forms.Panel();
            this.plcControlTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnPlcDisConn = new Sunny.UI.UIButton();
            this.btnPlcAdd = new Sunny.UI.UIButton();
            this.btnPlcConn = new Sunny.UI.UIButton();
            this.btnPlcEdit = new Sunny.UI.UIButton();
            this.btnPlcSave = new Sunny.UI.UIButton();
            this.btnPlcCancel = new Sunny.UI.UIButton();
            this.btnPlcDel = new Sunny.UI.UIButton();
            this.btnPlcRebuild = new Sunny.UI.UIButton();
            this.tabTagTable = new System.Windows.Forms.TabPage();
            this.TagTableTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.midTopTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblQueueDepth = new Sunny.UI.UILabel();
            this.lblQueueDepthDesc = new Sunny.UI.UILabel();
            this.btnPlcLoadIn = new Sunny.UI.UIButton();
            this.btnPlcExport = new Sunny.UI.UIButton();
            this.lblRefreshFreq = new Sunny.UI.UILabel();
            this.txtRefreshFreq = new Sunny.UI.UITextBox();
            this.btnSetCurrent = new Sunny.UI.UIButton();
            this.btnSetAll = new Sunny.UI.UIButton();
            this.TagControlTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.tagItemRadioButton = new Sunny.UI.UIRadioButton();
            this.btnTagEdit = new Sunny.UI.UISymbolButton();
            this.btnTagCancel = new Sunny.UI.UISymbolButton();
            this.btnTagSave = new Sunny.UI.UISymbolButton();
            this.btnTagAdd = new Sunny.UI.UISymbolButton();
            this.btnTagWrite = new Sunny.UI.UISymbolButton();
            this.btnTagRead = new Sunny.UI.UISymbolButton();
            this.btnTagDel = new Sunny.UI.UISymbolButton();
            this.btnTagDown = new Sunny.UI.UISymbolButton();
            this.btnTagUpper = new Sunny.UI.UISymbolButton();
            this.groupItemRadioButton = new Sunny.UI.UIRadioButton();
            this.TagTableSplitContainer = new Sunny.UI.UISplitContainer();
            this.dgvGroupList = new Sunny.UI.UIDataGridView();
            this.dgvTagList = new Sunny.UI.UIDataGridView();
            this.rootTableLayoutPanel.SuspendLayout();
            this.leftTableLayoutPanel.SuspendLayout();
            this.leftTopTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvPLCList)).BeginInit();
            this.uiTabControl1.SuspendLayout();
            this.tabConfig.SuspendLayout();
            this.plcConfigTableLayoutPanel.SuspendLayout();
            this.plcControlTableLayoutPanel.SuspendLayout();
            this.tabTagTable.SuspendLayout();
            this.TagTableTableLayoutPanel.SuspendLayout();
            this.midTopTableLayoutPanel.SuspendLayout();
            this.TagControlTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TagTableSplitContainer)).BeginInit();
            this.TagTableSplitContainer.Panel1.SuspendLayout();
            this.TagTableSplitContainer.Panel2.SuspendLayout();
            this.TagTableSplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvGroupList)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTagList)).BeginInit();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.rootTableLayoutPanel.ColumnCount = 2;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.rootTableLayoutPanel.Controls.Add(this.leftTableLayoutPanel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.uiTabControl1, 1, 0);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 1;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(1940, 1220);
            this.rootTableLayoutPanel.TabIndex = 0;
            this.rootTableLayoutPanel.TagString = null;
            // 
            // leftTableLayoutPanel
            // 
            this.leftTableLayoutPanel.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.leftTableLayoutPanel.ColumnCount = 1;
            this.leftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftTableLayoutPanel.Controls.Add(this.leftTopTableLayoutPanel, 0, 0);
            this.leftTableLayoutPanel.Controls.Add(this.dgvPLCList, 0, 1);
            this.leftTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.leftTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.leftTableLayoutPanel.Name = "leftTableLayoutPanel";
            this.leftTableLayoutPanel.RowCount = 2;
            this.leftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.leftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95F));
            this.leftTableLayoutPanel.Size = new System.Drawing.Size(388, 1220);
            this.leftTableLayoutPanel.Style = Sunny.UI.UIStyle.Custom;
            this.leftTableLayoutPanel.TabIndex = 0;
            this.leftTableLayoutPanel.TagString = null;
            // 
            // leftTopTableLayoutPanel
            // 
            this.leftTopTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.leftTopTableLayoutPanel.ColumnCount = 1;
            this.leftTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.leftTopTableLayoutPanel.Controls.Add(this.lblPlcList, 0, 0);
            this.leftTopTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.leftTopTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.leftTopTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.leftTopTableLayoutPanel.Name = "leftTopTableLayoutPanel";
            this.leftTopTableLayoutPanel.RowCount = 1;
            this.leftTopTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.leftTopTableLayoutPanel.Size = new System.Drawing.Size(388, 61);
            this.leftTopTableLayoutPanel.Style = Sunny.UI.UIStyle.Custom;
            this.leftTopTableLayoutPanel.TabIndex = 0;
            this.leftTopTableLayoutPanel.TagString = null;
            // 
            // lblPlcList
            // 
            this.lblPlcList.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.lblPlcList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPlcList.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPlcList.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblPlcList.Location = new System.Drawing.Point(3, 3);
            this.lblPlcList.Margin = new System.Windows.Forms.Padding(3);
            this.lblPlcList.Name = "lblPlcList";
            this.lblPlcList.Size = new System.Drawing.Size(382, 55);
            this.lblPlcList.TabIndex = 12;
            this.lblPlcList.Text = "PLC列表";
            this.lblPlcList.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // dgvPLCList
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvPLCList.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvPLCList.BackgroundColor = System.Drawing.Color.White;
            this.dgvPLCList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvPLCList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvPLCList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvPLCList.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvPLCList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvPLCList.EnableHeadersVisualStyles = false;
            this.dgvPLCList.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvPLCList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvPLCList.Location = new System.Drawing.Point(0, 61);
            this.dgvPLCList.Margin = new System.Windows.Forms.Padding(0);
            this.dgvPLCList.Name = "dgvPLCList";
            this.dgvPLCList.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvPLCList.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvPLCList.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvPLCList.RowTemplate.Height = 23;
            this.dgvPLCList.SelectedIndex = -1;
            this.dgvPLCList.Size = new System.Drawing.Size(388, 1159);
            this.dgvPLCList.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvPLCList.TabIndex = 1;
            // 
            // uiTabControl1
            // 
            this.uiTabControl1.Controls.Add(this.tabConfig);
            this.uiTabControl1.Controls.Add(this.tabTagTable);
            this.uiTabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.uiTabControl1.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.uiTabControl1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.uiTabControl1.ItemSize = new System.Drawing.Size(150, 50);
            this.uiTabControl1.Location = new System.Drawing.Point(388, 0);
            this.uiTabControl1.MainPage = "";
            this.uiTabControl1.Margin = new System.Windows.Forms.Padding(0);
            this.uiTabControl1.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.uiTabControl1.Name = "uiTabControl1";
            this.uiTabControl1.SelectedIndex = 0;
            this.uiTabControl1.Size = new System.Drawing.Size(1552, 1220);
            this.uiTabControl1.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.uiTabControl1.TabBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.uiTabControl1.TabIndex = 1;
            this.uiTabControl1.TabPageTextAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            this.uiTabControl1.TabSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.uiTabControl1.TabUnSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.uiTabControl1.TabUnSelectedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(25)))), ((int)(((byte)(100)))));
            this.uiTabControl1.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // tabConfig
            // 
            this.tabConfig.Controls.Add(this.plcConfigTableLayoutPanel);
            this.tabConfig.Location = new System.Drawing.Point(0, 50);
            this.tabConfig.Name = "tabConfig";
            this.tabConfig.Size = new System.Drawing.Size(1552, 1170);
            this.tabConfig.TabIndex = 1;
            this.tabConfig.Text = "连接配置";
            this.tabConfig.UseVisualStyleBackColor = true;
            // 
            // plcConfigTableLayoutPanel
            // 
            this.plcConfigTableLayoutPanel.ColumnCount = 1;
            this.plcConfigTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.plcConfigTableLayoutPanel.Controls.Add(this.pnlConfigContent1, 0, 1);
            this.plcConfigTableLayoutPanel.Controls.Add(this.plcControlTableLayoutPanel, 0, 0);
            this.plcConfigTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plcConfigTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.plcConfigTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.plcConfigTableLayoutPanel.Name = "plcConfigTableLayoutPanel";
            this.plcConfigTableLayoutPanel.RowCount = 2;
            this.plcConfigTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.plcConfigTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 93F));
            this.plcConfigTableLayoutPanel.Size = new System.Drawing.Size(1552, 1170);
            this.plcConfigTableLayoutPanel.TabIndex = 0;
            this.plcConfigTableLayoutPanel.TagString = null;
            // 
            // pnlConfigContent1
            // 
            this.pnlConfigContent1.AutoScroll = true;
            this.pnlConfigContent1.BackColor = System.Drawing.Color.White;
            this.pnlConfigContent1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlConfigContent1.Location = new System.Drawing.Point(0, 81);
            this.pnlConfigContent1.Margin = new System.Windows.Forms.Padding(0);
            this.pnlConfigContent1.Name = "pnlConfigContent1";
            this.pnlConfigContent1.Size = new System.Drawing.Size(1552, 1089);
            this.pnlConfigContent1.TabIndex = 4;
            // 
            // plcControlTableLayoutPanel
            // 
            this.plcControlTableLayoutPanel.ColumnCount = 9;
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.plcControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 4F));
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcDisConn, 6, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcAdd, 0, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcConn, 5, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcEdit, 1, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcSave, 2, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcCancel, 3, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcDel, 4, 0);
            this.plcControlTableLayoutPanel.Controls.Add(this.btnPlcRebuild, 7, 0);
            this.plcControlTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.plcControlTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.plcControlTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.plcControlTableLayoutPanel.Name = "plcControlTableLayoutPanel";
            this.plcControlTableLayoutPanel.RowCount = 1;
            this.plcControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.plcControlTableLayoutPanel.Size = new System.Drawing.Size(1552, 81);
            this.plcControlTableLayoutPanel.TabIndex = 5;
            this.plcControlTableLayoutPanel.TagString = null;
            // 
            // btnPlcDisConn
            // 
            this.btnPlcDisConn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcDisConn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcDisConn.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcDisConn.Location = new System.Drawing.Point(1121, 5);
            this.btnPlcDisConn.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcDisConn.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcDisConn.Name = "btnPlcDisConn";
            this.btnPlcDisConn.Size = new System.Drawing.Size(176, 71);
            this.btnPlcDisConn.TabIndex = 13;
            this.btnPlcDisConn.Text = "断开";
            this.btnPlcDisConn.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcAdd
            // 
            this.btnPlcAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcAdd.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcAdd.Location = new System.Drawing.Point(5, 5);
            this.btnPlcAdd.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcAdd.Name = "btnPlcAdd";
            this.btnPlcAdd.Size = new System.Drawing.Size(176, 71);
            this.btnPlcAdd.TabIndex = 5;
            this.btnPlcAdd.Text = "新增";
            this.btnPlcAdd.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcConn
            // 
            this.btnPlcConn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcConn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcConn.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcConn.Location = new System.Drawing.Point(935, 5);
            this.btnPlcConn.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcConn.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcConn.Name = "btnPlcConn";
            this.btnPlcConn.Size = new System.Drawing.Size(176, 71);
            this.btnPlcConn.TabIndex = 10;
            this.btnPlcConn.Text = "连接";
            this.btnPlcConn.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcEdit
            // 
            this.btnPlcEdit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcEdit.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcEdit.Location = new System.Drawing.Point(191, 5);
            this.btnPlcEdit.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcEdit.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcEdit.Name = "btnPlcEdit";
            this.btnPlcEdit.Size = new System.Drawing.Size(176, 71);
            this.btnPlcEdit.TabIndex = 6;
            this.btnPlcEdit.Text = "编辑";
            this.btnPlcEdit.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcSave
            // 
            this.btnPlcSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcSave.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcSave.Location = new System.Drawing.Point(377, 5);
            this.btnPlcSave.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcSave.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcSave.Name = "btnPlcSave";
            this.btnPlcSave.Size = new System.Drawing.Size(176, 71);
            this.btnPlcSave.TabIndex = 7;
            this.btnPlcSave.Text = "保存";
            this.btnPlcSave.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcCancel
            // 
            this.btnPlcCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcCancel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcCancel.Location = new System.Drawing.Point(563, 5);
            this.btnPlcCancel.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcCancel.Name = "btnPlcCancel";
            this.btnPlcCancel.Size = new System.Drawing.Size(176, 71);
            this.btnPlcCancel.TabIndex = 8;
            this.btnPlcCancel.Text = "取消";
            this.btnPlcCancel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcDel
            // 
            this.btnPlcDel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcDel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcDel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcDel.Location = new System.Drawing.Point(749, 5);
            this.btnPlcDel.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcDel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcDel.Name = "btnPlcDel";
            this.btnPlcDel.Size = new System.Drawing.Size(176, 71);
            this.btnPlcDel.TabIndex = 9;
            this.btnPlcDel.Text = "删除";
            this.btnPlcDel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcRebuild
            // 
            this.btnPlcRebuild.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcRebuild.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcRebuild.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcRebuild.Location = new System.Drawing.Point(1307, 5);
            this.btnPlcRebuild.Margin = new System.Windows.Forms.Padding(5);
            this.btnPlcRebuild.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcRebuild.Name = "btnPlcRebuild";
            this.btnPlcRebuild.Size = new System.Drawing.Size(176, 71);
            this.btnPlcRebuild.TabIndex = 12;
            this.btnPlcRebuild.Text = "重建";
            this.btnPlcRebuild.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // tabTagTable
            // 
            this.tabTagTable.Controls.Add(this.TagTableTableLayoutPanel);
            this.tabTagTable.Location = new System.Drawing.Point(0, 50);
            this.tabTagTable.Name = "tabTagTable";
            this.tabTagTable.Size = new System.Drawing.Size(1552, 1170);
            this.tabTagTable.TabIndex = 0;
            this.tabTagTable.Text = "地址表";
            this.tabTagTable.UseVisualStyleBackColor = true;
            // 
            // TagTableTableLayoutPanel
            // 
            this.TagTableTableLayoutPanel.BackColor = System.Drawing.Color.White;
            this.TagTableTableLayoutPanel.ColumnCount = 2;
            this.TagTableTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 89F));
            this.TagTableTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11F));
            this.TagTableTableLayoutPanel.Controls.Add(this.midTopTableLayoutPanel, 0, 0);
            this.TagTableTableLayoutPanel.Controls.Add(this.TagControlTableLayoutPanel, 1, 1);
            this.TagTableTableLayoutPanel.Controls.Add(this.TagTableSplitContainer, 0, 1);
            this.TagTableTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TagTableTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.TagTableTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.TagTableTableLayoutPanel.Name = "TagTableTableLayoutPanel";
            this.TagTableTableLayoutPanel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 0);
            this.TagTableTableLayoutPanel.RowCount = 2;
            this.TagTableTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.TagTableTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 95F));
            this.TagTableTableLayoutPanel.Size = new System.Drawing.Size(1552, 1170);
            this.TagTableTableLayoutPanel.TabIndex = 0;
            this.TagTableTableLayoutPanel.TagString = null;
            // 
            // midTopTableLayoutPanel
            // 
            this.midTopTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.midTopTableLayoutPanel.ColumnCount = 9;
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 25F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTopTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.midTopTableLayoutPanel.Controls.Add(this.lblQueueDepth, 5, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.lblQueueDepthDesc, 4, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.btnPlcLoadIn, 7, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.btnPlcExport, 8, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.lblRefreshFreq, 0, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.txtRefreshFreq, 1, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.btnSetCurrent, 2, 0);
            this.midTopTableLayoutPanel.Controls.Add(this.btnSetAll, 3, 0);
            this.midTopTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.midTopTableLayoutPanel.Location = new System.Drawing.Point(0, 2);
            this.midTopTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.midTopTableLayoutPanel.Name = "midTopTableLayoutPanel";
            this.midTopTableLayoutPanel.RowCount = 1;
            this.midTopTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.midTopTableLayoutPanel.Size = new System.Drawing.Size(1381, 58);
            this.midTopTableLayoutPanel.TabIndex = 4;
            this.midTopTableLayoutPanel.TagString = null;
            // 
            // lblQueueDepth
            // 
            this.lblQueueDepth.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQueueDepth.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblQueueDepth.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblQueueDepth.Location = new System.Drawing.Point(679, 3);
            this.lblQueueDepth.Margin = new System.Windows.Forms.Padding(3);
            this.lblQueueDepth.Name = "lblQueueDepth";
            this.lblQueueDepth.Size = new System.Drawing.Size(76, 52);
            this.lblQueueDepth.TabIndex = 16;
            this.lblQueueDepth.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblQueueDepthDesc
            // 
            this.lblQueueDepthDesc.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblQueueDepthDesc.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblQueueDepthDesc.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblQueueDepthDesc.Location = new System.Drawing.Point(555, 3);
            this.lblQueueDepthDesc.Margin = new System.Windows.Forms.Padding(3);
            this.lblQueueDepthDesc.Name = "lblQueueDepthDesc";
            this.lblQueueDepthDesc.Size = new System.Drawing.Size(118, 52);
            this.lblQueueDepthDesc.TabIndex = 15;
            this.lblQueueDepthDesc.Text = "队列深度:";
            this.lblQueueDepthDesc.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnPlcLoadIn
            // 
            this.btnPlcLoadIn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcLoadIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcLoadIn.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcLoadIn.Location = new System.Drawing.Point(1106, 3);
            this.btnPlcLoadIn.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcLoadIn.Name = "btnPlcLoadIn";
            this.btnPlcLoadIn.Size = new System.Drawing.Size(132, 52);
            this.btnPlcLoadIn.TabIndex = 13;
            this.btnPlcLoadIn.Text = "导入";
            this.btnPlcLoadIn.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPlcExport
            // 
            this.btnPlcExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPlcExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPlcExport.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPlcExport.Location = new System.Drawing.Point(1244, 3);
            this.btnPlcExport.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPlcExport.Name = "btnPlcExport";
            this.btnPlcExport.Size = new System.Drawing.Size(134, 52);
            this.btnPlcExport.TabIndex = 14;
            this.btnPlcExport.Text = "导出";
            this.btnPlcExport.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // lblRefreshFreq
            // 
            this.lblRefreshFreq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblRefreshFreq.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblRefreshFreq.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblRefreshFreq.Location = new System.Drawing.Point(3, 3);
            this.lblRefreshFreq.Margin = new System.Windows.Forms.Padding(3);
            this.lblRefreshFreq.Name = "lblRefreshFreq";
            this.lblRefreshFreq.Size = new System.Drawing.Size(132, 52);
            this.lblRefreshFreq.TabIndex = 2;
            this.lblRefreshFreq.Text = "刷新频率:";
            this.lblRefreshFreq.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtRefreshFreq
            // 
            this.txtRefreshFreq.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtRefreshFreq.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtRefreshFreq.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtRefreshFreq.Location = new System.Drawing.Point(142, 5);
            this.txtRefreshFreq.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txtRefreshFreq.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtRefreshFreq.Name = "txtRefreshFreq";
            this.txtRefreshFreq.Padding = new System.Windows.Forms.Padding(5);
            this.txtRefreshFreq.ShowText = false;
            this.txtRefreshFreq.Size = new System.Drawing.Size(130, 48);
            this.txtRefreshFreq.TabIndex = 3;
            this.txtRefreshFreq.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.txtRefreshFreq.Watermark = "";
            // 
            // btnSetCurrent
            // 
            this.btnSetCurrent.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSetCurrent.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSetCurrent.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSetCurrent.Location = new System.Drawing.Point(279, 3);
            this.btnSetCurrent.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSetCurrent.Name = "btnSetCurrent";
            this.btnSetCurrent.Size = new System.Drawing.Size(132, 52);
            this.btnSetCurrent.TabIndex = 4;
            this.btnSetCurrent.Text = "设置当前";
            this.btnSetCurrent.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnSetAll
            // 
            this.btnSetAll.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSetAll.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSetAll.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSetAll.Location = new System.Drawing.Point(417, 3);
            this.btnSetAll.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSetAll.Name = "btnSetAll";
            this.btnSetAll.Size = new System.Drawing.Size(132, 52);
            this.btnSetAll.TabIndex = 5;
            this.btnSetAll.Text = "设置所有";
            this.btnSetAll.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // TagControlTableLayoutPanel
            // 
            this.TagControlTableLayoutPanel.ColumnCount = 1;
            this.TagControlTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.TagControlTableLayoutPanel.Controls.Add(this.tagItemRadioButton, 0, 1);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagEdit, 0, 2);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagCancel, 0, 3);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagSave, 0, 4);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagAdd, 0, 5);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagWrite, 0, 10);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagRead, 0, 9);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagDel, 0, 6);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagDown, 0, 8);
            this.TagControlTableLayoutPanel.Controls.Add(this.btnTagUpper, 0, 7);
            this.TagControlTableLayoutPanel.Controls.Add(this.groupItemRadioButton, 0, 0);
            this.TagControlTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TagControlTableLayoutPanel.Location = new System.Drawing.Point(1381, 60);
            this.TagControlTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.TagControlTableLayoutPanel.Name = "TagControlTableLayoutPanel";
            this.TagControlTableLayoutPanel.RowCount = 12;
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 7F));
            this.TagControlTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 27F));
            this.TagControlTableLayoutPanel.Size = new System.Drawing.Size(171, 1110);
            this.TagControlTableLayoutPanel.TabIndex = 6;
            this.TagControlTableLayoutPanel.TagString = null;
            // 
            // tagItemRadioButton
            // 
            this.tagItemRadioButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.tagItemRadioButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tagItemRadioButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.tagItemRadioButton.ImageInterval = 15;
            this.tagItemRadioButton.Location = new System.Drawing.Point(3, 58);
            this.tagItemRadioButton.MinimumSize = new System.Drawing.Size(1, 1);
            this.tagItemRadioButton.Name = "tagItemRadioButton";
            this.tagItemRadioButton.RadioButtonSize = 20;
            this.tagItemRadioButton.Size = new System.Drawing.Size(165, 49);
            this.tagItemRadioButton.TabIndex = 10;
            this.tagItemRadioButton.Text = "子级操作";
            // 
            // btnTagEdit
            // 
            this.btnTagEdit.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagEdit.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagEdit.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagEdit.Location = new System.Drawing.Point(10, 120);
            this.btnTagEdit.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagEdit.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagEdit.Name = "btnTagEdit";
            this.btnTagEdit.Radius = 10;
            this.btnTagEdit.Size = new System.Drawing.Size(151, 57);
            this.btnTagEdit.Symbol = 61508;
            this.btnTagEdit.TabIndex = 7;
            this.btnTagEdit.Text = "编辑";
            this.btnTagEdit.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagCancel
            // 
            this.btnTagCancel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagCancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagCancel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagCancel.Location = new System.Drawing.Point(10, 197);
            this.btnTagCancel.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagCancel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagCancel.Name = "btnTagCancel";
            this.btnTagCancel.Radius = 10;
            this.btnTagCancel.Size = new System.Drawing.Size(151, 57);
            this.btnTagCancel.Symbol = 559208;
            this.btnTagCancel.TabIndex = 8;
            this.btnTagCancel.Text = "取消";
            this.btnTagCancel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagSave
            // 
            this.btnTagSave.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagSave.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagSave.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagSave.Location = new System.Drawing.Point(10, 274);
            this.btnTagSave.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagSave.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagSave.Name = "btnTagSave";
            this.btnTagSave.Radius = 10;
            this.btnTagSave.Size = new System.Drawing.Size(151, 57);
            this.btnTagSave.Symbol = 557713;
            this.btnTagSave.TabIndex = 6;
            this.btnTagSave.Text = "保存";
            this.btnTagSave.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagAdd
            // 
            this.btnTagAdd.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagAdd.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagAdd.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagAdd.Location = new System.Drawing.Point(10, 351);
            this.btnTagAdd.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagAdd.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagAdd.Name = "btnTagAdd";
            this.btnTagAdd.Radius = 10;
            this.btnTagAdd.Size = new System.Drawing.Size(151, 57);
            this.btnTagAdd.Symbol = 300043;
            this.btnTagAdd.TabIndex = 0;
            this.btnTagAdd.Text = "添加";
            this.btnTagAdd.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagWrite
            // 
            this.btnTagWrite.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagWrite.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagWrite.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagWrite.Location = new System.Drawing.Point(10, 736);
            this.btnTagWrite.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagWrite.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagWrite.Name = "btnTagWrite";
            this.btnTagWrite.Radius = 10;
            this.btnTagWrite.Size = new System.Drawing.Size(151, 57);
            this.btnTagWrite.Symbol = 559810;
            this.btnTagWrite.TabIndex = 5;
            this.btnTagWrite.Text = "写入";
            this.btnTagWrite.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagRead
            // 
            this.btnTagRead.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagRead.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagRead.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagRead.Location = new System.Drawing.Point(10, 659);
            this.btnTagRead.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagRead.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagRead.Name = "btnTagRead";
            this.btnTagRead.Radius = 10;
            this.btnTagRead.Size = new System.Drawing.Size(151, 57);
            this.btnTagRead.Symbol = 559810;
            this.btnTagRead.TabIndex = 4;
            this.btnTagRead.Text = "读取";
            this.btnTagRead.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagDel
            // 
            this.btnTagDel.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagDel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagDel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagDel.Location = new System.Drawing.Point(10, 428);
            this.btnTagDel.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagDel.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagDel.Name = "btnTagDel";
            this.btnTagDel.Radius = 10;
            this.btnTagDel.Size = new System.Drawing.Size(151, 57);
            this.btnTagDel.Symbol = 558829;
            this.btnTagDel.TabIndex = 1;
            this.btnTagDel.Text = "删除";
            this.btnTagDel.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagDown
            // 
            this.btnTagDown.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagDown.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagDown.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagDown.Location = new System.Drawing.Point(10, 582);
            this.btnTagDown.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagDown.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagDown.Name = "btnTagDown";
            this.btnTagDown.Radius = 10;
            this.btnTagDown.Size = new System.Drawing.Size(151, 57);
            this.btnTagDown.Symbol = 558843;
            this.btnTagDown.TabIndex = 3;
            this.btnTagDown.Text = "下移";
            this.btnTagDown.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnTagUpper
            // 
            this.btnTagUpper.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnTagUpper.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnTagUpper.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnTagUpper.Location = new System.Drawing.Point(10, 505);
            this.btnTagUpper.Margin = new System.Windows.Forms.Padding(10);
            this.btnTagUpper.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnTagUpper.Name = "btnTagUpper";
            this.btnTagUpper.Radius = 10;
            this.btnTagUpper.Size = new System.Drawing.Size(151, 57);
            this.btnTagUpper.Symbol = 558840;
            this.btnTagUpper.TabIndex = 2;
            this.btnTagUpper.Text = "上移";
            this.btnTagUpper.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // groupItemRadioButton
            // 
            this.groupItemRadioButton.Cursor = System.Windows.Forms.Cursors.Hand;
            this.groupItemRadioButton.Dock = System.Windows.Forms.DockStyle.Fill;
            this.groupItemRadioButton.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.groupItemRadioButton.ImageInterval = 15;
            this.groupItemRadioButton.Location = new System.Drawing.Point(3, 3);
            this.groupItemRadioButton.MinimumSize = new System.Drawing.Size(1, 1);
            this.groupItemRadioButton.Name = "groupItemRadioButton";
            this.groupItemRadioButton.RadioButtonSize = 20;
            this.groupItemRadioButton.Size = new System.Drawing.Size(165, 49);
            this.groupItemRadioButton.TabIndex = 9;
            this.groupItemRadioButton.Text = "组级操作";
            // 
            // TagTableSplitContainer
            // 
            this.TagTableSplitContainer.BackColor = System.Drawing.Color.Transparent;
            this.TagTableSplitContainer.Cursor = System.Windows.Forms.Cursors.Default;
            this.TagTableSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TagTableSplitContainer.Location = new System.Drawing.Point(0, 60);
            this.TagTableSplitContainer.Margin = new System.Windows.Forms.Padding(0);
            this.TagTableSplitContainer.MinimumSize = new System.Drawing.Size(20, 20);
            this.TagTableSplitContainer.Name = "TagTableSplitContainer";
            this.TagTableSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // TagTableSplitContainer.Panel1
            // 
            this.TagTableSplitContainer.Panel1.Controls.Add(this.dgvGroupList);
            // 
            // TagTableSplitContainer.Panel2
            // 
            this.TagTableSplitContainer.Panel2.Controls.Add(this.dgvTagList);
            this.TagTableSplitContainer.Size = new System.Drawing.Size(1381, 1110);
            this.TagTableSplitContainer.SplitterDistance = 300;
            this.TagTableSplitContainer.SplitterWidth = 5;
            this.TagTableSplitContainer.TabIndex = 7;
            // 
            // dgvGroupList
            // 
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvGroupList.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            this.dgvGroupList.BackgroundColor = System.Drawing.Color.White;
            this.dgvGroupList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvGroupList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvGroupList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvGroupList.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgvGroupList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvGroupList.EnableHeadersVisualStyles = false;
            this.dgvGroupList.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvGroupList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvGroupList.Location = new System.Drawing.Point(0, 0);
            this.dgvGroupList.Margin = new System.Windows.Forms.Padding(0);
            this.dgvGroupList.Name = "dgvGroupList";
            this.dgvGroupList.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvGroupList.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvGroupList.RowsDefaultCellStyle = dataGridViewCellStyle10;
            this.dgvGroupList.RowTemplate.Height = 23;
            this.dgvGroupList.SelectedIndex = -1;
            this.dgvGroupList.Size = new System.Drawing.Size(1381, 300);
            this.dgvGroupList.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvGroupList.TabIndex = 6;
            // 
            // dgvTagList
            // 
            dataGridViewCellStyle11.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvTagList.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle11;
            this.dgvTagList.BackgroundColor = System.Drawing.Color.White;
            this.dgvTagList.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle12.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle12.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle12.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle12.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle12.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle12.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle12.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvTagList.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle12;
            this.dgvTagList.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle13.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle13.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle13.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle13.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle13.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle13.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle13.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvTagList.DefaultCellStyle = dataGridViewCellStyle13;
            this.dgvTagList.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvTagList.EnableHeadersVisualStyles = false;
            this.dgvTagList.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvTagList.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvTagList.Location = new System.Drawing.Point(0, 0);
            this.dgvTagList.Margin = new System.Windows.Forms.Padding(0);
            this.dgvTagList.Name = "dgvTagList";
            this.dgvTagList.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle14.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle14.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle14.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle14.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle14.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle14.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle14.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvTagList.RowHeadersDefaultCellStyle = dataGridViewCellStyle14;
            dataGridViewCellStyle15.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle15.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvTagList.RowsDefaultCellStyle = dataGridViewCellStyle15;
            this.dgvTagList.RowTemplate.Height = 23;
            this.dgvTagList.SelectedIndex = -1;
            this.dgvTagList.Size = new System.Drawing.Size(1381, 805);
            this.dgvTagList.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvTagList.TabIndex = 6;
            // 
            // FormPLCDebug
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.Color.White;
            this.ClientSize = new System.Drawing.Size(1940, 1220);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FormPLCDebug";
            this.Text = "FormPLCDebug";
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.leftTableLayoutPanel.ResumeLayout(false);
            this.leftTopTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvPLCList)).EndInit();
            this.uiTabControl1.ResumeLayout(false);
            this.tabConfig.ResumeLayout(false);
            this.plcConfigTableLayoutPanel.ResumeLayout(false);
            this.plcControlTableLayoutPanel.ResumeLayout(false);
            this.tabTagTable.ResumeLayout(false);
            this.TagTableTableLayoutPanel.ResumeLayout(false);
            this.midTopTableLayoutPanel.ResumeLayout(false);
            this.TagControlTableLayoutPanel.ResumeLayout(false);
            this.TagTableSplitContainer.Panel1.ResumeLayout(false);
            this.TagTableSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TagTableSplitContainer)).EndInit();
            this.TagTableSplitContainer.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvGroupList)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvTagList)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel rootTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel leftTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel leftTopTableLayoutPanel;
        private Sunny.UI.UIDataGridView dgvPLCList;
        private Sunny.UI.UIButton btnPlcExport;
        private Sunny.UI.UIButton btnPlcLoadIn;
        private Sunny.UI.UIButton btnPlcRebuild;
        private Sunny.UI.UIButton btnPlcConn;
        private Sunny.UI.UIButton btnPlcDel;
        private Sunny.UI.UIButton btnPlcCancel;
        private Sunny.UI.UIButton btnPlcSave;
        private Sunny.UI.UIButton btnPlcEdit;
        private Sunny.UI.UIButton btnPlcAdd;
        private Sunny.UI.UITabControl uiTabControl1;
        private System.Windows.Forms.TabPage tabTagTable;
        private System.Windows.Forms.TabPage tabConfig;
        private Sunny.UI.UITableLayoutPanel TagTableTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel midTopTableLayoutPanel;
        private Sunny.UI.UILabel lblRefreshFreq;
        private Sunny.UI.UITextBox txtRefreshFreq;
        private Sunny.UI.UIButton btnSetCurrent;
        private Sunny.UI.UIButton btnSetAll;
        private Sunny.UI.UITableLayoutPanel plcConfigTableLayoutPanel;
        private System.Windows.Forms.Panel pnlConfigContent1;
        private Sunny.UI.UITableLayoutPanel plcControlTableLayoutPanel;
        private Sunny.UI.UILabel lblPlcList;
        private Sunny.UI.UIButton btnPlcDisConn;
        private Sunny.UI.UISplitContainer TagTableSplitContainer;
        private Sunny.UI.UIDataGridView dgvGroupList;
        private Sunny.UI.UIDataGridView dgvTagList;
        private Sunny.UI.UITableLayoutPanel TagControlTableLayoutPanel;
        private Sunny.UI.UISymbolButton btnTagEdit;
        private Sunny.UI.UISymbolButton btnTagCancel;
        private Sunny.UI.UISymbolButton btnTagSave;
        private Sunny.UI.UISymbolButton btnTagAdd;
        private Sunny.UI.UISymbolButton btnTagWrite;
        private Sunny.UI.UISymbolButton btnTagRead;
        private Sunny.UI.UISymbolButton btnTagDel;
        private Sunny.UI.UISymbolButton btnTagDown;
        private Sunny.UI.UISymbolButton btnTagUpper;
        private Sunny.UI.UIRadioButton groupItemRadioButton;
        private Sunny.UI.UIRadioButton tagItemRadioButton;
        private Sunny.UI.UILabel lblQueueDepthDesc;
        private Sunny.UI.UILabel lblQueueDepth;
    }
}