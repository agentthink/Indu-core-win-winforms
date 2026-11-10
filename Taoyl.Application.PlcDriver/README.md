# Taoyl.Application.PlcDriver —— PLC 驱动 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**工业上位机的 PLC 通讯驱动库**，基于 HSL HslCommunication 12.5.3。定位：安全、可靠、结构清晰、可扩展、非过度工程。

**是什么**：统一 `IPlc` 契约（5 种协议：欧姆龙 CIP / FINS、西门子 S7、Modbus TCP / RTU）、地址表模型与轮询调度（`IoWorker`）、连接状态机与自动重连（`PlcConnectionState`）、读写策略字典（`IPlcProtocolAccess`）、强类型值容器（`TypedValueHolder<T>`，读路径零装箱）。

**不是什么**：不认识业务语义（不知道"启动按钮"代表什么）、不管理设备实例与生命周期（在 `Taoyl.Application.Business` 的 Devices 子域）、不做 MES 对接、不碰 UI。

**依赖铁律**：公共 API 零 HSL 类型 —— HSL 只存在于 `Transports/HslBaseTransport`（转换点）与各协议 `Drivers/*`（装配点）；结果统一 `ResponseResult`。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| HslCommunication 12.5.3 | `DeviceTcpNet` / `DeviceSerialPort` / `SiemensS7Net` / `OmronConnectedCipNet` / `OmronFinsTcpNet` / `ModbusTcpNet` / `ModbusRtu` / `PipeTcpNet`；`LogNetDateTime` 日志 |
| MiniExcel 1.42.0 | 地址表 Excel 读写（`ExcelTagConfigRow`） |
| System.Runtime.CompilerServices.Unsafe 6.1.2 | `TypedValueHolder<T>` 的 `Interlocked.MemoryBarrier` 依赖 |
| `Taoyl.Application.Share` | 无直接类型使用；按分层惯例保留引用位 |

被依赖：`Taoyl.Application.Business`（`Devices/Factory/PlcDeviceFactory`、`Devices/Adapter/PlcDeviceAdapter`）、`Taoyl.Application.Device`（`IDevice` 契约方向）。

⚠️ **授权限制属通讯库侧，不是本库**：HSL 免费版官方标注「可免费运行 24 小时」仅作功能性验证，超时后**所有网络通信不再成功**。本库不调用 `HslCommunication.Authorization.SetAuthorizationCode`，不携带任何授权码 —— 未购买授权时长时间连续运行会表现为通讯中断，本库的地址表、状态机与业务语义均不受影响。

## 3. 功能与 API

### `IPlc`（`Contracts/IPlc.cs`）

组合 `IPlcReadWrite` + 连接管理 + 身份标识。

| 成员 | 签名 | 说明 |
|---|---|---|
| 名称 | `string Name { get; }` | 实例标识（= 配置 `name`） |
| 协议 | `string ProtocolType { get; }` | 配置原始串，与 `Devices.json` 的 `protocol` 同源（即 `PlcProtocol` 成员名） |
| 队列深度 | `int QueueDepth { get; }` | `IoExecutor` 队列深度透传（诊断用） |
| 连接 | `ResponseResult Connect()` / `ResponseResult Close()` | 缓存模式/IoWorker 生命周期内聚于此，外部零启停入口 |

### `IPlcReadWrite`（`Contracts/IPlcReadWrite.cs`）

**失败一律返回 `Fail`，不抛异常**。

| 分组 | 签名 |
|---|---|
| 直接读写 | `ResponseResult WriteTag(string group, string tag, object value)`、`ResponseResult<T> ReadTag<T>(string group, string tag)`、`ResponseResult<Dictionary<string,object>> ReadGroup(string group)`、`ResponseResult WriteGroup(string group, Dictionary<string,object> values)` |
| 缓存读写 | `ResponseResult WriteTagCached<T>(string group, string tag, T value)`、`ResponseResult<T> ReadTagCached<T>(string group, string tag, bool deepCopy = true)`、`bool TryReadTagCached<T>(string group, string tag, out T value)`、`ResponseResult<Dictionary<string,object>> ReadGroupCached(string group)` |
| 纯表访问 | `ResponseResult<T> GetValue<T>(string group, string tag)`、`ResponseResult SetValue(string group, string tag, object value)` —— **不走队列不通讯**，供 UI 绑定/监控展示 |
| 底层 | `ResponseResult ReadRaw(...)` / `ResponseResult WriteRaw(...)` | 组名/标签名为空，请求自包含（Address/DataType/Length） |

### `IPlcConfigManager`（`Config/IPlcConfigManager.cs`）

地址表与主配置的唯一持有者。

| 分组 | 成员 |
|---|---|
| 主配置 | `PlcConfig LoadJsonConfigForPlc(string jsonString)`（同名缓存短路）、`PlcConfig UpdatePlcConfig(PlcConfig)`（**覆盖式保存**，绕开短路）、`PlcConfig GetPlcConfig(string)`、`bool RemovePlcConfig(string)` |
| 地址表 | `bool LoadTagPollingConfigForPlc(string plcName, string excelPath, IPlcLogger externalLogger = null)`、`ConcurrentDictionary<string, TagGroup> GetTagTable(string plcName)`、`bool RemovePlcTagGroups(string plcName)` |
| 导出 | `bool SaveTagPollingConfigToExcel(string plcName, string excelPath, IPlcLogger externalLogger = null)`、`bool SaveTagPollingConfigToConfiguredFile(string plcName, IPlcLogger externalLogger = null)` |
| 调试接口 | `AddTagGroup` / `UpdateTagGroup` / `RemoveTagGroup` / `AddTag` / `UpdateTag` / `RemoveTag`（CD 原子操作，运行时动态增删改） |

### `PlcDataType`（`Models/PlcDataType.cs`）

**11 单值**：`Byte` / `Bool` / `Short` / `UShort` / `Int` / `UInt` / `Long` / `ULong` / `Float` / `Double` / `String`
**10 数组**：`ByteArray` / `BoolArray` / `ShortArray` / `UShortArray` / `IntArray` / `UIntArray` / `LongArray` / `ULongArray` / `FloatArray` / `DoubleArray`

🔴 **成员名是磁盘契约**（地址表 Excel 的 `DataType` 列），改名即破坏存量文件。

### `ResponseResult`（`Contracts/ResponseResult.cs`）

对标 HSL `OperateResult` 的自定义镜像：`IsSuccess` / `ErrorCode` / `Message` + 泛型 `Content`。

| ErrorCode | 含义 |
|---|---|
| `0` | 成功 |
| `< 0` | 网络类错误（断线判定依据） |
| `10000` | 默认未知 |
| `> 0` 且 `≠ 10000` | PLC 原生错误码 |

泛型版显式实现 `IResponseResultContent.ContentObject`，供以 object 方式取内容（零反射）。

### `PlcConfig`（`Models/Config/PlcConfig.cs`）

主配置模型，**JSON 契约**（与 `Devices.json` 字段同源）。

| 字段 | 说明 |
|---|---|
| `name` / `type` / `ip` / `port` | 标识与端点。**`ip` = 连接端点**：网络协议填 IP，串口协议填 COM 号（显示名「IP地址/串口号」） |
| `protocol` | 协议串，`Enum.TryParse<PlcProtocol>` 大小写不敏感 |
| `ConnectTimeOut` / `ReceiveTimeOut` | **顶层属性**（非 `protocolParams`），未配置回退 5000 / 10000ms |
| `plctagsPath` / `pollingTime` / `RetryConnect` / `logPath` | 地址表路径 / 轮询间隔 / 重连次数上限 / 日志目录 |
| `protocolParams` | 协议差异参数字典（键不区分大小写） |

## 4. 快速开始

容器注册（`Taoyl.IFALaunch/Modules/DeviceModule.cs`）：

```csharp
// PLC 配置管理器（保留静态单例 Instance 过渡，注入走同一实例）
builder.RegisterInstance(ConfigManager.Instance).As<IPlcConfigManager>().SingleInstance();

// 设备工厂（构造注入 IPlcConfigManager）
builder.RegisterType<PlcDeviceFactory>().As<IDeviceFactory>().SingleInstance();
```

设备创建链路：

```
DeviceManager → PlcDeviceFactory.CreateDevice(name, config)
  → IPlcConfigManager.LoadJsonConfigForPlc        （同名缓存短路：重建前先 UpdatePlcConfig 保存新配置 → 此处返回最新）
  → 协议白名单：Enum.TryParse<PlcProtocol>(plcConfig.Protocol) → _protocolFactories 查表（大小写不敏感）
      → OmronConnectedCipNet → OmronCip.Create(plcConfig, configManager)   （返回 IPlc）
      → SiemensS7Net        → SiemensS7.Create(plcConfig, configManager)   （返回 IPlc）
  → 两个驱动同一个三动作（以 S7 为例）：
      → new SiemensS7Net(plcType)                              （HSL 设备；机型须在构造期定，构造后改不了）
      → HslTcpTransport.AttachTcpPipe(hslPlc, cfg, DefaultPort) （管道装配：IP/端口兜底/超时/关管策略）
      → new HslTcpTransport(device)                            （传输原语）
      → new SiemensS7ProtocolAccess(transport)                 （协议策略）
      → new SiemensS7(plcConfig, protocolAccess, configManager) （3 参构造，transport 基类自取）
  → PlcDeviceAdapter(name, plcConfig, plc) 包装为 IDevice
```

工厂是**无状态纯工厂**（每次 new，不缓存实例）：实例由 `DeviceManager` 统一持有，防重/释放/重建均为其职责。运行时改配置重建 = ① `UpdatePlcConfig` 保存 → ② `DeviceManager.BuildDevice`（先建新 → 成功后摘旧键并占位 → 最后释放旧实例）。

## 5. 使用示例

### 5.1 读写（直接 / 缓存 / 纯表）

```csharp
IPlc plc = deviceAdapter.GetRawPlc();

// 直接读写：立即与 PLC 通信，同步返回；失败返回 Fail 不抛异常
var writeResult = plc.WriteTag("Group1", "StartBtn", true);
if (!writeResult.IsSuccess) { /* 处理 writeResult.Message */ }

var readResult = plc.ReadTag<int>("Group1", "CurrPos");
if (readResult.IsSuccess) { int value = readResult.Content; }

// 缓存读写：读走轮询刷新的表值，写入队由 IoWorker 执行
plc.WriteTagCached<float>("Group1", "TargetSpeed", 100.5f);
var speedResult = plc.ReadTagCached<float>("Group1", "TargetSpeed");

// 高频缓存读（无结果对象开销）
if (plc.TryReadTagCached("Group1", "CurrPos", out float pos)) { }

// 纯表访问（不通讯）
var valueResult = plc.GetValue<int>("Group1", "CurrPos");   // ResponseResult<int>
plc.SetValue("Group1", "ManualValue", 42);                  // ResponseResult
```

三种读法的区别：

| 方法 | 通讯 | 取的是 | 典型场景 |
|---|---|---|---|
| `ReadTag<T>` | ✅ 立即 | PLC 当前值 | 手动触发、低频、要确定性 |
| `ReadTagCached<T>` | ❌ | 轮询表最新值 | 界面刷新、高频 |
| `GetValue<T>` | ❌ | 表值（可能与 PLC 不同步） | 只读展示、调试 |

### 5.2 配置（JSON）

```json
{
  "name": "PLC1",
  "type": "PLC",
  "ip": "192.168.1.10",
  "port": "44818",
  "protocol": "OmronConnectedCipNet",
  "ConnectTimeOut": "5000",
  "ReceiveTimeOut": "10000",
  "RetryConnect": "0",
  "plctagsPath": "./Config/PlcTagPolling_PLC_OmronCip_R.xlsx",
  "pollingTime": "20",
  "logPath": "./Logs/PLC/PLC1",
  "protocolParams": {
    "ReconnectBaseDelay": "3000",
    "ReconnectMaxDelay": "90000"
  }
}
```

| 参数 | 说明 |
|---|---|
| `RetryConnect` | **自动重连次数上限**：空/0 = 无限重试直到 Close（默认）；>0 = 最多重连 N 次后停止，需上层再 `Connect()` |
| `ConnectTimeOut` / `ReceiveTimeOut` | **必须写顶层**。`protocolParams` 里的同名键**已失效**（不再被读取） |
| `port` | 可留空 = 用协议默认端口（CIP 44818 / S7 102，各驱动私有 `DefaultPort`）。非标准端口才填，填了必须合法（1~65535） |
| `ReconnectBaseDelay` / `ReconnectMaxDelay` | 重连退避的起点与封顶（默认 3000 / 90000ms），走 `protocolParams` |
| **S7 专属** | `PLCType`（默认 `S1500`，可选 `S1200`/`S300`/`S400`/`S200Smart`/`S200`，未知值抛 `ArgumentException`）、`Rack` / `Slot`（**仅显式配置才写**，S300/S400 按现场机架槽位配） |

### 5.3 地址表（Excel，15 列）

`PlcTagPolling_*.xlsx`，Sheet1，表头：

`GroupID/组序号` · `GroupName/组名` · `GroupDesc/组描述` · `TagID/组中序号` · `TagName/标签名称` · `TagDesc/标签描述` · `Address/地址` · `DataType/数据类型` · `Length/元素个数地址长度` · `RefreshFreq/刷新频率` · `RWTimes/读写次数` · `Value/值` · `LastOperationTime/最后读写时间` · `LastOperationResult/最后读写操作结果` · `LastOperationMsg/最后读写信息`

| 列 | 要点 |
|---|---|
| `RefreshFreq` | **-1 = 不刷新**（不参与轮询）；其余为刷新周期 |
| `Length` | **数组一律填元素个数**（含 `ByteArray`）。String 由协议解释：CIP 填字符数；S7 填 0 读原生 `STRING[n]` 整串 / N 读 N 字节裸解码 |
| 组属性 | `GroupID`/`GroupDesc` 是组内行冗余列，加载取组首行；存量 12 列文件缺列时组序号按首现序自增兜底 |

导出：`IPlcConfigManager.SaveTagPollingConfigToExcel(plcName, path)` —— 当前内存地址表（含调试界面改动）导成同模板快照，可回导。

## 6. 类库结构

| 文件 | 说明 |
| ---- | ---- |
| **Contracts/**（契约层，零业务依赖） | |
| `ResponseResult.cs` | 操作结果类型（`IsSuccess`/`ErrorCode`/`Message` + 泛型 `Content` + 静态工厂）；含非泛型视图接口 `IResponseResultContent` |
| `IPlc.cs` | PLC 主接口：组合 `IPlcReadWrite` + 连接管理/状态 + `Name` / `ProtocolType` / `QueueDepth` |
| `IPlcReadWrite.cs` | 读写接口：直接读写 + 缓存读写 + 纯表访问 + 底层读写，失败一律 `Fail` |
| `IPlcTransport.cs` | 传输层接口（纯原语）：Connect/Close + 类型化读写 + `CheckConnectionStatus` |
| `PlcProtocol.cs` | 协议枚举（**成员名 = 磁盘契约串**）：`OmronConnectedCipNet` / `SiemensS7Net` 等 |
| **Models/** | |
| `PlcDataType.cs` | 数据类型枚举（11 单值 + 10 数组） |
| `TagItem.cs` | 标签项（地址表 15 列映射 + `TypedValue` 强类型容器 + LastOperation 三件套） |
| `TagGroup.cs` | 标签组（`GroupName` / `GroupDesc` / `GroupID` / `Tags`），组身份三重一致不变量 |
| `Models/Config/PlcConfig.cs` | PLC 设备配置（JSON 契约，`protocolParams` 承载协议差异） |
| `Models/Config/ExcelTagConfigRow.cs` | 地址表 Excel 映射（15 列表头，`[ExcelColumnName]` 显式映射） |
| `Models/Cache/ITypedValue.cs` | 值容器类型擦除视图（`DataType` / `HasValue` / `GetObject` / `SetObject`） |
| `Models/Cache/TypedValueHolder.cs` | 强类型容器 `TypedValueHolder<T>`（`T _value` 直存，零装箱；`Interlocked.MemoryBarrier` 保可见性） |
| `Models/Cache/TypedValueHolderFactory.cs` | 值容器工厂（`DataType` → CLR 类型映射 + `MakeGenericType`） |
| **Config/** | |
| `IPlcConfigManager.cs` | 配置与地址表管理契约（主配置 / 地址表 / 导出 / 调试接口） |
| `ConfigManager.cs` | 实现（静态单例 `Instance` 过渡 + 注入并存）；地址表在配置加载阶段顺带加载 |
| **Transports/**（传输原语层） | |
| `HslBaseTransport.cs` | 适配器基类（TCP / 串口共用）：读写转发与连接状态判定收口；**唯一 HSL `OperateResult`→`ResponseResult` 转换点**；连接动作下沉子类 |
| `HslTcpTransport.cs` | TCP 形态：收 `DeviceTcpNet` 强类型；**静态 `AttachTcpPipe(device, plcConfig, defaultPort)` = 管道装配唯一收口**（IP / 端口兜底 / 两个超时 / `CloseOnRecvTimeOutTick=-1` 关管策略），各协议驱动共用 |
| `HslSerialTransport.cs` | 串口形态：收 `DeviceSerialPort` 强类型；`AttachSerialPipe` = 串口参数解析唯一落点（**必须在 `Open()` 之前调用**）；HSL 串口无真异步，用 `Task.Run` 包装 |
| **Protocols/**（协议层：怎么解释数据类型） | |
| `IPlcProtocolAccess.cs` | 协议访问接口：Read/Write 策略字典（按需查询，未注册返回 false）；读委托带值容器输出槽，不依赖 `TagItem` |
| `BaseProtocolAccess.cs` | 模板方法：初始化 21 条读写策略；读策略在泛型闭包内 `TryWriteHolder` 强类型写入（零装箱）；String 读朴素透传，长度语义交协议 |
| `OmronCipProtocolAccess.cs` | 欧姆龙 CIP 差异（**唯一一条：String = ASCII**）：覆写 String 读写 + **原样写**（不 PadRight/Substring，避免静默截断）；**`Length ≤ 0` 显式 Fail**（CIP 无「0=整串」语义） |
| `SiemensS7ProtocolAccess.cs` | 西门子 S7 差异（**唯一一条：String 读写策略**）：读不覆写（`len==0` 由 HSL 分流原生 STRING）；写必须覆写走**读-改-写**（读 MAX/CUR → 校验 ≤ MAX → 写 `[MAX,CUR]+bytes`），超长报错不截断 |
| **IO/**（IO 引擎三组件） | |
| `IoRequest.cs` | IO 请求（Kind/Address/DataType/Length/Value/GroupName/TagName） |
| `IoQueue.cs` | 单队列：`TryEnqueue` 按 Kind+组+标签去重（**仅缓存读**）；队列满/关闭时回滚去重标记；`Clear()` 会话终结用 |
| `PlcConnectionState.cs` | 连接状态机（手写轻状态机）：三态 + 防抖 N=3 + 指数退避 + `TryReconnectAsync` |
| `IoExecutor.cs` | **唯一执行入口（会话模式）**：`ExecuteSession(req, syncDirect)`；持 `_ioLock` / `_directBusy` / 策略字典；支持 raw 模式 |
| `IoWorker.cs` | 单线程调度循环：生成 → 入队 → 出队 → 执行 → 断连重连分支 |
| **Drivers/**（驱动运行时） | |
| `PlcRuntime.cs` | 运行时基类：业务 API 薄壳 + 装配三组件 + `Connect`/`Close`（IoWorker 生命周期内聚） |
| `OmronCip.cs` / `SiemensS7.cs` / `OmronFinsTcp.cs` / `ModbusTcp.cs` / `ModbusRtu.cs` | 各协议驱动：静态 `Create(plcConfig, configManager)` 返回 `IPlc`；装配 HSL 设备 → 管道 → 传输 → 协议访问 → 运行时 |
| **Logging/** | |
| `IPlcLogger.cs` | 日志接口（继承 `IDisposable`，5 个级别方法），公共 API 无 HSL 类型 |
| `PlcLogger.cs` | 实现（包装 HSL `LogNetDateTime`，按小时切分）；`ResolveDirectory` 解析目录；释放后 Write* 静默丢弃 |

## 7. 内部逻辑

### 架构分层（依赖只向下）

```
业务层 → IPlc (Contracts)
        ↓
PlcRuntime（Drivers）：业务 API 薄壳 + 装配三组件 + 连接/缓存控制
        ↓
IoExecutor / IoWorker / PlcConnectionState（IO：会话执行 / 调度循环 / 状态机）
        ↓
IPlcProtocolAccess（Protocols：怎么解释数据类型 —— 读写策略字典）
        ↓ 查 DataType
IPlcTransport（Transports：怎么执行原语）── 唯一 HSL 转换点
        ↓
HslCommunication
```

**协议层 vs 传输层边界**：策略字典（含 String 编码与长度语义差异）归协议层；传输层是纯原语（无 `DataType` 概念、无编码参数）。**地址格式（CIP tag 名 / S7 `DB1.DBD100`）是纯数据** —— 由各协议在 HSL 内部消化，驱动不解析、不校验。

**换通讯库只改 Transport**：新增实现类实现 `IPlcTransport`，`CheckConnectionStatus` 按新库错误语义重写，PlcRuntime / 协议层 / IO 层零改动。

### 数据流

```
轮询读：  IoWorker 循环 → GeneratePollRequests（遍历地址表取到期标签）
          → IoQueue.TryEnqueue（去重）→ 出队 → ExecuteSession(req, false)
          → 查策略字典 → transport 原语 → 策略闭包强类型写容器 → Commit（三件套 + RWTimes）

直接读写：ReadTag/WriteTag → ExecuteSession(req, true)（旁路不入队，_directBusy 置位暂停出队）

缓存写：  WriteTagCached → 入队即 Success → IoWorker 会话执行（断线期间入队，重连后按序补发）

异常：    ExecuteSession 结束后 → UpdateConnectionFromResult（锁外，防抖 N=3）→ 判定断开则 IoWorker 进重连分支
```

### IO 模型（会话模式统一收口）

- **唯一执行入口**：`IoExecutor.ExecuteSession(req, syncDirect)` = 锁内 Execute（查策略字典 → transport 原语）→ 锁外连接状态更新（防抖）→ 锁外 Commit（Value + 三件套 + RWTimes≥9999 归 0）
- **三来源**：缓存轮询读 / 缓存写入队 / 直接读写旁路
- **去重**：`IoQueue.TryEnqueue` 按 `Kind+组名+标签名` **仅对缓存读去重**（队列满回滚标记）；写不去重
- **raw 模式**：组名/标签名为空时跳过标签校验，请求自包含（Address/DataType/Length）
- **取值通道**：高频缓存读由策略闭包强类型写入容器（零装箱）；`ReadGroup` 整组直读走 `IResponseResultContent.ContentObject` 非泛型视图（零反射）

### 连接状态与重连

- **三态**：`Disconnected` → `Connecting` → `Connected`；**Connect 失败显式回落 `Disconnected`**（防卡 Connecting）
- **判定**：每次 Execute 后 `UpdateConnectionFromResult(result)` → `transport.CheckConnectionStatus(result)`
- **防抖**：连续 N=3 次异常才判断开（成功清零；断开后不再计数）
- **触发**：**仅 IoWorker 循环**（断连时 `ShouldAttemptReconnect` 退避放行才重连）；直接读写只更新状态不触发
- **执行**：`TryReconnectAsync` = Close → Connect；退避首试立即 → base×2ⁿ → 间隔封顶；取消令牌可打断
- **`_isConnected` 是缓存不是实时探测**：只在 Execute 后按 IO 结果更新，两次 IO 之间可能滞后

### 缓存模式与 IoWorker 生命周期

- **本质**：IoWorker = 轮询调度 + 缓存写出队 + **唯一自动重连触发者** → 它不是一个"附加功能开关"，而是实例正常工作的一部分，故内聚进连接生命周期，`IPlc` 不暴露启停入口
- **启停**：`Connect()` 的 finally 调 `EnsureIoWorkerRunning()`（幂等，**成败不论** —— 成功则轮询刷新，失败则循环按退避接管重连）；`Close()`/`Dispose()` 调 `StopIoWorker()`（Cancel + 最多等 2s）
- **单实例**：实例级仅一个 IoWorker；断线不重建（循环内切重连分支）；Close 停并置 null 后再次 Connect 才是全新启动
- **会话终结清账**：`Close()` 调 `IoExecutor.ClearQueue()`（清内容 + 去重集）—— **防手动断开再连接时向 PLC 补发上一会话的过期写**；内部自动重连不经 Close，断线期间堆积的写保留、重连成功后补发

### 值存储与零装箱

- **存储**：`TagItem.TypedValue`（`ITypedValue` 引用，volatile）指向 `TypedValueHolder<T>`（加载期按 `DataType` 定型）；内部 `T _value` 直存，`Set/TryGet` 全程强类型
- **写入**：读策略委托带值容器输出槽 `(addr, len, holder)`；object 低频写（写成功回写/纯表写/重载迁移）经 `SetObject`；**`TagItem.Value` 只读，无写门面**
- **读取**：`ReadTagCached<T>` 优先 `holder.TryGet(out value)`（引用转换零分配）；数组深拷贝保护保留
- **收益边界**：真正受益的是 5 个单值类型（Bool/Short/Int/Float/Double），数组本身是引用类型；写路径与 object 视图仍装箱，均属低频路径，刻意保留
- **未刷新语义**：`_hasValue` 状态位区分未刷新/已赋值（等价旧 `Value == null`）

### Byte / ByteArray 读写语义（HSL OmronConnectedCipNet 实证）

| 类型 | Length 语义 | 读 | 写 |
|---|---|---|---|
| `Byte` | 固定 1 字节 | `Read(addr,1)` 取返回数组首位（带 `IsSuccess && Content != null && Length > 0` 防御） | 显式包单字节数组 `Write(addr, new[]{ val })` → HSL `Write(string,byte[])`（typeCode 209/BYTE，写 1 字节） |
| `ByteArray` | **元素个数**（与其他数组一致，非字节数） | 朴素 `Read(addr,len)`：len 即元素个数，返回该 tag 的全部字节 | 按数组实际元素个数写入，不做补齐 |

- **HSL 语义（12.5.3 反编译实证）**：`Read(address, length)` 的 length 是**数组元素个数**（官方 XML：`In the case of arrays, the length of the array`）；`length == 1` 走特判分支，直接返回该 tag 的全部字节。故欧姆龙 CIP 与基类"统一传输单元语义"天然一致 —— `Byte` / `ByteArray` 不是协议差异，协议层不覆写
- **类型码对照**（HSL `WriteTag(address, typeCode, data, length)`）：`Write(addr,byte)` = 194/SINT 写 1 字节；`Write(addr,byte[])` = **209/BYTE**（传输层走这条）；`Write(addr,short)` = 195/INT；各数组写传 `values.Length` 作元素个数
- ⚠️ **`Byte` 写对应 PLC 端 BYTE 类型 tag**：若现场 tag 定义为 SINT（194），类型不匹配设备直接报错；需要时另立数据类型，不在 `Byte` 上兜底
- ⚠️ **地址带 `[index]`**：HSL `Write(address, byte[])` 在地址**不带 `[index]`** 时取 `length = 1`（带 `[index]` 才取 `data.Length`）→ 写多元素数组建议地址写 `D100[0]` 形式；`bool[]` 写同理
- **文本写入格式**（`PlcTextValue.Parse`）：`ByteArray` 分隔符支持**空格或逗号**（`"0x01 0x02"` / `"1,2"`），元素支持十进制与 **`0x` 十六进制前缀**；格式/越界错误返回 `Fail` 不抛异常

### 日志

- 实例级（PlcRuntime 与 ConfigManager 各自独立），非全局单例；`IPlcLogger : IDisposable`
- **释放后 Write* 静默丢弃**（自防御），容忍关停期残留线程
- **目录解析**：绝对路径原样使用；未配置/相对路径 → 挂**程序所在盘根**再拼相对路径
- **产物形态**：HSL `LogNetDateTime` 以目录为参数、按小时生成 `Logs_yyyyMMdd_HH.txt` —— **构造传目录即可，勿再拼 `.log` 文件名**（否则 HSL 会把该名字建成多余子目录）

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 策略字典下沉协议层 | 数据类型解释是协议差异的唯一来源；字典让"加类型只加一行"成立，且未注册能显式返回 false 而非静默兜底 |
| 传输层只留纯原语 | 传输层一旦认识 `DataType`，换通讯库就要连带改语义映射；纯原语让"换 Transport 不动上层"成立 |
| 结果统一 `ResponseResult` | 通讯失败是常态，异常式 API 会逼每个调用点包 try/catch；结果对象让失败成为数据 |
| 缓存模式内聚连接生命周期 | 它不是可选功能，是断线自愈的载体；暴露启停只会让调用方把 IoWorker 关掉然后抱怨不重连 |
| 会话模式统一 `ExecuteSession` | 消除手动五步（_directBusy/锁/策略/状态更新/Commit），三来源行为一致 |
| 去重只对缓存读 | 写请求有顺序与语义，去重会丢写；读是幂等的，去重才能压住高频轮询 |
| 状态机手写不引库 | 三态 + 防抖 + 退避，`Stateless` 在这个规模上是过度设计 |
| 读路径强类型通道 | 高频轮询下装箱是真实开销；写路径低频，保留 object 换实现简单 |
| 地址表集中归 ConfigManager | 运行时零启停入口 + 现取不缓存，改即生效；`PlcRuntime` 只持有引用不持有表 |

## 8. 注意事项与 FAQ

🔴 **纯写型实例不会发现断线**。断线判定（连续 N=3 失败）与重连触发（IoWorker 仅 `!IsConnected` 分支）都依赖持续 IO 流 —— 地址表全 `RefreshFreq=-1` 且业务低频时，断线后 `IsConnected` 恒 true，`RetryConnect=0` 也不生效。**解法（零代码）**：地址表放一个 `RefreshFreq>0` 的心跳标签。

🔴 **`WriteTagCached` 返回 Success 只代表入队成功**，不代表已写入 PLC。断线期间入队的写在重连后按序补发（含过期值）。需要回执用 `WriteTag`；"使能/启动"类瞬态写必须自行判活。

🔴 **`protocolParams` 里的 `ConnectTimeOut`/`ReceiveTimeOut` 已失效**，必须写顶层。

🔴 **S7 联调前置（非代码问题）**：① DB 必须取消「优化的块访问」；② PLC 侧必须允许 PUT/GET 通信；③ 地址与数据类型必须自相一致（`DBD`=4B / `DBW`=2B / `DBB`=1B）；④ 首次连接核一次 Rack/Slot。

🔴 **S7 `Length=0` 三处必须同规则**：配置加载 / 调试界面预检 / 工作行提交 —— 缺一处 `Length=0` 会被静默改写成 1，S7 双模失效（表现为读到 `[MAX][CUR]` 头垃圾）。

⚠️ **`Byte` 写对应 PLC 端 BYTE 类型 tag**。若现场 tag 定义为 SINT，类型不匹配设备直接报错；需要时另立数据类型，不在 `Byte` 上兜底。

⚠️ **HSL `Write(address, byte[])` 在地址不带 `[index]` 时取 `length = 1`**，写多元素数组建议地址写 `D100[0]` 形式。

⚠️ **`port` 留空 = 协议默认端口**。`NormalizePlcConfig` 不再兜 Port/Protocol（原兜底 `"44818"`/`"EIP"` 会把 S7 配置静默改成欧姆龙端口）；`protocol` 留空由校验与协议白名单响亮报错。

⚠️ **导出是纯内存读**：`SaveTagPollingConfigToExcel` 不自动落盘、不改任何配置源。

**Q：为什么地址表不在 `PlcRuntime` 里？**
`PlcRuntime` 是每个 PLC 一个实例，地址表归 `ConfigManager` 统一持有（两层 CD）。运行时改地址表只想改一处，不想通知 N 个实例刷新引用。

**Q：`ConfigManager.Instance` 静态单例还用吗？**
保留过渡。它与注入的 `IPlcConfigManager` 是同一实例，但新代码走注入。

**Q：断线后缓存值会清空吗？**
不会。重连后第一轮轮询覆盖；期间读到的仍是断线前最后一次成功的值。

**Q：连续运行十几个小时后所有 PLC 同时连不上，重启程序又恢复？**
先排除 HSL 授权计时（见 §2 授权说明）：未激活的免费版超时后**所有**网络通信恒定失败，特征是「全部设备一起掉线、重连也无效、重启进程恢复」，与本库的重连逻辑无关。排除后再查网络与设备。

## 9. 扩展指南

### 9.1 新增一种 PLC 协议

**加协议 = 改 2 处 + 新增 2 文件**，其余层零改动：

| # | 文件 | 动作 |
|---|---|---|
| 1 | `Contracts/PlcProtocol.cs` | 加枚举成员（**成员名即磁盘契约串**：`Devices.json` 的 `protocol` 键，按 `Enum.TryParse` 大小写不敏感解析） |
| 2 | `Taoyl.Application.Business/Devices/Factory/PlcDeviceFactory.cs` | 协议字典加 1 行（**该字典 = 协议白名单**，`CreateDevice` 与 `TryValidateConfig` 同源，漏加即"不支持"） |
| 3 | `Drivers/Xxx.cs` | 新增驱动 `: PlcRuntime`（静态 `Create` 三动作：HSL 设备 → `HslTcpTransport.AttachTcpPipe(...)` → 传输 → 协议访问 → 本类实例）。**协议差异参数在 `Create` 内校验、非法直接抛异常即可** —— 异常由 `BuildDevice` 事务序兜住 |
| 4 | `Protocols/XxxProtocolAccess.cs` | 新增协议差异 `: BaseProtocolAccess`（**只覆写实测有差异的数据类型**） |

**零改动层**：`HslBaseTransport` / `HslTcpTransport` / `HslSerialTransport` / `IPlcTransport` / `IoExecutor` / `IoWorker` / `PlcConnectionState` / `PlcRuntime` / **UI**（含新建模板端口 —— 留空即用驱动默认）全部不动。

**覆写判据（别凭感觉）**：先确认目标协议在 HSL 里 is-a `DeviceTcpNet`（是则传输层可直接复用），再逐类核对基类 **21 条策略**，**只覆写实测有差异的**；"看起来不一样"不算依据，要有反编译或 Demo 实测支撑。

### 9.2 西门子 S7 与欧姆龙 CIP 的实测差异（HSL 12.5.3 反编译 + Demo 实测）

**差异只有三处，其余零差异**：

| 维度 | 欧姆龙 CIP | 西门子 S7-1500 | 判定 |
|---|---|---|---|
| 地址模型 | tag 名（符号寻址） | 绝对地址 `DB1.DBD100` / `M100` / `I1.6` | 纯数据，零代码 |
| 单值读写 | 覆写 | 覆写 | 无差异 |
| 数组读写（10 类） | `Read(addr, len)` = len 个元素 | 同（构造内 `WordLength = 2` 换算） | 无差异 |
| `Byte` / `ByteArray` | len = tag 元素个数 | len = **字节数** | byte 单位下数值巧合等同 ⇒ 实测无差 |
| 🔴 **String 读** | `Read(addr,len)` 后**剥 2 字节头** | `len==0` → 原生 S7 STRING；`len>0` → 回退基类读 len 字节**不剥头** | **有差**（已覆写） |
| 🔴 **String 写** | 纯写 `WriteTag(208, [len]+bytes)` | **读-改-写**：读 MAX/CUR → 校验 ≤ MAX → 写 `[MAX,CUR]+bytes` | **本质不同**（已覆写） |
| `BoolArray` 的 len | 字节数 | **位个数** | 语义不同（地址表注意） |
| 协议错误码 | 无数字码（全落 10000） | 原生码 5/6/10 等 | 无害：`CheckConnectionStatus` 只把 `< 0` 判断线 |
| 字节序 | tag 类型自带 | `ReverseBytesTransform`，默认 `ABCD` = Siemens 原生 | 默认正确 |

### 9.3 西门子 S7 地址规则

S7 地址字段是**位地址**（HSL `CalculateAddressStarted`：无点 ⇒ 值 × 8；有点 ⇒ 前段 × 8 + 后段），**只有位类型才写 3 段**：

| 数据类型 | 地址写法 |
|---|---|
| Bool / BoolArray | `DB1.0.1`、`M1.6`、`I1.6`、`Q1.6`（第 3 段 = 位号 0~7） |
| Byte / Short / Int / Float / String / 各 Array | `DB1.0`、`DB1.DBB100`、`DB1.DBW100`、`DB1.DBD100`、`M100`、`MW100` |

⚠️ **在 Byte/Short 上误写 3 段（如 `DB1.0.1`）会把位偏移带进地址，读到隔壁数据** —— 位号只属于位类型。

### 9.4 西门子 String 的 Length 语义（双模，由使用方决定）

| `Length` | S7 行为 | 适用场景 |
|---|---|---|
| `0` | **原生 S7 STRING**：HSL 读 2 字节 MAX/CUR 头 → 按 CUR 取 → **剥头** | PLC 侧声明为 `STRING[n]`（推荐） |
| `N`（>0） | 基类回落：读 **N 字节裸解码、不剥头** | 无头字符数组 `ARRAY OF CHAR` |

- **分流点在 HSL**（`SiemensS7Net.ReadString` 覆写：`len == 0` 走原生串，`len > 0` 回落基类）→ 与 PLC 侧约定不一致就会读到**头 2 字节垃圾**（MAX=254 经 ASCII 解码为 `?`）
- **同一 `IPlcTransport.ReadString(addr, len)` 入口在两协议下语义相反**：CIP 的 `len` 是元素个数，S7 靠 `len==0` 才剥头 → `length` 本就是协议相关参数，由使用方给定、协议实现解释
- ⚠️ **`Length=0` 仅对 S7 有定义**：CIP 的 String 仍按元素个数填，存量 CIP 地址表**勿改**（协议层已加护栏：`Length ≤ 0` 直接 `Fail`）
- **写为原样写（不填充不截断）**：基类写是 `PadRight(len).Substring(0, len)` —— ① 与 `STRING[n]` 定长冲突；② `Length=0` 时 `.Substring(0,0)` 会**静默写空串清掉 PLC 数据**
- ⚠️ **写入地址必须是 PLC 侧声明的 `STRING[n]` 地址**。误指向非 STRING 区时，HSL 在 `MAX==0` 会兜底 254 并写 256 字节，**可能覆盖 DB 相邻内存** —— 驱动无法校验，责任在地址表填写

### 9.5 兼容旧调用

| 旧 API | 新 API | 说明 |
|---|---|---|
| `PlcDeviceAdapter` | 不变 | 仅 using 调整 + Connect 删 EnableCacheMode |
| `ConfigManager.Instance` | 保留过渡 | 新代码走构造注入 |
| `IPlc.EnableReconnection/DisableReconnection` | 已删除 | 重连由 IoWorker 循环自动管理 |
| `IPlc.AddTagGroup` | 已删除 | 地址表集中归 ConfigManager；运行时增删走 `IPlcConfigManager` 调试接口 |
| `IPlc.IsCacheModeEnabled` | 已删除 | 无业务调用点 |
| `IPlc.EnableCacheMode/DisableCacheMode` | 已删除 | 缓存模式内聚连接生命周期 |
| `GetTagValue/SetTagValue`（静态遍历） | `GetValue<T>/SetValue` | 业务层显式指定组名+标签名 |
| `GetPlcTagGroups` | `GetTagTable`（返回实时引用） | 现取不缓存 |
| `GetOrLoadTagTables` | 已删除 | 配置加载阶段顺带加载 |
| `T ReadTag<T>`（异常式） | `ResponseResult<T> ReadTag<T>` | 失败返回 Fail 不抛 |
| `AddressHelper.TryParseOmronAddress` | 已删除 | 全库零调用点，地址格式由协议在 HSL 内消化 |
| HSL `OperateResult` | `ResponseResult` | HSL 类型仅在 `HslBaseTransport` 内 |
| `ITypedCache/TypedCache<T>`（集合型） | `ITypedValue/TypedValueHolder<T>`（单值容器） | 读路径强类型通道零装箱 |

## 10. 相关文档

- `Taoyl.Application.Business/README.md` —— 设备工厂与适配器（本库的唯一消费方）
- `Taoyl.Application.Device/README.md` —— `IDevice` 契约与设备运行时
- `Taoyl.Application.Communication/README.md` —— 并行的第二条通讯线（Modbus 主站，基于 NModbus）
- `Taoyl.UI.Pages/ViewModels/PlcDebugViewModel.*.cs` —— 调试界面（配置编辑 / 地址表 / 读写测试）
- `Taoyl.IFALaunch/Modules/DeviceModule.cs` —— 唯一的注册点
