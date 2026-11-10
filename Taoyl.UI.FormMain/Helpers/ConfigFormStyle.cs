using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// ConfigFormRenderer 的全局样式配置，所有可调参数集中于此——**宽、高、内外边距全部在这里定义**，
    /// 属性区段（ConfigFormRenderer）与字典区段（ConfigDictSection）读同一套值，改一处两处同步。
    /// 尺寸（行高/列宽/控件高）是 100% 设计基准值，运行时按 UiDpi.Scale 放大（System aware 下物理像素布局，
    /// 行高/列宽硬编码会装不下 GlobalFontScale 放大的文字；计算属性幂等，Render 每次读取即得缩放值）。
    /// **行内边距刻意不缩放**（见「行度量」区）：它是留白不是文字承载尺寸，保持物理像素只会更宽松。
    /// </summary>
    public static class ConfigFormStyle
    {
        // ── 布局（100% 基准值 → UiDpi.Scale 运行时放大）──
        // LabelWidth 由 180 提到 240：容纳最长参数名/字典英文键；仍超出的文本由 AttachTip 悬停补全全名，不做截断测量
        public static int LabelWidth  => UiDpi.Scale(200);
        public static int RowHeight   => UiDpi.Scale(42);
        public static int GroupHeight => UiDpi.Scale(32);
        public static int IndentWidth => UiDpi.Scale(20);

        // ── 下拉列表项高（UIComboBox 无 ItemSize，只有 ItemHeight；默认 25 物理像素不随 DPI 缩放
        //    → 高 DPI 下项高夹字，且下拉总高 = 4 + min(项数,8)×ItemHeight 一并偏矮）──
        public static int ComboItemHeight => UiDpi.Scale(25);

        // ── 字典行增删（ConfigFormRenderer 字典组专用；含字典的扁平行才启用第三列，无字典调用点零回归）──
        public static int DictOpsWidth    => UiDpi.Scale(56);  // 键值行行尾 ✕ 删除按钮列宽
        public static int DictKeyBoxWidth => UiDpi.Scale(160); // 添加参数栏键名输入框宽
        public static int DictAddBtnWidth => UiDpi.Scale(80);  // 添加参数栏"添加"按钮宽

        #region 行度量（宽高 + 内外边距：属性区段与字典区段共用同一套）

        // ── 边距基准值（物理像素，**刻意不做 UiDpi.Scale**）──
        // 与上面的控件尺寸不同：这些是行内留白，不承载文字。不缩放 → 高 DPI 下行内可用高更大，只会更宽松不会夹字；
        // 收敛前后视觉零变化。将来若要整体跟着 DPI 走，只需把这组常量的读取处改成 UiDpi.Scale(...)，调用点无需改动。
        private const int RowLabelIndent   = 12; // 内容行标签左内缩（属性行 / 字典键值行 / 添加栏提示）
        private const int GroupLabelIndent = 8;  // 组头标签左内缩
        private const int LabelRightPad    = 6;  // 内容行标签右内缩（与值列拉开）
        private const int EditorPad        = 6;  // UITextBox 内文字左右内缩
        private const int RowEdge          = 3;  // 行内编辑器左右外边距
        private const int RowVGap          = 2;  // 行内编辑器上下外边距（可用高 = RowHeight − 2×RowVGap）
        private const int DelLeft          = 6;  // 行尾 ✕ 钮左外距（与值框拉开）
        private const int DelRight         = 4;  // 行尾 ✕ 钮右外距
        private const int SwitchGap        = 8;  // UISwitch 左外距
        private const int SwitchVGap       = 4;  // UISwitch 上下外距（自绘尺寸小，上下留白略大于编辑器）

        /// <summary>内容行标签内边距（属性行 / 字典键值行 / 添加栏提示）：按层级缩进</summary>
        public static Padding LabelPadding(int level)
            => new Padding(RowLabelIndent + level * IndentWidth, 0, LabelRightPad, 0);

        /// <summary>组头标签内边距（普通组头 / 字典组头）：跨列，右侧不留白</summary>
        public static Padding GroupPadding(int level)
            => new Padding(GroupLabelIndent + level * IndentWidth, 0, 0, 0);

        /// <summary>输入框内文字内缩（UITextBox.Padding）</summary>
        public static Padding EditorPadding => new Padding(EditorPad, 0, EditorPad, 0);

        /// <summary>行内编辑器外边距（文本框 / 下拉框 / 添加栏三件套）：上下 RowVGap → 可用高 = RowHeight − 2×RowVGap</summary>
        public static Padding EditorMargin => new Padding(RowEdge, RowVGap, RowEdge, RowVGap);

        /// <summary>行尾 ✕ 删除钮外边距（上下与同行编辑器一致，保证等高）</summary>
        public static Padding DelMargin => new Padding(DelLeft, RowVGap, DelRight, RowVGap);

        /// <summary>
        /// 添加栏容器外边距。**左右必须归零**——栏内子控件自带 EditorMargin，
        /// 容器再加左右边距 → 键名框比键值行编辑框右移 4px（历史真不一致，本次已修）；
        /// **上下必须归零**——容器边距与子控件边距会叠加，双重扣减后可用高不足以容纳
        /// UITextBox 内层 UIEdit（edit.Height + 2）→ 文字底部被裁。
        /// </summary>
        public static Padding BarMargin => Padding.Empty;

        /// <summary>UISwitch 外边距</summary>
        public static Padding SwitchMargin => new Padding(SwitchGap, SwitchVGap, 0, SwitchVGap);

        #endregion

        // ── 字体 ──
        public static Font RowFont   = new Font("Microsoft YaHei", 12F);
        public static Font GroupFont = new Font("Microsoft YaHei", 12F, FontStyle.Bold);

        // ── 颜色 ──
        public static Color GroupColor   = Color.FromArgb(0, 90, 170);
        public static Color LabelColor   = Color.FromArgb(40, 40, 40);
        public static Color TextBoxFill  = Color.White;
        public static Color AltRowFill   = Color.FromArgb(248, 248, 252);

        #region 悬停提示

        /// <summary>
        /// 悬停提示组件按「渲染根」分桶（键 = 渲染宿主控件，桶内共享一个 ToolTip）。
        /// </summary>
        /// <remarks>
        /// 为什么不共用全局单例：
        /// ① 一个窗体内可能同时存在多个渲染根（FormParamSetting 的系统页/基础页），全局单例的 RemoveAll
        ///    会连带清掉另一根已挂好的提示；
        /// ② 同一渲染根反复 Render 时上一批控件已被摘除，其提示条目必须整体作废——否则 ToolTip 内部表
        ///    始终钉住已摘除的控件（HandleDestroyed 并不移除 created 条目），随 Render 次数累积：
        ///    这正是"悬停提示先有后无"的根因。
        /// 清理时机：a) ResetTips（渲染根控件树即将 Clear）；b) 渲染根 Dispose（窗体销毁）。
        /// </remarks>
        private static readonly Dictionary<Control, ToolTip> _tipBuckets = new Dictionary<Control, ToolTip>();

        /// <summary>新建提示组件（延迟参数统一：10s 滞留 / 400ms 首现 / 100ms 再现）</summary>
        private static ToolTip NewTip()
            => new ToolTip { AutoPopDelay = 10000, InitialDelay = 400, ReshowDelay = 100 };

        /// <summary>
        /// 丢弃某渲染根的提示桶（渲染根控件树即将 Clear 时先调——旧控件即将摘除，其提示条目必须一并作废）。
        /// </summary>
        /// <param name="scope">渲染根（分桶键，通常是渲染宿主面板）</param>
        public static void ResetTips(Control scope)
        {
            if (scope == null) return;
            if (!_tipBuckets.TryGetValue(scope, out ToolTip tip)) return;

            _tipBuckets.Remove(scope);
            tip.RemoveAll();
            tip.Dispose();
        }

        /// <summary>
        /// 为可能被省略号截断的文本登记悬停全名（属性标签 / 分组标题）。
        /// 刻意不做截断测量——避免把"自适应列宽"逻辑引回来；一律登记，文本溢出时鼠标悬停即见全名。
        ///
        /// ⚠️ **必须在控件入树之后调用**（反编译 System.Windows.Forms.ToolTip 实证）：
        /// 原生工具项注册走 CreateRegion，其条件为「控件已建句柄 && 控件 TopLevelControl 非空 && 顶层已建句柄」；
        /// 构建期控件尚未 Controls.Add 入树 → 顶层解析为空 → 工具项**静默不注册**（无异常、无日志、悬停无反应）。
        /// 调用点 = ConfigFormRenderer.Render 收尾（唯一的入树后统一挂载时机）。
        /// </summary>
        /// <param name="scope">渲染根（分桶键，与 Render 的清理/销毁时机一致）</param>
        /// <param name="control">被提示控件（须已入树）</param>
        /// <param name="text">悬停显示的全名</param>
        public static void AttachTip(Control scope, Control control, string text)
        {
            if (scope == null || control == null || string.IsNullOrEmpty(text)) return;

            if (!_tipBuckets.TryGetValue(scope, out ToolTip tip))
            {
                tip = NewTip();
                _tipBuckets.Add(scope, tip);
                // 渲染根销毁 → 整桶回收。桶是静态表（外部根），且 ToolTip 内部表强引用被提示控件、
                // 被提示控件又经 Parent 链强引用渲染根——不显式移除则整片控件树都吊在静态表上。
                EventHandler onDisposed = null;
                onDisposed = (s, e) => { scope.Disposed -= onDisposed; ResetTips(scope); };
                scope.Disposed += onDisposed;
            }
            tip.SetToolTip(control, text);
        }

        #endregion
    }
}
