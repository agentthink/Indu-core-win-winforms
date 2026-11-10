namespace Taoyl.UI.Pages
{
    partial class FormAudit
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
            this.rootTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.filterAreaTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.dtpTimeTo = new Sunny.UI.UIDatetimePicker();
            this.dtpTimeFrom = new Sunny.UI.UIDatetimePicker();
            this.lblTimeFrom = new Sunny.UI.UILabel();
            this.lblTimeTo = new Sunny.UI.UILabel();
            this.lblCategory = new Sunny.UI.UILabel();
            this.cmbCategory = new Sunny.UI.UIComboBox();
            this.lblPageSize = new Sunny.UI.UILabel();
            this.cmbPageSize = new Sunny.UI.UIComboBox();
            this.btnSearch = new Sunny.UI.UIButton();
            this.btnRefresh = new Sunny.UI.UIButton();
            this.btnExport = new Sunny.UI.UIButton();
            this.mianPanel = new Sunny.UI.UIPanel();
            this.TagTableSplitContainer = new Sunny.UI.UISplitContainer();
            this.masterShowTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.dgvOperations = new Sunny.UI.UIDataGridView();
            this.pagination = new Taoyl.UI.Pages.Controls.Pagination();
            this.dgvChanges = new Sunny.UI.UIDataGridView();
            this.rootTableLayoutPanel.SuspendLayout();
            this.filterAreaTableLayoutPanel.SuspendLayout();
            this.mianPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.TagTableSplitContainer)).BeginInit();
            this.TagTableSplitContainer.Panel1.SuspendLayout();
            this.TagTableSplitContainer.Panel2.SuspendLayout();
            this.TagTableSplitContainer.SuspendLayout();
            this.masterShowTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvOperations)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChanges)).BeginInit();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.filterAreaTableLayoutPanel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.mianPanel, 0, 1);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 2;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6.382979F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 93.61702F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(1556, 980);
            this.rootTableLayoutPanel.TabIndex = 0;
            this.rootTableLayoutPanel.TagString = null;
            // 
            // filterAreaTableLayoutPanel
            // 
            this.filterAreaTableLayoutPanel.ColumnCount = 11;
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.156051F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.79003F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.156051F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.79003F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.156051F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.79003F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 6.156051F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 11.79003F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9.400462F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9.400462F));
            this.filterAreaTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 9.414736F));
            this.filterAreaTableLayoutPanel.Controls.Add(this.dtpTimeTo, 3, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.dtpTimeFrom, 1, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.lblTimeFrom, 0, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.lblTimeTo, 2, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.lblCategory, 4, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.cmbCategory, 5, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.lblPageSize, 6, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.cmbPageSize, 7, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.btnSearch, 8, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.btnRefresh, 9, 0);
            this.filterAreaTableLayoutPanel.Controls.Add(this.btnExport, 10, 0);
            this.filterAreaTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filterAreaTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.filterAreaTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.filterAreaTableLayoutPanel.Name = "filterAreaTableLayoutPanel";
            this.filterAreaTableLayoutPanel.RowCount = 1;
            this.filterAreaTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.filterAreaTableLayoutPanel.Size = new System.Drawing.Size(1556, 62);
            this.filterAreaTableLayoutPanel.TabIndex = 1;
            this.filterAreaTableLayoutPanel.TagString = null;
            // 
            // dtpTimeTo
            // 
            this.dtpTimeTo.DateCultureInfo = new System.Globalization.CultureInfo("zh-CN");
            this.dtpTimeTo.DateFormat = "yyyy-MM-dd HH:mm";
            this.dtpTimeTo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpTimeTo.FillColor = System.Drawing.Color.White;
            this.dtpTimeTo.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dtpTimeTo.Location = new System.Drawing.Point(376, 18);
            this.dtpTimeTo.Margin = new System.Windows.Forms.Padding(3, 18, 3, 18);
            this.dtpTimeTo.MaxLength = 16;
            this.dtpTimeTo.MinimumSize = new System.Drawing.Size(63, 0);
            this.dtpTimeTo.Name = "dtpTimeTo";
            this.dtpTimeTo.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.dtpTimeTo.ShowToday = true;
            this.dtpTimeTo.Size = new System.Drawing.Size(177, 26);
            this.dtpTimeTo.SymbolDropDown = 61555;
            this.dtpTimeTo.SymbolNormal = 61555;
            this.dtpTimeTo.SymbolSize = 24;
            this.dtpTimeTo.TabIndex = 24;
            this.dtpTimeTo.Text = "2026-10-13 08:41";
            this.dtpTimeTo.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpTimeTo.Value = new System.DateTime(2026, 10, 13, 8, 41, 28, 45);
            this.dtpTimeTo.Watermark = "";
            // 
            // dtpTimeFrom
            // 
            this.dtpTimeFrom.DateCultureInfo = new System.Globalization.CultureInfo("zh-CN");
            this.dtpTimeFrom.DateFormat = "yyyy-MM-dd HH:mm";
            this.dtpTimeFrom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpTimeFrom.FillColor = System.Drawing.Color.White;
            this.dtpTimeFrom.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dtpTimeFrom.Location = new System.Drawing.Point(98, 18);
            this.dtpTimeFrom.Margin = new System.Windows.Forms.Padding(3, 18, 3, 18);
            this.dtpTimeFrom.MaxLength = 16;
            this.dtpTimeFrom.MinimumSize = new System.Drawing.Size(63, 0);
            this.dtpTimeFrom.Name = "dtpTimeFrom";
            this.dtpTimeFrom.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.dtpTimeFrom.ShowToday = true;
            this.dtpTimeFrom.Size = new System.Drawing.Size(177, 26);
            this.dtpTimeFrom.SymbolDropDown = 61555;
            this.dtpTimeFrom.SymbolNormal = 61555;
            this.dtpTimeFrom.SymbolSize = 24;
            this.dtpTimeFrom.TabIndex = 23;
            this.dtpTimeFrom.Text = "2026-10-13 08:41";
            this.dtpTimeFrom.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpTimeFrom.Value = new System.DateTime(2026, 10, 13, 8, 41, 28, 45);
            this.dtpTimeFrom.Watermark = "";
            // 
            // lblTimeFrom
            // 
            this.lblTimeFrom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeFrom.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTimeFrom.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblTimeFrom.Location = new System.Drawing.Point(3, 3);
            this.lblTimeFrom.Margin = new System.Windows.Forms.Padding(3);
            this.lblTimeFrom.Name = "lblTimeFrom";
            this.lblTimeFrom.Size = new System.Drawing.Size(89, 56);
            this.lblTimeFrom.TabIndex = 1;
            this.lblTimeFrom.Text = "起始";
            this.lblTimeFrom.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblTimeTo
            // 
            this.lblTimeTo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeTo.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTimeTo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblTimeTo.Location = new System.Drawing.Point(281, 3);
            this.lblTimeTo.Margin = new System.Windows.Forms.Padding(3);
            this.lblTimeTo.Name = "lblTimeTo";
            this.lblTimeTo.Size = new System.Drawing.Size(89, 56);
            this.lblTimeTo.TabIndex = 8;
            this.lblTimeTo.Text = "结束";
            this.lblTimeTo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lblCategory
            // 
            this.lblCategory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblCategory.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblCategory.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblCategory.Location = new System.Drawing.Point(559, 3);
            this.lblCategory.Margin = new System.Windows.Forms.Padding(3);
            this.lblCategory.Name = "lblCategory";
            this.lblCategory.Size = new System.Drawing.Size(89, 56);
            this.lblCategory.TabIndex = 10;
            this.lblCategory.Text = "页面";
            this.lblCategory.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cmbCategory
            // 
            this.cmbCategory.DataSource = null;
            this.cmbCategory.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbCategory.DropDownStyle = Sunny.UI.UIDropDownStyle.DropDownList;
            this.cmbCategory.FillColor = System.Drawing.Color.White;
            this.cmbCategory.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbCategory.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbCategory.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbCategory.Location = new System.Drawing.Point(654, 18);
            this.cmbCategory.Margin = new System.Windows.Forms.Padding(3, 18, 3, 18);
            this.cmbCategory.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbCategory.Name = "cmbCategory";
            this.cmbCategory.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbCategory.Size = new System.Drawing.Size(177, 26);
            this.cmbCategory.SymbolSize = 24;
            this.cmbCategory.TabIndex = 11;
            this.cmbCategory.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbCategory.Watermark = "";
            // 
            // lblPageSize
            // 
            this.lblPageSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageSize.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPageSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblPageSize.Location = new System.Drawing.Point(837, 3);
            this.lblPageSize.Margin = new System.Windows.Forms.Padding(3);
            this.lblPageSize.Name = "lblPageSize";
            this.lblPageSize.Size = new System.Drawing.Size(89, 56);
            this.lblPageSize.TabIndex = 8;
            this.lblPageSize.Text = "每页";
            this.lblPageSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
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
            this.cmbPageSize.Location = new System.Drawing.Point(932, 18);
            this.cmbPageSize.Margin = new System.Windows.Forms.Padding(3, 18, 3, 18);
            this.cmbPageSize.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbPageSize.Name = "cmbPageSize";
            this.cmbPageSize.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbPageSize.Size = new System.Drawing.Size(177, 26);
            this.cmbPageSize.SymbolSize = 24;
            this.cmbPageSize.TabIndex = 21;
            this.cmbPageSize.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbPageSize.Watermark = "";
            // 
            // btnSearch
            // 
            this.btnSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSearch.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSearch.Location = new System.Drawing.Point(1116, 8);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnSearch.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(138, 46);
            this.btnSearch.TabIndex = 18;
            this.btnSearch.Text = "查询";
            this.btnSearch.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnRefresh
            // 
            this.btnRefresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnRefresh.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnRefresh.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnRefresh.Location = new System.Drawing.Point(1262, 8);
            this.btnRefresh.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnRefresh.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(138, 46);
            this.btnRefresh.TabIndex = 19;
            this.btnRefresh.Text = "刷新";
            this.btnRefresh.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnExport
            // 
            this.btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExport.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnExport.Location = new System.Drawing.Point(1408, 8);
            this.btnExport.Margin = new System.Windows.Forms.Padding(4, 8, 4, 8);
            this.btnExport.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(144, 46);
            this.btnExport.TabIndex = 20;
            this.btnExport.Text = "导出 CSV";
            this.btnExport.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // mianPanel
            // 
            this.mianPanel.Controls.Add(this.TagTableSplitContainer);
            this.mianPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mianPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.mianPanel.Location = new System.Drawing.Point(0, 62);
            this.mianPanel.Margin = new System.Windows.Forms.Padding(0);
            this.mianPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.mianPanel.Name = "mianPanel";
            this.mianPanel.Size = new System.Drawing.Size(1556, 918);
            this.mianPanel.TabIndex = 2;
            this.mianPanel.Text = null;
            this.mianPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // TagTableSplitContainer
            // 
            this.TagTableSplitContainer.BackColor = System.Drawing.Color.Transparent;
            this.TagTableSplitContainer.CollapsePanel = Sunny.UI.UISplitContainer.UICollapsePanel.Panel2;
            this.TagTableSplitContainer.Cursor = System.Windows.Forms.Cursors.Default;
            this.TagTableSplitContainer.Dock = System.Windows.Forms.DockStyle.Fill;
            this.TagTableSplitContainer.Location = new System.Drawing.Point(0, 0);
            this.TagTableSplitContainer.Margin = new System.Windows.Forms.Padding(0);
            this.TagTableSplitContainer.MinimumSize = new System.Drawing.Size(20, 20);
            this.TagTableSplitContainer.Name = "TagTableSplitContainer";
            this.TagTableSplitContainer.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // TagTableSplitContainer.Panel1
            // 
            this.TagTableSplitContainer.Panel1.Controls.Add(this.masterShowTableLayoutPanel);
            // 
            // TagTableSplitContainer.Panel2
            // 
            this.TagTableSplitContainer.Panel2.Controls.Add(this.dgvChanges);
            this.TagTableSplitContainer.Size = new System.Drawing.Size(1556, 918);
            this.TagTableSplitContainer.SplitterDistance = 631;
            this.TagTableSplitContainer.SplitterWidth = 8;
            this.TagTableSplitContainer.TabIndex = 8;
            // 
            // masterShowTableLayoutPanel
            // 
            this.masterShowTableLayoutPanel.ColumnCount = 1;
            this.masterShowTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.masterShowTableLayoutPanel.Controls.Add(this.dgvOperations, 0, 0);
            this.masterShowTableLayoutPanel.Controls.Add(this.pagination, 0, 1);
            this.masterShowTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.masterShowTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.masterShowTableLayoutPanel.Name = "masterShowTableLayoutPanel";
            this.masterShowTableLayoutPanel.RowCount = 2;
            this.masterShowTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.masterShowTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 37F));
            this.masterShowTableLayoutPanel.Size = new System.Drawing.Size(1556, 631);
            this.masterShowTableLayoutPanel.TabIndex = 0;
            this.masterShowTableLayoutPanel.TagString = null;
            // 
            // dgvOperations
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvOperations.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvOperations.BackgroundColor = System.Drawing.Color.White;
            this.dgvOperations.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvOperations.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvOperations.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvOperations.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgvOperations.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvOperations.EnableHeadersVisualStyles = false;
            this.dgvOperations.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvOperations.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvOperations.Location = new System.Drawing.Point(0, 0);
            this.dgvOperations.Margin = new System.Windows.Forms.Padding(0);
            this.dgvOperations.Name = "dgvOperations";
            this.dgvOperations.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvOperations.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvOperations.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.dgvOperations.RowTemplate.Height = 23;
            this.dgvOperations.SelectedIndex = -1;
            this.dgvOperations.Size = new System.Drawing.Size(1556, 594);
            this.dgvOperations.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvOperations.TabIndex = 7;
            // 
            // pagination
            // 
            this.pagination.BackColor = System.Drawing.Color.White;
            this.pagination.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pagination.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pagination.Location = new System.Drawing.Point(5, 598);
            this.pagination.Margin = new System.Windows.Forms.Padding(5, 4, 5, 4);
            this.pagination.Name = "pagination";
            this.pagination.NavButtonFont = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pagination.PageButtonFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pagination.SelectedBackColor = System.Drawing.Color.White;
            this.pagination.SelectedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.pagination.SelectedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.pagination.Size = new System.Drawing.Size(1546, 29);
            this.pagination.TabIndex = 11;
            // 
            // dgvChanges
            // 
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvChanges.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            this.dgvChanges.BackgroundColor = System.Drawing.Color.White;
            this.dgvChanges.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvChanges.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dgvChanges.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgvChanges.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgvChanges.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvChanges.EnableHeadersVisualStyles = false;
            this.dgvChanges.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvChanges.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvChanges.Location = new System.Drawing.Point(0, 0);
            this.dgvChanges.Margin = new System.Windows.Forms.Padding(0);
            this.dgvChanges.Name = "dgvChanges";
            this.dgvChanges.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvChanges.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvChanges.RowsDefaultCellStyle = dataGridViewCellStyle10;
            this.dgvChanges.RowTemplate.Height = 23;
            this.dgvChanges.SelectedIndex = -1;
            this.dgvChanges.Size = new System.Drawing.Size(1556, 279);
            this.dgvChanges.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvChanges.TabIndex = 6;
            // 
            // FormAudit
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1556, 980);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FormAudit";
            this.Text = "";
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.filterAreaTableLayoutPanel.ResumeLayout(false);
            this.mianPanel.ResumeLayout(false);
            this.TagTableSplitContainer.Panel1.ResumeLayout(false);
            this.TagTableSplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.TagTableSplitContainer)).EndInit();
            this.TagTableSplitContainer.ResumeLayout(false);
            this.masterShowTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvOperations)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dgvChanges)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel rootTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel filterAreaTableLayoutPanel;
        private Sunny.UI.UIPanel mianPanel;
        private Sunny.UI.UISplitContainer TagTableSplitContainer;
        private Sunny.UI.UIDataGridView dgvChanges;
        private Sunny.UI.UITableLayoutPanel masterShowTableLayoutPanel;
        private Sunny.UI.UIDataGridView dgvOperations;
        private Sunny.UI.UILabel lblPageSize;
        private Sunny.UI.UIComboBox cmbPageSize;
        private Sunny.UI.UILabel lblTimeFrom;
        private Sunny.UI.UILabel lblTimeTo;
        private Sunny.UI.UILabel lblCategory;
        private Sunny.UI.UIComboBox cmbCategory;
        private Sunny.UI.UIButton btnSearch;
        private Sunny.UI.UIButton btnRefresh;
        private Sunny.UI.UIButton btnExport;
        private Controls.Pagination pagination;
        private Sunny.UI.UIDatetimePicker dtpTimeFrom;
        private Sunny.UI.UIDatetimePicker dtpTimeTo;
    }
}
