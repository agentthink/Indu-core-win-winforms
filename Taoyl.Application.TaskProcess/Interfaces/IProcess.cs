using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Taoyl.Application.TaskProcess.Models;

namespace Taoyl.Application.TaskProcess.Interfaces
{
    /// <summary>
    /// 流程契约。
    ///
    /// 🔴 继承 IDisposable：流程是**可释放资源**。整机重建（ClearAllProcesses）会丢弃实例并重新 Resolve，
    ///    实例若订阅过外部事件（设备变更 / 总线 / 定时器）而不退订，就会被订阅源强引用、永不回收。
    ///    Dispose 是唯一的释放时机——子类在 ProcessBase.OnDispose 里退订。
    ///
    /// 调用约定：**Dispose 前必须已 StopAsync**。活线程上释放 = 退了订阅但 looper 还在跑（僵尸态），
    /// 协调器 ClearAllProcesses 已按「先 StopAllAsync 再释放」实现，不要单独调。
    /// </summary>
    public interface IProcess : IDisposable
    {
        string ProcessName { get; }
        int CurrentStep { get; }

        /// <summary>流程状态（单层五态，线程生死即状态；锁内取值，与状态机运行时同源）</summary>
        ProcessState State { get; }

        ProcessBehaviorConfig BehaviorConfig { get; }
        ProcessCategory Category { get; }
        bool Start();
        bool Pause();
        bool Resume();
        bool Reset();
        Task<bool> StopAsync();
        Task<bool> InitializeAsync();
        ProcessStatus GetStatusSnapshot();
    }
}
