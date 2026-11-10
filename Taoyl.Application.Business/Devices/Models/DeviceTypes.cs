namespace Taoyl.Application.Business.Devices.Models
{
    /// <summary>
    /// 本应用的设备类型标识常量（PLC / Scanner / Scale）。
    /// 🔴 为什么在本层而不在底座：设备类型名录属于「这个现场有哪些设备」，不属于设备域契约。
    ///    底座把 DeviceType 当**不透明字符串**（IDeviceFactory.DeviceType / IDevice.DeviceType），从不判断取值；
    ///    新增视觉 / 运控 / 其他设备时，在本文件加一行即可，底座与 DeviceManager 零改动。
    /// ⚠️ 取值是磁盘契约：必须与 Devices.json 的 type 字段、工厂 DeviceType、适配器 DeviceType 一致。
    /// </summary>
    public static class DeviceTypes
    {
        /// <summary>PLC（欧姆龙/西门子/Modbus 等，走 PlcDeviceFactory）</summary>
        public const string Plc = "PLC";

        /// <summary>扫码枪（当前无工厂，仅配置镜像）</summary>
        public const string Scanner = "Scanner";

        /// <summary>秤体（ModBus，走 ModBusTcpDeviceFactory）</summary>
        public const string Scale = "Scale";
    }
}
