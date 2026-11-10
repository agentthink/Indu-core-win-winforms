using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Taoyl.Application.Business.Devices.Adapter;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Drivers;
using Taoyl.Application.PlcDriver.Models.Config;

namespace Taoyl.Application.Business.Devices.Factory
{
    /// <summary>
    /// PLC 设备工厂（无状态纯工厂）
    /// 职责：按配置创建 IPlc 实例（每次 new，不缓存、不去重）
    /// 设备实例由 DeviceManager 统一持有（_devices），防重/释放/重建均为 DeviceManager 职责
    /// 配置获取：CreateDevice 走 LoadJsonConfigForPlc（同名缓存短路——重建前先 UpdatePlcConfig 保存新配置，此处即返回最新配置）
    /// 协议白名单 = PlcProtocol 枚举成员 → 本工厂协议字典（加协议 = 枚举加成员 + 协议字典加键值 + 实现类三动作）
    /// TryValidateConfig = CreateDevice 校验前段（同源），供配置管理编排在锁外做 UX 前置提示（必填项 / 格式 / 协议白名单）。
    /// ⚠️ 它不承担「失败零副作用」责任——那由 DeviceManager.BuildDevice 的事务序保证（先建新、成功才换旧），
    ///    故协议差异参数（如 S7 的 PLCType / Rack / Slot）不在此处重复校验，由驱动 Create 抛异常经事务序兜住
    /// </summary>
    public class PlcDeviceFactory : IDeviceFactory
    {
        private readonly IPlcConfigManager _configManager;

        // 协议映射（策略）：键 = PlcProtocol 枚举（成员名即磁盘规范串）
        private readonly Dictionary<PlcProtocol, Func<PlcConfig, IPlc>> _protocolFactories;

        public string DeviceType => DeviceTypes.Plc;

        /// <summary>
        /// 构造函数（构造注入配置管理器，去 ConfigManager.Instance）
        /// </summary>
        public PlcDeviceFactory(IPlcConfigManager configManager)
        {
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));

            // 协议映射（策略）：注入配置管理器，随 PLC 实例创建链路传递
            _protocolFactories = new Dictionary<PlcProtocol, Func<PlcConfig, IPlc>>
            {
                [PlcProtocol.OmronConnectedCipNet] = cfg => OmronCip.Create(cfg, _configManager),
                [PlcProtocol.SiemensS7Net] = cfg => SiemensS7.Create(cfg, _configManager),
                [PlcProtocol.OmronFinsTcpNet] = cfg => OmronFinsTcp.Create(cfg, _configManager),
                [PlcProtocol.ModbusTcpNet] = cfg => ModbusTcp.Create(cfg, _configManager),
                [PlcProtocol.ModbusRtu] = cfg => ModbusRtu.Create(cfg, _configManager)
            };
        }

        public IDevice CreateDevice(string name, object config)
        {
            var configStr = JsonConvert.SerializeObject(config);
            // 获取配置（同名缓存短路：若已 UpdatePlcConfig 保存过新配置，此处返回缓存中的最新配置）
            var plcConfig = _configManager.LoadJsonConfigForPlc(configStr);
            if (plcConfig == null)
                throw new ArgumentException($"配置参数异常，无法解析为正确PLC配置");

            // 协议白名单：解析失败/不在字典 = 不支持（OrdinalIgnoreCase 大小写不敏感——磁盘/Excel 手写大小写均可）
            if (!TryResolveProtocol(plcConfig.Protocol, out var protocol))
                throw new NotSupportedException($"不支持的 PLC 协议: {plcConfig.Protocol}");

            var plc = _protocolFactories[protocol](plcConfig);
            return new PlcDeviceAdapter(name, plc);
        }

        /// <inheritdoc />
        public bool TryValidateConfig(object config, out string errMsg)
        {
            try
            {
                errMsg = null;
                if (config == null)
                {
                    errMsg = "PLC 配置为空";
                    return false;
                }

                // 兼容 PlcConfig 对象与 JToken（loader 传节点）/ 任意可反序列化对象
                var plcConfig = config as PlcConfig;
                if (plcConfig == null)
                {
                    var configStr = JsonConvert.SerializeObject(config);
                    plcConfig = JsonConvert.DeserializeObject<PlcConfig>(configStr);
                }
                if (plcConfig == null)
                {
                    errMsg = "配置无法解析为 PLC 配置";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(plcConfig.Name))
                {
                    errMsg = "设备名称不能为空";
                    return false;
                }
                if (string.IsNullOrWhiteSpace(plcConfig.Ip))
                {
                    errMsg = $"PLC[{plcConfig.Name}] 连接端点不能为空（网络协议填 IP 地址，串口协议填串口号，如 COM3）";
                    return false;
                }
                // 端口可留空 = 用驱动层默认端口（OmronCip=44818 / SiemensS7=102，见各驱动私有 DefaultPort）：
                // 留空让新建任何协议都不必改端口；填了就必须合法（非数字 / 越界仍拦）
                if (!string.IsNullOrWhiteSpace(plcConfig.Port)
                    && (!int.TryParse(plcConfig.Port, out int port) || port <= 0 || port > 65535))
                {
                    errMsg = $"PLC[{plcConfig.Name}] 端口号无效: {plcConfig.Port}（留空 = 用协议默认端口）";
                    return false;
                }
                if (!TryResolveProtocol(plcConfig.Protocol, out _))
                {
                    errMsg = $"PLC[{plcConfig.Name}] 不支持的通讯协议: {plcConfig.Protocol}";
                    return false;
                }
                // 地址表路径必填（校验非空、不校验文件存在——§6.3 "先保存配置后补表"工作流兼容：先填路径保存、后补 Excel 文件）。
                // 仅放编排预检层（BuildDevice/UpdateDevice 拦截新增与编辑保存）；勿下沉 ConfigManager.ValidateDevConfig
                // （启动加载链路共用，存量缺表配置会启动失败）
                if (string.IsNullOrWhiteSpace(plcConfig.PlcTagsPath))
                {
                    errMsg = $"PLC[{plcConfig.Name}] 地址表路径（PlcTagsPath）不能为空";
                    return false;
                }

                return true; // 校验通过
            }
            catch (Exception ex)
            {
                errMsg = $"PLC 配置校验异常: {ex.Message}";
                return false;
            }
        }

        /// <summary>协议串 → 白名单枚举（与 CreateDevice 同源：OrdinalIgnoreCase 解析 + 字典存在性）</summary>
        private bool TryResolveProtocol(string protocolText, out PlcProtocol protocol)
        {
            protocol = default;
            return !string.IsNullOrWhiteSpace(protocolText)
                && Enum.TryParse<PlcProtocol>(protocolText, ignoreCase: true, out protocol)
                && _protocolFactories.ContainsKey(protocol);
        }
    }
}
