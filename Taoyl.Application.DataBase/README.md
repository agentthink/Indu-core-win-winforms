# Taoyl.Application.DataBase —— 数据库连接基座 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**数据库连接的唯一切换点**：把 `DevConfig.json` 里的数据库配置翻译成一个 `SqlSugarScope` 实例，供全部仓储层注入使用。

**是什么**：数据库类型的业务枚举（`DatabaseType`，定义在 Share）；连接配置载体（`DbConnectionConfig`）；SqlSugar 实例的延迟创建与唯一出口（`SqlSugarHelper`）；SQLite 连接串的路径解析。

**不是什么**：不做仓储、不认识任何实体与表、不写业务查询、不管事务边界。它只回答一个问题——"给哪个库、用什么连接串、造出 Db"。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| Share | `DatabaseType` 枚举（`Share/Config`，业务层与配置层共用同一份） |
| SqlSugar 5.1.4.207 | `SqlSugarScope` / `ConnectionConfig` / `InitKeyType` / `SqlSugar.DbType` |
| System.Data.SQLite.Core 1.0.119 | SQLite 驱动。🔴 SqlSugar **只认这个**，不认 `Microsoft.Data.Sqlite`（见 §8） |
| MySql.Data 9.5.0 | MySQL 驱动，配置切回 MySQL 时可用 |
| Mapster 10.0.13 | 映射配置随本库传递供货给仓储层（本库自身不使用） |

被依赖：11 个仓储与服务（Alarm ×1、Audit ×2、AuthService ×5、Product ×2、AuditService）+ 组合根 `DataBaseModule` / `Bootstrapper`。

## 3. 功能与 API

### `DbConnectionConfig`

| 成员 | 类型 | 说明 |
|---|---|---|
| `DbType` | `DatabaseType` | 数据库类型 |
| `ConnectionString` | `string` | 连接字符串，原样来自 `DevConfig.DB.ConnectionString` |

### `SqlSugarHelper`

| 成员 | 签名 | 说明 |
|---|---|---|
| 构造 | `SqlSugarHelper(DbConnectionConfig dbConfig)` | 只保存配置，**不建立任何连接** |
| `Db` | `SqlSugarScope Db { get; }` | 首次访问才创建实例（`Lazy<T>(..., isThreadSafe: true)`） |

私有方法 `MapDbType` / `ResolveConnectionString` 见 §7。

### `DatabaseType`（定义在 `Share/Config/DatabaseType.cs`）

| 成员 | `[Description]` | 值 |
|---|---|---|
| `MySql` | MySQL | 0 |
| `Sqlite` | SQLite | 1 |

🔴 **只列已装驱动包的类型**。列了却没装包，用户在下拉里选中就是启动即炸（`MapDbType` 会走到 `NotSupportedException`）。当前只装了 MySQL 与 SQLite 两个驱动。

`[Description]` 供参数设置页下拉显示（`ConfigFormRenderer.MakeCombo` 走 `GetDescription`）；配置 JSON 里存的是**枚举名**（属性带 `StringEnumConverter`）。

## 4. 快速开始

组合根注册（`Taoyl.IFALaunch/Modules/DataBaseModule.cs`）：

```csharp
// 延迟到首次 Resolve 才读配置：保证 InitConfigs 预加载完成后拿到内存缓存值
builder.Register(c =>
{
    var dev = c.Resolve<IConfigService>().Get<DevConfig>();
    return new SqlSugarHelper(new DbConnectionConfig
    {
        DbType = dev.DB.DBType,
        ConnectionString = dev.DB.ConnectionString
    });
}).As<SqlSugarHelper>().SingleInstance();
```

启动期唯一触发点（`Bootstrapper.InitDatabase`）：

```csharp
var dbHelper = Container.Resolve<SqlSugarHelper>();
dbHelper.Db.CodeFirst.InitTables(typeof(UserInfo), /* ...共 9 张表... */);
```

🔴 `Resolve` 本身不建连接，`dbHelper.Db` 那一刻才创建。这是刻意的：`InitConfigs` 必须先跑完，否则读不到配置。

## 5. 使用示例

### 5.1 两种数据库的连接串

| 类型 | 连接串示例 |
|---|---|
| MySQL | `server=127.0.0.1;port=3306;database=tyl_ifa;uid=root;pwd=xxx;charset=utf8mb4;` |
| SQLite | `Data Source=Data\tyl_ifa.db;Version=3;Journal Mode=WAL;BusyTimeout=5000;Pooling=True;` |

### 5.2 SQLite 路径规则

| 写法 | 解析结果 |
|---|---|
| `Data Source=Data\tyl_ifa.db` | 相对路径 → `<exe目录>\Data\tyl_ifa.db`，目录不存在会自动创建 |
| `Data Source=D:\Db\tyl_ifa.db` | 绝对路径 → **原样使用**，代码不介入 |
| `Data Source=tyl_ifa.db` | 相对路径 → `<exe目录>\tyl_ifa.db` |

相对路径一律以**程序目录**（`AppDomain.BaseDirectory`）为基准，不是当前工作目录。

### 5.3 切换数据库

1. 参数设置页改「数据库类型」下拉 → 保存；或直接改 `config/DevConfig.json`
2. **必须同时改连接字符串** —— 类型和连接串不会自动对应
3. 重启程序（`DevConfig` 不做热生效）

```json
"DB": {
  "DBType": "Sqlite",
  "ConnectionString": "Data Source=Data\\tyl_ifa.db;Version=3;Journal Mode=WAL;BusyTimeout=5000;Pooling=True;"
}
```

### 5.4 仓储里怎么用

```csharp
public class AlarmRepository : IAlarmRepository
{
    private readonly SqlSugarHelper dbHelper;

    // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
    public AlarmRepository(SqlSugarHelper dbHelper)
    {
        this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
    }

    public int Insert(AlarmRecord record)
        => dbHelper.Db.Insertable(record).ExecuteReturnBigIdentity();
}
```

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| `SqlSugarHelper.cs` | `DbConnectionConfig`（2 属性）+ `SqlSugarHelper`（ctor / `Db` / `MapDbType` / `ResolveConnectionString`） |

`DatabaseType` 枚举在 `Taoyl.Application.Share/Config/DatabaseType.cs` —— 放 Share 是因为配置层（Configuration）也要用它，而配置层不能引 SqlSugar。

## 7. 内部逻辑

```
DevConfig.DB（DBType + ConnectionString）
   ↓ IConfigService.Get<DevConfig>()
DataBaseModule（延迟首 Resolve）
   ↓ new DbConnectionConfig
SqlSugarHelper.CreateDb()
   ├─ MapDbType             DatabaseType → SqlSugar.DbType
   └─ ResolveConnectionString  SQLite 相对路径 → exe 目录绝对路径 + 建目录
   ↓
ConnectionConfig { IsAutoCloseConnection = true, InitKeyType = Attribute }
   ↓
SqlSugarScope（单例，线程安全）
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| `DatabaseType` 枚举放 Share | 配置层不能引 SqlSugar——配置中心一旦认识 ORM，分层就废了 |
| `DbConnectionConfig.DbType` 用同一枚举 | 否则工厂层要 `ToString()` 再 `Parse`，多一道无意义转换且字符串拼错风险仍在 |
| SQLite 相对路径按 exe 目录解析 | `Environment.CurrentDirectory` 会被 `OpenFileDialog` 改到上次打开的文件夹；不解析则相对路径会在别处建空库 |
| 绝对路径原样返回 | 用户指定到哪就是哪，代码不介入 |
| `IsAutoCloseConnection = true` | 连接开合交给 SqlSugar + 驱动连接池，避免长连接泄漏 |
| `Db` 走 `Lazy<T>` | 构造期不触发 DB 创建，保证 `InitConfigs` 先于一切 Resolve 完成 |
| 驱动包选 `System.Data.SQLite.Core` | SqlSugar 5.x 的程序集里只引用了 `System.Data.SQLite`，`Microsoft.Data.Sqlite` 出现 0 次 |

## 8. 注意事项与 FAQ

🔴 **SQLite 并发写**：SQLite 是库级写锁。报警消费线程、审计写侧、产品写入同时进行会 `database is locked`。连接串必须带 `Journal Mode=WAL;BusyTimeout=5000;`（WAL 让读写不互斥，BusyTimeout 让写冲突时等待而非立刻抛）。

🔴 **interop 部署**：`System.Data.SQLite.Core` 是元包，实体在 `Stub.System.Data.SQLite.Core.NetFramework`，它通过 `buildTransitive` 把 `x86/SQLite.Interop.dll`、`x64/SQLite.Interop.dll` 复制到输出目录。发布包必须带这两个子目录，否则运行时报「无法加载 DLL 'SQLite.Interop.dll'」。

🔴 **SqlSugar 不建文件夹**：官方说明「带目录的 Data Source，ORM 没办法创建文件夹，需要手动建」。本库的 `ResolveConnectionString` 已代为创建，但**只对 SQLite 生效** —— 其他数据库类型请自行保证目录/库存在。

⚠️ **字符串比较语义不同**：MySQL 默认 collation 不区分大小写，SQLite 的 `BINARY` 区分。产品查询里 `CompareTo` 会被 SqlSugar 翻译成 `case when A>B`，换库后大小写混用的筛选结果会变。

⚠️ **CodeFirst 类型差异**：现有实体的 `ColumnDataType="TEXT"` 在 SQLite 下兼容；`Length=20` 之类在 SQLite 静默无效（不报错）；`IsIdentity` 映射为 `INTEGER PRIMARY KEY AUTOINCREMENT`。

⚠️ **切库不迁数据**：当前默认 SQLite 为空库起步，靠 CodeFirst 建 9 表 + `PermissionSyncService` 同步菜单权限 + 内置 admin 兜底。

## 9. 扩展指南

**新增一种数据库类型**，三处必须同步（顺序不能反）：

1. `Taoyl.Application.DataBase.csproj` **先加驱动包** —— 包没装就先加枚举，用户在下拉里选中就是启动即炸
2. `Share/Config/DatabaseType.cs` 加成员（带 `[Description]`）
3. `SqlSugarHelper.MapDbType` 加分支 —— 漏了会在首次 `Db` 访问时抛 `NotSupportedException`

验证顺序：先在 DevConfig.json 手工改类型 + 连接串 → 运行程序看 CodeFirst 能否建表 → 再回参数页确认下拉项出现。

## 10. 相关文档

- `Taoyl.Application.Configuration/Models/DevConfig.cs` —— `DBConfig` 定义（枚举 + 连接串 + JSON 转换器）
- `Taoyl.IFALaunch/Modules/DataBaseModule.cs` —— 唯一的注册点
- `Taoyl.IFALaunch/Bootstrapper.cs` —— `InitDatabase` 的 CodeFirst 与启动时序
- 各仓储库 README：`Taoyl.Application.Alarm` / `Audit` / `AuthService` / `Product`
