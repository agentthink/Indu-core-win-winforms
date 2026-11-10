
using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 原生WinForms带可配置断续线条的容器面板
    /// 完全基于System.Windows.Forms，无第三方依赖
    /// </summary>
    [ToolboxItem(true)]
    [DesignerCategory("Code")]
    [DefaultProperty("LineColor")]
    [Description("原生WinForms带可配置断续线条的容器面板")]
    public partial class DashedLinePanel : Panel
    {
        private Pen _linePen;
        private bool _contentPanelCreated = false;

        // 私有字段
        private int _segmentHeight = 8;
        private int _segmentGap = 4;
        private int _lineWidth = 2;
        private int _lineMargin = 2;
        private int _topBottomMargin = 8;
        private Color _lineColor = Color.FromArgb(80, 160, 255);

        // 内容面板
        private Panel _contentPanel;

        /// <summary>
        /// 线段高度（像素）
        /// </summary>
        [Category("线条样式")]
        [DefaultValue(8)]
        [Description("每段线条的高度（像素）")]
        public int SegmentHeight
        {
            get => _segmentHeight;
            set
            {
                if (_segmentHeight != value && value > 0)
                {
                    _segmentHeight = value;
                    Invalidate(); // 立即重绘
                }
            }
        }

        /// <summary>
        /// 线段间距（像素）
        /// </summary>
        [Category("线条样式")]
        [DefaultValue(4)]
        [Description("线条段之间的间距（像素）")]
        public int SegmentGap
        {
            get => _segmentGap;
            set
            {
                if (_segmentGap != value && value >= 0)
                {
                    _segmentGap = value;
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 线条宽度（像素）
        /// </summary>
        [Category("线条样式")]
        [DefaultValue(2)]
        [Description("线条的粗细（像素），范围1-10")]
        public int LineWidth
        {
            get => _lineWidth;
            set
            {
                if (_lineWidth != value && value >= 1 && value <= 10)
                {
                    _lineWidth = value;
                    RecreatePen();
                    UpdateContentPanelPadding();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 线条颜色
        /// </summary>
        [Category("线条样式")]
        [DefaultValue(typeof(Color), "80, 160, 255")]
        [Description("线条颜色")]
        public Color LineColor
        {
            get => _lineColor;
            set
            {
                if (_lineColor != value)
                {
                    _lineColor = value;
                    RecreatePen();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 线条距离面板边缘的边距
        /// </summary>
        [Category("布局")]
        [DefaultValue(2)]
        [Description("线条距离面板左右边缘的边距（像素）")]
        public int LineMargin
        {
            get => _lineMargin;
            set
            {
                if (_lineMargin != value && value >= 0)
                {
                    _lineMargin = value;
                    UpdateContentPanelPadding();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 上下起始/结束边距
        /// </summary>
        [Category("布局")]
        [DefaultValue(8)]
        [Description("线条上下起始和结束的边距（像素）")]
        public int TopBottomMargin
        {
            get => _topBottomMargin;
            set
            {
                if (_topBottomMargin != value && value >= 0)
                {
                    _topBottomMargin = value;
                    UpdateContentPanelPadding();
                    Invalidate();
                }
            }
        }

        /// <summary>
        /// 内容面板（可在其中放置其他控件）
        /// </summary>
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        public Panel ContentPanel
        {
            get
            {
                EnsureContentPanelCreated();
                return _contentPanel;
            }
        }

        public DashedLinePanel()
        {
            // 初始化画笔
            RecreatePen();

            // 启用双缓冲，防止闪烁
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.UserPaint |
                     ControlStyles.ResizeRedraw, true);

            // 默认边框样式
            BorderStyle = BorderStyle.None;
            BackColor = SystemColors.Window;

            // 初始化Padding
            UpdateContentPanelPadding();

            // 注意：这里不立即创建ContentPanel，在需要时创建
            // 这样确保在Designer中不会出问题
        }

        /// <summary>
        /// 添加自动调整Padding的方法
        /// </summary>
        private void UpdateContentPanelPadding()
        {
            if (_contentPanel != null && !_contentPanel.IsDisposed)
            {
                // 计算合适的Padding，确保内容不遮挡断续线
                int horizontalPadding = Math.Max(LineMargin + LineWidth + 5, 15);
                int verticalPadding = Math.Max(TopBottomMargin / 2, 5);

                _contentPanel.Padding = new Padding(
                    horizontalPadding,
                    verticalPadding,
                    horizontalPadding,
                    verticalPadding
                );
            }
        }

        /// <summary>
        /// 确保ContentPanel已创建
        /// </summary>
        private void EnsureContentPanelCreated()
        {
            if (!_contentPanelCreated || _contentPanel == null || _contentPanel.IsDisposed)
            {
                InitializeContentPanel();
                _contentPanelCreated = true;
            }
        }

        private void InitializeContentPanel()
        {
            _contentPanel = new Panel
            {
                Name = "ContentPanel",
                Dock = DockStyle.Fill,
                BackColor = Color.Transparent,
                BorderStyle = BorderStyle.None,
                Padding = new Padding(10, 10, 10, 10), // 默认间距
                Margin = new Padding(0)
            };

            Controls.Add(_contentPanel);
            _contentPanel.BringToFront();
        }

        // 重写OnHandleCreated，确保窗体句柄创建后才初始化ContentPanel
        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);

            if (!DesignMode)
            {
                EnsureContentPanelCreated();
            }
        }

        private void RecreatePen()
        {
            _linePen?.Dispose();
            _linePen = new Pen(_lineColor, _lineWidth)
            {
                Alignment = PenAlignment.Center,
                LineJoin = LineJoin.Round
            };
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;

            // 计算线条的X坐标（考虑边距）
            int leftX = LineMargin;
            int rightX = Width - LineMargin - 1;

            // 计算Y轴绘制区域（考虑上下边距）
            int startY = TopBottomMargin;
            int endY = Height - TopBottomMargin;

            // 绘制左侧断续线
            DrawDashedLine(g, leftX, startY, leftX, endY);

            // 绘制右侧断续线
            DrawDashedLine(g, rightX, startY, rightX, endY);
        }

        private void DrawDashedLine(Graphics g, int x, int y1, int x2, int y2)
        {
            int currentY = y1;

            while (currentY < y2)
            {
                // 计算当前线段的终点
                int segmentEnd = Math.Min(currentY + SegmentHeight, y2);

                // 绘制线段
                g.DrawLine(_linePen, x, currentY, x2, segmentEnd);

                // 移动到下一个线段的起始位置
                currentY += SegmentHeight + SegmentGap;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _linePen?.Dispose();
                _contentPanel?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}