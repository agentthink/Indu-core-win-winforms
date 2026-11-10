namespace Taoyl.Application.PlcDriver.Contracts
{
    /// <summary>
    /// PLC 通讯协议枚举。
    /// 成员名 = 磁盘契约规范串（Devices.json 的 protocol 键 / 地址表 Excel 的 protocol 列）：
    /// ToString() 即规范串，现场存量配置（"OmronConnectedCipNet"）原样兼容，零迁移。
    /// PlcConfig.Protocol 保持 string（JSON/Excel 双契约不动），枚举仅作校验/映射层。
    /// 扩展协议 = 加成员 → 设备工厂协议字典加键值 → 协议实现类（三动作，编译期可见漏配）。
    /// </summary>
    public enum PlcProtocol
    {
        /// <summary>欧姆龙 NJ/NX 系列 Ethernet/IP（CIP 协议，DeviceTcpNet）</summary>
        OmronConnectedCipNet,

        /// <summary>
        /// 西门子 S7 系列（S7-1200/1500/300/400/200Smart/200，S7 协议，DeviceTcpNet）
        /// 机型（PLCType）与 Rack/Slot 走 PlcConfig.ProtocolParams，默认 S1500 + HSL 预设机架槽位
        /// </summary>
        SiemensS7Net,

        /// <summary>
        /// 欧姆龙 CS/CJ/CP 系列（FINS TCP，字寻址，DeviceTcpNet）
        /// String 的 Length 规则 = 字数（每字 2 字节）；Bool 地址需带位号（如 D100.5）；默认端口 9600
        /// </summary>
        OmronFinsTcpNet,

        /// <summary>
        /// Modbus TCP（寄存器/线圈寻址，DeviceTcpNet）
        /// 地址即 HSL 原生语法（100 / x=4;100 / s=2;100 / 100.0 寄存器位）；默认端口 502
        /// </summary>
        ModbusTcpNet,

        /// <summary>
        /// Modbus RTU（寄存器/线圈寻址，DeviceSerialPort）
        /// 连接端点为串口号（PlcConfig.Ip 填 COM3 这类值）；串口参数在 ProtocolParams（BaudRate/DataBits/StopBits/Parity）
        /// 地址语法与 ModbusTcpNet 完全一致（HSL ModbusHelper 两形态共享）
        /// </summary>
        ModbusRtu,
    }
}
