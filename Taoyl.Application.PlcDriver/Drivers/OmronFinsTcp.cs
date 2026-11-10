using HslCommunication.Core;
using HslCommunication.Profinet.Omron;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.PlcDriver.Protocols;
using Taoyl.Application.PlcDriver.Transports;
using System;

namespace Taoyl.Application.PlcDriver.Drivers
{
    /// <summary>
    /// 欧姆龙FinsTcp协议驱动程序
    /// 支持CS/CJ/CP系列PLC（FINS TCP，字寻址），装配协议层与传输层
    /// 协议差异参数（PLCType/DataFormat/IsStringReverseByteWord）从 PlcConfig.ProtocolParams 读取，
    /// 未配置回退 HSL 默认（CSCJ/CDAB/true），配置了但非法抛异常（"配置错就响"，由 BuildDevice 事务序兜住）
    /// 固定参数（DA2=0 / ReceiveUntilEmpty=false）与 HSLDemo 模板对齐：当前值 = HSL 默认，显式赋值防 HSL 升级漂移；
    /// DA1/SA1 不赋值（连接握手自动从 PLC 获取/被 PLC 改写）
    /// 超时读 PlcConfig 顶层属性 ConnectTimeOut/ReceiveTimeOut（不走 ProtocolParams），未配置回退默认
    /// 管道装配统一走 HslTcpTransport.AttachTcpPipe，本类只传本协议默认端口（PlcConfig.Port 留空时兜底）
    /// HSL 侧要点（反编译实证 12.5.3，禁凭直觉臆断）：
    /// ① ConnectServer → InitializationOnConnect 自动发 FINS 握手帧，DA1/SA1 节点号从 PLC 自动获取，TCP 侧无需手工配置节点
    /// ② >500 字的读 HSL 自动分段（ReadSplits=500）；Bool 走位读，地址需带位号（如 D100.5）
    /// </summary>
    public class OmronFinsTcp : PlcRuntime
    {
        /// <summary>默认机型：CS/CJ/CP 系列（对应 HSL OmronPlcType.CSCJ）</summary>
        private const string DefaultPlcType = "CSCJ";

        /// <summary>默认字序：CDAB（32/64 位数据的字内字序，与 HSL 默认一致）</summary>
        private const string DefaultDataFormat = "CDAB";

        /// <summary>默认字符串字内反转解码：true（与 HSL 默认一致）</summary>
        private const string DefaultIsStringReverseByteWord = "true";

        /// <summary>默认端口：FINS TCP 标准端口 9600（PlcConfig.Port 留空时兜底）</summary>
        private const int DefaultPort = 9600;

        /// <summary>
        /// 工厂方法：创建并配置欧姆龙FinsTcp实例（各子类独立实现，返回 IPlc）
        /// </summary>
        /// <param name="plcConfig">PLC配置</param>
        /// <param name="configManager">配置管理器（注入，加载标签配置）</param>
        public static IPlc Create(PlcConfig plcConfig, IPlcConfigManager configManager)
        {
            // 1. 解析协议差异参数（ProtocolParams 可覆写，未配置回退默认，非法值抛异常）
            var plcType = ParsePlcType(plcConfig.GetProtocolParam("PlcType", DefaultPlcType));
            var dataFormat = ParseDataFormat(plcConfig.GetProtocolParam("DataFormat", DefaultDataFormat));
            var isStringReverseByteWord = ParseBool(
                plcConfig.GetProtocolParam("IsStringReverseByteWord", DefaultIsStringReverseByteWord), "IsStringReverseByteWord");

            // 2. 创建HSL设备（固定参数与 HSLDemo 模板对齐：DA2=0 / ReceiveUntilEmpty=false，显式赋值防升级漂移）
            var hslPlc = new OmronFinsNet
            {
                PlcType = plcType,
                DA2 = 0,
                ReceiveUntilEmpty = false,
            };
            hslPlc.ByteTransform.DataFormat = dataFormat;
            hslPlc.ByteTransform.IsStringReverseByteWord = isStringReverseByteWord;

            // 3. 装配TCP管道（IP/端口兜底/两个超时/关管策略统一由 HslTcpTransport.AttachTcpPipe 承担）
            HslTcpTransport.AttachTcpPipe(hslPlc, plcConfig, DefaultPort);

            // 4. 创建传输层（OmronFinsNet is-a DeviceTcpNet，唯一 HSL 转换点）
            var transport = new HslTcpTransport(hslPlc);

            // 5. 创建欧姆龙FinsTcp协议访问
            var protocolAccess = new OmronFinsTcpProtocolAccess(transport);

            // 6. 创建驱动实例（3参构造，transport 由基类从 protocolAccess.Communication 取）
            return new OmronFinsTcp(plcConfig, protocolAccess, configManager);
        }

        /// <summary>
        /// 解析机型串（ProtocolParams.PLCType，默认 CSCJ）
        /// 未知机型抛异常而非静默回落：机型填错在现场表现为"连上了但读写全错"，比启动即失败难查得多
        /// 抛出由 DeviceManager.BuildDevice 的事务序兜住（先建新、成功才换旧），不产生任何副作用
        /// </summary>
        private static OmronPlcType ParsePlcType(string plcTypeText)
        {
            if (Enum.TryParse(plcTypeText?.Trim(), ignoreCase: true, out OmronPlcType plcType))
                return plcType;

            throw new ArgumentException(
                $"欧姆龙FinsTcp 机型非法: \"{plcTypeText}\"（可选值见 OmronPlcType，如 CSCJ/CV），请检查 ProtocolParams.PLCType");
        }

        /// <summary>
        /// 解析字序串（ProtocolParams.DataFormat，默认 CDAB）；非法值抛异常（语义同 ParsePlcType）
        /// </summary>
        private static DataFormat ParseDataFormat(string dataFormatText)
        {
            if (Enum.TryParse(dataFormatText?.Trim(), ignoreCase: true, out DataFormat dataFormat))
                return dataFormat;

            throw new ArgumentException(
                $"欧姆龙FinsTcp 字序非法: \"{dataFormatText}\"（可选 ABCD/BADC/DCBA/CDAB），请检查 ProtocolParams.DataFormat");
        }

        /// <summary>
        /// 解析布尔参数（通用，非法值抛异常）；paramName 仅用于报错定位
        /// </summary>
        private static bool ParseBool(string valueText, string paramName)
        {
            if (bool.TryParse(valueText?.Trim(), out var value))
                return value;

            throw new ArgumentException(
                $"欧姆龙FinsTcp 参数非法: \"{valueText}\"（应为 true/false），请检查 ProtocolParams.{paramName}");
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        private OmronFinsTcp(PlcConfig plcConfig, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager)
            : base(plcConfig, protocolAccess, configManager)
        {
            _logger.WriteInfo($"欧姆龙FinsTcp驱动初始化完成: {plcConfig.Name}");
        }
    }
}
