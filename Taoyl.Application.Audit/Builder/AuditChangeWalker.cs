using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models;

namespace Taoyl.Application.Audit.Builder
{
    /// <summary>
    /// 递归比对引擎 —— 把"前后两棵 JToken 树"走成"字段级明细列表"。
    ///
    /// 三种节点各有各的走法，判据全部收在本类：
    /// - **对象**：取两侧属性名并集逐键递归（<see cref="WalkObject"/>）；
    /// - **数组**：按业务 Key 锚定配对（<see cref="WalkArray"/>，锚点规则见 <see cref="AuditArrayAnchor"/>）；
    /// - **标量**：直接比（判据 = JToken.DeepEquals，它认为 null ≠ ""，安全侧——宁可多记一条，不可漏判一次真实变更）。
    ///
    /// 🔴 **全程携带两套路径 + 当前 CLR 类型**：
    /// - <c>rawPath</c>：原名路径，只用于两处判定——顶层跳过名单、敏感字段脱敏（两处都必须看原名）；
    /// - <c>displayPath</c>：显示名路径，落 FieldPath（给人读的那一份）；
    /// - <c>type</c>：本节点对应的 CLR 类型，供下一段查属性取显示名；为 null 时翻译自然退化为原名。
    ///
    /// 本类不持有状态、不缓存 —— 缓存与翻译全在 <see cref="AuditPropertyMap"/>，路径拼装全在 <see cref="AuditFieldPath"/>。
    /// </summary>
    internal static class AuditChangeWalker
    {
        /// <summary>
        /// 递归比对入口：先把"一边缺席"消化掉，再按新值侧类型分派
        /// （对象逐属性、数组按业务 Key 锚定、标量直接比）。
        /// </summary>
        internal static void Walk(JToken before, JToken after, string rawPath, string displayPath,
            Type type, List<AuditFieldChange> result)
        {
            bool beforeAbsent = AuditValueFormatter.IsAbsent(before);
            bool afterAbsent = AuditValueFormatter.IsAbsent(after);

            if (beforeAbsent && afterAbsent) return;
            if (beforeAbsent) { RecordAdd(after, rawPath, displayPath, type, result); return; }
            if (afterAbsent) { RecordDelete(before, rawPath, displayPath, type, result); return; }

            switch (after.Type)
            {
                case JTokenType.Object:
                    // 类型换头（对象 ↔ 标量）没法逐属性展开，整条记修改
                    if (before.Type != JTokenType.Object) { RecordUpdate(before, after, rawPath, displayPath, result); return; }
                    WalkObject((JObject)before, (JObject)after, rawPath, displayPath, type, result);
                    return;

                case JTokenType.Array:
                    if (before.Type != JTokenType.Array) { RecordUpdate(before, after, rawPath, displayPath, result); return; }
                    WalkArray((JArray)before, (JArray)after, rawPath, displayPath,
                              AuditPropertyMap.ElementTypeOf(type), result);
                    return;

                default:
                    // 判据与 PlcDebugViewModel.ConfigsEqual 同源（JToken.DeepEquals）：
                    // 它认为 null ≠ ""，安全侧——宁可多记一条，不可漏判一次真实变更
                    if (!JToken.DeepEquals(before, after)) RecordUpdate(before, after, rawPath, displayPath, result);
                    return;
            }
        }

        /// <summary>
        /// 对象逐属性比对：取两侧属性名并集——只在旧值侧出现的走删除，只在新值侧出现的走新增。
        /// 用 SortedSet 而非 HashSet：明细顺序必须可复现（同一变更记两次，行序不能变），
        /// 现场比对两份审计记录时行序漂移会被误读成"改动不同"。
        /// 排序键用**原名**（英文，稳定）；展示路径在这一层换成显示名。
        /// </summary>
        private static void WalkObject(JObject before, JObject after, string rawPath, string displayPath,
            Type type, List<AuditFieldChange> result)
        {
            var names = new SortedSet<string>(StringComparer.Ordinal);
            foreach (var p in before.Properties()) names.Add(p.Name);
            foreach (var p in after.Properties()) names.Add(p.Name);

            foreach (var name in names)
            {
                // 字典（运行时键，如 PlcConfig.ProtocolParams）在类型上查不到属性 ⇒ 显示名退回键名，
                // 这正是期望行为：那是协议参数名，本就是给现场看的英文标识，不该被"翻译"成别的
                var prop = AuditPropertyMap.ResolveProperty(type, name);

                Walk(before[name], after[name],
                     AuditFieldPath.Join(rawPath, name),
                     AuditFieldPath.Join(displayPath, AuditPropertyMap.DisplayNameOf(prop) ?? name),
                     prop != null ? prop.PropertyType : null,
                     result);
            }
        }

        /// <summary>
        /// 数组比对：**按业务 Key 锚定**配对（顺序无关、增删不串位）。
        ///
        /// 三种走法：
        /// 1. 元素都能取到业务 Key → 按 Key 配对：同 Key 递归比字段（路径 参数[前称重].最小值）；
        ///    仅一侧存在的 Key = 整条新增/删除（**一条**装下整个元素，不逐字段展开）；
        /// 2. 元素取不到 Key（纯值列表）→ 整条记一条，**绝不按下标展开**；
        /// 3. 两侧一致 → 一条不记。
        ///
        /// 为什么废弃按下标比对：下标配对在列表增删一项时会全线错位，一次改动被放大成几十条假明细
        /// （历史故障：配方改一个下限，明细刷出 42 条"删除"——模板预置项与真实项错位所致）。
        /// </summary>
        /// <param name="elementType">元素 CLR 类型（供元素内部逐字段翻译；取不到则为 null）</param>
        private static void WalkArray(JArray before, JArray after, string rawPath, string displayPath,
            Type elementType, List<AuditFieldChange> result)
        {
            var beforeKeys = ExtractKeys(before);
            var afterKeys = ExtractKeys(after);

            // 空数组视为"可锚定"：空 ↔ 非空 时所有元素自然走整条新增/删除（正是期望的粒度）
            bool anchorable = beforeKeys.TrueForAll(k => k != null) && afterKeys.TrueForAll(k => k != null);

            if (!anchorable)
            {
                if (!JToken.DeepEquals(before, after))
                    RecordUpdate(before, after, rawPath, displayPath, result);
                return;
            }

            var beforeMap = PairByKey(before, beforeKeys);
            var afterMap = PairByKey(after, afterKeys);

            var keys = new SortedSet<string>(beforeMap.Keys, StringComparer.Ordinal);
            foreach (var key in afterMap.Keys) keys.Add(key);

            foreach (var key in keys)
            {
                JToken oldToken, newToken;
                bool hasOld = beforeMap.TryGetValue(key, out oldToken);
                bool hasNew = afterMap.TryGetValue(key, out newToken);

                // 配对段走锚点（稳定，改名不受影响、增删不错位），展示段走人性化名（给人读）
                string rawSegment = AuditFieldPath.Bracket(key);
                string displaySegment = AuditFieldPath.Bracket(
                    AuditArrayAnchor.ElementLabelOf(hasNew ? newToken : oldToken, key));

                if (hasOld && hasNew)
                {
                    Walk(oldToken, newToken,
                         AuditFieldPath.Append(rawPath, rawSegment),
                         AuditFieldPath.Append(displayPath, displaySegment),
                         elementType, result);
                    continue;
                }

                if (hasNew)
                    AppendChange(AuditFieldPath.Append(rawPath, rawSegment),
                                 AuditFieldPath.Append(displayPath, displaySegment),
                                 AuditChangeType.Add, null, newToken, result);
                else
                    AppendChange(AuditFieldPath.Append(rawPath, rawSegment),
                                 AuditFieldPath.Append(displayPath, displaySegment),
                                 AuditChangeType.Delete, oldToken, null, result);
            }
        }

        /// <summary>逐元素取业务 Key（取不到的位置留 null，用于判定整数组是否可锚定）。</summary>
        private static List<string> ExtractKeys(JArray array)
        {
            var keys = new List<string>(array.Count);
            for (int i = 0; i < array.Count; i++) keys.Add(AuditArrayAnchor.ElementKey(array[i]));
            return keys;
        }

        /// <summary>Key → 元素（Key 重复时后者胜：业务上不该出现，此处不放大成异常）。</summary>
        private static Dictionary<string, JToken> PairByKey(JArray array, List<string> keys)
        {
            var map = new Dictionary<string, JToken>(StringComparer.Ordinal);
            for (int i = 0; i < array.Count; i++)
                if (keys[i] != null) map[keys[i]] = array[i];
            return map;
        }

        /// <summary>新增：对象/数组展开到叶子，标量落一条（旧值留空）。</summary>
        private static void RecordAdd(JToken after, string rawPath, string displayPath, Type type,
            List<AuditFieldChange> result)
        {
            switch (after.Type)
            {
                case JTokenType.Object:
                    foreach (var p in ((JObject)after).Properties())
                    {
                        var prop = AuditPropertyMap.ResolveProperty(type, p.Name);
                        RecordAdd(p.Value,
                                  AuditFieldPath.Join(rawPath, p.Name),
                                  AuditFieldPath.Join(displayPath, AuditPropertyMap.DisplayNameOf(prop) ?? p.Name),
                                  prop != null ? prop.PropertyType : null,
                                  result);
                    }
                    return;

                case JTokenType.Array:
                    var addedArray = (JArray)after;
                    Type addedElementType = AuditPropertyMap.ElementTypeOf(type);
                    for (int i = 0; i < addedArray.Count; i++)
                    {
                        string segment = AuditFieldPath.ElementSegment(addedArray[i], i);
                        RecordAdd(addedArray[i],
                                  AuditFieldPath.Append(rawPath, segment),
                                  AuditFieldPath.Append(displayPath, segment),
                                  addedElementType, result);
                    }
                    return;

                default:
                    AppendChange(rawPath, displayPath, AuditChangeType.Add, null, after, result);
                    return;
            }
        }

        /// <summary>删除：对象/数组展开到叶子，标量落一条（新值留空）。</summary>
        private static void RecordDelete(JToken before, string rawPath, string displayPath, Type type,
            List<AuditFieldChange> result)
        {
            switch (before.Type)
            {
                case JTokenType.Object:
                    foreach (var p in ((JObject)before).Properties())
                    {
                        var prop = AuditPropertyMap.ResolveProperty(type, p.Name);
                        RecordDelete(p.Value,
                                     AuditFieldPath.Join(rawPath, p.Name),
                                     AuditFieldPath.Join(displayPath, AuditPropertyMap.DisplayNameOf(prop) ?? p.Name),
                                     prop != null ? prop.PropertyType : null,
                                     result);
                    }
                    return;

                case JTokenType.Array:
                    var removedArray = (JArray)before;
                    Type removedElementType = AuditPropertyMap.ElementTypeOf(type);
                    for (int i = 0; i < removedArray.Count; i++)
                    {
                        string segment = AuditFieldPath.ElementSegment(removedArray[i], i);
                        RecordDelete(removedArray[i],
                                     AuditFieldPath.Append(rawPath, segment),
                                     AuditFieldPath.Append(displayPath, segment),
                                     removedElementType, result);
                    }
                    return;

                default:
                    AppendChange(rawPath, displayPath, AuditChangeType.Delete, before, null, result);
                    return;
            }
        }

        private static void RecordUpdate(JToken before, JToken after, string rawPath, string displayPath,
            List<AuditFieldChange> result)
        {
            AppendChange(rawPath, displayPath, AuditChangeType.Update, before, after, result);
        }

        // 🔴 红线：本类的静态方法**禁止**出现在 SqlSugar 表达式树里
        // （此处与 SqlSugar 无关，是同一条项目纪律的延续：表达式树里只允许成员访问与闭包变量）

        /// <summary>
        /// 落一条明细。敏感字段在此统一脱敏——判据收在构建器里，
        /// 而不是让五个调用方各自记得"这个字段别记值"（那种纪律保证迟早破）。
        /// </summary>
        /// <param name="rawPath">原名路径，只供脱敏判定（显示名可能已把 Password 翻成"密码"）</param>
        /// <param name="displayPath">显示名路径，落 FieldPath 给人读</param>
        private static void AppendChange(string rawPath, string displayPath, AuditChangeType changeType,
            JToken oldValue, JToken newValue, List<AuditFieldChange> result)
        {
            bool masked = AuditValueFormatter.IsSensitive(rawPath);

            result.Add(new AuditFieldChange
            {
                FieldPath = string.IsNullOrEmpty(displayPath) ? AuditFieldPath.RootPath : displayPath,
                ChangeType = changeType,
                OldValue = masked ? null : AuditValueFormatter.Format(oldValue),
                NewValue = masked ? null : AuditValueFormatter.Format(newValue)
            });
        }
    }
}
