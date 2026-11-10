using HslCommunication.Profinet.Omron;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.PlcDriver.Protocols;
using Taoyl.Application.PlcDriver.Transports;

namespace Taoyl.Application.PlcDriver.Drivers
{
    /// <summary>
    /// 欧姆龙CIP协议驱动程序
    /// 支持NJ/NX/CP系列PLC，装配协议层与传输层
    /// 超时读 PlcConfig 顶层属性 ConnectTimeOut/ReceiveTimeOut（不走 ProtocolParams），未配置回退默认
    /// 管道装配统一走 HslTcpTransport.AttachTcpPipe，本类只传本协议默认端口（PlcConfig.Port 留空时兜底）
    /// 注意：欧姆龙 CIP 无 Slot 概念（西门子/Fins 才有），Slot/Rack 读法见 SiemensS7
    /// </summary>
    public class OmronCip : PlcRuntime
    {
        /// <summary>默认端口：EtherNet/IP 标准端口 44818（PlcConfig.Port 留空时兜底）</summary>
        private const int DefaultPort = 44818;

        /// <summary>
        /// 工厂方法：创建并配置欧姆龙CIP实例（各子类独立实现，返回 IPlc）
        /// </summary>
        /// <param name="plcConfig">PLC配置</param>
        /// <param name="configManager">配置管理器（注入，加载标签配置）</param>
        public static IPlc Create(PlcConfig plcConfig, IPlcConfigManager configManager)
        {
            // 1. 创建HSL设备（机型专属参数：CIP 空闲超时）
            var hslPlc = new OmronConnectedCipNet
            {
                // CIP 空闲超时 = 2^ConnectionTimeoutMultiplier × 8 秒（写进 ForwardOpen byte[40]），默认 2 → 32 秒。
                // 此处显式写 2（即 HSL 默认值）：把隐式契约摆在明面上，防后续照抄连接参数时漏掉它；改 6 ≈ 8 分钟。
                ConnectionTimeoutMultiplier = 2,
            };

            // 2. 装配TCP管道（IP/端口兜底/两个超时/关管策略统一由 HslTcpTransport.AttachTcpPipe 承担）
            HslTcpTransport.AttachTcpPipe(hslPlc, plcConfig, DefaultPort);

            // 3. 创建传输层（OmronConnectedCipNet is-a DeviceTcpNet，唯一 HSL 转换点）
            var transport = new HslTcpTransport(hslPlc);

            // 4. 创建欧姆龙CIP协议访问
            var protocolAccess = new OmronCipProtocolAccess(transport);

            // 5. 创建驱动实例（3参构造，transport 由基类从 protocolAccess.Communication 取）
            return new OmronCip(plcConfig, protocolAccess, configManager);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        private OmronCip(PlcConfig plcConfig, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager)
            : base(plcConfig, protocolAccess, configManager)
        {
            _logger.WriteInfo($"欧姆龙CIP驱动初始化完成: {plcConfig.Name}");
        }
    }
}
