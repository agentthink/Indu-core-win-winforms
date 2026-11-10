using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models;

namespace Taoyl.Application.PlcDriver.Protocols
{
    /// <summary>
    /// 欧姆龙FinsTcp协议访问（对应驱动 OmronFinsTcp）
    /// 职责：提供欧姆龙 FINS TCP 协议特有的读写策略
    /// ⚠️ FINS 是字寻址协议（与 CIP 的 tag 模型、S7 的字节寻址都不同），Length 规则以协议定义为准：
    /// String：Length = 字数（每字 2 字节）——FINS 协议即如此定义，不做换算适配（HSLDemo 实测 Length=1 读回 2 字符）
    /// 读写策略与基类零差异，无需覆写：
    /// String 读走基类 = ReadString(addr, len)（HSL 默认编码即 ASCII，反编译实证 DeviceCommunication L204-207）
    /// String 写走基类 = ASCII 原样全写（HSL 内部补齐偶数字节后走字写：无长度头、无读改写）
    /// 类型化读走 HSL 字感知换算（GetWordLength = WordLength × 元素个数 × 类型字数：Short=1 字 / Int·Float=2 字 / Long·Double=4 字，反编译实证）
    /// Byte 读 = Read(addr,1) 读 1 字取字内高字节（FINS 无字节区，行为确定，无需覆写）
    /// ⚠️ Byte 写不覆写：基类写 1 字节 → HSL 字写命令的字数 = 1/2 = 0（整除）= 畸形 FINS 帧，
    ///    PLC 回 end code 错误（响亮失败，不会写坏相邻数据）——FINS 无字节写语义，现场地址表避免使用 Byte 写
    /// </summary>
    public class OmronFinsTcpProtocolAccess : BaseProtocolAccess
    {
        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public OmronFinsTcpProtocolAccess(IPlcTransport communication) : base(communication)
        {
        }
        #endregion

        #region 策略初始化（FinsTcp 特有）
        // 读写策略与基类零差异，无需覆写；协议差异说明全部收在类注释。
        #endregion
    }
}
