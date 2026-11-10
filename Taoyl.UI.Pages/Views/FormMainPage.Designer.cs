using System.Drawing;

namespace Taoyl.UI.Pages
{
    partial class FormMainPage
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(FormMainPage));
            this.mainTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.leftTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.uiLine1 = new Sunny.UI.UILine();
            this.leftTopTableLayoutPanel1 = new Sunny.UI.UITableLayoutPanel();
            this.pnlConnStatusTitle = new Sunny.UI.UILabel();
            this.lblConnStatus = new Sunny.UI.UILine();
            this.uiLine3 = new Sunny.UI.UILine();
            this.uiLine4 = new Sunny.UI.UILine();
            this.rightTableLayoutPanel = new Sunny.UI.UITableLayoutPanel();
            this.tabControlLog = new Sunny.UI.UITabControl();
            this.tabInfoLog = new System.Windows.Forms.TabPage();
            this.gvInfoLog = new Sunny.UI.UIDataGridView();
            this.tabWarnLog = new System.Windows.Forms.TabPage();
            this.gvWarnLog = new Sunny.UI.UIDataGridView();
            this.tabMESLog = new System.Windows.Forms.TabPage();
            this.gvMESLog = new Sunny.UI.UIDataGridView();
            this.tabErrorLog = new System.Windows.Forms.TabPage();
            this.gvErrorLog = new Sunny.UI.UIDataGridView();
            this.pnlLayout = new Sunny.UI.UIPanel();
            this.dashedLinePanel1 = new Taoyl.UI.Pages.DashedLinePanel();
            this.mainTableLayoutPanel.SuspendLayout();
            this.leftTableLayoutPanel.SuspendLayout();
            this.leftTopTableLayoutPanel1.SuspendLayout();
            this.rightTableLayoutPanel.SuspendLayout();
            this.tabControlLog.SuspendLayout();
            this.tabInfoLog.SuspendLayout();
            this.tabMESLog.SuspendLayout();
            this.tabErrorLog.SuspendLayout();
            this.SuspendLayout();
            //
            // mainTableLayoutPanel
            //
            this.mainTableLayoutPanel.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.mainTableLayoutPanel, "mainTableLayoutPanel");
            this.mainTableLayoutPanel.Controls.Add(this.leftTableLayoutPanel, 0, 0);
            this.mainTableLayoutPanel.Controls.Add(this.rightTableLayoutPanel, 1, 0);
            this.mainTableLayoutPanel.Name = "mainTableLayoutPanel";
            this.mainTableLayoutPanel.TagString = null;
            //
            // leftTableLayoutPanel
            //
            resources.ApplyResources(this.leftTableLayoutPanel, "leftTableLayoutPanel");
            this.leftTableLayoutPanel.Controls.Add(this.uiLine1, 0, 3);
            this.leftTableLayoutPanel.Controls.Add(this.leftTopTableLayoutPanel1, 0, 0);
            this.leftTableLayoutPanel.Controls.Add(this.uiLine3, 0, 1);
            this.leftTableLayoutPanel.Controls.Add(this.dashedLinePanel1, 0, 2);
            this.leftTableLayoutPanel.Controls.Add(this.uiLine4, 0, 5);
            this.leftTableLayoutPanel.Name = "leftTableLayoutPanel";
            this.leftTableLayoutPanel.TagString = null;
            //
            // uiLine1
            //
            this.uiLine1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.uiLine1, "uiLine1");
            this.uiLine1.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLine1.Name = "uiLine1";
            //
            // leftTopTableLayoutPanel1
            //
            resources.ApplyResources(this.leftTopTableLayoutPanel1, "leftTopTableLayoutPanel1");
            this.leftTopTableLayoutPanel1.Controls.Add(this.pnlConnStatusTitle, 1, 0);
            this.leftTopTableLayoutPanel1.Controls.Add(this.lblConnStatus, 0, 0);
            this.leftTopTableLayoutPanel1.Name = "leftTopTableLayoutPanel1";
            this.leftTopTableLayoutPanel1.TagString = null;
            //
            // pnlConnStatusTitle
            //
            this.pnlConnStatusTitle.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.pnlConnStatusTitle, "pnlConnStatusTitle");
            this.pnlConnStatusTitle.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.pnlConnStatusTitle.Name = "pnlConnStatusTitle";
            //
            // lblConnStatus
            //
            this.lblConnStatus.BackColor = System.Drawing.Color.Transparent;
            this.lblConnStatus.Direction = Sunny.UI.UILine.LineDirection.Vertical;
            resources.ApplyResources(this.lblConnStatus, "lblConnStatus");
            this.lblConnStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblConnStatus.LineColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(216)))), ((int)(((byte)(240)))));
            this.lblConnStatus.LineSize = 5;
            this.lblConnStatus.Name = "lblConnStatus";
            this.lblConnStatus.Style = Sunny.UI.UIStyle.Custom;
            //
            // uiLine3
            //
            this.uiLine3.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.uiLine3, "uiLine3");
            this.uiLine3.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLine3.Name = "uiLine3";
            //
            // uiLine4
            //
            this.uiLine4.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.uiLine4, "uiLine4");
            this.uiLine4.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.uiLine4.Name = "uiLine4";
            //
            // rightTableLayoutPanel
            //
            resources.ApplyResources(this.rightTableLayoutPanel, "rightTableLayoutPanel");
            this.rightTableLayoutPanel.Controls.Add(this.tabControlLog, 0, 1);
            this.rightTableLayoutPanel.Controls.Add(this.pnlLayout, 0, 0);
            this.rightTableLayoutPanel.Name = "rightTableLayoutPanel";
            this.rightTableLayoutPanel.TagString = null;
            //
            // tabControlLog
            //
            this.tabControlLog.Controls.Add(this.tabInfoLog);
            this.tabControlLog.Controls.Add(this.tabWarnLog);
            this.tabControlLog.Controls.Add(this.tabErrorLog);
            this.tabControlLog.Controls.Add(this.tabMESLog);
            resources.ApplyResources(this.tabControlLog, "tabControlLog");
            this.tabControlLog.DrawMode = System.Windows.Forms.TabDrawMode.OwnerDrawFixed;
            this.tabControlLog.MainPage = "";
            this.tabControlLog.MenuStyle = Sunny.UI.UIMenuStyle.Custom;
            this.tabControlLog.Name = "tabControlLog";
            this.tabControlLog.SelectedIndex = 0;
            this.tabControlLog.ShowTabDivider = false;
            this.tabControlLog.SizeMode = System.Windows.Forms.TabSizeMode.Fixed;
            this.tabControlLog.Style = Sunny.UI.UIStyle.Custom;
            this.tabControlLog.TabBackColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.tabControlLog.TabPageTextAlignment = System.Windows.Forms.HorizontalAlignment.Center;
            this.tabControlLog.TabSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.tabControlLog.TabUnSelectedColor = System.Drawing.Color.FromArgb(((int)(((byte)(243)))), ((int)(((byte)(249)))), ((int)(((byte)(255)))));
            this.tabControlLog.TabUnSelectedForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(25)))), ((int)(((byte)(100)))));
            this.tabControlLog.TipsFont = new System.Drawing.Font("微软雅黑", 9F, System.Drawing.FontStyle.Bold);
            this.tabControlLog.TipsForeColor = System.Drawing.Color.Black;
            //
            // tabInfoLog
            //
            this.tabInfoLog.Controls.Add(this.gvInfoLog);
            resources.ApplyResources(this.tabInfoLog, "tabInfoLog");
            this.tabInfoLog.Name = "tabInfoLog";
            this.tabInfoLog.UseVisualStyleBackColor = true;
            //
            // gvInfoLog
            //
            this.gvInfoLog.AllowUserToAddRows = false;
            this.gvInfoLog.AllowUserToDeleteRows = false;
            this.gvInfoLog.AllowUserToResizeRows = false;
            this.gvInfoLog.BackgroundColor = System.Drawing.Color.White;
            this.gvInfoLog.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gvInfoLog.ColumnHeadersVisible = false;
            this.gvInfoLog.ReadOnly = true;
            this.gvInfoLog.RowHeadersVisible = false;
            this.gvInfoLog.VirtualMode = true;
            resources.ApplyResources(this.gvInfoLog, "gvInfoLog");
            this.gvInfoLog.Name = "gvInfoLog";
            //
            // tabWarnLog
            //
            this.tabWarnLog.Controls.Add(this.gvWarnLog);
            resources.ApplyResources(this.tabWarnLog, "tabWarnLog");
            this.tabWarnLog.Name = "tabWarnLog";
            this.tabWarnLog.UseVisualStyleBackColor = true;
            //
            // gvWarnLog
            //
            this.gvWarnLog.AllowUserToAddRows = false;
            this.gvWarnLog.AllowUserToDeleteRows = false;
            this.gvWarnLog.AllowUserToResizeRows = false;
            this.gvWarnLog.BackgroundColor = System.Drawing.Color.White;
            this.gvWarnLog.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gvWarnLog.ColumnHeadersVisible = false;
            this.gvWarnLog.ReadOnly = true;
            this.gvWarnLog.RowHeadersVisible = false;
            this.gvWarnLog.VirtualMode = true;
            resources.ApplyResources(this.gvWarnLog, "gvWarnLog");
            this.gvWarnLog.Name = "gvWarnLog";
            //
            // tabMESLog
            //
            this.tabMESLog.Controls.Add(this.gvMESLog);
            resources.ApplyResources(this.tabMESLog, "tabMESLog");
            this.tabMESLog.Name = "tabMESLog";
            this.tabMESLog.UseVisualStyleBackColor = true;
            //
            // gvMESLog
            //
            this.gvMESLog.AllowUserToAddRows = false;
            this.gvMESLog.AllowUserToDeleteRows = false;
            this.gvMESLog.AllowUserToResizeRows = false;
            this.gvMESLog.BackgroundColor = System.Drawing.Color.White;
            this.gvMESLog.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gvMESLog.ColumnHeadersVisible = false;
            this.gvMESLog.ReadOnly = true;
            this.gvMESLog.RowHeadersVisible = false;
            this.gvMESLog.VirtualMode = true;
            resources.ApplyResources(this.gvMESLog, "gvMESLog");
            this.gvMESLog.Name = "gvMESLog";
            //
            // tabErrorLog
            //
            this.tabErrorLog.Controls.Add(this.gvErrorLog);
            resources.ApplyResources(this.tabErrorLog, "tabErrorLog");
            this.tabErrorLog.Name = "tabErrorLog";
            this.tabErrorLog.UseVisualStyleBackColor = true;
            //
            // gvErrorLog
            //
            this.gvErrorLog.AllowUserToAddRows = false;
            this.gvErrorLog.AllowUserToDeleteRows = false;
            this.gvErrorLog.AllowUserToResizeRows = false;
            this.gvErrorLog.BackgroundColor = System.Drawing.Color.White;
            this.gvErrorLog.CellBorderStyle = System.Windows.Forms.DataGridViewCellBorderStyle.None;
            this.gvErrorLog.ColumnHeadersVisible = false;
            this.gvErrorLog.ReadOnly = true;
            this.gvErrorLog.RowHeadersVisible = false;
            this.gvErrorLog.VirtualMode = true;
            resources.ApplyResources(this.gvErrorLog, "gvErrorLog");
            this.gvErrorLog.Name = "gvErrorLog";
            //
            // pnlLayout
            //
            resources.ApplyResources(this.pnlLayout, "pnlLayout");
            this.pnlLayout.BackColor = System.Drawing.Color.Transparent;
            this.pnlLayout.BackgroundImage = global::Taoyl.UI.Pages.Properties.Resources.注液;
            this.pnlLayout.Name = "pnlLayout";
            this.pnlLayout.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            //
            // dashedLinePanel1
            //
            this.dashedLinePanel1.BackColor = System.Drawing.Color.Transparent;
            resources.ApplyResources(this.dashedLinePanel1, "dashedLinePanel1");
            this.dashedLinePanel1.LineMargin = 10;
            this.dashedLinePanel1.LineWidth = 5;
            this.dashedLinePanel1.Name = "dashedLinePanel1";
            this.dashedLinePanel1.SegmentHeight = 24;
            //
            // FormMainPage
            //
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackgroundImage = global::Taoyl.UI.Pages.Properties.Resources.首页背景;
            resources.ApplyResources(this, "$this");
            this.ControlBox = true;
            this.Controls.Add(this.mainTableLayoutPanel);
            this.Name = "FormMainPage";
            this.mainTableLayoutPanel.ResumeLayout(false);
            this.leftTableLayoutPanel.ResumeLayout(false);
            this.leftTopTableLayoutPanel1.ResumeLayout(false);
            this.rightTableLayoutPanel.ResumeLayout(false);
            this.tabControlLog.ResumeLayout(false);
            this.tabInfoLog.ResumeLayout(false);
            this.tabMESLog.ResumeLayout(false);
            this.tabErrorLog.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private Sunny.UI.UITableLayoutPanel mainTableLayoutPanel;
        private Sunny.UI.UITableLayoutPanel leftTableLayoutPanel;
        private Sunny.UI.UILine uiLine4;
        private Sunny.UI.UILine uiLine3;
        private DashedLinePanel dashedLinePanel1;
        private Sunny.UI.UITableLayoutPanel leftTopTableLayoutPanel1;
        private Sunny.UI.UILine lblConnStatus;
        private Sunny.UI.UILine uiLine1;
        private Sunny.UI.UILabel pnlConnStatusTitle;
        private Sunny.UI.UITableLayoutPanel rightTableLayoutPanel;
        private Sunny.UI.UIPanel pnlLayout;
        private Sunny.UI.UITabControl tabControlLog;
        private System.Windows.Forms.TabPage tabInfoLog;
        private Sunny.UI.UIDataGridView gvInfoLog;
        private System.Windows.Forms.TabPage tabWarnLog;
        private Sunny.UI.UIDataGridView gvWarnLog;
        private System.Windows.Forms.TabPage tabMESLog;
        private Sunny.UI.UIDataGridView gvMESLog;
        private System.Windows.Forms.TabPage tabErrorLog;
        private Sunny.UI.UIDataGridView gvErrorLog;
    }
}
