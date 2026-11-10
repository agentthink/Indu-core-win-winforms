# Taoyl.Application.TaskProcess —— 流程基座 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**流程基座库**：流程契约 + 流程引擎 + 编排器。

**是什么**：流程的最小公共面（`IProcess : IDisposable`）；五态状态机引擎（`ProcessBase`，Stateless 单层五态 + looper 线程 + `OnDispose` 释放点）；流程生命周期编排（`ProcessCoordinator`：注册 / 批量控制 / 聚合状态 / 故障汇聚 / 释放）；故障载荷（`ProcessFault`）；日志端口（`IProcessLogSink`）。

**不是什么**：不认识报警库（故障经事件交给外层落库）、不认识日志库（经端口）、不认识设备 / CSV / 上下文（那是实现层的事）、不认识任何具体流程子类（注册器在外层）、不碰 UI。

**硬边界**：**零 ProjectReference**。唯一 NuGet 依赖是 `Stateless`（状态机）。基座内两个类的构造依赖分别是 **0 个**（`ProcessBase()`）和 **1 个**（`ProcessCoordinator(IProcessLogSink)`）。

## 2. 结构

| 目录 | 内容 |
|---|---|
| `Interfaces/` | `IProcess`（流程契约）/ `IProcessCoordinator`（编排契约）/ `IProcessLogSink`（日志端口） |
| `Models/` | `ProcessStatus.cs`（ProcessState / ProcessTrigger / ProcessCategory / SystemProcessState / ProcessBehaviorConfig / ProcessStatus）/ `ProcessStepResult.cs` / `ProcessFault.cs` |
| `Services/` | `ProcessBase`（引擎）/ `ProcessCoordinator`（编排） |

## 3. 运行时装配

| 组件 | 构造依赖 | 谁提供 |
|---|---|---|
| `ProcessBase` | **无**（零参数） | — |
| `ProcessCoordinator` | `IProcessLogSink` | 外层实现（`LogHelpProcessLogSink`），组合根注册 |

组合根还须 **Resolve 一个故障消费者**（把 `ProcessFaulted` 接到报警落库），否则流程故障不会落库。基座对此有自检：首次 `Register` 时若无订阅方，记一条 Error。

## 4. 故障链路（本库最重要的约定）

```
ProcessBase.TriggerException(code, message)
    │  ① 锁内：CanFire(RaiseException) → Fire → 构造 ProcessFault 赋 CurrentException
    │  ② 锁外：RaiseFaulted(fault)          ← 🔴 必须在锁外（订阅方会做 DB I/O）
    ▼
ProcessBase.Faulted（internal 事件，只有同程序集的 Coordinator 能订阅）
    ▼
ProcessCoordinator.OnProcessFaulted
    ├─① RaiseProcessFaulted → ProcessFaulted（public，基座对外唯一出口）
    │        ▼  外层 Router（实现层）
    │     AlarmService.Raise(AlarmRecordDto) → 落库 → 总线广播 → 弹窗 / 查询页
    └─② PauseOthersOnFault(source) → 联动暂停其他 Running/Paused 且允许人工暂停的流程
```

**三条硬约定**：

1. **`Faulted` 是 `internal`**：外部拿不到单实例的故障，只能经 `IProcessCoordinator.ProcessFaulted`。出口唯一，不会出现「有人绕过协调器直接接流程」。
2. **锁外派发**：`TriggerException` 在锁内只写状态与载荷，事件在锁外发。订阅方会做报警落库（DB I/O），持 `stateGate` 派发会阻塞一切状态读写。
3. **订阅由基座自己完成**：`ProcessCoordinator.Register` 里 `source.Faulted += OnProcessFaulted`，不靠外部纪律。漏挂 = 静默丢报警，这种事不能交给调用方。

**故障码约定**：

| 码 | 来源 |
|---|---|
| `E000` | 基座内部错误（looper 创建失败；在 `Start()` 的锁外 catch 里报） |
| `E001` | 子类 `CheckHealth()` 返回 false |
| `E999` | looper 循环内未捕获异常 |
| 业务自定义 | 子类 `ProcessStepResult.Exception(code, msg)` |

## 5. 新增一个流程：完整清单

| # | 动作 | 位置 | 要点 |
|---|---|---|---|
| 1 | 继承 `ProcessBase`，实现 `ExecuteStep` | 实现层 `Processes/` | 返回 `Next/Stay/Jump/Complete/Exception` 之一 |
| 2 | `InitProcess()` 里设 `ProcessName` | 同上 | **唯一键**，重名在 `Register` 时抛异常 |
| 3 | 构造声明自己需要的能力 | 同上 | 基类零参数，子类想要什么自己加（实现层通常只收一个 `IProcessContext`） |
| 4 | 实现 `OnDispose()` | 同上 | 退订事件 / 释放自有资源；没有可释放的写空方法体（抽象强制过一遍这个判断） |
| 5 | 容器注册 | 组合根 | 自动扫描：锚点必须是**实现层程序集**（见 §8） |
| 6 | 注册器 `Init()` 里 Resolve + `Register` | 实现层 | `Register` 内含 `InitializeAsync`，失败会抛 |

### 可照抄的最小流程

```csharp
using System.Threading;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    internal sealed class MyProcess : ProcessBase
    {
        // 基类零参数：子类要什么自己声明（这里不依赖任何东西）
        public MyProcess() { }

        public override void InitProcess()
        {
            ProcessName = "W01-C01";      // 唯一键：工位-通道
            Category = ProcessCategory.Production;
            WaitTime = 200;               // 步序间隔（毫秒）
        }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            switch (step)
            {
                case 10:
                    // 业务动作
                    return ProcessStepResult.Next();

                case 20:
                    if (!SomethingReady())
                        return ProcessStepResult.Stay();        // 保持本步，下一拍重试

                    if (Failed())
                        return ProcessStepResult.Exception("E201", "采集超时");   // 进异常态 + 上报 + 联动暂停

                    return ProcessStepResult.Next();

                default:
                    return ProcessStepResult.Complete();        // 正常收尾（状态自转 Stopped）
            }
        }

        // 健康检查：false → E001（子类不重写则恒 true）
        protected override bool CheckHealth() => true;

        // 释放点（基类 abstract，必须实现）：退订事件 / 释放自有资源。
        // 本流程什么都没订，写空方法体——抽象强制实现就是为了让每个新流程过一遍这个判断。
        protected override void OnDispose() { }
    }
}
```

## 6. 五态与不变式

```
Idle      --Start---------> Running     唯一启动入口，建 looper 线程
Stopped   --Start---------> Running     重启（跑过之后）
Running   --Pause---------> Paused      步间暂停（本步执行完才停）
Running/Paused --RaiseException-> Exception   步序异常 / 健康检查失败 / 循环异常统一入口
Paused    --Resume-------> Running      放行门控
Exception --Reset--------> Paused       软恢复：步序不归零，从异常步重试
Running/Paused/Exception --Stop|Finish--> Stopped   杀线程并等待 looper 退出
Stopped   --Initialize---> Idle         重新初始化回待命（两段式第二段）
```

| # | 不变式 |
|---|---|
| 1 | **状态即线程生死**：线程死则状态必停（looper 退出必经 `Finish` 自转 → Stopped） |
| 2 | **状态唯一写入口 = 状态机 `Fire`**（`stateGate` 锁内 `CanFire → Fire`），任何路径禁直赋 |
| 3 | **`stateGate` 锁内禁 await**：looper 收尾自转要拿同锁，锁内等待必死锁（`StopAsync` = 锁内 Fire + 锁外 await） |
| 4 | **进 Idle 唯一路径 = 两段式 `Initialize`**：先 `StopAsync` 杀线程（→ Stopped）再 `Fire(Initialize)` |
| 5 | **`Reset` = 软恢复**：步序不归零，清异常，从异常步重试（既定行为，勿改为「归零」） |
| 6 | **锁外派发故障**：`TriggerException` 锁内只写状态与载荷 |
| 7 | **`ProcessName` 在 `InitProcess()` 里赋值**：`Register` 的查重必须放在 `InitializeAsync` 之后，否则全部撞成 `"Unnamed"` |
| 8 | **`Dispose` 前必须 `StopAsync`**：活线程上释放 = 退了订阅但 looper 还在跑（`ClearAllProcesses` 已按「先 `StopAllAsync` 再释放」实现） |

## 7. 日志分工（别把故障写成日志）

| 事件性质 | 去哪 | 为什么 |
|---|---|---|
| 流程故障（E001/E999/E000/业务异常） | `ProcessFaulted` → 报警落库 | 需人处理、要留痕、可能联动停机 |
| 协调器运维流水（注册、初始化、重建） | `IProcessLogSink` Info/Warn | 追溯「谁在何时做了什么」，不需人处理 |
| 协调器操作失败（暂停失败、批量失败） | `IProcessLogSink` Error | 多为程序缺陷；走报警会污染报警表并弹窗 |
| `ProcessBase` 自身 | **一条不记** | 状态机没有可追溯的业务信息 |

## 8. 潜规则（不看会踩）

1. **`Faulted` 是 `internal`**：只有同程序集的 `ProcessCoordinator` 能订阅。**不要把它改成 public**——那会让外部绕过协调器直接接流程，出口就不唯一了。
2. **`ProcessBase` 与 `ProcessCoordinator` 必须同程序集**：靠的是 `internal` 可见性。若将来把 Coordinator 拆出去，这里会编译不过（这是有意的护栏）。
3. **扫描锚点必须落在实现层程序集**：`ProcessBase` 在本库，`typeof(ProcessBase).Assembly` 扫不到任何子类——**零注册且不报错**。正确写法 `typeof(ProcessRegistry).Assembly`（或任意实现层类型）。
4. **`Register` 的查重顺序**：`InitializeAsync` → `TryAdd` → 失败抛。顺序反了会让第 2 个流程就撞车。
5. **`ProcessFaulted` 必须有人订阅**：否则故障只进 `Trace`。基座在首次 `Register` 时自检并记 Error。
6. **`ProcessCoordinator` 只持有流程实例的强引用**：`ClearAllProcesses` 后旧实例可被 GC——前提是流程**没有反过来订阅长活对象**。两个方向要分清：`Faulted` 是「协调器订阅流程」（流程消亡即随之解绑，不阻止回收）；`DeviceChanged` 是「流程订阅单例」（会阻止回收，见第 7 条）。
7. **订阅放构造、退订放 `OnDispose`**：放 `InitProcess` 会随每次 Initialize 重入累积处理器；只订阅不退订 = 单例订阅源强引用流程实例，整机重建后永不回收。
8. **`ClearAllProcesses` 有副作用**：它逐个 `Dispose` 再清表。调用前必须已 `StopAllAsync`（`InitializeAllAsync` 内部已保证顺序，不要单独调它）。
9. **回调线程不是 looper 线程**：`DeviceChanged` 这类事件在触发方线程（如 UI 保存的工作线程）派发，处理器里只做引用赋值之类的一步操作；要重连请另起 `Task`，且失败自己判（`Task.Run` 的异常不会冒泡回调用方）。

## 9. 词汇表

- **ProcessName**：流程唯一键（= 工位-通道，如 `W03-C02`）。重名即错误，`Register` 抛异常。
- **ProcessState**：五态，状态即线程生死（Idle/Stopped 无线程，Running/Paused/Exception 有线程）。
- **ProcessTrigger**：八触发器（Start/Pause/Resume/Reset/Stop/Finish/RaiseException/Initialize）。
- **ProcessFault**：故障载荷（Source/Code/Message/OccurredAt），继承 `EventArgs` 直接当事件参数。
- **Faulted**：`ProcessBase` 的 internal 事件，单实例故障出口（只给 Coordinator）。
- **ProcessFaulted**：`IProcessCoordinator` 的 public 事件，基座对外唯一故障出口。
- **ProcessesNeedRebuild**：全量重建请求（普通委托，取代原 `EventBus + AllProcessesNeedInitEvent`）。
- **ProcessStepResult**：步序执行结果（Next/Stay/Jump/Complete/Exception）。
- **OnDispose**：`ProcessBase` 的抽象释放点（对应 `IProcess : IDisposable`），子类在此退订事件 / 释放自有资源；由 `ProcessCoordinator.ClearAllProcesses` 触发，幂等。
- **BehaviorConfig**：`PauseOthersOnException`（异常联动暂停，默认 true）/ `AllowManualPause`（允许人工暂停，也是「是否参与聚合状态」的过滤器）/ `IsEnabled`。

## 10. 相关文档

- `Taoyl.Application.Business/README.md`（实现层：具体流程子类、`IProcessContext`、故障 Router、注册器）
- `Taoyl.Application.Device/README.md`（设备基座，同构先例）
