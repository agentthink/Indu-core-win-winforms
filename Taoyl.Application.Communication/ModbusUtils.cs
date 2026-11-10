using System;
using System.Text;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// Modbus 数据转换工具类，支持四种字节序及 4/8 字节数据。
    /// <para>设计原则：DataFormat 命名与设备手册完全一致。</para>
    /// <para>实现原理：Modbus 协议规定寄存器传输时高字节在前（Big-Endian within register）。</para>
    /// <para>本类将"协议拆字"与"设备字节序重排"分离，逻辑清晰且读写对称。</para>
    /// </summary>
    public static class ModbusUtils
    {
        #region 读取：寄存器 → 数值

        /// <summary>
        /// 将两个连续寄存器转换为 float。
        /// </summary>
        /// <param name="registers">寄存器数组</param>
        /// <param name="startIndex">起始索引（从该位置取 2 个寄存器）</param>
        /// <param name="format">设备字节序，与设备手册命名一致</param>
        /// <returns>转换后的 float 值</returns>
        public static float ConvertToFloat(ushort[] registers, int startIndex, DataFormat format)
        {
            byte[] bytes = RegisterToBytes(registers, startIndex, 2, format);
            return BitConverter.ToSingle(bytes, 0);
        }

        /// <summary>
        /// 将两个连续寄存器转换为 int。
        /// </summary>
        public static int ConvertToInt32(ushort[] registers, int startIndex, DataFormat format)
        {
            byte[] bytes = RegisterToBytes(registers, startIndex, 2, format);
            return BitConverter.ToInt32(bytes, 0);
        }

        /// <summary>
        /// 将单个寄存器转换为 short（16 位有符号）。
        /// <para>ABCD / CDAB：寄存器内高字节在前，直接强转。</para>
        /// <para>BADC / DCBA：寄存器内低字节在前，需先交换字节再强转。</para>
        /// </summary>
        public static short ConvertToInt16(ushort register, DataFormat format)
        {
            // ABCD / CDAB：寄存器内高字节在前，ushort 的内存布局本身就是正确的
            if (format == DataFormat.ABCD || format == DataFormat.CDAB)
                return (short)register;

            // BADC / DCBA：寄存器内低字节在前，需要交换高低字节
            // 例如寄存器值 0x3412 实际表示 0x1234
            return (short)SwapBytes(register);
        }

        /// <summary>
        /// 将单个寄存器转换为 ushort（16 位无符号）。
        /// <para>ABCD / CDAB：寄存器内高字节在前，直接透传。</para>
        /// <para>BADC / DCBA：寄存器内低字节在前，需先交换字节。</para>
        /// </summary>
        public static ushort ConvertToUInt16(ushort register, DataFormat format)
        {
            if (format == DataFormat.ABCD || format == DataFormat.CDAB)
                return register;
            return SwapBytes(register);
        }

        /// <summary>
        /// 将四个连续寄存器转换为 double。
        /// </summary>
        /// <param name="registers">寄存器数组</param>
        /// <param name="startIndex">起始索引（从该位置取 4 个寄存器）</param>
        /// <param name="format">设备字节序</param>
        /// <returns>转换后的 double 值</returns>
        public static double ConvertToDouble(ushort[] registers, int startIndex, DataFormat format)
        {
            byte[] bytes = RegisterToBytes(registers, startIndex, 4, format);
            return BitConverter.ToDouble(bytes, 0);
        }

        /// <summary>
        /// 将寄存器数组转换为字符串（ASCII 编码）。
        /// <para>注：字符串统一按高字节在前拆字，与 Modbus 协议传输顺序一致。</para>
        /// <para>多数设备对字符串直接按字节流解释，不做字节序转换，因此不受 format 影响。</para>
        /// </summary>
        /// <param name="registers">寄存器数组</param>
        /// <param name="startIndex">起始索引</param>
        /// <param name="registerCount">寄存器个数</param>
        /// <param name="format">设备字节序（保留参数，当前实现统一按大端处理）</param>
        public static string ConvertToString(ushort[] registers, int startIndex, int registerCount, DataFormat format,Encoding encoding=null)
        {
            encoding = encoding ?? Encoding.ASCII;
            byte[] bytes = new byte[registerCount * 2];

            // 统一按高字节在前拆字，与 Modbus 协议传输顺序一致
            // 不跟随数值型的字节序重排，因为字符串直接按字节流解释
            for (int i = 0; i < registerCount; i++)
            {
                bytes[i * 2] = HighByte(registers[startIndex + i]);
                bytes[i * 2 + 1] = LowByte(registers[startIndex + i]);
            }

            // 去除末尾 '\0' 终止符
            int length = bytes.Length;
            while (length > 0 && bytes[length - 1] == 0) length--;
            return encoding.GetString(bytes, 0, length);
        }

        /// <summary>
        /// 将寄存器数组转换为字符串（默认按 ABCD 大端解析）。
        /// </summary>
        public static string ConvertToString(ushort[] registers, int startIndex, int registerCount)
            => ConvertToString(registers, startIndex, registerCount, DataFormat.ABCD);

        #endregion

        #region 写入：数值 → 寄存器

        /// <summary>
        /// 将 float 转换为两个寄存器。
        /// </summary>
        public static ushort[] ConvertFromFloat(float value, DataFormat format)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BytesToRegisters(bytes, format);
        }

        /// <summary>
        /// 将 int 转换为两个寄存器。
        /// </summary>
        public static ushort[] ConvertFromInt32(int value, DataFormat format)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BytesToRegisters(bytes, format);
        }

        /// <summary>
        /// 将 short 转换为单个寄存器。
        /// </summary>
        public static ushort ConvertFromInt16(short value, DataFormat format)
        {
            ushort raw = (ushort)value;

            // ABCD / CDAB：高字节在前，直接存储
            if (format == DataFormat.ABCD || format == DataFormat.CDAB)
                return raw;

            // BADC / DCBA：低字节在前，交换后存储
            return SwapBytes(raw);
        }

        /// <summary>
        /// 将 ushort 转换为单个寄存器。
        /// </summary>
        public static ushort ConvertFromUInt16(ushort value, DataFormat format)
        {
            // ABCD / CDAB：高字节在前，直接存储
            if (format == DataFormat.ABCD || format == DataFormat.CDAB)
                return value;

            // BADC / DCBA：低字节在前，交换后存储
            return SwapBytes(value);
        }

        /// <summary>
        /// 将 double 转换为四个寄存器。
        /// </summary>
        public static ushort[] ConvertFromDouble(double value, DataFormat format)
        {
            byte[] bytes = BitConverter.GetBytes(value);
            return BytesToRegisters(bytes, format);
        }

        /// <summary>
        /// 将字符串转换为寄存器数组（ASCII 编码）。
        /// <para>注：字符串统一按高字节在前组字，与 Modbus 协议传输顺序一致。</para>
        /// <para>多数设备对字符串直接按字节流解释，不做字节序转换，因此不受 format 影响。</para>
        /// </summary>
        /// <param name="value">字符串</param>
        /// <param name="registerCount">目标寄存器个数（不足补 '\0'）</param>
        /// <param name="format">设备字节序（保留参数，当前实现统一按大端处理）</param>
        public static ushort[] ConvertFromString(string value, int registerCount, DataFormat format,Encoding encoding=null)
        {
            encoding = encoding ?? Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(value ?? string.Empty);
            byte[] padded = new byte[registerCount * 2];
            Array.Copy(bytes, padded, Math.Min(bytes.Length, padded.Length));

            ushort[] regs = new ushort[registerCount];

            // 统一按高字节在前组字，与 Modbus 协议传输顺序一致
            // 不跟随数值型的字节序重排，因为字符串直接按字节流解释
            for (int i = 0; i < registerCount; i++)
            {
                regs[i] = MakeUshort(padded[i * 2], padded[i * 2 + 1]);
            }

            return regs;
        }

        /// <summary>
        /// 将字符串转换为寄存器数组（默认按 ABCD 大端编码）。
        /// </summary>
        public static ushort[] ConvertFromString(string value, int registerCount)
            => ConvertFromString(value, registerCount, DataFormat.ABCD);

        #endregion

        #region 核心私有方法

        /// <summary>
        /// 将寄存器数组还原为字节流，并按设备字节序重排为 BitConverter 可识别的小端格式。
        /// <para>读取路径：registers → 协议拆字 → ApplyFormat → BitConverter</para>
        /// </summary>
        private static byte[] RegisterToBytes(ushort[] registers, int startIndex, int registerCount, DataFormat format)
        {
            // Step 1：按 Modbus 协议拆字。每个寄存器固定高字节在前。
            // 例如 0x42F6 → bytes[0]=0x42, bytes[1]=0xF6
            byte[] bytes = new byte[registerCount * 2];
            for (int i = 0; i < registerCount; i++)
            {
                bytes[i * 2] = HighByte(registers[startIndex + i]);
                bytes[i * 2 + 1] = LowByte(registers[startIndex + i]);
            }

            // Step 2：按设备字节序重排，使结果符合 BitConverter 的小端预期
            return ApplyFormat(bytes, format);
        }

        /// <summary>
        /// 将 BitConverter 产出的小端字节流按设备字节序重排，并组装为寄存器数组。
        /// <para>写入路径：BitConverter → ApplyFormat → 协议组字 → registers</para>
        /// </summary>
        private static ushort[] BytesToRegisters(byte[] bytes, DataFormat format)
        {
            // Step 1：按设备字节序重排（ApplyFormat 是自逆的，读写复用）
            bytes = ApplyFormat(bytes, format);

            // Step 2：按 Modbus 协议组字。每两个字节组成一个寄存器，高字节在前。
            int regCount = (bytes.Length + 1) / 2;
            ushort[] regs = new ushort[regCount];
            for (int i = 0; i < regCount; i++)
            {
                int hi = i * 2;      // 高字节索引
                int lo = hi + 1;     // 低字节索引
                regs[i] = MakeUshort(bytes[hi], lo < bytes.Length ? bytes[lo] : (byte)0);
            }
            return regs;
        }

        /// <summary>
        /// 字节序重排核心。按数据长度分发到 4 字节或 8 字节处理逻辑。
        /// 四种变换均为自逆操作，读写复用同一个实现。
        /// <para>读取：将设备字节流转换为 BitConverter 小端字节流。</para>
        /// <para>写入：将 BitConverter 小端字节流转换为设备字节流。</para>
        /// </summary>
        /// <param name="bytes">输入的字节数组（4 字节或 8 字节）</param>
        /// <param name="format">设备字节序</param>
        private static byte[] ApplyFormat(byte[] bytes, DataFormat format)
        {
            if (bytes.Length == 4)
                return ApplyFormat4(bytes, format);
            if (bytes.Length == 8)
                return ApplyFormat8(bytes, format);
            throw new ArgumentException("字节序转换目前仅支持 4 字节或 8 字节数据。");
        }

        /// <summary>
        /// 4 字节字节序重排（float / int32）。
        /// </summary>
        private static byte[] ApplyFormat4(byte[] bytes, DataFormat format)
        {
            byte[] result = new byte[4];

            // 约定：bytes[0..3] 是 RegisterToBytes 按"高字节在前"拆出的原始字节流
            // 各设备格式下，原始字节流的内容如下：
            // ABCD  → [B3, B2, B1, B0]  （标准大端）
            // BADC  → [B2, B3, B0, B1]  （字节交换）
            // CDAB  → [B1, B0, B3, B2]  （字交换）
            // DCBA  → [B0, B1, B2, B3]  （完全小端）
            // 目标：统一转换为 [B0, B1, B2, B3] 供 BitConverter 解析

            switch (format)
            {
                case DataFormat.ABCD:
                    // 原始 [B3,B2,B1,B0] → 完全反转 → [B0,B1,B2,B3]
                    result[0] = bytes[3];
                    result[1] = bytes[2];
                    result[2] = bytes[1];
                    result[3] = bytes[0];
                    break;

                case DataFormat.BADC:
                    // 原始 [B2,B3,B0,B1] → 字交换 → [B0,B1,B2,B3]
                    result[0] = bytes[2];
                    result[1] = bytes[3];
                    result[2] = bytes[0];
                    result[3] = bytes[1];
                    break;

                case DataFormat.CDAB:
                    // 原始 [B1,B0,B3,B2] → 字节交换 → [B0,B1,B2,B3]
                    result[0] = bytes[1];
                    result[1] = bytes[0];
                    result[2] = bytes[3];
                    result[3] = bytes[2];
                    break;

                case DataFormat.DCBA:
                    // 原始 [B0,B1,B2,B3] → 已是小端，直接透传
                    result[0] = bytes[0];
                    result[1] = bytes[1];
                    result[2] = bytes[2];
                    result[3] = bytes[3];
                    break;
            }

            return result;
        }

        /// <summary>
        /// 8 字节字节序重排（double）。
        /// 将 8 字节看作两个 4 字节字块处理，复用 ApplyFormat4 逻辑。
        /// </summary>
        private static byte[] ApplyFormat8(byte[] bytes, DataFormat format)
        {
            byte[] result = new byte[8];

            switch (format)
            {
                case DataFormat.ABCD:
                    // 完全反转
                    for (int i = 0; i < 8; i++) result[i] = bytes[7 - i];
                    break;

                case DataFormat.DCBA:
                    // 完全小端，直接透传
                    Array.Copy(bytes, result, 8);
                    break;

                case DataFormat.BADC:
                    // 每个 4 字节字内做 BADC（字节交换）
                    {
                        byte[] hi = ApplyFormat4(new[] { bytes[0], bytes[1], bytes[2], bytes[3] }, DataFormat.BADC);
                        byte[] lo = ApplyFormat4(new[] { bytes[4], bytes[5], bytes[6], bytes[7] }, DataFormat.BADC);
                        Array.Copy(hi, 0, result, 0, 4);
                        Array.Copy(lo, 0, result, 4, 4);
                    }
                    break;

                case DataFormat.CDAB:
                    // 字交换：两个 4 字节字互换位置
                    {
                        byte[] hi = ApplyFormat4(new[] { bytes[0], bytes[1], bytes[2], bytes[3] }, DataFormat.ABCD);
                        byte[] lo = ApplyFormat4(new[] { bytes[4], bytes[5], bytes[6], bytes[7] }, DataFormat.ABCD);
                        Array.Copy(lo, 0, result, 0, 4); // 低位字放前面
                        Array.Copy(hi, 0, result, 4, 4); // 高位字放后面
                    }
                    break;
            }

            return result;
        }

        #endregion

        #region 位操作辅助方法（提高可读性，消除魔法数字）

        /// <summary>
        /// 提取 16 位值的高字节（位 15..8）。
        /// <para>原理：右移 8 位，丢弃低字节，保留高字节。</para>
        /// <para>示例：0x1234 → 0x12</para>
        /// </summary>
        private static byte HighByte(ushort value) => (byte)(value >> 8);

        /// <summary>
        /// 提取 16 位值的低字节（位 7..0）。
        /// <para>原理：与 0xFF（0000 0000 1111 1111）按位与，清零高字节，保留低字节。</para>
        /// <para>示例：0x1234 → 0x34</para>
        /// </summary>
        private static byte LowByte(ushort value) => (byte)(value & 0xFF);

        /// <summary>
        /// 将两个字节组合为一个 ushort，高字节在前。
        /// <para>原理：高字节左移 8 位到高位，再和低字节按位或拼接。</para>
        /// <para>示例：0x12, 0x34 → 0x1234</para>
        /// </summary>
        private static ushort MakeUshort(byte high, byte low) => (ushort)((high << 8) | low);

        /// <summary>
        /// 交换 16 位值的高低字节。
        /// <para>原理：高字节右移 8 位到低字节位，低字节左移 8 位到高字节位，再按位或合并。</para>
        /// <para>示例：0x1234 → 0x3412</para>
        /// </summary>
        private static ushort SwapBytes(ushort value) => (ushort)((value << 8) | (value >> 8));

        #endregion
    }
}