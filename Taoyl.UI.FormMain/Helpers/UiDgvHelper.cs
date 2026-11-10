using System;
using System.Drawing;
using System.Windows.Forms;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// DataGridView DPI 与样式统一辅助（收敛多窗体重复样板）。
    /// 三件套：单元格字体物理放大（原生绘制不走 GlobalFontScale）、列头字体保持 12F（SunnyUI 走 GlobalFontScale）、
    /// 行高/列头高按 DPI 补偿；列头深蓝统一（0,40,170）；行号由 RowPostPaint 绘制。
    /// 字体由调用方创建并负责释放（窗体 OnFormClosing Dispose）。
    /// </summary>
    public static class UiDgvHelper
    {
        /// <summary>列头统一深蓝（与 FormProcessMonitor/FormRoleManager 等项目页面一致）</summary>
        public static readonly Color HeaderBackColor = Color.FromArgb(0, 40, 170);

        /// <summary>
        /// 应用 DPI 三件套 + 列头样式 + 行号绘制。
        /// </summary>
        /// <param name="dgv">目标网格（UIDataGridView/DataGridView 通用）</param>
        /// <param name="cellFont">单元格/数据行字体（物理放大，12F × scale）</param>
        /// <param name="headerFont">列头字体（SunnyUI 网格保持 12F；标准网格物理放大）</param>
        /// <param name="controlFont">dgv.Font（列头基准字体，SunnyUI 网格保持 12F）</param>
        /// <param name="showRowNumbers">是否显示行头行号列（默认 true；页面已有"序号"数据列时传 false 去重）</param>
        public static void Setup(DataGridView dgv, Font cellFont, Font headerFont, Font controlFont, bool showRowNumbers = true)
        {
            dgv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;
            dgv.RowTemplate.Height = UiDpi.Scale(44);
            dgv.Font = controlFont;
            dgv.DefaultCellStyle.Font = cellFont;
            dgv.RowsDefaultCellStyle.Font = cellFont;

            // 列头深蓝 + 白字 + 居中 + 禁用系统样式（EnableHeadersVisualStyles=false 才生效）
            dgv.ColumnHeadersDefaultCellStyle = new DataGridViewCellStyle
            {
                Font = headerFont,
                Alignment = DataGridViewContentAlignment.MiddleCenter,
                BackColor = HeaderBackColor,
                ForeColor = Color.White
            };
            dgv.EnableHeadersVisualStyles = false;

            // 列头高度：弃用 AutoSize（运行时改字体后不重算必切），按 DPI 手算
            dgv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            dgv.ColumnHeadersHeight = UiDpi.Scale(36);

            // 行号列（RowPostPaint 绘制行号）；不需要时隐藏行头且不挂绘制事件（防行号文本叠到首列）
            if (showRowNumbers)
            {
                dgv.RowHeadersWidth = UiDpi.Scale(50);
                dgv.RowHeadersVisible = true;
                dgv.RowPostPaint += OnRowPostPaint;
            }
            else
            {
                dgv.RowHeadersVisible = false;
                dgv.RowHeadersWidth = UiDpi.Scale(50);
            }
        }

        /// <summary>
        /// 尝试把焦点所在网格的「当前单元格」文本复制到剪贴板（取 FormattedValue ⇒ 所见即所复制，走 CellFormatting）。
        ///
        /// 用途：DataGridView 原生 Ctrl+C 的复制范围由 SelectionMode 决定（GetClipboardContent 内部 switch SelectionMode，
        /// FullRowSelect ⇒ 复制整行）。本方法供窗体在 ProcessCmdKey 覆写里调用，命中即复制当前单元格并返回 true，
        /// 调用方直接 return true 吞掉按键 —— 键消息在 PreTranslateMessage 阶段被判为已处理，不会再派发到 DGV 的
        /// ProcessKeyEventArgs / ProcessDataGridViewKey 复制路径。
        ///
        /// 返回 false（交还原生逻辑）的三种情形：
        /// 1. 非 Ctrl+C / Ctrl+Insert —— Ctrl+Shift+C 等组合一律放行，保留原生整行复制入口；
        /// 2. msg.HWnd 不匹配任一传入网格 —— 焦点在别的控件（如文本框），或单元格处于编辑态（焦点已转给内嵌编辑控件，
        ///    其 HWND 与网格不同），故编辑框内「选中文字再复制」不受影响，无需额外判编辑态；
        /// 3. 当前单元格无效（无 CurrentCell）。
        /// 注：键消息沿父链冒泡时 HWnd 不被改写，故可用它精确判定焦点归属。
        /// </summary>
        /// <param name="msg">ProcessCmdKey 的原始消息（只读 HWnd）</param>
        /// <param name="keyData">按键数据</param>
        /// <param name="grids">候选网格白名单（未接入的网格不要传，保持其原生复制行为；null 项自动跳过）</param>
        /// <returns>true = 已复制当前单元格文本，调用方应吞掉该按键</returns>
        public static bool TryCopyFocusedCell(ref Message msg, Keys keyData, params DataGridView[] grids)
        {
            // 只拦 Ctrl+C / Ctrl+Insert；其余组合（含 Ctrl+Shift+C）放行给原生逻辑
            if (keyData != (Keys.Control | Keys.C) && keyData != (Keys.Control | Keys.Insert))
                return false;
            if (grids == null)
                return false;

            // 焦点归属判定：非编辑态下焦点就在网格自身，msg.HWnd 即网格句柄
            DataGridView target = null;
            foreach (DataGridView dgv in grids)
            {
                if (dgv != null && dgv.IsHandleCreated && dgv.Handle == msg.HWnd)
                {
                    target = dgv;
                    break;
                }
            }
            if (target == null)
                return false;

            DataGridViewCell cell = target.CurrentCell;
            if (cell == null || cell.RowIndex < 0)
                return false;

            // 空单元格：吞键但不动剪贴板——既不退回原生整行复制，也不触发 SetText 的空串异常
            var text = cell.FormattedValue?.ToString();
            if (string.IsNullOrEmpty(text))
                return true;

            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                // 剪贴板被其它进程占用（OleSetClipboard 失败）等：静默降级但仍吞掉按键，
                // 避免退回原生整行复制、把与用户意图无关的内容塞进剪贴板
                System.Diagnostics.Debug.WriteLine($"复制单元格失败: {ex.Message}");
            }
            return true;
        }

        /// <summary>
        /// 绘制行号，异常时静默处理，绝不抛异常避免搞崩 DataGridView 渲染管线
        /// </summary>
        private static void OnRowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                var dgv = (DataGridView)sender;
                using (var brush = new SolidBrush(dgv.RowHeadersDefaultCellStyle.ForeColor))
                {
                    string lineNo = (e.RowIndex + 1).ToString();
                    e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                    using (var sf = new StringFormat
                    {
                        LineAlignment = StringAlignment.Center,
                        Alignment = StringAlignment.Center
                    })
                    {
                        e.Graphics.DrawString(lineNo, e.InheritedRowStyle.Font, brush,
                            new Rectangle(e.RowBounds.Location.X, e.RowBounds.Location.Y,
                                dgv.RowHeadersWidth, e.RowBounds.Height), sf);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"添加行号时发生错误: {ex.Message}");
            }
        }
    }
}
