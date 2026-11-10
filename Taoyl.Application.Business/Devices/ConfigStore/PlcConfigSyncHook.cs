using System;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Models.Config;

namespace Taoyl.Application.Business.Devices.ConfigStore
{
    /// <summary>
    /// PLC 驱动缓存同步钩子（缓存型设备的唯一实现）。
    /// 原 DeviceManager.SyncRuntimeConfig / ClearRuntimeConfig 的 `if (deviceType == PLC)` 分支原样搬来，逻辑零变化：
    /// - Sync：登记/更新 PLC 主配置缓存（PlcDeviceFactory.CreateDevice 经同名缓存短路取最新，故必须先于实例构建）；
    /// - Clear：清主配置 + 清地址表组（旧地址表文件不删——现场资产，改名后需手工同步 PlcTagsPath/LogPath）。
    /// </summary>
    public sealed class PlcConfigSyncHook : IDeviceConfigSyncHook
    {
        private readonly IPlcConfigManager _plcConfigManager;

        public PlcConfigSyncHook(IPlcConfigManager plcConfigManager)
        {
            _plcConfigManager = plcConfigManager ?? throw new ArgumentNullException(nameof(plcConfigManager));
        }

        /// <inheritdoc />
        public bool CanHandle(string deviceType)
        {
            return string.Equals(deviceType, DeviceTypes.Plc, StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public void Sync(string deviceType, object config)
        {
            _plcConfigManager.UpdatePlcConfig((PlcConfig)config);
        }

        /// <inheritdoc />
        public bool Clear(string deviceType, string name)
        {
            // 两项都要执行（非短路 |）：主配置与地址表组是两套缓存，任一命中即算清理过
            return _plcConfigManager.RemovePlcConfig(name) | _plcConfigManager.RemovePlcTagGroups(name);
        }
    }
}
