# Taoyl.Application.Communication —— Modbus 通讯库 README

> [Taoyl.IFA](https://gitee.com/taoyanl/Taoyl.IFA) · 产线设备上位机框架 · MIT

## 1. 定位与边界

**Modbus 主站的统一门面**：把 TCP / RTU 两种物理形态收敛成一个 `IReadWriteDevice`，向上提供"连接 + 字节序转换 + 强类型读写"，向下封装 NModbus。

**是什么**：连接状态机（5 态）、心跳与自动重连（单 Task 循环 + 指数退避 + 连接代次隔离）、60 余个强类型读写方法（Bool / Int16 / UInt16 / Int32 / Float / Double / String，含同步与异步）、字节序转换（4 种格式）、原始报文异步落盘日志。

**不是什么**：不认识 PLC 地址语义（`DB1.DBD100`、CIP tag 名这类解释在 `Taoyl.Application.PlcDriver`）、不管理设备实例与配置（在 `Taoyl.Application.Business` 的 Devices 子域）、不做协议之上的业务编排、不碰 WinForms。

## 2. 依赖说明

| 依赖 | 借用什么 |
|---|---|
| NModbus 3.0.83 | `ModbusFactory` / `IModbusMaster` / `IModbusSerialMaster` / `ModbusLogger` 抽象 |
| NModbus.Serial 3.0.83 | `factory.CreateRtuMaster(SerialPort)` 串口主站 |

被依赖：`Taoyl.Application.Business`（`Devices/Adapter/ModBusTcpDeviceAdapter`、`Devices/Factory/ModBusTcpDeviceFactory`）。

**纯类库，零项目引用** —— 不引 Share、不引任何同级模块，可独立复用。

## 3. 功能与 API

### 三个接口的分层

| 接口 | 职责 |
|---|---|
| `IDeviceConnection` | 连接生命周期：状态、事件、`Connect` / `ConnectAsync` / `Disconnect` |
| `IReadWriteOperations` | 数据读写：原生报文批量 + 强类型读写，全部同步/异步成对 |
| `IReadWriteDevice` | 上面两个的组合（`IReadWriteDevice : IDeviceConnection, IReadWriteOperations`），业务只认它 |

### `IDeviceConnection`（`IReadWriteDevice.cs`）

| 成员 | 签名 | 说明 |
|---|---|---|
| 状态 | `ConnectionState State { get; }` | 由内部状态机维护，外部只读 |
| 可读写 | `bool IsConnected { get; }` | 启用心跳时 = 物理连接 **且** 心跳未超时；未启用则只看物理连接 |
| 状态事件 | `event Action<ConnectionState> StateChanged` | **后台线程触发**，UI 订阅须 `Invoke` |
| 异常事件 | `event Action<Exception> CommunicationError` | 断线、心跳超时、重连失败 |
| 连接 | `OperateResult Connect()` / `Task<OperateResult> ConnectAsync()` | 幂等：已 Connected/Connecting 时直接返回成功 |
| 断开 | `OperateResult Disconnect()` | 停心跳与重连任务，释放端口/Socket |

### `IReadWriteOperations`（60 个方法，按能力域）

所有方法返回 `OperateResult` / `OperateResult<T>`，**失败不抛异常**；`timeout` 参数可选（缺省用 `ConnectionParams.Timeout`）。

| 能力域 | 同步 / 异步签名 |
|---|---|
| 原生批量读 | `OperateResult<ushort[]> ReadHoldingRegisters(ushort startAddress, ushort numberOfPoints, int? timeout = null)` / `ReadInputRegisters` / `ReadCoils` / `ReadDiscreteInputs` |
| 原生批量写 | `OperateResult WriteMultipleRegisters(ushort startAddress, ushort[] data, int? timeout = null)` / `WriteCoils(ushort, bool[])` |
| Bool | `OperateResult<bool> ReadCoil(ushort address, int? timeout = null)` / `WriteCoil(ushort, bool)` |
| Int16 / UInt16 | `OperateResult<short> ReadInt16(...)`、`OperateResult<short[]> ReadInt16Array(ushort address, ushort length, ...)`；`WriteInt16` / `WriteInt16Array` 及 UInt16 对应组 |
| Int32（2 寄存器） | `OperateResult<int> ReadInt32(...)`、`ReadInt32Array(..., ushort length, ...)`、`WriteInt32` / `WriteInt32Array` |
| Float（2 寄存器） | `OperateResult<float> ReadFloat(...)`、`ReadFloatArray(...)`、`WriteFloat` / `WriteFloatArray` |
| Double（4 寄存器） | `OperateResult<double> ReadDouble(...)`、`ReadDoubleArray(...)`、`WriteDouble` / `WriteDoubleArray` |
| String | `OperateResult<string> ReadString(ushort address, ushort length, int? timeout = null)`；`WriteString(address, value)` 自动算长度、`WriteString(address, value, ushort registerCount)` 固定缓冲区 |

每个方法都有 `XxxAsync` 版本，返回 `Task<OperateResult>` / `Task<OperateResult<T>>`。

### `ConnectionParams`（`ConnectionParams.cs`）

| 分组 | 成员 | 默认值 | 说明 |
|---|---|---|---|
| 从站 | `SlaveId` | `1` | Modbus 从站号 |
| 格式 | `DataFormat` | `ABCD` | 多字节字节序，见 `ModbusUtils` |
| 超时 | `Timeout` / `ConnectTimeout` | `1000` / `3000` | 单次读写 / 建连 |
| 重连 | `AutoReconnect` | `true` | 断线自动重连 |
| | `MaxReconnectRetries` | `0` | 0 = 无限重试直到成功 |
| | `ReconnectDelayMs` / `MaxReconnectDelayMs` | `1000` / `30000` | 指数退避的起点与上限 |
| 重试 | `MaxRetries` / `ReadRetryIntervalMs` | `0` / `200` | 单次读写失败重试次数与间隔（与重连退避无关） |
| 节流 | `DelayBetweenMessages` | `0` | 两条命令最小间隔，慢速串口设备给喘息时间 |
| 心跳 | `EnableHeartbeat` | `false` | 关时只认物理连接 |
| | `HeartbeatIntervalMs` / `HeartbeatTimeoutMs` | `5000` / `15000` | 检测周期 / 判死阈值 |
| | `HeartbeatAddress` / `HeartbeatLength` | `0` / `1` | 伪心跳读取的寄存器 |
| 文本 | `TextEncoding` | `ASCII` | 字符串编解码 |
| 日志 | `LogPathTemplate` | `null` | 报文日志路径模板（`string.Format` 占位符），空 = 不记录 |

子类：`TcpConnectionParams`（`Ip` / `Port`=502）、`RtuConnectionParams`（`PortName` / `BaudRate`=9600 / `DataBits`=8 / `Parity` / `StopBits`）。

### `OperateResult` / `OperateResult<T>`

| 成员 | 说明 |
|---|---|
| `bool IsSuccess` / `string ErrorMessage` | 结果与错误描述（成功时 `ErrorMessage == null`） |
| `T Data` | 仅 `OperateResult<T>` 有，成功时有效 |
| `static Success()` / `static Fail(string)` | 工厂方法（泛型版为 `Success(T)` / `new Fail(string)`） |

### `ConnectionState` / `DataFormat`

| 枚举 | 成员 |
|---|---|
| `ConnectionState` | `Disconnected` / `Connecting` / `Connected` / `Reconnecting` / `Closing` |
| `DataFormat` | `ABCD`（大端，Modbus 默认）/ `BADC`（字内字节交换）/ `CDAB`（字序交换）/ `DCBA`（全小端） |

## 4. 快速开始

```csharp
using Taoyl.Application.Communication;

var master = new ModbusTcpMaster(new TcpConnectionParams
{
    Ip = "192.168.1.10",
    Port = 502,
    SlaveId = 1,
    Timeout = 1000,
    ConnectTimeout = 3000,
    AutoReconnect = true,
    EnableHeartbeat = true,
    HeartbeatAddress = 0,
    HeartbeatIntervalMs = 5000,
    HeartbeatTimeoutMs = 15000,
    LogPathTemplate = "Logs/Modbus/dev_{0:yyyyMMdd}.txt",   // 相对路径按程序目录展开
});

var conn = master.Connect();
if (!conn.IsSuccess)
{
    Console.WriteLine($"连接失败: {conn.ErrorMessage}");
    return;
}

var value = master.ReadInt16(100);
if (value.IsSuccess)
    Console.WriteLine($"D100 = {value.Data}");
else
    Console.WriteLine($"读取失败: {value.ErrorMessage}");

master.Dispose();
```

## 5. 使用示例

### 5.1 TCP 与 RTU 的差异只在参数

```csharp
// TCP
IReadWriteDevice tcp = new ModbusTcpMaster(new TcpConnectionParams { Ip = "127.0.0.1", Port = 502 });

// RTU（串口）
IReadWriteDevice rtu = new ModbusRtuMaster(new RtuConnectionParams
{
    PortName = "COM3", BaudRate = 9600, DataBits = 8,
    Parity = Parity.None, StopBits = StopBits.One,
    SlaveId = 1,
});
```

两者之后的所有调用完全一致 —— 差异已被基类收敛。

### 5.2 字节序：读出来不对先查 `DataFormat`

```csharp
// 同一个 32 位浮点，四种字节序结果不同
var p = new TcpConnectionParams { DataFormat = DataFormat.CDAB };   // 字序交换，常见于部分电表/温控
var master = new ModbusTcpMaster(p);

var temp = master.ReadFloat(200);      // 占 2 个寄存器（200、201）
```

`Double` **只支持 `ABCD` / `DCBA`** —— `BADC` / `CDAB` 对 4 寄存器组合没有明确行业约定，代码里没实现。

### 5.3 写字符串：两种签名别混

```csharp
// A. 自动算长度：按内容补 '\0'，寄存器数向上取整
master.WriteString(300, "ABC");

// B. 固定缓冲区：PLC 侧预留 N 个寄存器时用，不足补 0，避免残留旧数据
master.WriteString(300, "ABC", registerCount: 10);
```

PLC 侧是定长 `STRING[n]` 时**必须用 B**，否则残留旧字符会拼在新内容后面。

### 5.4 断线与心跳

```csharp
master.StateChanged += s => Console.WriteLine($"状态: {s}");
master.CommunicationError += ex => Console.WriteLine($"通讯异常: {ex.Message}");

// 启用心跳后：读不到 HeartbeatAddress 且超过 HeartbeatTimeoutMs → 判死 → Reconnecting → 指数退避重连
// 关闭心跳：只认 tcpClient.Connected / serialPort.IsOpen，网络半开（对端掉电但网线通）发现不了
```

`IsConnected` 与 `State` 的区别：`IsConnected` 是"现在能不能读写"（心跳参与判断），`State` 是状态机当前档位。

### 5.5 报文日志

```csharp
new TcpConnectionParams
{
    LogPathTemplate = "Logs/Modbus/dev_{0:yyyyMMdd HH}.txt",   // 按小时一个文件
}
```

- 相对路径按**程序目录**展开，绝对路径原样使用
- 异步队列写盘，写失败静默（不干扰主流程）
- 关闭：`LogPathTemplate` 留空

## 6. 类库结构

| 文件 | 内容 |
|---|---|
| `IReadWriteDevice.cs` | 三个接口：`IDeviceConnection` / `IReadWriteOperations` / `IReadWriteDevice` |
| `ConnectionParams.cs` | `ConnectionState` / `DataFormat` 枚举 + `ConnectionParams` / `TcpConnectionParams` / `RtuConnectionParams` |
| `OperateResult.cs` | `OperateResult` 与泛型 `OperateResult<T>` |
| `ModbusMasterBase.cs` | 抽象基类（1382 行）：状态机 / 心跳重连 / 信号量 / 重试 / 60 个强类型读写 / 自定义报文 |
| `ModbusTcpMaster.cs` | TCP 适配：`TcpClient` 建连 + `factory.CreateMaster` |
| `ModbusRtuMaster.cs` | RTU 适配：`SerialPort` 建连 + `factory.CreateRtuMaster` |
| `ModbusUtils.cs` | 字节序转换（`ConvertToFloat` / `ConvertFromInt32` / `ConvertToString` 等 14 个静态方法） |
| `FileModbusLogger.cs` | 报文日志：`BlockingCollection` 队列 + 单 Task 写盘，实现 NModbus `ModbusLogger` |
| `Pipes/IPipe.cs` | 通信管道契约：连接 + `SendAndReceive(byte[], timeout)` |
| `Pipes/PipeTcpNet.cs` | TCP 管道实现 |
| `Pipes/PipeSerialPort.cs` | 串口管道实现 |

⚠️ `Pipes/` 三个文件目前**零消费者**（全仓只有定义处互相引用）。`Taoyl.Application.PlcDriver` 里出现的 `PipeTcpNet` 是 `HslCommunication.Core.Pipe.PipeTcpNet`，同名不同类。这层是为"裸报文收发"预留的抽象，尚未接入任何调用链。

## 7. 内部逻辑

### 连接与状态流转

```
Connect() / ConnectAsync()
  └─ semaphore.Wait（全类唯一并发闸门）
       ├─ State 已是 Connected/Connecting → 直接返回成功（幂等）
       ├─ UpdateState(Connecting)
       ├─ DisconnectCore()          ← 先清旧连接，避免残留
       ├─ ConnectCore() / ConnectCoreAsync()   ← 子类实现物理连接
       │    失败 → UpdateState(Disconnected) → 返回 Fail
       └─ 成功 → connectionGeneration++        ← 新代次
                 lastHeartbeatAckUtc = now
                 currentBackoffMs = ReconnectDelayMs
                 reconnectAttemptCount = 0
                 UpdateState(Connected)
                 StartHeartbeatLoop()
```

### 心跳与重连（单 Task）

```
HeartbeatLoopAsync(generation, token)
  while (!token.IsCancellationRequested)
    ├─ generation != 当前代次 → return（旧连接的循环自杀）
    ├─ State == Connected    → TickHeartbeatAsync（读 HeartbeatAddress）
    │                           └─ 超时 → State = Reconnecting → 立即 TickReconnectAsync
    ├─ State == Reconnecting → TickReconnectAsync（指数退避 + 抖动，重新 Connect）
    └─ 其他状态（Disconnected/Connecting/Closing）→ return
```

### 关键设计决策

| 决策 | 理由 |
|---|---|
| 单 Task 同时管心跳与重连 | 两个独立 Task 会各自判断状态机，出现"心跳在重连时还在读"的竞态；单循环按状态分派，天然互斥 |
| 连接代次（`connectionGeneration`） | 旧连接断线时抛的迟到异常不能影响新连接；代次一变，旧循环立刻退出、旧异常丢弃 |
| 心跳占信号量但用 500ms 超时 | 心跳与业务读写不能同时打同一条链路；拿不到锁就跳过本轮，不排队堵死业务 |
| 指数退避 + 随机抖动 | 多设备同时掉线时固定间隔会让重连风暴打满网络；抖动把重连点摊开 |
| `MaxReconnectRetries = 0` 表示无限 | 产线设备掉线是常态，默认应该一直试；要有限次由调用方显式配 |
| 重试与重连分两套参数 | `MaxRetries` 是"这一次读写再试几次"，`MaxReconnectRetries` 是"连接断了重连几次"，语义完全不同 |
| 所有方法返回 `OperateResult` 不抛 | 通讯失败是业务常态，异常会让调用方每处都包 try/catch；结果对象让失败成为数据 |
| `DataFormat` 枚举而非 bool 数组 | 字节序是四种行业约定，用枚举表达比 `(bool swapWord, bool swapByte)` 更不易错 |
| `DelayBetweenMessages` 默认 0 | 只有慢速串口设备需要，默认关掉避免给正常设备加延迟 |
| 报文日志异步队列 | 报文频率高（几十毫秒一条），同步写盘会拖慢通讯；队列 + 单写线程隔离 IO |

## 8. 注意事项与 FAQ

🔴 **`StateChanged` / `CommunicationError` 在后台线程触发**，UI 订阅必须 `BeginInvoke` 切回主线程，直接改控件会抛跨线程异常。

🔴 **`Disconnect()` 之后禁止再读写**。重连任务已停，读写只会返回失败；重新用要再 `Connect()`。

🔴 **心跳读的是同一个信号量**：`HeartbeatIntervalMs` 配得比单次读写超时还小，会让业务读写频繁拿不到锁。经验值：心跳间隔 ≥ 3 × `Timeout`。

🔴 **`DataFormat` 影响所有多字节类型**。改它等于改 Float / Int32 / Double / String 的全部解释方式；不要为了一个点位改全局参数。

⚠️ **`Double` 只支持 `ABCD` / `DCBA`**。选 `BADC` / `CDAB` 时 `ReadDouble` 的结果无意义（4 寄存器没有通行的字内+字序组合定义）。

⚠️ **`WriteString(address, value)` 与 `WriteString(address, value, registerCount)` 语义不同**：前者按内容长度算，后者按固定长度补 0。PLC 侧定长缓冲区必须用后者。

⚠️ **`ModbusRtuMaster` 当前零外部调用**。仓内只有 `ModBusTcpDeviceAdapter` 用 TCP；RTU 已实现并通过参数验证，但没有设备适配器接入。

⚠️ **`Pipes/` 未接入**。见 §6。

⚠️ **`OperateResult<T>.Data` 在失败时是 `default(T)`**，不是 null 判断能覆盖的（值类型）。先看 `IsSuccess` 再取值。

**Q：为什么 60 个方法不抽成泛型 `Read<T>`？**
Modbus 的寄存器宽度由类型决定（Int32 占 2 个、Double 占 4 个），泛型方法里要靠反射或字典映射回寄存器数，等于把编译期确定的事推到运行期。显式方法签名让"读几个寄存器"在调用处一眼可见。

**Q：能不能直接用 NModbus？**
可以，但会失去：状态机与自动重连、连接代次隔离、统一字节序、`OperateResult` 契约、报文日志。这些是本库唯一的价值。

**Q：通讯一直失败怎么排查？**
① 看 `CommunicationError` 的错误文本；② 打开 `LogPathTemplate` 看原始报文；③ 用 HSL Demo 或 Modbus Poll 直连设备验证链路；④ 检查 `SlaveId`、`DataFormat`、寄存器地址基址（0-based / 1-based）。

## 9. 扩展指南

**A. 新增一种传输形态（如 Modbus ASCII / UDP）**

1. 继承 `ConnectionParams` 加该形态参数（参考 `TcpConnectionParams`）
2. 继承 `ModbusMasterBase`，实现 4 个抽象成员：`IsConnectedCore` / `NativeMaster` / `ConnectCore` / `ConnectCoreAsync` / `DisconnectCore`
3. 读写逻辑、状态机、心跳、重试**一行都不用写** —— 全在基类

**B. 接入一个使用 RTU 的设备**

1. 在 `Taoyl.Application.Business/Devices` 参照 `ModBusTcpDeviceAdapter` 写 RTU 适配器
2. 工厂类 `ModBusTcpDeviceFactory` 旁边加 RTU 工厂并注册到 `DeviceModule`
3. `RtuConnectionParams` 从设备配置映射（`Devices.json` 里串口参数在 `ProtocolParams`）

**C. 接入 `Pipes` 层（若确实需要裸报文）**

1. 调用方持有 `IPipe`，用 `SendAndReceive(byte[], timeout)` 拿原始响应
2. 注意 `IPipe` 的 `Connect`/`Disconnect` 与 `ModbusMasterBase` 的状态机**没有联动**，两套连接生命期要自己管
3. 若只是想做协议扩展，走 A 更省事

## 10. 相关文档

- `Taoyl.Application.Business/README.md` —— 设备适配器与工厂（本库的唯一消费方）
- `Taoyl.Application.PlcDriver/README.md` —— PLC 协议栈（与 Modbus 并列的另一条通讯线，基于 HSL 而非 NModbus）
- `Taoyl.IFALaunch/Modules/DeviceModule.cs` —— 设备工厂与适配器注册
- `Taoyl.UI.Pages/ViewModels/PlcDebugViewModel.Io.cs` —— 通讯调试页的读写调用形态
