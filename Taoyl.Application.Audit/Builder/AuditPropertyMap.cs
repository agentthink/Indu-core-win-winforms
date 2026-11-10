using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.Audit.Builder
{
    /// <summary>
    /// 类型反射中心 —— **属性查询缓存** 与 **中文显示名翻译**。
    ///
    /// 译文规则：显示名优先取 `[Description]`、其次 `[DisplayName]`，都没有才退回属性名。
    /// 为什么必须翻译：审计的读者是人（审计员 / 日后接手的工程师）。落 JSON 属性名
    /// （Min / UserNumber / autoLogoutTime）等于把"这人到底改了什么"变成猜谜题——现场已就此提过整改
    /// （"所改即所记！改的是什么，显示的就是什么"）。
    /// 翻译与列名的既有惯例同源：FormUserManager 的列头就是反射 `[Description]` 拿的中文名。
    ///
    /// 🔴 本类所有方法在**取不到类型**时（快照是 JToken / 字典键 / 未知类型）必须安静退回 null，
    /// 由调用方退回原名 —— 翻译失效只让明细不好读，绝不允许影响比对结果。
    ///
    /// 本类不持有任何状态外的副作用：缓存只增不改，可安全并发访问。
    /// </summary>
    internal static class AuditPropertyMap
    {
        /// <summary>
        /// 类型 → (C# 属性名 / JSON 属性名 → PropertyInfo) 双索引映射缓存。
        ///
        /// 为什么要缓存：路径翻译是**逐段**查属性的，一条明细要穿过整条路径；而 Save 一次能出几十条明细，
        /// 每次比对都重走反射是不可接受的。
        /// 为什么索引两种名字：JSON 键名未必等于属性名——DevConfig.AutoLogoutTime 带
        /// `[JsonProperty("autoLogoutTime")]`，只按属性名索引的话这一栏查不到、翻译直接失效。
        /// 为什么用 ConcurrentDictionary：写侧可能在后台线程（PLC 保存走 Task.Run），读侧同时在查。
        /// </summary>
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertyCache =
            new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();

        /// <summary>
        /// 推断根类型，供逐段翻译查属性。
        ///
        /// 从**实体侧**取：调用方可能一侧传 JToken 快照（RecipeEditVM 的 editSnapshot）、另一侧传实体，
        /// 此时实体那侧就是类型来源。两侧都是 JToken（或 null）时返回 null ——
        /// 翻译整体退化为原来的属性名，明细照样正确、只是不好读，绝不影响比对结果。
        /// </summary>
        internal static Type RootTypeOf(object before, object after)
        {
            if (before != null && !(before is JToken)) return before.GetType();
            if (after != null && !(after is JToken)) return after.GetType();
            return null;
        }

        /// <summary>
        /// 把跳过名单（调用方传的 C# 属性名）换成**显示名**集合。
        ///
        /// 为什么必须换：FieldPath 落的是显示名，而调用方传的是 nameof(...)（C# 属性名），
        /// 两者不等时直接比对会静默失配——表现为配方保存又冒出"变更人员 / 变更时间"两条与操作者意图无关的明细。
        /// 无显示名的属性，其显示名即原名，集合里保持原名即可。
        /// </summary>
        internal static HashSet<string> TranslateSkipNames(string[] names, Type rootType)
        {
            if (names == null || names.Length == 0) return null;

            var set = new HashSet<string>(StringComparer.Ordinal);
            foreach (var name in names)
            {
                var prop = ResolveProperty(rootType, name);
                set.Add(DisplayNameOf(prop) ?? name);
            }
            return set;
        }

        /// <summary>
        /// 查属性：比对时的键名来自 JObject（JSON 键名），而调用方可能传 C# 属性名，
        /// 故映射表里两种名字都索引。类型为 null 或查不到一律返回 null（调用方退回原名）。
        /// </summary>
        internal static PropertyInfo ResolveProperty(Type type, string name)
        {
            if (type == null || string.IsNullOrEmpty(name)) return null;

            var map = PropertyCache.GetOrAdd(type, BuildPropertyMap);
            PropertyInfo prop;
            return map.TryGetValue(name, out prop) ? prop : null;
        }

        /// <summary>
        /// 建 (C# 属性名 / JSON 属性名 → PropertyInfo) 双索引映射。
        /// 必须双索引：JSON 键名未必等于属性名——DevConfig.AutoLogoutTime 带
        /// `[JsonProperty("autoLogoutTime")]`，只按属性名索引那一栏就查不到、翻译直接失效。
        /// </summary>
        private static Dictionary<string, PropertyInfo> BuildPropertyMap(Type type)
        {
            var map = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);

            foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (!prop.CanRead) continue;

                map[prop.Name] = prop; // 索引器会以 Item 之名进来，JSON 里不会有该键，无害

                var json = prop.GetCustomAttribute<JsonPropertyAttribute>();
                if (json != null && !string.IsNullOrEmpty(json.PropertyName))
                    map[json.PropertyName] = prop;
            }

            return map;
        }

        /// <summary>
        /// 属性的中文显示名：`[Description]` 优先，其次 `[DisplayName]`，都没有返回 null（调用方退回原属性名）。
        ///
        /// 为什么两个特性都要认：本仓两种惯例并存——实体模型（RecipeParameter / UserInfo / Role）用
        /// `[Description]`，配置模型（DevConfig / PlcConfig / DBConfig）用 `[DisplayName]`。
        /// 与本仓取显示名的既有先例同源：FormUserManager 列头取 `[Description]`，
        /// ConfigFlatModel / ResourceMapper 取 `[DisplayName]`；此处的差别是**两个都认**——
        /// 审计路径要穿过五个域，只认一个就有半个仓的字段翻译不出来。
        /// 注意 `[DisplayName]` 特指 System.ComponentModel 那个（本仓模型用的即是它，
        /// 不是 DataAnnotations 里的同名特性）；日后若引入 DataAnnotations 版，须在此一并认。
        /// </summary>
        internal static string DisplayNameOf(PropertyInfo prop)
        {
            if (prop == null) return null;

            var description = prop.GetCustomAttribute<DescriptionAttribute>();
            if (description != null && !string.IsNullOrWhiteSpace(description.Description))
                return description.Description.Trim();

            var display = prop.GetCustomAttribute<DisplayNameAttribute>();
            if (display != null && !string.IsNullOrWhiteSpace(display.DisplayName))
                return display.DisplayName.Trim();

            return null;
        }

        /// <summary>
        /// 集合属性的元素类型（数组走 GetElementType，泛型集合走单泛型参数，再兜底扫 IEnumerable&lt;T&gt;）。
        ///
        /// Dictionary&lt;K,V&gt; 有两个泛型参数 ⇒ 返回 null：它在 JSON 里是**对象**不是数组，
        /// 本就该走逐键比对，不能被误当数组处理（PlcConfig.ProtocolParams 即此例）。
        /// 取不到类型时返回 null，元素内部翻译退化为原名（不影响比对结果）。
        /// </summary>
        internal static Type ElementTypeOf(Type type)
        {
            if (type == null) return null;
            if (type.IsArray) return type.GetElementType();

            if (type.IsGenericType)
            {
                var args = type.GetGenericArguments();
                return args.Length == 1 ? args[0] : null; // Dictionary<K,V> 是对象，不是数组
            }

            foreach (var face in type.GetInterfaces())
                if (face.IsGenericType && face.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                    return face.GetGenericArguments()[0];

            return null;
        }
    }
}
