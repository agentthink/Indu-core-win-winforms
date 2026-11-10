# Taoyl.Application.FileUtil —— 文件工具 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**CSV 读写的唯一实现**：给流程侧导出采集数据、给 UI 侧导出查询报表，统一走 `ICsvService`。

**是什么**：`ICsvService` 契约（5 个方法）、`CsvService` 实现（基于 CsvHelper，按文件路径分锁、UTF-8 BOM 追加写、DataTable 转换、尾 N 行截取）。

**不是什么**：不处理 Excel（xlsx 用 `MiniExcel`，各模块自己引）、不做文件压缩/加解密、不做路径策略（调用方给完整路径）、不吞异常。

当前库只有 CSV 一个能力 —— 名字叫 FileUtil 是因为历史上还计划放别的文件工具，现在没放。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| CsvHelper 33.1.0 | `CsvReader` / `CsvWriter` / `CsvConfiguration`（引号内逗号与换行、表头映射） |

⚠️ csproj 里另有一条 `Newtonsoft.Json 13.0.4`，**当前代码未使用**（历史残留）。清理它属于依赖治理，与功能无关。

**零项目引用** —— 不引 Share、不引任何同级模块，可独立复用。

## 3. 功能与 API

### `ICsvService`

| 方法 | 签名 | 行为 |
|---|---|---|
| 批量写 | `void Records<T>(IEnumerable<T> data, string filePath)` | **追加写**：文件已存在则不重复写表头；目录不存在自动建；`data == null` 直接返回不报错 |
| 单条写 | `void Record<T>(T data, string filePath)` | 内部转 `Records(new[]{ data })` |
| 读强类型 | `IEnumerable<T> ReadAll<T>(string path)` | **立即物化**（`ToList`）后返回；空文件返回空集合 |
| 读表 | `DataTable ReadAllToDataTable(string path)` | 等价 `ReadAllToDataTable(path, 0)` |
| 读表（截尾） | `DataTable ReadAllToDataTable(string path, int maxRows)` | `maxRows > 0` 时只取**最后 N 行**；空文件返回空表（不是错误） |

所有方法**失败即抛**（`IOException` / `CsvHelperException` / `UnauthorizedAccessException`），由调用方决定提示文案。

## 4. 快速开始

容器注册（`Taoyl.IFALaunch/Modules/SharedModule.cs`）：

```csharp
builder.RegisterType<CsvService>().As<ICsvService>().SingleInstance();
```

最小用法：

```csharp
public class ExportViewModel
{
    private readonly ICsvService csv;

    public ExportViewModel(ICsvService csv) => this.csv = csv;

    public void Export(string file, List<AlarmRecordDto> rows)
    {
        csv.Records(rows, file);      // 追加写；文件不存在时自动写表头
    }
}
```

## 5. 使用示例

### 5.1 导出查询结果（一次性覆盖）

```csharp
// Records 是追加语义。要覆盖，先删文件再写：
if (File.Exists(file)) File.Delete(file);
csv.Records(rows, file);
```

### 5.2 追加采集数据（按天分文件）

```csharp
string file = Path.Combine(logRoot, $"采集_{DateTime.Now:yyyyMMdd}.csv");
csv.Records(batch, file);     // 同一天多次写，只有第一次写表头
```

### 5.3 读回来做校验（只取最后 N 行）

```csharp
// 防"写成功但文件为空"：写后回读最后 1 行确认落盘
DataTable dt = csv.ReadAllToDataTable(file, maxRows: 1);
if (dt.Rows.Count == 0)
    throw new InvalidOperationException("写入后回读为空");
```

### 5.4 导出成 DataTable 给表格控件

```csharp
DataTable dt = csv.ReadAllToDataTable(file);
dgv.DataSource = dt;       // 列名即 CSV 表头
```

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| `ICsvService.cs` | 契约（5 方法）+ 归属说明（CSV 是通用文件能力，不属流程基座） |
| `CsvService.cs` | 实现：按路径分锁、BOM、追加、DataTable 转换、尾部截取 |

## 7. 内部逻辑

### 写入链路

```
Records<T>(data, filePath)
  ├─ data == null → 直接返回
  ├─ 目录不存在 → CreateDirectory
  ├─ 文件已存在 → HasHeaderRecord = false（追加不重复表头）
  └─ lock(LockFor(filePath))                      ← 按规范化绝对路径分锁
       using StreamWriter(filePath, append: true, new UTF8Encoding(true))   ← BOM
       using CsvWriter
         WriteRecords(data)
```

### 读取链路

```
ReadAll<T>(path)
  └─ lock(LockFor(path))
       using StreamReader + CsvReader
         csv.GetRecords<T>().ToList()      ← 必须物化
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 锁按文件路径分（`ConcurrentDictionary<string, object>`） | 原来三个 `static` 锁让所有文件、所有实例串行 —— 导出大文件时全站 CSV 操作一起卡住 |
| 读取立即 `ToList()` | CsvHelper 的 `GetRecords` 是惰性迭代，直接返回会在 `reader` 释放后枚举失败（必抛） |
| 失败一律上抛 | 原实现 `return null` + 空 `catch`，让"没数据"和"读失败"无法区分，写失败也不可见 |
| `maxRows` 重载用 CsvHelper 解析 | 原手写 `Split(',')`，字段含逗号就列错位；引号内换行更是直接崩 |
| 写入用 `UTF8Encoding(true)`（带 BOM） | Excel 打开无 BOM 的 UTF-8 CSV 会乱码 —— 现场第一反馈就是"表是乱码" |
| 追加写时关闭表头 | 同一文件多次写，只有首次写表头，否则中间夹杂表头行 |
| 目录为空串时不建目录 | 原实现 `CreateDirectory("")` 抛异常被空 catch 吞掉，表现为"相对路径导出静默不写" |
| 空文件读成空表而非异常 | 空文件是正常状态（首次导出前），不是错误 |

## 8. 注意事项与 FAQ

🔴 **`Records` 是追加，不是覆盖**。要覆盖必须先删文件。这是现场最容易踩的坑：同一天点两次导出，第二次的数据接在第一次后面。

🔴 **写失败会抛异常**，调用方必须处理。审计/报警导出这类场景应在捕获后提示用户并记日志，不要空 `catch`。

🔴 **`ReadAll<T>` 返回的是内存集合**，大文件（几十万行）会一次性占满内存。超大文件用 `ReadAllToDataTable(path, maxRows)` 截尾，或自己在调用侧分片。

⚠️ **字段类型靠 CsvHelper 默认转换**。`DateTime` / `int` / `double` 都能转，但格式不符合 `InvariantCulture` 会抛 `TypeConversionException`。要自定义映射就在调用侧加 `ClassMap`（本库未暴露配置入口）。

⚠️ **CSV 表头就是属性名**。`class AlarmRow { public string 报警时间 { get; set; } }` 会写出中文表头 —— 需要英文列名时用 `[Name("Time")]`（CsvHelper 特性）或单独定义导出 DTO。

⚠️ **本库不写 BOM 到已有文件**：仅新建文件时带 BOM。如果一个无 BOM 的空文件已存在，追加后仍无 BOM。

**Q：为什么不用 `MiniExcel` 统一出 xlsx？**
两条路并存是有意的：CSV 用于**大流量日志与审计**（体量小、可直接 grep），xlsx 用于**人看的报表**（样式、多 sheet）。本库只管前者，后者由各模块直接引 MiniExcel。

**Q：能不能加个 `Append` 方法区分覆盖与追加？**
可以，但要改契约。当前用"先删再写"也能达成，先积累真实需求再动接口。

**Q：读大文件有没有流式接口？**
没有。`ReadAll<T>` 会物化，`ReadAllToDataTable` 也会全读进内存再截尾。真需要流式时，用 CsvHelper 自己写会更直接（本库不打算做 ORM 式封装）。

## 9. 扩展指南

**A. 新增一种文本格式（如 TSV）**

1. `CsvConfiguration.Delimiter` 改成 `\t` 即可复用整套逻辑
2. 但接口方法签名不含配置参数 —— 要么加一个 `ICsvService` 重载，要么单独建 `ITsvService`
3. 判据：调用方是否需要选择分隔符？需要就新建接口，不需要就在实现里固定

**B. 需求变成"导出 xlsx"**

不要在 FileUtil 里加 MiniExcel 依赖。各模块已经引了 MiniExcel（`Product` / `PlcDriver`），直接在那边写导出更合适 —— 本库的定位是"最通用的 CSV"。

**C. 清理 `Newtonsoft.Json` 冗余引用**

1. 确认全库无 `using Newtonsoft` → 当前已确认无
2. 从 `Taoyl.Application.FileUtil.csproj` 删除该 `PackageReference`
3. 重新编译（无引用变化，纯减包）

## 10. 相关文档

- `Taoyl.IFALaunch/Modules/SharedModule.cs` —— 唯一的注册点
- `Taoyl.UI.Pages/ViewModels/AuditQueryViewModel.cs` —— 审计导出（写前删文件 + 写后回读校验）
- `Taoyl.UI.Pages/ViewModels/AlarmQueryViewModel.cs` —— 报警导出（同形态）
- `Taoyl.Application.Business/Mes/MesLogSink.cs` —— MES 交互 CSV（本库的另一个消费方）
