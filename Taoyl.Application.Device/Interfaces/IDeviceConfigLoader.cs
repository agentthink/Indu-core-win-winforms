using System.Collections.Generic;
using Taoyl.Application.Device.Models;

namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 配置文件加载器策略接口（一条配置记录 → 一个 DeviceCreationRequest）
    /// </summary>
    public interface IDeviceConfigLoader
    {
        bool CanLoad(string filePath);
        IEnumerable<DeviceCreationRequest> Load(string filePath);
    }
}
