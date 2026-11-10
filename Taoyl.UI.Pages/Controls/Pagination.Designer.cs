namespace Taoyl.UI.Pages.Controls
{
    partial class Pagination
    {
        /// <summary> 
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region 组件设计器生成的代码

        /// <summary> 
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            this.panelBar = new System.Windows.Forms.TableLayoutPanel();
            this.btnPrev = new Sunny.UI.UISymbolButton();
            this.btnNext = new Sunny.UI.UISymbolButton();
            this.btnPage1 = new Sunny.UI.UISymbolButton();
            this.btnPage2 = new Sunny.UI.UISymbolButton();
            this.btnPage3 = new Sunny.UI.UISymbolButton();
            this.btnPage4 = new Sunny.UI.UISymbolButton();
            this.btnPage5 = new Sunny.UI.UISymbolButton();
            this.btnPage6 = new Sunny.UI.UISymbolButton();
            this.btnPage7 = new Sunny.UI.UISymbolButton();
            this.btnPage8 = new Sunny.UI.UISymbolButton();
            this.btnPage9 = new Sunny.UI.UISymbolButton();
            this.lblJumpLeft = new Sunny.UI.UILabel();
            this.txtJump = new Sunny.UI.UITextBox();
            this.lblJumpRight = new Sunny.UI.UILabel();
            this.btnJump = new Sunny.UI.UISymbolButton();
            this.panelBar.SuspendLayout();
            this.SuspendLayout();
            // 
            // panelBar
            // 
            // 列序即控件出场顺序：上一页 | 下一页 | 9 个页码槽 | 第 | 输入框 | 页 | 确定 | 末尾吸收列
            // 列宽全是 Absolute，运行时由 Pagination.ApplyColumnWidths 按尺寸属性重写；
            // 此处字面量是 100% 设计基准值（= 属性默认值），只用于设计期预览。
            // 末列 Percent 100 是**吸收列**（无控件）：所有内容列都是 Absolute，
            // 宿主把控件拉宽时余量全部落到这一列 ⇒ 内容贴左、右侧留白，不会有某一列被莫名撑开。
            // 🔴 行样式必须显式写：只设列不设行时，该行会退化成 AutoSize（框架实证），高 DPI 下文字底部被裁。
            this.panelBar.ColumnCount = 16;
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 78F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 31F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 56F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 24F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 64F));
            this.panelBar.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelBar.Controls.Add(this.btnPrev, 0, 0);
            this.panelBar.Controls.Add(this.btnNext, 1, 0);
            this.panelBar.Controls.Add(this.btnPage1, 2, 0);
            this.panelBar.Controls.Add(this.btnPage2, 3, 0);
            this.panelBar.Controls.Add(this.btnPage3, 4, 0);
            this.panelBar.Controls.Add(this.btnPage4, 5, 0);
            this.panelBar.Controls.Add(this.btnPage5, 6, 0);
            this.panelBar.Controls.Add(this.btnPage6, 7, 0);
            this.panelBar.Controls.Add(this.btnPage7, 8, 0);
            this.panelBar.Controls.Add(this.btnPage8, 9, 0);
            this.panelBar.Controls.Add(this.btnPage9, 10, 0);
            this.panelBar.Controls.Add(this.lblJumpLeft, 11, 0);
            this.panelBar.Controls.Add(this.txtJump, 12, 0);
            this.panelBar.Controls.Add(this.lblJumpRight, 13, 0);
            this.panelBar.Controls.Add(this.btnJump, 14, 0);
            this.panelBar.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panelBar.Location = new System.Drawing.Point(0, 0);
            this.panelBar.Margin = new System.Windows.Forms.Padding(0);
            this.panelBar.Name = "panelBar";
            this.panelBar.RowCount = 1;
            this.panelBar.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.panelBar.Size = new System.Drawing.Size(606, 37);
            this.panelBar.TabIndex = 0;
            // 
            // btnPrev
            // 
            this.btnPrev.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPrev.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPrev.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPrev.ImageAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnPrev.Margin = new System.Windows.Forms.Padding(0);
            this.btnPrev.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPrev.Name = "btnPrev";
            this.btnPrev.Padding = new System.Windows.Forms.Padding(5, 0, 5, 0);
            this.btnPrev.RadiusSides = Sunny.UI.UICornerRadiusSides.LeftTop | Sunny.UI.UICornerRadiusSides.LeftBottom;
            this.btnPrev.Size = new System.Drawing.Size(75, 29);
            this.btnPrev.Symbol = 61700;
            this.btnPrev.TabIndex = 0;
            this.btnPrev.TagString = "<";
            this.btnPrev.Text = "上一页";
            this.btnPrev.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnPrev.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnNext
            // 
            this.btnNext.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnNext.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnNext.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnNext.ImageAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.btnNext.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnNext.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnNext.Name = "btnNext";
            this.btnNext.Padding = new System.Windows.Forms.Padding(6, 0, 5, 0);
            this.btnNext.RadiusSides = Sunny.UI.UICornerRadiusSides.RightTop | Sunny.UI.UICornerRadiusSides.RightBottom;
            this.btnNext.Size = new System.Drawing.Size(75, 29);
            this.btnNext.Symbol = 61701;
            this.btnNext.TabIndex = 1;
            this.btnNext.TagString = ">";
            this.btnNext.Text = "下一页";
            this.btnNext.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.btnNext.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage1
            // 
            this.btnPage1.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage1.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage1.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage1.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage1.Name = "btnPage1";
            this.btnPage1.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage1.Size = new System.Drawing.Size(29, 29);
            this.btnPage1.Symbol = 0;
            this.btnPage1.TabIndex = 2;
            this.btnPage1.Text = "1";
            this.btnPage1.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage2
            // 
            this.btnPage2.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage2.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage2.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage2.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage2.Name = "btnPage2";
            this.btnPage2.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage2.Size = new System.Drawing.Size(29, 29);
            this.btnPage2.Symbol = 0;
            this.btnPage2.TabIndex = 3;
            this.btnPage2.Text = "2";
            this.btnPage2.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage3
            // 
            this.btnPage3.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage3.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage3.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage3.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage3.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage3.Name = "btnPage3";
            this.btnPage3.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage3.Size = new System.Drawing.Size(29, 29);
            this.btnPage3.Symbol = 0;
            this.btnPage3.TabIndex = 4;
            this.btnPage3.Text = "3";
            this.btnPage3.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage4
            // 
            this.btnPage4.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage4.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage4.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage4.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage4.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage4.Name = "btnPage4";
            this.btnPage4.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage4.Size = new System.Drawing.Size(29, 29);
            this.btnPage4.Symbol = 0;
            this.btnPage4.TabIndex = 5;
            this.btnPage4.Text = "4";
            this.btnPage4.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage5
            // 
            this.btnPage5.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage5.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage5.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage5.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage5.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage5.Name = "btnPage5";
            this.btnPage5.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage5.Size = new System.Drawing.Size(29, 29);
            this.btnPage5.Symbol = 0;
            this.btnPage5.TabIndex = 6;
            this.btnPage5.Text = "5";
            this.btnPage5.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage6
            // 
            this.btnPage6.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage6.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage6.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage6.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage6.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage6.Name = "btnPage6";
            this.btnPage6.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage6.Size = new System.Drawing.Size(29, 29);
            this.btnPage6.Symbol = 0;
            this.btnPage6.TabIndex = 7;
            this.btnPage6.Text = "6";
            this.btnPage6.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage7
            // 
            this.btnPage7.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage7.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage7.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage7.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage7.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage7.Name = "btnPage7";
            this.btnPage7.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage7.Size = new System.Drawing.Size(29, 29);
            this.btnPage7.Symbol = 0;
            this.btnPage7.TabIndex = 8;
            this.btnPage7.Text = "7";
            this.btnPage7.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage8
            // 
            this.btnPage8.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage8.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage8.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage8.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage8.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage8.Name = "btnPage8";
            this.btnPage8.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage8.Size = new System.Drawing.Size(29, 29);
            this.btnPage8.Symbol = 0;
            this.btnPage8.TabIndex = 9;
            this.btnPage8.Text = "8";
            this.btnPage8.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // btnPage9
            // 
            this.btnPage9.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnPage9.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnPage9.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnPage9.Margin = new System.Windows.Forms.Padding(2, 0, 0, 0);
            this.btnPage9.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnPage9.Name = "btnPage9";
            this.btnPage9.RadiusSides = Sunny.UI.UICornerRadiusSides.None;
            this.btnPage9.Size = new System.Drawing.Size(29, 29);
            this.btnPage9.Symbol = 0;
            this.btnPage9.TabIndex = 10;
            this.btnPage9.Text = "9";
            this.btnPage9.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // lblJumpLeft
            // 
            this.lblJumpLeft.AutoSize = false;
            this.lblJumpLeft.BackColor = System.Drawing.Color.Transparent;
            this.lblJumpLeft.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJumpLeft.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblJumpLeft.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblJumpLeft.Margin = new System.Windows.Forms.Padding(9, 0, 0, 0);
            this.lblJumpLeft.Name = "lblJumpLeft";
            this.lblJumpLeft.Size = new System.Drawing.Size(21, 29);
            this.lblJumpLeft.TabIndex = 11;
            this.lblJumpLeft.Text = "第";
            this.lblJumpLeft.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtJump
            // 
            this.txtJump.Cursor = System.Windows.Forms.Cursors.IBeam;
            this.txtJump.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtJump.DoubleValue = 1D;
            this.txtJump.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.txtJump.IntValue = 1;
            this.txtJump.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.txtJump.Maximum = 100D;
            this.txtJump.Minimum = 1D;
            this.txtJump.MinimumSize = new System.Drawing.Size(1, 1);
            this.txtJump.Name = "txtJump";
            this.txtJump.Padding = new System.Windows.Forms.Padding(5);
            this.txtJump.ShowText = false;
            this.txtJump.Size = new System.Drawing.Size(53, 29);
            this.txtJump.TabIndex = 12;
            this.txtJump.Text = "1";
            this.txtJump.TextAlignment = System.Drawing.ContentAlignment.MiddleCenter;
            this.txtJump.Type = Sunny.UI.UITextBox.UIEditType.Integer;
            this.txtJump.Watermark = "";
            // 
            // lblJumpRight
            // 
            this.lblJumpRight.AutoSize = false;
            this.lblJumpRight.BackColor = System.Drawing.Color.Transparent;
            this.lblJumpRight.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblJumpRight.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lblJumpRight.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(48)))), ((int)(((byte)(48)))), ((int)(((byte)(48)))));
            this.lblJumpRight.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.lblJumpRight.Name = "lblJumpRight";
            this.lblJumpRight.Size = new System.Drawing.Size(21, 29);
            this.lblJumpRight.TabIndex = 13;
            this.lblJumpRight.Text = "页";
            this.lblJumpRight.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnJump
            // 
            this.btnJump.Cursor = System.Windows.Forms.Cursors.Hand;
            this.btnJump.Dock = System.Windows.Forms.DockStyle.Fill;
            this.btnJump.Font = new System.Drawing.Font("微软雅黑", 10.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.btnJump.Margin = new System.Windows.Forms.Padding(3, 0, 0, 0);
            this.btnJump.MinimumSize = new System.Drawing.Size(1, 1);
            this.btnJump.Name = "btnJump";
            this.btnJump.RadiusSides = Sunny.UI.UICornerRadiusSides.All;
            this.btnJump.Size = new System.Drawing.Size(61, 29);
            this.btnJump.Symbol = 0;
            this.btnJump.TabIndex = 14;
            this.btnJump.Text = "确定";
            this.btnJump.TipsFont = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            // 
            // Pagination
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.Controls.Add(this.panelBar);
            this.Font = new System.Drawing.Font("微软雅黑", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "Pagination";
            this.Size = new System.Drawing.Size(606, 37);
            this.panelBar.ResumeLayout(false);
            this.panelBar.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel panelBar;
        private Sunny.UI.UISymbolButton btnPrev;
        private Sunny.UI.UISymbolButton btnNext;
        private Sunny.UI.UISymbolButton btnPage1;
        private Sunny.UI.UISymbolButton btnPage2;
        private Sunny.UI.UISymbolButton btnPage3;
        private Sunny.UI.UISymbolButton btnPage4;
        private Sunny.UI.UISymbolButton btnPage5;
        private Sunny.UI.UISymbolButton btnPage6;
        private Sunny.UI.UISymbolButton btnPage7;
        private Sunny.UI.UISymbolButton btnPage8;
        private Sunny.UI.UISymbolButton btnPage9;
        private Sunny.UI.UILabel lblJumpLeft;
        private Sunny.UI.UITextBox txtJump;
        private Sunny.UI.UILabel lblJumpRight;
        private Sunny.UI.UISymbolButton btnJump;
    }
}
