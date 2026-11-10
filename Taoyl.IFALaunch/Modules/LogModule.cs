// 对应类库：Taoyl.Application.LogDriver
using Autofac;
using Taoyl.Application.LogDriver;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.LogDriver.Services;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 日志模块：注册日志服务与 UI 日志显示（均为共享单例）
    /// </summary>
    public class LogModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // 日志服务（共享单例：与 LogHelp 静态转发、LogForm 事件订阅同一实例）
            builder.Register(c => NLogLogService.Instance)
                   .As<ILogService>()
                   .SingleInstance();

            // UI 日志显示（单例，内部订阅 ILogService.LogWritten 事件）
            builder.Register(c => LogForm.Instance)
                   .As<ILogDisplay>()
                   .SingleInstance();
        }
    }
}
