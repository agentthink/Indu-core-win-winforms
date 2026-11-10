namespace Taoyl.UI.Pages
{
    partial class FormProductQuery
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
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle1 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle2 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle3 = new System.Windows.Forms.DataGridViewCellStyle();
            System.Windows.Forms.DataGridViewCellStyle dataGridViewCellStyle4 = new System.Windows.Forms.DataGridViewCellStyle();
            this.rootTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topRightPanel = new Sunny.UI.UIPanel();
            this.topLeftPanel = new Sunny.UI.UIPanel();
            this.filterTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.txtKeyword = new Sunny.UI.UITextBox();
            this.lblKeyword = new Sunny.UI.UILabel();
            this.cmbOperator = new Sunny.UI.UIComboBox();
            this.cmbField = new Sunny.UI.UIComboBox();
            this.cmbTable = new Sunny.UI.UIComboBox();
            this.lblTable = new Sunny.UI.UILabel();
            this.topMidPanel = new Sunny.UI.UIPanel();
            this.timeTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btnSearch = new Sunny.UI.UIButton();
            this.dtpTimeTo = new Sunny.UI.UIDatetimePicker();
            this.lblTimeTo = new Sunny.UI.UILabel();
            this.dtpTimeFrom = new Sunny.UI.UIDatetimePicker();
            this.chkTimeEnable = new Sunny.UI.UICheckBox();
            this.dgvRecords = new Sunny.UI.UIDataGridView();
            this.btnExport = new Sunny.UI.UIButton();
            this.bottomTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.lblPageSize = new Sunny.UI.UILabel();
            this.cmbPageSize = new Sunny.UI.UIComboBox();
            this.pagination = new Taoyl.UI.Pages.Controls.Pagination();
            this.rootTableLayoutPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.topRightPanel.SuspendLayout();
            this.topLeftPanel.SuspendLayout();
            this.filterTableLayoutPanel.SuspendLayout();
            this.topMidPanel.SuspendLayout();
            this.timeTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecords)).BeginInit();
            this.bottomTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.rootTableLayoutPanel.Controls.Add(this.dgvRecords, 0, 1);
            this.rootTableLayoutPanel.Controls.Add(this.topTableLayoutPanel, 0, 0);
            this.rootTableLayoutPanel.Controls.Add(this.bottomTableLayoutPanel, 0, 2);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.rootTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 3;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 12F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 82F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 6F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(1660, 1032);
            this.rootTableLayoutPanel.TabIndex = 0;
            this.rootTableLayoutPanel.TagString = null;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 3;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 10F));
            this.topTableLayoutPanel.Controls.Add(this.topRightPanel, 2, 0);
            this.topTableLayoutPanel.Controls.Add(this.topLeftPanel, 0, 0);
            this.topTableLayoutPanel.Controls.Add(this.topMidPanel, 1, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(3, 3);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 1;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(1654, 117);
            this.topTableLayoutPanel.TabIndex = 2;
            this.topTableLayoutPanel.TagString = null;
            // 
            // topRightPanel
            // 
            this.topRightPanel.Controls.Add(this.btnExport);
            this.topRightPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topRightPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topRightPanel.Location = new System.Drawing.Point(1492, 5);
            this.topRightPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topRightPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topRightPanel.Name = "topRightPanel";
            this.topRightPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topRightPanel.Size = new System.Drawing.Size(158, 107);
            this.topRightPanel.TabIndex = 2;
            this.topRightPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topLeftPanel
            // 
            this.topLeftPanel.Controls.Add(this.filterTableLayoutPanel);
            this.topLeftPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLeftPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topLeftPanel.Location = new System.Drawing.Point(4, 5);
            this.topLeftPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topLeftPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topLeftPanel.Name = "topLeftPanel";
            this.topLeftPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topLeftPanel.Size = new System.Drawing.Size(653, 107);
            this.topLeftPanel.TabIndex = 0;
            this.topLeftPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // filterTableLayoutPanel
            // 
            this.filterTableLayoutPanel.ColumnCount = 4;
            this.filterTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.filterTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.filterTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.filterTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.filterTableLayoutPanel.Controls.Add(this.txtKeyword, 1, 1);
            this.filterTableLayoutPanel.Controls.Add(this.lblKeyword, 0, 1);
            this.filterTableLayoutPanel.Controls.Add(this.cmbOperator, 3, 0);
            this.filterTableLayoutPanel.Controls.Add(this.cmbField, 2, 0);
            this.filterTableLayoutPanel.Controls.Add(this.cmbTable, 1, 0);
            this.filterTableLayoutPanel.Controls.Add(this.lblTable, 0, 0);
            this.filterTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.filterTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.filterTableLayoutPanel.Name = "filterTableLayoutPanel";
            this.filterTableLayoutPanel.RowCount = 2;
            this.filterTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.filterTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.filterTableLayoutPanel.Size = new System.Drawing.Size(643, 97);
            this.filterTableLayoutPanel.TabIndex = 0;
            this.filterTableLayoutPanel.TagString = null;
            // 
            // txtKeyword
            // 
            this.filterTableLayoutPanel.SetColumnSpan(this.txtKeyword, 3);
            this.txtKeyword.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtKeyword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtKeyword.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtKeyword.Location = new System.Drawing.Point(111, 53);
            this.txtKeyword.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.txtKeyword.MinimumSize = new System.Drawing.Size(1, 16);
            this.txtKeyword.Name = "txtKeyword";
            this.txtKeyword.Padding = new System.Windows.Forms.Padding(5);
            this.txtKeyword.ShowText = false;
            this.txtKeyword.Size = new System.Drawing.Size(517, 39);
            this.txtKeyword.TabIndex = 17;
            this.txtKeyword.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txtKeyword.Watermark = "输入关键字";
            // 
            // lblKeyword
            // 
            this.lblKeyword.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblKeyword.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblKeyword.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblKeyword.Location = new System.Drawing.Point(3, 51);
            this.lblKeyword.Margin = new System.Windows.Forms.Padding(3);
            this.lblKeyword.Name = "lblKeyword";
            this.lblKeyword.Size = new System.Drawing.Size(90, 43);
            this.lblKeyword.TabIndex = 16;
            this.lblKeyword.Text = "关键字";
            this.lblKeyword.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cmbOperator
            // 
            this.cmbOperator.DataSource = null;
            this.cmbOperator.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbOperator.FillColor = System.Drawing.Color.White;
            this.cmbOperator.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbOperator.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbOperator.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbOperator.Location = new System.Drawing.Point(561, 5);
            this.cmbOperator.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbOperator.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbOperator.Name = "cmbOperator";
            this.cmbOperator.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbOperator.Size = new System.Drawing.Size(67, 38);
            this.cmbOperator.SymbolSize = 24;
            this.cmbOperator.TabIndex = 7;
            this.cmbOperator.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbOperator.Watermark = "";
            // 
            // cmbField
            // 
            this.cmbField.DataSource = null;
            this.cmbField.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbField.FillColor = System.Drawing.Color.White;
            this.cmbField.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbField.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbField.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbField.Location = new System.Drawing.Point(336, 5);
            this.cmbField.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbField.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbField.Name = "cmbField";
            this.cmbField.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbField.Size = new System.Drawing.Size(195, 38);
            this.cmbField.SymbolSize = 24;
            this.cmbField.TabIndex = 5;
            this.cmbField.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbField.Watermark = "";
            // 
            // cmbTable
            // 
            this.cmbTable.DataSource = null;
            this.cmbTable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbTable.FillColor = System.Drawing.Color.White;
            this.cmbTable.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbTable.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbTable.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbTable.Location = new System.Drawing.Point(111, 5);
            this.cmbTable.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.cmbTable.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbTable.Name = "cmbTable";
            this.cmbTable.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbTable.Size = new System.Drawing.Size(195, 38);
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
            this.lblTable.Size = new System.Drawing.Size(90, 42);
            this.lblTable.TabIndex = 2;
            this.lblTable.Text = "产品表";
            this.lblTable.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topMidPanel
            // 
            this.topMidPanel.Controls.Add(this.timeTableLayoutPanel);
            this.topMidPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topMidPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topMidPanel.Location = new System.Drawing.Point(665, 5);
            this.topMidPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.topMidPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topMidPanel.Name = "topMidPanel";
            this.topMidPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topMidPanel.Size = new System.Drawing.Size(819, 107);
            this.topMidPanel.TabIndex = 1;
            this.topMidPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timeTableLayoutPanel
            // 
            this.timeTableLayoutPanel.ColumnCount = 4;
            this.timeTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.timeTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.timeTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.timeTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.timeTableLayoutPanel.Controls.Add(this.btnSearch, 0, 1);
            this.timeTableLayoutPanel.Controls.Add(this.dtpTimeTo, 3, 0);
            this.timeTableLayoutPanel.Controls.Add(this.lblTimeTo, 2, 0);
            this.timeTableLayoutPanel.Controls.Add(this.dtpTimeFrom, 1, 0);
            this.timeTableLayoutPanel.Controls.Add(this.chkTimeEnable, 0, 0);
            this.timeTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.timeTableLayoutPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.timeTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.timeTableLayoutPanel.Name = "timeTableLayoutPanel";
            this.timeTableLayoutPanel.RowCount = 2;
            this.timeTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.timeTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.timeTableLayoutPanel.Size = new System.Drawing.Size(809, 97);
            this.timeTableLayoutPanel.TabIndex = 0;
            this.timeTableLayoutPanel.TagString = null;
            // 
            // btnSearch
            // 
            this.btnSearch.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnSearch.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnSearch.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnSearch.Location = new System.Drawing.Point(5, 53);
            this.btnSearch.Margin = new System.Windows.Forms.Padding(5);
            this.btnSearch.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnSearch.Name = "btnSearch";
            this.btnSearch.Size = new System.Drawing.Size(111, 39);
            this.btnSearch.TabIndex = 18;
            this.btnSearch.Text = "查询";
            this.btnSearch.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // dtpTimeTo
            // 
            this.dtpTimeTo.DateCultureInfo = new System.Globalization.CultureInfo("zh-CN");
            this.dtpTimeTo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpTimeTo.FillColor = System.Drawing.Color.White;
            this.dtpTimeTo.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dtpTimeTo.Location = new System.Drawing.Point(499, 5);
            this.dtpTimeTo.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.dtpTimeTo.MaxLength = 19;
            this.dtpTimeTo.MinimumSize = new System.Drawing.Size(63, 0);
            this.dtpTimeTo.Name = "dtpTimeTo";
            this.dtpTimeTo.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.dtpTimeTo.Size = new System.Drawing.Size(295, 38);
            this.dtpTimeTo.SymbolDropDown = 61555;
            this.dtpTimeTo.SymbolNormal = 61555;
            this.dtpTimeTo.SymbolSize = 24;
            this.dtpTimeTo.TabIndex = 14;
            this.dtpTimeTo.Text = "2026-10-22 12:48:32";
            this.dtpTimeTo.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpTimeTo.Value = new System.DateTime(2026, 10, 22, 12, 48, 32, 620);
            this.dtpTimeTo.Watermark = "";
            // 
            // lblTimeTo
            // 
            this.lblTimeTo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTimeTo.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblTimeTo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblTimeTo.Location = new System.Drawing.Point(447, 3);
            this.lblTimeTo.Margin = new System.Windows.Forms.Padding(3);
            this.lblTimeTo.Name = "lblTimeTo";
            this.lblTimeTo.Size = new System.Drawing.Size(34, 42);
            this.lblTimeTo.TabIndex = 13;
            this.lblTimeTo.Text = "至";
            this.lblTimeTo.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // dtpTimeFrom
            // 
            this.dtpTimeFrom.DateCultureInfo = new System.Globalization.CultureInfo("zh-CN");
            this.dtpTimeFrom.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dtpTimeFrom.FillColor = System.Drawing.Color.White;
            this.dtpTimeFrom.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dtpTimeFrom.Location = new System.Drawing.Point(136, 5);
            this.dtpTimeFrom.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.dtpTimeFrom.MaxLength = 19;
            this.dtpTimeFrom.MinimumSize = new System.Drawing.Size(63, 0);
            this.dtpTimeFrom.Name = "dtpTimeFrom";
            this.dtpTimeFrom.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.dtpTimeFrom.Size = new System.Drawing.Size(293, 38);
            this.dtpTimeFrom.SymbolDropDown = 61555;
            this.dtpTimeFrom.SymbolNormal = 61555;
            this.dtpTimeFrom.SymbolSize = 24;
            this.dtpTimeFrom.TabIndex = 12;
            this.dtpTimeFrom.Text = "2026-10-22 12:48:32";
            this.dtpTimeFrom.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.dtpTimeFrom.Value = new System.DateTime(2026, 10, 22, 12, 48, 32, 595);
            this.dtpTimeFrom.Watermark = "";
            // 
            // chkTimeEnable
            // 
            this.chkTimeEnable.Cursor = System.Windows.Forms.Cursors.Hand;
            this.chkTimeEnable.Dock = System.Windows.Forms.DockStyle.Fill;
            this.chkTimeEnable.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.chkTimeEnable.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.chkTimeEnable.Location = new System.Drawing.Point(3, 3);
            this.chkTimeEnable.MinimumSize = new System.Drawing.Size(1, 1);
            this.chkTimeEnable.Name = "chkTimeEnable";
            this.chkTimeEnable.Size = new System.Drawing.Size(115, 42);
            this.chkTimeEnable.TabIndex = 11;
            this.chkTimeEnable.Text = "按时间筛选";
            // 
            // dgvRecords
            // 
            this.dgvRecords.AllowUserToResizeRows = false;
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvRecords.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgvRecords.BackgroundColor = System.Drawing.Color.White;
            this.dgvRecords.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecords.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgvRecords.ColumnHeadersHeight = 32;
            this.dgvRecords.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgvRecords.EnableHeadersVisualStyles = false;
            this.dgvRecords.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvRecords.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgvRecords.Location = new System.Drawing.Point(3, 126);
            this.dgvRecords.Name = "dgvRecords";
            this.dgvRecords.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgvRecords.RowHeadersDefaultCellStyle = dataGridViewCellStyle3;
            this.dgvRecords.RowHeadersVisible = false;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgvRecords.RowsDefaultCellStyle = dataGridViewCellStyle4;
            this.dgvRecords.SelectedIndex = -1;
            this.dgvRecords.Size = new System.Drawing.Size(1654, 840);
            this.dgvRecords.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgvRecords.TabIndex = 3;
            // 
            // btnExport
            // 
            this.btnExport.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnExport.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnExport.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnExport.Location = new System.Drawing.Point(5, 5);
            this.btnExport.Margin = new System.Windows.Forms.Padding(15, 5, 15, 5);
            this.btnExport.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnExport.Name = "btnExport";
            this.btnExport.Size = new System.Drawing.Size(148, 97);
            this.btnExport.TabIndex = 20;
            this.btnExport.Text = "导出";
            this.btnExport.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // bottomTableLayoutPanel
            // 
            this.bottomTableLayoutPanel.ColumnCount = 3;
            this.bottomTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 80F));
            this.bottomTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 5F));
            this.bottomTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.bottomTableLayoutPanel.Controls.Add(this.cmbPageSize, 2, 0);
            this.bottomTableLayoutPanel.Controls.Add(this.lblPageSize, 1, 0);
            this.bottomTableLayoutPanel.Controls.Add(this.pagination, 0, 0);
            this.bottomTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.bottomTableLayoutPanel.Location = new System.Drawing.Point(3, 972);
            this.bottomTableLayoutPanel.Name = "bottomTableLayoutPanel";
            this.bottomTableLayoutPanel.RowCount = 1;
            this.bottomTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.bottomTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.bottomTableLayoutPanel.Size = new System.Drawing.Size(1654, 57);
            this.bottomTableLayoutPanel.TabIndex = 4;
            this.bottomTableLayoutPanel.TagString = null;
            // 
            // lblPageSize
            // 
            this.lblPageSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblPageSize.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblPageSize.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblPageSize.Location = new System.Drawing.Point(1326, 3);
            this.lblPageSize.Margin = new System.Windows.Forms.Padding(3);
            this.lblPageSize.Name = "lblPageSize";
            this.lblPageSize.Size = new System.Drawing.Size(76, 51);
            this.lblPageSize.TabIndex = 2;
            this.lblPageSize.Text = "每页";
            this.lblPageSize.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // cmbPageSize
            // 
            this.cmbPageSize.DataSource = null;
            this.cmbPageSize.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbPageSize.FillColor = System.Drawing.Color.White;
            this.cmbPageSize.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.cmbPageSize.ItemHoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(155)))), ((int)(((byte)(200)))), ((int)(((byte)(255)))));
            this.cmbPageSize.ItemSelectForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.cmbPageSize.Location = new System.Drawing.Point(1410, 5);
            this.cmbPageSize.Margin = new System.Windows.Forms.Padding(5);
            this.cmbPageSize.MinimumSize = new System.Drawing.Size(63, 0);
            this.cmbPageSize.Name = "cmbPageSize";
            this.cmbPageSize.Padding = new System.Windows.Forms.Padding(0, 0, 30, 2);
            this.cmbPageSize.Size = new System.Drawing.Size(239, 47);
            this.cmbPageSize.SymbolSize = 24;
            this.cmbPageSize.TabIndex = 3;
            this.cmbPageSize.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.cmbPageSize.Watermark = "";
            // 
            // pagination
            // 
            this.pagination.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pagination.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pagination.Location = new System.Drawing.Point(4, 4);
            this.pagination.Margin = new System.Windows.Forms.Padding(4);
            this.pagination.Name = "pagination";
            this.pagination.NavButtonFont = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pagination.PageButtonFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.pagination.SelectedBackColor = System.Drawing.Color.White;
            this.pagination.SelectedBorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.pagination.SelectedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.pagination.Size = new System.Drawing.Size(1315, 49);
            this.pagination.TabIndex = 1;
            // 
            // FormProductQuery
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1660, 1032);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FormProductQuery";
            this.Text = "产品数据查询";
            this.rootTableLayoutPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.topRightPanel.ResumeLayout(false);
            this.topLeftPanel.ResumeLayout(false);
            this.filterTableLayoutPanel.ResumeLayout(false);
            this.topMidPanel.ResumeLayout(false);
            this.timeTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgvRecords)).EndInit();
            this.bottomTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel rootTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UIPanel topRightPanel;
        private Sunny.UI.UIPanel topLeftPanel;
        private Sunny.UI.UITableLayoutPanel filterTableLayoutPanel;
        private Sunny.UI.UITextBox txtKeyword;
        private Sunny.UI.UILabel lblKeyword;
        private Sunny.UI.UIComboBox cmbOperator;
        private Sunny.UI.UIComboBox cmbField;
        private Sunny.UI.UIComboBox cmbTable;
        private Sunny.UI.UILabel lblTable;
        private Sunny.UI.UIPanel topMidPanel;
        private Sunny.UI.UITableLayoutPanel timeTableLayoutPanel;
        private Sunny.UI.UIButton btnSearch;
        private Sunny.UI.UIDatetimePicker dtpTimeTo;
        private Sunny.UI.UILabel lblTimeTo;
        private Sunny.UI.UIDatetimePicker dtpTimeFrom;
        private Sunny.UI.UICheckBox chkTimeEnable;
        private Sunny.UI.UIDataGridView dgvRecords;
        private Sunny.UI.UIButton btnExport;
        private Sunny.UI.UITableLayoutPanel bottomTableLayoutPanel;
        private Controls.Pagination pagination;
        private Sunny.UI.UILabel lblPageSize;
        private Sunny.UI.UIComboBox cmbPageSize;
    }
}
