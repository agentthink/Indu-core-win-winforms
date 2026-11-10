using System;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.Audit.Builder
{
    /// <summary>
    /// 数组元素的取名与配对 —— **本模块里两个最容易被混为一谈的概念在此显式分开**：
    ///
    /// - **配对锚点**（<see cref="ElementKey"/> ← <see cref="KeyNames"/>）：**求稳**。
    ///   决定"前后两侧哪两个元素是同一条数据"，直接影响比对**正确性**，
    ///   故只认用户改不了的稳定键；宁可退化成粗粒度（整个数组一条 JSON），也不冒险取可编辑名。
    /// - **展示名**（<see cref="ElementLabelOf"/> ← <see cref="LabelNames"/>）：**求好读**。
    ///   只决定"路径里显示成什么"，取错最多是路径不好读，**不影响比对正确性**，覆盖宽一点是正收益。
    ///
    /// 把两者合成一个名单是这套逻辑最自然的写法，也是最危险的写法：
    /// 展示首选的 DisplayName 恰恰是用户可编辑字段，拿它配对 ⇒ 改一次显示名，
    /// 前后两侧算出两个不同锚点 ⇒ 同一条数据被读成"删一条 + 增一条"。
    /// </summary>
    internal static class AuditArrayAnchor
    {
        /// <summary>
        /// 数组元素的**展示名**候选（与 <see cref="KeyNames"/> 的配对锚点刻意分开：一个管好读、一个管配对稳）。
        /// 取第一个非空标量值，都没有则退回配对锚点、再退回下标。
        /// 与锚点名单的取舍相反，这里**不做收缩**：展示名只决定"路径里显示成什么"，
        /// 取错最多是路径不好读，**不影响比对正确性**（配对另走 <see cref="KeyNames"/>），覆盖宽一点是正收益。
        /// 四个名字对应本仓列表模型的实际字段：RecipeParameter.DisplayName（配方参数）/
        /// 通用 Name 与 RoleName / 地址表元素 TagName。
        /// </summary>
        private static readonly string[] LabelNames =
        {
            "DisplayName", "Name", "RoleName", "TagName"
        };

        /// <summary>
        /// 数组元素的**配对锚点**候选（**顺序即优先级**，取第一个非空标量值）。
        /// 展示名另见 <see cref="LabelNames"/>——两者分开的理由见本类注释（锚点求稳、展示求好读）。
        ///
        /// 🔴 **顺序是功能性的，不是随手排的**：稳定键（Key / Code / Id / UserId —— 建立后就不再变的
        /// 业务标识或数据库主键）必须排在可编辑名（Name / RoleName）**之前**。
        /// 排反的后果正是本模块最怕的那种静默错账：锚点取到可编辑字段 ⇒ 用户改一次名，前后两侧算出两个
        /// 不同锚点 ⇒ 同一条数据被读成"删一条 + 增一条"，而不是"某字段改了"。
        /// 这类错不报错、不丢数据，只会让审计看起来像有人删了东西——比漏记更难查。
        ///
        /// 名单**刻意保持收敛**（六个），不再往"看起来可能有用"的属性名上扩：
        /// 多一个候选名 = 多一次误判机会（撞上某个可编辑的业务名就翻车），而收益只是省下补一行代码。
        /// 本仓现有消费者：RecipeParameter.Key（配方参数数组）；Id / UserId / Code / RoleName 是通用主键名，
        /// 覆盖后续可能接 diff 的列表模型。
        /// ⚠️ 地址表元素主名 TagName **刻意不在此列**：它是用户可编辑字段，放进来正撞上面那条红线；
        /// 它只作展示名（见 <see cref="LabelNames"/>），故地址表数组无法按锚点配对，
        /// 会退化为"整个数组一条 JSON"——这是**已知且刻意**的取舍，不是遗漏。
        ///
        /// 新增列表模型时若主键不在此列，往这里补一个名字即可（无需改比对逻辑）；
        /// 补之前先确认该字段**用户改不了**。
        /// </summary>
        private static readonly string[] KeyNames =
        {
            "Key", "Code", "Id", "UserId", "Name", "RoleName"
        };

        /// <summary>
        /// 元素的人性化展示名（先 <see cref="LabelNames"/>，再退回配对锚点，都没有才返回 null）。
        /// 为什么不索性拿它当配对锚点：DisplayName 是用户可编辑字段，
        /// 编辑态里改了显示名、前后两侧锚点就不同 ⇒ 会被误判成"删一条 + 增一条"。
        /// </summary>
        internal static string ElementLabelOf(JToken token, string fallbackKey)
        {
            var obj = token as JObject;
            if (obj != null)
            {
                foreach (var name in LabelNames)
                {
                    var value = obj[name] as JValue;
                    if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                        continue;

                    string text = Convert.ToString(value.Value, CultureInfo.InvariantCulture);
                    if (!string.IsNullOrWhiteSpace(text)) return text.Trim();
                }
            }
            return string.IsNullOrEmpty(fallbackKey) ? null : fallbackKey;
        }

        /// <summary>
        /// 取元素的**配对锚点**（按 <see cref="KeyNames"/> 优先级，取第一个非空标量值）。
        /// 与展示名（<see cref="ElementLabelOf"/>）刻意分开：锚点求稳（用户不可编辑），展示求好读。
        ///
        /// 只认标量：遇到对象 / 数组直接跳过（属性名撞车时不至于把整棵子树塞进路径段）。
        /// </summary>
        internal static string ElementKey(JToken token)
        {
            var obj = token as JObject;
            if (obj == null) return null;

            foreach (var name in KeyNames)
            {
                var value = obj[name] as JValue;
                if (value == null || value.Type == JTokenType.Null || value.Type == JTokenType.Undefined)
                    continue;

                string text = Convert.ToString(value.Value, CultureInfo.InvariantCulture);
                if (!string.IsNullOrEmpty(text)) return text;
            }
            return null;
        }
    }
}
