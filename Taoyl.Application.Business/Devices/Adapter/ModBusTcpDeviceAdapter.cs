using System;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Communication;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Business.Devices.Adapter
{
    public class ModBusTcpDeviceAdapter : IDevice
    {
        private readonly ModbusTcpMaster scaleInstance;

        public string Name { get; }
        public string DeviceType => DeviceTypes.Scale;
        public bool IsConnected => scaleInstance?.IsConnected ?? false;

        public ModBusTcpDeviceAdapter(string name, ModbusTcpMaster instance)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            scaleInstance = instance ?? throw new ArgumentNullException(nameof(instance));
        }

        public bool Connect()
        {
            if (scaleInstance.IsConnected) return true;
            try
            {
                var connected = scaleInstance.Connect();

                return connected.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        public void Disconnect()
        {
            try
            {
                scaleInstance?.Disconnect();
            }
            catch { }
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// 通用原生驱动获取（新代码推荐，统一接口）
        /// </summary>
        public T GetRawDev<T>()
        {
            if (scaleInstance is T result)
                return result;

            throw new InvalidCastException(
                $"设备 [{Name}] 的底层驱动类型为 {scaleInstance?.GetType().Name}，无法转换为 {typeof(T).Name}");
        }
    }
}
