using System;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Transport
{
    /// <summary>
    /// 发送失败。成功走返回值，失败走异常——不再造「半个成功」的结果对象。
    ///
    /// 🔴 异常只在 HttpTransport → MesClient 两层之间流转，不会穿透到流程（Invoke 永不抛）：
    /// 它的作用是把"底层发生了什么"翻译成 MesFailureKind，重试判据在 MesClient.ShouldRetry。
    /// 🔴 Message 就是 MesResult.Response 里给人看的文案（TryOnce 直接塞进 Body）——
    /// 改文案等于改现场看到的排查信息，属契约而非日志。
    /// </summary>
    internal sealed class HttpTransportException : Exception
    {
        public MesFailureKind Kind { get; }

        public HttpTransportException(MesFailureKind kind, string message) : base(message)
        {
            Kind = kind;
        }
    }
}
