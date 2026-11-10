using System.Text;
using System.Threading.Tasks;

namespace Taoyl.Application.PlcDriver.Contracts
{
    /// <summary>
    /// PLC传输层接口（纯原语）
    /// 职责：抽象底层通讯库的类型化读写原语，不含协议语义（策略字典在协议层）
    /// 边界：无策略字典、无 IsConnected（连接状态由 PlcRuntime 维护）、无编码参数（编码决策在协议层策略字典）
    /// 公共 API 零 HSL 类型，结果统一为 ResponseResult
    /// </summary>
    public interface IPlcTransport
    {
        #region 连接管理
        /// <summary>
        /// 连接PLC设备（一次干净尝试，不含重连状态机）
        /// </summary>
        ResponseResult Connect();

        /// <summary>
        /// 连接PLC设备（异步版，与 Connect 语义一致）
        /// </summary>
        Task<ResponseResult> ConnectAsync();

        /// <summary>
        /// 断开PLC连接
        /// </summary>
        void Close();

        /// <summary>
        /// 断开PLC连接（异步版，与 Close 语义一致：忽略关闭结果与异常）
        /// </summary>
        Task CloseAsync();

        /// <summary>
        /// 检查操作结果并判断连接状态（纯函数，只看 result，不依赖内部状态）
        /// </summary>
        /// <param name="result">操作结果</param>
        /// <returns>true=连接正常，false=连接异常</returns>
        bool CheckConnectionStatus(ResponseResult result);
        #endregion

        #region 基础读写（字节级）
        /// <summary>
        /// 从指定地址读取字节数据
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="length">
        /// 读取长度 = 协议的传输单元个数（即数组元素个数，非字节数）：
        /// 欧姆龙 CIP 下即 tag 的元素个数（length=1 时返回该 tag 的全部字节）；
        /// 返回数组的实际字节数 = 元素个数 × 元素字节数，由底层协议决定
        /// </param>
        /// <returns>操作结果（Content = 读取到的字节数组）</returns>
        ResponseResult<byte[]> Read(string address, ushort length);

        /// <summary>
        /// 向指定地址写入字节数据（写入字节数 = data.Length，与寻址单元无关）
        /// </summary>
        /// <param name="address">起始地址</param>
        /// <param name="data">待写入的字节数据</param>
        ResponseResult Write(string address, byte[] data);
        #endregion

        #region 类型化读取
        /// <summary>
        /// 读取单个布尔值（单值，区别于数组重载）
        /// </summary>
        ResponseResult<bool> ReadBool(string address);

        /// <summary>
        /// 读取布尔值数组
        /// </summary>
        ResponseResult<bool[]> ReadBool(string address, ushort length);

        /// <summary>
        /// 读取16位整数
        /// </summary>
        ResponseResult<short> ReadInt16(string address);

        /// <summary>
        /// 读取16位整数数组
        /// </summary>
        ResponseResult<short[]> ReadInt16(string address, ushort length);

        /// <summary>
        /// 读取16位无符号整数
        /// </summary>
        ResponseResult<ushort> ReadUInt16(string address);

        /// <summary>
        /// 读取16位无符号整数数组
        /// </summary>
        ResponseResult<ushort[]> ReadUInt16(string address, ushort length);

        /// <summary>
        /// 读取32位整数
        /// </summary>
        ResponseResult<int> ReadInt32(string address);

        /// <summary>
        /// 读取32位整数数组
        /// </summary>
        ResponseResult<int[]> ReadInt32(string address, ushort length);

        /// <summary>
        /// 读取32位无符号整数
        /// </summary>
        ResponseResult<uint> ReadUInt32(string address);

        /// <summary>
        /// 读取32位无符号整数数组
        /// </summary>
        ResponseResult<uint[]> ReadUInt32(string address, ushort length);

        /// <summary>
        /// 读取64位整数
        /// </summary>
        ResponseResult<long> ReadInt64(string address);

        /// <summary>
        /// 读取64位整数数组
        /// </summary>
        ResponseResult<long[]> ReadInt64(string address, ushort length);

        /// <summary>
        /// 读取64位无符号整数
        /// </summary>
        ResponseResult<ulong> ReadUInt64(string address);

        /// <summary>
        /// 读取64位无符号整数数组
        /// </summary>
        ResponseResult<ulong[]> ReadUInt64(string address, ushort length);

        /// <summary>
        /// 读取单精度浮点数
        /// </summary>
        ResponseResult<float> ReadFloat(string address);

        /// <summary>
        /// 读取单精度浮点数数组
        /// </summary>
        ResponseResult<float[]> ReadFloat(string address, ushort length);

        /// <summary>
        /// 读取双精度浮点数
        /// </summary>
        ResponseResult<double> ReadDouble(string address);

        /// <summary>
        /// 读取双精度浮点数数组
        /// </summary>
        ResponseResult<double[]> ReadDouble(string address, ushort length);

        /// <summary>
        /// 读取字符串（默认编码）
        /// </summary>
        ResponseResult<string> ReadString(string address, ushort length);

        /// <summary>
        /// 使用指定编码读取字符串
        /// </summary>
        ResponseResult<string> ReadString(string address, ushort length, Encoding encoding);
        #endregion

        #region 类型化写入
        /// <summary>
        /// 写入布尔值
        /// </summary>
        ResponseResult Write(string address, bool value);

        /// <summary>
        /// 写入布尔值数组
        /// </summary>
        ResponseResult Write(string address, bool[] values);

        /// <summary>
        /// 写入16位整数
        /// </summary>
        ResponseResult Write(string address, short value);

        /// <summary>
        /// 写入16位整数数组
        /// </summary>
        ResponseResult Write(string address, short[] values);

        /// <summary>
        /// 写入16位无符号整数
        /// </summary>
        ResponseResult Write(string address, ushort value);

        /// <summary>
        /// 写入16位无符号整数数组
        /// </summary>
        ResponseResult Write(string address, ushort[] values);

        /// <summary>
        /// 写入32位整数
        /// </summary>
        ResponseResult Write(string address, int value);

        /// <summary>
        /// 写入32位整数数组
        /// </summary>
        ResponseResult Write(string address, int[] values);

        /// <summary>
        /// 写入32位无符号整数
        /// </summary>
        ResponseResult Write(string address, uint value);

        /// <summary>
        /// 写入32位无符号整数数组
        /// </summary>
        ResponseResult Write(string address, uint[] values);

        /// <summary>
        /// 写入64位整数
        /// </summary>
        ResponseResult Write(string address, long value);

        /// <summary>
        /// 写入64位整数数组
        /// </summary>
        ResponseResult Write(string address, long[] values);

        /// <summary>
        /// 写入64位无符号整数
        /// </summary>
        ResponseResult Write(string address, ulong value);

        /// <summary>
        /// 写入64位无符号整数数组
        /// </summary>
        ResponseResult Write(string address, ulong[] values);

        /// <summary>
        /// 写入单精度浮点数
        /// </summary>
        ResponseResult Write(string address, float value);

        /// <summary>
        /// 写入单精度浮点数数组
        /// </summary>
        ResponseResult Write(string address, float[] values);

        /// <summary>
        /// 写入双精度浮点数
        /// </summary>
        ResponseResult Write(string address, double value);

        /// <summary>
        /// 写入双精度浮点数数组
        /// </summary>
        ResponseResult Write(string address, double[] values);

        /// <summary>
        /// 写入字符串（默认编码）
        /// </summary>
        ResponseResult Write(string address, string value);

        /// <summary>
        /// 使用指定编码写入字符串
        /// </summary>
        ResponseResult Write(string address, string value, Encoding encoding);
        #endregion
    }
}
