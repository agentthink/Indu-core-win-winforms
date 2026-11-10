# Taoyl.Application.LogDriver

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

> .NET Framework 4.7.2 | C# 7.3 | 依赖：Taoyl.Application.Share / NLog 6.0.6 / SunnyUI

日志域类库：**NLog 落盘 = 完整性载体，UI 日志栏 = 实时状态指示灯**。提供 5 通道日志记录、事件订阅、CSV 落盘与虚拟化 UI 显示；落盘策略由配置中心（LogConfig）经代码 overlay 驱动，**目录分层与保留期清理由本库自管**。公共 API 不暴露任何 NLog 类型。

> 当前版本：UI 读路径换源（2026-10-07），**日志目录按年月分层 + 保留期由 `LogArchiveCleaner` 清理（2026-11-04）**。

---

## 1. 定位与边界

**是什么**：

- 工控日志域的完整闭环：业务写入 → NLog 异步落盘（CSV，通道分目录 + 年月分层）→ UI 实时状态栏
- 落盘策略可配置：根目录 / 编码 / 切分粒度 / 文件名前缀 / 保留天数，全部由 `LogConfig`（配置中心）驱动，代码 overlay 到 NLog
- 保留期自管：`LogArchiveCleaner` 启动时按**文件名日期**清理过期 csv 与空目录（NLog 的归档清理只扫当前一层，目录分层后够不着旧月份）
- 峰值承载 ≈ **1000 行/秒**（100 个任务线程 × 每线程 ≤10 条/秒）

**不是什么**：

- UI 日志栏**不是排障工具**：不做滚动回看 / 暂停 / 搜索 / 过滤 / 聚合——历史排查直接上落盘文件
- 不做结构化实体日志：所有信息由调用方转成字符串填入 message（实体链已摘除）
- 不做写失败弹窗暂停（K4 为后续议题，本期写失败静默 + internal log / emergency 留痕）
- **不管 MES 交互记录**：`MesLogSink` 写的 `{根目录}\Api\{接口名}\`（9 列完整报文）不归本库清理，它目前没有保留期

---

## 2. 依赖说明

| 依赖 | 为什么 | 借用姿势 |
|---|---|---|
| Taoyl.Application.Share | `ILogSettings` / `ILogCategoryPolicy` 中继契约 + `LogCategory` / `LogFileEncoding` / `LogSplitPeriod` 跨层公共词汇 | Configuration 不引本库、本库不引 Configuration ⇒ 走 Share 中继（`IAutoLogoutSettings` 同款先例） |
| NLog 6.0.6 | 落盘引擎：异步写入（AsyncWrapper）/ CsvLayout（RFC 4180）/ 按布局自动建目录 | 只在 `NLogLogService` / `LogTargetConfigurator` 内部使用，公共 API 零暴露；**归档清理不用它**（见 7.3-9） |
| SunnyUI 3.9.1 | `UIDataGridView`——SunnyUI 唯一具备虚拟化的列表容器（UIListBox 路线物理不可行） | 仅 `LogForm` UI 侧使用 |

> Newtonsoft.Json 已从本库移除（实体链摘除后零引用）；配置序列化由 Configuration 侧的 `JsonConfigSerializer` 负责。

---

## 3. 功能与 API

### 3.1 能力域

| 能力域 | 载体 | 说明 |
|---|---|---|
| 通道日志写入 | `ILogService` / `LogHelp` | 5 通道：Debug / Info / Warn / Error / MES；通道即方法名 |
| UI 实时显示 | `ILogDisplay` / `LogForm` | 虚拟模式 DataGridView，100ms 心跳，视口外自动冻结、10s 静止自动钉底 |
| 落盘策略配置 | `LogTargetConfigurator` | NLog.config 载入后按 `ILogSettings` overlay（根目录 / 编码 / 切分 / 前缀 / 目录分层） |
| 归档清理 | `LogArchiveCleaner` | 启动时按文件名日期删过期 csv 与空目录；`KeepDays = -1` 关闭该通道的清理 |
| 降级链路 | `NLogLogService` 内部 | 三层降级：L1 主配置 → L2 最小 File 配置 → L3 直接写盘 |
| 退出 flush | `ILogService.Shutdown()` | 进程退出前 flush 异步队列残留 |

### 3.2 ILogService 真实签名

```csharp
public interface ILogService
{
    // —— 通道日志（通道即方法名，级别不参与对外契约）——
    void Debug(string message, params object[] args);        // 落盘，UI 暂不显示
    void Info(string message, params object[] args);
    void Warn(string message, params object[] args);
    void Error(Exception ex, string message = null, params object[] args);  // ex 走 Exception 列
    void MES(string message, params object[] args);           // 记录级别恒 Info

    // —— 事件与生命周期 ——
    event EventHandler<LogEventArgs> LogWritten;              // 单一事件处理所有通道
    bool EnableEvents { get; set; }                           // 默认 true
    LogLevel MinimumEventLevel { get; set; }                  // 默认 Info；只影响 UI 事件链
    void Initialize(ILogSettings logSettings);                // NLog.config 载入 + overlay
    void Shutdown();                                          // 进程退出前 flush 异步队列
}
```

### 3.3 ILogDisplay 真实签名

```csharp
public interface ILogDisplay
{
    void Init(Dictionary<LogCategory, DataGridView> gridMappings = null);  // 类别 → 显示网格
    void SetUpdateInterval(int milliseconds);                // 10~1000ms 生效，定稿默认 100ms
    void SetCategoryColor(LogCategory category, Color color);
    void ClearCategory(LogCategory category);
    void ClearAll();
}
```

### 3.4 词汇表

| 类型 | 成员 | 说明 |
|---|---|---|
| `LogCategory`（Share.Logging） | Debug / Info / Warn / Error / MES | **通道**：决定落哪个目录、哪个文件 |
| `LogLevel`（本库 Models） | Debug=0 / Info=1 / Warn=2 / Error=3 | **级别**：记录的数据字段（CSV 的 Level 列），不参与对外契约 |
| `LogFileEncoding`（Share.Logging） | Utf8Bom（默认）/ Utf8 / Gbk | 文件编码；Utf8Bom 是 Excel 中文友好的前提 |
| `LogSplitPeriod`（Share.Logging） | Day（默认）/ Hour | 切分粒度：Day = 目录到月、文件名 8 位；Hour = 目录到日、文件名 10 位 |
| `KeepDays`（ILogCategoryPolicy） | -1 / ≥ 1 | 保留天数：-1 = 不清理；≥ 1 时超期文件由 `LogArchiveCleaner` 按文件名日期删除。`0` 非法 |

---

## 4. 快速开始

**注册**（`IFALaunch/Modules/LogModule.cs`，均已就绪无需改动）：

```csharp
builder.Register(c => NLogLogService.Instance).As<ILogService>().SingleInstance();
builder.Register(c => LogForm.Instance).As<ILogDisplay>().SingleInstance();
```

**配置预加载与初始化**（`Bootstrapper.InitConfigs`）：

```csharp
var cfg = Container.Resolve<IConfigService>();
cfg.Get<DevConfig>();
cfg.Get<BaseConfig>();
var log = cfg.Get<LogConfig>();        // 模板随程序分发（CopyConfigOnce）→ bin\config\LogConfig.json
MigrateLegacyLogPath(cfg, log);        // 一次性迁移：DevConfig.json 遗留 LogPath 键 → RootPath

Container.Resolve<ILogService>().Initialize(log);   // 内部：NLog.config 构造 → overlay → 赋值
```

**退出 flush**（`Program.Main`，`Application.Run` 返回后）：

```csharp
Bootstrapper.Container.Resolve<ILogService>().Shutdown();
```

---

## 5. 使用示例

### 场景选型表

| 场景 | 用法 |
|---|---|
| 业务过程记录 | `LogHelp.Info` / 构造注入 `ILogService` |
| 故障记录（带堆栈） | `LogHelp.Error(ex, "上下文")` —— ex 走 CSV 的 Exception 列 |
| MES 数据留痕 | `LogHelp.MES("...")` |
| 调试细节（只落盘） | `LogHelp.Debug("...")` —— UI 暂不显示 |
| UI 显示定制 | 注入 `ILogDisplay`（通常无需动手，LogForm 自管） |

### 场景 1：业务写入（静态门面，免 DI 直调）

```csharp
LogHelp.Info("设备 {0} 启动完成，耗时 {1}ms", deviceName, elapsed);

// ⚠️ args 走 NLog 结构化格式化：模板占位符与参数个数必须匹配，
//    写错时该条会进 emergency 留痕（fail fast，不静默吞）
```

### 场景 2：异常记录

```csharp
try
{
    DoSomething();
}
catch (Exception ex)
{
    // ✅ ex 作为独立参数：完整堆栈进 CSV 的 Exception 列（${exception:format=tostring}）
    LogHelp.Error(ex, "工位 {0} 下单失败", station);
    // ❌ 不要 LogHelp.Error(ex.ToString())：堆栈混进 Message 列，逗号/换行污染 CSV 结构
}
```

### 场景 3：UI 显示（页面 OnLoad 绑定，真实挂点 = FormMainPage）

```csharp
// FormMainPage.ConfigureLogForm（OnLoad，UI 线程）：
var gridMappings = new Dictionary<LogCategory, DataGridView>
{
    { LogCategory.Info,  gvInfoLog },
    { LogCategory.Error, gvErrorLog },
    { LogCategory.MES,   gvMESLog }
};
LogForm.Instance.Init(gridMappings);   // 未绑定的通道只落盘不显示（显式决策）
```

### 场景 4：修改落盘策略

1. 改 `bin\config\LogConfig.json`（如 `Info.KeepDays` 改 90；改 `-1` = 关闭该通道的清理）
2. **重启进程**（🔴 LogConfig 任何改动一律要求重启，不做热生效）
3. 启动时 `LogTargetConfigurator.Apply` 按新值 overlay 到全部 FileTarget；随后 `LogArchiveCleaner` 异步清一次

> `KeepDays` 只接受 `-1` 或 `≥ 1`。`0` 会让截止日停在今天、把今天之前的全部判过期（不是「不清理」而是「只留当天」）——启动校验与界面校验都拒绝。

---

## 6. 类库结构

| 文件 | 职责 | 关键点 |
|---|---|---|
| `Interfaces/ILogService.cs` | 服务契约 | 5 通道方法 + LogWritten 事件 + Initialize(ILogSettings) / Shutdown；零 NLog 类型 |
| `Interfaces/ILogDisplay.cs` | UI 显示契约 | Init / SetUpdateInterval / SetCategoryColor / ClearCategory / ClearAll |
| `Models/LogLevel.cs` | 级别枚举（4 成员） | 仅事件链内部使用；`ToNLogLevel` 映射 NLog |
| `Services/NLogLogService.cs` | NLog 实现 + 共享单例 | Initialize（甲路径 overlay）/ LogInternal（核心写入）/ TriggerEvent / 三层降级 / Shutdown |
| `Services/LogTargetConfigurator.cs` | 落盘策略配置器 | 两段式定位 FileTarget → overlay（fileName 分层 / 保留期关停 / 防残留清零 / 编码）+ 校验；静态无状态 |
| `Services/LogArchiveCleaner.cs` | 归档清理器（internal） | 按文件名日期删过期 csv：合法日期目录才进（剪枝）、只删本通道前缀形状、只删空目录；单出口异常政策 |
| `Services/LogArchiveLayout.cs` | 目录/文件名格式常量（internal） | 写入与解析同源：`PeriodFormats` / `Day·HourStampFormat` / `StampSeparator` / `CsvExtension` |
| `Models/LogCleanResult.cs` | 清理统计（internal，根命名空间） | 删除数 / 失败数 / 空目录数 / 文件名清单（≤20）/ 首个未预期异常 |
| `LogHelp.cs` | 静态兼容门面 | 转发到 `NLogLogService.Instance`，业务层免 DI 直调 |
| `LogForm.cs` | UI 显示管理器（单例） | 环形缓冲 + 视口快照 + 虚拟网格 + 100ms 心跳；实现 ILogDisplay |
| `Models/LogRingBuffer.cs` | 环形缓冲（internal sealed） | `LogEventArgs[]` 定长绕回：Count / TotalAppends / **CopyTo 整窗拷贝**（TryGet 已删，注释含布局图示）/ Clear |
| `Models/LogGridBinding.cs` | 网格绑定与渲染状态（internal sealed） | Grid / Category / Ring / **Snapshot / SnapshotCount** / LastSeenAppends / `int?` FreezeStartTicks / LastFreezeFirstRow |
| `Models/LogEventArgs.cs` | 事件参数 | **4 快照字段**：Category / Message / Timestamp / Level（namespace 保持 `Taoyl.Application.LogDriver` 根命名空间） |
| `NLog.config` | NLog 结构配置 | 5 target（CsvLayout 列结构唯一真值源）+ default-wrapper + 5 rule |

---

## 7. 内部逻辑

### 7.1 写入链路

```
业务线程：LogHelp.* / ILogService → NLogLogService.LogInternal
    → logger.Log(LogEventInfo) → AsyncWrapper 队列 → FileTarget 落盘（CSV，完整性载体）
    → TriggerEvent（EnableEvents && level ≥ MinimumEventLevel）
    → LogForm.OnLogWritten → LogRingBuffer.Append（只存事件引用，永不抛）
UI 线程：100ms 心跳 → 帧头 Ring.CopyTo 整窗拷贝渲染帧快照 → 行数同步/钉底/重绘
        → CellValueNeeded（取数/悬停全文/点击取数一律读 binding.Snapshot，不直读环）
        → FormatLine 就地格式化（状态指示灯；冻结期快照定格，重绘不拉新数据）
```

### 7.2 落盘文件形态

```
{RootPath}\
├── Debug\2026\09\Debug_20260926.csv      4 列：Time,Level,ThreadId,Message
├── Info\2026\09\Info_20260926.csv        4 列（同上）
├── Warn\2026\09\Warn_20260926.csv        4 列（同上）
├── Error\2026\09\Error_20260926.csv      5 列：同上 + Exception
├── MES\2026\09\MES_20260926.csv          4 列（同上）
├── Api\{接口名}\{接口名}_20260926.csv      MES 交互记录（9 列，MesLogSink 写，本库不清理）
└── logs\
    ├── internal-nlog.txt                 NLog internal 留痕（写失败看这里）
    ├── emergency.log                     L2：主配置失败时的最小配置输出
    └── emergency-fallback.log            L3：连 L2 都失败时直接写盘（10MB 限长）
```

按小时切分多一层日目录：`{类别}\2026\09\26\Info_2026092615.csv`（文件名 10 位）。
目录与文件名的格式常量都在 `LogArchiveLayout`；目录层级数 = `PeriodFormats.Length`。

### 7.3 关键设计决策

| # | 决策 | 为什么 |
|---|---|---|
| 1 | **通道即方法名**（B1） | 级别与通道是两个概念：通道定落盘位置，级别是记录的数据字段。对外契约不暴露级别参数 |
| 2 | **MES 恒 Info**（B2） | MES 只给人看，标 Error 会制造假告警 |
| 3 | **异步 + Block/50000** | `<targets async="true">` 是无参构造恒 10000/Discard（静默丢最老）⇒ 必须 `<default-wrapper>` 显式配 Block——K3「一条不许丢」优先，认下队列满时峰值退化同步 |
| 4 | **甲路径 overlay**（S0 实验 1） | `new XmlLoggingConfiguration → overlay → 赋值`；乙路径 `Reload()` 会重新解析 XML 把 overlay 冲掉 |
| 5 | **两段式定位 target**（S0 第 0 号） | `<default-wrapper>` 顶替 target 名（内层 FileTarget 被改名 `xxx_wrapped`）且 AllTargets 同时给出 wrapper 与 wrapped ⇒ 先按 `Target.Name` 命中 → 再 Unwrap 下钻 + HashSet 去重；`FindTargetByName<FileTarget>` 必失败 |
| 6 | **代码永不赋 `target.Layout`** | 锁死 CSV 后无任何赋值场景；赋值会踩 `TargetWithLayoutHeaderAndFooter.Layout` setter 分支 2（只换内层、外层 CsvLayout 仍在）的静默失效坑 ⇒ 列结构 = XML 唯一真值源 |
| 7 | **三层降级** | L1 主配置 → L2 最小 File 配置（正常 logger 名）→ L3 直接写盘（独立文件 + 锁 + 10MB 限长）。每层唯一写入者，结构消除「emergency 两个写入者互抢句柄」 |
| 8 | **规则 minlevel 全 Trace** | 级别不参与过滤——全 rule 不匹配 = 静默丢弃；Level 是 CSV 数据字段 |
| 9 | **保留期移交 `LogArchiveCleaner`**（2026-11-04） | 目录分层后 NLog 的清理只扫「当前文件所在那一层」（`Path.GetDirectoryName` + `GetFiles`，不递归），旧月份永远轮不到它，且失效不报错 ⇒ `MaxArchiveDays` 覆盖成 0、删除权整体移交本库（单一删除者） |
| 10 | **UI 虚拟化 + 只存事件引用** | 1000 行/秒下非虚拟模式必然闪烁（搬运病灶）；格式化在读路径（≤视口 25 行/帧），写路径连字符串拼接都省 |
| 11 | **Error 独享 Exception 列** | 只有 `Error(ex, msg)` 能携带异常，其余通道 API 无异常参数 ⇒ 该列恒空即删（4+1 列结构） |
| 12 | **读路径换源：整窗视口快照**（2026-10-07） | 单元格事件一律读 `binding.Snapshot`（心跳帧头 `Ring.CopyTo` 整窗拷贝，先读 count 后拷）：跟尾态快照 = 本帧、冻结态 = 定格帧 ⇒ 冻结期点击/遮挡/Resize/滚动重绘不拉新数据（内容跳变治本）；引用 memmove 零 GC 分配，快照数组构造一次终身复用；治域边界 = 只治冻结态（跟尾态点击跳变与 25ms 可读窗口同源，既有接受） |
| 13 | **布局常量单点**（`LogArchiveLayout`） | 写入（`BuildFileName`）与解析（`TryGetPeriodStart` / `TryParseStamp`）共用同一组格式常量，`MaxDepth = PeriodFormats.Length`：改格式只改一处、两侧同时生效。分散成两份字面量＝改一边不报错、清理静默停摆 |
| 14 | **判据是「归属」而不是「看起来像日期」** | 只删 `{本通道前缀}_{日期段}.csv` 这一个精确形状：别人的文件、被加 `_bak` 的、前缀对不上的一律不动。删除不可逆，宁可留垃圾也不误删 |
| 15 | **清理器单出口异常政策** | IO 类（权限 / 占用 / 并发删）就地接住、绝不中断整轮；其余异常只可能来自代码缺陷，冒到 `Clean` 顶层记一条带堆栈的 Error。不拿「吞掉一切」换表面安稳，也不拿「收窄成冒泡」破坏韧性 |
| 16 | **只在启动清一次** | LogConfig 改动必重启 ⇒ 配置变更天然被下次启动覆盖；进程长期不重启只表现成「晚几天清理」，不挂定时器、不引状态文件 |

---

## 8. 注意事项与 FAQ

### 🔴 铁律

| # | 铁律 | 后果 |
|---|---|---|
| 1 | **写路径永不抛**：LogInternal/TriggerEvent 全 catch，失败转 emergency | 100 个写线程的节拍红线；任何 throw 都会打断业务线程 |
| 2 | **UI 写路径只存事件引用**：LogRingBuffer.Append 禁 Invoke、禁格式化、禁字符串拼接 | 1000 行/秒下任何搬运都是闪烁病灶 |
| 3 | **FormatLine 只读快照字段**（Timestamp/Message/Category/Level） | 读活对象 = 同一行两次绘制内容不同（非确定性显示） |
| 4 | **代码永不赋 `target.Layout`** | CsvLayout 列结构 = XML 唯一真值源；赋值踩 setter 分支 2 = 切格式静默失效 |
| 5 | **冻结超时分支不可 `continue`**（LogForm.ShouldSkipForFreeze） | 清零计时后跳帧 ⇒ 恢复永不发生（第十一轮死循环根因，代码注释已写明） |
| 6 | **LogConfig 改动一律重启** | 幂等短路不做热生效（F3）；运行期重放 overlay = 配置双份真相 |
| 7 | **UI 读路径只读快照**：单元格事件（取数/悬停/点击）一律从 `binding.Snapshot` 取，**禁直读环形缓冲** | 直读环 = 冻结期重绘拉新数据 ⇒ 内容跳变（问题 1 根因）；快照的「所见即所存」语义不可回退 |
| 8 | **清理器只删「本通道前缀 + 日期段 + .csv」** | 判据放宽到「看起来像日期」就可能误删别人的文件；删除不可逆，宁可留垃圾 |

### ⚠️ 坑位 / 运维约定

| # | 事项 |
|---|---|
| 1 | 保留期按**文件名里的日期**判龄（不再看创建时间）：拷贝/恢复备份骗不过它；反过来，文件名被改过或前缀被改过就**再也不删**（判据是形状 + 归属） |
| 2 | Excel 只读打开当天 CSV **可行**（AutoFlush 每条都刷），但会挡住该文件的过期清理；关闭后下次写入即恢复 |
| 3 | Hour 模式：单日目录 24 个文件、单月目录 ≈ `24 × 当月天数`；清理只遍历文件名，毫秒级 |
| 4 | 换扩展名 / 换文件名前缀后**旧文件永不被清理**（形状判据变了）⇒ 上线时手工清一次 |
| 5 | 写失败留痕看 `logs\internal-nlog.txt`（internalLogLevel=Warn）；主配置坏了看 `logs\emergency.log`（L2） |
| 6 | `emergency-fallback.log` 出现 = 连 L2 都设不进去，属严重信号，需人工介入 |
| 7 | 消息模板占位符与参数不匹配 ⇒ 该条进 emergency（fail fast，不静默降级） |
| 8 | `LogCategory` 在 Share（新 5 成员）；本库 `Models.LogLevel` 是级别（4 成员）——两者名字相近但语义不同（通道 vs 级别） |
| 9 | `KeepDays = -1` = 该通道**不清理**（配置页标签「保留天数(-1=不清理)」）：误设后日志无限增长且不告警，改完记得回填 |
| 10 | 布局迁移残留：老的平铺文件（`{类别}\Info_2026xxxx.csv`）仍在类别根、形状匹配 ⇒ 照常清理；老的 `MES\{接口名}\` 目录不是日期目录 ⇒ **永远不进**，需手工迁移 |

---

## 9. 扩展指南

### A. 新增一个日志通道（如 Audit 变更流）

1. `Share/Logging/LogCategory.cs` 加枚举成员
2. `Configuration/Models/LogConfig.cs` 加固定属性 + `GetPolicy` 加 case（编译期穷尽检查会强制同步）
3. `NLog.config` 加 target（CsvLayout）+ rule（`minlevel="Trace"`）
4. `LogTargetConfigurator.GetTargetName` 加 case
5. `ILogService` / `NLogLogService` / `LogHelp` 加方法组
6. 需要显示则 `FormMainPage` 加网格绑定（否则只落盘）
7. 注意：`LogForm.InitializeRings` 走 `Enum.GetValues` 自动跟随，无需改
8. 目录/文件名格式一律走 `LogArchiveLayout`，**不要在任何地方另写一套日期格式字符串**——清理器按它解析，写错一边就是「日志写了但永不清理」

### B. 配置中心「日志策略页」（后续议题）

`ConfigService.Get<LogConfig>()` 与 UI 无关，页面直接编辑 LogConfig 对象即可（`ConfigBase` 的 SetProperty/SetDirty 链路已就绪）；改完**必须提示用户重启**。

### C. K4 写失败弹窗并暂停软件（后续议题）

前置证据已在档：异步下写失败感知路径全部失效（continuation 同线程判定 + ProcessPendingEvents 的 catch 只接致命异常），唯一可行 = 自建包装 Target（拦截点 `WriteAsyncThreadSafe`，注意 `WrapperTargetBase.Write` 是 sealed）。
