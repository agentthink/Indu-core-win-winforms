using System.Collections.Generic;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.FileUtil;

namespace Taoyl.Application.Business.ProcessHosting
{
    /// <summary>
    /// 流程上下文：注入给流程子类，作为子类获取「基础设施能力」的唯一入口。
    ///
    /// 位置：留在实现层，不能上移基座——它暴露 IDeviceManager / IAlarmService / ICsvService，
    /// 上移会让零引用的 TaskProcess 被迫引用 Device / Alarm / FileUtil（基座不认识这些）。
    ///
    /// 🔴 成员准入规则（10-18 定，防它长成上帝容器）：
    /// 只收「基础设施级能力」——设备、报警、CSV、跨流程共享字典。
    /// 业务级能力（数据库、MES HTTP、视觉、业务文件格式）**不进上下文**：那是具体流程自己的依赖，
    /// 由子类自行注入。判据：只有某一个流程用得上的东西，就不该在这里。
    /// </summary>
    public interface IProcessContext
    {
        /// <summary>
        /// 设备管理器。长期持引用的两种做法（详见 IDeviceManager 注释）：
        /// ① 每步现取；② 缓存 + 订阅 <c>DeviceChanged</c> 命中即置空 + <c>OnDispose</c> 退订。
        /// 两种都不存在「跨重建永久有效的实例引用」——重建即 Dispose 旧实例。
        /// </summary>
        IDeviceManager DeviceManager { get; }

        /// <summary>报警服务（子类业务报警直调 Raise，不经状态机门控；框架故障走基座故障出口）</summary>
        IAlarmService AlarmService { get; }

        /// <summary>CSV 读写</summary>
        ICsvService CsvTools { get; }

        /// <summary>
        /// 跨流程共享字典（线程安全）。
        /// 有意保留：设计初即作为跨流程共享状态位使用。
        /// ⚠️ 键名自定、无编译期约束——跨流程协作优先走事件，确需共享状态时才用这里。
        /// </summary>
        IDictionary<string, object> GlobalState { get; }
    }
}
