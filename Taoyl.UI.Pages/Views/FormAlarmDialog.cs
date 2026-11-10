using Sunny.UI;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 报警弹窗（提示/警告/异常三通道共用同一骨架，瞬态窗体 + VM 单例常驻数据源，均非模态 Show 且置顶）：
    /// Presenter 追加进 VM 通道数据源 → Bind(AlarmType) 按通道策略应用配色/标题/自关规则 → Show。
    /// - 提示通道：空闲自关（清空不 Ack，错过看查询页），手动确认走同一 Ack 链留痕；
    /// - 警告通道：常驻待确认，一次确认批量 Ack + 关窗（预警防错过）；
    /// - 异常通道：常驻待确认（安全兜底在流程层联动暂停），确认链与警告通道同构。
    /// 配色与标题即通道严重度色阶：提示=黄 / 警告=橙 / 异常=红（差异收敛在 VM 通道策略字典）。
    /// </summary>
    public partial class FormAlarmDialog : UIForm
    {
        private readonly AlarmDialogViewModel viewModel;

        /// <summary>本窗体绑定的报警类型（Bind 时赋值，决定配色/数据源/确认作用域）</summary>
        private AlarmType channelType;

        /// <summary>自关通道空闲计时器（策略含空闲毫秒数的通道：最后一条追加后无新报警即收窗）</summary>
        private Timer idleCloseTimer;

        public FormAlarmDialog(AlarmDialogViewModel viewModel)
        {
            this.viewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);

            SetupForm();
            SetupDgv();
            dgvRecords.RowPostPaint += (s, e) => OnRowPostPaint(dgvRecords, e);
        }

        private void SetupForm()
        {
            // 窗体尺寸按 DPI 缩放
            float dpiScale = UiDpi.DpiScale;
            ClientSize = new Size((int)Math.Round(ClientSize.Width * dpiScale), (int)Math.Round(ClientSize.Height * dpiScale));
            ZoomScaleRect = new Rectangle(ZoomScaleRect.X, ZoomScaleRect.Y, ClientSize.Width, ClientSize.Height);
            TitleHeight=(int)Math.Round(TitleHeight * dpiScale);
        }

        /// <summary>
        /// 通道绑定（Presenter 显示前调用）：查 VM 通道策略应用配色/标题/置顶，绑数据源、接确认命令、订收窗事件；
        /// 自关通道（策略含空闲毫秒数）另建空闲计时器并订追加事件重置计时。通道差异全在策略字典，本方法零特判。
        /// </summary>
        public void Bind(AlarmType type)
        {
            channelType = type;
            AlarmChannelPolicy policy = viewModel.PolicyOf(type);

            TitleColor = policy.BackColor;
            BackColor = policy.BackColor;
            Text = policy.Title;
            TopMost = policy.TopMost;

            dgvRecords.DataSource = viewModel.RecordsOf(type);
            btnConfirm.Click += OnConfirmClick;
            viewModel.RequestClose += OnRequestClose;

            if (policy.IdleCloseMilliseconds.HasValue)
            {
                // 自关通道：空闲自关（追加即重置）；自关=清空不 Ack；退订在 OnFormClosed 统一收口
                idleCloseTimer = new Timer { Interval = policy.IdleCloseMilliseconds.Value };
                idleCloseTimer.Tick += OnIdleCloseTick;
                viewModel.RecordAppended += OnRecordAppended;
                idleCloseTimer.Start();
            }
        }

        /// <summary>确认按钮：同一命令按本窗报警类型参数化，只处理对应数据源。</summary>
        private void OnConfirmClick(object sender, EventArgs e)
        {
            viewModel.ConfirmCommand.Execute(channelType);
        }

        /// <summary>收窗请求（仅响应本类型）：统一 Close（非模态关闭后自动 Dispose）。</summary>
        private void OnRequestClose(AlarmType closedType)
        {
            if (closedType != channelType) return;

            Close();
        }

        /// <summary>自关窗展示中收到新追加：重置空闲计时（常驻显示，不弹新窗）。</summary>
        private void OnRecordAppended(AlarmType appendedType)
        {
            if (appendedType != channelType) return;

            idleCloseTimer.Stop();
            idleCloseTimer.Start();
        }

        /// <summary>空闲自关：清空未确认条目（清空不 Ack）后收窗。</summary>
        private void OnIdleCloseTick(object sender, EventArgs e)
        {
            viewModel.ClearChannel(channelType);
            Close();
        }

        /// <summary>窗体关闭：归还通道显示标记 + 退订 VM 事件（VM 单例，防滞留引用扣住已释放窗体）+ 释放计时器。</summary>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            viewModel.MarkShowing(channelType, false);
            viewModel.RequestClose -= OnRequestClose;
            viewModel.RecordAppended -= OnRecordAppended;

            if (idleCloseTimer != null)
            {
                idleCloseTimer.Stop();
                idleCloseTimer.Dispose();
                idleCloseTimer = null;
            }

            base.OnFormClosed(e);
        }

        #region 表格装配（照 FormAlarmQuery 基准：[Description] 列头 + 横排自适应列宽 + DPI 缩放）

        private float dgvDpiScale = 1F;
        private const int RowHeightBasis = 36;

        /// <summary>列头高基准：36 装不下加粗大字（18pt@150% 行高 ≈36px 正好卡满被切），加 8px 余量</summary>
        private const int ColumnHeaderHeightBasis = 44;

        private const float GridFontSize = 12F;

        private void SetupDgv()
        {
            dgvDpiScale = UiDpi.DpiScale;
            var grid = dgvRecords;

            grid.AutoGenerateColumns = false;
            grid.ReadOnly = true;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = false;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.AllowUserToResizeRows = false;

            // 列宽随内容自适应（AllCells）：内容过长的列自动加宽，整体超宽出横向滚动条
            grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
            grid.ScrollBars = ScrollBars.Both;

            // 行高按 DPI 缩放（FormAlarmQuery 基准：36px @100%）
            grid.RowTemplate.Height = (int)Math.Round(RowHeightBasis * dgvDpiScale);

            // 字体随 DPI 补偿（行高放大后字体不缩放会在高 DPI 下截断；微软雅黑对齐其它页面基准）
            grid.Font = new Font("微软雅黑", GridFontSize);
            grid.DefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale);
            grid.RowsDefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale);
            grid.ColumnHeadersDefaultCellStyle.Font = new Font("微软雅黑", GridFontSize * dgvDpiScale, FontStyle.Bold);

            grid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
            
            grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.EnableResizing;
            grid.ColumnHeadersHeight = (int)Math.Round(ColumnHeaderHeightBasis * dgvDpiScale);

            // 配色去蓝改白：斑马纹/行头/列头/网格线统一白底深字
            grid.StripeOddColor = Color.White;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.White;
            grid.RowHeadersDefaultCellStyle.BackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.BackColor = Color.White;
            grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(48, 48, 48);
            grid.GridColor = Color.FromArgb(208, 208, 208);

            // 选中态：白底黑字（系统默认蓝高亮在白底方案里太跳；斑马纹行未单设选中色，走此默认下传）
            grid.RowsDefaultCellStyle.SelectionBackColor = Color.White;
            grid.RowsDefaultCellStyle.SelectionForeColor = Color.FromArgb(48, 48, 48);
            grid.RowHeadersDefaultCellStyle.SelectionBackColor = Color.White;
            grid.RowHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(48, 48, 48);

            
            // 主键 / 处理人员 / 处理时间不展示
            AddTextColumn(grid, nameof(AlarmRecordDto.RaiseTime), "yyyy-MM-dd HH:mm:ss");
            AddTextColumn(grid, nameof(AlarmRecordDto.Type), null);
            AddTextColumn(grid, nameof(AlarmRecordDto.Code), null);
            AddTextColumn(grid, nameof(AlarmRecordDto.Source), null);
            AddTextColumn(grid, nameof(AlarmRecordDto.Message), null);
            AddTextColumn(grid, nameof(AlarmRecordDto.ActionGuide), null);
        }

        /// <summary>按模型 [Description] 生成绑定列（列头文案与查询页/导出同源）。</summary>
        private void AddTextColumn(DataGridView grid, string dataProperty, string format)
        {
            var prop = typeof(AlarmRecordDto).GetProperty(dataProperty);
            var desc = prop?.GetCustomAttributes(typeof(DescriptionAttribute), false)
                .Cast<DescriptionAttribute>().FirstOrDefault();

            var col = new DataGridViewTextBoxColumn
            {
                Name = "col" + dataProperty,
                HeaderText = desc?.Description ?? dataProperty,
                DataPropertyName = dataProperty,
                ReadOnly = true,
                SortMode = DataGridViewColumnSortMode.NotSortable
            };
            if (format != null) col.DefaultCellStyle.Format = format;
            grid.Columns.Add(col);
        }

        #endregion

        #region 行号绘制

        /// <summary>
        /// 绘制行号，异常时静默处理，绝不抛异常避免搞崩 DataGridView 渲染管线
        /// </summary>
        private void OnRowPostPaint(DataGridView dgv, DataGridViewRowPostPaintEventArgs e)
        {
            try
            {
                using var brush = new SolidBrush(dgv.RowHeadersDefaultCellStyle.ForeColor);
                string lineNo = (e.RowIndex + 1).ToString();

                e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

                using var sf = new StringFormat
                {
                    LineAlignment = StringAlignment.Center,
                    Alignment = StringAlignment.Center
                };

                e.Graphics.DrawString(lineNo, e.InheritedRowStyle.Font, brush,
                    new Rectangle(e.RowBounds.Location.X, e.RowBounds.Location.Y,
                        dgv.RowHeadersWidth, e.RowBounds.Height), sf);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"添加行号时发生错误: {ex.Message}");
            }
        }

        #endregion
    }
}
