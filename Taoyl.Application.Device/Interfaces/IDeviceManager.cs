using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Taoyl.Application.Device.Models;

namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备管理器（实例纯域接口）：统一管理所有硬件设备运行实例——构建/获取/移除/连接。
    /// 语义边界：本接口只谈运行实例（注册表内 IDevice），不触碰任何配置文件；
    /// 配置文件（Devices.json）的载入/增/改/删在 IDeviceConfigManager（配置纯域）与编排启动用例
    /// （IDeviceOrchestration.LoadFromConfigFile）；
    /// "配置→实例联动"的业务用例（新增设备/保存生效/删除设备/启动装配）在 IDeviceOrchestration（编排聚合口，UI 默认入口）。
    /// ⚠️ 引用生命周期：BuildDevice/RemoveDevice 会销毁旧实例，之后旧 IDevice 引用即失效（已 Dispose）。
    /// 长期持有实例引用的调用方有两种做法：
    ///   ① 每次现取（GetDevice 是一次字典查找，成本可忽略）——推荐；
    ///   ② 订阅 <see cref="DeviceChanged"/>，变更时自行重取（本接口只提供通知，不强制订阅）。
    /// 🔴 无论哪种做法，都不存在「跨重建永久有效的实例引用」——实例会被 Dispose，这是重建语义本身决定的。
    /// </summary>
    public interface IDeviceManager
    {
        T GetDevice<T>(string name) where T : class, IDevice;
        IDevice GetDevice(string name);
        IReadOnlyList<IDevice> AllDevices { get; }

        /// <summary>
        /// 设备变更通知：实例**新增 / 替换（重建）/ 移除**完成后触发。
        ///
        /// 定位：给「长期持有设备引用」的调用方一个失效信号——收到通知后自行重新 GetDevice。
        /// 库只提供能力，订阅与否由使用方决定；不订阅 = 回到「每次现取」的做法。
        ///
        /// 约定：
        /// ① 载荷只含「类型 + 名字 + 种类」，不含 IDevice 实例（旧实例已 Dispose，新实例不该被缓存）；
        /// ② **锁外触发**：不会在持有设备锁时回调，处理器可以安全调用 GetDevice；
        /// ③ 逐个订阅方异常隔离：某个处理器抛异常不影响设备操作本身，异常经 IDeviceLogSink 记录；
        /// ④ 在**触发方线程**上执行（UI 保存 = 工作线程；启动装配 = 启动线程），需要 UI 线程请自行 marshal；
        /// ⑤ 处理器内**不要**再调用 BuildDevice / RemoveDevice / UpdateDevice / DeleteDevice（会再产生变更）。
        /// </summary>
        event EventHandler<DeviceChangedEventArgs> DeviceChanged;

        /// <summary>
        /// 构建（登记）指定设备——实例 upsert 唯一入口：先用配置创建新实例，成功后完成键切换（摘旧键 → 占位新 → 释放旧），
        /// 并放回管理器。无旧实例 = 首次构建（编排 AddDevice 尾部）；有旧实例 = 重建（保存生效/调试「重建」键）。
        /// ⚠️ 实例配置源 = 驱动缓存（PLC：工厂 CreateDevice 经同名缓存短路取最新，并非入参 config 直接生效）：
        /// 修改配置后的"保存生效"必须先经编排（IDeviceOrchestration.UpdateDevice 内部先同步缓存再构建），
        /// 直接调本方法传新 config 会因缓存短路建出旧配置实例（静默错误）。本方法是实例创建的唯一入口，不新增裸 CreateDevice。
        /// ⚠️ 事务序（先建新、成功才换旧）：CreateDevice 抛异常时旧实例与实例表均未被触碰——「失败零副作用」为结构保证，
        /// 调用方无须预先 TryValidateConfig 兜底（该预检仅为锁外 UX 前置提示，不是正确性依赖）。
        /// 同名 upsert 落在同一个字典键（容器装不下两个同名设备），故键切换与 Dispose 解耦：摘旧键后立刻占位、
        /// 释放旧实例留到最后，避免键在 IoWorker 退出（StopIoWorker 上限 2s）期间空窗。
        /// 注意：构建/重建后旧 IDevice 引用失效，持有方必须重新 GetDevice（将返回 null）。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="name">设备名称</param>
        /// <param name="config">设备配置对象（缓存已为最新时的兜底入参）</param>
        /// <returns>构建后的设备实例</returns>
        IDevice BuildDevice(string deviceType, string name, object config);

        /// <summary>
        /// 按设备类型获取设备集合（如 "PLC"）
        /// </summary>
        IReadOnlyList<IDevice> GetDevicesByType(string deviceType);

        /// <summary>
        /// 移除指定设备：锁内从管理器移除并完整释放实例（含底层驱动完整释放链，与 BuildDevice 释放段一致）。
        /// 仅移除运行实例，不触碰任何配置文件——纯实例移除（配置项删除走 IDeviceConfigManager.RemoveConfig；
        /// 业务删除"配置+实例+缓存一并"走 IDeviceOrchestration.DeleteDevice，UI 默认后者）。
        /// 注意：移除后旧 IDevice 引用失效，持有方必须重新 GetDevice（将返回 null）。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="name">设备名称</param>
        /// <returns>是否成功移除（false=设备不存在或参数无效）</returns>
        bool RemoveDevice(string deviceType, string name);

        Task<bool> ConnectAllAsync();
        Task DisconnectAllAsync();
    }
}
