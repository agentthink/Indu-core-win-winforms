using HslCommunication;
using HslCommunication.Core.Device;
using Taoyl.Application.PlcDriver.Contracts;
using System;
using System.Text;
using System.Threading.Tasks;

namespace Taoyl.Application.PlcDriver.Transports
{
    /// <summary>
    /// HSL通讯库传输适配器基类（TCP / 串口共用）
    /// 职责：适配HSL库 DeviceCommunication 到 IPlcTransport 纯原语接口
    /// 边界：唯一 HSL 类型（OperateResult）→ ResponseResult 转换点，驱动库其余层零 HSL 泄漏
    /// 分工：读写转发与连接状态判定在此收口；连接动作是形态专属（TCP=ConnectServer/ConnectClose，
    ///       串口=Open/Close），由形态子类覆写
    /// 设计模式：适配器模式 + 模板方法（连接动作下沉）
    /// </summary>
    public abstract class HslBaseTransport : IPlcTransport
    {
        /// <summary>HSL设备对象（形态子类各自持有强类型引用，基类只做转发）</summary>
        protected readonly DeviceCommunication _device;

        /// <summary>默认接收超时（ms）：PlcConfig.ReceiveTimeOut 未配置时使用</summary>
        protected const int DefaultReceiveTimeOut = 10000;

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="device">HSL库设备对象（如 OmronConnectedCipNet / ModbusRtu）</param>
        protected HslBaseTransport(DeviceCommunication device)
        {
            _device = device ?? throw new ArgumentNullException(nameof(device));
        }
        #endregion

        #region 连接管理（形态专属，子类实现）
        /// <summary>连接PLC设备</summary>
        public abstract ResponseResult Connect();

        /// <summary>连接PLC设备（异步）</summary>
        public abstract Task<ResponseResult> ConnectAsync();

        /// <summary>断开PLC连接</summary>
        public abstract void Close();

        /// <summary>断开PLC连接（异步）</summary>
        public abstract Task CloseAsync();
        #endregion

        #region 连接状态判定
        /// <summary>
        /// 检查操作结果并判断连接状态（通用三分法）
        /// | 状态 | ErrorCode | 含义 |
        /// | 成功 | 0 | 连接正常 |
        /// | 传输失败 | &lt; 0 | Socket/连接/超时问题 |
        /// | 非负错误 | ≥ 0 | 设备或库返回的业务错误，不影响连接判定 |
        /// 形态子类覆写时**必须先调 base** 再补本形态特例：跳过 base 会丢失通用判定，
        /// 表现为读写失败被当成「连接正常」
        /// </summary>
        public virtual bool CheckConnectionStatus(ResponseResult result)
        {
            if (result == null)
                return false;

            if (result.IsSuccess)
                return true;

            // 负错误码 = 传输层失败（TCP socket 异常 / 串口收发失败均归此类）
            if (result.ErrorCode < 0)
                return false;

            return true;
        }
        #endregion

        #region 转换点（HSL OperateResult → ResponseResult）
        /// <summary>
        /// 转换非泛型操作结果（ErrorCode/Message 透传）
        /// </summary>
        protected ResponseResult ConvertResult(OperateResult result)
        {
            if (result == null)
                return ResponseResult.Fail("通讯结果为空");

            return result.IsSuccess
                ? ResponseResult.Success()
                : ResponseResult.Fail(result.Message, result.ErrorCode);
        }

        /// <summary>
        /// 转换泛型操作结果（ErrorCode/Message/Content 透传）
        /// </summary>
        protected ResponseResult<T> ConvertResult<T>(OperateResult<T> result)
        {
            if (result == null)
                return ResponseResult<T>.Fail("通讯结果为空");

            return result.IsSuccess
                ? ResponseResult<T>.Success(result.Content)
                : ResponseResult<T>.Fail(result.Message, result.ErrorCode);
        }
        #endregion

        #region 基础读写（字节级）
        public ResponseResult<byte[]> Read(string address, ushort length)
        {
            return ConvertResult(_device.Read(address, length));
        }

        public ResponseResult Write(string address, byte[] data)
        {
            return ConvertResult(_device.Write(address, data));
        }
        #endregion

        #region 类型化读取
        public ResponseResult<bool> ReadBool(string address)
        {
            return ConvertResult(_device.ReadBool(address));
        }

        public ResponseResult<bool[]> ReadBool(string address, ushort length)
        {
            return ConvertResult(_device.ReadBool(address, length));
        }

        public ResponseResult<short> ReadInt16(string address)
        {
            return ConvertResult(_device.ReadInt16(address));
        }

        public ResponseResult<short[]> ReadInt16(string address, ushort length)
        {
            return ConvertResult(_device.ReadInt16(address, length));
        }

        public ResponseResult<ushort> ReadUInt16(string address)
        {
            return ConvertResult(_device.ReadUInt16(address));
        }

        public ResponseResult<ushort[]> ReadUInt16(string address, ushort length)
        {
            return ConvertResult(_device.ReadUInt16(address, length));
        }

        public ResponseResult<int> ReadInt32(string address)
        {
            return ConvertResult(_device.ReadInt32(address));
        }

        public ResponseResult<int[]> ReadInt32(string address, ushort length)
        {
            return ConvertResult(_device.ReadInt32(address, length));
        }

        public ResponseResult<uint> ReadUInt32(string address)
        {
            return ConvertResult(_device.ReadUInt32(address));
        }

        public ResponseResult<uint[]> ReadUInt32(string address, ushort length)
        {
            return ConvertResult(_device.ReadUInt32(address, length));
        }

        public ResponseResult<long> ReadInt64(string address)
        {
            return ConvertResult(_device.ReadInt64(address));
        }

        public ResponseResult<long[]> ReadInt64(string address, ushort length)
        {
            return ConvertResult(_device.ReadInt64(address, length));
        }

        public ResponseResult<ulong> ReadUInt64(string address)
        {
            return ConvertResult(_device.ReadUInt64(address));
        }

        public ResponseResult<ulong[]> ReadUInt64(string address, ushort length)
        {
            return ConvertResult(_device.ReadUInt64(address, length));
        }

        public ResponseResult<float> ReadFloat(string address)
        {
            return ConvertResult(_device.ReadFloat(address));
        }

        public ResponseResult<float[]> ReadFloat(string address, ushort length)
        {
            return ConvertResult(_device.ReadFloat(address, length));
        }

        public ResponseResult<double> ReadDouble(string address)
        {
            return ConvertResult(_device.ReadDouble(address));
        }

        public ResponseResult<double[]> ReadDouble(string address, ushort length)
        {
            return ConvertResult(_device.ReadDouble(address, length));
        }

        public ResponseResult<string> ReadString(string address, ushort length)
        {
            return ConvertResult(_device.ReadString(address, length));
        }

        public ResponseResult<string> ReadString(string address, ushort length, Encoding encoding)
        {
            return ConvertResult(_device.ReadString(address, length, encoding));
        }
        #endregion

        #region 类型化写入
        public ResponseResult Write(string address, bool value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, bool[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, short value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, short[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, ushort value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, ushort[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, int value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, int[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, uint value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, uint[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, long value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, long[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, ulong value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, ulong[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, float value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, float[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, double value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, double[] values)
        {
            return ConvertResult(_device.Write(address, values));
        }

        public ResponseResult Write(string address, string value)
        {
            return ConvertResult(_device.Write(address, value));
        }

        public ResponseResult Write(string address, string value, Encoding encoding)
        {
            return ConvertResult(_device.Write(address, value, encoding));
        }
        #endregion
    }
}
