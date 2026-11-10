# Taoyl.Application.ProcessMonitoring —— 流程监控门面 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**UI 与流程引擎之间的一层薄门面**：把"界面上能点的动作"和"界面上要看的状态"固定成一份接口，UI 只认这份接口，不认 `IProcessCoordinator`。

**是什么**：`IMonitorService` 契约（批量控制 5 个、单流程控制 5 个、监控 2 个、流程配置 3 个）、`MonitorService` 实现（每个方法一行转发到 `IProcessCoordinator`）。

**不是什么**：不含任何流程逻辑（状态机、线程、异常联动全在 `Taoyl.Application.TaskProcess`）、不持有流程实例、不做设备管理、不做 MES 通讯。

**为什么不直接让 UI 用 `IProcessCoordinator`**：协调器是流程域的编排口（`Register` / `ClearAllProcesses` / `StopAllAsync`），这些能力 UI 不该碰。门面把"能给的"和"不该给的"划成一份独立契约 —— 结构上让 UI 拿不到越界能力。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| `Taoyl.Application.TaskProcess` | `IProcessCoordinator`（唯一实现依赖）、`ProcessStatus`（状态快照）、`SystemProcessState`（4 态聚合）、`ProcessBehaviorConfig`（行为配置） |

被依赖：`Taoyl.UI.FormMain`（主界面按钮、状态栏）、`Taoyl.UI.Pages`（流程监控页）。

**纯转发，无第三方包**。

## 3. 功能与 API

### `IMonitorService`（`IMonitorService.cs`）

**批量控制（主界面按钮）**

| 方法 | 说明 |
|---|---|
| `void StartProcess()` | 启动全部流程（`coordinator.StartAll()`） |
| `void PauseProcess()` | 暂停全部 |
| `void ResumeProcess()` | 继续全部 |
| `void ResetProcess()` | 复位全部（仅清异常，不自动继续） |
| `Task InitializeProcessAsync()` | 初始化全部（进 Idle 的唯一路径） |

**单流程控制（流程监控页）**

| 方法 | 返回 | 说明 |
|---|---|---|
| `bool StartProcess(string processName)` | 找不到流程返回 `false` | 单个启动 |
| `bool PauseProcess(string processName)` | 同上 | 单个暂停 |
| `bool ResumeProcess(string processName)` | 同上 | 单个继续 |
| `bool ResetProcess(string processName)` | 同上 | 单个复位 |
| `Task InitializeProcessAsync(string processName)` | — | 单个初始化 |

**监控**

| 方法 | 返回 | 说明 |
|---|---|---|
| `SystemProcessState GetSystemState()` | `Idle` / `Running` / `Paused` / `Exception` | 系统级聚合状态（`Exception` 优先级最高） |
| `IReadOnlyCollection<ProcessStatus> GetAllProcessStatus()` | 每个流程一份快照 | 流程监控页的表格数据源 |

**流程配置（运行时内存修改）**

| 方法 | 说明 |
|---|---|
| `bool SetProcessEnabled(string processName, bool enabled)` | 禁用后不参与系统状态聚合 |
| `bool SetProcessPauseOthersOnException(string processName, bool value)` | 异常时是否暂停其他流程（默认 true = 安全模式） |
| `ProcessBehaviorConfig GetProcessConfig(string processName)` | 取行为配置（找不到返回 null） |

### 边界声明（接口注释里写死的三条）

| 不归本门面 | 归属 |
|---|---|
| 设备清单与连接状态 | 设备域 —— UI 直接注入 `IDeviceManager` 读 `AllDevices`（只读列表，无需编排能力） |
| MES 交互日志 | 日志域 —— 统一走 `ILogService.MES()` 通道（原 `GetMesInfoIn/Out` 已删） |
| 流程注册与清理 | 组合根 —— `Register` / `ClearAllProcesses` 只在 `Bootstrapper` 调用 |

## 4. 快速开始

容器注册（`Taoyl.IFALaunch/Modules/ProcessModule.cs`）：

```csharp
builder.RegisterType<MonitorService>().As<IMonitorService>().SingleInstance();
```

注入使用：

```csharp
public class MainViewModel
{
    private readonly IMonitorService monitor;

    public MainViewModel(IMonitorService monitor) => this.monitor = monitor;

    public void OnStartClick()  => monitor.StartProcess();
    public void OnPauseClick()  => monitor.PauseProcess();
    public void OnResetClick()  => monitor.ResetProcess();
}
```

## 5. 使用示例

### 5.1 主界面三个按钮（状态与按钮可用性联动）

```csharp
// 系统态只用于状态指示灯（4 态聚合，Exception 优先级最高）
SystemProcessState state = monitor.GetSystemState();

// 按钮可用性不要自己按状态硬编码，用状态机给出的合法触发器：
// PermittedTriggers 由 ProcessBase 在锁内求值，与运行时同源，规则扩展后自动跟随
var status = monitor.GetAllProcessStatus().First(p => p.ProcessName == "MainProcess");
startEnabled  = status.PermittedTriggers.Contains(ProcessTrigger.Start);
pauseEnabled  = status.PermittedTriggers.Contains(ProcessTrigger.Pause);
resetEnabled  = status.PermittedTriggers.Contains(ProcessTrigger.Reset);
```

🔴 不要用 `switch (state)` 硬编码按钮可用性 —— 那等于在 UI 里复制一份状态机规则，规则一改就两边打架。`PermittedTriggers` 是唯一数据源。

### 5.2 流程监控页：逐行控制

```csharp
foreach (ProcessStatus status in monitor.GetAllProcessStatus())
{
    // 快照字段：ProcessName / Category / State / PermittedTriggers / Step / ExceptionCode / ExceptionMessage
    row.Name         = status.ProcessName;
    row.StateText    = status.State.ToString();
    row.StartEnabled = status.PermittedTriggers.Contains(ProcessTrigger.Start);
    row.PauseEnabled = status.PermittedTriggers.Contains(ProcessTrigger.Pause);
    row.ResetEnabled = status.PermittedTriggers.Contains(ProcessTrigger.Reset);
}

// 行内按钮
if (!monitor.StartProcess("MainProcess"))
    messageBox.ShowWarning("流程不存在，或当前状态不允许启动");
```

### 5.3 运行时改行为配置

```csharp
monitor.SetProcessEnabled("测试-设备通讯", enabled: false);   // 自检流程默认停用，不参与状态聚合
monitor.SetProcessPauseOthersOnException("MainProcess", value: true);
```

🔴 只改**内存中的 `BehaviorConfig`**，不落盘。要持久化得自己写配置文件（当前没有这个需求）。

### 5.4 初始化（进 Idle 的唯一路径）

```csharp
await monitor.InitializeProcessAsync();          // 全部
await monitor.InitializeProcessAsync("MainProcess");   // 单个
```

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| `IMonitorService.cs` | 门面契约（15 个方法）+ 三条边界声明 |
| `MonitorService.cs` | 实现（纯转发 + null 防御） |

## 7. 内部逻辑

```
UI（FormMain / FormProcessMonitor）
   ↓ 只认 IMonitorService
MonitorService（本库：翻译 + 空值防御）
   ↓
IProcessCoordinator（Taoyl.Application.TaskProcess）
   ├─ StartAll / PauseAll / ResumeAll / ResetAll / InitializeAllAsync
   ├─ GetProcess(name) → IProcess.Start / Pause / Resume / Reset / GetStatusSnapshot
   ├─ GetSystemState() → SystemProcessState（4 态聚合）
   └─ Processes → 逐个 GetStatusSnapshot()
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 单独建门面，不让 UI 直接用 `IProcessCoordinator` | 协调器带 `Register` / `ClearAllProcesses` / `StopAllAsync` 这类编排能力，UI 拿到就能把流程表清空；门面把可见子集固定下来，越界在结构上不可能 |
| 方法都是**同步**（除初始化） | 流程的 Start/Pause/Reset 本身是状态切换（内部起线程但立即返回），做成 `Task` 只会让 UI 到处 `await` 一个已完成的任务 |
| 单流程方法返回 `bool`，批量方法返回 `void` | 单个操作可能因为"名字不存在 / 状态不允许"失败，调用方要提示；批量操作的失败由流程内部记日志，UI 不需要逐条处理 |
| `GetAllProcessStatus` 返回快照集合 | 直接暴露 `IProcess` 会让 UI 能反向调用流程方法，绕过门面；快照是纯数据 |
| 配置修改只改内存 | 现场需求是"临时停某个流程观察"，不是"永久改配置"；落盘会引入"改了配置但没重启"的不一致 |
| 设备状态不进门面 | 设备清单属设备域，UI 注入 `IDeviceManager` 只读即可；塞进门面会让本库同时依赖设备域，边界变糊 |

## 8. 注意事项与 FAQ

🔴 **`StartProcess()`（无参）与 `StartProcess(name)` 是完全不同的语义**：前者批量、后者单体。重载看起来一样，误用时表现为"只想启一个，结果全启动了"。

🔴 **单流程方法返回 `false` 有三种可能**：流程名不存在、流程当前状态不允许该操作、流程被禁用。UI 提示要写清楚，不要只说"操作失败"。

🔴 **`Exception` 状态下必须先 `Reset` 才能 `Start`**。这是流程基座的语义（复位 = 清异常回到 Paused，不自动继续），门面只是透传。

⚠️ **`SetProcessEnabled` 的禁用不停止正在运行的流程**，只把它排除在状态聚合之外。要真正停下用 `PauseProcess(name)`。

⚠️ **`GetProcessConfig` 可能返回 null**（流程名不存在），调用方要判空。

⚠️ **本门面不做线程封送**。`GetAllProcessStatus` 在后台线程调用也能拿到快照，但更新 UI 仍需 `Invoke`。

**Q：这么多一行转发的方法，是不是过度设计？**
不是。判据是"能不能少"：直接给 UI `IProcessCoordinator` 能少 15 个方法，但会多出 `Register` / `ClearAllProcesses` 两个越界口子。用 15 个转发换一个结构约束，划算。

**Q：为什么 `InitializeProcessAsync` 是异步，其他都是同步？**
初始化要建 looper 线程并等它起来，是真异步；`Start/Pause/Reset` 只是状态切换（Stateless 触发器），同步即可。

**Q：主界面的设备状态从哪来？**
`IDeviceManager.AllDevices`（`Taoyl.Application.Device`），与本门面无关。

## 9. 扩展指南

**A. 新增一个"UI 需要但门面没有"的流程能力**

1. 先判断归属：是流程域的编排能力，还是 UI 的展示需求？
2. 展示需求 → 在 `ProcessStatus` 里加快照字段（`Taoyl.Application.TaskProcess`），门面无需改
3. 控制需求 → `IMonitorService` 加方法 + 实现里一行转发
4. 不要为了"以后可能用到"提前加 —— 每加一个方法，门面就多一份要维护的承诺

**B. 新增一个流程子类后需要改这里吗？**

不用。流程注册在 `Bootstrapper.InitProcesses()`，门面按名字操作，不认识具体类型。

**C. 要做"流程组"（按产线段批量控制）**

1. 分组信息放流程模型（`ProcessBehaviorConfig` 加 `GroupName`）
2. 门面加 `StartGroup(string groupName)`，实现里筛 `coordinator.Processes` 后逐个调
3. 不要改 `StartAll` 的语义 —— 那个是"全部"，加分组参数会破坏调用方

## 10. 相关文档

- `Taoyl.Application.TaskProcess/README.md` —— 流程基座（状态机、五态语义、异常联动）
- `Taoyl.Application.Business/README.md` —— 具体流程子类（`DeviceTestProcess` / `ProductTestProcess` 等）
- `Taoyl.UI.FormMain/README.md` —— 主界面按钮与状态联动
- `Taoyl.IFALaunch/Modules/ProcessModule.cs` —— 唯一的注册点与流程注册链
