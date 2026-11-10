namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备工厂抽象：每种设备类型实现一个具体工厂
    /// </summary>
    public interface IDeviceFactory
    {
        /// <summary>设备类型标识（由实现层定义，与工厂/适配器声明的取值一致）</summary>
        string DeviceType { get; }

        /// <summary>
        /// 根据名称和配置创建设备（不负责连接）
        /// </summary>
        /// <param name="name">设备逻辑名称（与配置里的名称字段一致）</param>
        /// <param name="config">
        /// 设备配置对象。<b>⚠️ 有两种形态，实现必须都能处理：</b>
        /// ① 启动装配路径 = IDeviceConfigLoader 解析出的 JToken（原始节点，未定型）；
        /// ② 编辑保存路径 = 调用方传入的具体配置对象（如 PlcConfig）。
        /// 直接强转 <c>(MyConfig)config</c> 会在 ① 上抛 InvalidCastException，且只在重启装配时暴露。
        /// 统一做法：<c>config as MyConfig ?? JsonConvert.DeserializeObject&lt;MyConfig&gt;(JsonConvert.SerializeObject(config))</c>。
        /// </param>
        IDevice CreateDevice(string name, object config);

        /// <summary>
        /// 校验设备配置（与 CreateDevice 同源校验，预检通过 = CreateDevice 必成功——供配置管理编排在
        /// 释放/构建实例前预检，规避 BuildDevice 非事务的"旧释放后新建失败"风险）。
        /// </summary>
        /// <param name="config">设备配置对象</param>
        /// <param name="errMsg">校验失败原因（通过时置 null；UI 可直接展示）</param>
        /// <returns>true=校验通过；false=校验失败（errMsg 携带原因）</returns>
        bool TryValidateConfig(object config, out string errMsg);
    }
}
