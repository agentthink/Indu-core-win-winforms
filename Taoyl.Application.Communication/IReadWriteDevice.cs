using System;
using System.Threading.Tasks;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// 设备连接生命周期管理接口。
    /// 负责连接状态机、心跳检测、自动重连等底层通讯生命周期。
    /// </summary>
    public interface IDeviceConnection : IDisposable
    {
        #region 状态与事件

        /// <summary>当前连接状态（由内部状态机维护）</summary>
        ConnectionState State { get; }

        /// <summary>
        /// 当前是否处于可读写状态。
        /// 若启用心跳，此属性结合物理连接与心跳应答综合判断；
        /// 若未启用心跳，仅反映物理连接状态。
        /// </summary>
        bool IsConnected { get; }

        /// <summary>连接状态变更事件（可能在后台线程触发，UI 订阅需 Invoke/BeginInvoke）</summary>
        event Action<ConnectionState> StateChanged;

        /// <summary>通讯异常事件（连接断开、心跳超时、重连失败等）</summary>
        event Action<Exception> CommunicationError;

        #endregion

        #region 连接管理

        /// <summary>同步建立连接</summary>
        OperateResult Connect();

        /// <summary>异步建立连接（真异步路径）</summary>
        Task<OperateResult> ConnectAsync();

        /// <summary>断开连接并释放网络/串口资源，停止心跳与重连任务</summary>
        OperateResult Disconnect();

        #endregion
    }

    /// <summary>
    /// Modbus 数据读写操作接口。
    /// 隐藏 TCP/RTU 差异，所有方法返回 OperateResult 不抛出异常。
    /// </summary>
    public interface IReadWriteOperations
    {
        #region 原生报文批量读写

        /// <summary>同步读取保持寄存器</summary>
        OperateResult<ushort[]> ReadHoldingRegisters(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>异步读取保持寄存器</summary>
        Task<OperateResult<ushort[]>> ReadHoldingRegistersAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>同步读取输入寄存器</summary>
        OperateResult<ushort[]> ReadInputRegisters(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>异步读取输入寄存器</summary>
        Task<OperateResult<ushort[]>> ReadInputRegistersAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>同步读取线圈</summary>
        OperateResult<bool[]> ReadCoils(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>异步读取线圈</summary>
        Task<OperateResult<bool[]>> ReadCoilsAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>同步读取离散输入</summary>
        OperateResult<bool[]> ReadDiscreteInputs(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>异步读取离散输入</summary>
        Task<OperateResult<bool[]>> ReadDiscreteInputsAsync(ushort startAddress, ushort numberOfPoints, int? timeout = null);

        /// <summary>同步写入多个保持寄存器</summary>
        OperateResult WriteMultipleRegisters(ushort startAddress, ushort[] data, int? timeout = null);

        /// <summary>异步写入多个保持寄存器</summary>
        Task<OperateResult> WriteMultipleRegistersAsync(ushort startAddress, ushort[] data, int? timeout = null);

        /// <summary>同步写入多个线圈</summary>
        OperateResult WriteCoils(ushort startAddress, bool[] data, int? timeout = null);

        /// <summary>异步写入多个线圈</summary>
        Task<OperateResult> WriteCoilsAsync(ushort startAddress, bool[] data, int? timeout = null);

        #endregion

        #region 强类型读取 — Bool

        /// <summary>同步读取单个线圈</summary>
        OperateResult<bool> ReadCoil(ushort address, int? timeout = null);

        /// <summary>异步读取单个线圈</summary>
        Task<OperateResult<bool>> ReadCoilAsync(ushort address, int? timeout = null);

        #endregion

        #region 强类型读取 — Int16

        /// <summary>同步读取单个 Int16（1 个寄存器）</summary>
        OperateResult<short> ReadInt16(ushort address, int? timeout = null);

        /// <summary>异步读取单个 Int16</summary>
        Task<OperateResult<short>> ReadInt16Async(ushort address, int? timeout = null);

        /// <summary>同步读取连续多个 Int16</summary>
        OperateResult<short[]> ReadInt16Array(ushort address, ushort length, int? timeout = null);

        /// <summary>异步读取连续多个 Int16</summary>
        Task<OperateResult<short[]>> ReadInt16ArrayAsync(ushort address, ushort length, int? timeout = null);

        #endregion

        #region 强类型读取 — UInt16

        /// <summary>同步读取单个 UInt16（1 个寄存器）</summary>
        OperateResult<ushort> ReadUInt16(ushort address, int? timeout = null);

        /// <summary>异步读取单个 UInt16</summary>
        Task<OperateResult<ushort>> ReadUInt16Async(ushort address, int? timeout = null);

        /// <summary>同步读取连续多个 UInt16</summary>
        OperateResult<ushort[]> ReadUInt16Array(ushort address, ushort length, int? timeout = null);

        /// <summary>异步读取连续多个 UInt16</summary>
        Task<OperateResult<ushort[]>> ReadUInt16ArrayAsync(ushort address, ushort length, int? timeout = null);

        #endregion

        #region 强类型读取 — Int32

        /// <summary>同步读取单个 Int32（自动读 2 个寄存器）</summary>
        OperateResult<int> ReadInt32(ushort address, int? timeout = null);

        /// <summary>异步读取单个 Int32</summary>
        Task<OperateResult<int>> ReadInt32Async(ushort address, int? timeout = null);

        /// <summary>同步读取连续多个 Int32（每 2 个寄存器一个值）</summary>
        OperateResult<int[]> ReadInt32Array(ushort address, ushort length, int? timeout = null);

        /// <summary>异步读取连续多个 Int32</summary>
        Task<OperateResult<int[]>> ReadInt32ArrayAsync(ushort address, ushort length, int? timeout = null);

        #endregion

        #region 强类型读取 — Float

        /// <summary>同步读取单个 Float（自动读 2 个寄存器）</summary>
        OperateResult<float> ReadFloat(ushort address, int? timeout = null);

        /// <summary>异步读取单个 Float</summary>
        Task<OperateResult<float>> ReadFloatAsync(ushort address, int? timeout = null);

        /// <summary>同步读取连续多个 Float</summary>
        OperateResult<float[]> ReadFloatArray(ushort address, ushort length, int? timeout = null);

        /// <summary>异步读取连续多个 Float</summary>
        Task<OperateResult<float[]>> ReadFloatArrayAsync(ushort address, ushort length, int? timeout = null);

        #endregion

        #region 强类型读取 — Double

        /// <summary>同步读取单个 Double（自动读 4 个寄存器）</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        OperateResult<double> ReadDouble(ushort address, int? timeout = null);

        /// <summary>异步读取单个 Double</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        Task<OperateResult<double>> ReadDoubleAsync(ushort address, int? timeout = null);

        /// <summary>同步读取连续多个 Double（每 4 个寄存器一个值）</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        OperateResult<double[]> ReadDoubleArray(ushort address, ushort length, int? timeout = null);

        /// <summary>异步读取连续多个 Double</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        Task<OperateResult<double[]>> ReadDoubleArrayAsync(ushort address, ushort length, int? timeout = null);

        #endregion

        #region 强类型读取 — String

        /// <summary>同步读取字符串（ASCII 编码，length 为寄存器个数）</summary>
        OperateResult<string> ReadString(ushort address, ushort length, int? timeout = null);

        /// <summary>异步读取字符串</summary>
        Task<OperateResult<string>> ReadStringAsync(ushort address, ushort length, int? timeout = null);

        #endregion

        #region 强类型写入 — Bool

        /// <summary>同步写入单个线圈</summary>
        OperateResult WriteCoil(ushort address, bool value, int? timeout = null);

        /// <summary>异步写入单个线圈</summary>
        Task<OperateResult> WriteCoilAsync(ushort address, bool value, int? timeout = null);

        #endregion

        #region 强类型写入 — Int16

        /// <summary>同步写入单个 Int16</summary>
        OperateResult WriteInt16(ushort address, short value, int? timeout = null);

        /// <summary>异步写入单个 Int16</summary>
        Task<OperateResult> WriteInt16Async(ushort address, short value, int? timeout = null);

        /// <summary>同步写入连续多个 Int16</summary>
        OperateResult WriteInt16Array(ushort address, short[] values, int? timeout = null);

        /// <summary>异步写入连续多个 Int16</summary>
        Task<OperateResult> WriteInt16ArrayAsync(ushort address, short[] values, int? timeout = null);

        #endregion

        #region 强类型写入 — UInt16

        /// <summary>同步写入单个 UInt16</summary>
        OperateResult WriteUInt16(ushort address, ushort value, int? timeout = null);

        /// <summary>异步写入单个 UInt16</summary>
        Task<OperateResult> WriteUInt16Async(ushort address, ushort value, int? timeout = null);

        /// <summary>同步写入连续多个 UInt16</summary>
        OperateResult WriteUInt16Array(ushort address, ushort[] values, int? timeout = null);

        /// <summary>异步写入连续多个 UInt16</summary>
        Task<OperateResult> WriteUInt16ArrayAsync(ushort address, ushort[] values, int? timeout = null);

        #endregion

        #region 强类型写入 — Int32

        /// <summary>同步写入单个 Int32（自动拆为 2 个寄存器）</summary>
        OperateResult WriteInt32(ushort address, int value, int? timeout = null);

        /// <summary>异步写入单个 Int32</summary>
        Task<OperateResult> WriteInt32Async(ushort address, int value, int? timeout = null);

        /// <summary>同步写入连续多个 Int32</summary>
        OperateResult WriteInt32Array(ushort address, int[] values, int? timeout = null);

        /// <summary>异步写入连续多个 Int32</summary>
        Task<OperateResult> WriteInt32ArrayAsync(ushort address, int[] values, int? timeout = null);

        #endregion

        #region 强类型写入 — Float

        /// <summary>同步写入单个 Float（自动拆为 2 个寄存器）</summary>
        OperateResult WriteFloat(ushort address, float value, int? timeout = null);

        /// <summary>异步写入单个 Float</summary>
        Task<OperateResult> WriteFloatAsync(ushort address, float value, int? timeout = null);

        /// <summary>同步写入连续多个 Float</summary>
        OperateResult WriteFloatArray(ushort address, float[] values, int? timeout = null);

        /// <summary>异步写入连续多个 Float</summary>
        Task<OperateResult> WriteFloatArrayAsync(ushort address, float[] values, int? timeout = null);

        #endregion

        #region 强类型写入 — Double

        /// <summary>同步写入单个 Double（自动拆为 4 个寄存器）</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        OperateResult WriteDouble(ushort address, double value, int? timeout = null);

        /// <summary>异步写入单个 Double</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        Task<OperateResult> WriteDoubleAsync(ushort address, double value, int? timeout = null);

        /// <summary>同步写入连续多个 Double</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        OperateResult WriteDoubleArray(ushort address, double[] values, int? timeout = null);

        /// <summary>异步写入连续多个 Double</summary>
        /// <remarks>double类型解析只支持字节序ABCD/DCBA</remarks>
        Task<OperateResult> WriteDoubleArrayAsync(ushort address, double[] values, int? timeout = null);

        #endregion

        #region 强类型写入 — String

        /// <summary>
        /// 同步写入字符串（ASCII 编码，按内容自动计算寄存器数）。
        /// 末尾自动补 '\0'，寄存器数向上取整。
        /// </summary>
        OperateResult WriteString(ushort address, string value, int? timeout = null);

        /// <summary>异步写入字符串（自动计算长度）</summary>
        Task<OperateResult> WriteStringAsync(ushort address, string value, int? timeout = null);

        /// <summary>
        /// 同步写入字符串（指定固定寄存器数）。
        /// 适用于 PLC 预留固定缓冲区的场景，不足部分补 0，避免残留旧数据。
        /// </summary>
        /// <param name="address">起始寄存器地址</param>
        /// <param name="value">字符串内容</param>
        /// <param name="registerCount">固定寄存器个数（0 表示自动计算）</param>
        OperateResult WriteString(ushort address, string value, ushort registerCount, int? timeout = null);

        /// <summary>异步写入字符串（指定固定寄存器数）</summary>
        Task<OperateResult> WriteStringAsync(ushort address, string value, ushort registerCount, int? timeout = null);

        #endregion
    }


    /// <summary>
    /// 统一的 Modbus 主站接口，隐藏 TCP/RTU 差异。
    /// 组合连接生命周期管理与数据读写操作。
    /// </summary>
    public interface IReadWriteDevice : IDeviceConnection, IReadWriteOperations
    {
    }
}