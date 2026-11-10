using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Taoyl.Application.Business.Devices.Adapter;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Communication;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Business.Devices.Factory
{
    public class ModBusTcpDeviceFactory : IDeviceFactory
    {
        private static readonly JsonSerializerSettings jsonSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
            // 可选：忽略不存在字段、格式化输出
            Formatting = Formatting.Indented,
            MissingMemberHandling = MissingMemberHandling.Ignore
        };
        public string DeviceType => DeviceTypes.Scale;

        public IDevice CreateDevice(string name, object config)
        {
            var configStr = JsonConvert.SerializeObject(config, jsonSettings);
            var devConfig=JsonConvert.DeserializeObject<TcpConnectionParams>(configStr, jsonSettings); // 验证配置是否正确
            if (devConfig == null)
                throw new ArgumentException($"配置参数异常，无法解析为正确配置模型");

            ModbusTcpMaster device = new ModbusTcpMaster(devConfig);

            return new ModBusTcpDeviceAdapter(name, device);

        }

        /// <inheritdoc />
        public bool TryValidateConfig(object config, out string errMsg)
        {
            try
            {
                errMsg = null;
                if (config == null)
                {
                    errMsg = "秤体配置为空";
                    return false;
                }

                // 兼容 TcpConnectionParams 对象与 JToken（loader 传节点）/ 任意可反序列化对象（如 ScaleConfig 镜像 POCO，camelCase 契约单向可转）
                var devConfig = config as TcpConnectionParams;
                if (devConfig == null)
                {
                    var configStr = JsonConvert.SerializeObject(config, jsonSettings);
                    devConfig = JsonConvert.DeserializeObject<TcpConnectionParams>(configStr, jsonSettings);
                }
                if (devConfig == null)
                {
                    errMsg = "配置无法解析为秤体连接参数";
                    return false;
                }

                if (string.IsNullOrWhiteSpace(devConfig.Ip))
                {
                    errMsg = "IP 地址不能为空";
                    return false;
                }
                if (devConfig.Port <= 0 || devConfig.Port > 65535)
                {
                    errMsg = $"端口号无效: {devConfig.Port}";
                    return false;
                }

                return true; // 校验通过
            }
            catch (Exception ex)
            {
                errMsg = $"秤体配置校验异常: {ex.Message}";
                return false;
            }
        }
    }
}
