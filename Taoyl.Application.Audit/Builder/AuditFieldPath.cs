using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.Audit.Builder
{
    /// <summary>
    /// 明细路径的**渲染**（点分 / 下标 / 方括号段的拼装）。
    ///
    /// 路径有两种形态，成对出现、用途严格分离（见 <see cref="AuditChangeBuilder"/> 类注释）：
    /// - **原名路径**：JSON 属性名，只用于两处判定——顶层跳过名单、敏感字段脱敏。
    ///   这两处都必须看原名：跳过名单来自 nameof(...)、敏感名单是英文名，翻译后再比对全部失配。
    /// - **显示名路径**：模型上的中文显示名，落 FieldPath（给人读的那一份）。
    ///
    /// 本类不判断该用哪一种 —— 两个路径由调用方分别拼装后一起传下去。
    /// </summary>
    internal static class AuditFieldPath
    {
        /// <summary>顶层就是标量时的兜底路径（整对象被替换的极端情形），避免 FieldPath 出现空串无主行。</summary>
        internal const string RootPath = "(根)";

        /// <summary>对象属性段：点分拼接（父路径为空时直接用子名，避免出现前导点）。</summary>
        internal static string Join(string path, string name)
        {
            return string.IsNullOrEmpty(path) ? name : path + "." + name;
        }

        /// <summary>路径追加一个数组段（段自带方括号，故直接相接、不加分隔符）。</summary>
        internal static string Append(string path, string segment)
        {
            return (path ?? string.Empty) + segment;
        }

        internal static string Bracket(string text)
        {
            return "[" + text + "]";
        }

        /// <summary>
        /// 数组元素路径段：优先人性化展示名，退回配对锚点，再退回下标。
        /// 下标是**最后兜底**而非首选——它与位置绑定，增删一项就换了意思（见 AuditChangeWalker.WalkArray）。
        /// </summary>
        internal static string ElementSegment(JToken element, int index)
        {
            string label = AuditArrayAnchor.ElementLabelOf(element, AuditArrayAnchor.ElementKey(element));
            return label != null ? Bracket(label) : Bracket(index.ToString(CultureInfo.InvariantCulture));
        }
    }
}
