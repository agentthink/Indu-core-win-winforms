# Taoyl.Application.Business —— 业务实现库 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**业务实现库**：具体流程子类 + 具体设备实现 + 流程宿主设施（上下文 / 注册器 / 故障适配）+ 进程内事件总线实现。

**是什么**：具体业务流程（`Processes/`）；设备侧的**具体实现**（工厂 / 适配器 / 配置加载器 / 配置节注册表 / 驱动缓存同步钩子 / 日志出口）；流程子类的上下文（`IProcessContext`）；构造即注册的流程注册器（`ProcessRegistry`）；故障 → 报警的适配器（`ProcessFaultRouter`）；弱引用事件总线实现（`EventBus`，契约在 `Share.Eventing`）。

**不是什么**：不定义流程契约与引擎（在 `TaskProcess` 基座）、不定义设备契约与运行时（在 `Device` 基座）、不含业务语义之外的框架（引擎归基座）、不管 UI 呈现（监控数据走 `ProcessMonitoring` 与 `UI.Pages`）。

与近邻的分工：

| 库 | 角色 |
|---|---|
| `TaskProcess` | 流程**基座**：契约 + 五态引擎 + 编排器（零依赖，仅 Stateless） |
| `Device` | 设备**基座**：契约 + 运行时 + 通用配置设施（零依赖） |
| `FileUtil` | 通用文件能力（CSV 读写） |
| `ProcessMonitoring` | 流程监控与控制门面（只依赖 `TaskProcess` 契约，隔断 UI 对实现层的依赖） |
| **本库** | **业务实现**：引擎跑起来、设备接得上、故障落得下去 |

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| TaskProcess | 流程基座：契约（`IProcess` / `IProcessCoordinator`）+ 端口（`IProcessLogSink`）+ 模型（`ProcessState` / `ProcessTrigger` / `ProcessStatus` / `ProcessStepResult` / `ProcessBehaviorConfig` / `ProcessFault`）+ 引擎（`ProcessBase`）+ 编排（`ProcessCoordinator`） |
| Device | 设备契约（`IDeviceManager` / `IDeviceConfigManager` / `IDeviceOrchestration` / `IDeviceFactory` / `IDeviceConfigLoader` / `IDeviceConfigStore` / `IDeviceConfigSection` / `IDeviceConfigSyncHook` / `IDeviceLogSink`）+ 运行时（`DeviceManager`）+ 通用配置设施 |
| Alarm | `IAlarmService`（`ProcessFaultRouter` 调 Raise 落库）+ `AlarmRecordDto`（报警弹窗 / 查询页；流程侧快照已改 `ProcessFault`） |
| Product | `IProductCellInfoService`（产品落库测试流程的写入口，DTO 进出）+ `ProductCellInfoDto` |
| PlcDriver | `IPlcConfigManager`（驱动缓存同步 / 建实例）+ `PlcConfig` 等配置模型 |
| Share | `IEventBus` 契约（`Eventing` 域，本库提供实现） |
| LogDriver | `LogHelp`（写侧永不抛；两个日志端口的实现都落到这里） |
| FileUtil | `ICsvService`（流程子类经上下文取用） |
| Communication | Modbus 通讯（`ModBusTcpDeviceAdapter` / `ModBusTcpDeviceFactory`） |

被依赖：仅 `IFALaunch` 组合根。无类库引本库——本库是依赖链叶子。

🔴 基座（`TaskProcess` / `Device` / `Share`）**零 ProjectReference**（`Share` 连 NuGet 都不引）。

## 3. 结构

```
Taoyl.Application.Business/
├─ Processes/         流程子类（业务实现，会随工位增长）
├─ ProcessHosting/    流程宿主设施（不增长）
├─ Devices/           设备实现（工厂 / 适配器 / 配置 / 日志出口）
├─ Mes/               MES 调用记录与日志出口
└─ Eventing/          事件总线实现（跨域基础设施）
```

**划分判据**：`Processes/` 会增长（Phase 2 一工位一文件），`ProcessHosting/` 不会；混在一起，将来 20 个流程子类里夹着 6 个设施文件，每次找东西都要过滤。

## 4. 流程域

### 4.1 上下文：`IProcessContext`（子类唯一入口）

| 成员 | 用途 |
|---|---|
| `IDeviceManager DeviceManager` | 设备读写。长期持引用两种做法：**每步现取**，或缓存 + 订阅 `DeviceChanged` 置空 + `OnDispose` 退订；不存在跨重建永久有效的引用 |
| `IAlarmService AlarmService` | 业务报警直调 `Raise`（不经状态机门控） |
| `ICsvService CsvTools` | CSV 读写 |
| `IDictionary<string, object> GlobalState` | 跨流程共享字典（线程安全；键名无编译期约束） |

🔴 **成员准入规则**：只收「基础设施级能力」。业务级能力（数据库、MES HTTP、视觉、业务文件格式）**不进上下文**——那是具体流程自己的依赖，自己注入。判据：只有某一个流程用得上的东西，就不该在这里。

🔴 **设备订阅式写法**（示范 = `Processes/DeviceTestProcess`）：构造里 `DeviceChanged += OnDeviceChanged`（每实例只跑一次）→ 处理器只判名字 + 置空引用（跑在触发方线程，不做耗时操作）→ `CheckHealth` 空则重取 → `OnDispose` 里 `-=` 退订。**订阅与退订必须成对**，只订阅不退订会被单例设备管理器强引用，整机重建后永不回收。

### 4.2 注册器：`ProcessRegistry`（构造即注册）

`Init()` 在构造末尾自调——流程「出生即就绪」，启动后监控页即可见全部流程（待命态）。
同时订阅 `IProcessCoordinator.ProcessesNeedRebuild`（全量重建路径共用同一段 `Init()`）。

### 4.3 故障适配：`ProcessFaultRouter`

**构造即接线**：构造时订阅 `IProcessCoordinator.ProcessFaulted`，把故障转成 `AlarmRecordDto` 调 `AlarmService.Raise`（落库 → 总线广播 → 弹窗 / 查询页）。

⚠️ 删掉它的注册或 `Resolve` = 报警静默丢失（无编译 / 运行错误）。基座 `Register` 有接线自检，启动日志会记一条 Error。

### 4.4 日志出口：`LogHelpProcessLogSink`

把基座的 `IProcessLogSink` 落到 `LogHelp`。只承接协调器运维流水；流程故障不写日志（报警已落库）。

## 5. 设备域：`IDeviceOrchestration`（UI 与启动装配默认入口）

| 方法 | 语义 |
|---|---|
| `AddDevice` | 按配置新增（校验 / 查重 / 驱动缓存同步 / 构建实例） |
| `UpdateDevice` | 保存生效：释放旧实例 → 驱动缓存同步 → 配置 upsert → 重建 |
| `DeleteDevice` | 释放实例 + 驱动缓存清理 + 删配置节 |
| `LoadFromConfigFile` | 启动装配：配置镜像载入 + 按清单批量构建 |

⚠️ 编排类方法操作后**旧 `IDevice` 引用失效**，持有方必须重新 `GetDevice`（详见 `Device/README.md` §7~§8）。

## 6. 总线域：`IEventBus`（契约 `Share.Eventing`，实现 `Eventing/EventBus.cs`）

`Subscribe<T>(handler) → IDisposable 令牌`（🔴 令牌必须存字段）/ `Publish<T>(msg)`（同步分发、异常隔离、递归深度 10 保护）/ `GetHandlerCount<T>()` / `Clear<T>()` / `ClearAll()`。

用途：报警落库后广播（弹窗 / 查询页订阅）。**流程域已不用总线**——基座改为普通委托 `ProcessesNeedRebuild`。

## 7. 快速开始

容器注册（`IFALaunch/Modules/ProcessModule.cs`）：

```csharp
builder.RegisterType<ProcessContext>().As<IProcessContext>().SingleInstance();
builder.RegisterType<LogHelpProcessLogSink>().As<IProcessLogSink>().SingleInstance();
builder.RegisterType<ProcessCoordinator>().As<IProcessCoordinator>().SingleInstance();
builder.RegisterType<ProcessRegistry>().As<IProcessRegistry>().SingleInstance();
builder.RegisterType<ProcessFaultRouter>().SingleInstance();       // 构造即接线
builder.RegisterType<EventBus>().As<IEventBus>().SingleInstance();

// 所有流程子类自动注册（🔴 锚点必须是实现层程序集，见 §9.1）
builder.RegisterAssemblyTypes(typeof(ProcessRegistry).Assembly)
       .Where(t => t.IsSubclassOf(typeof(ProcessBase)) && !t.IsAbstract)
       .AsSelf()
       .InstancePerDependency();
```

启动触发（`IFALaunch/Bootstrapper.cs` `InitComponents`，🔴 唯一触发点，删掉即链路静默死亡）：

```csharp
Container.Resolve<ProcessFaultRouter>();    // 构造即订阅 ProcessFaulted → 报警落库
Container.Resolve<IProcessRegistry>();      // 构造尾完成全部流程注册（监控页数据源）
Container.Resolve<AlarmPopupPresenter>();   // 构造即订阅报警总线
```

最小流程子类：

```csharp
using System;
using System.Threading;
using Taoyl.Application.Business.ProcessHosting;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    internal sealed class MyProcess : ProcessBase
    {
        private readonly IProcessContext context;

        // 基座零参数，子类只收一个上下文
        public MyProcess(IProcessContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Category = ProcessCategory.Production;
        }

        public override void InitProcess()
        {
            ProcessName = "W01-C01";      // 唯一键，重名 Register 抛异常
            WaitTime = 200;
        }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            var plc = context.DeviceManager.GetDevice("PLC_OmronCip_R");   // 每步现取
            if (plc == null) return ProcessStepResult.Stay();

            return ProcessStepResult.Next();
        }
    }
}
```

## 8. 使用示例

| 场景 | 用什么 |
|---|---|
| 新增业务流程 | 继承 `ProcessBase`，只收一个 `IProcessContext`（见上），零注册配置 |
| 设备增删改 | `IDeviceOrchestration` 编排方法（操作后旧引用失效须重新 `GetDevice`） |
| 流程内发业务报警 | `context.AlarmService.Raise(...)`（不经状态机门控） |
| 流程内上报异常 | `ProcessStepResult.Exception(code, msg)`（进异常态 + 故障出口 + 联动暂停） |
| 跨流程共享状态 | `context.GlobalState`（优先走事件，确需共享时才用） |
| 导出 CSV | `context.CsvTools` |
| 订阅总线事件 | 构造注入 `IEventBus` → `Subscribe<T>` → **令牌存字段** |

## 9. 注意事项

1. 🔴 **扫描锚点必须是实现层程序集**：`ProcessBase` 在基座里，`typeof(ProcessBase).Assembly` 扫不到任何子类——**零注册且不报错**（监控页 0 行）。用 `typeof(ProcessRegistry).Assembly`。
2. 🔴 **`Bootstrapper.InitComponents` 是注册唯一触发点**：删 `Resolve` 或改启动序列 = 监控页空表、无流程，且不报错。
3. 🔴 **故障 Router 必须被 `Resolve`**：否则报警静默丢失（基座自检会在启动日志记 Error）。
4. 🔴 **状态机锁内禁 await**；状态唯一写入口 = `Fire`，禁直赋 `state`（细节见 `TaskProcess/README.md` §6）。
5. 🔴 **总线订阅令牌必须存字段**：局部变量被 GC 回收 = 订阅静默失效。
6. 🔴 **业务侧不依赖 `Raise` 后的 `record.Id`**：回填在消费线程，时序不保证。
7. ⚠️ 编排方法操作后旧 `IDevice` 失效，必须重新 `GetDevice`。
8. ⚠️ `ProcessName` 在 `InitProcess()` 里才赋值，`Register` 的查重必须在其后。
9. ⚠️ `Processes/` 下的 5 个自检流程（日志 / 报警 / MES / 设备通讯 / 产品落库）默认 `IsEnabled = false`，且开关只改运行时值不落盘：测哪个在监控页启用哪个，测完关掉，别接进生产启动序列。

## 10. 扩展指南

- **新增业务流程**：`Processes/` 下继承 `ProcessBase`（单参 `IProcessContext`）→ 实现 `ExecuteStep` → 程序集扫描自动注册 → 注册器 `Init()` 里 `Resolve` + `Register`。
- **新增设备类型**：`Devices/Models` 加配置模型 + `DeviceConfigSections` 加「模型 ↔ 节」映射 + 实现适配器与工厂（详见 `Device/README.md` §4）。
- **新增总线事件**：事件体定义在发布方域 → 发布方 `Publish<T>` → 订阅方构造注入 `IEventBus` 订阅并存令牌字段。
- **新增配置加载器**：实现 `IDeviceConfigLoader` → DI 注册，`DeviceManager` 经 `IEnumerable` 自动纳入。
- **全量重建路径**：`Coordinator.InitializeAllAsync` → 触发 `ProcessesNeedRebuild`（普通委托）→ `ProcessRegistry.OnProcessesNeedRebuild` → `Init()`——与启动注册共用同一段代码。

## 11. 相关文档

- `Taoyl.Application.TaskProcess/README.md`（流程基座：契约 / 引擎 / 编排 / 故障链路）
- `Taoyl.Application.Device/README.md`（设备基座：契约 / 运行时 / 配置设施）
- `Taoyl.Application.Alarm/README.md`（报警总线：Raise / Ack 与三态语义）
