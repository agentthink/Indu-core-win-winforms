using System;
using System.Globalization;
using System.Linq;
using Taoyl.Application.PlcDriver.Contracts;

namespace Taoyl.Application.PlcDriver.Models
{
    /// <summary>
    /// PLC文本值解析器（调试界面写入通道的文本 → 强类型值）
    /// 与 ConfigManager.FormatValueForExport（值 → 文本）对称：导出的文本可直接回填解析写入。
    /// 规则：
    /// - Bool 收 1/0/true/false（忽略大小写）
    /// - 数组类型收逗号分隔文本（"1,2,3"，元素去空白；空串元素 = 跳过不补位）
    /// - ByteArray 分隔符支持空格/逗号（"0x01 0x02" 或 "1,2"），元素支持十进制或 0x 十六进制前缀
    /// - String 收原文本（去首尾空白）
    /// - 其余标量走各自 Parse（InvariantCulture）
    /// 失败语义：一律返回 Fail（携带具体原因），不抛异常
    /// </summary>
    public static class PlcTextValue
    {
        /// <summary>
        /// 解析文本为指定数据类型的强类型值（object 装箱视图；写请求 IoRequest.Value 直接消费）
        /// </summary>
        /// <param name="dataType">PLC数据类型</param>
        /// <param name="text">输入文本</param>
        /// <returns>操作结果（Success 携带强类型值 / Fail 携带解析原因）</returns>
        public static ResponseResult<object> Parse(PlcDataType dataType, string text)
        {
            if (text == null)
                return ResponseResult<object>.Fail("写入值为空");

            string t = text.Trim();
            if (t.Length == 0)
                return ResponseResult<object>.Fail("写入值为空");

            try
            {
                object value;
                switch (dataType)
                {
                    case PlcDataType.Byte:
                        value = byte.Parse(t.Replace("0x", ""), System.Globalization.NumberStyles.HexNumber);
                        break;
                    case PlcDataType.Bool:
                        value = ParseBool(t);
                        break;
                    case PlcDataType.Short:
                        value = short.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.UShort:
                        value = ushort.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.Int:
                        value = int.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.UInt:
                        value = uint.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.Long:
                        value = long.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.ULong:
                        value = ulong.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.Float:
                        value = float.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.Double:
                        value = double.Parse(t, CultureInfo.InvariantCulture);
                        break;
                    case PlcDataType.String:
                        value = t;
                        break;
                    case PlcDataType.ByteArray:
                        value = SplitByteElements(t).Select(ParseByteElement).ToArray();
                        break;
                    case PlcDataType.BoolArray:
                        value = SplitElements(t).Select(ParseBool).ToArray();
                        break;
                    case PlcDataType.ShortArray:
                        value = SplitElements(t).Select(s => short.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.UShortArray:
                        value = SplitElements(t).Select(s => ushort.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.IntArray:
                        value = SplitElements(t).Select(s => int.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.UIntArray:
                        value = SplitElements(t).Select(s => uint.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.LongArray:
                        value = SplitElements(t).Select(s => long.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.ULongArray:
                        value = SplitElements(t).Select(s => ulong.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.FloatArray:
                        value = SplitElements(t).Select(s => float.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    case PlcDataType.DoubleArray:
                        value = SplitElements(t).Select(s => double.Parse(s, CultureInfo.InvariantCulture)).ToArray();
                        break;
                    default:
                        return ResponseResult<object>.Fail($"不支持的数据类型: {dataType}");
                }
                return ResponseResult<object>.Success(value);
            }
            catch (Exception ex)
            {
                return ResponseResult<object>.Fail($"解析[{dataType}]失败: {ex.Message}");
            }
        }

        /// <summary>布尔文本解析：true/false（忽略大小写）或 1/0；其余抛 FormatException</summary>
        private static bool ParseBool(string s)
        {
            string v = s.Trim();
            if (bool.TryParse(v, out bool b)) return b;
            if (v == "1") return true;
            if (v == "0") return false;
            throw new FormatException($"无法解析布尔值: {s}");
        }

        /// <summary>逗号分隔为元素文本（逐元素去首尾空白；空元素保留空串，交由目标类型 Parse 报错）</summary>
        private static string[] SplitElements(string t)
        {
            return t.Split(',').Select(s => s.Trim()).ToArray();
        }

        /// <summary>
        /// 字节数组专用分隔：空格 / 逗号 / 制表符 / 换行均可（如 "0x01 0x02"、"1,2"、"0x0A, 0x0B"）
        /// 空元素直接丢弃（RemoveEmptyEntries）——字节数组常由报文或表格粘贴而来，宽容分隔比"空元素报错"更实用
        /// </summary>
        private static string[] SplitByteElements(string t)
        {
            return t.Split(new[] { ',', ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>字节元素解析：支持十进制（"12"/"255"）与 0x 十六进制前缀（"0x0C"/"0xFF"，忽略大小写）；越界抛 OverflowException 由外层转 Fail</summary>
        private static byte ParseByteElement(string s)
        {
            string v = s.Trim();
            if (v.Length > 2 && v.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return Convert.ToByte(v.Substring(2), 16);
            }
            return byte.Parse(v, CultureInfo.InvariantCulture);
        }
    }
}
