# Taoyl.Application.Alarm —— 报警库 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

报警 = **设备/流程状况记录与响应**（第三根横切支柱）：一条报警 = 发生（入库）→ 呈现（弹窗分诊）→ 处理（人看过）三时刻，全部落 `sys_alarm_records` 单表。

**是什么**：通用报警总线——任何模块注入 `IAlarmService` 直调 `Raise` 即一等报警来源；UI 订阅事件总线上的已落库记录做分通道弹窗，历史查询走服务的查询方法。

**不是什么**：不管控制联动（异常联动暂停由 ProcessCoordinator 消费报警总线实现）；不做恢复判定（第一版无 Clear，列留口二期再谈）；不做报警码登记表（自由编码）；不做自动清理。

与近邻的分工：LogDriver = 全量流水（CSV，无人交互）；Audit = **人**的操作留痕；Alarm = **机器**的状况记录（需人响应、有处理语义）。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| Share | `IEventBus`（落库完成后广播 `AlarmRecordDto`；总线为跨域广播设施，契约住 Share.Eventing，实现在 Business/Eventing，组合根注册单例） |
| DataBase | `SqlSugarHelper`（Lazy Db，构造期不触发建库）+ **Mapster 10.0.13**（经基座 PackageReference 传递供货） |
| Share | `EnumExtensions.GetDescription` |
| LogDriver | `LogHelp.Error`（写侧永不抛） |

被依赖：Business（`ProcessFaultRouter` 调 `Raise` 落库——基座不认识报警库；异常联动暂停由 `ProcessCoordinator.PauseOthersOnFault` 在基座内完成，不再订阅 `AlarmRecordDto`）；UI.Pages（Presenter 弹窗 + 查询页）。

🔴 基座（Share / DataBase / LogDriver）**零反向依赖**。**不依赖 TaskProcess**（10-12：IEventBus 契约迁 Share.Eventing，Alarm 与流控内核脱钩——为 1 个接口背整个内核即依赖倒置缺失；同批删 IProcessContext.EventBus 挂件，内核零 ProjectReference 保持）。

### 数据访问边界（2026-10-14）

四层压扁进模块：`UI/VM ──DTO──> IAlarmService ──> AlarmService（实体域→DTO 域唯一转换边界，Mapster Adapt）──> IAlarmRepository（实体进出）──> AlarmRepository（SqlSugar 收口）`。

- **实体 `AlarmRecord` 的可见域 = 仓储层 + 服务方法栈**；服务接口 / 总线广播 / 流程域快照 / UI 一律 `AlarmRecordDto`。
- 服务不出现 SqlSugar 类型（含 using），泄漏在结构上不可能；仓储接口签名实体进出。

## 3. 功能与 API

### IAlarmService（UI 与业务的唯一入口，DTO 进出）

| 方法 | 签名 | 语义 |
|---|---|---|
| Raise | `void Raise(AlarmRecordDto alarm)` | 入队即返回（微秒级零 IO）；**RaiseTime 入队时刻补**，Id 落库后抄回原 dto；满容量(1000)丢弃+LogError，永不抛 |
| Ack | `bool Ack(AlarmRecordDto record)` | 按 Id 同步直写补 HandleUser/HandleTime，永不抛（低频人工动作，Audit 规范）；返回 false = 落库失败（调用方提示并留存重试） |
| Query | `List<AlarmRecordDto> Query(AlarmQueryFilter filter)` | 条件查询（产生时间倒序），超 5 万行抛异常提示缩小范围 |

### IAlarmRepository（实体进出，只供 AlarmService 使用，UI 不注入）

`int Insert(AlarmRecord)`（自增 Id 回填进实体）/ `int Update(AlarmRecord)`（按主键全列更新，返回受影响行数）/ `List<AlarmRecord> Query(AlarmQueryFilter)`——插入/更新/查询全在仓储接口上，由 AlarmService 调用；UI 只拿 IAlarmService，结构上够不到仓储。

`AlarmQueryFilter` 只有两维：`TimeFrom / TimeTo`（DateTime?）、`Type`（**AlarmType? 可空枚举**，文案转换在仓储 BuildQuery 一次性完成——静态方法禁入表达式树）。无分页维度：Query 一次拉全量（上限保护），分页由查询页 VM 内存分页（照流程监控页先例）。

### 模型与枚举

- `AlarmRecord`（**落库实体**）：`sys_alarm_records` 表行；列注释归 `[SugarColumn(ColumnDescription=...)]`。只由仓储使用，出仓储层即转 DTO。
- `AlarmRecordDto`（**DTO**）：进出服务接口 / 总线广播 / 流程域快照 / UI。与实体全字段同构（9/9），成员 `Id / RaiseTime / Type(文案) / Code / Source / Message / ActionGuide(可空,≤200) / HandleUser(可空) / HandleTime(可空)`；`[Description]` = 查询页/弹窗/导出中文列头。🔴 出现在总线上即已落库（Id 真值）。
- `AlarmType`：**三态**——提示(1)/警告(2)/异常(3)，[Description] 文案即存储契约（列宽 20）：2026-10-12 命名定稿（原「记录/提示」框架期作废），投产起不得改，只可新增。通道语义：**提示=自关弹窗不暂停**（确认可 Ack 留痕）/ **警告=常驻待确认不暂停** / **异常=常驻待确认+流程联动暂停**；展示策略（配色/标题/自关/置顶）收敛在 AlarmDialogViewModel 通道策略字典，新增类型=字典加一行。
- `AlarmEnumText`：枚举↔文案（Of 正向取文案 / TryParse 文案反查，缺 [Description] 降级枚举名，静态建表缓存）。

## 4. 快速开始

容器注册（`IFALaunch/Modules/AlarmModule.cs`）：

```csharp
builder.RegisterType<AlarmRepository>().As<IAlarmRepository>().SingleInstance();
builder.RegisterType<AlarmService>().As<IAlarmService>().SingleInstance();
```

Mapster 装配（`IFALaunch/Mapping/MappingBootstrapper.cs`，任何 Adapt 之前）——本模块程序集必须进 Scan 清单，否则启动期 `Compile()` 报「未登记」：

```csharp
cfg.RequireDestinationMemberSource = true;
cfg.RequireExplicitMapping = true;
cfg.Scan(
    typeof(AuditMappingRegister).Assembly,
    typeof(AlarmMappingRegister).Assembly);   // ← 本模块
cfg.Compile();
```

最小示例（任意模块直调）：

```csharp
// 构造注入 IAlarmService alarmService
alarmService.Raise(new AlarmRecordDto
{
    Source = "相机1",
    Type = AlarmEnumText.Of(AlarmType.Exception),
    Code = "CAM001",
    Message = "取像超时",
    ActionGuide = "检查相机连接与光源"
});
// 入队即返回；RaiseTime 服务补；消费线程落库后总线广播（Id 已抄回原 dto）
```

UI 侧订阅 + 处理：

```csharp
// 令牌必须存字段（IEventBus 契约）
token = eventBus.Subscribe<AlarmRecordDto>(OnAlarmPersisted);

private void OnAlarmPersisted(AlarmRecordDto record)
{
    uiContext.Post(ShowOnUi, record);   // 只做切线程，微秒级返回
}

// 弹窗确认命令内：HandleUser/HandleTime 在 dto 上补齐后传 Ack
record.HandleUser = userSession?.CurrentUser?.Username;
record.HandleTime = DateTime.Now;
alarmService.Ack(record);
```

## 5. 使用示例

| 场景 | 用什么 |
|---|---|
| 流程异常报警 | 无需调用——基座 `ProcessFaulted` → `ProcessFaultRouter` → `Raise`（`ProcessBase` 不再直连报警） |
| 增量模块业务报警 | `Raise(new AlarmRecordDto{...})`，Type 经 `AlarmEnumText.Of(AlarmType.X)` 取文案（手写中文绕过枚举 = 脱离契约） |
| 弹窗处理 | `Ack(dto)`（FormAlarmDialog 确认按钮 → AlarmDialogViewModel.Confirm） |
| 历史查询/导出 | `IAlarmService.Query(filter)`（全量，上限保护；分页由查询页 VM 内存分页） |

## 6. 类库结构

| 文件 | 职责 |
|---|---|
| Interfaces/IAlarmService.cs | 服务契约（Raise/Ack/Query，**DTO 进出**） |
| Interfaces/IAlarmRepository.cs | 仓储契约（Insert/Update/Query，**实体进出**） |
| Models/Entities/AlarmRecord.cs | 落库实体（SugarTable 直打 sys_alarm_records） |
| Models/Dtos/AlarmRecordDto.cs | 服务/总线/流程域/UI 契约（9 字段，[Description] 列头） |
| Models/Dtos/AlarmQueryModels.cs | AlarmQueryFilter（时间范围 + 类型两维） |
| Models/AlarmType.cs | 三态枚举（文案即存储契约） |
| Models/AlarmEnumText.cs | 枚举↔文案映射（Of/TryParse） |
| Mapping/AlarmMappingRegister.cs | 实体↔DTO 映射登记（Mapster IRegister，**双向** 9/9 同构：读侧 Entity→DTO、写侧 DTO→Entity） |
| Services/AlarmService.cs | 队列 + 消费线程 + Ack 直写 + 查询转发（单例） |
| Repository/AlarmRepository.cs | SqlSugar 实现（实体进出） |

## 7. 内部逻辑

```
调用方(任意线程) ──Raise(dto)──> 补 RaiseTime ──> BlockingCollection(1000, 满则丢弃+LogError)
                                 │ IsBackground 消费线程（唯一写者）
                                 ▼
                     dto.Adapt<AlarmRecord>() → INSERT(ExecuteReturnBigIdentity)
                                 │ Id 抄回原 dto（保快照同步反映落库值）
                     eventBus.Publish<AlarmRecordDto>（出现在总线上即已落库）
                                 │
              ┌──────────────────┼──────────────────────┐
              ▼                  ▼                      ▼
   AlarmPopupPresenter    ProcessCoordinator        未来订阅者（MES 等）
   Post 切 UI 线程分诊     Type=异常才联动暂停
   (提示=自关弹窗/         PauseOthersOnException
    警告=常驻确认/
    异常=常驻确认+联动暂停)
   点确认 → dto 补 HandleUser/HandleTime → Ack(dto) → Adapt 实体 → UPDATE 全列
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 实体 `AlarmRecord` + DTO `AlarmRecordDto` 分离 | 死规则：实体可见域 = 仓储层 + 服务方法栈；服务接口 / 总线 / 流程域 / UI 一律 DTO |
| 映射**双向**登记 | 读侧 Query 走 `Entity→DTO`、写侧 ConsumeLoop / Ack 走 `DTO→Entity`，两个方向都要显式登记；`RequireExplicitMapping` 下漏登反向 = 写侧整链抛 `CompileException`（首版即中此招） |
| 消费线程「抄回原 dto」而非 new | 报警弹窗 / 查询页与总线订阅方共享该实例，须同步反映落库 Id（流程域快照已改 `ProcessFault`，不再共享） |
| 仓储 `Update(entity)` 全列、不具名拆参 | 设计决策：具名 `UpdateHandle(id,user,time)` 在实体加字段时要改签名；收实体则零成本（代价 = 调用方须传完整实体） |
| UI 只拿 IAlarmService | Insert/Update 在仓储接口上，服务不漏仓储引用——结构封死绕服务直写 |
| INSERT 走消费线程，Ack 直写 | INSERT 高频（looper 零 IO 红线）；Ack 低频人工动作，排队纯多余一跳 |
| 弹窗数据源 = 落库后广播 | 弹的必已存 + Id 真值回写零竞态 |
| 查询条件只留时间范围+类型两维 | 全字段可查是过度设计，需要再加 |
| 满容量丢弃不阻塞 | TryAdd：报警泛滥时拖死 looper 是灾难，丢弃+日志是唯一保底 |
| RaiseTime 入队时刻补 | 发生时间=调用时刻，与落库时刻无关 |

## 8. 注意事项

1. **写必须过 IAlarmService**：仓储别在 UI/业务注入使用。
2. **Type 落库文案只经 AlarmEnumText**：文案即存储契约，投产不得改，只可新增成员。
3. **进总线 handler 微秒级返回**：总线分发跑在 AlarmService 消费线程上（含 Coordinator 联动暂停），重活自理（UI 侧 Post 切线程）。
4. **业务侧不依赖 Raise 后的 dto.Id**：Id 抄回在消费线程，时序不保证——要真 Id 请订阅总线。
5. **`Ack` 走 `Update(entity)` 全列写回**：传入 dto 必须字段完整（来自总线广播/查询的完整记录 + 补齐处理两字段）；只 new 部分字段的 dto 去 Ack 会清空其余列。
6. 三态弹窗语义：提示=空闲 5s 自关（清空不 Ack，确认可 Ack 留痕）；警告/异常=常驻待确认（确认 Ack 留痕，异常另触发流程联动暂停）。均非模态置顶，三窗可并存。
7. 启动窗口期（Presenter 激活前）的报警入库但不弹，历史可查。

## 9. 扩展指南

- **新增报警来源模块**：csproj 引 Alarm → 构造注入 IAlarmService → Raise。零配置。
- **新增报警类型**：AlarmType 加成员 + [Description]（≤20 字）→ AlarmDialogViewModel 通道策略字典加一行注册（展示策略+数据源一次声明）→ 运行即生效。只加不改；漏注册时 State() 抛异常哨兵。
- **查询加维度**：AlarmQueryFilter 加字段 + AlarmRepository.BuildQuery 拼条件，两处同步。
- **新增 DTO 字段**：`AlarmRecordDto` 与实体同步加（保持同构）→ 映射核对自动通过；服务接口/总线/UI 签名零改动（收 DTO）。
- **二期接 Clear**：接口加 `Clear(source, code)`，消费线程 UPDATE（同队列保序）。
- **MES 转发**：订阅 `eventBus.Subscribe<AlarmRecordDto>`，handler 内入自己的发送队列。

## 10. 相关文档

- `Taoyl.Application.Audit/README.md`（查询页 / 仓储化 / DTO 契约先例，母本）
