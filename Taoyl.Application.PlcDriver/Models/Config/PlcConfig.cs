using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Taoyl.Application.PlcDriver.Models.Config
{
    /// <summary>
    /// PLC设备配置（JSON 契约）
    /// 公共参数（IP/Port/超时/轮询等）+ 协议差异参数（ProtocolParams，各机型自读）
    /// JSON 键全小写，与 Devices.json 的字段名一致
    /// </summary>
    public class PlcConfig
    {
        [DisplayName("设备名称")]
        [JsonProperty("name")]
        public string Name { get; set; }

        [DisplayName("设备类型")]
        [JsonProperty("type")]
        public string Type { get; set; }

        /// <summary>
        /// 连接端点：网络协议填 IP 地址（IPv4/IPv6 字面量，不支持主机名）；串口协议填串口号（如 COM3）
        /// 串口形态的其余参数（BaudRate/DataBits/StopBits/Parity）在 ProtocolParams
        /// </summary>
        [DisplayName("IP地址/串口号")]
        [JsonProperty("ip")]
        public string Ip { get; set; }

        [DisplayName("端口号(留空=默认)")]
        [JsonProperty("port")]
        public string Port { get; set; }

        /// <summary>
        /// 日志用端点文本：Port 有值时拼为「端点:端口」，留空只显示端点（串口端点天然无端口）
        /// 只读派生属性，不参与 JSON 契约；[JsonIgnore] 防序列化回写污染 Devices.json
        /// </summary>
        [JsonIgnore]
        public string EndpointText =>
            string.IsNullOrWhiteSpace(Port) ? Ip : $"{Ip}:{Port}";

        [DisplayName("通讯协议")]
        [JsonProperty("protocol")]
        public string Protocol { get; set; }

        [DisplayName("连接超时时间")]
        [JsonProperty("ConnectTimeOut")]
        public string ConnectTimeOut { get; set; }

        [DisplayName("接收超时时间")]
        [JsonProperty("ReceiveTimeOut")]
        public string ReceiveTimeOut { get; set; }

        [DisplayName("标签配置文件路径")]
        [JsonProperty("plctagsPath")]
        public string PlcTagsPath { get; set; }

        [DisplayName("轮询间隔(ms)")]
        [JsonProperty("pollingTime")]
        public string PollingTime { get; set; }

        [DisplayName("重连次数(≤0无限)")]
        [JsonProperty("RetryConnect")]
        public string RetryConnect { get; set; }

        [DisplayName("日志目录")]
        [JsonProperty("logPath")]
        public string LogPath { get; set; } = "./Logs/PLC";

        /// <summary>
        /// 协议差异参数（键值对，各协议机型自读）
        /// 例：重连退避 ReconnectBaseDelay/ReconnectMaxDelay；西门子 PLCType/Rack/Slot 等
        /// 注意：连接/接收超时已上移为顶层属性 ConnectTimeOut/ReceiveTimeOut，本字典内的同名键不再被读取
        /// 键不区分大小写（OrdinalIgnoreCase），JSON 写 "reconnectbasedelay" 也能取到
        /// </summary>
        [DisplayName("协议差异参数")]
        [JsonProperty("protocolParams")]
        public Dictionary<string, string> ProtocolParams { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取协议差异参数
        /// </summary>
        /// <param name="key">参数键</param>
        /// <param name="defaultValue">默认值（未配置或为空时返回）</param>
        public string GetProtocolParam(string key, string defaultValue)
        {
            if (ProtocolParams != null &&
                ProtocolParams.TryGetValue(key, out var value) &&
                !string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
            return defaultValue;
        }
    }
}