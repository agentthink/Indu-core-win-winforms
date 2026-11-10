using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Taoyl.Application.MesDriver.Models;
using Taoyl.Application.MesDriver.Request.Body;
using Taoyl.Application.MesDriver.Transport;

namespace Taoyl.Application.MesDriver.Request
{
    /// <summary>
    /// 请求模板：一次装配的成品，可按同一份素材反复造 HttpRequestMessage。
    ///
    /// 🔴 不直接存 HttpRequestMessage：.NET 不允许同一个请求发两次，重试每次都要新实例。
    /// Create 只跑一次，Build 跑 N 次。
    /// 🔴 字段三组：报文成品（Method / Url / Headers / ContentType / Body，Build 与 Render 共用）、
    /// 展示辅助（Query，编码前原值，只给 Render）、传输策略（TimeoutMs / MaxAttempts / BackoffMs）。
    /// 🔴 Build 与 Render 共用同一份成品，且 Render 用 NeedsContent() 判断内容头会不会发出，
    /// 所以「看到的请求」与「实发报文」在我们设置的要素上物理一致。HttpClient 自动追加的
    /// Accept-Encoding / Connection / Content-Length 不由我们决定，不写进 Render。
    /// </summary>
    internal sealed class RequestTemplate
    {
        public MesHttpMethod Method;

        /// <summary>拼好查询参数的最终地址。Render 的 url 段就是它。</summary>
        public string Url;

        /// <summary>最终查询参数（已含 ApiKey 注入项）。只给 Render 看——URL 里是 %XX，这里给原值。</summary>
        public List<MesHeader> Query;

        /// <summary>最终请求头：用户配置 + 鉴权注入，不含 Content-Type。</summary>
        public List<MesHeader> Headers;

        /// <summary>
        /// 最终内容类型。null = 这条不补（None 形态）。
        /// 🔴 单独一位而不是塞进 Headers：它只能挂在 HttpContent.Headers 上，且"发不发"
        /// 取决于有没有内容段——那个判断只有这里知道。
        /// </summary>
        public string ContentType;

        public string Body;
        public int TimeoutMs;
        public int MaxAttempts;
        public int BackoffMs;

        /// <summary>
        /// 是否验证 SSL 服务器证书。由 Create 从配置赋值，全库只有那一个构造点。
        /// 🔴 刻意不写 `= true`：配置默认关，写 true 会造成「配置关、模板开」的矛盾，
        /// 而它永远不会生效，只会让后来的人以为这里有安全兜底。
        /// </summary>
        public bool EnableCertificateValidation;

        /// <summary>
        /// 定义 + 调用覆盖层 → 请求模板。装配的唯一入口，异常由 MesClient.Execute 接住归 ConfigError。
        /// </summary>
        internal static RequestTemplate Create(MesApiDefinition definition, MesCallOptions options)
        {
            // 形态表只查一次：ContentType 与正文出自同一个 builder，不会两处走偏
            IBodyBuilder bodyBuilder = RequestBody.For(definition.BodyType);

            // 覆盖层在这里消化掉：下游两域收到的是「合并后的列表」，不知道覆盖层存在。
            // 🔴 两个比较器不同，别统一：头名大小写不敏感（RFC 7230），参数名大小写敏感。
            List<MesHeader> mergedHeaders = ApplyOverrides(definition.Headers, options?.Headers, StringComparer.OrdinalIgnoreCase);
            List<MesHeader> mergedQuery = ApplyOverrides(definition.QueryParams, options?.QueryParams, StringComparer.Ordinal);

            // 鉴权先算出来，头与参数才能各自决定放不放它。覆盖层的 Auth 整体替换配置的
            AuthInjection auth = AuthInjection.From(options?.Auth ?? definition.Auth);

            string configContentType;
            List<MesHeader> headers = RequestHeaders.Build(mergedHeaders, auth, out configContentType);

            // 参数先齐（含鉴权参数）再拼地址
            List<MesHeader> query = RequestUrl.CollectQuery(mergedQuery, auth);

            return new RequestTemplate
            {
                Method = definition.Method,
                Url = RequestUrl.Build(definition.Url, query),
                Query = query,
                Headers = headers,

                // 请求头页显式配的赢；没配才用形态的默认值（None 为 null，不补）
                ContentType = configContentType ?? bodyBuilder.ContentType,

                Body = bodyBuilder.Build(definition.Body ?? string.Empty, options?.Body),

                TimeoutMs = definition.TimeoutMs,
                MaxAttempts = definition.Retry?.MaxAttempts ?? MesDefaults.MaxAttempts,
                BackoffMs = definition.Retry?.BackoffMs ?? MesDefaults.BackoffMs,
                EnableCertificateValidation = definition.EnableCertificateValidation
            };
        }

        /// <summary>
        /// 配置列表 + 调用覆盖项 → 合并后的新列表。同名替换整条、新名追加，不改任何输入。
        /// 🔴 nameComparer 必须按域传：头名大小写不敏感（RFC 7230），查询参数名大小写敏感。
        /// 统一成一个比较器必出错——统一 Ordinal 会发出重复的 Content-Type；
        /// 统一 IgnoreCase 会把 barcode / BarCode 合成一条，且名字变成覆盖项的写法，
        /// 服务端按配置里那个写法就取不到了（配置的值被静默顶掉）。
        /// </summary>
        private static List<MesHeader> ApplyOverrides(List<MesHeader> config, List<MesHeader> overrides, StringComparer nameComparer)
        {
            var merged = new List<MesHeader>();
            if (config != null)
            {
                foreach (MesHeader item in config)
                {
                    if (item != null)
                        merged.Add(item);
                }
            }

            if (overrides == null)
                return merged;

            foreach (MesHeader item in overrides)
            {
                if (item == null)
                    continue;

                // 与配置侧规则刻意相反（配置侧空名静默过滤）：覆盖项是代码写的，
                // 空名 100% 是写错，静默丢的后果是「以为加了这个头、实际没发」，没有任何地方看得出来。
                if (string.IsNullOrWhiteSpace(item.Name))
                    throw new FormatException("调用覆盖项缺少 Name");

                int index = merged.FindIndex(existing => nameComparer.Equals(existing.Name, item.Name));
                if (index >= 0)
                    merged[index] = item;   // 同名替换整条，Name 用覆盖项的写法
                else
                    merged.Add(item);
            }

            return merged;
        }

        /// <summary>枚举 → 报文里的方法名。Build 与 Render 共用，保证两处同一个值。</summary>
        private string MethodName
        {
            get { return Method.ToString().ToUpperInvariant(); }
        }

        /// <summary>按模板造一个新的 HttpRequestMessage。每次重试都要调一次。</summary>
        internal HttpRequestMessage Build()
        {
            var request = new HttpRequestMessage(new HttpMethod(MethodName), Url);

            // 证书策略是请求级的：handler 回调从这个本地字典读标记，不上网、不进报文
            if (!EnableCertificateValidation)
                request.Properties[HttpTransport.SkipCertPropertyKey] = true;

            foreach (MesHeader header in Headers)
            {
                // 🔴 返回值不能丢：非法头名（含空格 / 非 ASCII）是返回 false 而不是抛。
                // 不检查就等于静默丢头——Render 里有、实发没有，最难查的一类偏差。
                if (!request.Headers.TryAddWithoutValidation(header.Name, header.Value))
                    throw new FormatException("请求头设置失败: " + header.Name + "（头名非法，或属于内容头 / 框架保留头）");
            }

            if (NeedsContent())
            {
                var content = new ByteArrayContent(Encoding.UTF8.GetBytes(Body ?? string.Empty));

                // 没有 Content-Type 就不加——空 POST 实发「Content-Length: 0、无 Content-Type」，
                // 与 Postman 选 none 的报文一致
                if (!string.IsNullOrEmpty(ContentType))
                    content.Headers.TryAddWithoutValidation("Content-Type", ContentType);

                request.Content = content;
            }

            return request;
        }

        /// <summary>
        /// 渲染成六段结构化 JSON：method / url（最终地址）/ query（编码前原值）/ headers /
        /// contentType（null = 这条不发）/ body。不是原始报文——body 会被转义，不能整段导进 Postman。
        /// 🔴 整段包 try：渲染只是给人看，任何失败都不许把已装配好的信息变成空串。
        /// </summary>
        internal string Render()
        {
            try
            {
                var root = new JObject
                {
                    ["method"] = MethodName,
                    ["url"] = Url ?? string.Empty,
                    ["query"] = HeaderObject(Query),
                    ["headers"] = HeaderObject(Headers),

                    // 与 Build 共用同一份判断，不会两处走偏
                    ["contentType"] = NeedsContent() && !string.IsNullOrEmpty(ContentType)
                        ? new JValue(ContentType)
                        : JValue.CreateNull(),

                    ["body"] = Body ?? string.Empty
                };

                return root.ToString(Formatting.Indented);
            }
            catch (Exception ex)
            {
                return "method: " + MethodName + Environment.NewLine
                     + "url: " + (Url ?? string.Empty) + Environment.NewLine
                     + "（请求信息渲染失败: " + ex.Message + "）";
            }
        }

        /// <summary>
        /// 名值对列表 → JSON 对象，空项跳过。
        /// 🔴 重名降级成数组：头与参数都允许重名（?tag=a&amp;tag=b），不能因对象形态吞掉第二条
        /// ——那会让 Render 显示的少于实发的。
        /// </summary>
        private static JObject HeaderObject(List<MesHeader> source)
        {
            var obj = new JObject();
            if (source == null)
                return obj;

            foreach (MesHeader item in source)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.Name))
                    continue;

                string value = item.Value ?? string.Empty;
                JToken existing = obj[item.Name];

                if (existing == null)
                    obj[item.Name] = value;
                else if (existing is JArray array)
                    array.Add(value);
                else
                    obj[item.Name] = new JArray(existing, value);
            }

            return obj;
        }

        /// <summary>
        /// 要不要建内容段（不是"有没有正文"）：有正文，或方法是 POST / PUT / PATCH——
        /// 后者即使正文为空也要带 Content-Length: 0，部分服务端 / WAF 会拒绝裸 POST。
        /// </summary>
        private bool NeedsContent()
        {
            if (!string.IsNullOrEmpty(Body))
                return true;

            return Method == MesHttpMethod.Post
                || Method == MesHttpMethod.Put
                || Method == MesHttpMethod.Patch;
        }
    }
}
