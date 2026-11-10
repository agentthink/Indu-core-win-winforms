# Taoyl.IFALaunch — 组合根（Composition Root）

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

全解决方案**唯一 WinExe 入口**（IFALaunch.exe），职责只有一件：**装配**——容器构建、启动序列编排、单实例与 DPI 处理。不承载任何业务逻辑；业务实现在各应用层类库中，本程序集只决定「谁实现谁」。

铁律：**类库永不引用 Autofac**（保持 DI 无知、零依赖基座可测试可替换），注册代码全部集中在 `Modules/` 目录，组合根是唯一知道全部实现的地方。

## 2. 依赖说明

直接 ProjectReference（SDK 风格 csproj，共 18 条）。表中省略 `Taoyl.Application.` 前缀。铁律：**组合根 Module 注册代码直接 using 的程序集必须逐条显式列出**，禁止依赖传递引用编译——传递链一断，组合根会毫无征兆编不过。

| 项目 | 引用原因 |
|---|---|
| Business | 设备实现 / 流程宿主 / 流程子类 / EventBus |
| ProcessMonitoring | 流程监控与控制门面 |
| Alarm | 报警服务与仓储注册 |
| Audit | 审计写侧 / 读侧注册 |
| AuthService | 认证 / 权限 / 会话 / 刷卡 + PermissionSyncService |
| Product | 产品数据源与查询服务 |
| Configuration | 配置中心三件套注册 |
| TaskProcess | 流程基座（ProcessCoordinator / ProcessBase） |
| Device | 设备契约与 DeviceManager |
| DataBase | SqlSugarHelper |
| FileUtil | CSV / 文件能力 |
| LogDriver | 日志服务注册 |
| MesDriver | MES 配置管理与客户端 |
| PlcDriver | PLC 配置管理 |
| Share | 跨域契约（Paths / Eventing / Config） |
| TranslateDriver | 翻译服务 |
| UI.FormMain / UI.Pages | 界面层（主窗体 / 页面 / VM / 服务） |

## 3. 启动序列

```
Program.Main
 ├─ 1. 防重复启动（RunningInstance → 激活已有实例）
 ├─ 2. InitializeUiStyles（SunnyUI DPI 缩放 + 全局字体按系统 DPI 补偿）
 └─ 3. Bootstrapper.Initialize
     ├─ MappingBootstrapper.Configure（Mapster 全局配置：开关 → Scan → Compile）
     ├─ 注册 12 个 Module（顺序见 §4）→ ContainerBuilder.Build
     ├─ InitConfigs（配置预加载：DevConfig / BaseConfig / LogConfig + **日志 UI 提前激活** / 配方 LoadAll）
     ├─ InitDatabase（SqlSugar CodeFirst 9 表 + PermissionSyncService.SyncOnStartup）
     ├─ LoadDeviceConfig（Devices.json 装载编排宽口）
     └─ InitComponents（启动期组件 Resolve 即激活：ProcessFaultRouter / IProcessRegistry / AlarmPopupPresenter + MES 配置加载）
 └─ 4. Container.Resolve<FormMain>() → Application.Run
```

设备**连接**不在组合根：FormMain 启动后经 `IDeviceManager.ConnectAllAsync` 触发。

> 🔴 **日志 UI 由组合根在 `InitConfigs` 提前激活**（裸 `Container.Resolve<ILogDisplay>()` 触发单例构造——
> **构造即订阅**，不绑网格）：环缓冲住在 `LogForm` 内部、事件订阅原只在 `Init`，而 UI 侧 `Init(网格映射)`
> 要等主页面加载（`FormMainPage.ConfigureLogForm`）——不提前激活，启动期日志（如内置管理员兜底复活的 Warn）
> 既无订阅者也不入环 ⇒ UI 永远看不到。`InitComponents` 在 `InitDatabase` 之后，**承担不了这个职责**。

## 4. 模块映射表（类库 ↔ Module 文件一一对应）

每个 Module 文件头第一行注释标注对应类库；注册顺序 = 启动叙事：基础 → 业务域 → 设备 → 流程 → 界面。

| Module 文件 | 对应类库 | 注册概要 |
|---|---|---|
| LogModule | Application.LogDriver | ILogService / ILogDisplay（单例） |
| ConfigModule | Application.Configuration | 序列化器 / ConfigService / RecipeService<> 开放泛型 |
| DataBaseModule | Application.DataBase | SqlSugarHelper（**延迟首 Resolve** 读 DevConfig） |
| SharedModule | TranslateDriver + FileUtil | 翻译三契约（TransManager 同单例）+ CSV |
| AuthModule | Application.AuthService | 5 仓储 SqlSugar 单后端直接注册 + PermissionSyncService + 会话/权限/刷卡 + 2 软配置窄接口 |
| AuditModule | Application.Audit | 写侧 AuditService / 读侧 AuditQueryService（分契约） |
| AlarmModule | Application.Alarm | AlarmRepository + AlarmService（写侧队列 / 消费线程在服务内） |
| ProductModule | Application.Product | CellinfoProductRepository + ProductCellInfoService（产品表数据源） |
| DeviceModule | Business.Devices + PlcDriver | 设备工厂×2 / 加载器×2 / ConfigStore / DeviceManager 三契约 / PlcConfigManager |
| MesModule | Business.Mes + Application.MesDriver | MesConfigManager / MesLogSink / MesClient（全单例） |
| ProcessModule | Business.ProcessHosting + Eventing + ProcessMonitoring | 流程宿主设施（上下文/注册器/故障路由/日志出口）/ EventBus / MonitorService / 流程子类程序集扫描 |
| FormModule | UI.FormMain + UI.Pages | UI 服务 3 + MenuManager + VM + 窗体 |

## 5. 扩展 SOP

**新增类库**：类库实现接口 → `Modules/` 新建 `XxxModule.cs`（文件头标注对应类库）→ `Bootstrapper.Initialize` 加一行 `RegisterModule`。

**新增页面**：`FormModule` 加一行 `RegisterPage<Taoyl.UI.Pages.FormXxx>(builder)` → `Menu.json` 加节点（`DllName` = 命名空间、`Class` = 类名）。🔴 Menu.json 双份真相：bin 运行时活文件 + `Taoyl.UI.FormMain/config/` 模板，**必须同时改**。

**新增设备类型**：`DevicesConfigFile` 加节 + `DeviceConfigSections.All` 加一行（DeviceManager 主体零改动）。

## 6. 契约链

**页面解析链**：`Menu.json（DllName + Class）` → `MenuManager.GetFormInstance` 拼 `formKey = DllName.Class` → `TryResolveNamed` → `FormModule.RegisterPage<T>` 注册的 Named 名 = `typeof(T).FullName`。三处命名靠拼接对齐：Named 名由类型自动生成（改名即跟随），Menu.json 是唯一需人工维护的一侧。页面注册为 `SingleInstance`，**容器即唯一实例缓存**——`MenuManager` 不再自持字典（双层缓存会与容器生命周期脱节）。解析缺失兜底双层：`GetFormInstance` 解析失败抛明确异常（含 DisplayName + formKey）；`FormMain.ValidatePageRegistrations` 启动自检遍历 Menu.json 页面节点用 `IsRegisteredWithName` 预检（查注册表不实例化），缺失弹窗列出——「点开才炸」前移到「起来就报」。

**注入约定**：窗体 = 属性注入（PropertiesAutowired）；VM 与服务（含 MenuManager）= 构造注入。Autofac 默认走参数最多的构造函数，非窗体类型挂 PropertiesAutowired 只是冗余残留，一律不加。

## 7. 注意事项与 FAQ

🔴 **延迟首 Resolve 纪律**：仓储与 SqlSugarHelper 均为延迟解析——`InitConfigs` 必须先执行，才能拿到内存缓存配置（ConfigService.Get 自愈 + 默认 Json 安全侧）。此为既定项，勿重开。

🔴 **Mapster 装配在容器构建之前**：MappingBootstrapper.Configure（`Mapping/MappingBootstrapper.cs`）必须在任何 Adapt 调用之前执行。两严格开关（RequireDestinationMemberSource / RequireExplicitMapping）都开——后者顺带覆盖"Register 程序集漏扫"。Compile 后禁再改配置；新增模块的 Register 程序集在此文件 Scan 清单加一行。

🔴 **InitDatabase 失败即抛**：CodeFirst 9 表（UserInfo / Role / Permission / UserRole / RolePermission / AuditOperationEntity / AuditFieldChangeEntity / AlarmRecord / ProductCellInfo）初始化失败不允许带病启动。

⚠️ `ConfigManager.Instance` 静态单例为过渡形态（DeviceModule 注入 IPlcConfigManager 走同一实例）。

⚠️ csproj `CopyConfigOnce` Target：首次构建拷贝 config 模板到输出目录，**不存在才拷**——保护现场修改；现场改动回库需手工拷回模板目录。

## 8. 相关文档

- 各库 README：仓库内 13 份（`Taoyl.Application.*/README.md`），母本 = `Taoyl.Application.Audit/README.md`（10 节标准）
