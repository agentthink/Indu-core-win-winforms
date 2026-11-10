using System.Text;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Cache;
using System;
using System.Collections.Generic;

namespace Taoyl.Application.PlcDriver.Protocols
{
    /// <summary>
    /// 基础协议访问（模板方法，原 BasePlcStrategyFactory）
    /// 职责：初始化通用读写策略字典（协议层：怎么解释数据类型）
    /// 连接状态检查已迁入传输层（HslBaseTransport.CheckConnectionStatus）
    /// 批量读取已搁置（逻辑存档 2026-08-29 日志第九轮），全库优化完成后按存档实现
    /// 注：原抽象方法 ParseAddress（解析 FINS 风格地址 E0.1/D100）已于 2026-09-12 删除——
    /// 全库零调用点，且其地址模型与 CIP 的 tag 名模型不对应，属预抽象死代码
    /// </summary>
    public abstract class BaseProtocolAccess : IPlcProtocolAccess
    {
        #region 保护字段
        protected readonly IPlcTransport _communication;

        /// <summary>读取策略字典（返回ResponseResult；委托签名：地址,长度,值容器输出槽；读成功值强类型写入容器零装箱）</summary>
        protected readonly Dictionary<PlcDataType, Func<string, int, ITypedValue, ResponseResult>> _readStrategies;

        /// <summary>写入策略字典（返回ResponseResult；委托签名：地址,值,长度；写路径低频保留 object 传值）</summary>
        protected readonly Dictionary<PlcDataType, Func<string, object, int, ResponseResult>> _writeStrategies;
        #endregion

        #region IPlcProtocolAccess 实现
        public IPlcTransport Communication => _communication;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="communication">通讯传输层</param>
        protected BaseProtocolAccess(IPlcTransport communication)
        {
            _communication = communication ?? throw new ArgumentNullException(nameof(communication));

            // 初始化策略字典
            _readStrategies = new Dictionary<PlcDataType, Func<string, int, ITypedValue, ResponseResult>>();
            _writeStrategies = new Dictionary<PlcDataType, Func<string, object, int, ResponseResult>>();

            // 初始化策略
            InitializeStrategies();
        }
        #endregion

        #region 策略初始化（模板方法）
        /// <summary>
        /// 初始化策略（模板方法，子类可覆写单类策略）
        /// </summary>
        protected virtual void InitializeStrategies()
        {
            InitializeReadStrategies();
            InitializeWriteStrategies();
        }

        /// <summary>
        /// 初始化读取策略（子类可重写）
        /// 统一收口：每个策略 = 「transport 原语 → ApplyRead 投递值容器」；
        /// 容器类型不匹配时报错而非静默跳过（见 ApplyRead）
        /// </summary>
        protected virtual void InitializeReadStrategies()
        {
            // 基础类型读取策略（Bool 用单值 ReadBool，返回 ResponseResult<bool>，与 DataType.Bool 单值语义一致）
            _readStrategies[PlcDataType.Bool] = (addr, len, holder) => ApplyRead(_communication.ReadBool(addr), holder);

            // Byte（统一"字节语义"：读返回数组首位；写显式包单字节数组）
            // 与 HSL 官方 ReadByte/Write(byte) 同构；CIP 是 tag 模型，length 即元素个数，无需协议层特化
            _readStrategies[PlcDataType.Byte] = (addr, len, holder) =>
            {
                var r = _communication.Read(addr, 1);
                // 防御：仅成功且确实取到数据时才投递（失败/空内容不触碰容器，防越界）
                if (!r.IsSuccess || r.Content == null || r.Content.Length == 0)
                    return r;
                return ApplyRead(ResponseResult<byte>.Success(r.Content[0]), holder);
            };

            _readStrategies[PlcDataType.Short] = (addr, len, holder) => ApplyRead(_communication.ReadInt16(addr), holder);
            _readStrategies[PlcDataType.UShort] = (addr, len, holder) => ApplyRead(_communication.ReadUInt16(addr), holder);
            _readStrategies[PlcDataType.Int] = (addr, len, holder) => ApplyRead(_communication.ReadInt32(addr), holder);
            _readStrategies[PlcDataType.UInt] = (addr, len, holder) => ApplyRead(_communication.ReadUInt32(addr), holder);
            _readStrategies[PlcDataType.Long] = (addr, len, holder) => ApplyRead(_communication.ReadInt64(addr), holder);
            _readStrategies[PlcDataType.ULong] = (addr, len, holder) => ApplyRead(_communication.ReadUInt64(addr), holder);
            _readStrategies[PlcDataType.Float] = (addr, len, holder) => ApplyRead(_communication.ReadFloat(addr), holder);
            _readStrategies[PlcDataType.Double] = (addr, len, holder) => ApplyRead(_communication.ReadDouble(addr), holder);
            _readStrategies[PlcDataType.String] = (addr, len, holder) => ApplyRead(_communication.ReadString(addr, (ushort)len), holder);

            // 数组类型读取策略
            // ByteArray（统一"字节语义"：Length 即元素个数，原样透传传输层，读回不截断）
            // 与 ShortArray/FloatArray 同构；CIP 语义一致，无协议层换算
            _readStrategies[PlcDataType.ByteArray] = (addr, len, holder) => ApplyRead(_communication.Read(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.BoolArray] = (addr, len, holder) => ApplyRead(_communication.ReadBool(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.ShortArray] = (addr, len, holder) => ApplyRead(_communication.ReadInt16(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.UShortArray] = (addr, len, holder) => ApplyRead(_communication.ReadUInt16(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.IntArray] = (addr, len, holder) => ApplyRead(_communication.ReadInt32(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.UIntArray] = (addr, len, holder) => ApplyRead(_communication.ReadUInt32(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.LongArray] = (addr, len, holder) => ApplyRead(_communication.ReadInt64(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.ULongArray] = (addr, len, holder) => ApplyRead(_communication.ReadUInt64(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.FloatArray] = (addr, len, holder) => ApplyRead(_communication.ReadFloat(addr, (ushort)len), holder);
            _readStrategies[PlcDataType.DoubleArray] = (addr, len, holder) => ApplyRead(_communication.ReadDouble(addr, (ushort)len), holder);
        }

        /// <summary>
        /// 读原语结果 → 强类型写入值容器（泛型闭包内调用，零装箱）
        /// 容器 null（raw 模式无标签）仅返回结果不写值；
        /// 容器泛型与策略返回类型不匹配时**返回 Fail**，不再静默跳过
        /// </summary>
        /// <typeparam name="T">值类型（与策略对应 DataType 一致）</typeparam>
        /// <param name="result">transport 原语返回的泛型结果</param>
        /// <param name="holder">值容器输出槽（null=raw 模式无标签）</param>
        protected ResponseResult ApplyRead<T>(ResponseResult<T> result, ITypedValue holder)
        {
            if (result == null)
                return ResponseResult.Fail("通讯结果为空");

            // 原语失败：原样上报（含 ErrorCode/Message），不触碰容器
            if (!result.IsSuccess)
                return result;

            // raw 模式：无标签容器，只返结果
            if (holder == null)
                return result;

            if (holder is TypedValueHolder<T> typedHolder)
            {
                typedHolder.Set(result.Content);
                return result;
            }

            // 容器泛型 ≠ 策略返回类型 → 值无法投递，必须报错。
            // 旧实现静默跳过 → 返回 Success 但业务侧值永不更新（最隐蔽的一类现场故障）。
            // 成因：运行期直接改了 TagItem.DataType，而容器定型于 DataType（见 TagItem.EnsureTypedValue）。
            // ErrorCode 用默认 10000（**不可用负数**：CheckConnectionStatus 会把负数判为断线，误触发重连）
            return ResponseResult.Fail(
                $"值容器类型不匹配: 容器[{holder.DataType}] ≠ 读取类型[{typeof(T).Name}]，值未投递");
        }

        /// <summary>
        /// 初始化写入策略（子类可重写）
        /// </summary>
        protected virtual void InitializeWriteStrategies()
        {
            // Byte：必须显式包成单字节数组走 Write(string,byte[])——
            // 若直接传 Convert.ToByte(val)，IPlcTransport 无 Write(string,byte) 重载，会隐式提升解析到 Write(string,short)，写整 16 位字（清掉同字高字节）
            _writeStrategies[PlcDataType.Byte] = (addr, val, len) => _communication.Write(addr, new[] { Convert.ToByte(val) });
            _writeStrategies[PlcDataType.Bool] = (addr, val, len) => _communication.Write(addr, (bool)val);
            _writeStrategies[PlcDataType.Short] = (addr, val, len) => _communication.Write(addr, Convert.ToInt16(val));
            _writeStrategies[PlcDataType.UShort] = (addr, val, len) => _communication.Write(addr, Convert.ToUInt16(val));
            _writeStrategies[PlcDataType.Int] = (addr, val, len) => _communication.Write(addr, Convert.ToInt32(val));
            _writeStrategies[PlcDataType.UInt] = (addr, val, len) => _communication.Write(addr, Convert.ToUInt32(val));
            _writeStrategies[PlcDataType.Long] = (addr, val, len) => _communication.Write(addr, Convert.ToInt64(val));
            _writeStrategies[PlcDataType.ULong] = (addr, val, len) => _communication.Write(addr, Convert.ToUInt64(val));
            _writeStrategies[PlcDataType.Float] = (addr, val, len) => _communication.Write(addr, Convert.ToSingle(val));
            _writeStrategies[PlcDataType.Double] = (addr, val, len) => _communication.Write(addr, Convert.ToDouble(val));
            // String 写 = ASCII 原样全写（不按 Length 填充/截断）：定长语义由 PLC 侧声明（如 STRING[n]）决定，上位机不参与填充；
            // 旧定长模板 PadRight(len).Substring(0,len) 已删（实装 4 协议全部覆写为原样写，模板零消费者——
            // 且其按「字符」填充与多字节编码下 PLC 侧「字节长度」不符，len=1 时 "ABC" 只写 "A" 属静默截断丢数据）；
            // 不引入 HSL 的按字节重载 Write(address, value, int length, Encoding)：内部走 Write(address, byte[])，CIP 上命中 209/BYTE 覆写（写坏）；
            // 后续若出现「按长度写」语义的协议，在子类覆写本策略
            _writeStrategies[PlcDataType.String] = (addr, val, len) =>
            {
                string str = val?.ToString() ?? string.Empty;
                return _communication.Write(addr, str, Encoding.ASCII);
            };
            _writeStrategies[PlcDataType.ByteArray] = (addr, val, len) => _communication.Write(addr, (byte[])val);
            _writeStrategies[PlcDataType.BoolArray] = (addr, val, len) => _communication.Write(addr, (bool[])val);
            _writeStrategies[PlcDataType.ShortArray] = (addr, val, len) => _communication.Write(addr, (short[])val);
            _writeStrategies[PlcDataType.UShortArray] = (addr, val, len) => _communication.Write(addr, (ushort[])val);
            _writeStrategies[PlcDataType.IntArray] = (addr, val, len) => _communication.Write(addr, (int[])val);
            _writeStrategies[PlcDataType.UIntArray] = (addr, val, len) => _communication.Write(addr, (uint[])val);
            _writeStrategies[PlcDataType.LongArray] = (addr, val, len) => _communication.Write(addr, (long[])val);
            _writeStrategies[PlcDataType.ULongArray] = (addr, val, len) => _communication.Write(addr, (ulong[])val);
            _writeStrategies[PlcDataType.FloatArray] = (addr, val, len) => _communication.Write(addr, (float[])val);
            _writeStrategies[PlcDataType.DoubleArray] = (addr, val, len) => _communication.Write(addr, (double[])val);
        }
        #endregion

        #region IPlcProtocolAccess 接口实现
        /// <summary>
        /// 尝试获取读取策略（未注册的数据类型返回 false，不抛 KeyNotFoundException；
        /// 仅按需查询，不向外暴露内部字典引用——IReadOnlyDictionary 仍是假封装，向下转型照样能改）
        /// </summary>
        public bool TryGetReadStrategy(PlcDataType dataType, out Func<string, int, ITypedValue, ResponseResult> strategy)
        {
            return _readStrategies.TryGetValue(dataType, out strategy);
        }

        /// <summary>
        /// 尝试获取写入策略（未注册的数据类型返回 false，不抛 KeyNotFoundException）
        /// </summary>
        public bool TryGetWriteStrategy(PlcDataType dataType, out Func<string, object, int, ResponseResult> strategy)
        {
            return _writeStrategies.TryGetValue(dataType, out strategy);
        }
        #endregion
    }
}
