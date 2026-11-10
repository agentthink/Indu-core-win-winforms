using Newtonsoft.Json;
using System.ComponentModel;

namespace Taoyl.Application.Business.Devices.Models
{
    /// <summary>
    /// 扫码枪设备配置（Devices.json "scanners" 节镜像 POCO）。
    /// 键对齐现场文件原样（name/Type/cmd，含大小写——整文件重写时该节键零漂移）。
    /// 目前 Scanner 无设备工厂（DeviceModule 注释留位），本类仅作配置文件镜像与未来调试界面的编辑载体。
    /// </summary>
    public class ScannerConfig
    {
        [DisplayName("设备名称")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [DisplayName("设备类型")]
        [JsonProperty("Type")]
        public string Type { get; set; }

        [DisplayName("启动命令")]
        [JsonProperty("cmd")]
        public string Cmd { get; set; }
    }
}
