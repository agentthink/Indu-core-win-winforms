namespace Taoyl.Application.MesDriver.Transport
{
    /// <summary>发送成功（含 4xx / 5xx——那算拿到响应，不算失败）。</summary>
    internal struct HttpResult
    {
        public int StatusCode;
        public string Body;
    }
}
