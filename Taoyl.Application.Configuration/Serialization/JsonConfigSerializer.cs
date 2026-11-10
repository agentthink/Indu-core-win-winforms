using Newtonsoft.Json;

namespace Taoyl.Application.Configuration.Serialization
{
    /// <summary>
    /// 基于 Newtonsoft.Json 的配置序列化器实现。
    /// ObjectCreationHandling.Replace 确保反序列化时集合属性被整体替换，
    /// 而非追加到构造函数初始化的项目中。
    /// </summary>
    public class JsonConfigSerializer : IConfigSerializer
    {
        private static readonly JsonSerializerSettings settings = new JsonSerializerSettings
        {
            ObjectCreationHandling = ObjectCreationHandling.Replace,
            Formatting = Formatting.Indented,
        };

        public T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, settings);
        }

        public string Serialize<T>(T obj)
        {
            return JsonConvert.SerializeObject(obj, settings);
        }
    }
}
