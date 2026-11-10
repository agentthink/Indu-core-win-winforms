using HslCommunication.Profinet.Siemens;
using System;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.PlcDriver.Protocols;
using Taoyl.Application.PlcDriver.Transports;

namespace Taoyl.Application.PlcDriver.Drivers
{
    /// <summary>
    /// 西门子S7协议驱动程序
    /// 支持S7-1200/1500/300/400/200Smart/200，装配协议层与传输层
    /// 协议差异参数（PLCType/Rack/Slot）从 PlcConfig.ProtocolParams 读取，未配置回退默认（S1500）
    /// 管道装配统一走 HslTcpTransport.AttachTcpPipe，本类只传本协议默认端口（PlcConfig.Port 留空时兜底）
    /// ⚠️ 两处 HSL 装配约束（实测）：
    /// ① PLCType（SiemensPLCS）在 HSL 是私有字段，仅构造函数可定 —— 必须先解析机型再 new 设备，不能经对象初始化器赋值
    /// ② Rack/Slot 虽有公开 setter，但 setter 内部改写 plcHead1[21]（机架槽位共用字节），
    ///    故仅在 ProtocolParams 显式配置时才写，默认零干预（S1500 预设已等价 rack0/slot0）
    /// ⚠️ 本类 Create 在机型 / Rack / Slot 非法时抛异常（"配置错就响"优于"连上了但读写全错"）——
    ///    该异常由 DeviceManager.BuildDevice 的「先建新、成功才换旧」事务序兜住：抛出不产生任何副作用
    /// </summary>
    public class SiemensS7 : PlcRuntime
    {
        /// <summary>默认机型：S7-1500（对应 HSL SiemensPLCS.S1500）</summary>
        private const string DefaultPlcType = "S1500";

        /// <summary>默认端口：S7 标准端口 102（ISO-on-TCP / RFC1006），PlcConfig.Port 留空时兜底</summary>
        private const int DefaultPort = 102;

        /// <summary>
        /// 工厂方法：创建并配置西门子S7实例（各子类独立实现，返回 IPlc）
        /// </summary>
        /// <param name="plcConfig">PLC配置</param>
        /// <param name="configManager">配置管理器（注入，加载标签配置）</param>
        public static IPlc Create(PlcConfig plcConfig, IPlcConfigManager configManager)
        {
            // 1. 解析机型（必须在 new 之前：HSL 的机型字段构造过后改不了，详见类注释①）
            var plcType = ParsePlcType(plcConfig.GetProtocolParam("PLCType", DefaultPlcType));

            // 2. 创建HSL设备（协议差异参数从 ProtocolParams 读取：PLCType/Rack/Slot）
            var hslPlc = new SiemensS7Net(plcType);

            // 3. 装配TCP管道（IP/端口兜底/两个超时/关管策略统一由 HslTcpTransport.AttachTcpPipe 承担）
            HslTcpTransport.AttachTcpPipe(hslPlc, plcConfig, DefaultPort);

            // 4. 机架/槽位：仅显式配置才写（无条件赋值 = 用 0/0 覆盖 HSL 机型预设，属倒退）
            ApplyRackSlot(hslPlc, plcConfig);

            // 5. 创建传输层（SiemensS7Net is-a DeviceTcpNet，唯一 HSL 转换点）
            var transport = new HslTcpTransport(hslPlc);

            // 6. 创建西门子S7协议访问
            var protocolAccess = new SiemensS7ProtocolAccess(transport);

            // 7. 创建驱动实例（3参构造，transport 由基类从 protocolAccess.Communication 取）
            return new SiemensS7(plcConfig, protocolAccess, configManager);
        }

        /// <summary>
        /// 解析机型串（ProtocolParams.PLCType，默认 S1500）
        /// 未知机型抛异常而非静默回落：机型填错在现场表现为"连上了但读写全错"，比启动即失败难查得多
        /// 抛出由 DeviceManager.BuildDevice 的事务序兜住（先建新、成功才换旧），不产生任何副作用
        /// </summary>
        private static SiemensPLCS ParsePlcType(string plcTypeText)
        {
            if (Enum.TryParse(plcTypeText?.Trim(), ignoreCase: true, out SiemensPLCS plcType))
                return plcType;

            throw new ArgumentException(
                $"不支持的西门子机型 PLCType: {plcTypeText}（可用值：{string.Join("/", Enum.GetNames(typeof(SiemensPLCS)))}）");
        }

        /// <summary>
        /// 应用机架/槽位（ProtocolParams.Rack/Slot，仅显式配置才写）
        /// 两者 setter 内部都是 plcHead1[21] = Rack*32 + Slot（共用同一字节），故赋值顺序无关
        /// </summary>
        private static void ApplyRackSlot(SiemensS7Net hslPlc, PlcConfig plcConfig)
        {
            var rack = ParseOptionalByte(plcConfig.GetProtocolParam("Rack", null), "西门子机架号 Rack");
            if (rack.HasValue)
                hslPlc.Rack = rack.Value;

            var slot = ParseOptionalByte(plcConfig.GetProtocolParam("Slot", null), "西门子槽位号 Slot");
            if (slot.HasValue)
                hslPlc.Slot = slot.Value;
        }

        /// <summary>
        /// 解析可选的字节型协议参数：未配置/空白 ⇒ null（= 不干预 HSL 机型预设）；非法 ⇒ 抛异常
        /// </summary>
        /// <param name="text">ProtocolParams 原始值</param>
        /// <param name="displayName">报错用的中文参数名</param>
        private static byte? ParseOptionalByte(string text, string displayName)
        {
            if (string.IsNullOrWhiteSpace(text))
                return null;

            if (!byte.TryParse(text.Trim(), out var value))
                throw new ArgumentException(OptionalByteError(text, displayName));

            return value;
        }

        /// <summary>可选字节型协议参数非法时的错误文案（Create 抛出用）</summary>
        private static string OptionalByteError(string text, string displayName)
            => $"{displayName} 无效: {text}（应为 0~255 的整数）";

        /// <summary>
        /// 构造函数
        /// </summary>
        private SiemensS7(PlcConfig plcConfig, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager)
            : base(plcConfig, protocolAccess, configManager)
        {
            _logger.WriteInfo($"西门子S7驱动初始化完成: {plcConfig.Name}");
        }
    }
}
