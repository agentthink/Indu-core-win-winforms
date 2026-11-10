using System;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Business.Devices.Adapter
{
    /// <summary>
    /// 通用设备适配器。
    /// 适用于厂家驱动已实现 Connect/Disconnect/IsConnected 的情况，
    /// 无需手写具体 Adapter 类，一行代码完成包装。
    /// </summary>
    /// <typeparam name="TDriver">厂家驱动类型</typeparam>
    public sealed class GenericDeviceAdapter<TDriver> : IDevice
    {
        private readonly TDriver _driver;
        private readonly Func<TDriver, bool> _connect;
        private readonly Action<TDriver> _disconnect;
        private readonly Func<TDriver, bool> _isConnected;
        private readonly Action<TDriver> _dispose;

        public string Name { get; }
        public string DeviceType { get; }

        public bool IsConnected => _isConnected(_driver);

        public GenericDeviceAdapter(
            string name,
            string deviceType,
            TDriver driver,
            Func<TDriver, bool> connect,
            Action<TDriver> disconnect,
            Func<TDriver, bool> isConnected,
            Action<TDriver> dispose = null)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            DeviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
            _driver = driver;
            _connect = connect ?? throw new ArgumentNullException(nameof(connect));
            _disconnect = disconnect ?? throw new ArgumentNullException(nameof(disconnect));
            _isConnected = isConnected ?? throw new ArgumentNullException(nameof(isConnected));
            _dispose = dispose ?? disconnect;
        }

        public bool Connect() => _connect(_driver);

        public void Disconnect() => _disconnect(_driver);

        public void Dispose() => _dispose(_driver);

        public T GetRawDev<T>()
        {
            if (_driver is T result)
                return result;

            throw new InvalidCastException(
                $"设备 [{Name}] 的底层驱动类型为 {_driver?.GetType().Name}，无法转换为 {typeof(T).Name}");
        }
    }

    /// <summary>
    /// 通用设备适配器静态辅助类，简化创建调用
    /// </summary>
    public static class DeviceAdapter
    {
        /// <summary>
        /// 创建通用设备适配器
        /// </summary>
        public static IDevice Create<TDriver>(
            string name,
            string deviceType,
            TDriver driver,
            Func<TDriver, bool> connect,
            Action<TDriver> disconnect,
            Func<TDriver, bool> isConnected,
            Action<TDriver> dispose = null)
        {
            return new GenericDeviceAdapter<TDriver>(
                name, deviceType, driver, connect, disconnect, isConnected, dispose);
        }
    }
}