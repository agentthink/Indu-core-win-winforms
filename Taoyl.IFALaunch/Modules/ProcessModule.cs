// 对应类库：Taoyl.Application.Business（ProcessHosting / Eventing 子域）+ Taoyl.Application.ProcessMonitoring
using Autofac;
using Taoyl.Application.Business.Eventing;
using Taoyl.Application.Business.ProcessHosting;
using Taoyl.Application.ProcessMonitoring;
using Taoyl.Application.TaskProcess.Interfaces;
using Taoyl.Application.TaskProcess.Services;
using Taoyl.Application.Share.Eventing;
using Module = Autofac.Module;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 流程模块：上下文、协调器、注册器、控制任务、所有流程子类
    /// </summary>
    public class ProcessModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<ProcessContext>().As<IProcessContext>().SingleInstance();

            // 基座日志出口（基座零 ProjectReference，只认 IProcessLogSink 端口）
            builder.RegisterType<LogHelpProcessLogSink>().As<IProcessLogSink>().SingleInstance();

            builder.RegisterType<ProcessCoordinator>().As<IProcessCoordinator>().SingleInstance();
            builder.RegisterType<ProcessRegistry>().As<IProcessRegistry>().SingleInstance();

            // 流程故障 → 报警落库（基座不认识报警库；由 Bootstrapper 挂到 ProcessFaulted 上）
            builder.RegisterType<ProcessFaultRouter>().SingleInstance();

            // 事件总线（定义于 Core，进程内事件通道）——仍服务报警广播与 UI 弹窗，与流程基座无关
            builder.RegisterType<EventBus>().As<IEventBus>().SingleInstance();

            // 监控服务
            builder.RegisterType<MonitorService>().As<IMonitorService>().SingleInstance();

            // 所有流程子类自动注册
            // 🔴 扫描锚点 = 实现层程序集：ProcessBase 已迁入 TaskProcess 基座，
            //    锚在基座会扫出空集合（流程零注册）且不报错。
            // 🔴 ExternallyOwned 必须加：流程实现了 IProcess : IDisposable，Autofac 默认会把
            //    每个 InstancePerDependency 的 IDisposable 实例记进容器的 Disposer（强引用），
            //    直到容器 Dispose 才释放——本项目从不 Dispose 容器，整机重建每 Resolve 一次就留一个。
            //    加它 = 容器不跟踪、不代释放；生命周期归 ProcessCoordinator.ClearAllProcesses 管。
            builder.RegisterAssemblyTypes(typeof(ProcessRegistry).Assembly)
                   .Where(t => t.IsSubclassOf(typeof(ProcessBase)) && !t.IsAbstract)
                   .AsSelf()
                   .InstancePerDependency()
                   .ExternallyOwned();
        }
    }
}