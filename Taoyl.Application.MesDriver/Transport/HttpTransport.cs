using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using Taoyl.Application.MesDriver.Internal;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Transport
{
    /*
     * ── 本类做不到的事，怎么办 ──────────────────────────────────────────────
     * 本文件是库内唯一的发送出口。正文必须是 string（Render 要显示、重试要复用），
     * 所以 multipart / 流式正文 / 自定义 HttpContent 它做不了。那种情况直接用
     * System.Net.Http.HttpClient——本库不提供也不阻止（完整示例见 README §5.7）。
     *
     * 正文的六种形态，选型就是选这个类：
     *   StringContent             字符串 + 编码/媒体类型    → 纯文本
     *   ByteArrayContent          字节数组                  → 本文件的用法（正文先成型成 string 再编码）
     *   StreamContent             流，不占内存              → 大报文 / 大文件
     *   FormUrlEncodedContent     k=v&…                     → 表单（自己设 Content-Type）
     *   MultipartFormDataContent  多段（文本 + 文件）       → 文件上传
     *   自定义 HttpContent        边算边写                  → 流式产出 / 进度回调
     *
     * 最小骨架（与下面 Send 等价，但没有治理能力）：
     *   using (var handler = new HttpClientHandler { UseProxy = false, AllowAutoRedirect = false })
     *   using (var client  = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
     *   using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
     *   using (var timeout = new CancellationTokenSource(15000))
     *   using (var linked  = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token))
     *   using (var response = client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token)
     *                               .GetAwaiter().GetResult())
     *   {
     *       int code    = (int)response.StatusCode;
     *       string text = …;   // 自己限量读、自己按声明解码
     *   }
     *
     * 四条必须自己补的：
     *   ① UseProxy = false           工控机常配系统代理，会绕路或直接失败
     *   ② AllowAutoRedirect = false  否则 302 会被跟成「200 + 登录页 HTML」，流程判成成功
     *   ③ 超时用双 CTS，不用 client.Timeout —— 后者只管到「拿到响应头」，读 body 阶段不受控
     *   ④ 解码认 BOM —— 带 BOM 的 JSON 不剥掉 JToken.Parse 直接抛；UTF-16 按 charset 解会乱码
     *
     * 走这条路不产生 MesResult、不落日志、不重试——是代价，不是缺陷。
     * ───────────────────────────────────────────────────────────────────────
     */
    /// <summary>
    /// HTTP 发送层。只做两件事：把 request 发出去、把响应读回来（读取在 HttpResponseReader）。
    /// 不管重试、不管配置、不管 body 怎么来、不管结果怎么组装。
    ///
    /// 🔴 用 HttpClient 单例而不是 HttpWebRequest：后者的 Timeout 只覆盖到「拿到响应头」，
    /// 读 body 阶段不受控（服务端发完头不发 body 会永久挂死）。配 linked CTS 才能让超时覆盖
    /// 整个请求生命周期——这是 per-api TimeoutMs 的唯一干净实现。
    /// 🔴 不用 IHttpClientFactory（勿因网文改动）：它治的三个病我们都没有——Socket 耗尽（本来就是
    /// 进程级单例）、DNS 陈旧（IP 直连）、管道集成（重试语义特殊，自己做）。并发能力来自连接池
    /// （DefaultConnectionLimit=64），不属于 HttpClient 实例。
    /// 🔴 绝不碰 DefaultRequestHeaders——各家鉴权方式不同，并发时 A 流程的 Bearer 会串到 B 流程。
    /// 🔴 静态构造推迟到首次 Send，ServicePointManager 那几条设置仍在首个连接之前生效。
    /// </summary>
    internal static class HttpTransport
    {
        /// <summary>
        /// 请求级标记：这条请求跳过 SSL 证书验证。写进 HttpRequestMessage.Properties
        /// （本地字典，不上网、不进报文），由 handler 回调读取。
        /// 🔴 与配置字段方向相反：配置侧 EnableCertificateValidation 描述意图（默认 false = 不验证），
        /// 本常量描述行为（出现即放行）。取反只发生在 RequestTemplate.Build 一处，别再加第二次。
        /// 🔴 只能用字符串键：.NET Framework 的 Properties 没有强类型键。
        /// </summary>
        internal const string SkipCertPropertyKey = "Mes.SkipCert";

        private static readonly HttpClient Client;

        static HttpTransport()
        {
            // 进程级开关，幂等。DefaultConnectionLimit 默认 2，多流程并发时会排队。
            // 用 Math.Max 而不是直接赋值：不覆盖其它模块已经调大的值。
            ServicePointManager.DefaultConnectionLimit = Math.Max(ServicePointManager.DefaultConnectionLimit, 64);

            // 🔴 进程级全局设置（ServicePointManager 影响整个进程的 HTTP 行为）：
            // 当前全进程仅本库使用 HttpClient/HttpWebRequest（已 Grep 核实），无他人受影响。
            // 未来若有第二个 HTTP 模块依赖 100-continue 发大报文，由该模块自行处理冲突。
            ServicePointManager.Expect100Continue = false;
            // 用 |= 不覆盖：4.7.2 没有 Tls13 枚举，写 Tls13 编译不过
            ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

            var handler = new HttpClientHandler
            {
                UseProxy = false,   // 工控机常配系统代理，会绕路或直接失败
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,

                // 🔴 不跟随重定向。本库的调用方是流程专用线程，没有人盯着看：
                // 一旦跟随，MES 会话失效跳登录页的 302 会被吞成「200 + 一段 HTML」，
                // MesResult 记成 HttpStatus=200 / Failure=None，调用方判成成功、流程继续走。
                // HttpResult 只有 StatusCode 与 Body，最终 URI 不暴露——跟了也没有痕迹可查。
                // 关掉之后 302 会作为状态码显式返回；调用方的成功判据是 HttpStatus == 200（见 IMesClient）。
                AllowAutoRedirect = false,

                // 证书验证：按请求级标记判定。接口关掉了验证（配置默认）就带 SkipCertPropertyKey，
                // 这里直接放行；没带标记的按标准校验（errors == SslPolicyErrors.None）。
                // request 为 null 时走标准校验——方向安全，不会误放行。
                ServerCertificateCustomValidationCallback = (request, certificate, chain, errors) =>
                {
                    if (request != null
                        && request.Properties.TryGetValue(SkipCertPropertyKey, out object skip)
                        && skip is bool flagged && flagged)
                        return true;

                    return errors == SslPolicyErrors.None;
                }
            };

            Client = new HttpClient(handler)
            {
                Timeout = Timeout.InfiniteTimeSpan   // 超时由 per-request 双 CTS 控制
            };
        }

        /// <summary>
        /// 发一次。成功（含 4xx/5xx）返回状态码与正文；失败抛 HttpTransportException。
        /// timeoutMs 调用方保证 &gt; 0——MesConfigStore.Normalize 是唯一修正点，这里不再兜底。
        /// </summary>
        public static HttpResult Send(HttpRequestMessage request, int timeoutMs, CancellationToken ct)
        {
            using (var timeout = new CancellationTokenSource(timeoutMs))
            using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token))
            {
                try
                {
                    // ResponseHeadersRead：拿到头就返回，body 流式读——否则 8MB 上限形同虚设
                    using (HttpResponseMessage response = Client
                               .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token)
                               .GetAwaiter()
                               .GetResult())
                    {
                        return new HttpResult
                        {
                            StatusCode = (int)response.StatusCode,
                            Body = HttpResponseReader.Read(response)
                        };
                    }
                }
                catch (OperationCanceledException)
                {
                    // 双 CTS 才能区分：单 CTS 时超时也会让 ct.IsCancellationRequested 为 true
                    if (ct.IsCancellationRequested)
                        throw new HttpTransportException(MesFailureKind.Cancelled, "取消");

                    throw new HttpTransportException(MesFailureKind.Timeout, "超时 " + timeoutMs + "ms");
                }
                catch (HttpRequestException ex)
                {
                    throw new HttpTransportException(MesFailureKind.Network, "连接失败: " + ExceptionText.Innermost(ex));
                }
                catch (IOException ex)
                {
                    // 🔴 ResponseHeadersRead 下「读 body」是独立的网络 IO 阶段：服务端发完响应头再掐连接
                    // （反代回收 / LB 空闲超时 / 进程重启）在这里抛 IOException，不是 HttpRequestException。
                    // 🔴 取消与超时在这一阶段同样表现为 IOException（底层是 Abort() 关 socket，同步流读
                    // 没有 token→OperationCanceledException 的翻译层）。所以必须先判两个 token，
                    // 否则「超时」被误报成「网络错误」，「取消」还会走可重试分支。
                    if (ct.IsCancellationRequested)
                        throw new HttpTransportException(MesFailureKind.Cancelled, "取消");

                    if (timeout.IsCancellationRequested)
                        throw new HttpTransportException(MesFailureKind.Timeout, "超时 " + timeoutMs + "ms");

                    throw new HttpTransportException(MesFailureKind.Network, "读取响应失败: " + ExceptionText.Innermost(ex));
                }
            }
        }
    }
}
