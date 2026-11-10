using System.Collections.Generic;

namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备配置管理器（配置纯域接口）：只负责设备配置（Devices.json 镜像节）的 查/增/改/删 + 落盘(.bak)。
    /// 语义边界（与 IDeviceManager / IDeviceOrchestration 的能力划分）：
    /// - 本接口 = 配置纯域：增/改/删只动配置文件节 + 落盘，**不创建/释放/重建任何实例**（与运行时解耦）。
    /// - IDeviceManager = 实例纯域：创建/移除/获取/连接（与配置解耦）。
    /// - IDeviceOrchestration = 组合编排口（继承本接口 + IDeviceManager）："按配置新增/保存生效/删除设备"
    ///   这类配置→实例联动的业务用例。UI 设备管理页面向编排口；本窄口供配置中心等只想要配置能力的中立调用方。
    /// 签名全 object/string + 泛型 T——本契约层零具体设备类型依赖（T 由调用方携带）。
    /// 实现：DeviceManager 单类多契约组合实现（见 IDeviceOrchestration 头注）。
    /// 注意：本接口操作的是磁盘配置（Devices.json），不含实例副作用。
    /// </summary>
    public interface IDeviceConfigManager
    {
        /// <summary>
        /// 按设备配置类型读取磁盘态配置列表（镜像：启动 LoadFromConfigFile 载入，增/改/删同步更新）。
        /// T = 具体设备配置类型（PLC→PlcConfig / Scanner→ScannerConfig / Scale→ScaleConfig），
        /// 按 typeof(T) 映射对应配置文件节；未知 T 抛 NotSupportedException。
        /// </summary>
        /// <typeparam name="T">设备配置类型</typeparam>
        IReadOnlyList<T> GetDeviceConfigs<T>() where T : class;

        /// <summary>
        /// 新增配置节项并落盘(.bak)。只动配置节，不创建实例（实例创建走编排口 AddDevice）。
        /// 配置节内同名视为失败（抛异常）；与实例表不查重（那是编排口跨域职责）。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="config">设备配置对象</param>
        void AddConfig(string deviceType, object config);

        /// <summary>
        /// 更新配置节项并落盘(.bak)（改名/编辑）。只动配置节，不重建实例（实例重建走编排口 UpdateDevice）。
        /// oldName 非空且与 config.Name 不同 = 改名（先删旧节项再按新名追加）；oldName 为空或同名 = 原位更新。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="oldName">原设备名（新增/同名编辑传 null）</param>
        /// <param name="config">编辑后的完整配置对象（含新 Name）</param>
        void UpdateConfig(string deviceType, string oldName, object config);

        /// <summary>
        /// 删除配置节项并落盘(.bak)。只动配置节，不释放实例（实例释放走 IDeviceManager.RemoveDevice 或编排口 DeleteDevice）。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="name">设备名称</param>
        /// <returns>是否命中并删除（false=配置节无此项）</returns>
        bool RemoveConfig(string deviceType, string name);
    }
}
