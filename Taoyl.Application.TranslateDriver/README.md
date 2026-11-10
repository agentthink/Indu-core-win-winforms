# Taoyl.Application.TranslateDriver —— 翻译引擎 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**界面多语言的运行时引擎**：把"界面上的中文"变成"按当前语言查表得到的文本"，并管理这些文本的存储与提取。

**是什么**：语言状态与切换事件（`ITranslationLanguageService`）、运行时窗体翻译（`ITranslationService`）、资源条目的两级缓存与 JSON 落盘（`ResourceCache` / `ResourceLoader`）、控件翻译的三级策略（实例 → 精确类型 → 默认）、三段式键规则、`ToTranslate` 静态门面。

**不是什么**：不含任何翻译工具窗体（`FrmResourceConfig` / `FrmResourceScanner` 在 `Taoyl.UI.Pages`）、不调翻译 API（翻译内容靠人工维护或外部 Excel 导入）、不依赖任何 UI 控件库的编译期引用（唯一框架引用是 WinForms 基础类型）。

**核心约束**：引擎不编译期依赖 SunnyUI。`UISwitch` 这类第三方控件靠 `Type.GetType("Sunny.UI.UISwitch, SunnyUI")` 运行时解析 —— 宿主没有 SunnyUI 时自动失效，引擎照常工作。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| Newtonsoft.Json 13.0.4 | 资源文件的序列化/反序列化 |
| `Taoyl.Application.Share` | 无直接使用；本项目按分层惯例保留引用位（实际契约自包含） |
| .NET Framework WinForms | `Control` / `Form` / `TabControl` 等基础类型（仅 `System.Windows.Forms`，不含 SunnyUI） |

被依赖：`Taoyl.UI.FormMain`（主窗体翻译）、`Taoyl.UI.Pages`（各页面）、`Taoyl.IFALaunch`（组合根注册）。

翻译工具窗体（`FrmResourceConfig` / `FrmResourceScanner`）另需 MiniExcel + Microsoft.CodeAnalysis.CSharp + Newtonsoft.Json —— 那是窗体侧的显式依赖，与本库无关。

## 3. 功能与 API

### `ITranslationLanguageService`（面向 VM / 非 UI 消费方）

| 成员 | 说明 |
|---|---|
| `LanguageType CurrentLanguage { get; set; }` | 设置新语言后自动触发全局刷新 |
| `event Action<LanguageType> LanguageChanged` | 语言变更事件，VM 据此重算动态文本 |
| `bool AutoRegisterOnMiss { get; set; }` | `ToTranslate` 未命中时是否自动注册条目（默认 `false`） |

**接口内零 WinForms 类型** —— ViewModel 可以安全注入，不会把 UI 依赖带进 VM。

### `ITranslationService`（面向 UI 运行时）

继承 `ITranslationLanguageService`，额外提供：

| 成员 | 说明 |
|---|---|
| `void TranslateForm(Form form)` | 翻译指定窗体及其所有子控件（递归） |
| `string TranslateText(...)` | 翻译单个文本资源；**未命中返回 null**，调用方自行回退 |
| `void RegisterControl(Control control, ...)` | 为控件注册定制化的翻译与提取逻辑 |
| `void UseRecommendedExclusions()` | 一键添加推荐排除类型（滚动条、进度条、输入控件等） |

### `IResourceManager`（面向资源管理工具）

| 成员 | 说明 |
|---|---|
| `string BasePath { get; set; }` | 资源文件根目录（默认 `./Language`） |
| `event Action<string> ClassChanged` | 新类资源加入缓存时触发（界面树刷新） |
| `IEnumerable<ResourceEntry> GetClassResourcesMerged(string ns, string cls)` | 取指定类的全部条目（含与本地文件合并） |
| `IEnumerable<ResourceEntry> LoadEntriesFromFile(string path)` | 从 JSON 读条目 |
| `void AddRange(...)` / `void ClearClass(...)` | 批量添加更新 / 清空指定类缓存 |
| `void SaveClassResources(...)` | 按 `{BasePath}/{Type}/{Namespace}/{Class}.json` 落盘 |
| `void ExtractUIResources(...)` | 扫描窗体静态文本生成条目 |

### 枚举

| 枚举 | 成员 |
|---|---|
| `LanguageType` | `zh_CN` / `en_US` / `de_DE` / `hu_HU` / `id_ID` / `es_ES`（JSON 里语言代码把 `_` 换成 `-`） |
| `ResourceType` | `UI`（界面控件文本）/ `Text`（日志、消息、异常等非界面文本） |

### `ResourceEntry`

`Key`（唯一键）/ `Namespace` / `ClassName` / `ControlName` / `Type` / `OriginalText` / `Translations`；工厂 `CreateUI` / `CreateText`；`GetText(lang)` / `SetText(lang, text)`。

## 4. 快速开始

csproj 引用：

```xml
<ProjectReference Include="..\Taoyl.Application.TranslateDriver\Taoyl.Application.TranslateDriver.csproj" />
```

容器注册（`Taoyl.IFALaunch/Modules/SharedModule.cs`）：

```csharp
// 三个接口指向同一个 TransManager 单例：
//   FrmResourceConfig 属性注入 IResourceManager
//   VM 注入窄接口 ITranslationLanguageService（只消费语言状态/事件，不持有 WinForms 方法）
builder.Register(c => TransManager.Current)
       .As<ITranslationService>()
       .As<ITranslationLanguageService>()
       .As<IResourceManager>()
       .SingleInstance();
```

工具窗体另需在 `FormModule` 注册：

```csharp
builder.RegisterType<FrmResourceConfig>()
       .Named<Form>("Taoyl.UI.Pages.FrmResourceConfig")
       .PropertiesAutowired();     // 自动注入 IResourceManager
```

## 5. 使用示例

### 5.1 三种消费方各注入哪个接口

```csharp
// VM / 非 UI 消费方（动态文本链路）——窄接口，无 WinForms 类型
translationService.CurrentLanguage = LanguageType.en_US;      // 触发全局刷新
translationService.LanguageChanged += OnLanguageChanged;      // 订阅后重算动态文本

// UI 运行时（切语言 / 翻译窗体）——全量接口
translationService.TranslateForm(form);

// 资源管理（配置工具）——注入 IResourceManager
var entries = resourceManager.GetClassResourcesMerged(ns, cls).ToList();
resourceManager.SaveClassResources(entries, ResourceType.UI);
```

### 5.2 业务代码：`ToTranslate` 静态门面（无需注入）

```csharp
using Taoyl.Application.TranslateDriver.TranslateExtensions;

// 静态界面文本：直接调用；未命中仅返回原文
label.Text = "设备启动".ToTranslate(this);

// 动态文本：key 是静态语义键，值走 args
statusLabel.Text = "设备{0}运行中".ToTranslate(this, stationName);
```

### 5.3 翻译使用规范

| 场景 | 方式 |
|---|---|
| 静态界面文本（Designer / 渲染器写死） | `TranslateForm` 自动处理（控件必须有 `Name`；参数表单由 `ConfigFormRenderer` 的 `lbl_`/`grp_` 规约承载） |
| 运行时动态赋值的文本 | **赋值点调用 `ToTranslate`**（命令式），值随赋值时机跟随当前语言；工控轮询刷新场景天然随切语言更新 |
| 输入控件（TextBox 家族） | 一律排除，不翻译不提取（参数值保护） |
| 一次性动态文本（赋值后不刷新） | 切语言不自动更新 —— 文本跟随数据源生命周期，属正确语义 |
| 外部 Excel 翻译 | 只改语言列，**勿动 Namespace/ClassName/资源键列**（决定资源归属） |
| key 规范 | 传静态语义键，动态值走 `params args`，杜绝动态 key 雪球 |
| `ToTranslate<T>` 的 T | **必须等于调用点所在类**（Text 资源归产生者）。写窗体类会导致提取产物归属与消费端查询路径不一致而永远未命中；提取器会 Debug 警告 |
| ListView 动态项 | **运行时动态增删的项必须设置 `item.Name` 作翻译 key**；索引 key（`Item_n`）仅适用于静态列表 |
| UISwitch 开关 | 三段式键 `UI.{控件Name}.Active` / `UI.{控件Name}.InActive`，由 `UISwitchTranslator` 自动处理 |
| 普通控件 / TabPage / 树节点 / DGV 列头 | 三段式键 `UI.{控件名}.{属性名}`（`.Text` / `.HeaderText`），各翻译器自动生成 |

### 5.4 资源文件布局

```
{BasePath}/UI/{Namespace}/{Class}.json    界面控件资源（归窗体类：TranslateForm 按窗体类查询）
{BasePath}/Text/{Namespace}/{Class}.json  文本/日志资源（归产生者：ToTranslate<T> 的 T = 调用点所在类）
```

默认 `BasePath = ./Language`。

## 6. 类库结构

```
Taoyl.Application.TranslateDriver/
├── Core/                         实体与枚举
│   ├── ResourceEntry.cs          资源条目实体（键规则、多语言字典、工厂方法）
│   └── LanguageType.cs           LanguageType（语言枚举）+ ResourceType（UI/Text 分类）
├── Interfaces/                   对外契约（UI 层与资源管理界面依赖此层）
│   ├── ITranslationLanguageService.cs  语言窄契约（VM 注入：语言状态/事件，无 WinForms 类型）
│   ├── ITranslationService.cs    UI 运行时翻译契约（继承窄契约 + 窗体翻译等，TransManager 实现）
│   └── IResourceManager.cs       资源管理契约（FrmResourceConfig 等配置工具注入使用）
├── Management/
│   └── TransManager.cs           翻译管理器单例 Facade（实现上述两接口，引擎核心）
├── Storage/                      资源存储层
│   ├── IResourceCache.cs         IResourceCache + IResourceLoader 接口
│   ├── ResourceCache.cs          两级并发缓存（类 → 条目），键规则唯一实现处
│   ├── ResourceLoader.cs         JSON 文件读写、路径推断、键保留策略
│   └── ResourceMapper.cs         ResourceEntry ↔ DataTable 双向映射（网格编辑/Excel 交换格式，无 UI 依赖）
├── Translation/                  控件翻译策略层
│   ├── IControlTranslator.cs     翻译器契约（Translate/ExtractEntries）
│   ├── IControlTranslationStrategy.cs  策略中心契约
│   ├── ControlTranslationStrategy.cs   三级策略中心（实例→精确类型→默认）+ 排除类型
│   ├── StandardControlTranslator.cs    默认兜底翻译器（TextBox 家族双保险排除）
│   ├── TabControlTranslator.cs   TabControl/UITabControl 页签翻译
│   ├── TreeViewTranslator.cs     TreeView 节点翻译
│   ├── ListViewTranslator.cs     ListView 列头/项翻译
│   ├── DataGridViewTranslator.cs DataGridView 列头翻译
│   ├── UISwitchTranslator.cs     SunnyUI UISwitch 扩展翻译器（反射 ActiveText/InActiveText，零编译期依赖）
│   ├── ManualControlTranslator.cs 委托驱动的实例翻译器
│   └── NoOpTranslator.cs         排除控件的空翻译器
├── TranslateExtensions/
│   └── TranslateExtensions.cs    ToTranslate 扩展方法（业务代码翻译入口，静态门面）
└── Taoyl.Application.TranslateDriver.csproj
```

### 逐代码文件说明

| 文件 | 职责 | 关键成员 / 实现要点 |
|---|---|---|
| `Core/ResourceEntry.cs` | 资源条目实体 | `Key`（唯一键）、`Namespace/ClassName/ControlName/Type/OriginalText/Translations`；`GenerateKey()` 规则 `{Type}.{ControlName\|8位Guid}`；`GetText/SetText`（语言代码 `_`→`-`）；工厂 `CreateUI/CreateText` |
| `Core/LanguageType.cs` | 语言与资源类型枚举 | `LanguageType`（zh_CN/en_US/de_DE/hu_HU/id_ID/es_ES）；`ResourceType`（UI/Text） |
| `Interfaces/ITranslationLanguageService.cs` | 语言窄契约 | `CurrentLanguage`、`LanguageChanged`、`AutoRegisterOnMiss`；**无任何 WinForms 类型**，供 ViewModel 注入订阅语言切换 |
| `Interfaces/ITranslationService.cs` | UI 运行时契约 | 继承窄契约，另含 `TranslateForm`、`TranslateText`、`RegisterControl`、`UseRecommendedExclusions` |
| `Interfaces/IResourceManager.cs` | 资源管理契约 | `BasePath`、`ClassChanged`、`GetClassResourcesMerged`、`LoadEntriesFromFile`、`AddRange`、`ClearClass`、`SaveClassResources`、`ExtractUIResources` |
| `Management/TransManager.cs` | 单例 Facade，引擎核心 | `Current`（Lazy 单例 + 测试钩子）；实现两接口；`EnsureClassLoaded` 懒加载（含非法路径字符防御）；键语义唯一编排点 |
| `Storage/IResourceCache.cs` | 存储接口 | `IResourceCache`（增删查事件）、`IResourceLoader`（LoadAll/LoadFile/SaveClassResources） |
| `Storage/ResourceCache.cs` | 两级缓存 | 一级 `{Namespace}.{ClassName}`，二级 **dict 键 = entry.Key（单一事实来源）**；`Get` 先精确后按 ControlName 遍历兜底；`ConflictLogs` 原文冲突日志 |
| `Storage/ResourceLoader.cs` | JSON 读写 | `LoadFile` 空 Key 兜底生成、**文件已有 Key 原样保留**；`SaveClassResources` 按 `BasePath/{Type}/{Namespace}/{Class}.json` 落盘 |
| `Storage/ResourceMapper.cs` | 表格交换格式 | `ToList`（DataTable→条目）与 `ToDataTable`（条目→DataTable）；仅依赖 System.Data，供工具窗体的网格编辑与 Excel 导入导出 |
| `Translation/IControlTranslator.cs` | 翻译器契约 | `CanTranslate`、`Translate`、`ExtractEntries` |
| `Translation/IControlTranslationStrategy.cs` | 策略中心契约 | `GetTranslator`、精确翻译器注册/移除、排除类型管理、手动翻译器注册 |
| `Translation/ControlTranslationStrategy.cs` | 三级策略中心 | 实例 → 精确类型（含基类匹配）→ 默认；排除类型优先于一切；`RecommendedExcludedTypes` 计算属性：WinForms 编译期 + **SunnyUI 类型名运行时 `Type.GetType` 解析** |
| `Translation/StandardControlTranslator.cs` | 默认兜底 | `CanTranslate => true`；`Translate/ExtractEntries` 对 `TextBoxBase` 直接跳过（参数值保护双保险） |
| `Translation/TabControlTranslator.cs` | Tab 页签翻译 | 基于 `TabControl` 基类处理（UITabControl 是子类，多态覆盖） |
| `Translation/TreeViewTranslator.cs` | 树节点翻译 | 节点文本递归翻译/提取 |
| `Translation/ListViewTranslator.cs` | 列表翻译 | 列头/项文本 |
| `Translation/DataGridViewTranslator.cs` | 表格翻译 | 列头文本（不碰单元格值） |
| `Translation/UISwitchTranslator.cs` | SunnyUI UISwitch 扩展翻译器 | UISwitch 继承 `UIControl`（无 WinForms 文本基类），开关两态文本由反射处理；类型运行时解析、属性缓存，宿主无 SunnyUI 时自动失效 |
| `Translation/ManualControlTranslator.cs` | 委托实例翻译器 | 由调用方提供 apply/extract 委托 |
| `Translation/NoOpTranslator.cs` | 空翻译器 | 排除控件专用，不翻译不提取 |
| `TranslateExtensions/TranslateExtensions.cs` | ToTranslate 静态门面 | 3 个重载（object context / T / Type）；未命中按 `AutoRegisterOnMiss` 决定是否自动注册；args 格式化安全处理 |

## 7. 内部逻辑

### 核心数据流

```
翻译读路径： 业务代码 ToTranslate → TransManager.TranslateText → EnsureClassLoaded（懒加载 JSON）
             → ResourceCache.Get → 返回当前语言文本（未命中 → fallback，按开关自动注册占位）

窗体翻译：   TranslateForm → 逐控件 GetTranslator（三级策略）→ IControlTranslator.Translate → 设文本

资源写路径： FrmResourceConfig 编辑 → ResourceMapper → ClearClass + AddRange → SaveClassResources 落盘

提取路径：   FrmResourceScanner（Roslyn 扫源码 ToTranslate）→ 导出 Excel/JSON → FrmResourceConfig 导入合并
```

### 键规则（单一事实来源）

UI 资源统一三段式键：`entry.Key = "{Type}.{控件名/语义键}.{属性名}"`。

| 控件类型 | 属性后缀 | 示例 |
|---|---|---|
| 普通控件 / TabPage / TreeView 节点 / ListView 项 | `.Text` | `UI.btnStart.Text` |
| DataGridView 列头 | `.HeaderText` | `UI.colStatus.HeaderText` |
| ListView 组 | `.Header` | — |
| UISwitch 开关 | `.Active` / `.InActive` | 两态各一条 |
| 窗体标题 | `{窗体类名}.Text` | — |
| Text 资源（`ToTranslate` 语义键） | 不受此规则约束 | ControlName 即语义键本身 |

配套规则：

- `ControlName` 为空时用 8 位 GUID
- **缓存 dict 的二级键统一用 `entry.Key`**（不再用 `{Type}.{ControlName}` 重拼）—— 杜绝键分裂
- `ResourceCache.Get`：先精确匹配，未命中再遍历该类字典按 `ControlName` 兜底（兼容自定义 Key / GUID Key 的历史条目）
- `ResourceLoader.LoadFile`：**文件已有 Key 原样保留**，仅空 Key 兜底生成 —— 修复了每次加载重新生成 GUID 导致键冲突堆积的问题

### 自动注册语义（`AutoRegisterOnMiss`）

| 取值 | 行为 |
|---|---|
| `false`（默认） | `ToTranslate` 未命中**仅返回原文，无任何写副作用** —— 读路径纯净，业务代码放心调用 |
| `true` | 未命中自动创建条目加入**内存缓存**（随后 `FrmResourceConfig` 中可见，人工补翻译后保存）。**只进内存不落盘** —— 自动写盘与手动资源管理冲突 |

翻译提取统一走 `FrmResourceConfig` 的「获取UI资源」：**先提取 → 人工翻译 → 保存落盘**，流程显式可控。

### 输入控件排除（参数值保护）

- 输入/数值类控件（TextBox / MaskedTextBox / NumericUpDown / DateTimePicker + SunnyUI 输入控件）**不参与翻译与提取** —— `Text` 是数据不是界面文本
- 双保险：① `RecommendedExcludedTypes` 排除集合（WinForms 编译期 + SunnyUI 运行时解析）；② `StandardControlTranslator` 对 `TextBoxBase` 直接跳过
- ⚠️ `UITextBox` 继承 `UIPanel` 而非 `TextBoxBase`，双保险对它失效，只靠排除集合兜住
- **例外**：`UISwitch` 不在排除之列 —— 开关文本是界面文本，由 `UISwitchTranslator` 翻译

### 语言切换与刷新

`CurrentLanguage` 变更 → 触发 `LanguageChanged` + `RefreshAllOpenForms`（遍历已打开窗体递归翻译）。动态赋值的文本不在其列（见 §5.3）。

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 三个接口，同一实现 | VM 只需要语言状态时不该拿到 `TranslateForm(Form)` 这类 WinForms 方法；窄接口让"VM 零 UI 依赖"在结构上成立 |
| 引擎不编译期引 SunnyUI | 翻译库要能被任何 WinForms 项目复用；`UISwitch` 靠反射解析，宿主没有 SunnyUI 就自动失效 |
| 三段式键而非纯语义键 | 提取器扫源码时只能看到控件名，看不到业务语义；三段式让"自动提取"与"手工查询"用同一套键 |
| 缓存二级键用 `entry.Key` | 若用 `{Type}.{ControlName}` 重拼，自定义 Key 的条目就查不到 —— 键分裂是最难查的一类 bug |
| 已有 Key 原样保留 | 每次加载重新生成 GUID，会让同一个控件在不同次启动里拿到不同键，资源文件越存越乱 |
| `AutoRegisterOnMiss` 默认关且不落盘 | 读路径带写副作用，会让业务代码的调用顺序影响资源文件内容；落盘更把影响永久化 |
| 排除集合 + 翻译器双重排除 | 排除集合可被误清空，翻译器层的 `TextBoxBase` 判断是最后一道 |
| `ToTranslate<T>` 的 T 必须是调用点类 | Text 资源按命名空间+类名归档；写窗体类会让提取产物与查询路径不一致，永远查不到 |

## 8. 注意事项与 FAQ

🔴 **`ToTranslate<T>` 的泛型参数必须是调用点所在类**，不能图省事写窗体类。提取器会 Debug 警告，但不会拦你 —— 表现为"翻译了但界面上没变"。

🔴 **动态 key 是资源杀手**。`($"设备{id}启动").ToTranslate(this)` 每来一个新 id 就生成一条垃圾条目。正确写法：`"设备{0}启动".ToTranslate(this, id)`。

🔴 **运行时动态增删的 ListView 项必须设 `item.Name`**。索引 key（`Item_n`）只在项顺序固定的静态列表里成立。

🔴 **输入控件的 `Text` 是数据不是界面文本**，一律排除。只读展示用 Label / Button 承载。

⚠️ **`TranslateText` 未命中返回 null**（不是原文）。要用它就得自己处理回退，日常用 `ToTranslate` 更安全。

⚠️ **切语言不刷新一次性赋值的文本**：文本跟随数据源生命周期，属正确语义。要跟随语言就改走 `ToTranslate`。

⚠️ **`AutoRegisterOnMiss = true` 只进内存**，重启即失。要落地必须去 `FrmResourceConfig` 保存。

⚠️ **外部 Excel 只改语言列**。动 `Namespace` / `ClassName` / 资源键列等于改资源归属，已翻译的内容会全部失联。

**Q：为什么提取器和运行时要用两套代码扫同一批文本？**
提取器（`FrmResourceScanner`，Roslyn 扫源码）能覆盖手写代码里的 `ToTranslate`；运行时扫描（`ExtractUIResources`）能覆盖 Designer 生成的静态文本。两者互补，缺一个就有资源漏提取。

**Q：`TransManager.Current` 静态单例还要用吗？**
保留兼容。它与 DI 注入的实例是**同一个对象**，但新代码应走注入 —— 静态单例没法替换实现，也没法在测试里隔离。`SetCurrentForTesting` / `ResetCurrent` 是测试钩子。

**Q：翻译文件放哪？**
默认 `./Language`（即 `<exe>/Language`），可由 `IResourceManager.BasePath` 覆盖。

## 9. 扩展指南

**A. 新增一种语言**

1. `LanguageType` 加成员（格式 `语言_区域`，如 `ja_JP`）
2. 已有资源文件补该语言的翻译列 —— `ResourceEntry.Translations` 是字典，键为语言代码（`_`→`-`）
3. 配置界面语言下拉自动出现（走 `Enum.GetValues`），无需改 UI

**B. 新增一种控件翻译器**

1. `Translation/` 下加 `XxxTranslator : IControlTranslator`，实现 `CanTranslate` / `Translate` / `ExtractEntries`
2. 在 `ControlTranslationStrategy` 构造里注册为精确类型（或实例级）
3. 若该控件属输入类，同时加进 `RecommendedExcludedTypes` —— 否则会被默认翻译器误翻
4. 键的后缀按 §7 的表选定，不要自创第四段

**C. 新增一种资源类型（如 `Tooltip`）**

1. `ResourceType` 加成员
2. `ResourceLoader` 的路径模板 `{BasePath}/{Type}/...` 天然支持，无需改
3. `ResourceMapper` 的列映射要确认新类型是否需要额外列
4. ⚠️ 这是一个"牵一发动全身"的改动：`ResourceType` 参与落盘路径，改错会让存量文件全部找不到

**D. 接入第三方控件库（如 DevExpress）**

1. 不要加编译期引用 —— 照 `UISwitchTranslator` 的做法，用 `Type.GetType("Xxx.Yyy, Assembly")` 运行时解析
2. 在 `RecommendedExcludedTypes` 或新翻译器里处理
3. 宿主没有该库时，`Type.GetType` 返回 null，翻译器自动失效，引擎不受影响

## 10. 相关文档

- `Taoyl.UI.FormMain/README.md` —— `TranslateForm` 在页面装载链里被调用（屏幕外装载时"呈现前翻译"）
- `Taoyl.Application.Share/README.md` —— 契约层惯例（本库契约自包含，未使用 Share 类型）
- `Taoyl.IFALaunch/Modules/SharedModule.cs` —— 唯一的注册点
- `Taoyl.UI.Pages/Views/FrmResourceConfig.cs` —— 资源管理工具窗体（本库的最大消费方）
