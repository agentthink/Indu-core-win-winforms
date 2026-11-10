# Taoyl.Application.Device —— 设备契约与运行时 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**设备域基座库**：设备契约 + 设备运行时 + 与具体设备无关的配置基础设施。

**是什么**：设备实例的最小公共面（`IDevice`）；实例注册表与连接编排（`IDeviceManager`）；配置→实例联动的业务用例（`IDeviceOrchestration`）；Devices 配置文件的通用读写机制（`IDeviceConfigStore` / `IDeviceConfigSection` / `IDeviceConfigLoader`）；日志与驱动缓存同步两个端口（`IDeviceLogSink` / `IDeviceConfigSyncHook`）。

**不是什么**：不认识任何具体设备类型（不定义 `"PLC"` 之类的常量）、不认识任何驱动、不含任何工厂/适配器。这些都是实现层（`Business`）的事，通过端口注入进来。

**硬边界**：**零 ProjectReference**。唯一 NuGet 依赖是 `Newtonsoft.Json`——只服务「设备配置是 JSON」这一事实，与厂商无关。根模型由泛型参数注入（`DeviceConfigStore<TModel>`），底座不认识任何具体配置模型。

## 2. 结构

| 目录 | 内容 |
|---|---|
| `Interfaces/` | 10 个契约：`IDevice` / `IDeviceManager` / `IDeviceOrchestration` / `IDeviceFactory` / `IDeviceConfigManager` / `IDeviceConfigLoader` / `IDeviceConfigStore` / `IDeviceConfigSection` / `IDeviceConfigSyncHook` / `IDeviceLogSink` |
| `Models/` | `DeviceCreationRequest`（loader → manager 的 DTO）、`DeviceChangeKind` / `DeviceChangedEventArgs`（变更通知载荷） |
| `Services/` | `DeviceManager`（运行时：实例注册 + 配置编排 + 变更通知） |
| `Config/` | `DeviceConfigStore<TModel>`（JSON 镜像存储）、`DeviceConfigSection<TModel>`（类型 ↔ 节映射） |
| `ConfigLoader/` | `JsonDeviceConfigLoader`（JSON 配置加载器，节点名由节声明） |

## 3. 运行时装配：`DeviceManager` 的六个依赖

| 依赖 | 谁提供 | 说明 |
|---|---|---|
| `IEnumerable<IDeviceFactory>` | 实现层，每种设备类型一个 | `DeviceType` 是不透明字符串，底座从不判断取值 |
| `IEnumerable<IDeviceConfigLoader>` | 实现层，每种文件格式一个 | JSON 用底座自带的 `JsonDeviceConfigLoader`；xlsx 等自写 |
| `IDeviceConfigStore` | 用 `DeviceConfigStore<TRoot>` | `TRoot` = 本应用的配置文件根模型 |
| `IEnumerable<IDeviceConfigSection>` | 实现层，每种设备类型一条 | 声明「这个类型对应根模型里的哪个列表」+「配置文档里的哪个节点」（`ConfigNode`）；DeviceManager 与 JSON 加载器共用这一份声明 |
| `IEnumerable<IDeviceConfigSyncHook>` | 缓存型设备才需要 | 直读型设备可注册 0 个 |
| `IDeviceLogSink` | 实现层 | 必须有一个（底座的 `DeviceManager` 不接受 null） |

## 4. 新增一种设备类型：完整清单

以新增「视觉设备 Vision」为例，**底座改动 0 行、组合根改动 0 行**。

| # | 动作 | 位置 | 要点 |
|---|---|---|---|
| 1 | 写配置 POCO `VisionConfig` | 应用层 `Models/` | 名称字段要与根模型、节声明一致 |
| 2 | 写适配器 `VisionDeviceAdapter : IDevice` | 应用层 `Devices/Adapter/` | 5 个成员；`GetRawDev<T>` 类型不符抛 `InvalidCastException` |
| 3 | 写工厂 `VisionDeviceFactory : IDeviceFactory` | 应用层 `Devices/Factory/` | **自动注册**（组合根按 `IDeviceFactory` 扫描）；注意 §5 的 config 双形态 |
| 4 | 根模型加 `List<VisionConfig> Visions` | 应用层 `Models/DevicesConfigFile` | 底座不知道根模型形状，只要节能取到列表 |
| 5 | 节声明加一行：`new DeviceConfigSection<TRoot>(deviceType, configNode, typeof(VisionConfig), m => m.Visions, c => ...Name)` | 应用层 `Devices/ConfigStore/DeviceConfigSections` | 这一行同时供 DeviceManager（列表映射）与 JSON 加载器（节点分派）使用 |

> 缓存型设备（配置要先同步进驱动缓存）再加第 6 步：实现 `IDeviceConfigSyncHook` 并注册。
> 只有配置镜像、不建实例的类型（如 Scanner）：跳过第 2、3 步即可——节声明仍然有效。

**为什么是 5 步而不是更少**：第 1~3 步是三种不同的知识（配置长什么样 / 怎么连 / 怎么建），无法合并；
第 4 步是根模型的类型形状（编译期可查，值得保留）；第 5 步是唯一一处「声明」，其余全部由它派生。

> 逐步可照抄的代码见 **§6**；`BuildDevice` / `RemoveDevice` 与设备级用例的区别见 **§7**。

## 5. 契约里的潜规则：`config` 有两种形态

**`IDeviceFactory.CreateDevice(name, object config)` 的 `config` 可能是 `JToken`，也可能是具体配置对象。**

- 启动装配路径：`JsonDeviceConfigLoader` 解析出的 `JToken` 直接透传；
- 编辑保存路径：UI 传进来的具体配置对象（如 `PlcConfig`）。

所以工厂里**不能直接 `(VisionConfig)config` 强转**——启动装配时会 `InvalidCastException`，而编辑保存路径正常，故障只在重启后暴露。统一写法：

```csharp
var cfg = config as VisionConfig
          ?? JsonConvert.DeserializeObject<VisionConfig>(JsonConvert.SerializeObject(config));
```

> 历史坑已消除：「配置叫什么名字」原先存在两处（加载器的 `nameField` 字符串 + 节的 `nameOf` lambda），
> 不一致时「装配能起、但配置增删改查不到该设备」。现在加载器直接调 `section.GetName(...)`，
> 节点名（`ConfigNode`）与取名字都只来自 `IDeviceConfigSection` 一处声明。

## 6. 新增一种设备类型：完整示例（可照抄）

以「视觉设备 Vision」为例，从零到跑通的全部代码。**底座与组合根都不改。**

### 6.1 要写 / 要改的文件

| 动作 | 文件 | 说明 |
|---|---|---|
| 新增 | `Devices/Models/VisionConfig.cs` | 配置 POCO |
| 新增 | `Devices/Adapter/VisionDeviceAdapter.cs` | 设备适配器 |
| 新增 | `Devices/Factory/VisionDeviceFactory.cs` | 工厂（自动注册，无需改组合根） |
| 改 1 行 | `Devices/Models/DeviceTypes.cs` | 加类型常量（可选） |
| 改 1 行 | `Devices/Models/DevicesConfigFile.cs` | 根模型加列表 |
| 改 1 行 | `Devices/ConfigStore/DeviceConfigSections.cs` | 加节声明 |
| 改 1 段 | `config/Devices.json` | 加节点 |
| **不改** | `Device` 库 / `DeviceModule` | — |

### 6.2 步骤 1：配置 POCO

```csharp
using Newtonsoft.Json;

namespace Taoyl.Application.Business.Devices.Models
{
    /// <summary>视觉设备配置（Devices.json 的 "visions" 节）。</summary>
    public class VisionConfig
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("ip")]
        public string Ip { get; set; }

        [JsonProperty("port")]
        public int Port { get; set; } = 8080;

        [JsonProperty("recipePath")]
        public string RecipePath { get; set; }
    }
}
```

`JsonProperty` 的取值决定磁盘上的键名——这一层就是「配置文件长什么样」的唯一真相。

### 6.3 步骤 2：适配器

```csharp
using System;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Business.Devices.Adapter
{
    /// <summary>视觉设备适配器：把 IVisionClient 适配成 IDevice。</summary>
    internal sealed class VisionDeviceAdapter : IDevice
    {
        private readonly IVisionClient _client;

        public VisionDeviceAdapter(string name, IVisionClient client)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _client = client ?? throw new ArgumentNullException(nameof(client));
        }

        public string Name { get; }
        public string DeviceType => DeviceTypes.Vision;
        public bool IsConnected => _client?.IsConnected ?? false;

        public bool Connect()
        {
            if (_client.IsConnected) return true;
            try { return _client.Connect(); }
            catch { return false; }
        }

        public void Disconnect()
        {
            try { _client?.Close(); } catch { }
        }

        public void Dispose()
        {
            Disconnect();
            try { _client?.Dispose(); } catch { }
        }

        public T GetRawDev<T>()
        {
            if (_client is T raw) return raw;
            throw new InvalidCastException(
                $"设备 [{Name}] 的底层驱动类型为 {_client?.GetType().Name}，无法转换为 {typeof(T).Name}");
        }
    }
}
```

三条实现纪律：

- `Connect` **不抛异常**，失败返回 `false` —— 启动装配的逐设备容错依赖它。
- `Dispose` **必须可重复调用** —— 重建路径会先释放旧实例。
- `GetRawDev<T>` 类型不符抛 `InvalidCastException` —— 调试页依赖这个契约。

### 6.4 步骤 3：工厂

```csharp
using System;
using Newtonsoft.Json;
using Taoyl.Application.Business.Devices.Adapter;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Business.Devices.Factory
{
    /// <summary>视觉设备工厂。注册由 DeviceModule 的 IDeviceFactory 扫描自动完成。</summary>
    public sealed class VisionDeviceFactory : IDeviceFactory
    {
        public string DeviceType => DeviceTypes.Vision;

        public IDevice CreateDevice(string name, object config)
        {
            var cfg = Bind(config);
            if (cfg == null) throw new ArgumentException("视觉设备配置无法解析");

            var client = new VisionClient(cfg.Ip, cfg.Port);
            return new VisionDeviceAdapter(name, client);
        }

        public bool TryValidateConfig(object config, out string error)
        {
            error = null;
            var cfg = Bind(config);
            if (cfg == null) { error = "视觉设备配置无法解析"; return false; }
            if (string.IsNullOrWhiteSpace(cfg.Name)) { error = "设备名称不能为空"; return false; }
            if (string.IsNullOrWhiteSpace(cfg.Ip)) { error = "IP 地址不能为空"; return false; }
            if (cfg.Port <= 0 || cfg.Port > 65535) { error = $"端口号无效: {cfg.Port}"; return false; }
            return true;
        }

        /// <summary>config 可能是 JToken（启动装配）或 POCO（编辑保存），统一绑定。</summary>
        private static VisionConfig Bind(object config)
        {
            if (config == null) return null;
            if (config is VisionConfig typed) return typed;
            return JsonConvert.DeserializeObject<VisionConfig>(JsonConvert.SerializeObject(config));
        }
    }
}
```

### 6.5 步骤 4：类型常量（可选，推荐）

```csharp
public static class DeviceTypes
{
    public const string Plc = "PLC";
    public const string Scanner = "Scanner";
    public const string Scale = "Scale";
    public const string Vision = "Vision";   // ← 新增
}
```

### 6.6 步骤 5：根模型加列表

```csharp
public class DevicesConfigFile
{
    [JsonProperty("PLCs")]     public List<PlcConfig> PLCs { get; set; } = new List<PlcConfig>();
    [JsonProperty("scanners")] public List<ScannerConfig> Scanners { get; set; } = new List<ScannerConfig>();
    [JsonProperty("scales")]   public List<ScaleConfig> Scales { get; set; } = new List<ScaleConfig>();
    [JsonProperty("visions")]  public List<VisionConfig> Visions { get; set; } = new List<VisionConfig>(); // ← 新增
}
```

### 6.7 步骤 6：节声明加一行

```csharp
public static readonly IReadOnlyList<IDeviceConfigSection> All = new List<IDeviceConfigSection>
{
    // ... 原有三条 ...
    new DeviceConfigSection<DevicesConfigFile>(
        DeviceTypes.Vision,        // 类型标识：必须与工厂的 DeviceType 一致
        "visions",                 // 配置节点名：必须与 Devices.json 的键一致
        typeof(VisionConfig),      // 配置 CLR 类型
        m => m.Visions,            // 根模型里的列表
        c => RequireConfig<VisionConfig>(c, DeviceTypes.Vision).Name),   // 取名字
};
```

这一行同时供两处使用：`DeviceManager`（按类型找列表、增删改查）与 `JsonDeviceConfigLoader`（按节点名分派）。**只声明这一次。**

### 6.8 步骤 7：配置文件加节点

```json
{
  "PLCs": [ ... ],
  "scanners": [ ... ],
  "scales": [ ... ],
  "visions": [
    {
      "name": "Vision_W01",
      "ip": "192.168.1.50",
      "port": 8080,
      "recipePath": "./Config/vision/w01.json"
    }
  ]
}
```

### 6.9 步骤 8：验证

1. 启动 → 日志无「未找到设备工厂」，`AllDevices` 里出现 `Vision_W01`。
2. `GetDevicesByType("Vision")` 返回 1 条。
3. `AddDevice("Vision", draft)` → 配置落盘新增一节，实例建出来。
4. `UpdateDevice("Vision", oldName, draft)` 改名 → 旧实例释放、旧键摘除、配置落盘。
5. `DeleteDevice("Vision", name)` → 实例 + 缓存钩子（若有）+ 配置一起清。
6. 配置里故意写错 `ip` → 启动时该设备被跳过并记日志，其余设备照常装配。

## 7. 实例域 vs 设备域：能力矩阵与不变式

`BuildDevice` / `RemoveDevice` 与 `AddDevice` / `UpdateDevice` / `DeleteDevice` **名字相近、签名相近，但动的东西完全不同**。改这几个方法之前先看这一节。

### 7.1 能力矩阵

| 方法 | 实例表 | 驱动运行时缓存 | 配置文件 | 典型场景 |
|---|---|---|---|---|
| `BuildDevice(type, name, config)` | **upsert + 释放旧实例** | 只读（不写） | 不动 | 建 / 换实例 |
| `RemoveDevice(type, name)` | **摘除 + 释放** | 不动 | 不动 | 摘实例 |
| `AddDevice(type, config)` | → BuildDevice | **写**（SyncHook） | **增 + 落盘** | 新增设备 |
| `UpdateDevice(type, oldName, config)` | → BuildDevice +［改名时］RemoveDevice 旧名 | **写 +［改名时］清旧名** | **改 + 落盘** | 保存生效 |
| `DeleteDevice(type, name)` | → RemoveDevice | **清** | **删 + 落盘** | 删除设备 |
| `LoadFromConfigFile(path)` | 批量 BuildDevice | 不动 | 读（镜像载入） | 启动装配 |

一句话：**`BuildDevice` / `RemoveDevice` 是原语（只碰实例）；另外三个是设备级用例（实例 + 缓存 + 配置三者一起动）。**

### 7.2 `BuildDevice` 的五条不变式（改它之前必须知道）

1. **唯一实例创建入口**：全库只有这一处调 `factory.CreateDevice`。绕过它直接建实例 = 「传新配置却建出旧实例」的静默错误（PLC 的配置源是驱动缓存，不是入参）。
2. **先建新、成功才换旧**：`CreateDevice` 在锁内、在任何字典操作之前。它抛异常 → 旧实例没释放、实例表没动。
3. **摘旧 → 立刻占位 → 最后释放**，三步不许换序：`Dispose` 夹在中间会让字典键空窗至 IoWorker 退出（上限 2 秒），期间 `GetDevice(name)` 返回 null。
4. **`Dispose` 必须在锁内同步执行**：PLC 侧常限制并发连接数，异步释放会出现「新实例已连、旧连接还挂着」→ PLC 拒连。
5. **不落盘、不同步缓存**：所以手动调它 = 纯重置（调试页「重建」键就是这个语义）。要让配置生效，必须走 `UpdateDevice`。

### 7.3 `RemoveDevice` 的三条

1. 只动实例表 + `Dispose`，**不碰配置、不碰驱动缓存**。
2. `Dispose` 锁内同步（同不变式 4）。
3. 未命中返回 `false`（幂等友好，重复删除不抛）。

### 7.4 选哪个：决策表

| 你想做的事 | 用哪个 |
|---|---|
| 配置改了，要生效并保存 | `UpdateDevice` |
| 配置没改，只想重启这台设备 | `BuildDevice`（调试页「重建」） |
| 新增一台设备（配置 + 实例） | `AddDevice` |
| 彻底删掉一台设备 | `DeleteDevice` |
| 临时摘掉实例，配置留着 | `RemoveDevice` |
| 启动时按配置文件批量装配 | `LoadFromConfigFile` |

## 8. 设备变更通知（引用失效的信号）

### 8.1 为什么有它

`BuildDevice` / `UpdateDevice` / 调试页「重建」都会**换实例**：旧实例被 `Dispose`。
长期持有 `IDevice` 的调用方（流程子类）手里的引用就失效了。

两种应对方式，库都支持：

| 方式 | 做法 | 适合 |
|---|---|---|
| **现取** | 每次使用前 `GetDevice(name)`（一次字典查找，成本可忽略） | 默认推荐 |
| **订阅** | `DeviceChanged += handler`，变更时自行重取 | 引用在多个方法间传递、现取不方便时 |

🔴 **不存在「跨重建永久有效的实例引用」**——实例会被释放，这是重建语义本身决定的。
事件只提供**失效信号**，不改变这一点。

### 8.2 事件契约

`IDeviceManager.DeviceChanged`，载荷 `DeviceChangedEventArgs { DeviceType, Name, Kind }`，
`Kind` = `Added`（新增）/ `Replaced`（同名重建）/ `Removed`（移除）。

五条约定：

1. **不含实例**：载荷只有类型、名字、种类。旧实例已释放，新实例也不该被缓存——要引用请自己 `GetDevice`。
2. **锁外触发**：不会在持有设备锁时回调，处理器里可以安全调用 `GetDevice`。
3. **异常隔离**：某个处理器抛异常不影响设备操作本身，异常记入 `IDeviceLogSink`。
4. **触发方线程**：UI 保存 = 工作线程；启动装配 = 启动线程。需要 UI 线程请自行 marshal。
5. **处理器内不要改设备**：不要调用 `BuildDevice` / `UpdateDevice` / `DeleteDevice`（会再产生变更）。

### 8.3 订阅写法

流程基座（`TaskProcess`）零构造依赖、不认识设备，所以设备经实现层的 `IProcessContext` 取用：

```csharp
internal sealed class WeldProcess : ProcessBase
{
    private const string DeviceName = "Weld_W03";
    private readonly IProcessContext context;
    private IDevice _welder;

    public WeldProcess(IProcessContext context)
    {
        this.context = context ?? throw new ArgumentNullException(nameof(context));

        // 🔴 订阅放构造，不放 InitProcess：InitProcess 每次 Initialize 都会重入，
        //    在里面 += 会累积重复处理器（基座不提供退订机制）。
        context.DeviceManager.DeviceChanged += OnDeviceChanged;
    }

    public override void InitProcess()
    {
        _welder = context.DeviceManager.GetDevice(DeviceName);
    }

    private void OnDeviceChanged(object sender, DeviceChangedEventArgs e)
    {
        if (!string.Equals(e.Name, DeviceName, StringComparison.Ordinal)) return;
        _welder = context.DeviceManager.GetDevice(DeviceName);   // Kind=Removed 时为 null
    }

    protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
    {
        if (_welder == null) return ProcessStepResult.Stay();   // 设备不在，等待
        // ...
        return ProcessStepResult.Next();
    }
}
```

⚠️ 事件是强引用：订阅方是长活对象（流程实例）时无须退订；短命对象记得 `-=`。

### 8.4 边界

- **不防竞态**：事件是事后通知。一次读写正好横跨重建瞬间，仍会拿到已释放的驱动。
- **不强制**：不订阅 = 回到现取的做法，库不替使用者做决定。

## 9. 词汇表

- **DeviceType**：设备类型标识，不透明字符串。取值是磁盘契约（与配置文件里的 `type` 字段一致），由实现层定义，底座不校验取值。
- **Section（配置节）**：设备类型 ↔ 根模型里某个列表的映射。新增设备类型 = 加一条 Section。
- **SyncHook（缓存同步钩子）**：配置变更后、建实例前，把新配置同步进驱动运行时缓存。只有缓存型驱动需要。
- **LogSink（日志出口）**：底座不引日志库，出问题时把异常交给外层记录。
- **变更通知**：`DeviceChanged` 事件，实例新增 / 替换 / 移除后触发；载荷只含类型、名字、种类，不含实例。
- **旧引用失效**：`BuildDevice` / `UpdateDevice` / `DeleteDevice` 之后，旧的 `IDevice` 引用已 Dispose，持有方必须重新 `GetDevice`。

## 10. 相关文档

- `Taoyl.Application.Business/README.md` —— 设备实现层：适配器、工厂、配置节声明（本库的"应用侧"）
- `Taoyl.Application.PlcDriver/README.md` —— PLC 驱动（`IDevice` 的主要实现来源）
- `Taoyl.Application.Communication/README.md` —— Modbus 主站（另一条设备通讯线）
- `Taoyl.UI.Pages/ViewModels/PlcDebugViewModel.Config.cs` —— 设备配置编辑与实例重建的界面链路
- `Taoyl.IFALaunch/Modules/DeviceModule.cs` —— 唯一的注册点（工厂扫描 + 配置加载器 + 配置存储）
