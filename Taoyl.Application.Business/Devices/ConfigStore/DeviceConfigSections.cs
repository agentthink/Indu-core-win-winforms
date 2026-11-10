using System;
using System.Collections.Generic;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Device.Config;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.PlcDriver.Models.Config;

namespace Taoyl.Application.Business.Devices.ConfigStore
{
    /// <summary>
    /// 设备配置节注册表（显式单点）：本应用支持的设备类型 → Devices.json 节 的映射。
    /// 🔴 本文件是「应用的数据形状」唯一落点：底座（Device）只提供泛型 DeviceConfigSection&lt;TModel&gt;，
    ///    具体的 TModel（DevicesConfigFile）与三条映射留在这里——新增设备类型只改本文件一行 + 根模型加一个列表，
    ///    不动底座、不动 DeviceManager。
    /// 引用方：DeviceModule（逐项 RegisterInstance 为 IDeviceConfigSection，注入 DeviceManager）。
    /// </summary>
    public static class DeviceConfigSections
    {
        /// <summary>已注册的设备配置节</summary>
        public static readonly IReadOnlyList<IDeviceConfigSection> All = new List<IDeviceConfigSection>
        {
            new DeviceConfigSection<DevicesConfigFile>(
                DeviceTypes.Plc,
                "PLCs",
                typeof(PlcConfig),
                m => m.PLCs,
                c => RequireConfig<PlcConfig>(c, DeviceTypes.Plc).Name),
            new DeviceConfigSection<DevicesConfigFile>(
                DeviceTypes.Scanner,
                "scanners",
                typeof(ScannerConfig),
                m => m.Scanners,
                c => RequireConfig<ScannerConfig>(c, DeviceTypes.Scanner).Name),
            new DeviceConfigSection<DevicesConfigFile>(
                DeviceTypes.Scale,
                "scales",
                typeof(ScaleConfig),
                m => m.Scales,
                c => RequireConfig<ScaleConfig>(c, DeviceTypes.Scale).Name),
        };

        private static T RequireConfig<T>(object config, string deviceType) where T : class
        {
            if (!(config is T typed))
                throw new ArgumentException($"{deviceType} 配置必须是 {typeof(T).Name} 对象");
            return typed;
        }
    }
}
