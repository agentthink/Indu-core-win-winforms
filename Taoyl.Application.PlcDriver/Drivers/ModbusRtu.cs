using HslCommunication.Core;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.PlcDriver.Protocols;
using Taoyl.Application.PlcDriver.Transports;
using System;
using HslModbusRtu = HslCommunication.ModBus.ModbusRtu;

namespace Taoyl.Application.PlcDriver.Drivers
{
    /// <summary>
    /// Modbus RTU协议驱动程序（串口）
    /// 支持标准 Modbus RTU 从站（寄存器/线圈寻址），装配协议层与串口传输层
    /// 连接端点 = PlcConfig.Ip（填串口号，如 COM3）；串口参数（BaudRate/DataBits/StopBits/Parity）从 ProtocolParams 读取，
    /// 未配置回退默认（9600/8/One/None），配置了但非法抛异常（"配置错就响"，由 BuildDevice 事务序兜住）
    /// 协议参数（Station/AddressStartWithZero/DataFormat/IsStringReverse）与 ModbusTcp 同规则，各自解析（驱动自包含）
    /// 地址即 HSL 原生语法，与 ModbusTcp 完全一致：100=保持寄存器（0-based 偏移）/ x=4;100=输入寄存器 /
    /// x=2;100=离散输入 / s=2;100=按地址携带站号 / 100.0=寄存器位（读 FC03 提取；写 mask FC22，设备不支持时自动降级读改写）
    /// 类名与 HSL 类型同名（HslCommunication.ModBus.ModbusRtu），文件内用 using 别名 HslModbusRtu 区分
    /// </summary>
    public class ModbusRtu : PlcRuntime
    {
        /// <summary>默认站号：1（ProtocolParams.Station 未配置时使用；地址级 s= 可覆盖）</summary>
        private const string DefaultStation = "1";

        /// <summary>默认地址基准：true=0-based 偏移（与 HSL 默认一致；1-based 设备配 false）</summary>
        private const string DefaultAddressStartWithZero = "true";

        /// <summary>默认字序：CDAB（32/64 位数据的字节序，与 HSL 默认一致）</summary>
        private const string DefaultDataFormat = "CDAB";

        /// <summary>默认字符串字内反转解码：false（与 HSL 默认一致；FINS 为 true，两者相反勿混）</summary>
        private const string DefaultIsStringReverse = "false";

        /// <summary>
        /// 工厂方法：创建并配置 ModbusRtu 实例（各子类独立实现，返回 IPlc）
        /// </summary>
        /// <param name="plcConfig">PLC配置（Ip=串口号）</param>
        /// <param name="configManager">配置管理器（注入，加载标签配置）</param>
        public static IPlc Create(PlcConfig plcConfig, IPlcConfigManager configManager)
        {
            // 1. 解析协议差异参数（ProtocolParams 可覆写，未配置回退默认，非法值抛异常）
            var station = ParseStation(plcConfig.GetProtocolParam("Station", DefaultStation));
            var addressStartWithZero = ParseBool(
                plcConfig.GetProtocolParam("AddressStartWithZero", DefaultAddressStartWithZero), "AddressStartWithZero");
            var dataFormat = ParseDataFormat(plcConfig.GetProtocolParam("DataFormat", DefaultDataFormat));
            var isStringReverse = ParseBool(
                plcConfig.GetProtocolParam("IsStringReverse", DefaultIsStringReverse), "IsStringReverse");

            // 2. 创建HSL串口设备（站号经构造注入）
            var hslPlc = new HslModbusRtu(station)
            {
                AddressStartWithZero = addressStartWithZero,
                // mask 位写（FC22）优先：设备端原子；设备回异常码 1（功能码不支持）时 HSL 自动降级读改写重试（调用方无感）
                EnableWriteMaskCode = true,
            };
            hslPlc.DataFormat = dataFormat;
            hslPlc.IsStringReverse = isStringReverse;

            // 3. 装配串口管道（串口号/串口参数/超时统一由 HslSerialTransport.AttachSerialPipe 承担；必须在 Open 前）
            HslSerialTransport.AttachSerialPipe(hslPlc, plcConfig);

            // 4. 创建传输层（ModbusRtu is-a DeviceSerialPort，唯一 HSL 转换点）
            var transport = new HslSerialTransport(hslPlc);

            // 5. 创建Modbus协议访问（TCP/RTU 共用 ModbusProtocolAccess）
            var protocolAccess = new ModbusProtocolAccess(transport);

            // 6. 创建驱动实例（3参构造，transport 由基类从 protocolAccess.Communication 取）
            return new ModbusRtu(plcConfig, protocolAccess, configManager);
        }

        /// <summary>
        /// 解析站号（ProtocolParams.Station，默认 1）
        /// 合法范围 1~247（Modbus 规范：0=广播、248~255 保留）；非法抛异常而非静默回落：
        /// 站号错在现场表现为"连上了但读写全错"，比启动即失败难查得多
        /// 抛出由 DeviceManager.BuildDevice 的事务序兜住（先建新、成功才换旧），不产生任何副作用
        /// </summary>
        private static byte ParseStation(string stationText)
        {
            if (byte.TryParse(stationText?.Trim(), out var station) && station >= 1 && station <= 247)
                return station;

            throw new ArgumentException(
                $"ModbusRtu 站号非法: \"{stationText}\"（应为 1~247 的整数），请检查 ProtocolParams.Station");
        }

        /// <summary>
        /// 解析字序串（ProtocolParams.DataFormat，默认 CDAB）；非法值抛异常（语义同 ParseStation）
        /// </summary>
        private static DataFormat ParseDataFormat(string dataFormatText)
        {
            if (Enum.TryParse(dataFormatText?.Trim(), ignoreCase: true, out DataFormat dataFormat))
                return dataFormat;

            throw new ArgumentException(
                $"ModbusRtu 字序非法: \"{dataFormatText}\"（可选 ABCD/BADC/DCBA/CDAB），请检查 ProtocolParams.DataFormat");
        }

        /// <summary>
        /// 解析布尔参数（通用，非法值抛异常）；paramName 仅用于报错定位
        /// </summary>
        private static bool ParseBool(string valueText, string paramName)
        {
            if (bool.TryParse(valueText?.Trim(), out var value))
                return value;

            throw new ArgumentException(
                $"ModbusRtu 参数非法: \"{valueText}\"（应为 true/false），请检查 ProtocolParams.{paramName}");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        private ModbusRtu(PlcConfig plcConfig, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager)
            : base(plcConfig, protocolAccess, configManager)
        {
            _logger.WriteInfo($"ModbusRtu驱动初始化完成: {plcConfig.Name}");
        }
    }
}
