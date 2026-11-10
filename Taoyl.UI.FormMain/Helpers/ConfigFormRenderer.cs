using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// 表单生成器：扁平行列表 → TableLayoutPanel 供目标容器使用。渲染三分支（按 Kind 分派）：
    /// - 属性行（Property）：编辑器通过 DataBindings 双向绑定到 Target 实例——用户编辑 → 自动写回，
    ///   外部修改 → UI 自动刷新，保证数据始终一致；带 OptionsSource 的 string 行渲为下拉；
    /// - 分组标题行（GroupHeader）：跨列标题；
    /// - 字典区段行（DictGroupHeader）：本行整段交出，由 ConfigDictSection 自持渲染（组头 + 添加栏 + 键值行），
    ///   字典的增删改全在区段内部直接作用于字典本体，本渲染器不参与字典行维护（外层行为 Absolute，行高取区段解析高度，
    ///   键增删后由区段自行回写）。
    /// 只读锁定：SetReadOnly 递归覆盖整棵控件树（含字典区段内部），调用方按会话态决定。
    /// </summary>
    public static class ConfigFormRenderer
    {
        // ── 编辑器工厂（按类型特征匹配，顺序即优先级）──
        private static readonly (Func<Type, bool> Matcher, Func<ConfigFlatRow, Control> Factory)[] EditorFactories =
        {
            (t => t == typeof(bool),    r => MakeSwitch(r)),
            (t => t == typeof(bool?),   r => MakeSwitch(r)),
            (t => t == typeof(int),     r => MakeTextBox(r)),
            (t => t == typeof(double),  r => MakeTextBox(r)),
            (t => t == typeof(float),   r => MakeTextBox(r)),
            (t => t == typeof(decimal), r => MakeTextBox(r)),
            (t => t == typeof(long),    r => MakeTextBox(r)),
            (t => t == typeof(short),   r => MakeTextBox(r)),
            (t => t == typeof(string),  r => MakeTextBox(r)),
            (t => t.IsEnum,             r => MakeCombo(r)),
        };

        /// <summary>
        /// 渲染扁平行到目标容器。
        /// </summary>
        /// <param name="target">承载容器（渲染前 Clear）</param>
        /// <param name="rows">扁平行列表</param>
        /// <param name="valueWidth">值列固定宽（&gt;0 时启用"内容定宽"模式：渲染根不随宿主宽伸缩，
        /// 内容总宽 = LabelWidth + valueWidth，宿主为 AutoScroll 容器时超宽部分由横向滚动条承载；
        /// ≤0 时值列为 Percent 100 自适应宿主宽——默认行为，FormParamSetting 等既有调用点不变）</param>
        public static void Render(Control target, List<ConfigFlatRow> rows, int valueWidth = 0)
        {
            target.SuspendLayout();
            // 旧控件树即将摘除 → 其悬停提示条目一并作废（否则 ToolTip 内部表钉住已摘除控件，随 Render 累积）
            ConfigFormStyle.ResetTips(target);
            // 摘除即释放——不能只 Clear（见 UiControlHelper 头注：句柄不随摘除销毁，只等 GC 会走终结器线程 Dispose）
            UiControlHelper.DisposeAndClear(target);

            bool wideContent = valueWidth > 0;
            // 含字典区段才启用第三列 ✕ 列（与区段内部 ✕ 列同宽）——保证字典行值框与属性行值框右边界对齐；
            // 无字典调用点（FormParamSetting 等）保持 2 列零回归
            bool hasDict = rows.Any(r => r.Kind == ConfigRowKind.DictGroupHeader);
            int columns = hasDict ? 3 : 2;
            var panel = new TableLayoutPanel { AutoSize = true, ColumnCount = columns };
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigFormStyle.LabelWidth));
            if (wideContent)
            {
                // 定宽内容：不 Dock，AutoSize 按 Absolute 列自算总宽（LabelWidth+valueWidth），
                // 宿主 AutoScroll 检测到子控件超视口即出横向滚动条
                panel.Location = new Point(0, 0);
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, valueWidth));
            }
            else
            {
                panel.Dock = DockStyle.Top;
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            }
            if (hasDict)
                panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ConfigFormStyle.DictOpsWidth));
            UiDpi.EnableDoubleBuffered(panel);
            panel.SuspendLayout();

            // 本批次待挂悬停提示：构建期控件尚未入树，此时 SetToolTip 会被静默丢弃（见 ConfigFormStyle.AttachTip），
            // 故先收集、待整棵树入树后统一挂载。列表是局部量——无静态状态，无跨批次残留。
            var tips = new List<KeyValuePair<Control, string>>();

            bool alt = false;
            foreach (var r in rows)
            {
                switch (r.Kind)
                {
                    case ConfigRowKind.DictGroupHeader:
                        AddDictSection(panel, r, columns);
                        alt = false;
                        break;

                    case ConfigRowKind.GroupHeader:
                        AddGroup(panel, r.Name, r.Level, columns, tips);
                        alt = false;
                        break;

                    default:
                        AddProperty(panel, r, ref alt, tips);
                        break;
                }
            }

            panel.ResumeLayout(performLayout: false);
            target.Controls.Add(panel);
            target.ResumeLayout(performLayout: true);
            // 整棵树已入树 → 统一挂载悬停提示（构建期入树前 SetToolTip 会静默失效，故收敛到此处挂）：
            // 属性行/组头由本渲染器挂到 target 桶；字典区段的内容行由区段自持提示，此处只负责唤起它挂载
            // （区段构造期的挂载发生在入树前，必然失效，故必须在这里补一次）。
            foreach (var kv in tips)
                ConfigFormStyle.AttachTip(target, kv.Key, kv.Value);
            foreach (Control c in panel.Controls)
            {
                ConfigDictSection section = c as ConfigDictSection;
                if (section != null) section.ApplyTips();
            }
        }

        public static void SetReadOnly(Control root, bool readOnly)
        {
            foreach (Control c in root.Controls)
            {
                if (c is UITextBox txt)      txt.ReadOnly = readOnly;
                else if (c is UISwitch sw)   sw.Enabled   = !readOnly;
                else if (c is UIComboBox cmb) cmb.Enabled = !readOnly;
                // 字典区段增删按钮（带 OpsTag）：只动标记控件，浏览态不出现可点入口；不影响渲染区其它 UIButton
                else if (c is UIButton btn && ConfigDictSection.OpsTag.Equals(btn.Tag as string, StringComparison.Ordinal))
                    btn.Enabled = !readOnly;
                if (c.Controls.Count > 0) SetReadOnly(c, readOnly);
            }
        }

        #region 行渲染

        private static void AddGroup(TableLayoutPanel p, string title, int level, int columns,
            List<KeyValuePair<Control, string>> tips)
        {
            p.RowCount++;
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, ConfigFormStyle.GroupHeight));
            var l = new UILabel {
                Name = "grp_" + title,   // 组头名称：供翻译资源提取/匹配（与 lbl_ 规则同族）
                Text = "── " + title + " ──", TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false, Height = ConfigFormStyle.GroupHeight, Dock = DockStyle.Fill,
                ForeColor = ConfigFormStyle.GroupColor, Font = ConfigFormStyle.GroupFont,
                Padding = ConfigFormStyle.GroupPadding(level),   // 边距统一在 ConfigFormStyle（编辑器）
            }; l.AutoEllipsis = true;
            tips.Add(new KeyValuePair<Control, string>(l, title));   // 超宽时悬停见全名（不做截断测量；入树后统一挂载）
            p.Controls.Add(l, 0, p.RowCount - 1); p.SetColumnSpan(l, columns);
        }

        /// <summary>字典区段行：Absolute 行 + 跨列放入自持区段控件（行高取区段解析高度，键增删后由区段自行回写）</summary>
        private static void AddDictSection(TableLayoutPanel p, ConfigFlatRow r, int columns)
        {
            var section = new ConfigDictSection(r.Name, r.Dict, r.Level);
            p.RowCount++;
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, section.ContentHeight));
            p.Controls.Add(section, 0, p.RowCount - 1);
            p.SetColumnSpan(section, columns);
        }

        private static void AddProperty(TableLayoutPanel p, ConfigFlatRow r, ref bool alt,
            List<KeyValuePair<Control, string>> tips)
        {
            p.RowCount++;
            p.RowStyles.Add(new RowStyle(SizeType.Absolute, ConfigFormStyle.RowHeight));
            var l = new UILabel {
                Name = LabelName(r),   // 参数描述名称：供翻译资源提取/匹配
                Text = r.Name, TextAlign = ContentAlignment.MiddleLeft,
                AutoSize = false, Height = ConfigFormStyle.RowHeight, Dock = DockStyle.Fill,
                ForeColor = ConfigFormStyle.LabelColor, Font = ConfigFormStyle.RowFont,
                Padding = ConfigFormStyle.LabelPadding(r.Level),   // 边距统一在 ConfigFormStyle（编辑器）
            }; l.AutoEllipsis = true;
            tips.Add(new KeyValuePair<Control, string>(l, r.Name));   // 描述超宽时悬停见全名（不做截断测量；入树后统一挂载）

            Control e = BuildEditor(r);
            if (alt) { l.BackColor = ConfigFormStyle.AltRowFill; TryAltFill(e, true); }
            int row = p.RowCount - 1;
            p.Controls.Add(l, 0, row); p.Controls.Add(e, 1, row);
            alt = !alt;
        }

        #endregion

        #region 编辑器 + DataBinding（从源自动读取初始值，不触发 SetProperty）

        private static Control BuildEditor(ConfigFlatRow r)
        {
            // 带候选选项源的 string 属性行 → 下拉（如 PlcConfig.Protocol → PlcProtocol 规范串；历史值不在候选不强制改写）
            if (r.OptionsSource != null && r.Property.PropertyType == typeof(string))
                return MakeOptionsCombo(r);

            Type t = r.Property.PropertyType;
            foreach (var e in EditorFactories)
                if (e.Matcher(t)) return e.Factory(r);
            return MakeTextBox(r); // 兜底：未知类型一律文本框
        }

        /// <summary>
        /// 候选选项源 string 属性行下拉（DropDownList）：选择即写回 Target 属性（事件写回，照 MakeCombo 模式）。
        /// 当前值不在候选中（历史手写值/新协议）→ 不预选中（下拉不动不改写，避免保存即覆盖手写值）。
        /// </summary>
        private static Control MakeOptionsCombo(ConfigFlatRow r)
        {
            var c = new UIComboBox
            {
                Name = EditorName(r),
                DropDownStyle = UIDropDownStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = ConfigFormStyle.RowFont,
                ItemHeight = ConfigFormStyle.ComboItemHeight,   // UIComboBox 无 ItemSize；默认 25 物理像素不随 DPI 缩放
                Margin = ConfigFormStyle.EditorMargin,          // 原用 WinForms 默认 (3,3,3,3)，与文本框不同高 → 统一
            };
            c.Items.AddRange(r.OptionsSource.Cast<object>().ToArray()); // string 项直接显示

            string current = r.Property.GetValue(r.Target)?.ToString() ?? string.Empty;
            if (!string.IsNullOrEmpty(current))
            {
                int idx = r.OptionsSource.ToList().IndexOf(current);
                if (idx >= 0)
                    c.SelectedIndex = idx; // 事件未挂，不触发写回
            }

            // 用户选择 → 写回（走 setter，设脏 + 通知）
            c.SelectedIndexChanged += (s, e) =>
            {
                if (c.SelectedItem != null)
                    r.Property.SetValue(r.Target, c.SelectedItem.ToString());
            };

            return c;
        }

        private static Control MakeTextBox(ConfigFlatRow r)
        {
            var t = new UITextBox { Name = EditorName(r), Dock = DockStyle.Fill,
                Font = ConfigFormStyle.RowFont, FillColor = ConfigFormStyle.TextBoxFill,
                Padding = ConfigFormStyle.EditorPadding, Margin = ConfigFormStyle.EditorMargin };
            // 先加绑定再设置任何属性——绑定自动从 ConfigBase 读值赋给 Text，
            // 此过程不调用 SetProperty，不产生脏标记
            t.DataBindings.Add("Text", r.Target, r.Property.Name);
            return t;
        }
        private static Control MakeSwitch(ConfigFlatRow r) { var s = new UISwitch
            { Name = EditorName(r), Dock = DockStyle.Left, Margin = ConfigFormStyle.SwitchMargin ,ActiveText="On",InActiveText="Off"};
            s.DataBindings.Add("Active", r.Target, r.Property.Name); return s; }
        private static Control MakeCombo(ConfigFlatRow r)
        {
            var c = new UIComboBox
            {
                Name = EditorName(r),
                DropDownStyle = UIDropDownStyle.DropDownList,
                Dock = DockStyle.Fill,
                Font = ConfigFormStyle.RowFont,
                ItemHeight = ConfigFormStyle.ComboItemHeight,   // UIComboBox 无 ItemSize；默认 25 物理像素不随 DPI 缩放
                Margin = ConfigFormStyle.EditorMargin,          // 原用 WinForms 默认 (3,3,3,3)，与文本框不同高 → 统一
            };

            // 包装：匿名类 { Display = 中文, Value = 枚举值 }（与 cmbLanguage 同套路）
            var options = Enum.GetValues(r.Property.PropertyType)
                .Cast<Enum>()
                .Select(v => new { Display = v.GetDescription(), Value = v })
                .ToList();

            c.DataSource = options;
            c.DisplayMember = "Display";
            c.ValueMember = "Value";

            // 初始选中（事件未挂，不触发写回）
            c.SelectedValue = r.Property.GetValue(r.Target);

            // 用户选择 → 写回（走 setter，设脏 + 通知）
            c.SelectedIndexChanged += (s, e) =>
            {
                if (c.SelectedValue != null)
                    r.Property.SetValue(r.Target, c.SelectedValue);
            };

            return c;
        }

        #endregion

        #region 辅助

        // 控件名规则：所属类型 + 属性名（供翻译资源提取/匹配）。
        // 字典键值行的 lbl_r_/cfg_r_ 同族命名在 ConfigDictSection 内保持一致。
        private static string EditorName(ConfigFlatRow r)
            => "cfg_" + r.Property.DeclaringType.Name + "_" + r.Property.Name;
        private static string LabelName(ConfigFlatRow r)
            => "lbl_" + r.Property.DeclaringType.Name + "_" + r.Property.Name;
        private static void TryAltFill(Control c, bool alt)
        { if (c is UITextBox t) t.FillColor = alt ? ConfigFormStyle.AltRowFill : ConfigFormStyle.TextBoxFill; }

        #endregion
    }
}
