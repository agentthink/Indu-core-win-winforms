using Sunny.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// 字典属性区段控件：把一个 IDictionary 属性（如 PlcConfig.ProtocolParams）渲染为
    /// 「组头 + 添加参数栏 + 键值行」的自持区段，占渲染根的一整行（跨列，外层行为 Absolute）。
    ///
    /// 核心设计（对比旧渲染器的"就地插删 + 行号平移"实现）：
    /// - 数据源 = 字典本体，键值行不缓存副本 → 增删直接改字典后整段 Rebuild；
    /// - 无行号维护：不维护"UI 行序 ↔ 行索引"双份状态，故无 RowOf/ShiftRowsBelow/RemoveRowAt/InsertDictKeyRow 一类脚手架；
    /// - 行内编辑仍实时写回字典（TextChanged），且 Rebuild 保留添加栏未提交输入 → 增删不会丢弃正在录入的内容；
    /// - **高度解析自管**：内部行全是 Absolute 常量 → ContentHeight 直接算出，Rebuild 后回写外层 Absolute 行
    ///   （不用 AutoSize：嵌套 TLP 推断高度实测少算，字典项少时添加栏被裁），渲染器无需挂回调；
    /// - 视觉沿用 ConfigFormStyle（组头/缩进/行高/内外边距/交替底色/列宽），与属性区段同款（同一套「行度量」），调用方零改动；
    /// - 控件命名沿用旧规则（组头 grp_、标签 lbl_r_键名、编辑器 cfg_r_键名），翻译资源提取规则不变；
    /// - 可能超宽的文本（组头、键名）由本区段**自持**一个 ToolTip：区段会整段 Rebuild（旧控件被摘除），
    ///   提示表必须随区段清空重建，不能挂在渲染根桶上（否则每次增删都要外部配合清理）；
    ///   挂载时机 = 入树之后（构造期 Rebuild 尚未入树，由 ConfigFormRenderer.Render 收尾调 ApplyTips 补挂）。
    ///
    /// 交互拍板（沿用旧实现）：键名自由输入无协议白名单；重名 = upsert 覆盖既有键（比较器 OrdinalIgnoreCase，大小写变体视为同键）；
    /// ✕ 删除不确认（草稿副本，取消会话即还原）。
    /// 只读锁定：由外部 ConfigFormRenderer.SetReadOnly 递归覆盖（本区段内增删按钮带 OpsTag 标记）。
    /// </summary>
    public class ConfigDictSection : TableLayoutPanel
    {
        /// <summary>增删按钮标记：SetReadOnly 据此仅禁能区段内增删按钮（不影响渲染区其它 UIButton）</summary>
        internal const string OpsTag = "cfg_dict_ops";

        /// <summary>标签控件名前缀（键值行键名）：与旧实现同族，翻译资源提取/匹配依赖</summary>
        private const string LabelPrefix = "lbl_r_";

        /// <summary>编辑器控件名前缀（键值行键名）：与旧实现同族，翻译资源提取/匹配依赖</summary>
        private const string EditorPrefix = "cfg_r_";

        private readonly IDictionary _dict;
        private readonly string _title;
        private readonly int _level;

        /// <summary>添加栏键名输入框（Rebuild 后重建，供连续录入还焦）</summary>
        private UITextBox _keyBox;

        /// <summary>添加栏值输入框（Rebuild 后重建，用于保留/清空未提交输入）</summary>
        private UITextBox _valueBox;

        /// <summary>区段自持提示组件（内容行随 Rebuild 整段重建，提示表必须随之清空）</summary>
        private ToolTip _tip;

        /// <summary>待挂载提示：入树前的登记先入队，ApplyTips 时统一挂载</summary>
        private readonly List<KeyValuePair<Control, string>> _pendingTips = new List<KeyValuePair<Control, string>>();

        /// <summary>
        /// 构造区段。三列布局（标签列 + 值列 + ✕ 列）与外层渲染根同宽同列宽，保证字典行值框与属性行值框右边界对齐。
        /// </summary>
        /// <param name="title">区段显示名（= 字典属性的 [DisplayName]）</param>
        /// <param name="dict">字典本体（后续增删改直接作用于该实例）</param>
        /// <param name="level">缩进层级（与同级属性行一致，组头用 level、内容行用 level+1）</param>
        public ConfigDictSection(string title, IDictionary dict, int level)
        {
            _title = title ?? string.Empty;
            _dict = dict ?? throw new ArgumentNullException(nameof(dict));
            _level = level;

            // 宽度随外层行（跨列全宽）；高度**解析自管**（见 ContentHeight / SyncRowHeight）——
            // 不用 AutoSize：嵌套 TLP 推断高度实测少算，字典项少时添加栏（最后一行）被裁
            AutoSize = false;
            Dock = DockStyle.Fill;
            Margin = Padding.Empty;   // 外层行高与 ContentHeight 精确对齐，不留 Margin 挤压子行
            ColumnCount = 3;
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigFormStyle.LabelWidth));
            ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigFormStyle.DictOpsWidth));
            UiDpi.EnableDoubleBuffered(this);

            Rebuild(clearPendingInput: false);
        }

        #region 整段重画

        /// <summary>
        /// 整段重画：清空 → 组头 + 添加栏 + 键值行（键按 Ordinal 排序，展示稳定，与旧 Flatten 同规则）、键值行交替底色。
        /// </summary>
        /// <param name="clearPendingInput">true = 清空添加栏未提交输入（添加成功后清场，便于连续录入）；
        /// false = 保留（删除路径，避免丢弃用户正在录的内容）</param>
        private void Rebuild(bool clearPendingInput)
        {
            string keepKey = clearPendingInput ? string.Empty : (_keyBox?.Text ?? string.Empty);
            string keepValue = clearPendingInput ? string.Empty : (_valueBox?.Text ?? string.Empty);

            SuspendLayout();
            // 旧控件即将摘除 → 提示表一并作废（ToolTip 内部表会钉住已摘除控件，不清则每次增删都残留一整套）
            _pendingTips.Clear();
            _tip?.RemoveAll();
            // 摘除即释放——不能只 Clear：被摘下的控件句柄仍在、又已失去外部引用，只等 GC 会在**终结器线程**上跑
            // UITextBox.Dispose → 内层 UIEdit.Dispose(true) → DestroyHandle → Handle getter ⇒ 跨线程异常（见 UiControlHelper 头注）
            UiControlHelper.DisposeAndClear(this);
            RowStyles.Clear();
            RowCount = 0;

            AddGroupRow();
            AddEditBarRow(keepKey, keepValue);

            var keys = _dict.Keys.Cast<object>()
                .Select(k => k?.ToString())
                .Where(k => !string.IsNullOrEmpty(k))
                .OrderBy(k => k, StringComparer.Ordinal)
                .ToList();

            bool alt = false;
            foreach (string key in keys)
            {
                AddKeyRow(key, alt);
                alt = !alt;
            }

            ResumeLayout(performLayout: true);
            SyncRowHeight();                        // 行数变化 → 高度变化，回写外层 Absolute 行
            // 交互触发的 Rebuild 此时早已入树 → 当场挂载；构造期（未入树）挂载会静默失效，留给渲染器收尾补调
            if (IsHandleCreated) ApplyTips();
            if (IsHandleCreated) _keyBox.Focus();   // 连续录入：焦点回到键名框（构造期句柄未创建则跳过）
        }

        /// <summary>组头行：跨三列，样式与普通分组标题一致（同一 ConfigFormStyle）</summary>
        private void AddGroupRow()
        {
            RowCount++;
            RowStyles.Add(new RowStyle(SizeType.Absolute, ConfigFormStyle.GroupHeight));

            var label = new UILabel
            {
                Name = "grp_" + _title,   // 组头命名与普通组头同族，供翻译资源提取/匹配
                Text = "── " + _title + " ──",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = ConfigFormStyle.GroupHeight,
                Dock = DockStyle.Fill,
                ForeColor = ConfigFormStyle.GroupColor,
                Font = ConfigFormStyle.GroupFont,
                Padding = ConfigFormStyle.GroupPadding(_level),   // 边距统一在 ConfigFormStyle（与普通组头同款）
            };
            label.AutoEllipsis = true;
            AttachTip(label, _title);   // 组头文本可能超宽 → 悬停见全名（入树后统一挂载）
            Controls.Add(label, 0, RowCount - 1);
            SetColumnSpan(label, 3);
        }

        /// <summary>添加参数栏行：标签列灰字提示，值列内嵌「键名框 + 值框 + 添加钮」</summary>
        private void AddEditBarRow(string keepKey, string keepValue)
        {
            RowCount++;
            RowStyles.Add(new RowStyle(SizeType.Absolute, ConfigFormStyle.RowHeight));
            int row = RowCount - 1;

            var hint = new UILabel
            {
                Text = "＋ 添加参数",
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = ConfigFormStyle.RowHeight,
                Dock = DockStyle.Fill,
                ForeColor = Color.Gray,
                Font = ConfigFormStyle.RowFont,
                Padding = ConfigFormStyle.LabelPadding(_level + 1),   // 与键值行标签同款（边距统一在 ConfigFormStyle）
            };
            hint.AutoEllipsis = true;

            var bar = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                // ⚠️ 必须显式声明行样式：原实现只设 ColumnCount，行样式缺失 → 唯一一行退化为 AutoSize，
                //    行高 = 子控件默认高（UIButton 100×35，不随 DPI 变）→ 按钮固定 35px 而文字随 DPI 变大，
                //    高 DPI 下"添加"二字底部被裁。Percent 100 让单行撑满 bar，子控件拿满行高（随 DPI 线性增长）。
                RowCount = 1,
                // 上下归零（硬要求）：本行可用高 = RowHeight − bar 上下 Margin − 子控件上下 Margin，
                // 原来两处各扣 3+3 → 只剩 30，低于 UITextBox 内层 UIEdit 的需要高度（edit.Height + 2）→ 文字被裁；
                // 左右一并归零：栏内子控件自带 EditorMargin，容器再加 4 → 键名框比键值行编辑框右移 4px（历史错位，已修）
                Margin = ConfigFormStyle.BarMargin,
            };
            bar.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigFormStyle.DictKeyBoxWidth));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            bar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigFormStyle.DictAddBtnWidth));

            _keyBox = new UITextBox
            {
                Dock = DockStyle.Fill,
                Font = ConfigFormStyle.RowFont,
                FillColor = ConfigFormStyle.TextBoxFill,
                Padding = ConfigFormStyle.EditorPadding,
                Margin = ConfigFormStyle.EditorMargin,   // 与键值行编辑器严格等高
                Text = keepKey,
            };
            _valueBox = new UITextBox
            {
                Dock = DockStyle.Fill,
                Font = ConfigFormStyle.RowFont,
                FillColor = ConfigFormStyle.TextBoxFill,
                Padding = ConfigFormStyle.EditorPadding,
                Margin = ConfigFormStyle.EditorMargin,
                Text = keepValue,
            };
            var addBtn = new UIButton
            {
                Tag = OpsTag,
                Text = "添加",
                Dock = DockStyle.Fill,
                Margin = ConfigFormStyle.EditorMargin,
                Font = ConfigFormStyle.RowFont,
            };
            addBtn.Click += (s, e) => ApplyAdd();

            bar.Controls.Add(_keyBox, 0, 0);
            bar.Controls.Add(_valueBox, 1, 0);
            bar.Controls.Add(addBtn, 2, 0);
            Controls.Add(hint, 0, row);
            Controls.Add(bar, 1, row);
        }

        /// <summary>键值行：标签（键名，只读）+ 可编辑值框（TextChanged 实时写回字典）+ 行尾 ✕ 删除</summary>
        private void AddKeyRow(string key, bool alt)
        {
            RowCount++;
            RowStyles.Add(new RowStyle(SizeType.Absolute, ConfigFormStyle.RowHeight));
            int row = RowCount - 1;

            var label = new UILabel
            {
                Name = LabelPrefix + key,
                Text = key,
                TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false,
                Height = ConfigFormStyle.RowHeight,
                Dock = DockStyle.Fill,
                ForeColor = ConfigFormStyle.LabelColor,
                Font = ConfigFormStyle.RowFont,
                Padding = ConfigFormStyle.LabelPadding(_level + 1),   // 与键值行标签同款（边距统一在 ConfigFormStyle）
            };
            label.AutoEllipsis = true;
            AttachTip(label, key);      // 字典键名最易超宽（英文长键）→ 悬停见全名（入树后统一挂载）
            if (alt) label.BackColor = ConfigFormStyle.AltRowFill;

            var editor = new UITextBox
            {
                Name = EditorPrefix + key,
                Dock = DockStyle.Fill,
                Font = ConfigFormStyle.RowFont,
                FillColor = alt ? ConfigFormStyle.AltRowFill : ConfigFormStyle.TextBoxFill,
                Padding = ConfigFormStyle.EditorPadding,
                Margin = ConfigFormStyle.EditorMargin,   // 与添加栏输入框严格等高
            };
            editor.Text = _dict[key]?.ToString() ?? string.Empty;   // 先取初值再挂事件，避免初始化触发写回
            // 实时写回字典（闭包捕获本行键名，随本行控件一同被重画替换而失效）
            editor.TextChanged += (s, e) => _dict[key] = editor.Text;

            var del = new UIButton
            {
                Tag = OpsTag,
                Text = "×",
                Dock = DockStyle.Fill,
                // 上下与同行编辑器统一（原 7 使按钮高仅 RowHeight−14=28，比值框矮 8px → 明显不齐）
                Margin = ConfigFormStyle.DelMargin,
                Font = ConfigFormStyle.RowFont,
                FillColor = Color.IndianRed, // 破坏性操作红底提示（无确认弹窗，颜色即语义）
                ForeColor = Color.White,
            };
            // ✕ 不确认：草稿副本，取消会话即还原；删键后整段重画
            del.Click += (s, e) => { _dict.Remove(key); Rebuild(clearPendingInput: false); };

            Controls.Add(label, 0, row);
            Controls.Add(editor, 1, row);
            Controls.Add(del, 2, row);
        }

        #endregion

        #region 悬停提示

        /// <summary>登记一条悬停提示（组头 / 键名可能超宽）；入树前只入队，ApplyTips 统一挂载</summary>
        private void AttachTip(Control control, string text)
        {
            if (control == null || string.IsNullOrEmpty(text)) return;
            _pendingTips.Add(new KeyValuePair<Control, string>(control, text));
        }

        /// <summary>
        /// 挂载本区段登记的悬停提示（幂等：挂完即清队列）。
        /// **必须在区段入树之后调用**——原生工具项注册要求 TopLevelControl 非空，入树前调用静默失效。
        /// 交互触发的 Rebuild 自行调用（此时早已入树）；构造期的 Rebuild 由 ConfigFormRenderer.Render 收尾补调。
        /// </summary>
        public void ApplyTips()
        {
            if (_pendingTips.Count == 0) return;
            if (_tip == null)
                _tip = new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 100 };
            foreach (var kv in _pendingTips)
                _tip.SetToolTip(kv.Key, kv.Value);
            _pendingTips.Clear();
        }

        /// <summary>区段销毁 → 释放自持提示组件（ToolTip 非控件，不随控件树 Dispose 级联）</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _tip?.Dispose();
                _tip = null;
            }
            base.Dispose(disposing);
        }

        #endregion

        #region 高度自管

        /// <summary>
        /// 区段解析高度：内部行（组头 + 添加栏 + 键值行）全部是 Absolute 行高常量（ConfigFormStyle），
        /// 故高度可直接算出。刻意不交给布局引擎推断——嵌套 TLP 的 AutoSize 实测少算，
        /// 字典项少时添加栏恰是最后一行 → 底部被裁。
        /// </summary>
        public int ContentHeight
            => ConfigFormStyle.GroupHeight + ConfigFormStyle.RowHeight * (1 + VisibleKeyCount());

        /// <summary>可见键数：过滤规则与 Rebuild 完全一致（键 ToString 后非空），保证算出的高度与实际行数对齐</summary>
        private int VisibleKeyCount()
            => _dict.Keys.Cast<object>().Count(k => !string.IsNullOrEmpty(k?.ToString()));

        /// <summary>
        /// 把解析高度回写到外层承载行（外层行须为 Absolute，见 ConfigFormRenderer.AddDictSection），
        /// 再让外层重排——增删键后高度内聚自管，渲染器无需挂回调。构造期 Parent 为空，仅设定自身高度。
        /// </summary>
        private void SyncRowHeight()
        {
            int height = ContentHeight;
            Height = height;
            if (Parent is TableLayoutPanel p)
            {
                int row = p.GetRow(this);
                if (row >= 0 && row < p.RowStyles.Count && p.RowStyles[row].SizeType == SizeType.Absolute)
                {
                    p.RowStyles[row].Height = height;
                    p.PerformLayout();
                    p.Parent?.PerformLayout();   // 外层 AutoScroll 宿主同步滚动范围
                }
            }
        }

        #endregion

        #region 添加（upsert）

        /// <summary>
        /// 添加/覆盖：键名自由输入无白名单；大小写变体 = 同键（字典比较器 OrdinalIgnoreCase）→
        /// 规范化到现键名做覆盖（保留原键名大小写，避免与驱动侧引用漂移）；空键名忽略（无处安放行）。
        /// </summary>
        private void ApplyAdd()
        {
            string key = _keyBox?.Text?.Trim();
            if (string.IsNullOrEmpty(key)) return;

            string targetKey = null;
            foreach (object k in _dict.Keys)
                if (k is string ks && string.Equals(ks, key, StringComparison.OrdinalIgnoreCase)) { targetKey = ks; break; }

            _dict[targetKey ?? key] = _valueBox?.Text ?? string.Empty;
            Rebuild(clearPendingInput: true);
        }

        #endregion
    }
}
