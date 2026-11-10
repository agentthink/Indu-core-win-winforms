using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>HTTP 方法。用枚举而非字符串——避免手填 "POTS" 这类拼错，也不必到处做大小写归一。</summary>
    public enum MesHttpMethod
    {
        Get = 0,
        Post = 1,
        Put = 2,
        Delete = 3,
        Patch = 4
    }

    /// <summary>
    /// 报文形态（对齐 Postman 正文分组：无 / 表单 / 原始）。决定「Body 里存什么」和「发送前怎么处理」：
    /// None —— 无正文：不补 Content-Type，空 POST 发 Content-Length: 0；
    /// Json —— Body 是 JSON 文本，发送前压缩，与流程 body 做对象合并（配置打底、流程覆盖）；
    /// Form —— Body 是 MesHeader 数组文本（[{"Name":"k","Value":"v","Description":"…"}]），发送前转 k=v&amp;…；
    /// Text —— Body 是任意文本，原样发（流程传了就整个替换，不合并）；
    /// Xml  —— Body 是 XML 文本（SOAP 信封 / 裸 XML），合并规则同 Text，差别只有 Content-Type。
    ///
    /// 🔴 数值固化勿动：旧 Mes.json 按名字反序列化，Json=0 / Form=1 / Text=2 的兼容依赖这三个值不变。
    ///    Xml 追加在尾部取 4（3 已被 None 占用），同样不改动既有值。
    ///
    /// 🔴 新增一种形态要动 8 处，漏一处不会报错：
    ///    ① 本枚举；② 新建一个 XxxBodyBuilder 实现 + RequestBody 登记表加一行；③ MesBodyTypeUi.Table；
    ///    ④ MesApiEditModel 加一个编辑器字段；⑤ 同文件 ctor 的 switch；⑥ 同文件 ToDefinition 的 switch；
    ///    ⑦ FormMesDebug.Designer（radio + panel + textbox + tblBodyType 列宽）；
    ///    ⑧ FormMesDebug.bodyTypeMap 加一行——绑定、DPI 补偿、面板显隐都从这张表派生。
    /// </summary>
    public enum MesBodyType
    {
        Json = 0,
        Form = 1,
        Text = 2,

        /// <summary>无正文（新建接口的默认形态）。</summary>
        None = 3,

        /// <summary>XML 正文（SOAP 信封 / 裸 XML）。Content-Type 由形态表登记为 text/xml。</summary>
        Xml = 4
    }

    /// <summary>
    /// 单个 MES 接口定义。12 字段。
    /// 不含 BaseUrl / Environments：地址就是完整 URL，换地址用文本编辑器全局替换。
    /// 不含 Enabled：要停用就删掉这条配置——「禁用」与「不存在」行为完全一样。
    /// 不含 Encoding：四家文档的请求/返回格式均声明 charset=UTF-8，不需要配置项。
    /// 不含 RetryOn / BackoffMode：见 MesRetryPolicy 的说明。
    /// </summary>
    public class MesApiDefinition
    {
        /// <summary>
        /// 接口标识。调用方 Invoke 用的键。
        /// 🔴 用英文标识符而非中文名——中文名会被当作文案改，改一次就断调用。
        /// </summary>
        public string Name { get; set; }

        /// <summary>界面显示名，仅用于调试界面列表，不参与调用。</summary>
        public string DisplayName { get; set; }

        /// <summary>HTTP 方法。默认 POST。</summary>
        public MesHttpMethod Method { get; set; } = MesHttpMethod.Post;

        /// <summary>完整 URL，含协议与端口，不拆 BaseUrl。</summary>
        public string Url { get; set; }

        /// <summary>URL 查询参数，发送前逐项 URL 编码。</summary>
        public List<MesHeader> QueryParams { get; set; } = new List<MesHeader>();

        /// <summary>请求头。Content-Type 也走这里；未配则由 BodyType 推导默认值。</summary>
        public List<MesHeader> Headers { get; set; } = new List<MesHeader>();

        /// <summary>鉴权配置。配了类型后，本处注入的 Authorization 会覆盖 Headers 里的同名项。</summary>
        public MesAuthConfig Auth { get; set; } = new MesAuthConfig();

        /// <summary>
        /// 是否验证 SSL 服务器证书。默认 false（不验证），与 Postman 的默认一致——
        /// 现场 MES 多为内网自建、自签证书，默认开会卡在「无法建立 SSL/TLS 的安全通道」，
        /// 而那句报错不指向解法。
        ///
        /// 🔴 关闭不等于不加密：连接仍是 TLS 加密，只是不再验证「对方证书是否由可信 CA 签发」。
        /// 🔴 只作用于本接口：标记走 HttpRequestMessage.Properties（本地字典，不进报文）。
        /// 🔴 走公网 / 跨网段 / 有合规要求时务必打开。打开后是「按系统信任库校验」，
        ///    不是「只信任某个特定证书」——后者要加载证书文件，是另一个量级。
        /// </summary>
        public bool EnableCertificateValidation { get; set; }

        /// <summary>
        /// 报文底稿。形态由 BodyType 决定。
        /// 生产路径参与发送：作为底稿，由 Invoke 的 body 参数覆盖后发出。
        /// 调试路径：保存后点发送，Invoke(name) 不传 body，发出来的就是这份内容。
        /// 🔴 流程必填的字段在此留空，不要留调试时的假数据，
        /// 否则流程漏传会把假数据静默发给 MES。框架不做必填校验。
        /// 🔴 代码里手动创建定义时注意：BodyType 默认 Json，发 XML 等非 JSON 正文必须显式设置，
        ///    否则会在装配阶段报「配置正文不是合法 JSON」。
        /// </summary>
        public string Body { get; set; }

        /// <summary>报文形态。默认 JSON。</summary>
        public MesBodyType BodyType { get; set; } = MesBodyType.Json;

        /// <summary>单次请求超时（毫秒）。默认值见 MesDefaults。</summary>
        public int TimeoutMs { get; set; } = MesDefaults.TimeoutMs;

        /// <summary>
        /// 重试策略。驱动**不设总预算**，超时与总时长控制权在调用方（Invoke 收 CancellationToken）——
        /// 配置方需要自己算清最坏耗时：TimeoutMs × MaxAttempts + BackoffMs × 1.2 × (MaxAttempts − 1)，
        /// 其中单次退避会先被夹到 30s 上限（实际最长 36s）。
        /// 例：15000 / 3 / 500 → 45s + 1.2s ≈ 46s；15000 / 3 / 30000 → 45s + 72s ≈ 117s。
        /// </summary>
        public MesRetryPolicy Retry { get; set; } = new MesRetryPolicy();

        /// <summary>
        /// 深拷贝。调试界面编辑态用它隔离草稿与快照，保存时用它构造全新对象图。
        ///
        /// 🔴 用序列化往返而非手写逐字段赋值：手写清单与字段列表是两份知识，
        /// 加字段时忘了补本方法，Save 就会把该字段静默写回默认值。
        /// 副作用：今后若给某字段加 [JsonIgnore]，本方法也会跳过它（当前模型纯配置、无运行态字段）。
        /// </summary>
        public MesApiDefinition Clone()
        {
            return JsonConvert.DeserializeObject<MesApiDefinition>(
                JsonConvert.SerializeObject(this, CloneSettings));
        }

        /// <summary>
        /// null 字段不写出：否则 JSON 里显式的 null 会在反序列化时把
        /// Headers / QueryParams / Auth / Retry 覆盖成 null。
        /// </summary>
        private static readonly JsonSerializerSettings CloneSettings = new JsonSerializerSettings
        {
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>接口名比较规则：标识符，区分大小写。</summary>
        public static bool NameEquals(string a, string b)
        {
            return string.Equals(a, b, StringComparison.Ordinal);
        }
    }
}
