# Taoyl.UI.FormMain —— 主框架 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**主窗体的壳与骨架**：登录、主窗口、菜单树、页面导航、配置界面自动生成、通用 UI 工具。业务页面本身不在这里（在 `Taoyl.UI.Pages`）。

**是什么**：`FormLogin` / `FormMain` 两个窗体、`MenuManager`（菜单加载 + 权限过滤 + 页面解析）、`NavigationService`（导航事件）、`IMessageBoxService`（消息框抽象）、`IUserActivityMonitor`（无操作自动登出）、`ConfigFormRenderer`（配置模型 → 控件的反射渲染）、`UiDgvHelper` / `UiDpi` / `UiControlHelper`（UI 通用工具）。

**不是什么**：不含任何业务页面（产品查询、报警、审计、PLC 调试都在 `Taoyl.UI.Pages`）、不做权限判定（只消费 `AuthService` 给出的权限码集合）、不做业务校验、不碰数据库。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| SunnyUI 3.9.1 | `UIForm` / `UITreeView` / `UIPanel` / `UISwitch` / `UIComboBox` 等控件，`UIMessageBox` 消息框 |
| Autofac 7.1.0 | `IComponentContext` 按名字解析页面（`TryResolveNamed`） |
| CommunityToolkit.Mvvm 8.2.2 | `ObservableObject` / `[ObservableProperty]` / `IRelayCommand` 源生成器 |
| `Taoyl.Application.AuthService` | `IAuthService`（登录/登出/权限码）、`IPermissionService` |
| `Taoyl.Application.Audit` | `IAuditService`（登录事实与页面操作留痕） |
| `Taoyl.Application.Configuration` | `IConfigService`（`DevConfig` / `BaseConfig`）、`ConfigBase`（配置渲染的数据源） |
| `Taoyl.Application.Device` | 设备列表与状态（主界面展示） |
| `Taoyl.Application.LogDriver` | `ILogService` / `ILogDisplay`（状态栏日志） |
| `Taoyl.Application.TranslateDriver` | `ITranslationService`（界面多语言） |
| `Taoyl.Application.Share` | `ConfigPathName` / 权限码常量 / `IAutoLogoutSettings` 等窄契约 |
| `Taoyl.Application.ProcessMonitoring` | 流程监控聚合状态（主界面状态指示） |

被依赖：仅 `Taoyl.IFALaunch`（组合根，`FormModule` 注册）。

## 3. 功能与 API

### `MenuManager`（`Services/MenuManager.cs`）

| 成员 | 签名 | 说明 |
|---|---|---|
| 构造 | `MenuManager(IComponentContext context)` | 注入容器上下文，用于按名字解析页面 |
| 加载 | `bool LoadMenuConfig()` | 读 `Config/Menu.json`，失败返回 false |
| 菜单名 | `string MenuName { get; }` | 配置里的 `MenuName` |
| 全量 | `IReadOnlyList<MenuItem> GetAllMenuItems()` | 拍平所有层级（含子节点） |
| 过滤 | `List<MenuItem> GetMenuItemsByPermission(HashSet<string> userPermissionCodes)` | 按 `Page:<Name>` 权限码递归过滤，结果按 `SortOrder` 排序 |
| 实例 | `Form GetFormInstance(MenuItem menuItem)` | 用 `DllName.Class` 拼 `formKey` → `TryResolveNamed`；**不自持缓存**，容器即唯一实例缓存 |
| 默认页 | `MenuItem GetDefaultMenuItem(HashSet<string> userPermissionCodes)` | 优先 `IsDefault`，其次第一个有权限的页面 |
| 自检 | `List<string> GetUnregisteredPages()` | 返回 Menu.json 里配了但容器没注册的页面（启动自检用） |

### `NavigationService`（`Services/NavigationService.cs`）

| 成员 | 签名 | 说明 |
|---|---|---|
| 事件 | `event EventHandler<NavigationEventArgs> Navigated` | 导航完成（`MenuName` + `Form`） |
| 导航 | `void NavigateTo(string menuName)` | 解析菜单 → 取实例 → 触发事件。**不操作任何控件** |
| 返回 | `void GoBack()` | **空实现**（预留接口，导航历史栈尚未实现） |

导航服务只负责"解析 + 通知"，窗体怎么显示由 View 决定（本库默认是"屏幕外装载后切面板"）。

### `IMessageBoxService`（`Services/SunnyUIMessageBoxService.cs`）

| 成员 | 签名 |
|---|---|
| 错误 | `void ShowError(string message, bool showMask = true)` |
| 成功 / 提示 / 警告 | `ShowSuccess` / `ShowInfo` / `ShowWarning`（同签名） |
| 确认 | `bool ShowConfirm(string message, bool showMask = true)` / `ShowConfirm(string message, string title, bool showMask = true)` |
| 询问 | `bool ShowAsk(string message, bool showMask = true)` |
| 异常 | `void ShowException(Exception ex, bool showMask = true)` / `ShowErrorDetail(string message, string detail, bool showMask = true)` |

抽象出接口是为了让 ViewModel 不直接引用 SunnyUI —— VM 可单测、可替换实现。

### `IUserActivityMonitor`（`Services/UserActivityMonitor.cs`）

| 成员 | 说明 |
|---|---|
| `int TimeoutSeconds` | 无操作超时阈值（来自 `IAutoLogoutSettings.AutoLogoutTime`） |
| `bool IsMonitoring` | 是否在监控 |
| `event Action<int> CountdownUpdated` | 剩余秒数变化（状态栏倒计时） |
| `event Action AutoLogoutRequired` | 超时触发（主窗体据此登出） |
| `Start()` / `Stop()` / `RecordActivity()` / `ResetTimer()` / `GetRemainingSeconds()` | 生命周期与手动干预 |

内部用 `UserActivityMessageFilter : IMessageFilter` 挂在消息泵上，鼠标/键盘消息即算活动 —— 不依赖控件事件，空白处点击也算。

### `ConfigFormRenderer`（`Helpers/ConfigFormRenderer.cs`）

| 成员 | 签名 | 说明 |
|---|---|---|
| 渲染 | `static void Render(Control target, List<ConfigFlatRow> rows, int valueWidth = 0)` | 扁平行列表 → `TableLayoutPanel`。`valueWidth > 0` 时值列定宽（宿主 `AutoScroll` 出横滚），`≤ 0` 时自适应宿主宽 |
| 只读 | `static void SetReadOnly(Control root, bool readOnly)` | 递归覆盖整棵控件树（含字典区段内部） |

渲染三分支（按 `ConfigRowKind` 分派）：

| 行类型 | 渲染 |
|---|---|
| `Property` | 按属性类型选编辑器（bool → 开关；枚举 → 下拉；数值/字符串 → 文本框），通过 `DataBindings` 双向绑定到实例 |
| `GroupHeader` | 跨列分组标题 |
| `DictGroupHeader` | 整行交给 `ConfigDictSection` 自持（组头 + 添加参数栏 + 键值行），字典增删改全在区段内部完成 |

`ConfigFlatModel.Flatten(object config, int level)` 把嵌套配置对象拍平成 `List<ConfigFlatRow>`：递归展开 `ConfigBase` 属性、`IDictionary` 属性（`ProtocolParams` 这类）、枚举（生成 `OptionsSource`）。

### `MenuItem` / `MenuConfig`（`Models/`）

| 成员 | 说明 |
|---|---|
| `Name` | 菜单标识，权限码 = `"Page:" + Name` |
| `DisplayName` / `Description` / `Icon` / `SortOrder` | 展示 |
| `Level` | 旧权限等级（保留兼容，新代码用权限码） |
| `Class` / `DllName` | 页面定位：`DllName.Class` = Autofac Named 名 = `typeof(T).FullName` |
| `IsDefault` | 登录后默认打开 |
| `Children` / `IsVisible` / `IsFormNode()` / `IsParentNode()` / `Clone()` | 树结构与复制 |

`MenuConfig`：`MenuName` / `Version` / `Description` / `MenuItems`。

### UI 工具

| 类 | 关键成员 | 用途 |
|---|---|---|
| `UiControlHelper` | `static void DisposeAndClear(Control host)` | 清空子控件**并释放**——只 `Clear()` 的话句柄不销毁，等 GC 走终结器线程 Dispose，句柄回收滞后 |
| `UiDgvHelper` | `Setup(DataGridView, Font cellFont, Font headerFont, Font controlFont, bool showRowNumber)`；`TryCopyFocusedCell(ref Message, Keys, params DataGridView[])` | 统一表格样式 + `Ctrl+C` 复制当前单元格 |
| `UiDpi` | `DpiScale` / `Scale(int)` / `EnableDoubleBuffered` / `SetRedraw` | 高 DPI 缩放与重绘控制 |
| `ConfigFormStyle` | `LabelWidth` / `RowHeight` / `GroupHeight` / 各种 `Padding` | 配置界面尺寸常量与 `ToolTip` 管理 |

## 4. 快速开始

容器注册（`Taoyl.IFALaunch/Modules/FormModule.cs`）：

```csharp
// 主窗体：单例 + 属性注入（窗体依赖由容器填，不走构造）
builder.RegisterType<Taoyl.UI.FormMain.FormMain>()
       .PropertiesAutowired()
       .SingleInstance();

// 登录窗体：每次新建
builder.RegisterType<Taoyl.UI.FormMain.FormLogin>()
       .PropertiesAutowired()
       .InstancePerDependency();

// 页面：Named 名 = typeof(T).FullName，Menu.json 的 DllName + Class 必须与之一致
RegisterPage<Taoyl.UI.Pages.FormMainPage>(builder);
```

`RegisterPage<T>` 的实现（`FormModule` 内）：

```csharp
builder.RegisterType<T>()
       .Named<Form>(typeof(T).FullName)      // key 必须与 Menu.json 的 DllName.Class 逐字一致
       .SingleInstance();
```

启动顺序（`Bootstrapper`）：

```csharp
InitConfigs();      // 配置先落地
InitDatabase();     // CodeFirst + 权限同步
InitComponents();   // 消息框、活动监控、报警弹窗等 UI 服务
// 然后 Application.Run(Container.Resolve<FormMain>())
```

## 5. 使用示例

### 5.1 Menu.json 里怎么配一个页面

```json
{
  "Name": "DataQuery",
  "DisplayName": "数据查询",
  "Description": "产品数据筛选查询与导出",
  "Class": "FormProductQuery",
  "DllName": "Taoyl.UI.Pages",
  "Icon": "数据查询",
  "Level": 0,
  "SortOrder": 2,
  "Features": [
    { "Code": "Btn:Product_Export", "Name": "导出产品数据", "Level": 300 }
  ]
}
```

三处契约必须对齐：

| 位置 | 值 |
|---|---|
| `Menu.json` | `DllName` = `Taoyl.UI.Pages`，`Class` = `FormProductQuery` |
| 注册 | `RegisterPage<Taoyl.UI.Pages.FormProductQuery>` → Named 名 `Taoyl.UI.Pages.FormProductQuery` |
| 权限 | `Page:DataQuery`（`"Page:" + Name`）写进 `Menu.json` 的菜单权限，由 `PermissionSyncService` 同步入库 |

🔴 `Menu.json` 有**两份**：`Taoyl.UI.FormMain/config/Menu.json`（版本库模板）与 `<exe>/Config/Menu.json`（运行时）。改模板不会覆盖现场文件，两份都要改。

### 5.2 按权限出菜单

```csharp
// MainViewModel 在登录成功 / 权限变化时刷新 MenuItems
List<MenuItem> menus = menuManager.GetMenuItemsByPermission(authService.CurrentPermissions);

// FormMain 的私有方法：递归把 MenuItem 建成 TreeNode（分组节点无 Class/DllName，页面节点有）
BuildMenuTree(menus, null);
```

过滤规则：自身没 `Page:<Name>` 权限、但子节点有权限 → **保留为导航容器**；自己和子节点都没权限 → 整枝剔除。

### 5.3 导航到页面（屏幕外装载）

```csharp
// FormMain 响应 NavigationService.Navigated 的真实实现（已简化，完整版含异常兜底）
void ShowPage(Form form)
{
    if (currentForm != null)
        mainPanel.Controls.Remove(currentForm);      // 只摘当前页，不清空整个面板

    form.TopLevel = false;
    form.FormBorderStyle = FormBorderStyle.None;

    // 屏幕外装载：OnLoad 由 Show() 触发，把它挪到可视区外跑完，装载中间态一帧都不露面
    form.Location = new Point(-32000, -32000);
    form.Size = mainPanel.ClientSize;

    mainPanel.SuspendLayout();
    form.SuspendLayout();
    try
    {
        translationService?.TranslateForm(form);     // 懒加载页面：呈现前补翻译
        mainPanel.Controls.Add(form);
        form.Show();
    }
    finally
    {
        form.ResumeLayout(true);
        mainPanel.ResumeLayout(true);
    }

    // 🔴 Dock 必须在装载之后设：提前设会覆盖 Location，页面立刻回到可视区
    form.Dock = DockStyle.Fill;
    currentForm = form;
}
```

🔴 不要用 `form.Opacity` 做过渡：嵌入窗体 `TopLevel = false`，WinForms 的 `AllowTransparency` / `UpdateLayered` 都硬检查 `TopLevel`，会被静默忽略。防闪靠各页面自己的控件级双缓冲。

### 5.4 用配置模型自动生成设置界面

```csharp
// rows 由配置对象拍平而来
List<ConfigFlatRow> rows = ConfigFlatModel.Flatten(devConfig, level: 0);
ConfigFormRenderer.Render(panelBody, rows);          // 自适应宽
ConfigFormRenderer.SetReadOnly(panelBody, readOnly: true);   // 只读会话
```

- 属性标签取 `[DisplayName("中文")]`
- 枚举自动变下拉（`[Description]` 做显示文案）
- `IDictionary<string,string>` 属性（如 `PlcConfig.ProtocolParams`）渲染成可增删的键值区段

### 5.5 表格统一初始化

```csharp
UiDgvHelper.Setup(dgv, cellFont, headerFont, controlFont, showRowNumber: false);
// Ctrl+C 复制当前单元格
protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
    => UiDgvHelper.TryCopyFocusedCell(ref msg, keyData, dgv) || base.ProcessCmdKey(ref msg, keyData);
```

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| **Views/** | |
| `FormLogin.cs` | 登录窗体（账号密码 + 刷卡），`GetUsername()` / `GetPassword()` 供主窗体首次登录用 |
| `FormMain.cs` | 主窗体：菜单树、页面面板、状态栏、登出、自动登出联动 |
| **ViewModels/** | |
| `ViewModelBase.cs` | 继承 `ObservableObject`，加 `IDisposable` |
| `LoginViewModel.cs` | 登录命令、错误提示、`LoginCompleted` / `LoginCancel` 事件 |
| `MainViewModel.cs` | 流程控制按钮（开始/暂停/复位）、系统状态、登录/登出、导航请求事件 |
| **Services/** | |
| `MenuManager.cs` | 菜单加载、权限过滤、页面解析、自检 |
| `NavigationService.cs` | 导航事件与解析编排 |
| `SunnyUIMessageBoxService.cs` | `IMessageBoxService` 的 SunnyUI 实现 |
| `UserActivityMonitor.cs` | 无操作自动登出（消息过滤器 + 倒计时事件） |
| **Helpers/** | |
| `ConfigFlatModel.cs` | `ConfigRowKind` / `ConfigFlatRow` / `Flatten`（配置对象 → 扁平行） |
| `ConfigFormRenderer.cs` | 扁平行 → `TableLayoutPanel` 控件树（三分支 + 只读递归） |
| `ConfigDictSection.cs` | 字典属性区段（组头 + 添加栏 + 键值行，自持增删改） |
| `ConfigFormStyle.cs` | 尺寸常量与 `ToolTip` 生命周期 |
| `UiControlHelper.cs` | `DisposeAndClear`：摘除即释放 |
| `UiDgvHelper.cs` | 表格统一样式 + `Ctrl+C` |
| `UiDpi.cs` | DPI 缩放、双缓冲、重绘开关 |
| **Models/** | |
| `MenuConfig.cs` | 菜单文件根模型 |
| `MenuItem.cs` | 菜单节点（含 `IsFormNode` / `IsParentNode` / `Clone`） |

## 7. 内部逻辑

### 启动与登录链

```
Program.Main
  → Bootstrapper.BuildContainer()      容器构建（12 个 Module）
  → InitConfigs()                      配置预加载
  → InitDatabase()                     CodeFirst + PermissionSyncService
  → InitComponents()                   消息框、活动监控、报警弹窗
  → Application.Run(FormMain)
        FormMain.OnLoad
          ├─ 未登录 → ShowLoginDialog（FormLogin 每次新建）
          │     登录成功 → AuthService 装载会话与权限码 → Audit 记登录事实
          ├─ menuManager.LoadMenuConfig()      读 Config/Menu.json
          ├─ GetMenuItemsByPermission(codes)   按权限建树
          ├─ GetDefaultMenuItem(codes)         打开默认页
          └─ userActivityMonitor.Start()       启动无操作倒计时
```

### 页面解析链

```
Menu.json（DllName + Class）
   ↓ MenuManager.GetFormInstance
formKey = DllName + "." + Class
   ↓ IComponentContext.TryResolveNamed<Form>(formKey)
RegisterPage<T> 注册的 Named 名 = typeof(T).FullName
   ↓ 命中
Form 实例（SingleInstance：容器是唯一缓存）
```

启动自检：`FormMain.ValidatePageRegistrations` 遍历 Menu.json 的页面节点，用 `IsRegisteredWithName` 预检（查注册表、不实例化），缺失就在启动时弹窗列出 —— 把"点开才炸"前移到"起来就报"。

### 配置界面渲染链

```
配置对象（ConfigBase 子树）
   ↓ ConfigFlatModel.Flatten(config, level)
List<ConfigFlatRow>（Kind / Name / Level / Target / Property / Dict / OptionsSource）
   ↓ ConfigFormRenderer.Render(panel, rows)
TableLayoutPanel
   ├─ Property        → 按类型造编辑器 + DataBindings 双向绑定到 Target
   ├─ GroupHeader     → 跨列标题
   └─ DictGroupHeader → ConfigDictSection 自持（字典本体增删改）
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 页面按 `DllName.Class` 解析，不用 switch | 加页面只改 Menu.json + 一行注册，主窗体不认识任何业务页 |
| `RegisterPage` 的 Named 名用 `typeof(T).FullName` | 类改名时注册名自动跟随，只有 Menu.json 一侧需要人工维护（且启动自检会报） |
| 容器即唯一页面缓存 | 自己再存一份字典，窗体 Dispose 后容器仍持旧引用 → 僵尸单例；干脆不自持 |
| 权限过滤保留"仅有子节点权限"的父节点 | 否则有子页权限的用户看不到入口，菜单树断裂 |
| 屏幕外装载后再入面板 | `OnLoad` 里通常有数据加载，直接 Add 到可见面板会露出加载中间态（闪烁） |
| 配置界面反射生成 | 20 多个配置页手写会失控；`[DisplayName]` + 类型特征足够表达 |
| 编辑器按类型特征匹配（有序工厂表） | 新增类型只加一行 matcher，判断顺序即优先级，比 switch 好扩展 |
| `UiControlHelper.DisposeAndClear` 强制释放 | WinForms 句柄不随 `Controls.Clear()` 销毁，只等 GC 会把句柄回收推到终结器线程，反复重建页面时句柄会堆积 |
| `IMessageBoxService` 抽象 | VM 不引 SunnyUI，可单测、可替换 |
| 活动监控挂 `IMessageFilter` | 控件级事件覆盖不全（空白处点击、快捷键），消息泵才是唯一入口 |
| `FormMain` 单例、`FormLogin` 每次新建 | 主窗体全进程只有一个；登录窗体要"干净重开"，单例会残留上次输入的账号与错误提示 |

## 8. 注意事项与 FAQ

🔴 **页面必须注册，否则运行到才炸**。`Menu.json` 配了 `DllName.Class` 但没 `RegisterPage<T>` → `GetFormInstance` 返回 null。启动自检会兜住，但新增页面时仍要先注册。

🔴 **`Menu.json` 双份真相**：模板（`Taoyl.UI.FormMain/config/`）与运行时（`<exe>/Config/`）。`CopyConfigOnce` 只在目标不存在时拷贝 —— 改模板不影响现场。

🔴 **切页只摘当前页（`Controls.Remove(currentForm)`），不要 `Controls.Clear()`**。页面是单例，清空整个面板会把常驻控件一起摘掉；且被清掉的页面实例仍在容器里，下次拿到的是一个已无父容器的窗体。

🔴 **`UiControlHelper.DisposeAndClear` 不能换成 `Controls.Clear()`**，见 §7 决策表。

🔴 **窗体走属性注入（`PropertiesAutowired`），VM 与服务走构造注入**。Autofac 默认选参数最多的构造函数；非窗体类型挂 `PropertiesAutowired` 是冗余残留，一律不加。

⚠️ **`MenuItem.Level` 是旧权限模型遗留**。新代码用权限码（`Page:<Name>` + `Btn:<Action>`），`Level` 只作兼容，不要再往它上面加语义。

⚠️ **配置渲染的字典区段是"自持"的**：字典的增删改直接在 `ConfigDictSection` 内部作用于字典本体，`ConfigFormRenderer` 不参与行维护。想在字典行上加逻辑，改 `ConfigDictSection`，不要试图从渲染器拦截。

⚠️ **`ConfigFormRenderer.Render` 会先 `DisposeAndClear(target)`**，容器里原有控件会被释放。重复调用是安全的，但不要在同一个容器上并行渲染。

⚠️ **`ShowConfirm` 默认 `showMask = true`**：弹出遮罩后底层窗口不可操作。在长流程里连续弹框要注意遮罩叠加。

**Q：新加一个页面要动几处？**
三处：① `Taoyl.UI.Pages` 写 `FormXxx`（含 VM）；② `FormModule` 加 `RegisterPage<FormXxx>`；③ `Menu.json`（模板 + 运行时）加节点 + `PermissionSyncService` 同步权限码。

**Q：为什么主窗体的依赖是属性注入？**
`FormMain` 由 `Application.Run` 直接取实例，不方便从构造传全部依赖；且窗体是设计器产物，构造函数签名被设计器约束。非窗体类没有这些约束，一律构造注入。

**Q：状态栏那个日志指示灯怎么接的？**
`ILogDisplay` 在 `InitConfigs` 里被 `Resolve`，构造即订阅总线事件；主窗体把它挂到状态栏控件上。顺序错了会漏掉启动期日志（不报错，只是看不到）。

## 9. 扩展指南

**A. 新增一个业务页面**

1. `Taoyl.UI.Pages` 建 `FormXxx` + `XxxViewModel`，VM 构造注入所需服务
2. `FormModule` 加 `RegisterPage<Taoyl.UI.Pages.FormXxx>(builder)`
3. `Menu.json` 两份都加节点：`Name` / `DisplayName` / `Class` / `DllName` / `Icon` / `SortOrder` / `Features`
4. 重启 → `PermissionSyncService` 自动把 `Page:Xxx` 同步进权限表 → 角色管理页勾选授权
5. 若要 `FormMain` 状态栏联动，改 `MainViewModel`

**B. 新增一个配置设置页**

1. 配置模型已存在于 `Taoyl.Application.Configuration`（继承 `ConfigBase`）
2. 页面里 `ConfigFlatModel.Flatten(config, 0)` → `ConfigFormRenderer.Render(panel, rows)`
3. 保存走"`Clone<T>` 草稿 → 用户改 → `CopyTo` 回灌 → `Save<T>`"（见 Configuration README §5.2）
4. 属性要出现在界面上，加 `[DisplayName("中文")]`

**C. 换 UI 控件库（如从 SunnyUI 换 DevExpress）**

影响面：
- `Services/SunnyUIMessageBoxService.cs`（实现 `IMessageBoxService`，换实现即可）
- 所有 `Views/*.cs` 的控件类型与 Designer
- `Helpers/ConfigFormRenderer.cs` 的编辑器工厂（`MakeTextBox` / `MakeCombo` / `MakeSwitch`）
- `ConfigDictSection` / `UiDgvHelper` / `ConfigFormStyle`

`MenuManager` / `NavigationService` / `ConfigFlatModel` 是纯逻辑，不受影响。

## 10. 相关文档

- `Taoyl.UI.Pages/` —— 业务页面（本库的页面来源；该模块暂无独立 README）
- `Taoyl.Application.Configuration/README.md` —— 配置模型与 `ConfigBase`（渲染数据源）
- `Taoyl.Application.AuthService/README.md` —— 登录、权限码与 `Page:` / `Btn:` 约定
- `Taoyl.IFALaunch/README.md` —— 容器装配、`RegisterPage<T>` 与启动序列
- `Taoyl.IFALaunch/Modules/FormModule.cs` —— 唯一的注册点
