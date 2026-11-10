# Taoyl.Application.AuthService — 认证与 RBAC 权限域

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

用户认证（账号 / 刷卡登录、会话、登出）+ RBAC 权限域（用户 → 角色 → 权限码）。

## 1. 定位与边界

- **权限层唯一真相源在 `PermissionService`**：有效权限 = 等级继承（实时计算）∪ 手动授予（落库）。
- **AuthService 无状态**：只管认证动作与事实广播；会话状态在 `UserService`（以 `IUserSession` / `IUserManager` 双契约暴露）。
- **不是**：审计组件（登录事实经 `AuthFact` 事件广播，由消费方投递审计）；菜单渲染组件（UI 按权限码判定，数据源 `sys_permission`）。
- 密码现状：**明文**存储 / 迁移（含导出包）——审计侧敏感字段脱敏只管审计表，本模块文件与库表均为明文，交接时知悉（设计决策：不处理）。

## 2. 依赖说明

仅依赖 **DataBase + Share**（+ Newtonsoft.Json / SqlSugar）。**不依赖任何业务模块与 UI 层**：

| 依赖切面 | 做法 | 为什么 |
|---|---|---|
| Menu.json 解析 | 本模块自有 DTO（`MenuConfigDto` 3 类） | 不引用 UI 层模型，依赖方向保持 `消费方 → AuthService` |
| 审计留痕 | `AuthFactRecorded` 事件广播 `AuthFact`（Share.Auth 零依赖契约） | 本模块不认识 IAuditService；登录域只广播事实，消费方在 UI 层接线 |
| 存储 | SqlSugar 单后端仓储（组合根直接注册 Db 实现） | Json 后端已删除（2026-10-15）；现场必有数据库，且 Db 后端 `MapDbType` 已支持 SQLite ⇒ Json 无独立价值 |
| 启动提示 | **`bool SyncOnStartup(out string message)` 而非日志依赖** | 无管理员的兜底复活必须"有声"；本模块只**产生**提示文本，**呈现**（落日志 / 状态栏 / 弹窗）由组合根决定——不引日志类库。签名形状对齐本模块 `bool ... out string errorMsg` 惯例，且使同步失败可见（原先 catch 后只 Debug.WriteLine = 静默） |

## 3. 功能与 API

### 能力域（7 行）

| 能力 | 契约 | 一句话 |
|---|---|---|
| 认证 | `IAuthService` | Login / LoginWithCard / Logout + 事实广播；无状态 |
| 会话 | `IUserSession` | 当前用户 / 等级 / 权限码集合 + UserChanged 刷新事件 |
| 门禁 | `IAuthService.HasPermission` | 命令层首行判定，权限码三段式 |
| 用户管理 | `IUserManager` | CRUD + 角色分配，`out errorMsg` 模式 |
| 角色管理 | `IRoleService` | 角色 DTO 查询 / 保存（内置角色 RoleName·Level 只读）/ 删除（单事务解除关联），`out errorMsg` 模式 |
| RBAC 迁移 | `IRbacMigrationService` | 跨机导出 / 导入（备份 + Menu 指纹强校验 + 失败自动恢复） |
| 手动权限 | `IPermissionService` | 有效权限实时计算 + Grant / Revoke / 批量同步 |
| 刷卡源 | `ICardLoginProvider` | 键盘钩子实现（KeyboardHookService），刷卡器/扫码枪通用 |

### `IAuthService`（认证）

| 成员 | 签名 | 语义 |
|---|---|---|
| `Login` | `(bool success, string message) Login(string username, string password)` | 密码登录；内部完成 SetCurrentUser + 权限码装载 + 事实广播 |
| `LoginWithCard` | `(bool success, string message, UserDto user) LoginWithCard(string cardCode)` | 卡号登录；一般由键盘钩子自动触发 |
| `Logout` | `void Logout()` | 手动与超时登出共用同一入口 |
| `HasPermission` | `bool HasPermission(string permissionCode)` | 当前用户门禁；未登录 = 访客权限（userId≤0 实时计算） |
| `AuthFactRecorded` | `event Action<AuthFact>` | 登录成功 / 失败 / 注销事实；消费方**必须**用事件快照，绝不回读 IUserSession（登出事件后会话随即清空） |
| `CardLoginAttempted` | `event Action<string>` | 刷卡动作提示（UI 可订阅显示"识别到卡号…"） |

### `IUserSession`（会话）

| 成员 | 签名 | 语义 |
|---|---|---|
| `CurrentUser` | `UserDto CurrentUser { get; }` | 未登录返回**访客**（CreateGuest 哨兵），永不为 null |
| `IsLoggedIn` | `bool { get; }` | = `MaxRoleLevel > 0`（访客恒 false） |
| `CurrentLevel` | `int { get; }` | 最高角色等级（百进制：100 操作员 / 300 技师 / 500 工程师 / 700 管理员） |
| `CurrentPermissionCodes` | `HashSet<string> { get; }` | 登录时装载的有效权限码快照 |
| `UserChanged` | `event Action<UserDto>` | 登录 / 登出 / 自身信息修改（Reload）时触发，UI 订阅刷新身份显示 |
| `ReloadCurrentUser` | `void ReloadCurrentUser()` | **统一刷新入口**：用户改自己信息后调用（权限码重算） |
| `SetCurrentUser` / `ClearCurrentUser` | — | 由 AuthService 内部调用，**UI 不直接调** |

### `IUserManager`（用户管理，`out errorMsg` 模式）

| 方法 | 语义与坑位 |
|---|---|
| `GetAllUsers` / `GetByUsername` / `GetByCardCode` | 查询（出 `UserDto`，含 Password）；`GetByCardCode` 大小写不敏感（ToLower） |
| `AddUser(UserDto user, out string errorMsg)` | 新增；Password 从 DTO 读取，必须非空 |
| `UpdateUser(UserDto user, out string errorMsg)` | 🔴 DTO.Password 为空 = **保留原密码**；非空 = 更新密码；**不能编辑等级高于自己的用户** |
| `DeleteUser(int userId, out string errorMsg)` | 🔴 禁删当前登录用户；**不能删等级高于自己的用户**；**不能移除系统中的最后一个管理员账户** |
| `AssignRolesToUser(int userId, List<int> roleIds, out string errorMsg)` | 🔴 **禁分配等级高于当前用户自身的角色**（防越权）；**不能改等级高于自己的用户的角色**；**不能移除最后一个管理员账户** |
| `GetUserRoles(int userId)` → `List<RoleDto>` / `GetUsersByRoleId(int roleId)` → `List<UserDto>` | 关联查询 |
| `TouchLastLogin(int userId, out string errorMsg)` | 登录链专用：只更新 LastLoginTime，不走用户编辑校验 |

### `IRbacMigrationService`（RBAC 整机迁移）

| 方法 | 语义 |
|---|---|
| `ExportUsers(string filePath)` | 导出角色（含手动权限 Code）+ 用户（含角色名）到 JSON 包；以 RoleName/Code 为锚，不依赖自增 ID |
| `ImportUsers(string filePath, out string errorMsg)` | 导入五步：备份（失败即中止）→ Menu 指纹强校验 → 清库重建 → 名称锚点重建 → 强制注销；异常自动从备份恢复 |

> 迁移逻辑从 `UserService` 搬出（零语义变更）；`IUserManager` 不再承载导入导出。

### `IPermissionService`（权限真相源）

| 方法 | 语义 |
|---|---|
| `GetUserPermissionCodes(int userId)` / `GetRoleEffectivePermissionCodes(int roleId)` | 实时计算：等级继承 ∪ 手动授予；管理员 L≥700 短路返回全部激活码；**无角色（userId≤0 或已登录但无角色行）≡ 游客** |
| `HasPermission(int userId, string permissionCode)` | 按 userId 判定（`IAuthService.HasPermission` 的底层） |
| `GetAllPermissions()` → `List<PermissionDto>` | 全量权限（含失效）；UI 权限树自行过滤 `IsActive == 1`，审计权限码对照用全量 |
| `GetRolePermissionIds(int roleId)` | 角色手动授予部分（权限树勾选态数据源） |
| `GrantPermission` / `RevokePermission` | 单条手动授予 / 撤销 |
| `SyncManualPermissions(int roleId, List<int> desiredPermissionIds)` | 权限树保存：**差集增删**（批量覆盖） |

### 词汇表

- **权限码三段式**：页面 `Page:Name`、按钮 `Btn:Page_Action`，与 Menu.json 同源（启动时增量同步进 `sys_permission`）。
- **`RoleType` 百进制枚举**：Guest=0 / Operator=100 / Technician=300 / Engineer=500 / Administrator=700；`[Description]` 即内置角色名。等级即角色、角色可多个，生效等级 = `MaxRoleLevel`。
- **`AuthFact`**（Share.Auth）：`Kind`（LoginSucceeded / LoginFailed / Logout）+ `Username` + `RoleNames`（失败=空集合）+ `MaxRoleLevel`（失败=0）；刻意不带时间戳（同步直写，接手时刻即事实时刻）。

## 4. 快速开始

```csharp
// 0) AuthModule：5 仓储直接注册 Db 实现，延迟到首次 Resolve 时构造（Db 为 Lazy，构造期不建库）
builder.Register<IUserRepository>(c =>
    new DbUserRepository(c.Resolve<SqlSugarHelper>())).SingleInstance();
// 其余 4 仓储同形；PermissionSyncService 注册于此

//    FormModule：域服务注册
builder.RegisterType<UserService>().As<IUserSession>().As<IUserManager>().SingleInstance();
builder.RegisterType<RbacMigrationService>().As<IRbacMigrationService>().SingleInstance();
builder.RegisterType<KeyboardHookService>().As<ICardLoginProvider>().SingleInstance();
builder.RegisterType<AuthService>().As<IAuthService>().SingleInstance();  // 4 参构造全必填，DI 自动满足
builder.RegisterType<RoleService>().As<IRoleService>().SingleInstance();
builder.RegisterType<PermissionService>().As<IPermissionService>().SingleInstance();

// 1) 启动链（Bootstrapper 已做，调用方无需操心）：
//    CodeFirst 7 表（RBAC 5 + 审计 2）→ SyncOnStartup（Menu.json 增量同步 / 内置角色 / 内置 admin）

// 2) 最小调用：登录 + 门禁
var (ok, msg) = authService.Login("admin", "111");       // 首次运行种子账号 admin / 111 / 卡 11111111
if (ok && authService.HasPermission("Btn:Process_Start"))
{
    // 有权限才执行业务
}
```

> **存储后端**：SqlSugar 单后端（Json 后端已于 2026-10-15 删除）。`Config/Auth/*.json` 为历史 Json 后端数据文件，保留作回滚资产、**已不再被读取**。

## 5. 使用示例（按场景，可复制）

**场景选型**：

| 你要做 | 用什么 | 场景 |
|---|---|---|
| 登录 / 登出 / 刷卡 | `IAuthService` | 场景 1 |
| 命令门禁 | `HasPermission` | 场景 2 |
| 显示身份 / 订阅变化 | `IUserSession` | 场景 3 |
| 用户增删改 / 分配角色 | `IUserManager` | 场景 4 |
| 权限树保存 | `SyncManualPermissions` | 场景 5 |
| 跨机迁移 RBAC | `IRbacMigrationService` | 场景 6 |
| 登录事实 → 审计 | 订阅 `AuthFactRecorded` | 场景 7 |

### 场景 1：登录 / 登出 / 刷卡

```csharp
// 密码登录：UI 只负责展示成败；会话装载、权限码计算、事实广播全部由 AuthService 内部完成
var (success, message) = authService.Login(username, password);

// 刷卡登录：一般**不手动调**——KeyboardHookService 攒够卡号长度自动触发 OnCardRead；
// 仅自定义交互（如登录窗体手动按钮）才直调
var cardResult = authService.LoginWithCard(cardCode);   // (bool, string, UserDto)

// 登出：手动与超时共用同一入口（不区分动作类型）
authService.Logout();
```

### 场景 2：命令门禁（每个命令体首行）

```csharp
[RelayCommand]
private void Start()
{
    if (!authService.HasPermission("Btn:Process_Start")) return;  // 被拒不执行、也不留痕（审计规范）
    // …业务
}
// 权限码与 Menu.json 同源：页面 Page:Name / 按钮 Btn:Page_Action；被拒是"原因"不是"结果"，不进审计
```

### 场景 3：会话消费与身份显示刷新

```csharp
UserDto who    = userSession.CurrentUser;   // 未登录 = 访客哨兵，永不为 null
bool loggedIn   = userSession.IsLoggedIn;    // = MaxRoleLevel > 0
int level       = userSession.CurrentLevel;  // 300 技师 / 500 工程师 / 700 管理员 …

// 登录 / 登出 / 自身信息修改时刷新 UI 身份显示
userSession.UserChanged += user =>
{
    lblUserName.Text = user.Username;
    lblRoles.Text    = user.RoleNamesDisplay;
};

// 用户改了自己的信息（如密码/卡号）后：统一刷新入口（重取仓储 + 权限码重算 + 触发 UserChanged）
userSession.ReloadCurrentUser();
```

### 场景 4：用户管理（out errorMsg 模式，三个坑内嵌）

```csharp
var user = new UserDto { Username = "zhang", CardCode = "12345678", Password = "初始密码" };
if (!userManager.AddUser(user, out string err))
    messageBox.ShowError(err);                    // 失败原因在 errorMsg（重名/卡号冲突等）

// 修改：DTO.Password 留空 = 保留原密码（只改其他字段时不带密码）
user.Password = "";                               // 留空表示不改密码
if (!userManager.UpdateUser(user, out err)) { /* … */ }

// 删除：禁删当前登录用户（返回 false）
if (!userManager.DeleteUser(userId, out err)) { /* … */ }

// 分配角色：🔴 不能分配等级高于自己最高角色的角色（防越权）
if (!userManager.AssignRolesToUser(userId, new List<int> { roleId }, out err)) { /* … */ }
```

### 场景 5：权限树保存（手动授权批量同步）

```csharp
// 权限树勾选保存：一次批量覆盖 = 差集增删（比逐条 Grant/Revoke 少一半 IO）
permissionService.SyncManualPermissions(roleId, checkedPermissionIds);

// ⚠️ 取消勾选**等级继承已达标**的权限码不会生效——继承侧实时仍算出；
// 权限树 UI 对继承项只读展示，只允许勾/取消"低于角色等级的补充项"
```

### 场景 6：RBAC 跨机迁移（导出 / 导入）

```csharp
// 导出：角色（含手动权限 Code）+ 用户（含角色名）→ JSON 包；以 RoleName/Code 为锚，不依赖自增 ID
if (!rbacMigrationService.ExportUsers(@"D:\rbac_backup.json")) { /* … */ }

// 导入五步全自动（调用方只管成败提示）：
// ① 导入前自动备份到 Config/Auth/Backup/（备份失败即中止）→ ② Menu 指纹强校验（不一致直接拒绝）
// → ③ 清空本地 RBAC 全量重建 → ④ 按名称锚点重建（未匹配进 errors 不阻断）→ ⑤ 强制注销当前会话
if (!rbacMigrationService.ImportUsers(@"D:\rbac_backup.json", out string err))
    messageBox.ShowError(err);   // 导入过程异常 → 自动从备份恢复；恢复也失败则提示手动使用备份文件
```

### 场景 7：登录事实 → 审计（事件订阅铁律）

```csharp
// 消费方（FormMain）接线：登录域只广播，不关心有无订阅者
authService.AuthFactRecorded += fact => { /* 投递审计——完整代码见 Taoyl.Application.Audit/README.md 场景 5 */ };

// 🔴 三时序铁律（AuthService 内部已保证，消费方只需记住"只认快照"）：
//   登录成功在 SetCurrentUser 之后 / 登出在 ClearCurrentUser 之前 / 登录失败携带"尝试者"快照（角色空、等级 0）
// 回读 IUserSession = 读到空或下一位用户 —— 假账
```

## 6. 类库结构（逐代码文件说明表，一类型一文件共 35 文件；Models 分层 Entities/Dtos，namespace 跟随目录）

| 文件 | 说明 |
| ---- | ---- |
| **Interfaces/（13 接口，一接口一文件）** | |
| `IUserRepository.cs` | 用户数据仓储：CRUD + `GetByCardCode` + `CardCodeExists(cardCode, excludeUserId)` 卡号唯一性校验 + 窄更新 `UpdateEditableFields(entity, updatePassword)` / `UpdateLastLoginTime(userId, time)` |
| `IRoleRepository.cs` | 角色仓储：CRUD + `DeleteWithRelations(roleId)`（单事务删 sys_role_permission + sys_user_role + sys_role）+ `IsAssignedToUser(roleId, userId)` |
| `IPermissionRepository.cs` | 权限仓储：CRUD + `GetByType` + `GetActiveMenus` / `GetActiveFeatures`；Delete = 逻辑删 |
| `IUserRoleRepository.cs` | 用户-角色关联仓储 |
| `IRolePermissionRepository.cs` | 角色-权限（手动授予部分）关联仓储 |
| `IAuthService.cs` | 认证服务：Login / LoginWithCard / Logout / HasPermission + `CardLoginAttempted`、`AuthFactRecorded` 两事件——消费方**必须**使用事件携带的快照，绝不可回读 IUserSession |
| `IUserSession.cs` | 会话：CurrentUser / IsLoggedIn / CurrentLevel / CurrentPermissionCodes + UserChanged + Set/Clear/Reload |
| `IUserManager.cs` | 用户管理：CRUD + 角色分配 + `TouchLastLogin`（签名只出 UserDto）；**不含导入导出**（见 IRbacMigrationService） |
| `IRbacMigrationService.cs` | RBAC 整机迁移：`ExportUsers` / `ImportUsers`（跨机迁移桥，含 Menu 指纹校验与失败恢复） |
| `IRoleService.cs` | 角色服务：`GetAllRoles` / `SaveRole`（等级越权校验、内置角色 RoleName·Level 只读）/ `DeleteRole`（内置禁删、关联当前用户阻断、单事务解除关联） |
| `IPermissionSyncService.cs` | 启动同步 `SyncOnStartup()` |
| `IPermissionService.cs` | 权限码实时计算 + 手动授予 Grant / Revoke / SyncManualPermissions |
| `ICardLoginProvider.cs` | 刷卡源契约：CardRead / IsEnabled / Start / Stop |
| **Models/（根 3 + Entities/ 5 + Dtos/ 1）** | |
| `Entities/UserInfo.cs` | sys_users 落库实体；八个落库字段（含 Password）；不再承载 UI 列头 / 角色聚合 |
| `Entities/Role.cs` | sys_role；IsBuiltIn 内置标记 |
| `Entities/Permission.cs` | sys_permission；Type 1=菜单页面 `Page:*`、2=功能按钮；Level = 所需等级；IsActive 启用位——删除即逻辑删 |
| `Entities/UserRole.cs` | sys_user_role |
| `Entities/RolePermission.cs` | sys_role_permission，**仅存手动授予部分**——自动继承实时计算不落库，Source 字段已废弃 |
| `Dtos/AuthDtos.cs` | 五个 DTO：UserDto / RoleDto / PermissionDto / UserRoleDto / RolePermissionDto；UserDto 含 Password，Roles / MaxRoleLevel / RoleNamesDisplay 为 [JsonIgnore]，CreateGuest 访客哨兵在此。UserRoleDto / RolePermissionDto 为**契约完备性 DTO**（当前无 UI 消费点，业务内部仍用实体关系行） |
| `MenuConfigDto.cs` | Menu.json 解析 DTO 3 类（`MenuConfigDto` / `MenuItemDto` / `FeatureItemDto`，Features 的 Level 可空 = 继承父页面）——避免本模块依赖 UI 层 |
| `RoleType.cs` | **百进制等级**枚举：Guest=0 / Operator=100 / Technician=300 / Engineer=500 / Administrator=700，[Description] 即内置角色名 |
| `RbacExportPackage.cs` | RBAC 跨机迁移包 3 类：`RbacExportPackage` / `RoleExportItem`（角色 + 手动权限 Code 列表）/ `UserExportItem`（用户 + 角色名列表）——**以名称 / Code 为锚点，完全不依赖自增 ID** |
| **Mapping/（1 文件）** | |
| `AuthMappingRegister.cs` | 本模块唯一 Mapster 配置点：UserInfo↔UserDto / Role↔RoleDto / Permission↔PermissionDto / UserRole↔UserRoleDto / RolePermission↔RolePermissionDto 五组双向 |
| **Repositories/（5 文件，SqlSugar 仓储）** | |
| `DbUserRepository.cs` | SqlSugar 用户仓储实现；构造注入 `SqlSugarHelper`，方法内取 `.Db`（Lazy，构造期不触发 DB 创建）；窄更新走 `SetColumns` 只写指定列 |
| `DbRoleRepository.cs` / `DbPermissionRepository.cs` | SqlSugar 角色 / 权限仓储实现；查询一律 `ToLower()` 兼容大小写敏感库；**Permission.Delete = 逻辑删除（IsActive=0）**，其余物理删 |
| `DbUserRoleRepository.cs` / `DbRolePermissionRepository.cs` | SqlSugar 两关联仓储实现；`Add` 走 `ExecuteReturnIdentity` 回写自增主键 |
| **Services/（7 文件，域服务）** | |
| `UserService.cs` | `IUserSession + IUserManager` 实现——**实体↔DTO 唯一转换边界**（私有 `ToDto` 单点装配 Roles，所有用户读取出口统一经过）+ 会话状态持有（sessionLock + Clone 隔离）+ 用户 CRUD（新增校验 Password 非空并回写 UserId；UpdateUser 走仓储窄更新 `UpdateEditableFields`，空密码保留原值；`TouchLastLogin` 只写登录时间）+ **等级保护**（DeleteUser / UpdateUser / AssignRolesToUser 均拒绝对等级高于自己的用户操作，规则本体在服务层，UI 的 `CanEdit/CanDelete` 只作即时反馈）+ **防锁死**（AssignRolesToUser / DeleteUser 拒绝移除系统中最后一个管理员账户，判据 `HasAnyAdminLevelUser`） |
| `RbacMigrationService.cs` | `IRbacMigrationService` 实现——RBAC 跨机导出 / 导入（备份 → Menu 指纹强校验 → 清库重建 → 名称锚点重建 → 强制注销 → 失败恢复）；依赖 4 仓储 + IPermissionService + IUserSession。从 UserService 搬出 |
| `RoleService.cs` | `IRoleService` 实现——角色 DTO 查询 / 保存（等级越权校验、回填 Id；**内置角色 RoleName·Level 以库中原值覆盖**、新增一律 `IsBuiltIn=0`）/ 删除（内置禁删、关联当前用户阻断、`DeleteWithRelations` 单事务解除关联）。只依赖 `IRoleRepository + IUserSession` |
| `PermissionSyncService.cs` | 启动同步四步（见 §七启动链路）；④ 判据为「系统无任何管理员级用户」而非"admin 不存在"；`SyncOnStartup(out string message)` 返回 true 时 message 为提示信息（无则 null）、false 时为异常说明——由组合根落日志 |
| `PermissionService.cs` | **权限层唯一真相源**：有效权限 = 等级继承（实时计算 `role.Level >= perm.Level`）∪ 手动授予（RolePermission 表）；**管理员短路**（Level≥700 直接返回全部激活权限码）；`userId<=0` 按访客角色实时计算；SyncManualPermissions 差集增删 |
| `AuthService.cs` | 认证服务（**无状态**：登录 / 登出 / 刷卡 / 事件，权限查询全委托 IPermissionService）。**认证事实广播 `AuthFactRecorded`**：本类是全仓唯一做服务层留痕的域（刷卡登录由键盘钩子直达、UI 结构上不可观测；身份采集时机只有本类知道），经事件广播 `AuthFact` 快照，自身不持有任何审计类型。4 参构造**全必填**（`cardLoginProvider` 无默认值，构造即挂 `CardRead` 事件并 `Start()`） |
| `KeyboardHookService.cs` | `ICardLoginProvider` 实现：**WH_KEYBOARD_LL 低级键盘钩子**接刷卡器 / 扫码枪（键盘模拟输入设备）；累积字符到 `CardInputLength` 触发 `CardRead`（**Task.Run 异步触发，防阻塞钩子线程**；超时 `KeyInputTimeout` 自动重置累积串）；虚拟键码→字符自解析；三参数**实时读 `ICardLoginSettings`**（Share.Config 配置中心），改配置即时生效；Dispose 摘钩子 |

## 7. 内部逻辑

### RBAC 权限模型（唯一真相源：PermissionService）

```
用户 ──(sys_user_role)──> 角色 ──(sys_role_permission, 仅手动授予)──> 权限码
                              │
                              └─ 等级继承：role.Level >= perm.Level → 自动拥有（实时计算，不落库）

有效权限(role) = { p.IsActive==1 | role.Level >= p.Level }   ← 等级继承
              ∪ { p | (role,p) ∈ sys_role_permission }       ← 手动授予
有效权限(user) = ∪ 有效权限(其全部角色)                        ← 无角色（userId≤0 或未分配）→ 访客角色
管理员(L≥700)  = 全部激活权限码（短路，免逐条算）
```

- **等级即角色、角色可多个**：`RoleType` 百进制只约束内置角色；`Role` 表可自建角色。多角色取 `MaxRoleLevel` 为生效等级。
- **手动授予是"补充"而非"覆盖"**：权限树保存走 `SyncManualPermissions` 差集增删；取消勾选等级已达标的权限码**不会生效**（等级继承侧仍算出）——权限树 UI 侧对继承项只读展示。

### 启动链路（Bootstrapper 时序）

```
Bootstrapper.InitDatabase（容器构建后）
  → CodeFirst.InitTables(sys_users / sys_role / sys_permission / sys_user_role / sys_role_permission
                        + sys_audit_operations / sys_audit_field_changes)   ← RBAC 5 表 + 审计 2 表
  → PermissionSyncService.SyncOnStartup()，四步：
      ① ParseAllPermissions：读 Config/Menu.json → 权限清单（页面 Page:* + Features 按钮码，Level 继承或显式覆盖）
      ② SyncPermissionsToDatabase：增量同步——JSON 有 DB 无 → Add；都有 → 更新字段并复活 IsActive=1；
                                   DB 有 JSON 无 → 逻辑删（IsActive=0，权限码是 RBAC 资产不物理删）
      ③ EnsureBuiltInRoles：RoleType 枚举驱动内置角色（存在且 Level 变了才 Update）
      ④ EnsureBuiltInAdminUser：系统无任何管理员级用户时才兜底复活（admin 行在 ⇒ 只补角色、不动密码；
                                  行已被删 ⇒ 以默认密码重建）
  SyncOnStartup 出参 message = 提示文本（成功）或异常说明（失败），由组合根落 Warn 日志
  🔴 日志 UI 由组合根在 `InitConfigs` 提前激活（裸 `Resolve<ILogDisplay>()` 触发单例构造，
    构造即订阅）——否则启动期日志不入环、UI 看不到（详见 IFALaunch/README.md §3）
```

- 权限同步**只增改、逻辑删**：与 Menu.json 的双份真相约定一致（bin 运行时活文件 + `UI.FormMain/config/` 模板，必须同时改）。

### 认证事实广播（AuthFact 时序约束）

| 事实 | 时机铁律 | 快照规则 |
| ---- | ---- | ---- |
| LoginSucceeded | **必须在 SetCurrentUser 之后** | user 已带完整角色快照，与生效会话一致 |
| LoginFailed | 会话未建立时广播 | **显式构造"本次尝试者"快照**：角色空、等级 0——借会话身份会把上一位用户的角色按到本次尝试头上，是假账 |
| Logout | **必须在 ClearCurrentUser 之前** | 身份快照须在会话清空前采集；未登录状态的注销不产生事实 |

- 无人订阅 = 空条件调用，安全无副作用；消费方（FormMain）**必须**用事件携带的快照投递审计，绝不回读 IUserSession。
- 手动登出与超时登出一视同仁（共用 Logout，不区分动作类型）。

### 会话模型（UserService）

- **Clone 隔离**：`SetCurrentUser` 存副本、`ReloadCurrentUser` 从仓储重取——外部持有引用就地改写不影响会话。
- `UserChanged` 事件：登录 / 登出 / 自身信息修改（Reload）时触发，UI 订阅刷新身份显示。
- `IsLoggedIn` = `MaxRoleLevel > 0`（访客恒 false）。

### 刷卡登录链路

```
刷卡器/扫码枪（键盘模拟） → KeyboardHookService(WH_KEYBOARD_LL 累积到 CardInputLength)
  → CardRead(卡号) ──Task.Run 异步──> AuthService.OnCardRead
      → CardLoginAttempted（UI 可订阅显示提示）
      → IsEnabled 实时校验（配置关了就不登录）
      → LoginWithCard → GetByCardCode(ToLower) → 挂角色/算权限 → SetCurrentUser
      → RecordCardLoginLog：刷卡日志/yyyyMMdd.txt 纯文本追加（自吞异常，与审计两回事）
```

### 关键设计决策（为什么）

| 决策 | 为什么 |
|---|---|
| 权限实时计算、手动授予才落库 | 角色改等级立即生效，无"重算并回写"同步窗口；sys_role_permission 只存补充部分 |
| 管理员 L≥700 短路 | 免逐条计算；管理员语义 = 全部激活权限码，与逐条算结果恒等 |
| AuthService 无状态、会话独立 UserService | 登录动作与会话持有分离；会话 Clone 隔离防外部就地改写 |
| Permission 删除 = 逻辑删（IsActive=0） | 权限码是 RBAC 资产：历史审计记录 / 角色关联引用它，物理删 = 历史悬空 |
| Menu.json 走自有 `MenuConfigDto` 解析 | 不依赖 UI 层模型——依赖方向保持 `消费方 → AuthService` |
| Json 后端删除（2026-10-15） | 现场必有数据库，且 Db 后端 `MapDbType` 已支持 SQLite ⇒ Json 无独立价值；省掉"每改一次仓储契约要同步两份实现"的税 |
| AuthFact 三时序（§七表） | 快照必须与"事实发生瞬间"的身份一致——早一步未就绪、晚一步已清空 |
| 迁移以名称/Code 为锚 + Menu 指纹强校验 | 跨机自增 ID 不可靠；指纹（MD5 前 8 位，只算 Name/Code/Level）保证两机权限词汇一致，导入才不会静默错配 |
| 一类型一文件、namespace 不变的物理拆分 | 全仓 using 零改动；与 Audit 目录层级对齐（Interfaces/Models/Repositories/Services） |
| 内置角色 RoleName / Level 只读（2026-10-15） | 改 Name 会造重复内置角色 + 破坏导入导出锚点（以 RoleName 为锚）且无兜底；改 Level 会与启动纠正冲突。Remark 保留可改 |
| 无角色 ≡ 游客（2026-10-15） | 消除倒挂：原 `userId≤0` 给游客权限、`userId>0` 无角色给**空集**；现两者同规则回落访客权限 |
| 角色删除单事务解除关联（2026-10-15） | 原逐用户 `DeleteUser`（把用户也删了）+ 清手动权限 + 删角色，中途失败留半成品；现三表一个事务（`DeleteWithRelations`），只**解除**关联、用户保留 |
| 等级保护下沉到服务层（2026-10-15） | 原"不能操作等级高于自己的用户"只活在 UI 私有方法里（纪律保证）；`DeleteUser`/`UpdateUser` 是**公开服务契约**，任何服务间调用或新增写路径都能绕过 ⇒ 规则本体移入服务层，UI 只留即时反馈 |
| 防锁死四条规则（2026-10-15） | ①内置角色只读+ ②`AssignRolesToUser` / ③`DeleteUser` 拒移最后一个管理员 + ④`EnsureBuiltInAdminUser` 判据改"无任何管理员级用户"。判据统一 = `MaxRoleLevel ≥ 700` |
| 兜底复活必须"有声"（2026-10-15） | 静默复活会让操作者重启后无感地多出一个默认密码账户。做法：`bool SyncOnStartup(out string message)` 出参带出提示（成功）/ 异常（失败），组合根 `Bootstrapper` 落 `ILogService.Warn` → 启动日志 + LogDriver UI——**AuthService 不引日志类库**（只产生文本，不决定呈现） |

## 8. 注意事项与 FAQ

### 🔴 铁律总表（违反 = 数据 / 语义错误）

| # | 铁律 | 一句话理由 |
| - | ---- | ---- |
| 1 | 🔴 **消费 `AuthFactRecorded` 只认事件快照，绝不回读 IUserSession** | 登出事件发出后会话随即清空；回读 = 空 or 下一位用户 = 假账 |
| 2 | 🔴 **UI 不直接调 `SetCurrentUser` / `ClearCurrentUser`** | 会话装载与事实广播时序在 AuthService 内部（成功在 Set 后、登出在 Clear 前），绕过即破坏时序 |
| 3 | 🔴 **Menu.json 双份真相必须同时改**（bin 运行时活文件 + `UI.FormMain/config/` 模板） | 只改一份 → 现场运行时与出厂模板漂移，下次部署回退 |
| 4 | **导入前自动备份失败即中止**；导入后当前会话强制注销 | 防数据丢失；当前账号可能已被删或角色已变 |
| 5 | **Permission 删除走逻辑删**（IsActive=0），不物理删 | 权限码是 RBAC 资产，物理删 = 历史悬空 |
| 6 | **AssignRolesToUser 禁越权**：不能配高于自己的角色 | 权限提升必须由更高等级者执行 |
| 7 | 🔴 **不能操作等级高于自己的用户**（删 / 改 / 分配角色） | 规则本体在服务层（UI 按钮灰只是即时反馈，绕过 UI 即失效）；等级保护必须由服务契约保证 |
| 8 | 🔴 **不能移除系统中最后一个管理员账户** | `AssignRolesToUser` / `DeleteUser` 服务层判定（`MaxRoleLevel ≥ 700`）；`EnsureBuiltInAdminUser` 兜底但会 Warn——把自己锁死是唯一无法自救的错误 |
| 9 | **密码明文现状**（含导出包）——审计脱敏只管审计表 | 设计决策不处理；若将来上加密，导出包兼容是第一迁移问题 |

### ⚠️ 坑位与 FAQ

| 问 / 坑 | 答 |
|---|---|
| 取消勾选"等级已达标"的权限为什么没生效？ | 手动授予是补充不是覆盖，继承侧实时仍算出——权限树 UI 对继承项只读展示是配套约定 |
| 改了 Menu.json 为什么没生效？ | 同步只在启动时跑（SyncOnStartup）；改完重启，且双份真相同时改（铁律 3） |
| `Config/Auth/*.json` 还有用吗？ | 无。Json 后端已于 2026-10-15 删除，这些文件保留作回滚资产、**不再被读取**（跨机迁移用 RBAC 导出/导入，§五场景 6） |
| 为什么删不掉内置角色 / admin？ | 内置角色 `IsBuiltIn` 标记（RoleName / Level 也只读）+ 启动时 EnsureBuiltInRoles/EnsureBuiltInAdminUser 兜底复活 |
| 删角色会把关联用户一起删掉吗？ | 不会。删除只**解除**关联（用户保留），仅移除该角色；用户若失去全部角色 ⇒ 按游客规则（§七模型） |
| 没有分配角色的用户能登录吗？ | 能。登录成功但权限 = 游客可见范围，用户列表角色列显示"游客" |
| 内置角色不能改角色名 / 等级，那还能保存吗？ | 能，**保存按钮不禁用**——RoleName / Level 由输入框只读 + 服务层强制原值保证不可改；Remark 与**手动权限分配**照常保存 |
| UpdateUser 传了新密码没生效？ | 检查是否传空串——空 = 保留原密码（刻意语义，防"只想改个卡号把密码冲掉"） |
| 管理员账户删不掉 / 降不了级？ | 系统必须至少保留一个管理员级用户（`MaxRoleLevel ≥ 700`）——先建接班人 B 再降级自己 |
| 工程师为什么改不了管理员的资料？ | 等级保护：不能操作等级高于自己的用户（**服务层拦截**，UI 按钮灰只是即时反馈） |
| 重启后多出 admin / 日志里出现"已重建默认管理员"？ | 启动时检测到系统无任何管理员级用户 → 兜底复活并 Warn 提示；若是默认密码重建，请**立即改密** |
| 未登录时 HasPermission 返回什么？ | 访客规则实时计算（userId≤0 → 访客角色的权限）；IsLoggedIn=false 时门禁基本全拒 |
| 刷卡没反应？ | ①配置 IsEnabled（实时读，改配置即时生效）②卡号长度是否等于 CardInputLength ③钩子被其他低级钩子截胡的可能 |

## 9. 扩展指南

### A. 新增按钮权限码

1. `UI.FormMain/config/Menu.json` 对应页面 `Features` 加一项：`Name`（显示名）/ `Code`（`Btn:Page_Action`）/ `Level`（可空 = 继承父页面）。
2. **双份真相同时改**：bin 运行时活文件 + `UI.FormMain/config/` 模板（铁律 3）。
3. 重启 → SyncOnStartup 增量同步自动 Add 新码（消失的码逻辑删）；角色管理页按需授权。
4. 命令体首行挂 `HasPermission("Btn:...")` 门禁；命令名与码一一对应（可 grep 反查）。

### B. 新增页面（页码）

`Menu.json` 加页面节点（`Page:Name`）→ 同步自动建；审计侧 `AuditCategory` 新增成员对齐该页（见 Audit README §九 C——`System` 保持末位 10 勿动）。

### C. 新增 / 调整内置角色

`RoleType` 枚举加成员（百进制等级，值段不与现有冲突）→ 启动时 `EnsureBuiltInRoles` 枚举驱动自动建；改已有内置角色 Level → 启动时自动 Update（存在且 Level 变了才写库）。

### D. 新增按钮在主窗体（无菜单节点）

主窗体按钮不经菜单导航（如 `Btn:Process_Start`）：权限码照常进 Menu.json Features（或既有约定位置），审计 `AuditCategory` 归 `Main`（中文值"主界面"与 DisplayName 对齐的例外规则）。

### E. 存储后端相关

实体变更（加列）由 `Bootstrapper.InitDatabase` 的 CodeFirst 启动期处理。新增一个仓储 = 接口 + Db 实现 + `AuthModule.RegisterRepositories` 注册（照现有 5 仓储存抄结构）。

## 10. 相关文档

- `Taoyl.Application.Audit/README.md`——审计留痕：登录事实的消费方与投递规范（场景 5）。
- `Taoyl.Application.Share/Auth/AuthFact.cs`——认证事实契约本体（零依赖，双方共同认识）。
- `Taoyl.Application.Share/Auth/PermissionCodes.cs`——权限码分区清单（代码侧常量引用入口）。
- Menu.json 双份真相与页面码规范：`UI.FormMain/config/Menu.json`。
