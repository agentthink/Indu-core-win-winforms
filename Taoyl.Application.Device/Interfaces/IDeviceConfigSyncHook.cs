namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 驱动运行时缓存同步钩子：设备类型特有的「配置 → 驱动缓存」段。
    /// 背景：PLC 是缓存型设备（工厂 CreateDevice 经同名缓存短路取最新配置），配置变更必须先同步驱动缓存；
    /// Scale/Scanner 是直读型，无此步。原实现是 DeviceManager 里的 `if (deviceType == PLC)` 单分支
    /// 加对 IPlcConfigManager 的直接依赖——设备库因此绑死 PlcDriver，无法独立成零引用库。
    /// 现收敛为端口：设备库按注册顺序问 CanHandle，命中就调 Sync/Clear；具体实现（PlcConfigSyncHook）留在实现层。
    /// 出现第二个缓存型设备时各加一个实现即可，设备库零改动（第二实例原则的落地形态）。
    /// </summary>
    public interface IDeviceConfigSyncHook
    {
        /// <summary>本钩子是否处理该设备类型（OrdinalIgnoreCase 比较）</summary>
        bool CanHandle(string deviceType);

        /// <summary>配置新增/更新：同步驱动运行时缓存（纯内存写，不落盘）</summary>
        void Sync(string deviceType, object config);

        /// <summary>配置删除/改名：清理驱动运行时缓存。返回是否命中任何清理动作</summary>
        bool Clear(string deviceType, string name);
    }
}
