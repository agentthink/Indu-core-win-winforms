using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.Device.Models;

namespace Taoyl.Application.Device.ConfigLoader
{
    /// <summary>
    /// 通用 JSON 设备配置加载器。
    /// 节点映射不再手工维护：直接问每个 IDeviceConfigSection「你的节点名是什么」（ConfigNode），
    /// 名称也由该节从绑定后的配置对象里取——新增设备类型只声明一次，不存在第二份映射表。
    /// 支持：单文件多节点；节点为数组（多条请求）或对象（单条请求）。
    /// </summary>
    public class JsonDeviceConfigLoader : IDeviceConfigLoader
    {
        private readonly IReadOnlyList<IDeviceConfigSection> _sections;

        public JsonDeviceConfigLoader(IEnumerable<IDeviceConfigSection> sections)
        {
            if (sections == null) throw new ArgumentNullException(nameof(sections));
            _sections = new List<IDeviceConfigSection>(sections).AsReadOnly();
        }

        /// <inheritdoc />
        public bool CanLoad(string filePath)
        {
            return Path.GetExtension(filePath).Equals(".json", StringComparison.OrdinalIgnoreCase);
        }

        /// <inheritdoc />
        public IEnumerable<DeviceCreationRequest> Load(string filePath)
        {
            var json = JObject.Parse(File.ReadAllText(filePath));

            foreach (var section in _sections)
            {
                var token = json.SelectToken(section.ConfigNode);
                if (token == null)
                    continue;

                if (token is JArray array)
                {
                    foreach (var item in array)
                        yield return CreateRequest(item, section);
                }
                else if (token is JObject)
                {
                    yield return CreateRequest(token, section);
                }
            }
        }

        /// <summary>
        /// 一条节点 → 一个创建请求。
        /// 名称：把节点绑定成该类型的配置对象后由节自己取（大小写不敏感由绑定保证）；
        ///       取不到（缺名称字段 / 形状不符）→ GUID 兜底（与原策略「精确 → 大小写不敏感 → GUID」等价）。
        /// Config 仍传原始 JToken：工厂拿到文档原样，不经镜像 POCO 二次转换，避免字段丢失。
        /// </summary>
        private DeviceCreationRequest CreateRequest(JToken token, IDeviceConfigSection section)
        {
            string name = null;
            try
            {
                name = section.GetName(token.ToObject(section.ConfigType));
            }
            catch
            {
                // 形状不符 → 交给 GUID 兜底；建实例失败由 DeviceManager.LoadFromConfigFile 的逐设备容错记录并跳过
            }

            if (string.IsNullOrEmpty(name))
                name = Guid.NewGuid().ToString("N");

            return new DeviceCreationRequest
            {
                DeviceType = section.DeviceType,
                DeviceName = name,
                Config = token
            };
        }
    }
}
