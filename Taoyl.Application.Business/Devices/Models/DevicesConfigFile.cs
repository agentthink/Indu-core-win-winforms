using Newtonsoft.Json;
using System.Collections.Generic;
using Taoyl.Application.PlcDriver.Models.Config;

namespace Taoyl.Application.Business.Devices.Models
{
    /// <summary>
    /// 设备配置文件（Devices.json）全量强类型镜像。
    /// 三节与现场文件节点一一对应：PLCs（驱动运行时依赖，PlcConfig 现成）/ scanners（无 factory，仅防丢键镜像）/ scales（ModBus 秤，仅防丢键镜像）。
    /// 用途：DeviceConfigStore 内存持有本模型，DeviceManager 编排在锁内增删改节后 Save 落盘。
    /// ⚠️ 边界：模型化后保存 = 整文件重写，只认模型内键——模型外手写键保存即丢（当前现场键已被三模型全覆盖）。
    /// ⚠️ 行为：PLCs 节序列化后键归 PlcConfig 契约（camelCase），与原现场 Pascal 键风格不同——反序列化大小写不敏感，功能零影响（.bak 兜底）。
    /// </summary>
    public class DevicesConfigFile
    {
        /// <summary>PLC 设备节（键保留现场大写 "PLCs"）</summary>
        [JsonProperty("PLCs")]
        public List<PlcConfig> PLCs { get; set; } = new List<PlcConfig>();

        /// <summary>扫码枪设备节（键保留现场小写 "scanners"；Scanner 暂无 factory，模型化仅防重写丢键）</summary>
        [JsonProperty("scanners")]
        public List<ScannerConfig> Scanners { get; set; } = new List<ScannerConfig>();

        /// <summary>秤体设备节（键保留现场小写 "scales"；创建仍走 ModBusTcpDeviceFactory 的 TcpConnectionParams 单向转换）</summary>
        [JsonProperty("scales")]
        public List<ScaleConfig> Scales { get; set; } = new List<ScaleConfig>();
    }
}
