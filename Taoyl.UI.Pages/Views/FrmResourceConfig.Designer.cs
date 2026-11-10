namespace Taoyl.UI.Pages
{
    partial class FrmResourceConfig
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
            this.roottableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.downtableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.resouceTabControl = new Sunny.UI.UITabControl();
            this.tp_UI = new System.Windows.Forms.TabPage();
            this.dgv_UI = new Sunny.UI.UIDataGridView();
            this.tp_Text = new System.Windows.Forms.TabPage();
            this.dgv_Text = new Sunny.UI.UIDataGridView();
            this.menuTreeView = new Sunny.UI.UITreeView();
            this.ControlPanel = new Sunny.UI.UIPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.topLeftTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.txt_resourceName = new Sunny.UI.UITextBox();
            this.txt_moduleName = new Sunny.UI.UITextBox();
            this.txt_className = new Sunny.UI.UITextBox();
            this.lbl_resourceName = new Sunny.UI.UILabel();
            this.lbl_className = new Sunny.UI.UILabel();
            this.lbl_moduleName = new Sunny.UI.UILabel();
            this.topRightTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.btn_save = new Sunny.UI.UIButton();
            this.btn_export = new Sunny.UI.UIButton();
            this.btn_getTextRes = new Sunny.UI.UIButton();
            this.btn_search = new Sunny.UI.UIButton();
            this.btn_loadIn = new Sunny.UI.UIButton();
            this.btn_getUiRes = new Sunny.UI.UIButton();
            this.roottableLayoutPanel.SuspendLayout();
            this.downtableLayoutPanel.SuspendLayout();
            this.resouceTabControl.SuspendLayout();
            this.tp_UI.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_UI)).BeginInit();
            this.tp_Text.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Text)).BeginInit();
            this.ControlPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.topLeftTableLayoutPanel.SuspendLayout();
            this.topRightTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // roottableLayoutPanel
            // 
            this.roottableLayoutPanel.ColumnCount = 1;
            this.roottableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.roottableLayoutPanel.Controls.Add(this.downtableLayoutPanel, 0, 1);
            this.roottableLayoutPanel.Controls.Add(this.ControlPanel, 0, 0);
            this.roottableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.roottableLayoutPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.roottableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.roottableLayoutPanel.Name = "roottableLayoutPanel";
            this.roottableLayoutPanel.RowCount = 2;
            this.roottableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 15F));
            this.roottableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 85F));
            this.roottableLayoutPanel.Size = new System.Drawing.Size(1660, 1032);
            this.roottableLayoutPanel.TabIndex = 0;
            // 
            // downtableLayoutPanel
            // 
            this.downtableLayoutPanel.ColumnCount = 2;
            this.downtableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 26.20545F));
            this.downtableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 73.79455F));
            this.downtableLayoutPanel.Controls.Add(this.resouceTabControl, 1, 0);
            this.downtableLayoutPanel.Controls.Add(this.menuTreeView, 0, 0);
            this.downtableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.downtableLayoutPanel.Location = new System.Drawing.Point(3, 157);
            this.downtableLayoutPanel.Name = "downtableLayoutPanel";
            this.downtableLayoutPanel.RowCount = 1;
            this.downtableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.downtableLayoutPanel.Size = new System.Drawing.Size(1654, 872);
            this.downtableLayoutPanel.TabIndex = 0;
            // 
            // resouceTabControl
            // 
            this.resouceTabControl.Controls.Add(this.tp_UI);
            this.resouceTabControl.Controls.Add(this.tp_Text);
            this.resouceTabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.resouceTabControl.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.resouceTabControl.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.resouceTabControl.ItemSize = new System.Drawing.Size(150, 40);
            this.resouceTabControl.Location = new System.Drawing.Point(436, 3);
            this.resouceTabControl.MainPage = "";
            this.resouceTabControl.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.resouceTabControl.Name = "resouceTabControl";
            this.resouceTabControl.SelectedIndex = 0;
            this.resouceTabControl.Size = new System.Drawing.Size(1215, 866);
            this.resouceTabControl.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.resouceTabControl.TabBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.resouceTabControl.TabIndex = 0;
            this.resouceTabControl.TabPageTextAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            this.resouceTabControl.TabSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.resouceTabControl.TabSelectedForeColor = System.Drawing.Color.White;
            this.resouceTabControl.TabSelectedHighColor = System.Drawing.SystemColors.Highlight;
            this.resouceTabControl.TabUnSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.resouceTabControl.TabUnSelectedForeColor = System.Drawing.Color.Black;
            this.resouceTabControl.TipsFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // tp_UI
            // 
            this.tp_UI.Controls.Add(this.dgv_UI);
            this.tp_UI.Location = new System.Drawing.Point(0, 40);
            this.tp_UI.Name = "tp_UI";
            this.tp_UI.Size = new System.Drawing.Size(1215, 826);
            this.tp_UI.TabIndex = 0;
            this.tp_UI.Text = "UI资源";
            this.tp_UI.UseVisualStyleBackColor = true;
            // 
            // dgv_UI
            // 
            dataGridViewCellStyle1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgv_UI.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
            this.dgv_UI.BackgroundColor = System.Drawing.Color.White;
            this.dgv_UI.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle2.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle2.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle2.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle2.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle2.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle2.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_UI.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle2;
            this.dgv_UI.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle3.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle3.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle3.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle3.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle3.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle3.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_UI.DefaultCellStyle = dataGridViewCellStyle3;
            this.dgv_UI.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_UI.EnableHeadersVisualStyles = false;
            this.dgv_UI.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgv_UI.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgv_UI.Location = new System.Drawing.Point(0, 0);
            this.dgv_UI.Name = "dgv_UI";
            this.dgv_UI.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle4.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle4.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle4.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle4.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle4.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_UI.RowHeadersDefaultCellStyle = dataGridViewCellStyle4;
            dataGridViewCellStyle5.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle5.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgv_UI.RowsDefaultCellStyle = dataGridViewCellStyle5;
            this.dgv_UI.RowTemplate.Height = 23;
            this.dgv_UI.SelectedIndex = -1;
            this.dgv_UI.Size = new System.Drawing.Size(1215, 826);
            this.dgv_UI.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgv_UI.TabIndex = 0;
            // 
            // tp_Text
            // 
            this.tp_Text.Controls.Add(this.dgv_Text);
            this.tp_Text.Location = new System.Drawing.Point(0, 40);
            this.tp_Text.Name = "tp_Text";
            this.tp_Text.Size = new System.Drawing.Size(1215, 826);
            this.tp_Text.TabIndex = 1;
            this.tp_Text.Text = "Text资源";
            this.tp_Text.UseVisualStyleBackColor = true;
            // 
            // dgv_Text
            // 
            dataGridViewCellStyle6.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgv_Text.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle6;
            this.dgv_Text.BackgroundColor = System.Drawing.Color.White;
            this.dgv_Text.ColumnHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle7.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleCenter;
            dataGridViewCellStyle7.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle7.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle7.ForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle7.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle7.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle7.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_Text.ColumnHeadersDefaultCellStyle = dataGridViewCellStyle7;
            this.dgv_Text.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dataGridViewCellStyle8.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle8.BackColor = System.Drawing.SystemColors.Window;
            dataGridViewCellStyle8.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle8.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle8.SelectionBackColor = System.Drawing.SystemColors.Highlight;
            dataGridViewCellStyle8.SelectionForeColor = System.Drawing.SystemColors.HighlightText;
            dataGridViewCellStyle8.WrapMode = System.Windows.Forms.DataGridViewTriState.False;
            this.dgv_Text.DefaultCellStyle = dataGridViewCellStyle8;
            this.dgv_Text.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_Text.EnableHeadersVisualStyles = false;
            this.dgv_Text.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgv_Text.GridColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            this.dgv_Text.Location = new System.Drawing.Point(0, 0);
            this.dgv_Text.Name = "dgv_Text";
            this.dgv_Text.RowHeadersBorderStyle = System.Windows.Forms.DataGridViewHeaderBorderStyle.Single;
            dataGridViewCellStyle9.Alignment = System.Windows.Forms.DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle9.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            dataGridViewCellStyle9.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            dataGridViewCellStyle9.SelectionBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(160)))), ((int)(((byte)(255)))));
            dataGridViewCellStyle9.SelectionForeColor = System.Drawing.Color.White;
            dataGridViewCellStyle9.WrapMode = System.Windows.Forms.DataGridViewTriState.True;
            this.dgv_Text.RowHeadersDefaultCellStyle = dataGridViewCellStyle9;
            dataGridViewCellStyle10.BackColor = System.Drawing.Color.White;
            dataGridViewCellStyle10.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.dgv_Text.RowsDefaultCellStyle = dataGridViewCellStyle10;
            this.dgv_Text.RowTemplate.Height = 23;
            this.dgv_Text.SelectedIndex = -1;
            this.dgv_Text.Size = new System.Drawing.Size(1215, 826);
            this.dgv_Text.StripeOddColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(243)))), ((int)(((byte)(255)))));
            this.dgv_Text.TabIndex = 1;
            // 
            // menuTreeView
            // 
            this.menuTreeView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.menuTreeView.FillColor = System.Drawing.Color.White;
            this.menuTreeView.Font = new System.Drawing.Font("宋体", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.menuTreeView.Location = new System.Drawing.Point(4, 5);
            this.menuTreeView.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.menuTreeView.MinimumSize = new System.Drawing.Size(1, 1);
            this.menuTreeView.Name = "menuTreeView";
            this.menuTreeView.ScrollBarStyleInherited = false;
            this.menuTreeView.ShowText = false;
            this.menuTreeView.Size = new System.Drawing.Size(425, 862);
            this.menuTreeView.TabIndex = 1;
            this.menuTreeView.Text = "uiTreeView1";
            this.menuTreeView.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ControlPanel
            // 
            this.ControlPanel.BackColor = System.Drawing.Color.White;
            this.ControlPanel.Controls.Add(this.topTableLayoutPanel);
            this.ControlPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.ControlPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ControlPanel.Location = new System.Drawing.Point(4, 5);
            this.ControlPanel.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.ControlPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.ControlPanel.Name = "ControlPanel";
            this.ControlPanel.Padding = new System.Windows.Forms.Padding(5);
            this.ControlPanel.Size = new System.Drawing.Size(1652, 144);
            this.ControlPanel.TabIndex = 1;
            this.ControlPanel.Text = null;
            this.ControlPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            this.topTableLayoutPanel.ColumnCount = 2;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 65F));
            this.topTableLayoutPanel.Controls.Add(this.topLeftTableLayoutPanel, 0, 0);
            this.topTableLayoutPanel.Controls.Add(this.topRightTableLayoutPanel, 1, 0);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 1;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(1642, 134);
            this.topTableLayoutPanel.TabIndex = 0;
            this.topTableLayoutPanel.TagString = null;
            // 
            // topLeftTableLayoutPanel
            // 
            this.topLeftTableLayoutPanel.ColumnCount = 2;
            this.topLeftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.topLeftTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.topLeftTableLayoutPanel.Controls.Add(this.txt_resourceName, 1, 2);
            this.topLeftTableLayoutPanel.Controls.Add(this.txt_moduleName, 1, 1);
            this.topLeftTableLayoutPanel.Controls.Add(this.txt_className, 1, 0);
            this.topLeftTableLayoutPanel.Controls.Add(this.lbl_resourceName, 0, 2);
            this.topLeftTableLayoutPanel.Controls.Add(this.lbl_className, 0, 0);
            this.topLeftTableLayoutPanel.Controls.Add(this.lbl_moduleName, 0, 1);
            this.topLeftTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topLeftTableLayoutPanel.Location = new System.Drawing.Point(0, 0);
            this.topLeftTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topLeftTableLayoutPanel.Name = "topLeftTableLayoutPanel";
            this.topLeftTableLayoutPanel.RowCount = 3;
            this.topLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.topLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.topLeftTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.topLeftTableLayoutPanel.Size = new System.Drawing.Size(574, 134);
            this.topLeftTableLayoutPanel.TabIndex = 0;
            this.topLeftTableLayoutPanel.TagString = null;
            // 
            // txt_resourceName
            // 
            this.txt_resourceName.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_resourceName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_resourceName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txt_resourceName.Location = new System.Drawing.Point(233, 93);
            this.txt_resourceName.Margin = new System.Windows.Forms.Padding(4, 5, 40, 5);
            this.txt_resourceName.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_resourceName.Name = "txt_resourceName";
            this.txt_resourceName.Padding = new System.Windows.Forms.Padding(5);
            this.txt_resourceName.ShowText = false;
            this.txt_resourceName.Size = new System.Drawing.Size(301, 36);
            this.txt_resourceName.TabIndex = 8;
            this.txt_resourceName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txt_resourceName.Watermark = "";
            // 
            // txt_moduleName
            // 
            this.txt_moduleName.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_moduleName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_moduleName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txt_moduleName.Location = new System.Drawing.Point(233, 49);
            this.txt_moduleName.Margin = new System.Windows.Forms.Padding(4, 5, 40, 5);
            this.txt_moduleName.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_moduleName.Name = "txt_moduleName";
            this.txt_moduleName.Padding = new System.Windows.Forms.Padding(5);
            this.txt_moduleName.ShowText = false;
            this.txt_moduleName.Size = new System.Drawing.Size(301, 34);
            this.txt_moduleName.TabIndex = 7;
            this.txt_moduleName.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txt_moduleName.Watermark = "";
            // 
            // txt_className
            // 
            this.txt_className.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_className.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_className.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txt_className.Location = new System.Drawing.Point(233, 5);
            this.txt_className.Margin = new System.Windows.Forms.Padding(4, 5, 40, 5);
            this.txt_className.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_className.Name = "txt_className";
            this.txt_className.Padding = new System.Windows.Forms.Padding(5);
            this.txt_className.ShowText = false;
            this.txt_className.Size = new System.Drawing.Size(301, 34);
            this.txt_className.TabIndex = 6;
            this.txt_className.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txt_className.Watermark = "";
            // 
            // lbl_resourceName
            // 
            this.lbl_resourceName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_resourceName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_resourceName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lbl_resourceName.Location = new System.Drawing.Point(3, 91);
            this.lbl_resourceName.Margin = new System.Windows.Forms.Padding(3);
            this.lbl_resourceName.Name = "lbl_resourceName";
            this.lbl_resourceName.Size = new System.Drawing.Size(223, 40);
            this.lbl_resourceName.TabIndex = 4;
            this.lbl_resourceName.Text = "资源名称";
            this.lbl_resourceName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_className
            // 
            this.lbl_className.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_className.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_className.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lbl_className.Location = new System.Drawing.Point(3, 3);
            this.lbl_className.Margin = new System.Windows.Forms.Padding(3);
            this.lbl_className.Name = "lbl_className";
            this.lbl_className.Size = new System.Drawing.Size(223, 38);
            this.lbl_className.TabIndex = 3;
            this.lbl_className.Text = "类名";
            this.lbl_className.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // lbl_moduleName
            // 
            this.lbl_moduleName.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbl_moduleName.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lbl_moduleName.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lbl_moduleName.Location = new System.Drawing.Point(3, 47);
            this.lbl_moduleName.Margin = new System.Windows.Forms.Padding(3);
            this.lbl_moduleName.Name = "lbl_moduleName";
            this.lbl_moduleName.Size = new System.Drawing.Size(223, 38);
            this.lbl_moduleName.TabIndex = 5;
            this.lbl_moduleName.Text = "模块名";
            this.lbl_moduleName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topRightTableLayoutPanel
            // 
            this.topRightTableLayoutPanel.ColumnCount = 3;
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.topRightTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 33.33333F));
            this.topRightTableLayoutPanel.Controls.Add(this.btn_save, 2, 1);
            this.topRightTableLayoutPanel.Controls.Add(this.btn_export, 1, 1);
            this.topRightTableLayoutPanel.Controls.Add(this.btn_getTextRes, 0, 1);
            this.topRightTableLayoutPanel.Controls.Add(this.btn_search, 2, 0);
            this.topRightTableLayoutPanel.Controls.Add(this.btn_loadIn, 1, 0);
            this.topRightTableLayoutPanel.Controls.Add(this.btn_getUiRes, 0, 0);
            this.topRightTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topRightTableLayoutPanel.Location = new System.Drawing.Point(574, 0);
            this.topRightTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topRightTableLayoutPanel.Name = "topRightTableLayoutPanel";
            this.topRightTableLayoutPanel.RowCount = 2;
            this.topRightTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topRightTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topRightTableLayoutPanel.Size = new System.Drawing.Size(1068, 134);
            this.topRightTableLayoutPanel.TabIndex = 1;
            this.topRightTableLayoutPanel.TagString = null;
            // 
            // btn_save
            // 
            this.btn_save.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_save.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_save.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_save.Location = new System.Drawing.Point(762, 77);
            this.btn_save.Margin = new System.Windows.Forms.Padding(50, 10, 50, 10);
            this.btn_save.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_save.Name = "btn_save";
            this.btn_save.Size = new System.Drawing.Size(256, 47);
            this.btn_save.TabIndex = 15;
            this.btn_save.Text = "资源保存";
            this.btn_save.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_export
            // 
            this.btn_export.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_export.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_export.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_export.Location = new System.Drawing.Point(436, 77);
            this.btn_export.Margin = new System.Windows.Forms.Padding(80, 10, 80, 10);
            this.btn_export.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_export.Name = "btn_export";
            this.btn_export.Size = new System.Drawing.Size(196, 47);
            this.btn_export.TabIndex = 14;
            this.btn_export.Text = "导出";
            this.btn_export.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_getTextRes
            // 
            this.btn_getTextRes.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_getTextRes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_getTextRes.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_getTextRes.Location = new System.Drawing.Point(50, 77);
            this.btn_getTextRes.Margin = new System.Windows.Forms.Padding(50, 10, 50, 10);
            this.btn_getTextRes.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_getTextRes.Name = "btn_getTextRes";
            this.btn_getTextRes.Size = new System.Drawing.Size(256, 47);
            this.btn_getTextRes.TabIndex = 13;
            this.btn_getTextRes.Text = "文本资源抓取";
            this.btn_getTextRes.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_search
            // 
            this.btn_search.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_search.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_search.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_search.Location = new System.Drawing.Point(762, 10);
            this.btn_search.Margin = new System.Windows.Forms.Padding(50, 10, 50, 10);
            this.btn_search.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_search.Name = "btn_search";
            this.btn_search.Size = new System.Drawing.Size(256, 47);
            this.btn_search.TabIndex = 12;
            this.btn_search.Text = "资源搜索";
            this.btn_search.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_loadIn
            // 
            this.btn_loadIn.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_loadIn.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_loadIn.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_loadIn.Location = new System.Drawing.Point(436, 10);
            this.btn_loadIn.Margin = new System.Windows.Forms.Padding(80, 10, 80, 10);
            this.btn_loadIn.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_loadIn.Name = "btn_loadIn";
            this.btn_loadIn.Radius = 1;
            this.btn_loadIn.Size = new System.Drawing.Size(196, 47);
            this.btn_loadIn.TabIndex = 11;
            this.btn_loadIn.Text = "导入";
            this.btn_loadIn.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_getUiRes
            // 
            this.btn_getUiRes.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_getUiRes.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_getUiRes.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_getUiRes.Location = new System.Drawing.Point(50, 10);
            this.btn_getUiRes.Margin = new System.Windows.Forms.Padding(50, 10, 50, 10);
            this.btn_getUiRes.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_getUiRes.Name = "btn_getUiRes";
            this.btn_getUiRes.Size = new System.Drawing.Size(256, 47);
            this.btn_getUiRes.TabIndex = 9;
            this.btn_getUiRes.Text = "获取UI资源";
            this.btn_getUiRes.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // FrmResourceConfig
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(1660, 1032);
            this.Controls.Add(this.roottableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FrmResourceConfig";
            this.Text = "";
            this.TitleFont = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.roottableLayoutPanel.ResumeLayout(false);
            this.downtableLayoutPanel.ResumeLayout(false);
            this.resouceTabControl.ResumeLayout(false);
            this.tp_UI.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_UI)).EndInit();
            this.tp_Text.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_Text)).EndInit();
            this.ControlPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.topLeftTableLayoutPanel.ResumeLayout(false);
            this.topRightTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel roottableLayoutPanel;
        private System.Windows.Forms.TableLayoutPanel downtableLayoutPanel;
        private Sunny.UI.UITabControl resouceTabControl;
        private System.Windows.Forms.TabPage tp_UI;
        private System.Windows.Forms.TabPage tp_Text;
        private Sunny.UI.UIPanel ControlPanel;
        private Sunny.UI.UITreeView menuTreeView;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel topLeftTableLayoutPanel;
        private Sunny.UI.UILabel lbl_moduleName;
        private Sunny.UI.UILabel lbl_resourceName;
        private Sunny.UI.UILabel lbl_className;
        private Sunny.UI.UITextBox txt_className;
        private Sunny.UI.UITextBox txt_moduleName;
        private Sunny.UI.UITextBox txt_resourceName;
        private Sunny.UI.UITableLayoutPanel topRightTableLayoutPanel;
        private Sunny.UI.UIButton btn_getUiRes;
        private Sunny.UI.UIButton btn_loadIn;
        private Sunny.UI.UIButton btn_search;
        private Sunny.UI.UIButton btn_getTextRes;
        private Sunny.UI.UIButton btn_export;
        private Sunny.UI.UIButton btn_save;
        private Sunny.UI.UIDataGridView dgv_UI;
        private Sunny.UI.UIDataGridView dgv_Text;
    }
}