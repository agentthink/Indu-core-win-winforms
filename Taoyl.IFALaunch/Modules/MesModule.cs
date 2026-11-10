// 对应类库：Taoyl.Application.MesDriver
using System;
using System.IO;
using Autofac;
using Taoyl.Application.Business.Mes;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.FileUtil;
using Taoyl.Application.MesDriver;
using Taoyl.Application.MesDriver.Config;
using Taoyl.Application.MesDriver.Interfaces;
using Taoyl.Application.Share.Paths;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// MES 模块：配置管理器（含快照）+ 调用客户端 + 日志出口。
    ///
    /// 网络层进程级参数（DefaultConnectionLimit / TLS 1.2 / Expect100Continue /
    /// 代理 / ConnectionLeaseTimeout）由 HttpTransport 的静态构造自持，本文件不重复设置：
    /// 基座自己管自己的网络前提，不依赖组合根记得设；且基座里用 |= 与 Math.Max，
    /// 不会覆盖其它模块已经调大的值。首次发起 Send 时静态构造即执行，仍在第一次连接之前。
    ///
    /// 三个注册都是 SingleInstance：
    /// - IMesConfigManager 必须单例——快照就是它的状态，多实例会各持一份互相看不见；
    /// - IMesClient 无状态，单例只为省一次 HttpClient 包装对象的构造；
    /// - IMesLogSink 单例由实现决定（当前无状态）。
    /// </summary>
    public class MesModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            string configPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                ConfigPathName.MesFile);

            // 构造函数需要路径字符串，Autofac 无法自动解析 → 用 lambda 注册
            builder.Register(c => new MesConfigManager(configPath))
                   .As<IMesConfigManager>()
                   .SingleInstance();

            // MES 交互明细落盘：构造需要「日志根目录」字符串，Autofac 解析不了 →
            // 与上面的 MesConfigManager 同样用 lambda 注册。
            // 🔴 根目录从 LogConfig 取（与主日志同一份），但不把配置系统塞进适配器：
            // MesLogSink 只收一个 string，不认识 Configuration。
            builder.Register(c => new MesLogSink(
                       c.Resolve<ICsvService>(),
                       c.Resolve<IConfigService>().Get<LogConfig>().RootPath))
                   .As<IMesLogSink>()
                   .SingleInstance();

            builder.RegisterType<MesClient>()
                   .As<IMesClient>()
                   .SingleInstance();
        }
    }
}
