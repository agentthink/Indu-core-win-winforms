using Newtonsoft.Json;
using System.ComponentModel;

namespace Taoyl.Application.Business.Devices.Models
{
    /// <summary>
    /// 秤体（ModBus）设备配置（Devices.json "scales" 节镜像 POCO）。
    /// 键对齐现场文件原样（name/Type/ip/port/DataFormat/LogPathTemplate，含大小写——整文件重写时该节键零漂移）。
    /// Port 保持 string（现场文件即字符串 "502"，防强类型化引入反序列化差异）。
    /// 设备创建仍走 ModBusTcpDeviceFactory：本对象 → camelCase 序列化 → TcpConnectionParams（Communication 层）单向转换，字段已证覆盖。
    /// </summary>
    public class ScaleConfig
    {
        [DisplayName("设备名称")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [DisplayName("设备类型")]
        [JsonProperty("Type")]
        public string Type { get; set; }

        [DisplayName("IP地址")]
        [JsonProperty("ip")]
        public string Ip { get; set; }

        [DisplayName("端口号")]
        [JsonProperty("port")]
        public string Port { get; set; }

        [DisplayName("数据格式")]
        [JsonProperty("DataFormat")]
        public string DataFormat { get; set; }

        [DisplayName("报文日志路径模板")]
        [JsonProperty("LogPathTemplate")]
        public string LogPathTemplate { get; set; }
    }
}
