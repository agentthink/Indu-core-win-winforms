using System;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.PlcDriver.Contracts;

namespace Taoyl.Application.Business.Devices.Adapter
{
    internal class PlcDeviceAdapter : IDevice
    {
        private readonly IPlc _plcInstance;

        public string Name { get; }
        public string DeviceType => DeviceTypes.Plc;
        public bool IsConnected => _plcInstance?.IsConnected ?? false;

        // 构造函数直接接收已创建的 IPlc
        public PlcDeviceAdapter(string name, IPlc plcInstance)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            _plcInstance = plcInstance ?? throw new ArgumentNullException(nameof(plcInstance));
        }

        public bool Connect()
        {
            if (_plcInstance.IsConnected) return true;
            try
            {
                // 缓存模式（轮询+自动重连）由 PlcRuntime 连接生命周期自管理（Connect 内部启动 IoWorker），适配层无需补刀
                return _plcInstance.Connect();
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
                _plcInstance?.Close();
            }
            catch { }
        }

        public void Dispose()
        {
            Disconnect();
            // 完整释放底层驱动（停 IoWorker/收队列/释放日志句柄），谁持有谁释放
            try { _plcInstance?.Dispose(); } catch { }
        }

        /// <summary>
        /// 通用原生驱动获取（新代码推荐，统一接口）
        /// </summary>
        public T GetRawDev<T>()
        {
            if (_plcInstance is T result)
                return result;

            throw new InvalidCastException(
                $"设备 [{Name}] 的底层驱动类型为 {_plcInstance?.GetType().Name}，无法转换为 {typeof(T).Name}");
        }
    }
}