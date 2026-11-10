using System.Collections.Concurrent;
using System.Collections.Generic;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.FileUtil;

namespace Taoyl.Application.Business.ProcessHosting
{
    /// <summary>
    /// 流程上下文实现（单例）。
    /// 提供子类需要的四样基础设施能力：设备管理器、报警服务、CSV 读写、跨流程共享字典。
    /// 基座（TaskProcess）不认识本类——它只认 ProcessBase 的零参数构造与故障事件。
    /// </summary>
    public class ProcessContext : IProcessContext
    {
        public IDeviceManager DeviceManager { get; }
        public IAlarmService AlarmService { get; }
        public ICsvService CsvTools { get; }
        public IDictionary<string, object> GlobalState { get; } = new ConcurrentDictionary<string, object>();

        public ProcessContext(IDeviceManager deviceManager, IAlarmService alarmService, ICsvService csvTools)
        {
            DeviceManager = deviceManager ?? throw new System.ArgumentNullException(nameof(deviceManager));
            AlarmService = alarmService ?? throw new System.ArgumentNullException(nameof(alarmService));
            CsvTools = csvTools ?? throw new System.ArgumentNullException(nameof(csvTools));
        }
    }
}
