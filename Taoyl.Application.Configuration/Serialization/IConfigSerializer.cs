namespace Taoyl.Application.Configuration.Serialization
{
    /// <summary>
    /// 配置序列化/反序列化抽象接口。
    /// </summary>
    public interface IConfigSerializer
    {
        /// <summary>
        /// 将 JSON 字符串反序列化为指定类型的对象。
        /// </summary>
        T Deserialize<T>(string json);

        /// <summary>
        /// 将指定对象序列化为 JSON 字符串。
        /// </summary>
        string Serialize<T>(T obj);
    }
}
