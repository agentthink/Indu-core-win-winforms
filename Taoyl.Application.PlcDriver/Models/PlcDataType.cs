using System.ComponentModel;

namespace Taoyl.Application.PlcDriver.Models
{
    /// <summary>
    /// PLC数据类型枚举，包含数组类型（双维度模型：本枚举定"类型"，TagItem.Length 定"元素个数"）
    /// 命名约定：数组类型一律以 Array 结尾（ConfigManager.IsScalarType 依赖该约定判"Length 是否被忽略"）
    /// 显示名：数组类型用 [Description] 标注 "T[]" 短名（下拉/展示统一走 EnumExtensions.GetDescription()）；
    /// 标量不加特性——GetDescription() 无特性时回退枚举名，重复标注同名属冗余。
    /// ⚠️ 成员名同时是 Excel 地址表解析与 IsScalarType 的约定依赖，只可加特性、禁改名。
    /// 无符号/64 位四类（UShort/UInt/Long/ULong）于 2026-09-12 补入：
    /// 欧姆龙 CIP 侧已实证支持（OmronConnectedCipNet 覆写其数组读，单值读内部走覆写版），
    /// 对应 HSL 原语 ReadUInt16/ReadUInt32/ReadInt64/ReadUInt64 与 Write(ushort/uint/long/ulong)
    /// </summary>
    public enum PlcDataType
    {
        // 基础类型
        Byte,
        Bool,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        String,

        // 数组类型（用于批量策略路由；显示名 "T[]" ）
        [Description("Byte[]")]
        ByteArray,
        [Description("Bool[]")]
        BoolArray,
        [Description("Short[]")]
        ShortArray,
        [Description("UShort[]")]
        UShortArray,
        [Description("Int[]")]
        IntArray,
        [Description("UInt[]")]
        UIntArray,
        [Description("Long[]")]
        LongArray,
        [Description("ULong[]")]
        ULongArray,
        [Description("Float[]")]
        FloatArray,
        [Description("Double[]")]
        DoubleArray,
    }
}
