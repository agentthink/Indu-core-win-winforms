namespace Taoyl.UI.Pages
{
    partial class FrmResourceScanner
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
            this.rootTableLayoutPanel = new System.Windows.Forms.TableLayoutPanel();
            this.dgv_resource = new System.Windows.Forms.DataGridView();
            this.topPanel = new Sunny.UI.UIPanel();
            this.topTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.txt_resourcePath = new Sunny.UI.UITextBox();
            this.btn_CS = new Sunny.UI.UIButton();
            this.btn_folder = new Sunny.UI.UIButton();
            this.btn_export = new Sunny.UI.UIButton();
            this.btn_start = new Sunny.UI.UIButton();
            this.rootTableLayoutPanel.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgv_resource)).BeginInit();
            this.topPanel.SuspendLayout();
            this.topTableLayoutPanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // rootTableLayoutPanel
            // 
            this.rootTableLayoutPanel.ColumnCount = 1;
            this.rootTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.rootTableLayoutPanel.Controls.Add(this.dgv_resource, 0, 1);
            this.rootTableLayoutPanel.Controls.Add(this.topPanel, 0, 0);
            this.rootTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.rootTableLayoutPanel.Location = new System.Drawing.Point(0, 35);
            this.rootTableLayoutPanel.Name = "rootTableLayoutPanel";
            this.rootTableLayoutPanel.RowCount = 2;
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 19.71383F));
            this.rootTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 80.28617F));
            this.rootTableLayoutPanel.Size = new System.Drawing.Size(916, 629);
            this.rootTableLayoutPanel.TabIndex = 0;
            // 
            // dgv_resource
            // 
            this.dgv_resource.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgv_resource.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dgv_resource.Location = new System.Drawing.Point(3, 127);
            this.dgv_resource.Name = "dgv_resource";
            this.dgv_resource.RowTemplate.Height = 23;
            this.dgv_resource.Size = new System.Drawing.Size(910, 499);
            this.dgv_resource.TabIndex = 0;
            // 
            // topPanel
            // 
            this.topPanel.Controls.Add(this.topTableLayoutPanel);
            this.topPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topPanel.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.topPanel.Location = new System.Drawing.Point(3, 3);
            this.topPanel.MinimumSize = new System.Drawing.Size(1, 1);
            this.topPanel.Name = "topPanel";
            this.topPanel.Padding = new System.Windows.Forms.Padding(5);
            this.topPanel.Size = new System.Drawing.Size(910, 118);
            this.topPanel.TabIndex = 1;
            this.topPanel.Text = null;
            this.topPanel.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // topTableLayoutPanel
            // 
            this.topTableLayoutPanel.ColumnCount = 3;
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 60F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.topTableLayoutPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 20F));
            this.topTableLayoutPanel.Controls.Add(this.btn_start, 1, 1);
            this.topTableLayoutPanel.Controls.Add(this.btn_CS, 1, 0);
            this.topTableLayoutPanel.Controls.Add(this.btn_folder, 2, 0);
            this.topTableLayoutPanel.Controls.Add(this.txt_resourcePath, 0, 0);
            this.topTableLayoutPanel.Controls.Add(this.btn_export, 2, 1);
            this.topTableLayoutPanel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.topTableLayoutPanel.Location = new System.Drawing.Point(5, 5);
            this.topTableLayoutPanel.Margin = new System.Windows.Forms.Padding(0);
            this.topTableLayoutPanel.Name = "topTableLayoutPanel";
            this.topTableLayoutPanel.RowCount = 2;
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topTableLayoutPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.topTableLayoutPanel.Size = new System.Drawing.Size(900, 108);
            this.topTableLayoutPanel.TabIndex = 0;
            this.topTableLayoutPanel.TagString = null;
            // 
            // txt_resourcePath
            // 
            this.txt_resourcePath.ButtonSymbol = 61761;
            this.txt_resourcePath.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txt_resourcePath.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txt_resourcePath.Font = new System.Drawing.Font("微软雅黑", 9F);
            this.txt_resourcePath.Location = new System.Drawing.Point(4, 5);
            this.txt_resourcePath.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            this.txt_resourcePath.MinimumSize = new System.Drawing.Size(1, 16);
            this.txt_resourcePath.Name = "txt_resourcePath";
            this.txt_resourcePath.Padding = new System.Windows.Forms.Padding(5);
            this.txt_resourcePath.RectColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(100)))), ((int)(((byte)(255)))));
            this.txt_resourcePath.ShowText = false;
            this.txt_resourcePath.Size = new System.Drawing.Size(532, 44);
            this.txt_resourcePath.Style = Sunny.UI.UIStyle.Custom;
            this.txt_resourcePath.TabIndex = 1;
            this.txt_resourcePath.TextAlignment = System.Drawing.ContentAlignment.MiddleLeft;
            this.txt_resourcePath.Watermark = "";
            // 
            // btn_CS
            // 
            this.btn_CS.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_CS.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_CS.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_CS.Location = new System.Drawing.Point(560, 8);
            this.btn_CS.Margin = new System.Windows.Forms.Padding(20, 8, 20, 8);
            this.btn_CS.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_CS.Name = "btn_CS";
            this.btn_CS.Size = new System.Drawing.Size(140, 38);
            this.btn_CS.TabIndex = 7;
            this.btn_CS.Text = ".cs文件选择";
            this.btn_CS.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_folder
            // 
            this.btn_folder.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_folder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_folder.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_folder.Location = new System.Drawing.Point(740, 8);
            this.btn_folder.Margin = new System.Windows.Forms.Padding(20, 8, 20, 8);
            this.btn_folder.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_folder.Name = "btn_folder";
            this.btn_folder.Size = new System.Drawing.Size(140, 38);
            this.btn_folder.TabIndex = 6;
            this.btn_folder.Text = "文件夹选择";
            this.btn_folder.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_export
            // 
            this.btn_export.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_export.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_export.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_export.Location = new System.Drawing.Point(740, 62);
            this.btn_export.Margin = new System.Windows.Forms.Padding(20, 8, 20, 8);
            this.btn_export.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_export.Name = "btn_export";
            this.btn_export.Size = new System.Drawing.Size(140, 38);
            this.btn_export.TabIndex = 9;
            this.btn_export.Text = "导出";
            this.btn_export.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btn_start
            // 
            this.btn_start.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btn_start.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btn_start.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btn_start.Location = new System.Drawing.Point(560, 62);
            this.btn_start.Margin = new System.Windows.Forms.Padding(20, 8, 20, 8);
            this.btn_start.MinimumSize = new System.Drawing.Size(1, 1);
            this.btn_start.Name = "btn_start";
            this.btn_start.Size = new System.Drawing.Size(140, 38);
            this.btn_start.TabIndex = 8;
            this.btn_start.Text = "开始";
            this.btn_start.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // FrmResourceScanner
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.ClientSize = new System.Drawing.Size(916, 664);
            this.Controls.Add(this.rootTableLayoutPanel);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Name = "FrmResourceScanner";
            this.Text = "FrmResourceScanner";
            this.ZoomScaleRect = new System.Drawing.Rectangle(15, 15, 800, 450);
            this.rootTableLayoutPanel.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dgv_resource)).EndInit();
            this.topPanel.ResumeLayout(false);
            this.topTableLayoutPanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel rootTableLayoutPanel;
        private System.Windows.Forms.DataGridView dgv_resource;
        private Sunny.UI.UIPanel topPanel;
        private Sunny.UI.UITableLayoutPanel topTableLayoutPanel;
        private Sunny.UI.UITextBox txt_resourcePath;
        private Sunny.UI.UIButton btn_CS;
        private Sunny.UI.UIButton btn_folder;
        private Sunny.UI.UIButton btn_start;
        private Sunny.UI.UIButton btn_export;
    }
}