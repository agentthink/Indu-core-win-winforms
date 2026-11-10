using System;

namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 所有工控设备的基础接口
    /// </summary>
    public interface IDevice : IDisposable
    {
        /// <summary>
        /// 设备唯一逻辑名称（如 "PLC1_Reader"）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 设备类型描述，用于 UI 展示
        /// </summary>
        string DeviceType { get; }

        /// <summary>
        /// 是否已连接并可用
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 建立连接。如果已连接，可重新连接。
        /// </summary>
        bool Connect();

        /// <summary>
        /// 关闭连接，释放资源
        /// </summary>
        void Disconnect();

        /// <summary>
        /// 获取底层原生驱动实例。
        /// 泛型方法避免返回值装箱，支持任意目标类型。
        /// </summary>
        /// <typeparam name="T">目标驱动类型</typeparam>
        /// <exception cref="InvalidCastException">当底层驱动无法转换为 T 时抛出</exception>
        T GetRawDev<T>();
    }
}