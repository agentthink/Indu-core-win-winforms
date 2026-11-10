// 对应类库：Taoyl.Application.Business（Devices 子域）+ Taoyl.Application.PlcDriver
using Autofac;
using Taoyl.Application.Business.Devices;
using Taoyl.Application.Business.Devices.ConfigStore;
using Taoyl.Application.Business.Devices.Factory;
using Taoyl.Application.Business.Devices.Models;
using Taoyl.Application.Device.Config;
using Taoyl.Application.Device.ConfigLoader;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.Device.Services;
using Taoyl.Application.PlcDriver.Config;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 设备模块：注册中心、设备管理器、配置加载器、设备类型注册。
    /// </summary>
    public class DeviceModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {

            // PLC 配置管理器（保留静态单例 Instance 过渡，注入 IPlcConfigManager 走同一实例）
            builder.RegisterInstance(ConfigManager.Instance).As<IPlcConfigManager>().SingleInstance();

            // 设备工厂：按 IDeviceFactory 自动注册——新增设备类型只写工厂类，本文件零改动
            // （与 ProcessModule 的流程子类扫描同风格；工厂的构造依赖由容器自动装配）
            builder.RegisterAssemblyTypes(typeof(PlcDeviceFactory).Assembly)
                   .Where(t => !t.IsAbstract && typeof(IDeviceFactory).IsAssignableFrom(t))
                   .As<IDeviceFactory>()
                   .SingleInstance();

            // 通用 JSON 配置加载器（处理 Devices.json）：节点映射由各 IDeviceConfigSection 自己声明（ConfigNode），
            // 本文件不再手工维护第二份「节点 → 设备类型」映射表
            builder.RegisterType<JsonDeviceConfigLoader>().As<IDeviceConfigLoader>().SingleInstance();

            // 设备配置存储（通用 JSON 存储 + 本应用的根模型；.bak 备份）。存储机制在底座，根模型形状在应用
            builder.RegisterType<DeviceConfigStore<DevicesConfigFile>>().As<IDeviceConfigStore>().SingleInstance();

            // 设备配置节（类型 → Devices.json 节 的映射）：注册表仍是编译期单点（DeviceConfigSections.All），
            // 这里只是把它逐项喂进容器——设备库只认 IDeviceConfigSection，不认识 DevicesConfigFile
            foreach (var section in DeviceConfigSections.All)
                builder.RegisterInstance(section).As<IDeviceConfigSection>();

            // 驱动运行时缓存同步钩子（缓存型设备特有；PLC=缓存型，Scale/Scanner=直读型无此步）
            builder.RegisterType<PlcConfigSyncHook>().As<IDeviceConfigSyncHook>().SingleInstance();

            // 设备域日志出口（设备库零 ProjectReference，不引 LogDriver，只认 IDeviceLogSink 端口）
            builder.RegisterType<LogHelpDeviceLogSink>().As<IDeviceLogSink>().SingleInstance();

            // 设备管理器：单类三契约组合实现——实例纯域（IDeviceManager）+ 配置纯域（IDeviceConfigManager）+
            // 编排聚合（IDeviceOrchestration，UI 设备管理默认入口）。窄口隔离：进程层/配置中心等中立调用方仍可注入单域窄口
            builder.RegisterType<DeviceManager>()
                   .As<IDeviceOrchestration>()
                   .As<IDeviceManager>()
                   .As<IDeviceConfigManager>()
                   .SingleInstance();
        }
    }
}