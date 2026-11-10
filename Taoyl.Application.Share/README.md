# Taoyl.Application.Share —— 契约基座 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**零项目依赖的公共词汇层**：只放"两个模块都要认识、但这两个模块不能互相引用"的类型。

**是什么**：认证事实快照（`AuthFact`）、权限码常量（`PermissionCodes`）、日志通道与策略契约（`LogCategory` / `ILogSettings` / `ILogCategoryPolicy`）、事件总线契约（`IEventBus`）、数据库类型枚举（`DatabaseType`）、配置切片契约（`IAutoLogoutSettings` / `ICardLoginSettings`）、路径常量（`ConfigPathName`）、枚举扩展（`EnumExtensions`）。

**不是什么**：不实现任何逻辑（唯一有方法体的是 `EnumExtensions.GetDescription` 这个纯反射工具）、不引用任何项目或 NuGet 包、不认识数据库/UI/日志实现。

它只有一个职责：**打断横向依赖**。

## 2. 依赖说明

| 依赖 | 说明 |
|---|---|
| 无 | 仅 `net472` 框架程序集。**零 ProjectReference、零 PackageReference** |

被依赖：20 个项目中的 **12 个**。不引的 7 个各有用意：`TaskProcess` / `Device` / `Business` / `Communication` / `FileUtil` / `MesDriver` / `ProcessMonitoring` —— 要么刻意保持零依赖（TaskProcess 是流程基座，Communication / FileUtil 是纯工具库），要么只认自己领域内的契约（Device / Business / MesDriver）。

🔴 这条是硬约束：一旦 Share 引了任何项目或包，它就不再是中立的公共词汇层，而是某个模块的附属。新增依赖前先问：这个类型能不能放到引用方自己的模块里？

## 3. 功能与 API

### 3.1 Auth —— 认证事实与权限码

**`AuthFact`**（登录域向外广播的只读快照）

| 成员 | 类型 | 说明 |
|---|---|---|
| `Kind` | `AuthFactKind` | `LoginSucceeded = 1` / `LoginFailed = 2` / `Logout = 3` |
| `Username` | `string` | 本次尝试/生效的用户名；登录失败且未提供时为"未知用户" |
| `RoleNames` | `List<string>` | 角色名快照（**登录失败时为空集合**） |
| `MaxRoleLevel` | `int` | 最高角色等级快照（**登录失败时为 0**） |

🔴 身份字段必须在**事实发生时点**采集。登出后消费方若回头读 `IUserSession`，会读到空或下一位用户 —— 那是假账。

🔴 刻意不带时间戳：事件在业务方法内同步触发，接手时刻即事实发生时刻。将来若出现异步投递需求再补 `OccurredAt`。

**`PermissionCodes`**（权限码常量集中定义）

命名规则：`Page:页面` / `Btn:页面_功能`。

| 分组 | 示例 |
|---|---|
| 页面 | `Page:MainPage` / `Page:DataQuery` / `Page:PLCDebug` / `Page:Audit` |
| 主界面流程 | `Btn:Process_Start` / `Btn:Process_Pause` / `Btn:Process_Reset` |
| 流程监控 | `Btn:ProcessMonitor_Start` / `Btn:ProcessMonitor_InitAll` / `Btn:ProcessMonitor_ToggleEnabled` |
| 用户/角色管理 | `Btn:UserManager_Add·Edit·Delete·Import·Export`、`Btn:RoleManager_Add·Save·Delete·Reset` |
| 参数设置 | `Btn:ParamSystem_Edit·Save·Reset`（System / Base / Log 三组同构） |
| 配方 | `Btn:Recipe_New·Edit·Save·Delete·Switch·Import·Export` |
| PLC 调试 | 20 个码，按动作逐个设（`AddPlc` / `EditPlc` / `SavePlc` / `CancelPlc` / `DeletePlc` / `RebuildPlc` / `Connect` / `Read` / `Write` / `Import` / `Export` / `EditTag` / `AddRow` / `MoveUp`…） |
| 数据出库 | `Btn:Audit_Export` / `Btn:Alarm_Export` / `Btn:Product_Export`（**能查 ≠ 能导**，导出单独设码） |
| MES 调试 | `Btn:MesDebug_Add·Edit·Save·Cancel·Delete·Send`（**发送单独设码**：保存只改本地，发送真打到生产系统） |

### 3.2 Config —— 配置切片契约

| 类型 | 成员 | 对应实现 |
|---|---|---|
| `DatabaseType` | `MySql = 0` / `Sqlite = 1`，带 `[Description]` | 配置层与数据访问层共用同一份枚举（配置层不能引 SqlSugar） |
| `IAutoLogoutSettings` | `int AutoLogoutTime { get; }` | `DevConfig` 实现 |
| `ICardLoginSettings` | `EnableCardLogin` / `CardInputLength` / `KeyInputTimeout` | `AutoLoginConfig` 实现 |

这三个都是**配置切片**：消费方只想要两三个属性，不该被迫认识整个 `DevConfig`。

### 3.3 Eventing —— 事件总线契约

| 成员 | 签名 | 说明 |
|---|---|---|
| 订阅 | `IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class` | 返回令牌，**必须存字段**；令牌随订阅方 GC 回收时订阅自动失效 |
| 发布 | `void Publish<TEvent>(TEvent eventMessage) where TEvent : class` | 同步、按订阅顺序调用；**单个 handler 异常被隔离**，不影响其他订阅者 |
| 计数 | `int GetHandlerCount<TEvent>()` | 当前存活订阅数（诊断用） |
| 清空 | `void Clear<TEvent>()` / `void ClearAll()` | 手动清理 |

实现在 `Taoyl.Application.Business/Eventing/EventBus.cs`（弱引用 + 令牌），**不在本库** —— Share 只放契约。

### 3.4 Logging —— 日志通道与策略

| 类型 | 内容 |
|---|---|
| `LogCategory` | 5 成员：`Debug = 0` / `Info = 1` / `Warn = 2` / `Error = 3` / `MES = 4` |
| `ILogSettings` | `string RootPath` / `LogFileEncoding Encoding` / `ILogCategoryPolicy GetPolicy(LogCategory)` |
| `ILogCategoryPolicy` | `Category` / `SplitPeriod` / `KeepDays`（-1=不清理）/ `FileNamePrefix` |
| `LogSplitPeriod` | `Day = 0`（`yyyyMMdd`）/ `Hour = 1`（`yyyyMMddHH`），带 `[Description]` |
| `LogFileEncoding` | `Utf8Bom = 0` / `Utf8 = 1` / `Gbk = 2` |

这是本库最典型的**中继契约**：`Configuration` 侧的 `LogConfig` 实现 `ILogSettings`，`LogDriver` 侧消费它 —— 两者互不引用，只共同认识 Share。

### 3.5 Paths —— 路径常量

`ConfigPathName`：`DevicesFile`（`Config/Devices.json`）/ `MenuFile`（`Config/Menu.json`）/ `MesFile`（`Config/Mes.json`）/ `AuthBackupDir` / `LogsFolder` / `PLCLogFile` 等。

所有相对路径都以 `AppDomain.CurrentDomain.BaseDirectory` 为基准拼接。

### 3.6 Extensions —— 枚举扩展

`EnumExtensions.GetDescription(this Enum)`：优先 `[Description]`，无则回退枚举名。全仓 `[Description]` 的中文显示统一走它。

## 4. 快速开始

```xml
<!-- 任何需要共用词汇的项目，直接加项目引用即可，无需注册、无生命周期 -->
<ProjectReference Include="..\Taoyl.Application.Share\Taoyl.Application.Share.csproj" />
```

没有容器注册、没有初始化、没有配置 —— 全是编译期可见的类型。

## 5. 使用示例

### 5.1 发布/订阅认证事实（跨域解耦的标准姿势）

```csharp
// AuthService 侧：只认识 AuthFact，不认识审计
eventBus.Publish(new AuthFact
{
    Kind = AuthFactKind.LoginSucceeded,
    Username = username,
    RoleNames = roleNames,
    MaxRoleLevel = maxLevel,
});

// Audit 侧：只认识 AuthFact，不认识 AuthService
subscription = eventBus.Subscribe<AuthFact>(fact =>
    auditService.Record(BuildEntry(fact)));      // 令牌存字段，不要用局部变量
```

### 5.2 中继契约：配置实现、日志消费

```csharp
// Configuration 侧（LogConfig : ConfigBase, ILogSettings）
public ILogCategoryPolicy GetPolicy(LogCategory category) => category switch { ... };

// LogDriver 侧：只认 ILogSettings，不认 LogConfig
public void Initialize(ILogSettings settings)
{
    string root = LogTargetConfigurator.ResolveRoot(settings.RootPath);
    foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
    {
        ILogCategoryPolicy policy = settings.GetPolicy(category);   // 加类别不改接口
        ...
    }
}
```

### 5.3 权限码：只在菜单与界面用字符串，业务侧用常量

```csharp
// Menu.json 里写字符串
{ "Code": "Btn:Product_Export", "Name": "导出产品数据", "Level": 300 }

// 代码里用常量，避免拼错
if (!authService.HasPermission(PermissionCodes.BtnProductExport)) return;
```

### 5.4 枚举显示名

```csharp
LogSplitPeriod.Hour.GetDescription();     // "按小时"
DatabaseType.Sqlite.GetDescription();     // "SQLite"
```

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| **Auth/** | |
| `AuthFact.cs` | `AuthFactKind` 枚举 + `AuthFact` 事实快照 |
| `PermissionCodes.cs` | 全部权限码常量（页面 + 按钮） |
| **Config/** | |
| `DatabaseType.cs` | 数据库类型枚举（2 成员，带 `[Description]`） |
| `IAutoLogoutSettings.cs` | 自动注销切片 |
| `ICardLoginSettings.cs` | 刷卡登录切片 |
| **Eventing/** | |
| `IEventBus.cs` | 事件总线契约（5 方法） |
| **Logging/** | |
| `LogCategory.cs` | 5 通道枚举 |
| `LogSplitPeriod.cs` | 切分粒度枚举 |
| `LogFileEncoding.cs` | 编码枚举 |
| `ILogSettings.cs` | 日志全局设置契约 |
| `ILogCategoryPolicy.cs` | 单通道策略契约 |
| **Paths/** | |
| `ConfigPathName.cs` | 配置文件与目录常量 |
| **Extensions/** | |
| `EnumExtensions.cs` | `GetDescription` |

## 7. 内部逻辑

### 中继模式（本库存在的唯一理由）

```
        ┌───────────── Taoyl.Application.Share ─────────────┐
        │  ILogSettings / ILogCategoryPolicy / LogCategory  │
        └───────▲───────────────────────────────▲───────────┘
                │ 实现                          │ 消费
   Taoyl.Application.Configuration      Taoyl.Application.LogDriver
        （LogConfig 实现契约）              （只认契约，不认 LogConfig）
```

没有 Share 的话只有两条路：配置层引日志层（配置被日志绑架），或日志层引配置层（日志库再也无法独立复用）。Share 让两者都只依赖一个空壳。

同样的形状还有三处：

| 场景 | 提供方 | 消费方 | 中继类型 |
|---|---|---|---|
| 登录事实 | AuthService | Audit | `AuthFact` |
| 日志策略 | Configuration | LogDriver | `ILogSettings` / `ILogCategoryPolicy` |
| 自动登出配置 | Configuration | UI.FormMain | `IAutoLogoutSettings` |

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 零项目依赖、零 NuGet | 一旦引了包，它就从"公共词汇"变成"某模块的附属"；所有模块都被迫接受那个包的版本 |
| 只放类型，不放实现 | 事件总线的弱引用实现、日志的 NLog 落盘都在各自模块；Share 里放实现会让所有引用方被迫带上依赖 |
| `AuthFact` 带快照字段而非用户 Id | 消费方在异步/延迟场景下拿 Id 回查会读到已变更的数据；快照是唯一不会撒谎的形态 |
| 登录失败时 `RoleNames` 为空、`MaxRoleLevel = 0` | 身份确实未验证，借用当前在线用户的角色就是假账 |
| `ILogSettings.GetPolicy(category)` 用查询式而非集合属性 | 加通道不改接口；实现侧用 5 个固定属性保证编译期不漏 |
| 权限码常量集中在 Share | 页面（UI）、服务（AuthService）、同步（PermissionSyncService）三方共用一份，拼错编译期就报 |
| PLC 调试页按动作逐码而非归并 | 归并码把权限粒度绑死在实现分组上，现场想给"能改配置但不许删设备"留口子时无处下手 |
| 导出类权限独立设码 | "能查"和"能导"是两种风险，数据出库要单独授权 |

## 8. 注意事项与 FAQ

🔴 **`IEventBus.Subscribe` 返回的令牌必须存字段**。用局部变量接，方法一返回令牌就被 GC 回收，订阅静默消失（不报错，只是收不到事件）。

🔴 **`LogCategory` / `DatabaseType` / `AuthFactKind` 的数值是契约**，落库/落盘/配置里存的就是这些值。加成员只能追加，不能改已有值或顺序。

🔴 **`PermissionCodes` 的字符串就是数据库里的权限码**。`Menu.json` 的 `Features[].Code` 与之逐字对应，改常量等于改库里的数据 —— 要么同时迁移，要么别改。

⚠️ **`AuthFact` 不带时间戳是刻意的**，不是遗漏。见 §1 与 §3.1。

⚠️ **Share 里唯一有方法体的是 `EnumExtensions`**。它是纯反射无状态工具，可以接受；除此之外不要往这里加任何带逻辑的类型。

⚠️ **`ConfigPathName` 里全是相对路径**，用之前必须拼 `AppDomain.CurrentDomain.BaseDirectory`。

**Q：为什么不干脆做一个 `Taoyl.Application.Contracts` 大而全的契约工程？**
试过这条路：契约工程会不断膨胀成"所有模块的公共依赖"，任何一处改动都触发全解决方案重编译，且新人无法判断"这个类型该不该放进来"。当前的判据很硬：**两个模块都要认识、且不能互相引用**——不满足就不进 Share。

**Q：`PermissionCodes` 里的 `BtnProcessMointorInit` 拼写是不是错的？**
是历史拼写错误（`Mointor`），但它已经写进数据库权限表了。改名要迁移数据，暂不改 —— 用的时候直接引用常量，不要手写字符串。

**Q：能不能在 Share 里加个 `Result<T>` 统一返回类型？**
不要。`OperateResult` 在 Communication、`ResponseResult` 在 PlcDriver、`MesResult` 在 MesDriver —— 三套结果类型各自服务不同的错误语义（通讯错误码、PLC 错误码、HTTP 状态码）。强行统一会造出一个什么都能装但什么也表达不清的壳。

## 9. 扩展指南

**A. 新增一个中继契约（最常见）**

1. 先确认：是不是两个模块都要认识，且这两个模块不能互相引用？
2. 在对应域下建接口（如 `Logging/IXxxSettings.cs`），只放 `{ get; }` 属性
3. 提供方实现它（如 `Configuration` 的 `LogConfig`）
4. 消费方只注入接口，构造参数类型写成契约而不是实现
5. 组合根负责把实现注册成该接口（`builder.RegisterType<LogConfig>().As<ILogSettings>()` 之类）

**B. 新增权限码**

1. `PermissionCodes` 加常量，命名遵守 `Page:页面` / `Btn:页面_功能`
2. `Menu.json` 的 `Features[].Code` 写同一字符串
3. 重启 → `PermissionSyncService` 同步进权限表 → 角色管理页授权
4. 若已有角色需要默认拥有，要在同步逻辑里处理（新增码默认不授权给任何角色）

**C. 新增日志通道**

1. `LogCategory` 追加成员（**只能往后加，不能插队**）
2. `ILogSettings` 实现类补一个固定属性
3. `LogDriver` 的 `LogTargetConfigurator` 与 NLog.config 补对应 FileTarget
4. 顺序不能反：先加枚举再改实现，否则 `Enum.GetValues` 遍历时找不到策略

## 10. 相关文档

- `Taoyl.Application.Configuration/README.md` —— `ILogSettings` / `IAutoLogoutSettings` 的实现方
- `Taoyl.Application.LogDriver/README.md` —— `ILogSettings` 的消费方
- `Taoyl.Application.AuthService/README.md` —— `AuthFact` 的发布方与 `PermissionCodes` 的使用方
- `Taoyl.Application.DataBase/README.md` —— `DatabaseType` 的消费方
- `Taoyl.Application.Business/Eventing/EventBus.cs` —— `IEventBus` 的唯一实现
