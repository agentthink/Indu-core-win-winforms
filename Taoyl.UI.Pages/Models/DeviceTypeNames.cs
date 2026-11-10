namespace Taoyl.UI.Pages.Models
{
    /// <summary>
    /// UI 侧用到的设备类型标识（展示层自用，不引用实现层）。
    /// 只用于「这个页面管的是哪种设备」与「类型 → 展示名」的映射；
    /// 取值是磁盘契约，必须与 Devices.json 的 type 字段一致。
    /// 🔴 为什么 UI 也有一份：设备类型名录属于应用/展示层的词汇，底座只把它当不透明字符串；
    ///    若让 UI 去引实现层常量，就破了「UI 零引用实现层」的边界（§6.2）。
    /// </summary>
    public static class DeviceTypeNames
    {
        public const string Plc = "PLC";
        public const string Scanner = "Scanner";
        public const string Scale = "Scale";
    }
}
