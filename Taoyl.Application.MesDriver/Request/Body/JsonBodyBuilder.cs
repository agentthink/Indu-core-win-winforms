using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// JSON：配置打底、流程覆盖（对象合并、数组整体替换），结果压成单行。
    /// 🔴 配置正文写坏直接抛——静默原样发会让错误跑到服务端才暴露。
    /// </summary>
    internal sealed class JsonBodyBuilder : IBodyBuilder
    {
        public string ContentType { get { return "application/json; charset=utf-8"; } }

        public string Build(string configText, object invokeBody)
        {
            JToken configToken = BodyText.ParseConfigToken(configText);
            string invokeText = BodyText.ToText(invokeBody);

            // 没有覆盖层：配置是什么就发什么（压缩后）
            if (invokeText == null)
                return configToken == null ? string.Empty : configToken.ToString(Formatting.None);

            string raw;
            JObject merged = MergeObject(configToken as JObject, invokeText, out raw);

            // 覆盖层不是 JSON 对象：流程优先，按宽容规则发
            if (merged == null)
                return BodyText.Compress(raw);

            return merged.ToString(Formatting.None);
        }

        /// <summary>
        /// 配置对象打底、流程对象覆盖（数组整体替换）。覆盖层不是 JSON 对象时返回 null，
        /// 并通过 raw 带出原样内容。
        /// 🔴 与 FormBodyBuilder.MergeObject 是同一语义的两份副本，改规则必须同时改那处。
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
    }
}
