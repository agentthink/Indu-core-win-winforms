using Sunny.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;

namespace Taoyl.UI.Pages.Controls
{
    /// <summary>
    /// 分页控件（SunnyUI 外观，形态与折叠算法对标 SunnyUI UIPagination）。
    ///
    /// **为什么不直接用 UIPagination**：它的公开面只到 PageCount / PageSize / ActivePage / PagerCount 这一层，
    /// 没有"每页条数"的入口，也没法定制跳转区排布——而审计页需要"每页条数下拉 + 分页条"同排
    /// （服务端分页，页大小一变必须回第 1 页重查）；此外它的 17 个按钮全是运行时 new + 绝对定位，
    /// **设计期完全看不到效果**，改一次样式就得 F5 一次。
    ///
    /// 三条设计约束（改本控件前先读完）：
    ///
    /// 1. **一个控件 = 一列**。<see cref="TableLayoutPanel"/> 列数固定（15 个内容列 + 1 个末尾吸收列），
    ///    子控件一律 <c>Dock = Fill</c>；页面槽隐藏时把**该列列宽置 0**（列还在，列数不变）。
    ///    🔴 行样式必须**显式写** Percent 100：只设列不设行时，该行会退化成 AutoSize
    ///    （框架实证），高 DPI 下文字底部被裁。
    ///
    /// 2. **控件内零 DPI 逻辑**。尺寸 / 配色 / 字体全部走公开属性，默认值是 100% 设计基准；
    ///    "要不要放大、放大到几倍"由**宿主**决定（逐属性赋 <c>UiDpi.Scale(...)</c>）。
    ///    故本控件不引用 UiDpi，也不假设自己被放在哪种宿主里——换宿主 = 改宿主那几行，控件不动。
    ///    ⚠️ 但字号属性是**逃生口**：DPI 铁律下字号不放大（SunnyUI 按 GlobalFontScale 绘制文字，
    ///    再物理放大会双重放大），宿主默认不需要碰它们。
    ///
    /// 3. **高度由宿主决定**。分页条内所有控件 Dock=Fill，吃的是宿主表格行高——
    ///    控件自身不给高度，也就不会和"DGV 行高按 DPI 补偿"那条规则打架。
    ///
    /// 折叠算法照抄 UIPagination.SetShowButtons（参数固定 7）：
    /// 总页数 ≤ 9 → 全显不折叠；靠左 → 前段 + <c>&gt;&gt;</c> + 末页；
    /// 靠右 → 首页 + <c>&lt;&lt;</c> + 末段；居中 → 首页 + <c>&lt;&lt;</c> + 中间 5 个 + <c>&gt;&gt;</c> + 末页。
    /// 居中态正好占 1+1+5+1+1 = 9 个槽，<see cref="PageSlotCount"/> 即由此而来
    /// （原 PagerCount 公开属性已随槽位固定而删除：它只能在 5..9 里晃，是伪配置）。
    ///
    /// 宿主接线样板（审计页 FormAudit.ApplyPaginationScale）：
    /// <code>
    /// pagination.PrevNextButtonWidth = UiDpi.Scale(75);
    /// pagination.PageButtonWidth     = UiDpi.Scale(29);
    /// pagination.PageButtonWidthMedium = UiDpi.Scale(36);
    /// pagination.PageButtonWidthLong   = UiDpi.Scale(52);
    /// pagination.EllipsisWidth       = UiDpi.Scale(29);
    /// pagination.JumpLabelWidth      = UiDpi.Scale(21);
    /// pagination.JumpBoxWidth        = UiDpi.Scale(53);
    /// pagination.JumpButtonWidth     = UiDpi.Scale(61);
    /// </code>
    /// </summary>
    public partial class Pagination : UserControl
    {
        /// <summary>
        /// 页码槽数量（与 Designer 预置的 9 个 UISymbolButton、以及 TLP 的 9 个页码列一一对应）。
        /// 🔴 调整本值必须同步改三处：Designer 的按钮池与 ColumnStyles、<see cref="CollectPageButtons"/>、
        /// <see cref="ColumnPageSlotStart"/> 之后的列索引常量。
        /// </summary>
        private const int PageSlotCount = 9;

        /// <summary>
        /// 页码折叠参数（照抄 UIPagination 默认值 7）。
        /// 不再是公开属性：槽位固定为 9 之后，它只能在 5..9 里取奇数，是伪配置。
        /// </summary>
        private const int PagerCount = 7;

        /// <summary>省略号按钮的 Symbol（FontAwesome ellipsis-h，与 UIPagination 同值，保证图标一致）</summary>
        private const int EllipsisSymbol = 361761;

        // ── TLP 列索引（与 Designer 的 ColumnStyles 顺序严格对应）──
        //    ⚠️ 改 Designer 的列顺序 / 增删列，必须同步本区，否则列宽会改到错误的列上。

        /// <summary>「上一页」列索引</summary>
        private const int ColumnPrev = 0;

        /// <summary>「下一页」列索引</summary>
        private const int ColumnNext = 1;

        /// <summary>页码槽起始列索引（槽 0 → 本列，槽 8 → 本列 + 8）</summary>
        private const int ColumnPageSlotStart = 2;

        /// <summary>跳转区「第」列索引</summary>
        private const int ColumnJumpLabelLeft = 11;

        /// <summary>跳转输入框列索引</summary>
        private const int ColumnJumpBox = 12;

        /// <summary>跳转区「页」列索引</summary>
        private const int ColumnJumpLabelRight = 13;

        /// <summary>跳转「确定」按钮列索引</summary>
        private const int ColumnJumpButton = 14;

        // ── 列间距（设计基准，**不随 DPI 缩放**）──
        //    理由：这是像素级留白，缩放与否不影响可读性；而公开属性的语义只覆盖"控件本体宽度"，
        //    把间距混进属性会让"宽度 29"到底含不含间隔变得说不清。
        //    实际布局 = 列宽（属性值 + 前置间距），间隔靠控件自身的 Margin.Left 在单元格内让出。

        /// <summary>「下一页」与前一列之间的间距</summary>
        private const int NavGap = 3;

        /// <summary>页码槽之间的间距（无之间距会连成一整条，看不出分页）</summary>
        private const int SlotGap = 2;

        /// <summary>跳转区与页码区之间的间距（跳转区在视觉上要独立成组）</summary>
        private const int JumpGap = 9;

        /// <summary>跳转区内部（「第」/ 输入框 /「页」/「确定」）之间的间距</summary>
        private const int JumpItemGap = 3;

        /// <summary>
        /// 页码按钮池（索引 0 → btnPage1，索引 8 → btnPage9）。
        /// 由 <see cref="CollectPageButtons"/> 从 Designer 字段收集，运行期只读。
        /// </summary>
        private List<UISymbolButton> pageButtons;

        /// <summary>
        /// 每个槽当前的"渲染意图"（渲染期写入、点击期读取）。
        /// 为什么不把意图塞进按钮的 Tag：Tag 是 object、类型不安全，而按钮自己还要承载 TagString（翻译标识）；
        /// 单独一张意图表读起来就是"这个槽现在代表什么"，与外观设置彻底解耦。
        /// 实例预建 9 个长期复用，渲染只改字段、不 new，翻页不产生垃圾。
        /// </summary>
        private PageSlotIntent[] slotIntents;

        private int totalCount;
        private int pageSize = 20;
        private int activePage = 1;
        private int pageCount = 1;

        // ── 外观属性字段（初值 = 100% 设计基准，与 Designer 里的字面量一致）──

        private int prevNextButtonWidth = 75;
        private int pageButtonWidth = 29;
        private int pageButtonWidthMedium = 36;
        private int pageButtonWidthLong = 52;
        private int ellipsisWidth = 29;
        private int jumpLabelWidth = 21;
        private int jumpBoxWidth = 53;
        private int jumpButtonWidth = 61;

        private Color selectedBackColor = Color.White;
        private Color selectedForeColor = Color.FromArgb(80, 160, 255);
        private Color selectedBorderColor = Color.FromArgb(80, 160, 255);

        private Font pageButtonFont;
        private Font navButtonFont;

        public Pagination()
        {
            InitializeComponent();

            // 字体默认值直接取 Designer 的值 —— 属性面只做转发，不另立一套默认（否则两处真相）
            pageButtonFont = btnPage1.Font;
            navButtonFont = btnPrev.Font;

            CollectPageButtons();
            BindButtonEvents();
            ApplyColors();
            ApplyFonts();

            Render();
        }

        /// <summary>
        /// 页码变化（点击页码 / 上一页 / 下一页 / 省略号 / 跳转确定，**以及程序赋 <see cref="ActivePage"/>**）。
        ///
        /// 🔴 **与 SunnyUI UIPagination 同语义：程序赋值同样触发**（UIPagination 的 ActivePage setter 内部调
        /// DataBind → PageChanged）。所以调用方若用"VM → 控件"的单向同步，**必须在处理器里做幂等判断**
        /// （典型写法：`if (ViewModel.PageIndex == pagination.ActivePage) return;`），否则程序性同步会引发回环查询。
        ///
        /// 与 UIPagination 的差异：本事件**只在页码真的变了时**才触发（UIPagination 无值也触发）。
        /// 这不影响幂等写法，但省掉了"同步 TotalCount / PageSize 时白触发两次"的噪音。
        /// </summary>
        public event EventHandler PageChanged;

        #region 公开属性 — 分页状态

        /// <summary>总条目数（真相源在 VM；变化会重算总页数并重排页码槽）</summary>
        [DefaultValue(0)]
        [Description("总条目数")]
        [Category("Taoyl")]
        public int TotalCount
        {
            get { return totalCount; }
            set
            {
                int count = value < 0 ? 0 : value;
                if (count == totalCount) return;

                int previousPage = activePage;
                totalCount = count;
                RenderAndNotifyIfClamped(previousPage);
            }
        }

        /// <summary>每页条目个数（默认 20，与 UIPagination 一致）</summary>
        [DefaultValue(20)]
        [Description("每页条目个数")]
        [Category("Taoyl")]
        public int PageSize
        {
            get { return pageSize; }
            set
            {
                int size = value < 1 ? 1 : value;
                if (size == pageSize) return;

                int previousPage = activePage;
                pageSize = size;
                RenderAndNotifyIfClamped(previousPage);
            }
        }

        /// <summary>
        /// 当前页码（1 起）。赋值会被夹到 [1, 总页数]；**夹取后同样触发 <see cref="PageChanged"/>**，
        /// 调用方据此把 VM 里作为查询真相源的页码回正。
        /// </summary>
        [DefaultValue(1)]
        [Description("当前页码")]
        [Category("Taoyl")]
        public int ActivePage
        {
            get { return activePage; }
            set
            {
                int previousPage = activePage;
                activePage = ClampPage(value);
                RenderAndNotifyIfClamped(previousPage);
            }
        }

        /// <summary>总页数（只读，由 <see cref="TotalCount"/> / <see cref="PageSize"/> 算出；总条目为 0 时也是 1）</summary>
        [Browsable(false)]
        public int PageCount
        {
            get { return pageCount; }
        }

        #endregion

        #region 公开属性 — 外观（默认值 = 100% 设计基准，缩放由宿主决定）

        /// <summary>上一页 / 下一页按钮宽度（含 Symbol）</summary>
        [DefaultValue(75)]
        [Description("上一页 / 下一页按钮宽度（100% 设计基准；缩放由宿主负责）")]
        [Category("Taoyl")]
        public int PrevNextButtonWidth
        {
            get { return prevNextButtonWidth; }
            set
            {
                if (value < 1 || value == prevNextButtonWidth) return;
                prevNextButtonWidth = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>页码按钮宽度：1-2 位数字</summary>
        [DefaultValue(29)]
        [Description("页码按钮宽度：1-2 位数字（100% 设计基准）")]
        [Category("Taoyl")]
        public int PageButtonWidth
        {
            get { return pageButtonWidth; }
            set
            {
                if (value < 1 || value == pageButtonWidth) return;
                pageButtonWidth = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>页码按钮宽度：3 位数字</summary>
        [DefaultValue(36)]
        [Description("页码按钮宽度：3 位数字（100% 设计基准）")]
        [Category("Taoyl")]
        public int PageButtonWidthMedium
        {
            get { return pageButtonWidthMedium; }
            set
            {
                if (value < 1 || value == pageButtonWidthMedium) return;
                pageButtonWidthMedium = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>
        /// 页码按钮宽度：4 位及以上。
        /// 照抄 UIPagination.SetButtonWidth 的**实际生效值**：其源码里 `>=4` 连续两行同条件赋值（44 与 52），
        /// 以最后一行为准 ⇒ 真实行为就是 52。此处写下的是"跑起来真正是多少"，不是"看起来想写多少"。
        /// </summary>
        [DefaultValue(52)]
        [Description("页码按钮宽度：4 位及以上（100% 设计基准）")]
        [Category("Taoyl")]
        public int PageButtonWidthLong
        {
            get { return pageButtonWidthLong; }
            set
            {
                if (value < 1 || value == pageButtonWidthLong) return;
                pageButtonWidthLong = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>省略号按钮宽度（与一个页码按钮同宽，UIPagination 亦然）</summary>
        [DefaultValue(29)]
        [Description("省略号按钮宽度（100% 设计基准）")]
        [Category("Taoyl")]
        public int EllipsisWidth
        {
            get { return ellipsisWidth; }
            set
            {
                if (value < 1 || value == ellipsisWidth) return;
                ellipsisWidth = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>跳转区单字标签（"第" / "页"）宽度（关掉 AutoSize 后需自带宽度）</summary>
        [DefaultValue(21)]
        [Description("跳转区「第」/「页」标签宽度（100% 设计基准）")]
        [Category("Taoyl")]
        public int JumpLabelWidth
        {
            get { return jumpLabelWidth; }
            set
            {
                if (value < 1 || value == jumpLabelWidth) return;
                jumpLabelWidth = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>跳转输入框宽度</summary>
        [DefaultValue(53)]
        [Description("跳转输入框宽度（100% 设计基准）")]
        [Category("Taoyl")]
        public int JumpBoxWidth
        {
            get { return jumpBoxWidth; }
            set
            {
                if (value < 1 || value == jumpBoxWidth) return;
                jumpBoxWidth = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>跳转「确定」按钮宽度</summary>
        [DefaultValue(61)]
        [Description("跳转「确定」按钮宽度（100% 设计基准）")]
        [Category("Taoyl")]
        public int JumpButtonWidth
        {
            get { return jumpButtonWidth; }
            set
            {
                if (value < 1 || value == jumpButtonWidth) return;
                jumpButtonWidth = value;
                ApplyColumnWidths();
            }
        }

        /// <summary>
        /// 当前页按钮的填充色（默认白底）。
        /// 为什么默认值写在代码里、而配色不硬编码进 Designer：SunnyUI 的按钮底色来自主题（UIStyles），
        /// 把颜色序列化进 Designer 会在换主题时错色（FillSelectedColor 仍停在旧主题的蓝）。
        /// </summary>
        [Description("当前页按钮的填充色（默认白底）")]
        [Category("Taoyl")]
        public Color SelectedBackColor
        {
            get { return selectedBackColor; }
            set
            {
                if (value == selectedBackColor) return;
                selectedBackColor = value;
                ApplyColors();
            }
        }

        /// <summary>当前页按钮的前景色（默认取本仓主题主色 80,160,255）</summary>
        [Description("当前页按钮的前景色")]
        [Category("Taoyl")]
        public Color SelectedForeColor
        {
            get { return selectedForeColor; }
            set
            {
                if (value == selectedForeColor) return;
                selectedForeColor = value;
                ApplyColors();
            }
        }

        /// <summary>当前页按钮的边框色</summary>
        [Description("当前页按钮的边框色")]
        [Category("Taoyl")]
        public Color SelectedBorderColor
        {
            get { return selectedBorderColor; }
            set
            {
                if (value == selectedBorderColor) return;
                selectedBorderColor = value;
                ApplyColors();
            }
        }

        /// <summary>
        /// 页码按钮字体。**逃生口**：DPI 铁律下字号不放大（SunnyUI 按 GlobalFontScale 绘制文字），
        /// 宿主默认不需要设置它；默认值即 Designer 里的微软雅黑 12F。
        /// </summary>
        [Description("页码按钮字体（默认微软雅黑 12F；DPI 场景下不建议物理放大）")]
        [Category("Taoyl")]
        public Font PageButtonFont
        {
            get { return pageButtonFont; }
            set
            {
                if (value == null || ReferenceEquals(value, pageButtonFont)) return;
                pageButtonFont = value;
                ApplyFonts();
            }
        }

        /// <summary>
        /// 上一页 / 下一页 / 确定按钮、以及跳转区两个单字标签的字体。
        /// 默认值即 Designer 里的微软雅黑 10.5F（与 UIPagination 同值）。
        /// </summary>
        [Description("上一页 / 下一页 / 确定 / 跳转标签字体（默认微软雅黑 10.5F）")]
        [Category("Taoyl")]
        public Font NavButtonFont
        {
            get { return navButtonFont; }
            set
            {
                if (value == null || ReferenceEquals(value, navButtonFont)) return;
                navButtonFont = value;
                ApplyFonts();
            }
        }

        #endregion

        #region 初始化

        /// <summary>
        /// 收集 Designer 预置的 9 个页码按钮 + 预建意图表。
        /// 顺序即槽序：btnPage1 → 槽 0 … btnPage9 → 槽 8。
        /// </summary>
        private void CollectPageButtons()
        {
            pageButtons = new List<UISymbolButton>
            {
                btnPage1, btnPage2, btnPage3, btnPage4, btnPage5,
                btnPage6, btnPage7, btnPage8, btnPage9
            };

            if (pageButtons.Count != PageSlotCount)
                throw new InvalidOperationException(
                    "页码按钮池数量（" + pageButtons.Count + "）与 PageSlotCount（" + PageSlotCount
                    + "）不一致——改槽位数时必须同步 Designer 按钮池与本方法。");

            slotIntents = new PageSlotIntent[PageSlotCount];
            for (int i = 0; i < PageSlotCount; i++) slotIntents[i] = new PageSlotIntent();
        }

        private void BindButtonEvents()
        {
            btnPrev.Click += OnPrevClick;
            btnNext.Click += OnNextClick;
            btnJump.Click += OnJumpClick;

            for (int i = 0; i < pageButtons.Count; i++)
            {
                // 闭包必须捕获副本：直接在 lambda 里用 i 会全部指向最后一个槽
                int slot = i;
                pageButtons[i].Click += (s, e) => OnSlotClick(slot);
            }
        }

        /// <summary>把配色属性写到每个页码按钮上（选中态配色，未选中态由 SunnyUI 主题负责）。</summary>
        private void ApplyColors()
        {
            if (pageButtons == null) return;

            foreach (var button in pageButtons)
            {
                button.FillSelectedColor = selectedBackColor;
                button.ForeSelectedColor = selectedForeColor;
                button.RectSelectedColor = selectedBorderColor;
            }
        }

        /// <summary>把字体属性写到对应控件上（页码按钮池 + 上下页 / 确定 / 跳转标签）。</summary>
        private void ApplyFonts()
        {
            if (pageButtons != null)
            {
                foreach (var button in pageButtons)
                {
                    if (pageButtonFont != null) button.Font = pageButtonFont;
                }
            }

            if (navButtonFont == null) return;

            btnPrev.Font = navButtonFont;
            btnNext.Font = navButtonFont;
            btnJump.Font = navButtonFont;
            lblJumpLeft.Font = navButtonFont;
            lblJumpRight.Font = navButtonFont;
        }

        #endregion

        #region 布局（列宽）

        /// <summary>
        /// 把"尺寸属性 + 当前渲染意图"落到 TLP 的列宽上。**本控件唯一的布局出口**：
        /// 尺寸属性变化、渲染重排后都走这里，不再有第二处改尺寸的地方。
        ///
        /// 列宽 = 该列内容宽 + 前置间距；控件的 Margin.Left 让出这一段，故控件本体宽 = 属性值。
        /// 隐藏的页码槽把列宽置 0（列仍在、列数不变）——这是"槽位固定 + 内容自适应"的实现方式。
        /// </summary>
        private void ApplyColumnWidths()
        {
            if (panelBar == null || pageButtons == null) return;

            SetColumnWidth(ColumnPrev, prevNextButtonWidth);
            SetColumnWidth(ColumnNext, NavGap + prevNextButtonWidth);

            for (int i = 0; i < PageSlotCount; i++)
            {
                int width = 0;

                // 不可见的槽整列宽度归零（可见性由 Render/HideSlotsFrom 决定，这里只读不写）
                if (pageButtons[i].Visible)
                {
                    var intent = slotIntents[i];
                    width = SlotGap + (intent.IsEllipsis ? ellipsisWidth : PageButtonWidthOf(intent.Page));
                }

                SetColumnWidth(ColumnPageSlotStart + i, width);
            }

            SetColumnWidth(ColumnJumpLabelLeft, JumpGap + jumpLabelWidth);
            SetColumnWidth(ColumnJumpBox, JumpItemGap + jumpBoxWidth);
            SetColumnWidth(ColumnJumpLabelRight, JumpItemGap + jumpLabelWidth);
            SetColumnWidth(ColumnJumpButton, JumpItemGap + jumpButtonWidth);
        }

        /// <summary>写一列的绝对列宽（值未变时不写，避免无谓的 TLP 重排）。</summary>
        private void SetColumnWidth(int index, int width)
        {
            var style = panelBar.ColumnStyles[index];
            if (style.SizeType != SizeType.Absolute) style.SizeType = SizeType.Absolute;
            if (style.Width != width) style.Width = width;
        }

        /// <summary>页码按钮宽度：按文案长度分档（分档值照抄 UIPagination.SetButtonWidth）。</summary>
        private int PageButtonWidthOf(int page)
        {
            int digits = page.ToString(CultureInfo.InvariantCulture).Length;
            if (digits >= 4) return pageButtonWidthLong;
            if (digits >= 3) return pageButtonWidthMedium;
            return pageButtonWidth;
        }

        #endregion

        #region 渲染

        /// <summary>
        /// 重排页码槽（照抄 UIPagination.SetShowButtons 的四分支）。
        ///
        /// 本方法**只改外观与意图表，不触发 <see cref="PageChanged"/>**——
        /// 它被三个 setter 共用，若在此发事件，"总数变了"会被下游误读成"用户翻页"从而白查一次。
        /// 需要通知的场景由 <see cref="RenderAndNotifyIfClamped"/> 显式处理。
        /// </summary>
        private void Render()
        {
            pageCount = ComputePageCount();

            // 总数变少 / 页大小变大都可能把当前页顶出界：先夹回来，后面的分支才能按正确的当前页去折叠
            if (activePage > pageCount) activePage = pageCount;
            if (activePage < 1) activePage = 1;

            btnPrev.Enabled = activePage > 1;
            btnNext.Enabled = activePage < pageCount;

            if (totalCount == 0)
            {
                // 照抄 UIPagination：空数据也显示一个"1"，避免整条分页条看起来像坏的（此时按钮不可点）
                ShowPage(0, 1);
                HideSlotsFrom(1);
            }
            else if (pageCount <= PagerCount + 2)
            {
                for (int page = 1; page <= pageCount; page++) ShowPage(page - 1, page);
                HideSlotsFrom(pageCount);
            }
            else
            {
                // 槽 0..leftShow-1 = 前段页码；槽 leftShow = >>；槽 leftShow+1 = 末页
                int leftShow = PagerCount / 2 + 3;
                if (activePage <= leftShow)
                {
                    for (int page = 1; page <= leftShow; page++) ShowPage(page - 1, page);
                    ShowEllipsis(leftShow, PagerCount - 2, ">>");
                    ShowPage(leftShow + 1, pageCount);
                    HideSlotsFrom(leftShow + 2);
                }
                else
                {
                    int rightShow = pageCount - (PagerCount / 2 + 1) - 1;
                    if (activePage >= rightShow)
                    {
                        // 槽 0 = 首页；槽 1 = <<；槽 2.. = 末段页码
                        ShowPage(0, 1);
                        ShowEllipsis(1, 2 - PagerCount, "<<");

                        int slot = 2;
                        for (int page = rightShow; page <= pageCount; page++) ShowPage(slot++, page);
                        HideSlotsFrom(slot);
                    }
                    else
                    {
                        // 槽 0 = 首页；槽 1 = <<；中间 PagerCount-2 个以当前页为中心；末尾 = >> + 末页
                        ShowPage(0, 1);
                        ShowEllipsis(1, 2 - PagerCount, "<<");

                        int middle = PagerCount - 2;
                        int offset = middle / 2;
                        int slot = 2;
                        for (int i = 0; i < middle; i++) ShowPage(slot++, activePage - offset + i);

                        ShowEllipsis(slot++, PagerCount - 2, ">>");
                        ShowPage(slot++, pageCount);
                        HideSlotsFrom(slot);
                    }
                }
            }

            ApplyColumnWidths();

            txtJump.IntValue = activePage;
            txtJump.Maximum = pageCount;
        }

        /// <summary>
        /// 渲染一个页码槽：写意图 → 设外观 → 置可见。
        ///
        /// 宽度**不在这里设**：槽宽由 <see cref="ApplyColumnWidths"/> 按列宽统一决定，按钮自身 Dock=Fill。
        /// 一处尺寸出口才不会有"两个地方都能改宽"这种暗雷。
        /// </summary>
        private void ShowPage(int slot, int page)
        {
            var button = pageButtons[slot];
            var intent = slotIntents[slot];

            intent.IsEllipsis = false;
            intent.Page = page;
            intent.Delta = 0;

            string text = page.ToString(CultureInfo.InvariantCulture);
            if (button.Text != text) button.Text = text;

            button.Symbol = 0;
            button.TagString = string.Empty;
            button.Selected = page == activePage;
            button.Visible = true;
        }

        /// <summary>渲染一个省略号槽（点击按 <paramref name="delta"/> 相对跳转）。</summary>
        private void ShowEllipsis(int slot, int delta, string text)
        {
            var button = pageButtons[slot];
            var intent = slotIntents[slot];

            intent.IsEllipsis = true;
            intent.Page = 0;
            intent.Delta = delta;

            button.Symbol = EllipsisSymbol;
            button.SymbolOffset = new Point(-1, 1);
            button.Text = string.Empty;
            button.TagString = text;
            button.Selected = false;
            button.Visible = true;
        }

        /// <summary>自 <paramref name="startSlot"/> 起隐藏（含）——留在池里但不可见，下次渲染可原地复活。</summary>
        private void HideSlotsFrom(int startSlot)
        {
            for (int i = startSlot; i < PageSlotCount; i++) pageButtons[i].Visible = false;
        }

        /// <summary>
        /// 重排后若当前页被夹取（总数 / 页大小变化把当前页顶出界），把结果通知给调用方。
        ///
        /// 为什么必须通知：页码的**真相源在 VM**（它是查询参数），控件单方面夹取不同步回去，
        /// 就会出现"界面显示第 3 页、实际查的是第 7 页（空）"这种看起来像丢数据的错位。
        /// </summary>
        private void RenderAndNotifyIfClamped(int previousPage)
        {
            Render();
            if (activePage != previousPage) PageChanged?.Invoke(this, EventArgs.Empty);
        }

        #endregion

        #region 交互

        private void OnPrevClick(object sender, EventArgs e)
        {
            ActivePage = activePage - 1;
        }

        private void OnNextClick(object sender, EventArgs e)
        {
            ActivePage = activePage + 1;
        }

        /// <summary>
        /// 页码槽点击：页码槽直达目标页；省略号槽按意图表里的相对量跳（±(PagerCount-2) 页，与 UIPagination 同步长）。
        /// </summary>
        private void OnSlotClick(int slot)
        {
            var intent = slotIntents[slot];

            if (intent.IsEllipsis)
            {
                if (intent.Delta != 0) ActivePage = activePage + intent.Delta;
                return;
            }

            if (intent.Page > 0) ActivePage = intent.Page;
        }

        private void OnJumpClick(object sender, EventArgs e)
        {
            // IntValue 已由 UITextBox 按 Minimum/Maximum 夹过；ActivePage 再夹一次兜底（越界赋值不发事件）
            ActivePage = txtJump.IntValue;
        }

        #endregion

        #region 私有辅助

        /// <summary>总页数（总条目为 0 时按 1 页——与 UIPagination 同规则，UI 上总得有个"第 1 页"可显示）。</summary>
        private int ComputePageCount()
        {
            if (totalCount <= 0) return 1;
            return (totalCount + pageSize - 1) / pageSize;
        }

        private int ClampPage(int value)
        {
            if (value < 1) return 1;
            return value > pageCount ? pageCount : value;
        }

        #endregion

        /// <summary>
        /// 一个页码槽的渲染意图（渲染期写入、点击期读取）。
        /// </summary>
        private sealed class PageSlotIntent
        {
            /// <summary>true = 本槽是省略号（点击按 <see cref="Delta"/> 相对跳转）</summary>
            public bool IsEllipsis;

            /// <summary>页码槽的目标页（IsEllipsis = false 时有效；0 = 空槽）</summary>
            public int Page;

            /// <summary>省略号槽的相对跳转量（照抄 UIPagination：往后 +（PagerCount-2），往前 -(PagerCount-2)）</summary>
            public int Delta;
        }
    }
}
