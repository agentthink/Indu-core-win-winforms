using System.Text;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models;

namespace Taoyl.Application.PlcDriver.Protocols
{
    /// <summary>
    /// Modbus 协议访问（Modbus 族共用：ModbusTcp / ModbusRtu）
    /// 共用依据：HSL 层由 ModbusHelper 统一承载帧构建与类型解释（反编译实证 12.5.3），
    ///           数据类型策略与「TCP 还是串口」的传输形态无关——两驱动的协议差异完全一致
    /// 协议差异（覆写基类默认实现）：仅 String 读一条——
    /// String 读：ASCII 编码；⚠️ Length 下界是 1（读策略显式拒绝 ≤0）：Modbus 无「0 = 读原生整串」语义（那是 S7 专有），
    ///           放行 0 属错用，显式拒绝而非静默降级
    /// String 写：基类已是 ASCII 原样全写（奇数字节补齐后按字写），零差异，无需覆写
    /// 备注（协议差异由 HSL 层承载，不覆写）：
    /// String 的 Length 规则 = 寄存器字数（每字 2 字节，与 FINS 同构）；
    /// Bool 语义由地址形态自含（100=线圈 FC01/FC05；100.0=寄存器位，读 FC03+提取、写 mask FC22 或自动降级读改写）；
    /// Byte 写 = 1 字节 < 1 寄存器（FC16 写 0 寄存器，畸形帧响亮失败）——与 FINS 同款限制，现场避免使用 Byte 写
    /// </summary>
    public class ModbusProtocolAccess : BaseProtocolAccess
    {
        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public ModbusProtocolAccess(IPlcTransport communication) : base(communication)
        {
        }
        #endregion

        #region 策略初始化（Modbus 族特有）
        /// <summary>
        /// Modbus 读策略覆写：String 用 ASCII，len ≤ 0 直接报错（不静默降级）
        /// </summary>
        protected override void InitializeReadStrategies()
        {
            base.InitializeReadStrategies();

            _readStrategies[PlcDataType.String] = (addr, len, holder) =>
            {
                if (len <= 0)
                {
                    // ErrorCode 用默认 10000（不可用负数：CheckConnectionStatus 会把负数判为断线，误触发重连）
                    return ResponseResult.Fail(
                        $"Modbus 的 String 长度是寄存器字数，不支持 {len}（「0 = 读 PLC 原生整串」仅西门子 S7 有定义），请填正整数");
                }
                return ApplyRead(Communication.ReadString(addr, (ushort)len, Encoding.ASCII), holder);
            };
        }
        #endregion
    }
}
