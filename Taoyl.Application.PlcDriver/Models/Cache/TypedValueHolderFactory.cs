using System;
using System.Collections.Generic;

namespace Taoyl.Application.PlcDriver.Models.Cache
{
    /// <summary>
    /// 强类型值容器工厂（按 DataType 创建对应泛型实例，配置加载期一次）
    /// 类型映射字典 + MakeGenericType 反射创建——加载期一次性低频调用，反射开销可忽略；
    /// 新增数据类型只需在映射表补一行（与旧 TypedCacheFactory 同款形态）
    /// </summary>
    public static class TypedValueHolderFactory
    {
        // DataType → CLR 类型映射（与 PlcDataType 一一对应）
        private static readonly Dictionary<PlcDataType, Type> _typeMapping = new Dictionary<PlcDataType, Type>
        {
            [PlcDataType.Byte] = typeof(byte),
            [PlcDataType.Bool] = typeof(bool),
            [PlcDataType.Short] = typeof(short),
            [PlcDataType.UShort] = typeof(ushort),
            [PlcDataType.Int] = typeof(int),
            [PlcDataType.UInt] = typeof(uint),
            [PlcDataType.Long] = typeof(long),
            [PlcDataType.ULong] = typeof(ulong),
            [PlcDataType.Float] = typeof(float),
            [PlcDataType.Double] = typeof(double),
            [PlcDataType.String] = typeof(string),
            [PlcDataType.ByteArray] = typeof(byte[]),
            [PlcDataType.BoolArray] = typeof(bool[]),
            [PlcDataType.ShortArray] = typeof(short[]),
            [PlcDataType.UShortArray] = typeof(ushort[]),
            [PlcDataType.IntArray] = typeof(int[]),
            [PlcDataType.UIntArray] = typeof(uint[]),
            [PlcDataType.LongArray] = typeof(long[]),
            [PlcDataType.ULongArray] = typeof(ulong[]),
            [PlcDataType.FloatArray] = typeof(float[]),
            [PlcDataType.DoubleArray] = typeof(double[]),
        };

        /// <summary>
        /// 按数据类型创建值容器（T 与 DataType 一一对应，创建后定型终生不变）
        /// </summary>
        /// <param name="dataType">PLC数据类型</param>
        /// <returns>值容器实例</returns>
        /// <exception cref="NotSupportedException">不支持的数据类型</exception>
        public static ITypedValue Create(PlcDataType dataType)
        {
            if (!_typeMapping.TryGetValue(dataType, out var type))
                throw new NotSupportedException($"不支持的数据类型: {dataType}");

            var holderType = typeof(TypedValueHolder<>).MakeGenericType(type);
            return (ITypedValue)Activator.CreateInstance(holderType, dataType);
        }
    }
}
