namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// 无正文。空 POST 的 Content-Length: 0 由 RequestTemplate.NeedsContent 处理，
    /// 与正文无关——这里返回空串即可。
    /// </summary>
    internal sealed class NoneBodyBuilder : IBodyBuilder
    {
        /// <summary>不补 Content-Type：实发报文里就没有这个头。</summary>
        public string ContentType { get { return null; } }

        public string Build(string configText, object invokeBody)
        {
            return string.Empty;
        }
    }
}
