using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.Application.Product.Models;

namespace Taoyl.UI.Pages.Controls
{
    /// <summary>
    /// 产品两页（实时/查询）共用的表格管道：网格初始化、按列元数据建列、换源保滚动。
    /// 由来：两页 SetupDgv/RebuildColumns/RebindRows 曾逐行同构（约 80 行 × 2），第二实例出现后收敛于此。
    /// 列宽按类型分档（时间列加宽容纳 yyyy-MM-dd HH:mm:ss，其余固定基准），不做列显隐/列宽记忆。
    /// </summary>
    public static class ProductGridHelper
    {
        private const int RowHeightBasis = 36;
        private const int ColumnHeaderHeightBasis = 36;
        private const float GridFontSize = 12F;
        private const int ColumnWidthBasis = 110;
        private const int TimeColumnWidthBasis = 170;

        /// <summary>只读查询网格统一初始化（白底斑马纹、横竖滚动条、行列尺寸按 DPI 缩放）。</summary>
        public static void Setup(DataGridView grid, float scale)
        {
            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;
            grid.BackgroundColor = Color.White;
            grid.ScrollBars = ScrollBars.Both;
            grid.Font = new Font("微软雅黑", GridFontSize);
            grid.DefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * scale);
            grid.RowsDefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * scale);
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(235, 243, 255);
            grid.RowTemplate.Height = (int)Math.Round(RowHeightBasis * scale);
            grid.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = new Font("微软雅黑", GridFontSize, FontStyle.Bold),
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = Color.FromArgb(0, 40, 170),
                ForeColor = Color.White
            };
            grid.EnableHeadersVisualStyles = false;
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = (int)Math.Round(ColumnHeaderHeightBasis * scale);
        }

        /// <summary>按列元数据建一个文本列：列头 = Header，绑定 = Name，时间列格式化加宽，非字符串列右对齐。</summary>
        public static DataGridViewTextBoxColumn BuildColumn(ProductColumn column, float scale)
        {
            var isTime = column.Type == typeof(DateTime);
            var col = new DataGridViewTextBoxColumn
            {
                Name = "col" + column.Name,
                HeaderText = column.Header,
                DataPropertyName = column.Name,
                Width = (int)Math.Round((isTime ? TimeColumnWidthBasis : ColumnWidthBasis) * scale),
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            if (isTime)
            {
                col.DefaultCellStyle.Format = "yyyy-MM-dd HH:mm:ss";
            }
            if (column.Type != typeof(string))
            {
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            }
            return col;
        }

        /// <summary>
        /// 行换源重绑。preserveCurrentCell = true 时同时保持滚动位置与当前选中行
        /// （实时页定时刷新用，别把人滚回顶部、也别把高亮跳走）；
        /// false 时只保滚动（查询页翻页后应落在新结果上）。
        /// 注：选中按行号保持，不按记录标识——新数据前插会让同号行换成别的记录，
        /// 页面零表知识故不解析主键；要「盯住某条记录」用查询页按条码查。
        /// </summary>
        public static void RebindPreservingScroll(DataGridView grid, IList rows, bool preserveCurrentCell = false)
        {
            int keepRow = preserveCurrentCell && grid.CurrentCell != null ? grid.CurrentCell.RowIndex : -1;
            int keepScroll = grid.FirstDisplayedScrollingRowIndex;

            grid.DataSource = rows;

            if (keepScroll > 0 && keepScroll < grid.Rows.Count)
            {
                grid.FirstDisplayedScrollingRowIndex = keepScroll;
            }

            if (keepRow >= 0 && keepRow < grid.Rows.Count && grid.Columns.Count > 0)
            {
                grid.CurrentCell = grid.Rows[keepRow].Cells[0];
            }
        }
    }
}
