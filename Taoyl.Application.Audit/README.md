# Taoyl.Application.Audit — 审计留痕类库

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

工业上位机操作留痕库（谁、什么时候、在哪个页面、做了什么、数据改了什么）。

## 1. 定位与边界

- **审计 = 操作留痕，不是数据变更追踪**——记录点 = 操作发生地 = UI 命令层；审计永不高于业务。
- 记的是「人的一次操作」：头表一行 = 一次操作，明细 0..N 行 = 该操作改了哪些字段。
- **不是**数据同步 / 消息队列 / 业务校验组件：投递失败不阻断业务、不重试、不回填。
- 后端服务类（ConfigService / RecipeService / DeviceConfigStore / UserService）**不认识本契约**；唯一例外是登录域——但不直接投递，经 `AuthFactRecorded` 事件由 UI 层接线（见 §5 场景 5）。

## 2. 依赖说明

仅依赖 **DataBase + Share**（+ Newtonsoft.Json + Mapster）。**不依赖任何业务模块**（AuthService / LogDriver / Configuration 零依赖）：

| 依赖切面 | 做法 | 为什么 |
|---|---|---|
| 身份 | `AuditActor` 快照（UserInfo 的 3 字段最小切割）随请求带入，服务不反向读会话 | 依赖方向恒为 `消费方 → 审计`，结构上不可能产生依赖环 |
| 数据库 | Service 不摸库，注入 `IAuditRepository` / `IAuditQueryRepository`（仓储内注入 `SqlSugarHelper`，其 Db 是 Lazy，构造期不触发 DB 创建） | Service 禁出现 SqlSugar 类型（含 using），泄漏在结构上不可能；仓储形态买"依赖隔离的结构确定性" |
| 日志降级 | 直落本地 jsonl，不走 ILogService | ①避免反向依赖 LogDriver ②NLog FormatMessage 对花括号抛 FormatException（已踩过的坑） |
| 登录事实 | 消费 `Share.Auth.AuthFact` 事件（零依赖契约） | 登录域与审计互不认识，只共同认识 Share 里的契约 |
| 映射 | Mapster **10.0.13**（经 DataBase 基座 `PackageReference` 统一传递供货）；`Mapping/AuditMappingRegister.cs` 声明，组合根 Scan + Compile 装配 | 实体域→DTO 域唯一转换边界的载体；严格成员校验把"改名静默丢值"堵在启动期 |

## 3. 功能与 API

### 能力域（4 行）

| 能力 | 契约 | 一句话 |
|---|---|---|
| 写侧投递 | `IAuditService` | 投递一条记录；同步直写，返回即落地或已降级，**永不抛** |
| diff 比对 | 同上 | 取基线 / 字段级比对 / 集合逐项差异，产出 `AuditFieldChange` 明细 |
| 查询 | `IAuditQueryService` | 五维筛选分页 + 主从钻取；失败**必上抛** |
| 导出 | 同上 | 展开长表（一行一条明细），受双上限保护 |

### 写侧 `IAuditService`

| 方法 | 签名 | 语义 |
|---|---|---|
| `Record` | `void Record(AuditEntry entry)` | 投递一条。实现方内部消化一切异常，调用方无需 try/catch |
| `Snapshot` | `AuditBaseline Snapshot(object model)` | 取模型基线，供 `BuildChanges` 比对。🔴 禁 `ConfigBase.Clone<T>()`（Auto 集合翻倍 → 假"删除"），只走序列化方向 |
| `BuildChanges` | `List<AuditFieldChange> BuildChanges(object before, object after, params string[] except)` | 字段级明细；无变化返回**空列表**不是 null，直接 `Changes = changes`。`except` 传程序盖的章（ModifiedBy / ModifiedTime 等顶层 C# 属性名） |
| `BuildSetChanges` | `List<AuditFieldChange> BuildSetChanges(string fieldPath, IEnumerable<string> before, IEnumerable<string> after)` | 集合差异**逐项**展开（每个新增/移除元素各一条）。专治 `[JsonIgnore]` 集合（JSON diff 的盲区）；顺序不同成员相同 = 无变化 |

### 读侧 `IAuditQueryService`（异常策略与写侧相反）

| 方法 | 签名 | 语义 |
|---|---|---|
| `Query` | `AuditPageResult Query(AuditQueryFilter filter)` | 头表分页，时间倒序；`PageSize` 由实现夹到 [1, 1000] |
| `GetChanges` | `List<AuditFieldChangeDto> GetChanges(long operationId)` | 主从钻取：一条操作的全部字段级明细 |
| `QueryFlat` | `List<AuditFlatRowDto> QueryFlat(AuditQueryFilter filter)` | 导出长表，不分页全量；超上限抛 `InvalidOperationException` 由 UI 提示缩小范围 |

> **只读铁律：不提供任何删除 / 修改 / 清空方法**——结构上不给口子，防篡改第一道墙。

### 投递与查询模型

| 模型 | 构造 / 字段 | 说明 |
|---|---|---|
| `AuditEntry` | `(AuditCategory category, AuditAction action, string target, AuditActor actor = null, AuditResult result = AuditResult.Success)` + `Changes` 可空列表 | 投递请求 = 头 + 0..N 明细；`actor` null 或名字空归一"访客"；三维度收**枚举**（写反编译期报错） |
| `AuditActor` | `(string name, string roleNames, int level)`；静态 `Guest` 哨兵 | 操作者快照（Name/RoleNames/Level）；登录失败**显式构造** `new AuditActor(尝试的用户名, null, 0)` |
| `AuditQueryFilter` | TimeFrom / TimeTo / Category? / UserName / Result? / Action? / PageIndex=1 / PageSize=200 | 五维全可空 = 不加该维条件；页面/动作/结果收可空枚举，UI 下拉即枚举成员 |
| `AuditOperationDto` / `AuditFieldChangeDto` / `AuditFlatRowDto` | 列头走 `[Description]` | 查询页直接绑定的模型；读侧一律 **string 承载文案**，展示/导出零映射 |
| `AuditBaseline` | 不透明句柄 | 内部载荷 internal——调用方跨程序集拿不到，无法绕过契约自行比对 |

### 词汇表（四维度枚举，[Description] 即落库文案）

- **`AuditCategory` 页面**（13）：用户管理 / 角色管理 / 参数设置 / PLC调试 / 配方管理 / 流程监控 / 审计记录 / 产品数据 / 报警 / 主界面 / 多语言配置 / MES调试 / **系统**（跨页面会话类：登录/登出/登录失败，值 13 保持末位——下拉按值序填充，页面在前会话在后）。
- **`AuditAction` 动词**（25）：新建 / 修改 / 删除 / 保存 / 导入 / 导出 / 编辑（已停用新增）/ 重置 / 切换 / 读标签 / 写标签 / 连接 / 断开 / 重建 / 登录 / 登出 / 登录失败 / 分配角色（预留）/ 启动 / 暂停 / 流程复位 / 初始化 / 启用停用 / 异停设置 / 提取。语义 = 操作者点下的那个按钮。
- **`AuditResult` 结果**（2）：成功 / 失败。**没有"被拒"态**——被拒是原因不是结果，且门禁被拒不产生任何动词。
- **`AuditChangeType` 变更类型**（3）：新增 / 修改 / 删除。回答"数据发生了什么"，与 Action 的"做了什么"分两层（PLC 写标签 = Action「写标签」+ ChangeType「修改」）。

🔴 **文案即存储契约，一经投产不得修改**；变更流程只有一条：新增成员（见 §9）。

## 4. 快速开始

```csharp
// 0) 组合根注册（Taoyl.IFALaunch/Modules/AuditModule.cs）
builder.RegisterType<AuditRepository>().As<IAuditRepository>().SingleInstance();           // 写侧仓储
builder.RegisterType<AuditQueryRepository>().As<IAuditQueryRepository>().SingleInstance(); // 读侧仓储
builder.RegisterType<AuditService>().As<IAuditService>().SingleInstance();                  // 写侧服务
builder.RegisterType<AuditQueryService>().As<IAuditQueryService>().SingleInstance();       // 读侧服务

//    无启动期装配动作（AuditEnumText 静态构造自建缓存表，缺 [Description] 降级为枚举名）

//    Mapster 装配（MappingBootstrapper，任何 Adapt 之前）——顺序 = 开关 → Scan → Compile
TypeAdapterConfig.GlobalSettings.RequireDestinationMemberSource = true;
TypeAdapterConfig.GlobalSettings.RequireExplicitMapping = true;
TypeAdapterConfig.GlobalSettings.Scan(typeof(AuditMappingRegister).Assembly);
TypeAdapterConfig.GlobalSettings.Compile();

// 1) 页面 VM 构造注入（只用写侧就只注入 IAuditService）
public MainFormViewModel(IAuditService auditService, IUserSession userSession /*, ...*/)

// 2) 最小调用：一条动作型记录——返回即已落库或已降级，无需检查任何返回值
auditService.Record(new AuditEntry(
    AuditCategory.Config, AuditAction.Save, "BaseSettings",
    userSession.CurrentUser.ToAuditActor()));
```

落库两表：头表 `sys_audit_operations` + 明细表 `sys_audit_field_changes`（CodeFirst 随 `Bootstrapper.InitDatabase` 建）。DB 写失败自动降级本地 JSONL，调用方无感。

## 5. 使用示例（按场景，可复制）

**场景选型**：

| 你的操作 | 用什么 | 场景 |
|---|---|---|
| 点了按钮、无数据变化（复位/连接/读标签…） | `Record`，无明细 | 场景 1 |
| 编辑模型后保存（配置/配方/设备参数） | `Snapshot` → `BuildChanges` → `Record` | 场景 2 |
| `[JsonIgnore]` 集合（权限码/角色列表）差异 | `BuildSetChanges` → `Record` | 场景 3 |
| PLC 写标签（旧值不可知） | 手工一条明细 + `Record` | 场景 4 |
| 登录 / 登出 / 登录失败 | 订阅 `AuthFactRecorded` → `Record` | 场景 5 |
| 审计查询页 | `Query` / `GetChanges` | 场景 6 |
| 审计导出 | `QueryFlat` | 场景 7 |

### 场景 1：动作型操作（无明细）

```csharp
// 例：配置页"恢复默认值"——只填界面不落盘，真正落盘由随后的 Save 记账（铁律 8）。
// 留痕 = 只记"谁在哪个页面点了重置"，Changes 留空。
auditService.Record(new AuditEntry(
    AuditCategory.Config, AuditAction.Reset, AuditTarget,
    userSession.CurrentUser.ToAuditActor()));
```

### 场景 2：编辑后保存（字段级 diff，最常用）

```csharp
// ── 进入编辑时（BeginEdit）：取基线。🔴 必须早于 View 把控件值写回模型——晚了 diff 恒空 ──
editBaseline = auditService.Snapshot(Config);

// ── 保存命令体内：业务写成功之后才投递（铁律：宁漏记不记假账，Save 抛异常走 catch = 零记录）──
try
{
    configService.Save(Config);                        // ① 业务先落盘
    var changes = auditService.BuildChanges(           // ② 比对基线 → 字段级明细
        editBaseline, Config,
        nameof(Config.ModifiedBy), nameof(Config.ModifiedTime)); // 程序盖的章不参与比对
    IsEditing = false;
    auditService.Record(new AuditEntry(                // ③ 内联投递，不造 VM 私有包装层
        AuditCategory.Config, AuditAction.Save, AuditTarget,
        userSession.CurrentUser.ToAuditActor()) { Changes = changes });
    messageBoxService.ShowWarning("已保存", false, 3000);
}
catch (Exception ex) { messageBoxService.ShowError($"保存失败: {ex.Message}"); }
```

> 新建场景（new 出来的模型）：同样先取钩子基线（相对默认模板 diff），再在落盘动作里记账。配方保存的约定：`isCreating ? AuditAction.Add : AuditAction.Save`。

### 场景 3：集合差异逐项展开（[JsonIgnore] 集合专用）

```csharp
// 角色 / 权限码集合标了 [JsonIgnore]，走 JSON diff 会被整个丢掉 → BuildSetChanges。
// before/after 都是 IEnumerable<string>（权限码集合），基线同样在进入编辑时取好。
var changes = auditService.BuildSetChanges("手动权限", beforeCodes, afterCodes);
// 无变化返回空列表（不是 null）——每个新增/移除的权限点各占一条明细，一望即知。
auditService.Record(new AuditEntry(
    AuditCategory.Role, AuditAction.Update, role.RoleName,
    userSession.CurrentUser.ToAuditActor()) { Changes = changes });
```

### 场景 4：PLC 写标签（旧值不可知）

```csharp
// 旧值留空 = 不可知 ≠ 原值为空：写前读到的缓存值既非操作者所见、也非 PLC 真值，记了就是半假账。
string auditTarget = group + "." + tagName;            // 业务主键：组名.标签名
var auditChanges = new List<AuditFieldChange>
{
    new AuditFieldChange
    {
        FieldPath = auditTarget,
        ChangeType = AuditChangeType.Update,           // 「修改」，不是"写入"
        OldValue = null,                               // 🔴 留空 = 不可知
        NewValue = Convert.ToString(value, CultureInfo.InvariantCulture)
    }
};

// 驱动返回失败也是事实——Result.Fail 同样投递（审计记"做过但失败"，不是只记成功）。
ResponseResult result = await Task.Run(() => plc.ExecuteIo(request));
auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.WriteTag,
    auditTarget, userSession.CurrentUser.ToAuditActor(),
    result.IsSuccess ? AuditResult.Success : AuditResult.Fail) { Changes = auditChanges });
```

### 场景 5：登录域事实（事件接线，不直接投递）

```csharp
// 登录/登出/登录失败发生在登录窗体：刷卡登录由键盘钩子直达 AuthService，UI 结构上不可观测，
// 且身份采集时机只有登录域知道 → 登录域只广播 AuthFact（Share.Auth 零依赖契约），消费方接线：
authService.AuthFactRecorded += OnAuthFactRecorded;

private void OnAuthFactRecorded(AuthFact fact)
{
    if (fact == null) return;
    var action = fact.Kind == AuthFactKind.LoginSucceeded ? AuditAction.Login
               : fact.Kind == AuthFactKind.LoginFailed   ? AuditAction.LoginFailed
                                                         : AuditAction.Logout;
    var result = fact.Kind == AuthFactKind.LoginFailed ? AuditResult.Fail : AuditResult.Success;

    // 🔴 只认事件携带的快照，绝不回读 IUserSession（登出事件后会话随即清空 = 假账）
    auditService.Record(new AuditEntry(
        AuditCategory.System, action, fact.Username,
        new AuditActor(fact.Username,
                       string.Join(",", fact.RoleNames ?? new List<string>()),
                       fact.MaxRoleLevel),
        result));
}
```

### 场景 6：查询页（读侧异常必须上抛让用户看见）

```csharp
var filter = new AuditQueryFilter
{
    TimeFrom = DateTime.Today.AddDays(-7),   // 近一周（含当天全天）
    TimeTo   = DateTime.Today,
    Category = AuditCategory.Device,         // null = 全部页面
    PageIndex = 1,
    PageSize  = 50                           // 实现方夹到 [1, 1000]，别传超大值
};
try
{
    AuditPageResult page = auditQuery.Query(filter);   // 头表分页，时间倒序
    gridMain.DataSource = page.Items;                  // AuditOperationDto 列头走 [Description]
    pagination1.TotalCount = page.TotalCount;          // 分页控件用仓内 Controls/Pagination

    // 主从钻取：选中一条头记录 → 该操作改了哪些字段
    List<AuditFieldChangeDto> changes = auditQuery.GetChanges(selectedRow.Id);
}
catch (Exception ex)
{
    // 🔴 读侧与写侧策略相反：失败必须让用户看见——空白页会被误判"没有记录"，比报错更危险
    messageBox.ShowError($"审计查询失败: {ex.Message}");
}
```

### 场景 7：导出长表

```csharp
// 导出走独立权限码 Btn:Audit_Export（能查 ≠ 能导），且导出动作本身要留痕。
// QueryFlat 不分页全量，受 MaxFlatHeaders / MaxFlatRows 双上限保护，超限抛 InvalidOperationException。
try
{
    var rows = auditQuery.QueryFlat(filter);   // 一行一条明细；动作型操作头信息一行、明细列留空
    // …写 CSV：落盘前先删旧文件，导出后回读校验行数（消费者 FormAudit 约定）

    auditService.Record(new AuditEntry(
        AuditCategory.Audit, AuditAction.Export, "审计导出",
        userSession.CurrentUser.ToAuditActor()));
}
catch (InvalidOperationException)
{
    messageBox.ShowWarning("导出范围过大，请缩小时间范围后重试");
}
```

### 身份适配（两份，方向单向）

| 适配点 | 形态 | 适用 |
| ------ | ---- | ---- |
| `Taoyl.UI.Pages/Extensions/AuditActorExtensions.cs` | internal 扩展 `ToAuditActor()`，9 个页面 VM 共用 | Pages 域所有挂点（示例中的 `userSession.CurrentUser.ToAuditActor()`） |
| `Taoyl.UI.FormMain/ViewModels/MainViewModel.ToAuditActor` | private static | FormMain 自身（登录 / 登出 / 主界面流程按钮） |

> Pages → FormMain 单向依赖，不可反用；两份各自就地维护，勿为"统一"引入第三层包装。

## 6. 类库结构（逐代码文件说明表）

| 文件 | 说明 |
| ---- | ---- |
| **Interfaces/**（公开契约） | |
| `IAuditService.cs` | 投递契约（写侧，挂点唯一入口）：`Record` / `Snapshot` / `BuildChanges` / `BuildSetChanges`。三条铁律：①**宁漏记不记假账**（业务写成功后才投递，本方法自身绝不抛）②**审计永不高于业务**（失败只降级，不阻断、不弹窗、不重试、不回填）③**同步直写**（返回即落地或已降级）。落点原则见 §一 |
| `IAuditQueryService.cs` | 查询契约（读侧，与写侧分接口——**异常策略相反**：读侧失败必须上抛让用户看见，否则页面空白被误判"没有记录"，比报错更危险）。只读铁律：无任何删除/修改/清空方法 |
| `IAuditRepository.cs` | 写侧仓储契约（**实体进出**）：`Insert(header, details)`——头 + 明细同一事务收口在实现内（单方法 = 完整数据访问单元）。只供 `AuditService` 调用 |
| `IAuditQueryRepository.cs` | 读侧仓储契约（**实体进出**）：`Query` / `GetChanges` / `QueryFlat`；同文件定义容器 `AuditOperationPage` / `AuditFlatSource`（**承载实体，非 DTO，勿迁 Models**）。只供 `AuditQueryService` 调用 |
| **Models/**（投递模型）+ **Models/Dtos/**（查询 DTO） | |
| `AuditEntry.cs` | `AuditFieldChange`（明细条目 DTO：FieldPath / ChangeType / OldValue / NewValue；**旧值留空 = 不可知 ≠ 原值为空**）+ `AuditEntry`（投递请求 = 头 + 0..N 明细，构造唯一见 §三） |
| `AuditActor.cs` | 操作者身份快照（UserInfo 最小切割 3 字段）：三者都是**快照不是引用**（角色会改名/换组）；`Guest` 访客哨兵；登录失败场景显式构造 |
| `AuditBaseline.cs` | 基线句柄（`Snapshot` 产出 → `BuildChanges` 消费）：diff 实现的**不透明包装**，内部载荷 internal——调用方跨程序集拿不到 Token、无法绕过契约自行比对 |
| `Models/Dtos/AuditQueryModels.cs` | 读侧 DTO 群：`AuditQueryFilter` / `AuditPageResult` / `AuditOperationDto` / `AuditFieldChangeDto` / `AuditFlatRowDto`（详见 §三）。DTO 列头 `[Description]` 是查询页绑定元数据；实体侧列注释归 `SugarColumn.ColumnDescription`（实体已不带 `[Description]`），语义不同勿互抄 |
| **Mapping/**（实体 ↔ DTO 映射登记） | |
| `AuditMappingRegister.cs` | `IRegister` 实现：`NewConfig<AuditOperationEntity, AuditOperationDto>()` + `NewConfig<AuditFieldChangeEntity, AuditFieldChangeDto>()`（9/9、6/6 全同构，零差异声明；**单向映射**——只读侧 Entity→DTO，写侧手工构造实体，故以 `// 单向映射:` 登记豁免守卫「缺反向登记」检查）。全模块**唯一**的 TypeAdapterConfig 声明点；任何 Ignore/Map 必须登记在此并写 `// 映射豁免:` 供守卫核对 |
| **Repository/**（SqlSugar 实现，实体进出） | |
| `AuditRepository.cs` | 写侧实现：头表 + 明细表**同一事务**（`ExecuteReturnBigIdentity` 回填自增 Id；异常回滚上抛由服务层决定降级）。注入 `SqlSugarHelper`（构造期不触发 Db） |
| `AuditQueryRepository.cs` | 读侧实现：`BuildHeaderQuery` 五维条件拼装（静态方法禁入表达式树）/ 分页夹取（`MaxPageSize=1000`）/ 明文分批 IN（`IdBatchSize=500`，受 SQLite 变量上限 999 约束）/ 双层上限（`MaxFlatHeaders=50000` / `MaxFlatRows=200000`，展开行数在仓储侧预检） |
| **Services/**（实现） | |
| `AuditService.cs` | 写侧实现：经 `IAuditRepository` 写两表（头表存在则明细不丢）；DB 失败降级本地 JSONL（**仅失败时写**，平时零文件 IO）；`BuildHeader` / `BuildChangeEntities` = **枚举 → 落库文案的唯一转换点**，留在服务；头表定长列防御性截断（明细 Old/New 是 TEXT 不截断——截断即失真）；**只依赖仓储**（不接触 SqlSugar 类型）；两条通道都失败如实漏记 |
| `AuditQueryService.cs` | 读侧实现：注入 `IAuditQueryRepository`；**实体域→DTO 域唯一转换边界**（`Adapt<List<...Dto>>()`）+ 分页结果组装；`QueryFlat` 聚合组装留服务（`BuildFlatRow` 手工拼装，不套 Adapt——聚合不是平移映射）；导出超限异常由仓储抛出、UI 提示 |
| **Constants/**（维度枚举与文案闸门） | |
| `AuditConstants.cs` | 四维度枚举全集（见 §三词汇表）。🔴 成员 `[Description]` 文案即存储契约；成员值显式赋号锁定顺序 = 下拉顺序 = 值序 |
| `AuditEnumText.cs` | 枚举 ↔ 落库文案转换中枢：静态构造建缓存表（逐明细热路径，零反射纯查表）；缺 `[Description]` 降级为枚举名（对齐 `AlarmEnumText`），无启动期断言 |
| **Builder/**（diff 引擎，六类分工） | |
| `AuditChangeBuilder.cs` | **唯一门面**（public static，五域挂点共用——分散实现五份必然漂移）：`Snapshot`（JToken 序列化取基线，禁 Clone\<T\>）/ `Build` / `BuildSetChanges`。将来换声明式字段清单：三入口签名不动，只换 Walker 与 PropertyMap 实现 |
| `AuditChangeWalker.cs` | 递归比对引擎：**对象**逐属性并集（SortedSet 保序——同一变更记两次行序必须可复现）；**数组**按业务 Key 锚定配对（顺序无关、增删不串位）；**标量** `JToken.DeepEquals`（null ≠ ""，安全侧——宁可多记不可漏判）。全程双路径：rawPath（原名，只用于跳过名单与敏感判定）+ displayPath（显示名，落库给人读） |
| `AuditArrayAnchor.cs` | 数组元素取名与配对——**配对锚点 `KeyNames`**（求稳：Key/Code/Id/UserId/Name/RoleName，稳定键必须排在可编辑名之前——排反 = 改名被读成"删+增"）与**展示名 `LabelNames`**（求好读，取错最多路径不好读）。⚠️ TagName 刻意只作展示名（用户可编辑字段）——地址表数组退化为整条 JSON 是已知取舍 |
| `AuditPropertyMap.cs` | 类型反射中心：属性**双索引缓存**（C# 属性名 + JSON 键名——`[JsonProperty]` 改名字段必须双索引）+ **中文显示名翻译**（`[Description]` 优先、`[DisplayName]` 次之——本仓两种惯例并存）。🔴 取不到类型安静退回 null 由调用方退回原名——翻译失效只让明细不好读，绝不允许影响比对结果 |
| `AuditFieldPath.cs` | 明细路径渲染（点分 / 下标 / 方括号段拼装）：数组段优先展示名 → 退回锚点 → **下标最后兜底**（与位置绑定，增删一项就换了意思） |
| `AuditValueFormatter.cs` | 值格式化 + 脱敏：`Format`（标量 InvariantCulture 跨机器可读；对象/数组压缩 JSON）/ `IsSensitive`（🔴 敏感关键字**包含**匹配 fail-closed——精确匹配罩不住 NewPassword/ApiToken 等变体；命中只记变更事实、新旧值留空；判定传**原名**路径） |
| **Models/Entities/**（落库实体） | |
| `Models/Entities/AuditOperationEntity.cs` | 头表 `sys_audit_operations`：Id / Time / UserName(50) / RoleNames(200) / RoleLevel / Category(20) / Action(20) / Target(100) / Result(20)；无 Reason 列（21 CFR 合规需要时再加） |
| `Models/Entities/AuditFieldChangeEntity.cs` | 明细表 `sys_audit_field_changes`：OperationId / FieldPath(100) / ChangeType(20) / OldValue / NewValue（**TEXT 不截断**——截断即失真） |

## 7. 内部逻辑

### 写侧全链路

```
UI 命令层挂点（内联）
  ├─ 动作型：auditService.Record(new AuditEntry(category, action, target, actor, result) { Changes = ... })
  └─ 编辑型：before = auditService.Snapshot(model)   ← 必须在原地覆盖之前取（业务时序，契约无法代劳）
              changes = auditService.BuildChanges(before, after, except...)
AuditService.Record
  → BuildHeader / BuildChangeEntities：枚举 → [Description] 中文文案（AuditEnumText，唯一转换点）
  → IAuditRepository.Insert(header, details)
      → SqlSugar 同一事务：头表 sys_audit_operations + 明细表 sys_audit_field_changes
  → 失败降级：本地 JSONL 追加写（仅失败时写）
  → 永不抛异常：两条通道都失败 = 如实漏记
```

### diff 引擎分层

```
AuditChangeBuilder（门面：Snapshot / Build / BuildSetChanges）
  → AuditChangeWalker（递归引擎：对象 / 数组 / 标量三种走法，双路径携带）
      ├─ 数组配对 → AuditArrayAnchor（锚点求稳 / 展示求好读，两套名单）
      ├─ 路径拼装 → AuditFieldPath（点分 / 方括号 / 下标兜底）
      ├─ 名称翻译 → AuditPropertyMap（双索引缓存 + [Description]/[DisplayName] 翻译）
      └─ 落库加工 → AuditValueFormatter（InvariantCulture 格式化 + 敏感脱敏 fail-closed）
```

- 实现取 **JSON 往返而非反射克隆**：属性随模型自动跟进（新增成员零改动），`[JsonIgnore]` 计算属性（IsDirty 等派生值）天然不参与比对——它们不是"人改的"。
- **顶层跳过名单**（调用方传 `nameof(...)` C# 属性名）→ `AuditPropertyMap.TranslateSkipNames` 换成显示名集合再比对——FieldPath 落的是显示名，不换名比对会静默失配。

### 关键设计决策（为什么）

| 决策 | 为什么 |
|---|---|
| 读写分两个接口 | 异常策略相反（写永不抛 / 读必上抛），一个接口扛两种相反策略，实现方迟早写歪 |
| 三维度收枚举（2026-09-18 改型） | const string 时代 `AuditEntry(category, action, …)` 相邻参数写反**编译器不报错**，只会静默落一行永远查不出的记录 |
| 落库存 `[Description]` 文案而非 int | 库与 Excel 人眼可读，免维护"值↔文案"映射表；代价 = 文案冻结为契约（§八铁律 1） |
| 身份 3 字段快照 `AuditActor` | 审计不反向依赖 AuthService；快照防角色改名/换组导致追溯失真 |
| AuthService 走 `AuthFact` 事件而非直接投递 | 刷卡登录 UI 不可观测、身份采集时机只有登录域知道；事件让登录域与审计互不认识 |
| diff 走 JSON 往返、禁 `Clone<T>` | Clone 的 ObjectCreationHandling.Auto 复用集合并追加，预置集合模型克隆翻倍（配方曾刷 42 条假"删除"） |
| 数组按业务 Key 锚定配对 | 按下标比对在增删一项时全线错位、一次改动放大成几十条假明细；锚点只认用户改不了的稳定键 |
| 敏感值**包含**匹配 fail-closed | 精确匹配罩不住 NewPassword / ConfirmPassword / ApiToken 等变体，静默漏网；偏宽只少记一个值，偏窄 = 凭据进明文资产 |
| 降级 JSONL 不走 ILogService | ①依赖方向（不反向依赖 LogDriver）②NLog FormatMessage 对花括号抛 FormatException（已踩坑） |
| Service 只依赖仓储、不摸库 | Service 禁出现 SqlSugar 类型（含 using）——直查形态靠纪律自觉，仓储形态让泄漏在结构上不可能（"结构保证 > 纪律保证"） |
| 头 + 明细收为一个仓储方法 | 事务边界约定："单方法 = 完整数据访问单元"；摊成两个方法会把事务边界浮到服务层，Service 将为"怎么存"负责（越界） |
| 实体↔DTO 走 Mapster Adapt，调用点在服务 | 服务 = 实体域→DTO 域唯一转换边界；实体出栈即死，UI/契约只见 DTO |
| 映射配置集中 `AuditMappingRegister` | 全模块唯一 TypeAdapterConfig 声明点；组合根 Scan + Compile 启动期 fail-fast（严格成员校验，堵"改名静默丢值"） |

## 8. 注意事项与 FAQ

### 🔴 铁律总表（违反 = 数据 / 语义错误）

| # | 铁律 | 一句话理由 |
| - | ---- | ---- |
| 1 | 🔴 **文案即落库契约**：四维枚举的 [Description] 一经投产不得修改，`AuditEnumText` 是唯一闸门（**新增成员必须带 [Description]**——缺则降级为枚举名，无启动期报警） | 改文案 = 改数据语义，同一列两种说法 → 筛选与统计错乱 |
| 2 | **宁漏记不记假账**：业务写成功后才投递；异常一路上抛到 UI 的场景零记录 | 审计是历史事实，假账比漏记更毒 |
| 3 | **写侧永不抛 / 读侧必上抛**：两契约两策略，勿合并 | 空白页被误判"没有记录"比报错更危险 |
| 4 | **读侧 string 不反查枚举**：文案原样展示 / 导出 | 天然容忍历史文案与将来删掉的成员 |
| 5 | **敏感脱敏 fail-closed**：关键字**包含**匹配，命中只记事实不记值 | 匹配偏宽只少记一个值；偏窄 = 凭据进可导出的明文资产 |
| 6 | **基线取 `Snapshot`，禁 `ConfigBase.Clone<T>`** | Clone 走 Auto 集合翻倍 → 几十条假"删除" |
| 7 | **集合差异逐项展开 + `ExceptWith` 后必须显式 `OrderBy(Ordinal)`** | 读者逐条核权限，不该拿逗号长串自己拆；不排序行序漂移被误读 |
| 8 | **只记真正落盘 / 应用的变化**：进编辑态不记（Edit 成员仅历史兼容）；配方保存 `isCreating ? Add : Save` | 编辑态零变化，真账由落盘动作记 |
| 9 | **门禁被拒 / 前置拦截不留痕；查询动作自身不记审计** | 命令体首行被拦、没碰任何数据，记进来只会混进"什么都没发生"的行 |
| 10 | **一命令一码，Cancel 无码无痕**（取消不留痕：进编辑态不记账，见第 8 条） | 需要反查时按动词 grep 挂点即可定位，不合码 |
| 11 | **枚举值 = 下拉顺序，System 保持末位（值 10）** | 下拉按值序填充：页面在前、跨页面会话在后 |
| 12 | **数组配对锚点只认用户改不了的稳定键**，TagName 只作展示名 | 锚点取到可编辑字段 = 改名被读成删+增，静默错账 |

### ⚠️ 坑位与 FAQ

| 问 / 坑 | 答 |
|---|---|
| 为什么没有删除 / 清空接口？ | 只读铁律：防篡改第一道墙，结构上不给口子。表长期只增，磁盘容量属运维规划 |
| 为什么 `BuildChanges` 返回空列表而不是 null？ | 调用方直接 `Changes = changes` / `AddRange`，免判空——**无变化也是明确结果** |
| 明细里 OldValue 空是不是"原来是空"？ | 不是。留空 = 旧值**不可知**（PLC 写入即此类）；真"原值为空"会记空串 |
| 导出为什么抛异常而不是返回空？ | 超上限是**调用方筛选问题**，静默截断 = 导出一份不完整台账还看不出来 |
| 编辑基线什么时候取？ | 进入编辑时 / 选中对象时（钩子）。**晚于 View 写回 = diff 恒空**，这是最常见集成事故 |
| PageSize 传 int.MaxValue 会怎样？ | 实现夹到 1000；但别依赖夹取——服务端分页的意义就是把长期只增的留档表挡在内存外 |

## 9. 扩展指南

### A. 新页面接审计（标准动作）

1. `using Taoyl.Application.Audit.Interfaces;` + 按需 `using Taoyl.Application.Audit.Models;`；构造注入 `IAuditService`（查询页另注入 `IAuditQueryService`）。
2. 身份快照：按 §五适配表取 `AuditActor`；登录失败场景显式 `new AuditActor(尝试的用户名, null, 0)`。
3. 投递点**一律内联** `auditService.Record(new AuditEntry(...))`，写在业务写成功之后——不建 VM 私有投递包装层（样板代码会漂移）。
4. 编辑态挂点：进入编辑时 `Snapshot`——**必须早于 View 写回**；new 场景同样先取钩子基线。
5. `[JsonIgnore]` 集合用 `BuildSetChanges`（场景 3）。
6. Target 填**业务主键**（配置节名 / 设备名 / 配方名 / 组名.标签名 / 用户名 / 命令名(权限码)）；明细 FieldPath 由 diff 引擎自动翻译，无需挂点操心。

### B. 新增动词（AuditAction）

在 `AuditConstants.AuditAction` **新增成员**：值顺延、`[Description]` 即落库文案（≤20 字符，`AuditEnumText` 启动期自检兜底）；旧成员文案一律不动。若新动作有"数据变了"的形态，确认 `AuditChangeType` 三成员够表达（回答的是"数据发生了什么"，不是动作本身）。

### C. 新增页面维度（AuditCategory）

新页面成员值插在 1..12 页面段内（**勿动 System=13**）；插在中间时把其后的成员值一并顺延——值不落库（落库的是 `[Description]` 文案），顺延零数据影响。`[Description]` 中文值与该页 Menu.json 的 DisplayName 对齐（主界面例外——按钮长在主窗体、无菜单节点）。

### D. 新列表模型进数组锚点

新增列表模型的主键字段名不在 `AuditArrayAnchor.KeyNames`（Key/Code/Id/UserId/Name/RoleName）→ 补一个名字即可；**补之前确认该字段用户改不了**（稳定键要求）。用户可编辑的名字进 `LabelNames`。

### E. 新敏感字段

敏感字段名命中靠**包含**匹配（`AuditValueFormatter` 关键字名单）：字段名含 Password / Secret / Token 词根即自动脱敏；确需新增词根，往名单补"名字根"而非全名。

## 10. 相关文档

- `Taoyl.Application.AuthService/README.md`——身份来源（`AuthFactRecorded` 事件的源头）与 `AuthFact` 契约时序。
- `Taoyl.Application.Share/Auth/AuthFact.cs`——登录事实契约本体（零依赖，双方共同认识）。
- 查询页消费者约定参考：`Taoyl.UI.Pages/Views/FormAudit`（分页控件 `Controls/Pagination` 唯一，`PageChanged` 须幂等）。
