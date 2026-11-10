using System;
using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// 表单：配置是 MesHeader 数组文本 → JObject（丢描述）→ 流程覆盖 → k=v&amp;…。
    /// 覆盖层不是 JSON 对象（已拼好的 form 串）时原样发。
    /// 🔴 界面侧有对称实现（Taoyl.UI.Pages 的 MesApiEditModel.ParseFormBodyItems），
    /// **键值语义必须一致**（跳过空名、Value 缺失当空串）——改一处必须改另一处，编译期查不出。
    /// 两侧差异是刻意的：界面每项多存一个 Description（只给按键说明用，一个字节都不进报文，
    /// 本类不读它）；错误处理界面跳过非对象项（让人看见问题），本类抛（不发垃圾）。
    /// </summary>
    internal sealed class FormBodyBuilder : IBodyBuilder
    {
        public string ContentType { get { return "application/x-www-form-urlencoded"; } }

        public string Build(string configText, object invokeBody)
        {
            JObject configObject = ParseConfigArray(configText);
            string invokeText = BodyText.ToText(invokeBody);

            // 没有覆盖层：配置是什么就发什么
            if (invokeText == null)
                return configObject == null ? string.Empty : ToFormQuery(configObject);

            string raw;
            JObject merged = MergeObject(configObject, invokeText, out raw);

            // 覆盖层不是 JSON 对象：原样发（注意与 JsonBodyBuilder 的兜底不同——那边会压缩）
            if (merged == null)
                return raw ?? string.Empty;

            return ToFormQuery(merged);
        }

        /// <summary>
        /// 配置数组文本 → JObject，只取 Name / Value（Description 在此丢弃，绝不进请求）。
        /// 兼容旧格式（Body 直接是 {"k":"v"} 对象）。
        /// 🔴 非对象项抛（不跳过）：宁可本次调用失败，也不静默发出空正文。界面侧相反——它跳过，
        /// 因为崩溃在装载路径上更糟。
        /// </summary>
        private static JObject ParseConfigArray(string text)
        {
            JToken token = BodyText.ParseConfigToken(text);
            if (token == null)
                return null;

            var array = token as JArray;
            if (array == null)
                return token as JObject;   // 兼容旧配置

            var obj = new JObject();
            foreach (JToken item in array)
            {
                var itemObject = item as JObject;
                if (itemObject == null)
                    throw new FormatException("表单正文里有非对象项: " + item.ToString(Formatting.None));

                string name = (string)itemObject["Name"];
                if (string.IsNullOrWhiteSpace(name))
                    continue;

                obj[name] = (string)itemObject["Value"] ?? string.Empty;
            }
            return obj;
        }

        /// <summary>
        /// 配置对象打底、流程对象覆盖（数组整体替换）。覆盖层不是 JSON 对象时返回 null，
        /// 并通过 raw 带出原样内容。
        /// 🔴 与 JsonBodyBuilder.MergeObject 是同一语义的两份副本，改规则必须同时改那处。
        /// </summary>
        private static JObject MergeObject(JObject configObject, string invokeText, out string raw)
        {
            raw = invokeText;

            JObject invokeObject = BodyText.TryParseObject(invokeText);
            if (invokeObject == null)
                return null;

            if (configObject == null)
                return invokeObject;

            configObject.Merge(invokeObject, new JsonMergeSettings
            {
                // 数组整体替换：流程传的 wip_list 就是要发的完整列表，不与配置里的拼接
                MergeArrayHandling = MergeArrayHandling.Replace
            });

            return configObject;
        }

        /// <summary>
        /// JObject → application/x-www-form-urlencoded。
        /// 🔴 用 WebUtility.UrlEncode（空格变 +，符合 form 规范），
        /// 不用 Uri.EscapeDataString（空格变 %20，那是 URL path/query 的编码）。
        /// </summary>
        private static string ToFormQuery(JObject obj)
        {
            var parts = new List<string>();
            foreach (JProperty property in obj.Properties())
            {
                string value;
                if (property.Value == null || property.Value.Type == JTokenType.Null)
                    value = string.Empty;
                else if (property.Value.Type == JTokenType.String)
                    value = (string)property.Value;
                else
                    value = property.Value.ToString(Formatting.None);

                parts.Add(Encode(property.Name) + "=" + Encode(value));
            }

            return string.Join("&", parts);
        }

        private static string Encode(string value)
        {
            return WebUtility.UrlEncode(value ?? string.Empty) ?? string.Empty;
        }
    }
}
