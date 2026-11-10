using System;

namespace Taoyl.Application.Device.Models
{
    /// <summary>
    /// 设备变更通知载荷。
    /// 🔴 只给「类型 + 名字 + 种类」，**不给 IDevice 实例**：
    ///    · 递旧实例 = 把一个已 Dispose 的对象递出去；
    ///    · 递新实例 = 诱导订阅方直接缓存裸引用（重建后又会失效）。
    ///    订阅方需要引用请自己经 <c>IDeviceManager.GetDevice</c> 重新取。
    /// </summary>
    public sealed class DeviceChangedEventArgs : EventArgs
    {
        public DeviceChangedEventArgs(string deviceType, string name, DeviceChangeKind kind)
        {
            DeviceType = deviceType;
            Name = name;
            Kind = kind;
        }

        /// <summary>设备类型标识（如 "PLC"）</summary>
        public string DeviceType { get; }

        /// <summary>设备逻辑名称（订阅方按名字判断是不是自己关心的设备）</summary>
        public string Name { get; }

        /// <summary>变更种类</summary>
        public DeviceChangeKind Kind { get; }
    }
}
