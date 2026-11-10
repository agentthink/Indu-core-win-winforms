namespace Taoyl.Application.Device.Models
{
    /// <summary>
    /// 设备创建请求 DTO（加载器解析配置文件的产物：一条记录 = 一个待建设备实例）
    /// </summary>
    public class DeviceCreationRequest
    {
        /// <summary>设备类型标识（由实现层定义，如 "PLC"）</summary>
        public string DeviceType { get; set; }

        /// <summary>设备逻辑名称（配置文件里取到的 name 字段）</summary>
        public string DeviceName { get; set; }

        /// <summary>设备配置对象（各设备类型的具体配置模型，由工厂解释）</summary>
        public object Config { get; set; }
    }
}
