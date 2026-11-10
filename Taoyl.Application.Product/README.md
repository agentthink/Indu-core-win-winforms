# Taoyl.Application.Product —— 产品数据类库 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

产品库 = **产线采集数据的落库与展示底座**：每张产品表（每张表对应一个产品/工序）一套「实体 + DTO + 仓储 + 服务 + 映射」，流程子类负责写，两个通用页面（实时 / 查询）负责读。

**是什么**：
- 流程子类的类型化写入口（`IProductCellInfoService`）：多工位共用一张表，第一工位插入、后续工位按条码查改。
- 两个通用页面的读契约（`IProductDataSource`）：集合注入，新增产品表页零改动。
- 数据访问的泛型底座（`IProductRepository<TEntity>`）：所有产品表共用同一套 CRUD 与分页执行。

**不是什么**：
- 不做超差判定 / 配方比对（2026-10-20 决定整体砍掉，不留扩展点）。
- 不做统计汇总、不做数据清洗、不做自动归档。
- 不做字段级权限（页面级 `Page:ProductLive` / `Page:ProductQuery` + 导出 `Btn:Product_Export`）。

与近邻的分工：`LogDriver` = 全量运行流水（CSV，无人交互）；`Audit` = 人的操作留痕；`Alarm` = 机器状况记录；**Product = 产品实体的业务数据**（有明确业务字段、按产品建表、供追溯查询）。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| DataBase | `SqlSugarHelper`（`Db` 为 Lazy `SqlSugarScope`，构造期不触发建库）+ **SqlSugar 5.1.4.207** + **Mapster 10.0.13**（两包均经基座 PackageReference 传递供货） |

**csproj 当前引用**：`DataBase` / `LogDriver` / `Share`。
代码实际只用 `DataBase`（`SqlSugarHelper`）与传递来的 Mapster——`LogDriver`、`Share` 尚未产生调用，属当前未使用的引用（清理与否不影响功能）。

**被依赖**：
- `Taoyl.UI.Pages`：`ProductLiveViewModel` / `ProductQueryViewModel` 构造注入 `IEnumerable<IProductDataSource>`（集合注入，DeviceManager 同范式）。
- 流程子类（`Business` 等）：构造注入 `IProductCellInfoService`（具体产品服务）。

🔴 本库不依赖任何同级业务模块（TaskProcess / Device / Alarm / Audit / AuthService 等），数据访问只经 DataBase。

### 数据访问边界

```
UI / 流程子类 ──DTO──> IProductCellInfoService / IProductDataSource
                              │
                              ▼
                    ProductCellInfoService（Spec→表达式翻译 / 排序决策 / DTO↔Entity 映射）
                              │ 实体
                              ▼
                    ICellinfoProductRepository（具体产品仓储）
                              │ 继承
                              ▼
                    ProductRepository<TEntity>（泛型纯数据访问，零反射）
                              │
                              ▼
                          SqlSugar
```

- **实体可见域 = 仓储层 + 服务方法栈**；服务接口 / UI / 流程域一律 DTO。
- 服务不出现 SqlSugar 类型；仓储不认识 UI 查询语言 `ProductQuerySpec`（筛选翻译在服务层），只收表达式 + 排序字段名。

## 3. 功能与 API

### 3.1 泛型仓储 `IProductRepository<TEntity>`

所有产品表共用；`TEntity` = 落库实体。**零反射**：不解析主键、不回填自增 ID、不做排序兜底。

| 方法 | 签名 | 语义 |
|---|---|---|
| Insert | `int Insert(TEntity entity)` | 插入，返回受影响行数 |
| Update | `int Update(TEntity entity)` | 按实体主键更新（主键识别由 SqlSugar 依 `[SugarColumn]` 完成） |
| DeleteWhere | `int DeleteWhere(Expression<Func<TEntity, bool>> predicate)` | 按条件删 |
| QueryFirst | `TEntity QueryFirst(Expression<Func<TEntity, bool>> predicate)` | 查单条，无匹配返回 null |
| QueryList | `List<TEntity> QueryList(Expression<Func<TEntity, bool>> predicate)` | 查列表（predicate null = 全表） |
| QueryTop | `List<TEntity> QueryTop(Expression<Func<TEntity, bool>> predicate, string orderField, bool descending, int take)` | 取前 N 条，上限收口 `MaxLiveTake` |
| QueryPage | `List<TEntity> QueryPage(Expression<Func<TEntity, bool>> predicate, string orderField, bool descending, int pageIndex, int pageSize, out int total)` | 服务端分页，Count 与取页独立构建 |

`orderField` 为**属性名字符串**，仓储内部经 `ProductExpressionBuilder.BuildOrder` 转强类型表达式（不拼 SQL，防注入）；空 = 不加排序。

### 3.2 具体产品仓储

| 契约 | 说明 |
|---|---|
| `ICellinfoProductRepository : IProductRepository<ProductCellInfo>` | 电芯注液产品仓储入口；当前空接口，特殊数据访问（批量 / 事务 / 存储过程）在此追加 |
| `CellinfoProductRepository : ProductRepository<ProductCellInfo>, ICellinfoProductRepository` | 仅转构造，复用泛型基类全部实现 |

### 3.3 页面读契约 `IProductDataSource`

产品实时页 / 查询页的唯一依赖，页面零表知识。

| 成员 | 签名 | 语义 |
|---|---|---|
| Key | `string Key` | 表名（落库表名，如 `product_cellinfo`），下拉 Value |
| Title | `string Title` | 下拉显示名 |
| TimeField | `string TimeField` | 时间列属性名；null = 该表无时间维度 |
| Columns | `IReadOnlyList<ProductColumn> Columns` | 列元数据（反射缓存，顺序 = DTO 声明顺序） |
| QueryLatest | `IList QueryLatest(int take, string sortField, bool descending)` | 实时页取最新 N 行；`sortField` null = 回退 `TimeField` 倒序 |
| QueryPage | `IList QueryPage(ProductQuerySpec spec, int pageIndex, int pageSize, out int total)` | 查询页服务端分页 |

返回 `IList`，元素类型 = 该表 DTO（`dgv.DataSource` 直用）。

### 3.4 流程服务 `IProductCellInfoService`

| 方法 | 签名 | 语义 |
|---|---|---|
| Insert | `int Insert(ProductCellInfoDto dto)` | 第一工位插入，返回受影响行数 |
| Update | `int Update(ProductCellInfoDto dto)` | 后续工位补录（全列更新，必须传完整 DTO） |
| DeleteByKeyName | `int DeleteByKeyName(string keyName)` | 按电芯条码删除（慎用） |
| GetByKeyName | `ProductCellInfoDto GetByKeyName(string keyName)` | 按电芯条码查单条，无匹配返回 null |

### 3.5 模型与枚举

- `ProductColumn`：`Name`（属性名 = `DataPropertyName`）/ `Header`（DTO `[Description]`，缺省退回属性名）/ `Type`（可空包装已拆为底层类型）。只有三个字段。
- `ProductQuerySpec`：`FieldName` / `Operator`（`QueryOperator?`）/ `Keyword` / `TimeFrom` / `TimeTo`；全可空 = 不筛。
- `QueryOperator`：`Like(0)` / `Equal(1)` / `NotEqual(2)` / `LessThan(3)` / `LessThanOrEqual(4)` / `GreaterThan(5)` / `GreaterThanOrEqual(6)`。**Like 仅字符串列可用**（页面按 `Column.Type` 纠正，表达式构建器再防御一道）。
- `ProductDataLimits`：`DefaultLiveTake=500` / `MaxLiveTake=1000` / `MaxQueryTake=3000` / `DefaultQueryPageSize=20`。
- `ProductCellInfo`（实体）：`[SugarTable("product_cellinfo")]`，55 列，主键 `SNumber`（int 自增）。
- `ProductCellInfoDto`：与实体 55/55 同构，`[Description]` = 中文列头；`TimeField` = `EntryTime`（`DateTime?`，全表唯一非 string 时间列）。

## 4. 快速开始

容器注册（`Taoyl.IFALaunch/Modules/ProductModule.cs`）：

```csharp
builder.RegisterType<CellinfoProductRepository>()
       .As<ICellinfoProductRepository>()
       .SingleInstance();

builder.RegisterType<ProductCellInfoService>()
       .As<IProductCellInfoService>()
       .As<IProductDataSource>()      // ← 页面靠这个集合注入发现
       .SingleInstance();
```

模块挂载（`Bootstrapper.cs`）：

```csharp
builder.RegisterModule<ProductModule>();
...
InitTables(typeof(ProductCellInfo));   // CodeFirst 建表
```

Mapster 装配（`MappingBootstrapper.cs`）——本模块程序集必须进 Scan 清单，否则启动 `Compile()` 报「未登记」：

```csharp
cfg.Scan(
    typeof(AuditMappingRegister).Assembly,
    typeof(AlarmMappingRegister).Assembly,
    typeof(AuthMappingRegister).Assembly,
    typeof(ProductCellInfoMappingRegister).Assembly);   // ← 本模块
cfg.Compile();
```

最小可跑示例：

```csharp
// 流程子类构造注入 IProductCellInfoService
var dto = new ProductCellInfoDto
{
    KeyName = barcode,              // 电芯条码，多工位匹配键
    EntryTime = DateTime.Now,       // 🔴 禁 DateTime.Now.ToString()（区域格式坑）
    ProcName = "注液",
    // ... 其余字段按工位填写
};
service.Insert(dto);
```

## 5. 使用示例

| 场景 | 用什么 |
|---|---|
| 第一工位写记录 | `IProductCellInfoService.Insert(dto)` |
| 后续工位按条码补录 | `GetByKeyName(barcode)` → 改字段 → `Update(dto)` |
| 页面取全部产品表 | 构造注入 `IEnumerable<IProductDataSource>` |
| 导出全量 | `IProductDataSource.QueryPage(spec, 1, MaxQueryTake, out total)` + `ProductSchema.ToDataTable` |
| 某产品要批量/事务写 | 在具体仓储接口加方法，服务层调用 |

### 场景 1：第一工位插入

```csharp
public class InjectionStation1
{
    private readonly IProductCellInfoService productService;

    public InjectionStation1(IProductCellInfoService productService)
    {
        this.productService = productService;
    }

    public void OnCellArrived(string barcode)
    {
        productService.Insert(new ProductCellInfoDto
        {
            KeyName = barcode,
            EntryTime = DateTime.Now,       // 直接赋 DateTime，不走字符串格式化
            ProcName = "一次注液",
            StationNo = "ST01"
        });
    }
}
```

### 场景 2：后续工位按条码补录

```csharp
var record = productService.GetByKeyName(barcode);
if (record == null)
{
    // 前工位未写入：按业务决定补插或报警，不要静默 Update（Update 全列覆盖）
    return;
}

record.Aftersfc = aftBarcode;
record.AftWeight = weight.ToString("F3");
record.AfterWeightNO = "ST02";
productService.Update(record);   // 🔴 必须传完整 DTO：仓储按实体全列更新
```

### 场景 3：页面拿全部数据源

```csharp
public ProductLiveViewModel(IEnumerable<IProductDataSource> allSources, IMessageBoxService messageBoxService)
{
    var list = allSources.ToList();
    sources = list.ToDictionary(s => s.Key);
    Tables = list.Select(s => new ProductTableOption { Key = s.Key, Title = s.Title }).ToList();
}
```

新增产品表 = 组合根多一行 `.As<IProductDataSource>()`，页面零改动。

### 场景 4：查询 + 导出

```csharp
var spec = new ProductQuerySpec
{
    FieldName = nameof(ProductCellInfoDto.KeyName),
    Operator = QueryOperator.Like,
    Keyword = "ABC",
    TimeFrom = DateTime.Today.AddDays(-2),
    TimeTo = DateTime.Today.AddDays(1).AddSeconds(-1)   // 含当天全天
};

int total;
var rows = dataSource.QueryPage(spec, 1, ProductDataLimits.MaxQueryTake, out total);
var table = ProductSchema.ToDataTable(rows, dataSource.Columns);
MiniExcel.SaveAs(path, table);
```

## 6. 类库结构

| 文件 | 行 | 职责 |
|---|---|---|
| Interfaces/IProductRepository.cs | 44 | 泛型仓储契约（CRUD + QueryTop/QueryPage，纯数据访问） |
| Interfaces/ICellinfoProductRepository.cs | 13 | 电芯注液具体仓储接口（继承泛型基接口，留扩展口） |
| Interfaces/IProductDataSource.cs | 41 | 两页唯一依赖（元数据 + 通用查询） |
| Interfaces/IProductCellInfoService.cs | 24 | 流程子类 CRUD（DTO 进出） |
| Repository/ProductRepository.cs | 123 | 泛型仓储实现（SqlSugar，零反射） |
| Repository/CellinfoProductRepository.cs | 17 | 具体仓储（仅转构造，复用基类） |
| Services/ProductCellInfoService.cs | 93 | 元数据 + Spec 翻译 + 排序决策 + DTO↔Entity 映射 |
| Models/Entities/ProductCellInfo.cs | 181 | 落库实体（55 列，`[SugarTable]` + `[SugarColumn]`） |
| Models/Dtos/ProductCellInfoDto.cs | 179 | 服务/DTO/页面契约（55 属性，`[Description]` 列头） |
| Models/ProductColumn.cs | 22 | 列元数据（Name / Header / Type） |
| Models/ProductQuerySpec.cs | 38 | 查询条件（字段 + 操作符 + 关键字 + 时间范围） |
| Models/ProductDataLimits.cs | 23 | 行数上限常量（唯一真相源） |
| Query/ProductSchema.cs | 91 | **全库唯一反射点**：DTO→列元数据缓存 + 导出 ToDataTable |
| Query/ProductExpressionBuilder.cs | 176 | 字段名 → 表达式树（筛选 + 排序），唯一运行期字段落点 |
| Mapping/ProductCellInfoMappingRegister.cs | 23 | Mapster 双向登记（55/55 同构，无差异声明） |

全库约 1088 行。

## 7. 内部逻辑

### 实时页链路

```
pollTimer.Tick / 手动刷新
   → ProductLiveViewModel.Refresh()
   → IProductDataSource.QueryLatest(take, sortField, descending)
   → ProductCellInfoService（sortField null → 回退 TimeField 倒序）
   → ICellinfoProductRepository.QueryTop(null, orderField, descending, take)
   → ProductRepository.BuildQuery + ApplyOrder（字段名→表达式）
   → SqlSugar.Take(MaxLiveTake 收口)
   → Adapt<List<ProductCellInfoDto>>
   → View Rows 变更 → RebindPreservingScroll（保滚动 + 保选中行）
```

### 查询页链路

```
用户点「查询」
   → ProductQueryViewModel.Load()
   → IProductDataSource.QueryPage(spec, pageIndex, pageSize, out total)
   → ProductCellInfoService
        ├─ ProductExpressionBuilder.BuildFilter<ProductCellInfo>(spec, TimeField)
        └─ QueryPage(filter, TimeField, true, ...)
   → ProductRepository.FindPage（Count 与取页独立构建）
   → Adapt → View RebindPreservingScroll
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 泛型仓储 + 每产品具体仓储接口 | 通用 CRUD 一份实现（`ProductRepository<TEntity>`），具体接口留特殊数据访问扩展口；新增产品仓储层几乎零代码 |
| 仓储**零反射** | 不解析主键、不回填自增 ID、不做排序兜底——`Update` 的主键识别交给 SqlSugar 特性，排序确定性交给服务层 |
| 排序字段名字符串入仓储 | 字段名是 SQL 词汇，不是 UI 契约；仓储内部转强类型表达式，既让服务层干净又不拼 SQL |
| `ProductQuerySpec` 只在服务层翻译 | 它是页面查询语言（字段+操作符+关键字），仓储认识它就是分层被打穿 |
| 页面绑 `IList`（元素=DTO）不绑 DataTable | DataTable 只在导出出现一次（MiniExcel 认 DataTable，列头直接中文） |
| `ProductSchema` 全库唯一反射点 | DTO `[Description]` 反射一次缓存；`ToDataTable` 复用缓存的 `PropertyInfo[]`，不做二次反射 |
| `ProductColumn` 只有三字段 | 没有范围、没有配方、没有取值委托——页面要什么就是什么 |
| 集合注入取代门面 | `IEnumerable<IProductDataSource>`：注册多个、一次全给，新表零改动（DeviceManager 同范式） |
| 产品数据按条码定位 | 多工位共用一张表，`KeyName` 是跨工位匹配键；不按自增主键查删（主键只是库内序号） |
| `Update` 全列覆盖 | 仓储收实体，字段增减零成本；代价是调用方必须传完整 DTO |
| 上限收在仓储实现 | 结构保证，不靠调用方自觉（照 Audit `MaxPageSize` 规范） |
| 时间列只有 `EntryTime` 一个非 string | 时间筛选 / 排序 / 默认双键需要真时间语义；其余测量值列设计决策保持 string（字典序语义知情接受） |

## 8. 注意事项

### 🔴 铁律

1. **实体不出服务栈**：UI / 流程子类只接触 DTO；仓储接口签名实体进出，只供服务层注入。
2. **新增产品必须双向登记映射**：Mapster `RequireExplicitMapping` 下漏登反向，写侧整链抛 `CompileException`。
3. **`Update` 必须传完整 DTO**：仓储按实体全列更新，只填部分字段会把其余列清空。
4. **`DateTime` 字段直接赋 `DateTime.Now`**：禁 `DateTime.Now.ToString()`——区域格式坑（已入踩坑清单）。
5. **新增产品表要同时改两处**：`ProductModule` 注册 + `Bootstrapper.InitTables`；同程序集只加一次 Scan。

### ⚠️ 坑位

1. **string 列的 `< / <= / > / >=` 是字典序语义**：经 `string.CompareTo` 落树，SqlSugar 翻译为 `(case when A>B then 1 when A=B then 0 else -1 end)` 再与 0 比较。数值列若定义成 string，比较结果不是数值大小——知情接受。
2. **`Like` 只对 string 列有效**：页面选非文本字段时会自动纠正为 `=`；表达式构建器对越界调用抛 `NotSupportedException`。
3. **实时页「不排序」回退时间列倒序**：`QueryLatest` 的 `sortField` 为 null 时，服务层钉死 `TimeField` 倒序，不随页面方向下拉泄漏。
4. **实时页保选中按行号不按记录**：新数据前插会让同号行换成别的记录；要「盯住某条记录」用查询页按条码查。
5. **查询页首次打开是空表**：不自动查（2026-10-24 决定），用户点「查询」才动；切表也只清空不查。
6. **`QueryPage` 的 Count 与取页独立构建**：共用 `ISugarQueryable` 实例时 `Count()` 的聚合改写可能污染取页语句（照 Audit 先例）。
7. **导出超 3000 行会截断**：页面提示「命中 N 行，本次仅导出前 3000 行」，不会静默。

## 9. 扩展指南

### A. 新增一张产品表（8 步）

以「电芯化成」`product_formation` 为例（下表用 `ProductFormation` 作示例表名；模块现有实现是 `ProductCellInfo` + 通用仓储/服务）：

| 步 | 文件 | 内容 |
|---|---|---|
| 1 | `Models/Entities/ProductFormation.cs` | `[SugarTable("product_formation")]` + 55 列（照老项目字段） |
| 2 | `Models/Dtos/ProductFormationDto.cs` | 与实体同构，每属性 `[Description("中文列头")]` |
| 3 | `Interfaces/IFormationProductRepository.cs` | `: IProductRepository<ProductFormation>`（空接口） |
| 4 | `Repository/FormationProductRepository.cs` | `: ProductRepository<ProductFormation>, IFormationProductRepository`（仅转构造） |
| 5 | `Interfaces/IProductFormationService.cs` | 业务 CRUD（`Insert` / `Update` / `DeleteByBarcode` / `GetByBarcode`） |
| 6 | `Services/ProductFormationService.cs` | 实现业务接口 + `IProductDataSource`（元数据 + Spec 翻译 + 映射） |
| 7 | `Mapping/ProductFormationMappingRegister.cs` | `config.NewConfig<ProductFormation, ProductFormationDto>()` 双向 |
| 8 | `ProductModule.cs` + `Bootstrapper.cs` | 注册具体仓储 + 服务（`.As<IProductDataSource>()`）；`InitTables(typeof(ProductFormation))` |

**页面零改动**：两个页面经 `IEnumerable<IProductDataSource>` 自动多出下拉项。

### B. 新增字段

1. 实体与 DTO 同步加（保持同构）→ 映射核对脚本自动通过。
2. 若新字段是时间列，把服务实现的 `TimeField` 改过去（同时确认页面时间筛选用的是这一列）。
3. 服务接口 / 页面契约签名零改动（收 DTO）。

### C. 某产品需要特殊数据访问

在具体仓储接口加方法，实现类里写 SqlSugar：

```csharp
public interface IFormationProductRepository : IProductRepository<ProductFormation>
{
    int BatchInsert(List<ProductFormation> rows);
}
```

服务层构造签名不变（依赖具体接口而非泛型基接口）。

### D. 查询加维度

`ProductQuerySpec` 加字段 → `ProductExpressionBuilder.BuildFilter` 拼进表达式树 → 页面 VM 传值。两处同步。

### E. 实时页要「按记录保选中」

需要 `IProductDataSource` 加 `KeyField`（唯一标识列名），`RebindPreservingScroll` 按该列值匹配。当前未做（成本高于收益）。

## 10. 相关文档

- `Taoyl.Application.Alarm/README.md`（五件套范式母本）
- `Taoyl.Application.Audit/README.md`（查询页 / 仓储化 / DTO 契约先例）
- `Taoyl.UI.Pages/Views/FormProductQuery.cs`（查询页母本，服务端分页）
- `Taoyl.UI.Pages/Views/FormProductLive.cs`（实时页母本，定时刷新保位）
