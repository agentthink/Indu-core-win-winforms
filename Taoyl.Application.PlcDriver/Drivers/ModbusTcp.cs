using HslCommunication.Core;
using HslCommunication.ModBus;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.PlcDriver.Protocols;
using Taoyl.Application.PlcDriver.Transports;
using System;

namespace Taoyl.Application.PlcDriver.Drivers
{
    /// <summary>
    /// Modbus TCP协议驱动程序
    /// 支持标准 Modbus TCP 从站（寄存器/线圈寻址），装配协议层与传输层
    /// 协议差异参数（Station/AddressStartWithZero/DataFormat/IsStringReverse）从 PlcConfig.ProtocolParams 读取，
    /// 未配置回退默认（1/true/CDAB/false），配置了但非法抛异常（"配置错就响"，由 BuildDevice 事务序兜住）
    /// 超时读 PlcConfig 顶层属性 ConnectTimeOut/ReceiveTimeOut（不走 ProtocolParams），未配置回退默认
    /// 管道装配统一走 HslTcpTransport.AttachTcpPipe，本类只传本协议默认端口（PlcConfig.Port 留空时兜底）
    /// 地址即 HSL 原生语法：100=保持寄存器（0-based 偏移）/ x=4;100=输入寄存器 / x=2;100=离散输入 /
    /// s=2;100=按地址携带站号 / 100.0=寄存器位（读 FC03 提取；写 mask FC22，设备不支持时自动降级读改写）
    /// HSL 侧要点（反编译实证 12.5.3，禁凭直觉臆断）：
    /// ① 0/1 基换算唯一发生在 CheckModbusAddressStart（AddressStartWithZero=false 时地址-1）；无 40001 风格自动识别
    /// ② String 的 Length 规则 = 寄存器字数（每字 2 字节）；IsStringReverse 默认 false（与 FINS 的 true 相反，显式赋值防漂移）
    /// </summary>
    public class ModbusTcp : PlcRuntime
    {
        /// <summary>默认站号：1（ProtocolParams.Station 未配置时使用；地址级 s= 可覆盖）</summary>
        private const string DefaultStation = "1";

        /// <summary>默认地址基准：true=0-based 偏移（与 HSL 默认一致；1-based 设备配 false）</summary>
        private const string DefaultAddressStartWithZero = "true";

        /// <summary>默认字序：CDAB（32/64 位数据的字节序，与 HSL 默认一致）</summary>
        private const string DefaultDataFormat = "CDAB";

        /// <summary>默认字符串字内反转解码：false（与 HSL 默认一致；FINS 为 true，两者相反勿混）</summary>
        private const string DefaultIsStringReverse = "false";

        /// <summary>默认端口：Modbus TCP 标准端口 502（PlcConfig.Port 留空时兜底）</summary>
        private const int DefaultPort = 502;

        /// <summary>
        /// 工厂方法：创建并配置ModbusTcp实例（各子类独立实现，返回 IPlc）
        /// </summary>
        /// <param name="plcConfig">PLC配置</param>
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

            // 2. 创建HSL设备（无参构造的 127.0.0.1:5000 被 AttachTcpPipe 整体接管，禁裸用）
            var hslPlc = new ModbusTcpNet
            {
                Station = station,
                AddressStartWithZero = addressStartWithZero,
                // mask 位写（FC22）优先：设备端原子；设备回异常码 1（功能码不支持）时 HSL 自动降级读改写重试（调用方无感）
                EnableWriteMaskCode = true,
            };
            hslPlc.DataFormat = dataFormat;
            hslPlc.IsStringReverse = isStringReverse;

            // 3. 装配TCP管道（IP/端口兜底/两个超时/关管策略统一由 HslTcpTransport.AttachTcpPipe 承担）
            HslTcpTransport.AttachTcpPipe(hslPlc, plcConfig, DefaultPort);

            // 4. 创建传输层（ModbusTcpNet is-a DeviceTcpNet，唯一 HSL 转换点）
            var transport = new HslTcpTransport(hslPlc);

            // 5. 创建ModbusTcp协议访问
            var protocolAccess = new ModbusProtocolAccess(transport);

            // 6. 创建驱动实例（3参构造，transport 由基类从 protocolAccess.Communication 取）
            return new ModbusTcp(plcConfig, protocolAccess, configManager);
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
                $"ModbusTcp 站号非法: \"{stationText}\"（应为 1~247 的整数），请检查 ProtocolParams.Station");
        }

        /// <summary>
        /// 解析字序串（ProtocolParams.DataFormat，默认 CDAB）；非法值抛异常（语义同 ParseStation）
        /// </summary>
        private static DataFormat ParseDataFormat(string dataFormatText)
        {
            if (Enum.TryParse(dataFormatText?.Trim(), ignoreCase: true, out DataFormat dataFormat))
                return dataFormat;

            throw new ArgumentException(
                $"ModbusTcp 字序非法: \"{dataFormatText}\"（可选 ABCD/BADC/DCBA/CDAB），请检查 ProtocolParams.DataFormat");
        }

        /// <summary>
        /// 解析布尔参数（通用，非法值抛异常）；paramName 仅用于报错定位
        /// </summary>
        private static bool ParseBool(string valueText, string paramName)
        {
            if (bool.TryParse(valueText?.Trim(), out var value))
                return value;

            throw new ArgumentException(
                $"ModbusTcp 参数非法: \"{valueText}\"（应为 true/false），请检查 ProtocolParams.{paramName}");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        private ModbusTcp(PlcConfig plcConfig, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager)
            : base(plcConfig, protocolAccess, configManager)
        {
            _logger.WriteInfo($"ModbusTcp驱动初始化完成: {plcConfig.Name}");
        }
    }
}
