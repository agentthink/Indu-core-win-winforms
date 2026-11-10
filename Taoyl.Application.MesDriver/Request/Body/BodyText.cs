using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// 正文域的文本工具：object → 文本、解析、压缩，服务所有 IBodyBuilder。
    ///
    /// 🔴 解析分两个入口，不可合并：配置侧（ParseConfigToken）坏即抛，流程侧（TryParseObject）
    /// 坏返 null——Invoke 的 body 允许传表单串 / XML。合并成一个"宽容解析"，
    /// 配置写坏就会静默发出去。
    /// </summary>
    internal static class BodyText
    {
        /// <summary>
        /// 流程覆盖层 → 文本。null 原样返回（语义是"没有覆盖层"，与空串区分）；字符串直接用；
        /// 其它类型按 Newtonsoft 序列化。
        /// </summary>
        internal static string ToText(object body)
        {
            if (body == null)
                return null;

            string text = body as string;
            if (text != null)
                return text;

            return JsonConvert.SerializeObject(body, Formatting.None);
        }

        /// <summary>
        /// 配置正文 → JToken。空白返 null（没配正文）；解析失败抛 FormatException。
        /// 静默返回 null 会让 Form 发出空正文、Json 原样发出坏正文——两种都是"看着成功、内容不对"。
        /// 异常经 RequestTemplate.Create 归 ConfigError。
        /// </summary>
        internal static JToken ParseConfigToken(string configText)
        {
            if (string.IsNullOrWhiteSpace(configText))
                return null;

            try
            {
                return JToken.Parse(configText);
            }
            catch (Exception ex)
            {
                throw new FormatException("配置正文不是合法 JSON: " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 流程覆盖层 → JSON 对象。解析不了返 null（调用方据此走"覆盖层不是对象"的分支），
        /// **不抛**——Invoke 的 body 参数允许传已拼好的表单串或 XML。
        /// </summary>
        internal static JObject TryParseObject(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            try
            {
                return JToken.Parse(text) as JObject;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>能解析成 JSON 就压成无空白单行；不能则原样返回（表单串 / XML / 流程传的自由文本）。</summary>
        internal static string Compress(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return text ?? string.Empty;

            try
            {
                return JToken.Parse(text).ToString(Formatting.None);
            }
            catch
            {
                return text;
            }
        }
    }
}
