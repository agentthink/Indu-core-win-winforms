using System;
using System.Collections;

namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备配置节契约：把「设备类型 → 配置文件里的哪个列表」这条知识从编排方手里拿走。
    /// 编排方（DeviceManager）只认本接口 + 根模型 object，不认识具体根模型，也不认识任何具体配置模型；
    /// 具体映射由 DeviceConfigSection&lt;TModel&gt;（泛型实现，见 Services）+ 应用层注册表给出。
    /// 🔴 根模型与 config 一律 object：设备库对磁盘模型零知识（K9——不改磁盘格式，只把接口类型退化）。
    /// </summary>
    public interface IDeviceConfigSection
    {
        /// <summary>设备类型标识（由实现层定义，如 "PLC"）</summary>
        string DeviceType { get; }

        /// <summary>
        /// 配置文件中的节点名（JSON 键 / Excel 表名，如 "PLCs"）。
        /// 加载器据此把文档节点分派给本类型——声明一次，不再在组合根手工维护第二份映射表。
        /// </summary>
        string ConfigNode { get; }

        /// <summary>设备配置 CLR 类型（PlcConfig / ScannerConfig / ScaleConfig…）</summary>
        Type ConfigType { get; }

        /// <summary>取根模型的对应节列表（节属性已初始化，非空）</summary>
        IList GetList(object model);

        /// <summary>校验 config 与本设备类型匹配并取配置名（类型不符抛自解释 ArgumentException）</summary>
        string GetName(object config);

        /// <summary>节内按名称查是否存在（Ordinal，与存储原值比较）</summary>
        bool Contains(object model, string name);

        /// <summary>节内 upsert：同名命中原位替换（保列表顺序稳定）；未命中追加</summary>
        void Upsert(object model, object config);

        /// <summary>节内按名称移除（返回是否命中）</summary>
        bool Remove(object model, string name);
    }
}
