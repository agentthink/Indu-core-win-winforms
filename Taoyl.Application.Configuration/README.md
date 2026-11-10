# Taoyl.Application.Configuration —— 配置中心 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**全局配置与配方的唯一读写口**：把 `Config/*.json` 和 `Config/Recipes/*.json` 变成内存单例，向全系统提供"读一次、改多处、保存落盘"的能力。

**是什么**：泛型配置服务（`IConfigService`）、泛型配方服务（`IRecipeService<TRecipe>`）、配置模型基类（`ConfigBase`，负责属性通知 + 递归脏标记 + 草稿克隆）、JSON 序列化策略（`JsonConfigSerializer`）、原子写盘与多版本备份。

**不是什么**：不做 UI（配置界面由 `Taoyl.UI.FormMain` 的 `ConfigFormRenderer` 反射生成）、不做热生效（改完一律重启）、不管权限（谁能改哪个配置由 AuthService 决定）、不写日志（只提供 `LogConfig` 模型，消费在 `Taoyl.Application.LogDriver`）。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| Share | `ILogSettings` / `ILogCategoryPolicy` / `ICardLoginSettings` / `IAutoLogoutSettings` / `LogCategory` / `LogSplitPeriod` / `LogFileEncoding` / `DatabaseType` 等窄接口与枚举 |
| Newtonsoft.Json 13.0.4 | 序列化、`PopulateObject` 原地填充、`StringEnumConverter` |

被依赖：`Taoyl.UI.FormMain`（配置界面 + 主窗体）、`Taoyl.UI.Pages`（各设置页 VM）、`Taoyl.IFALaunch`（启动预加载）。`Taoyl.Application.LogDriver` **不直接引用**本库 —— 它只认 Share 的 `ILogSettings`，`LogConfig` 由组合根传入（见 `Bootstrapper.InitConfigs`）。

本库**不依赖**任何 ORM、UI、业务模块 —— 它是依赖链最底层之一，只认 Share。

## 3. 功能与 API

### `IConfigService`（`Interfaces/IConfigService.cs`）

| 成员 | 签名 | 说明 |
|---|---|---|
| 获取 | `T Get<T>() where T : ConfigBase, new()` | 首次访问从文件加载；文件不存在则用 `new T()` 的默认值；成功后 `MarkClean` 入缓存。**同一类型全进程只有一份实例** |
| 保存 | `void Save<T>() where T : ConfigBase` | 仅当 `IsDirty == true` 才写盘；写后 `MarkClean` |
| 批量保存 | `void SaveAll()` | 遍历缓存，逐个判断脏标记后写盘 |

### `IRecipeService<TRecipe>`

| 成员 | 签名 | 说明 |
|---|---|---|
| 列表 | `IReadOnlyList<TRecipe> Recipes { get; }` | 已加载配方 |
| 当前 | `TRecipe CurrentRecipe { get; }` | 当前激活配方（可能为 null） |
| 加载 | `void LoadAll()` | 读 `RecipeIndex.json` + 遍历 `Config/Recipes/*.json`，恢复上次激活项 |
| 切换 | `void SwitchRecipe(string recipeName)` | 不存在则抛 `ArgumentException`；成功后写索引并触发 `OnRecipeSwitched` |
| 保存 | `void SaveRecipe(TRecipe recipe)` | 同名冲突抛 `InvalidOperationException`；改名时备份删除旧文件 |
| 新建 | `TRecipe CreateNewRecipe(string name)` | 名称非空、非保留字 `RecipeIndex`、不重复；**只加内存，不落盘**，需再 `SaveRecipe` |
| 删除 | `void DeleteRecipe(string recipeName)` | **不允许删当前激活配方**（`InvalidOperationException`）；文件先备份再删 |
| 导入 | `void ImportRecipe(string filePath)` | 同名时直接覆盖（不弹框），覆盖后保留原配方的 `ModifiedTime` |
| 导出 | `void ExportRecipe(string recipeName, string filePath)` | 直接写目标路径，不走原子写 |
| 事件 | `event Action<TRecipe> OnRecipeSwitched` | **可能在后台线程触发**，UI 订阅须 `Invoke` |

### `ConfigBase`（`Models/ConfigBase.cs`）

所有配置模型的抽象基类，`INotifyPropertyChanged` 实现。

| 成员 | 签名 | 说明 |
|---|---|---|
| 脏标记 | `bool IsDirty { get; }` | **递归**：自身脏、或任一嵌套 `ConfigBase` 脏、或 `IEnumerable<ConfigBase>` 中任一项脏，都返回 true；`[JsonIgnore]` 不参与序列化 |
| 赋值入口 | `protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string) ` | 值有变才置脏 + 触发 `PropertyChanged`。**所有属性 setter 必须走它** |
| 通知 | `protected void OnPropertyChanged([CallerMemberName] string)` | 手动触发通知 |
| 置脏 | `void SetDirty()` | 只置自身，不递归；用于"重置默认值后需要能保存" |
| 清脏 | `void MarkClean()` | 递归清自身与所有子对象 |
| 草稿 | `T Clone<T>() where T : ConfigBase, new()` | JSON 序列化再反序列化，返回 `MarkClean` 后的副本（编辑草稿用） |
| 提交 | `void CopyTo(ConfigBase target)` | 把当前值 `PopulateObject` 到目标实例（原地覆盖，保持引用） |

### 配置模型一览

| 模型 | 文件 | 关键成员 |
|---|---|---|
| `DevConfig` | `Config/DevConfig.json` | `UILanguage` / `DB`（`DBConfig`：`DBType` + `ConnectionString`）/ `AutoLogin`（`AutoLoginConfig`）/ `AutoLogoutTime` |
| `BaseConfig` | `Config/BaseConfig.json` | `MaxSpeed` / `MinTemperature` / `MaxTemperature` / `TimeoutMs` |
| `LogConfig` | `Config/LogConfig.json` | `RootPath` / `Encoding` / 5 个固定通道策略 `Debug`·`Info`·`Warn`·`Error`·`MES` / `GetPolicy(LogCategory)` |
| `LogCategoryPolicyConfig` | 内嵌于 `LogConfig` | `Category` / `SplitPeriod`（按天·按小时）/ `KeepDays`（-1=不清理）/ `FileNamePrefix` |
| `MachineARecipeConfig` | `Config/Recipes/<名称>.json` | 6 个参数 + 静态常量类 `P`（`PreWeight` / `PostWeight` / `InjectionVolume` / `VacuumPressure` / `RefillVolume` / `ResistanceValue`） |
| `RecipeBase` | — | `RecipeName` / `ModifiedBy` / `Version` / `ModifiedTime` / `Comment` / `Parameters` / 索引器 `this[key]` |
| `RecipeParameter` | — | `Key`（只读业务锚点）/ `DisplayName` / `Unit` / `Comment` / `Min` / `Max` / `Value` |

`LogConfig` 的 5 个通道是**固定属性**而非列表 —— 编译期保证一个不少，且 `ConfigBase` 的递归判脏天然生效。

## 4. 快速开始

容器注册（`Taoyl.IFALaunch/Modules/ConfigModule.cs`）：

```csharp
builder.RegisterType<JsonConfigSerializer>().As<IConfigSerializer>().SingleInstance();
builder.RegisterType<ConfigService>().As<IConfigService>().SingleInstance();

// 配方服务是开放泛型：具体机型的 RecipeConfig 子类自动匹配
builder.RegisterGeneric(typeof(RecipeService<>)).As(typeof(IRecipeService<>)).SingleInstance();
```

启动期预加载（`Bootstrapper.InitConfigs`，必须在任何 Resolve 之前）：

```csharp
var cfg = Container.Resolve<IConfigService>();
cfg.Get<DevConfig>();          // 先落地，后面 DataBaseModule 等才取得到
cfg.Get<BaseConfig>();
var log = cfg.Get<LogConfig>();
Container.Resolve<ILogService>().Initialize(log);   // 日志系统 overlay 依赖这份配置

Container.Resolve<IRecipeService<MachineARecipeConfig>>().LoadAll();
```

🔴 `Get<T>()` 是**懒加载**：不预加载的话，`DataBaseModule` 里 `c.Resolve<IConfigService>().Get<DevConfig>()` 会自己加载，但那时 `Config/` 目录和日志系统可能还没就绪。预加载是启动顺序的一部分，不是优化。

## 5. 使用示例

### 5.1 读配置、改、保存

```csharp
// 注入 IConfigService（构造注入，不要 new ConfigService）
public class SomeViewModel
{
    private readonly DevConfig dev;

    public SomeViewModel(IConfigService config)
    {
        dev = config.Get<DevConfig>();     // 拿到的是共享单例，不是副本
    }

    public void SaveLanguage(string lang)
    {
        dev.UILanguage = lang;             // 走 SetProperty → 自动置脏 + 通知
        config.Save<DevConfig>();          // 脏才写盘，不脏直接 return
    }
}
```

### 5.2 编辑草稿再提交（配置页标准姿势）

```csharp
// 1. 拿单例的副本，改副本不影响运行中的配置
DevConfig draft = config.Get<DevConfig>().Clone<DevConfig>();

// 2. UI 绑定 draft，用户随便改（IsDirty 在 draft 上累积）

// 3. 用户点保存：把 draft 的值原地灌回单例
draft.CopyTo(config.Get<DevConfig>());

// 4. 落盘（此时单例已被置脏）
config.Save<DevConfig>();
```

🔴 第 3 步必须用 `CopyTo`（内部 `PopulateObject`，`ObjectCreationHandling.Auto`）——它**填充**嵌套对象、保持引用不变。若改成 `Replace`，`ICardLoginSettings` 这类指向 `AutoLoginConfig` 实例的窄接口会指向旧对象，改了等于没改。

### 5.3 配方：切换与读取参数

```csharp
var recipes = Container.Resolve<IRecipeService<MachineARecipeConfig>>();

recipes.SwitchRecipe("机型A-标准");                      // 写索引 + 触发事件
var recipe = recipes.CurrentRecipe;
double preWeight = recipe[MachineARecipeConfig.P.PreWeight].Value;   // 按 Key 取参数

// 订阅切换（后台线程，UI 必须切回来）
recipes.OnRecipeSwitched += r =>
    form.BeginInvoke(new Action(() => RefreshGrid(r)));
```

### 5.4 配方新建 / 保存 / 删除

```csharp
var draft = recipes.CreateNewRecipe("机型A-试产");     // 只进内存
draft[MachineARecipeConfig.P.InjectionVolume].Value = 12.5;
recipes.SaveRecipe(draft);                            // 落盘 + 写索引

recipes.DeleteRecipe("机型A-试产");                    // 删当前激活项会抛异常
```

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| **Interfaces/** | |
| `IConfigService.cs` | 配置服务契约（3 方法） |
| `IRecipeService.cs` | 配方服务契约（10 方法 + 1 事件） |
| **Models/** | |
| `ConfigBase.cs` | 配置基类：`SetProperty` / `IsDirty` 递归 / `Clone<T>` / `CopyTo` / `MarkClean` |
| `DevConfig.cs` | 开发配置根 + `DBConfig` / `AutoLoginConfig` |
| `BaseConfig.cs` | 设备基础运行参数 |
| `LogConfig.cs` | 日志根配置（5 个固定通道 + `GetPolicy`） |
| `LogCategoryPolicyConfig.cs` | 单通道策略（切分周期 / 保留期 / 文件名前缀） |
| `MachineARecipeConfig.cs` | A 机型配方（参数骨架 + 常量类 `P`） |
| `Params/RecipeBase.cs` | 配方基类（元数据 + 参数列表 + 索引器） |
| `Params/RecipeParameter.cs` | 单个配方参数 |
| **Serialization/** | |
| `IConfigSerializer.cs` | 序列化契约（`Serialize<T>` / `Deserialize<T>`） |
| `JsonConfigSerializer.cs` | Newtonsoft 实现，`ObjectCreationHandling.Replace` + 缩进输出 |
| **Services/** | |
| `ConfigService.cs` | 配置缓存 + 首次加载 + 原子写 + `GetFilePath` 特殊映射 |
| `RecipeService.cs` | 配方加载 / 切换 / 增删改 / 导入导出 + 索引维护 |
| `RecipeIndex.cs` | 索引文件模型（`CurrentRecipeName` + `RecipeNames`），`internal` |

## 7. 内部逻辑

### 配置读写链路

```
Get<T>()
  ├─ 缓存命中 → 直接返回同一实例
  └─ 未命中 → GetFilePath<T>()
                ├ DevConfig  → Config/DevConfig.json      （特殊映射）
                ├ BaseConfig → Config/BaseConfig.json     （特殊映射）
                └ 其他       → Config/<类型名>.json
             → 反序列化（Replace）或 new T() 默认值
             → MarkClean → 入缓存

Save<T>()
  ├─ IsDirty == false → 直接 return（不产生 IO）
  └─ 序列化 → AtomicWrite:
       1. 旧文件复制到 Backup/<名>_yyyyMMddHHmmss.json（多版本保留）
       2. 写 <文件名>.tmp
       3. 删原文件
       4. .tmp 改名回正式文件名
     → MarkClean
```

### 配方链路

```
LoadAll()
  ├─ 读 Config/Recipes/RecipeIndex.json（缺失则空索引）
  ├─ 遍历 Config/Recipes/*.json（排除 RecipeIndex.json）
  │    单个文件反序列化失败 → Debug 输出后跳过，不阻断其余
  ├─ 索引与实际文件对齐（索引里有但文件没了的名字剔除）
  └─ 恢复 CurrentRecipeName，找不到则取第一个

SwitchRecipe → current = target → SaveIndex → OnRecipeSwitched
SaveRecipe   → 校验重名 → 改名则备份删旧文件 → 原子写 → 更新索引
DeleteRecipe → 拒绝当前激活项 → 备份后删文件 → 更新索引
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 配置按类型名做缓存键，全进程单例 | 多份副本会让"谁改的"变成悬案；单例 + `IsDirty` 才能判断要不要落盘 |
| `IsDirty` 递归而不靠调用方手动标记 | 嵌套对象（`DBConfig` / `LogCategoryPolicyConfig`）改值也要能触发保存，否则漏存 |
| `SetProperty` 统一入口 | 直接赋字段会绕过置脏 —— 属性 setter 里手写 `isDirty = true` 是同一件事的重复实现 |
| `Clone<T>` 走 JSON 往返 | 零反射样板，新增属性不用改克隆代码；代价是性能（配置对象都是小对象，可接受） |
| `CopyTo` 用 `Auto` 而非 `Replace` | `PopulateObject` 的 `Auto` 是"填充"嵌套对象、保持引用；`Replace` 会换新对象，让缓存的窄接口引用失效 |
| 反序列化配置文件用 `Replace` | 集合属性被构造函数初始化过，`Auto` 会往里追加，导致重启后数据翻倍 |
| 保存前先备份到 `Backup/` | 配置文件被人手改坏是现场常态，多版本备份是最低成本的后悔药 |
| 原子写（tmp → 删 → rename） | 直接覆盖写，写到一半断电就是半个 JSON，下次启动反序列化直接失败 |
| 配方索引与实际文件双向对齐 | 索引可能失真（手工删文件、拷贝目录），加载时以文件为准修正索引 |
| `RecipeIndex` 是 `internal` | 它是本库的实现细节，外部只该看到 `IRecipeService` |

## 8. 注意事项与 FAQ

🔴 **`ConfigBase.CopyTo` 与 `JsonConfigSerializer` 的 `ObjectCreationHandling` 是两套，别统一**
- 反序列化配置文件：`Replace`（否则集合追加翻倍）
- `CopyTo` 原地填充：`Auto`（否则嵌套对象引用被替换）
改成同一套，必有一边坏。

🔴 **属性 setter 必须走 `SetProperty`**。直接写字段不会置脏，`Save` 时会被静默跳过（`IsDirty == false` 直接 return，不报错）。

🔴 **`Get<T>()` 返回的是共享单例**，直接改就等于改运行中配置。配置页必须 `Clone<T>()` 拿草稿，保存时 `CopyTo` 回灌。

🔴 **配方名 `RecipeIndex` 是保留字**，`CreateNewRecipe` 会拒绝；已存在同名配方也会抛 `InvalidOperationException`。

🔴 **不能删除当前激活的配方**。要删先切到别的配方。

⚠️ **导入同名配方会直接覆盖，不弹确认**。接口注释写的是"弹框确认后覆盖"，但实现里没有弹框 —— 确认逻辑在 UI 层，服务层只管覆盖。

⚠️ **`ExportRecipe` 不做原子写**，直接 `File.WriteAllText` 到目标路径。导出到 U 盘、网络盘时中途拔掉会留半个文件，但那只影响导出副本，不影响库内配方。

⚠️ **配置不热生效**。`DevConfig` / `LogConfig` 改完必须重启进程；这是有意设计（见 LogDriver README 的 F3 决策）。

⚠️ **`RecipeParameter.Value` 是 `double`**，整数参数也走 double，显示层自己做格式化。

**Q：配置文件在哪？**
`<exe目录>/Config/`，由 `CopyConfigOnce` MSBuild 目标在首次构建时从 `Taoyl.UI.FormMain/config/` 模板拷贝，之后不再覆盖（保护现场修改）。

**Q：为什么 `DevConfig` / `BaseConfig` 的文件名要特殊映射？**
历史原因，文件名早于"按类型名命名"的约定。现在保留映射是为了不破坏现场已有文件。

**Q：新增一个配置类型要改哪里？**
只需继承 `ConfigBase` 并加无参构造 —— `ConfigService` 按类型名自动映射到 `Config/<类型名>.json`，不用注册。若要特殊文件名，改 `GetFilePath`。

## 9. 扩展指南

**A. 新增一个配置类型**

1. 继承 `ConfigBase`，属性 setter 一律 `SetProperty(ref field, value)`
2. 需要出现在配置页：给属性加 `[DisplayName("中文")]`（`ConfigFormRenderer` 靠它做标签）
3. 启动预加载：在 `Bootstrapper.InitConfigs` 加一行 `cfg.Get<新类型>()`（不加载也能用，但首次 `Get` 时机不可控）
4. 配置文件首次运行自动生成（用类里的字段默认值）

**B. 新增一个配方类型（新机型）**

1. 继承 `RecipeBase`，构造函数里填 `Parameters`（Key / DisplayName / Unit / Min / Max）
2. 定义 `public static class P` 暴露 Key 常量，业务层引用常量而不是字符串
3. 注册：`builder.RegisterGeneric(typeof(RecipeService<>)).As(typeof(IRecipeService<>))` 已覆盖开放泛型，无需改动
4. 启动加载：`Container.Resolve<IRecipeService<新机型Config>>().LoadAll()`
5. 配方文件落在 `Config/Recipes/<配方名>.json`，与其他机型共用同一索引文件

**C. 换序列化实现（如 System.Text.Json）**

实现 `IConfigSerializer` 两个方法，替换 `JsonConfigSerializer` 的注册即可。但注意 `ConfigBase.Clone<T>` / `CopyTo` 内部直接用了 `JsonConvert`，换库要一并改这两处。

## 10. 相关文档

- `Taoyl.Application.DataBase/README.md` —— `DevConfig.DB` 的消费方（连接串与 `DatabaseType`）
- `Taoyl.Application.LogDriver/README.md` —— `LogConfig` 的消费方（NLog overlay 与保留期清理）
- `Taoyl.UI.FormMain/README.md` —— `ConfigFormRenderer`：配置模型 → 界面控件的反射渲染
- `Taoyl.IFALaunch/Modules/ConfigModule.cs` —— 唯一的注册点
- `Taoyl.IFALaunch/Bootstrapper.cs` —— `InitConfigs` 启动时序
