namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// XML 正文（SOAP 信封 / 裸 XML）：与 Text 一样原样发、不合并，差别只在 Content-Type。
    ///
    /// 🔴 独立成形态而不是复用 Text：按 Text 发会带上 text/plain，严格的服务端直接 415、
    /// 宽容的能通——排障全看人品。SOAP 1.2 厂商的 application/soap+xml 在请求头页手配覆盖。
    /// </summary>
    internal sealed class XmlBodyBuilder : IBodyBuilder
    {
        public string ContentType { get { return "text/xml; charset=utf-8"; } }

        public string Build(string configText, object invokeBody)
        {
            return BodyText.ToText(invokeBody) ?? configText;
        }
    }
}
