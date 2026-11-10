# Taoyl.Application.MesDriver · MES 调用驱动

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

> 配置驱动的 MES 接口调用层。加一条接口 = 改 `Mes.json`，不写代码。

---

## 1. 定位与边界

**是什么**：把「调一个 MES 接口」压缩成一次 `Invoke(接口名, 覆盖层)`。方法、地址、参数、请求头、鉴权、正文形态与重试策略全部来自配置，驱动只负责按形态装配、按声明发送、按分类回报。

**不是什么**：

| 不是 | 原因 |
|---|---|
| WSDL 导入 / 代码生成器 | 那是开发期一次性动作，SoapUI 已做到极致；运行时只需一条 HTTP 请求 |
| 业务结果解析器 | 业务码（`200` + 正文里的 `Result`/`code`）由调用方判断，驱动不猜（见 §8） |
| 协议栈 / 传输抽象层 | SOAP 就是 HTTP POST + XML 信封，HTTP 与 WebService 共享 90%，没有第二条协议要接 |
| 重试策略框架 | 只固化「超时 / 网络 / 5xx 重试，取消与配置错误不重试」，不按错误类型二次筛选 |

**一句话**：它解决的是「19 条接口怎么不写 19 份代码」，不是「怎么调 MES」。

---

## 2. 依赖说明

| 依赖 | 来源 | 用途 |
|---|---|---|
| `Newtonsoft.Json` | NuGet | 配置序列化、JSON 正文合并与压缩 |
| `System.Net.Http` | .NET Framework 自带 | `HttpClient` / `HttpRequestMessage` |

🔴 **零 ProjectReference**：不引 `Taoyl.Application.Share`、不引日志库、不引审计库。日志通过 `IMesLogSink` 端口由组合根外挂，结构上不产生依赖环。

---

## 3. 功能与 API

### 能力域

| 域 | 内容 |
|---|---|
| 装配 | 参数 / 地址 / 鉴权 / 请求头 / 正文 / 内容类型，六件事各有一个类 |
| 发送 | 进程级 `HttpClient` 单例，per-api 超时 + 双 CTS，不跟随重定向，8MB 限量读 |
| 重试 | 固定间隔 ±20% 抖动，基准夹到 30s；`MaxAttempts` 默认 1（不重试） |
| 配置 | 字节 ↔ 模型、原子写入 + 备份回退、落盘成功才热替换快照 |

### 接口签名

```csharp
// Taoyl.Application.MesDriver.Interfaces
MesResult Invoke(string apiName, MesCallOptions options = null, CancellationToken ct = default);   // IMesClient
MesResult Invoke(MesApiDefinition definition, CancellationToken ct = default);                     // IMesClient（全权，不进配置）

MesConfig Current { get; }                       // IMesConfigManager
string LastLoadError { get; }
string LastSaveError { get; }
void Reload();
MesApiDefinition GetApi(string name);            // 深拷贝，供编辑
IReadOnlyList<string> GetApiNames();
bool Save(string originalName, MesApiDefinition definition);
bool Delete(string name);

void Write(MesResult result);                    // IMesLogSink
```

### 模型与枚举

`MesApiDefinition`（12 字段）：`Name` / `DisplayName` / `Method` / `Url` / `QueryParams` / `Headers` / `Auth` / `EnableCertificateValidation` / `Body` / `BodyType` / `TimeoutMs` / `Retry`

`MesResult`（9 字段）：`ApiName` / `StartTime` / `EndTime` / `ElapsedMs` / `Request` / `Response` / `HttpStatus` / `Attempts` / `Failure`

| 枚举 | 成员 |
|---|---|
| `MesHttpMethod` | `Get` `Post` `Put` `Delete` `Patch` |
| `MesBodyType` | `Json=0` `Form=1` `Text=2` `None=3` `Xml=4` |
| `MesAuthType` | `None` `Bearer` `Basic` `ApiKey` |
| `MesKeyIn` | `Header` `Query` |
| `MesFailureKind` | `None` `Timeout` `Network` `Cancelled` `ConfigError` `Unknown` |

`MesFailureText.Of(kind)` 给统一中文文案（「超时」「连接失败」「配置错误」「驱动异常」…）。

---

## 4. 快速开始

### 容器注册

```csharp
string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPathName.MesFile);

builder.Register(c => new MesConfigManager(configPath)).As<IMesConfigManager>().SingleInstance();
builder.RegisterType<MesClient>().As<IMesClient>().SingleInstance();
builder.RegisterType<MesLogSink>().As<IMesLogSink>().SingleInstance();   // 可选，不注册则不打日志
```

三个都是 `SingleInstance`：配置管理器必须（快照就是它的状态），其余两个无状态，单例只为省构造。

### 最小配置

```json
{
  "Version": 1,
  "Apis": [
    {
      "Name": "IF034_一次注液",
      "DisplayName": "一次注液上报",
      "Method": 1,
      "Url": "http://10.1.1.20/mes/api/station",
      "QueryParams": [],
      "Headers": [],
      "Auth": { "Type": 0 },
      "EnableCertificateValidation": false,
      "Body": "{\"equip_code\":\"A01\",\"process_code\":\"P01\"}",
      "BodyType": 0,
      "TimeoutMs": 15000,
      "Retry": { "MaxAttempts": 1, "BackoffMs": 500 }
    }
  ]
}
```

---

## 5. 使用示例

### 场景选型

| 场景 | BodyType | MaxAttempts | 说明 |
|---|---|---|---|
| 查询（查工单 / 查物料） | `Json` / `None` | 2~3 | 幂等，可重试 |
| 过站上报 | `Json` / `Form` | **1** | 非幂等，重试会造重复数据 |
| SOAP 调用 | `Xml` | 1~2 | 信封原样发 |
| 表单提交 | `Form` | 1~2 | 界面按键值对编辑 |

> ⚠️ 仓内当前**没有生产调用方**（只有调试页），下列流程侧代码是推荐形态，不是既有挂点。

### 5.1 只发配置里的正文

```csharp
MesResult r = mesClient.Invoke("IF034_一次注液", null, ct);

if (r.HttpStatus == 200)        // 🔴 判 200，不判 != null——302 也会 != null
{
    // 业务码仍需调用方解析 r.Response
}
```

### 5.2 流程覆盖正文（Json）

```csharp
// 配置里打底 {"equip_code":"A01","process_code":"P01"}
// 流程只传变化的字段，对象合并、数组整体替换（不与配置拼接）
var options = new MesCallOptions
{
    Body = new { bar_code = "SN20261101001", wip_list = new[] { "A", "B" } }
};
MesResult r = mesClient.Invoke("IF034_一次注液", options, ct);
```

### 5.3 动态参数：查询参数 / 请求头 / 鉴权

配置里放固定值，运行期才知道的用 `MesCallOptions` 覆盖——同名替换、新名追加，其余沿用配置。

```csharp
var options = new MesCallOptions
{
    // GET 的动态参数（参数名大小写敏感）
    QueryParams =
    {
        new MesHeader("barcode", "SN20261102001"),
        new MesHeader("line", "L02")
    },

    // 签名 / 时间戳这类运行期才算得出的头（头名大小写不敏感）
    Headers =
    {
        new MesHeader("X-Timestamp", ts),
        new MesHeader("X-Sign", Hmac(secret, ts))
    },

    // 动态 token：整体替换配置里的鉴权
    Auth = new MesAuthConfig { Type = MesAuthType.Bearer, Token = dynamicToken }
};

MesResult r = mesClient.Invoke("IF034_查询过站", options, ct);
```

| 规则 | 说明 |
|---|---|
| 合并 | 同名替换整条、新名追加；配置里没被覆盖的原样保留 |
| 参数名大小写 | **敏感**（HTTP 无规定，服务端框架默认敏感）。`barcode` 与 `BarCode` 是两个参数 |
| 头名大小写 | **不敏感**（RFC 7230）。`content-type` 与 `Content-Type` 视为同一个 |
| `Auth` | 非 null 时**整体替换**配置的鉴权，不做字段级合并 |
| `Authorization` 头 | 不要走 `Headers` 传——鉴权生效时会先摘掉它再注入（「鉴权赢」）。动态 token 用 `Auth` |

> 🔴 **什么该写配置**：所有调用方都一样的固定值（`appId`、`format`、固定 token）留在 `Mes.json`。
> 覆盖层只放「每次调用都不一样」的值。挪进代码容易、挪回配置没人做——配置空心化之后，换个地址就要改代码重编译。
>
> 🔴 **要改结构或清空重填**（Url / Method / BodyType、删掉配置里某个参数）走 `Invoke(MesApiDefinition, ct)`，
> 见 §5.7。两个入口的边界：**按名 = 打补丁，按定义 = 全权**。

### 5.4 表单提交

配置侧 `Body` 存的是 `MesHeader` 数组文本（界面按键值对编辑），驱动转 `k=v&…` 发出：

```json
"Body": "[{\"Name\":\"username\",\"Value\":\"admin\"},{\"Name\":\"password\",\"Value\":\"123456\"}]"
```

流程侧也可以直接传已拼好的 form 串——`Body` 不是 JSON 对象时**原样发送**，不合并：

```csharp
var options = new MesCallOptions { Body = "username=admin&password=123456" };
```

### 5.5 SOAP（Xml 形态）

```json
"BodyType": 4,
"Headers": [
  { "Name": "SOAPAction", "Value": "\"http://tempuri.org/Add\"" },
  { "Name": "Content-Type", "Value": "text/xml; charset=utf-8" }
]
```

`Content-Type` 不配也行——Xml 形态的默认值就是 `text/xml; charset=utf-8`；配了则**配置赢**。SOAP 1.2 厂商要求 `application/soap+xml; action="…"` 时在请求头页手填覆盖即可。

### 5.6 调试页发送（仓内真实挂点）

```csharp
// Taoyl.UI.Pages/ViewModels/MesDebugViewModel.cs
MesResult result = await Task.Run(() => mesClient.Invoke(target, null, cts.Token));
```

不带 body、只发已保存的配置——这样日志里的 `ApiName` 与磁盘内容永远对得上，事后能按日志复现。

### 5.7 临时接口：直接给定义

接口还没进配置，或请求头要运行期才算（签名 / 时间戳），用第二个入口。**不进配置、不落盘**，但装配 / 重试 / 日志与按名调用完全同路。

```csharp
var definition = new MesApiDefinition
{
    Name = "temp-查库存",                 // 只用于日志标识
    Url  = "http://10.1.1.20/api/query",
    Body = "{\"bar_code\":\"SN001\"}"
};

MesResult r = mesClient.Invoke(definition, ct);
```

除 `Name` / `Url` 外各字段都有可用默认值，按场景补：

| 场景 | 额外要写的 |
|---|---|
| GET 查询 | `Method = MesHttpMethod.Get` |
| SOAP 调用 | `BodyType = MesBodyType.Xml` + `Headers.Add(new MesHeader("SOAPAction", "…"))` |
| 带 Bearer | `Auth.Type = MesAuthType.Bearer` + `Auth.Token = "…"` |
| 带签名头 | `Headers.Add(new MesHeader("X-Sign", Sign(body)))` |

⚠️ 两条边界：

- **`BodyType` 默认是 `Json`**。发 XML / 表单正文必须显式设置，否则装配阶段报「配置正文不是合法 JSON」。
- **临时接口不留审计**——审计只记 `Mes.json` 的变更。生产路径请走配置。

### 5.8 完全自由：直接用 HttpClient

驱动做不到的一类只有：**正文不是一段文本**（multipart 文件上传、流式大报文、自定义 `HttpContent`）。这种情况直接用 `System.Net.Http.HttpClient`——本库不提供包装入口，也不阻止。

```csharp
/// <summary>上传检测报告 + 表单字段。multipart 正文驱动不支持，必须手写。</summary>
public MesResult UploadReport(string url, string filePath, string barCode, CancellationToken ct)
{
    DateTime started = DateTime.Now;
    var watch = Stopwatch.StartNew();

    using (var handler = new HttpClientHandler
    {
        UseProxy = false,             // ① 工控机常配系统代理
        AllowAutoRedirect = false     // ② 否则 302 被跟成「200 + 登录页 HTML」
    })
    using (var client = new HttpClient(handler) { Timeout = Timeout.InfiniteTimeSpan })
    {
        var content = new MultipartFormDataContent();
        content.Add(new StringContent(barCode, Encoding.UTF8), "bar_code");
        var file = new StreamContent(File.OpenRead(filePath));
        file.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        content.Add(file, "report", Path.GetFileName(filePath));

        using (var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content })
        using (var timeout = new CancellationTokenSource(15000))
        using (var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeout.Token))
        {
            // ③ ResponseHeadersRead：拿到头就返回，body 流式读，否则大响应全进内存
            using (var response = client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, linked.Token)
                .GetAwaiter().GetResult())
            {
                watch.Stop();
                return new MesResult(
                    "temp-上传报告", started, started.AddMilliseconds(watch.ElapsedMilliseconds),
                    watch.ElapsedMilliseconds,
                    "POST " + url,                                   // Request：没有 Render，只能自己描述
                    ReadLimited(response, 8 * 1024 * 1024),          // ④ 自己限量读
                    (int)response.StatusCode, 1, MesFailureKind.None);
            }
        }
    }
}
```

四条必须自己补的（就是驱动替你做的）：

| # | 项 | 漏了会怎样 |
|---|---|---|
| ① | `UseProxy = false` | 绕系统代理，或直接连不上 |
| ② | `AllowAutoRedirect = false` | 会话失效被伪装成成功：拿到 200 + 一段 HTML |
| ③ | 双 CTS 控超时（不用 `client.Timeout`） | 后者只管到「拿到响应头」，服务端发完头不发 body 会永久挂死 |
| ④ | 解码认 BOM | 带 BOM 的 JSON 会让 `JToken.Parse` 直接抛；UTF-16 按 charset 解是满屏乱码 |

**不要以为这条路没有日志**：`MesResult` 的构造函数是 public，`IMesLogSink` 是公开端口，自己造一个丢进去就行——治理要自己做，但做得到。

对照表（接手人看这张就够）：

| 能力 | 驱动 | 手写 |
|---|---|---|
| URL + 参数编码 | 配置声明 + `EscapeDataString` | 自己拼，别漏编码 |
| 请求头 / 鉴权注入 | 配置 + `AuthInjection` | 自己 `Headers.Add` |
| Content-Type 决策 | 形态表 / 配置覆盖 | 自己设 |
| 超时覆盖读 body | 双 CTS | 自己写 |
| 不跟随重定向 / 不用代理 | handler 已设 | 自己设 |
| 证书按请求跳过 | `Properties` 标记 | 能做，但要自己挂回调 |
| 连接池复用 | 静态单例 | 自己持有 client（**别在方法里 new**） |
| 8MB 限量 + 截断标注 | 内置 | 自己写循环 |
| 按声明解码（BOM→charset） | 内置 | 自己写 |
| 失败六分类 + 文案 | 内置 | 自己 catch 归 Kind |
| 重试 + ±20% 抖动 | 内置 | 自己写循环 |
| `MesResult.Request` 渲染 | `Render()` 六段 | 自己拼串 |
| 日志 | logSink 自动 | 自己 `Write` |

---

## 6. 类库结构

**目录即执行顺序**：`Request/` 是装配段，`Transport/` 是收发段。

| 文件 | 行 | 职责 |
|---|---|---|
| `MesClient.cs` | 337 | 编排：查定义 → `RequestTemplate.Create` → 重试发送 → 组装 `MesResult`（唯一构造点） |
| **Request/** | | |
| `RequestTemplate.cs` | 255 | 请求模板：`Create` 装配五步、`Build` 造 `HttpRequestMessage`、`Render` 六段 JSON |
| `RequestUrl.cs` | 72 | 查询参数快照 + 拼最终地址 |
| `AuthInjection.cs` | 66 | 鉴权配置 → 往头塞什么 / 往参数塞什么 |
| `RequestHeaders.cs` | 72 | 配置头 + 鉴权头 → 最终请求头（分离 Content-Type） |
| `Body/RequestBody.cs` | 36 | 形态 → `IBodyBuilder` 的登记表 |
| `Body/IBodyBuilder.cs` | 20 | 一种形态的全部知识：`ContentType` + `Build` |
| `Body/JsonBodyBuilder.cs` | 58 | JSON：对象合并 + 压缩 |
| `Body/FormBodyBuilder.cs` | 124 | 表单：数组 → 对象 → `k=v&` |
| `Body/TextBodyBuilder.cs` | 17 | 纯文本：整体替换 |
| `Body/XmlBodyBuilder.cs` | 18 | XML：同 Text，仅 Content-Type 不同 |
| `Body/NoneBodyBuilder.cs` | 17 | 无正文 |
| `Body/BodyText.cs` | 87 | 文本工具：object→text、解析、压缩（区分配置侧/流程侧） |
| **Transport/** | | |
| `HttpTransport.cs` | 137 | 发送：静态 `HttpClient` 单例、双 CTS、证书回调 |
| `HttpResult.cs` | 9 | 状态码 + 正文 |
| `HttpTransportException.cs` | 23 | 带 `MesFailureKind` 的失败 |
| `HttpResponseReader.cs` | 117 | 流式限量读 + 按声明解码 |
| **其他** | | |
| `Internal/ExceptionText.cs` | 27 | 取最内层异常消息 |
| `Interfaces/` | 113 | 三个端口 |
| `Models/` | 9 文件 | 配置与结果模型（含 `MesCallOptions` 调用覆盖层） |
| `Config/` | 2 文件 | `MesConfigStore`（字节↔模型、原子写、备份回退）/ `MesConfigManager`（快照热替换 + 写锁） |

---

## 7. 内部逻辑

```
MesApiDefinition ──┐
                   ├─ RequestTemplate.Create ─┬─ ApplyOverrides(头 / 参数)    → 合并后的两个列表
MesCallOptions ────┘                          ├─ RequestBody.For(形态)       → 正文 + ContentType
                                              ├─ AuthInjection.From(生效鉴权) → 注入项（头 / 参数）
                                              ├─ RequestHeaders.Build(头, 注入项) → 头集合
                                              └─ RequestUrl.CollectQuery + Build → 最终地址
                                                        │
                                              RequestTemplate.Build() → HttpRequestMessage
                                                        │
                                              HttpTransport.Send → HttpResult
                                                        │
                                              HttpResponseReader.Read → string
                                                        │
                                              MesResult（Compose 唯一构造点）
```

### 关键设计决策

| 决策 | 为什么 |
|---|---|
| 不抽 `ITransport` / 协议维度 | HTTP 与 WebService 共享 90%，与 raw TCP 只共享 20%——切割线划错位置 |
| 不用 `IHttpClientFactory` | 它治的三个病我们都没有：Socket 耗尽（本来就是单例）、DNS 陈旧（IP 直连）、管道集成（重试语义特殊） |
| `AllowAutoRedirect = false` | 会话失效跳登录页的 302 会被吞成「200 + HTML」，流程判成成功继续走 |
| `Content-Type` 独立成字段 | 它只能挂 `HttpContent.Headers`，且"发不发"取决于有没有内容段——只有正文域知道 |
| 正文形态用「表 + 每个形态一个类」 | 加形态时改动局部化，且每个类的 `ContentType` 与成型逻辑写在一起，不会两处走偏 |
| 配置侧坏正文抛、流程侧宽容 | 配置是我们的责任；覆盖层 `Body` 允许传表单串 / XML |
| 覆盖层在 `Create` 一层消化 | 下游各域收到的是「合并后的列表」，不知道覆盖层存在——加第五个覆盖域只改这一处，装配器零改动 |
| 驱动不设总超时预算 | 超时控制权在调用方的 `CancellationToken`；注释里给了最坏耗时公式 |
| 退避固定 + 抖动，不指数 | `MaxAttempts` 一般 2~3 次，指数退避要 5 次以上才有意义 |

---

## 8. 注意事项与 FAQ

### 🔴 铁律

| # | 铁律 |
|---|---|
| 1 | **`Invoke` 永不抛**——任何失败都是 `HttpStatus == null` + `Failure` 分类 + `Response` 描述。ProcessBase 的 catch 会把异常转成 E999，等于停线 |
| 2 | **成功判据是 `HttpStatus == 200`，不是 `!= null`**——驱动不跟随重定向，302 也 `!= null` |
| 3 | **`Invoke` 的 `apiName` 是大小写敏感的唯一键**。改名必须先确认流程侧没有硬编码 |
| 4 | **`Mes.json` 是唯一真相**，且不在版本控制里。改 URL / 鉴权 / 正文属生产契约变更，界面保存会留审计，手改文件不会 |
| 5 | 配置粒度是「工序 × 接口」而非「接口」：同一接口在不同工序下固定字段取值不同，应配成多条独立记录 |

### ⚠️ 坑位

| 现象 | 原因 / 处理 |
|---|---|
| 改了 `Mes.json` 不生效 | 手改文件不会热加载（`Reload()` 无调用方），需重启；界面保存才会换快照 |
| 状态条「配置错误」+「请求构造失败」 | 配置正文不是合法 JSON / 接口名不存在 / 地址为空 / 头名非法（含空格或中文） |
| 状态条 302 但 `Failure = None` | 设计如此：拿到响应就算不是传输失败，是否成功由调用方按 200 判 |
| GET 的 `contentType` 是 `null` | 无正文就没有内容段，内容头无处可挂——实发确实没有 |
| Form 正文发出去是空的 | 检查 `Body` 是否为合法数组文本；驱动遇坏 JSON 会抛，不会静默发空 |
| 配了 `BackoffMs = 0` 仍在等 | `0` 与「未配置」同义，回落 `MesDefaults.BackoffMs` |
| 响应里出现 `�` | 声明与实际编码不符，驱动只标注不换编码重试——那会抹掉排障信号 |

### 常见 FAQ

**Q：能不能返回强类型对象？**
不能。驱动只保证传输层，业务码/字段结构由调用方解析。19 条接口结构各异，逐条建 DTO 是另一个量级的工程。

**Q：为什么没有 WSDL 导入？**
开发期用 SoapUI 生成信封，抄进 `Mes.json` 即可。WSDL 是工具期资产，不是运行时依赖。

**Q：多个流程并发调用安全吗？**
安全。`MesClient` 无状态，`HttpClient` 进程级单例，连接池上限 64。⚠️ 但 `UseCookies` 默认开启且 `CookieContainer` 全局一份——会话型 MES 靠它维持登录态，同时意味着跨流程共享会话。

**Q：要对接两个 MES 怎么办？**
**同一个 `Mes.json`，接口名加前缀**（如 `A_过站上报` / `B_过站上报`），一条都不同改代码。**不需要两个 `HttpClient`**：连接池按 `scheme+host+port` 分桶，一个 client 访问两个服务器就是往两个桶取连接，并发上限也按桶算。

只有两种差异才逼出第二个实例（`UseProxy` / `ClientCertificates` 是 handler 级，全进程一份）：

- 一个 MES 要走系统代理、另一个不能
- 一个 MES 要客户端证书（mTLS）

证书严格程度**不需要**拆：我们的证书策略是请求级的（`Properties` 标记），同一个 client 可以对 A 严格、对 B 跳过。

⚠️ Cookie 按**域名**隔离，所以 `10.1.1.20` 与 `10.1.1.30` 天然不串；但**同一 host 的不同端口 / 路径不隔离**——两个 MES 若共用主机且都用 Cookie 会话，会互相污染。

真需要分组展示时，给 `MesApiDefinition` 加一个 `Group` 字段（配 1 字段 + 调试页加筛选）比"多实例 + keyed DI"少一个数量级的工作量。

**Q：怎么绕过驱动直接发请求？**
见 §5.8。本库不提供包装入口——`HttpClient` 是 BCL 公开类，封不住也没必要封。

---

## 9. 扩展指南

### A. 加一种正文形态（要动 8 处，漏一处不会报错）

| # | 位置 |
|---|---|
| 1 | `MesBodyType` 枚举加值（现有值严禁改动，旧配置按名反序列化） |
| 2 | 新建 `XxxBodyBuilder : IBodyBuilder` + `RequestBody` 登记表加一行 |
| 3 | `MesBodyTypeUi.Table`（UI 侧提示与格式化按钮） |
| 4 | `MesApiEditModel` 加一个编辑器字段 |
| 5 | 同文件 `ctor` 的 switch（装载） |
| 6 | 同文件 `ToDefinition` 的 switch（收集） |
| 7 | `FormMesDebug.Designer`（radio + panel + textbox + `flpBodyType` 列宽） |
| 8 | `FormMesDebug.bodyTypeMap` 加一行——绑定、DPI 补偿、面板显隐都从这张表派生 |

同一份清单也写在 `MesBodyType` 枚举注释里（跟着枚举走，不会像外部文档那样过期）。

### B. 加一种鉴权方式

1. `MesAuthType` 加成员（值落 JSON，追加即可）
2. `AuthInjection.From` 加一个 `case`，返回 `Header` / `QueryParam` / 两者 / `null`
3. `FormMesDebug` 的 `authTypeMap` 加一行 + Designer 加面板

"鉴权赢"（摘掉手配的 `Authorization`）的规则只在 `RequestHeaders.Build` 一处实现，新分支不用碰。

### C. 加一条接口

纯配置：界面「新增」→ 填 → 保存；或直接改 `MesConfigManager` 指向的 `Mes.json`（后者需重启，且无审计）。

### D. 真要接 raw TCP

新增 `TcpMesClient : IMesClient`，并把 `MesConfigStore` / `MesConfigManager` 泛型化成 `TModel` 复用那套原子写入 / 备份回退 / 快照替换。**不要**往 `MesApiDefinition` 塞 `Ip` / `Port`。

### E. 改重试策略

`MesClient.ShouldRetry` 是唯一判据点，`GetDelayMs` 是唯一间隔点。改之前先想清楚"接口是否幂等"——那是唯一真正的判据，不按错误类型分。
