// 对应类库：Taoyl.Application.Alarm
using Autofac;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Alarm.Repository;
using Taoyl.Application.Alarm.Services;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 报警模块：仓储 + 服务（均单例）。UI 与业务只注入 IAlarmService。
    /// </summary>
    public class AlarmModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<AlarmRepository>()
                   .As<IAlarmRepository>()
                   .SingleInstance();

            builder.RegisterType<AlarmService>()
                   .As<IAlarmService>()
                   .SingleInstance();
        }
    }
}
