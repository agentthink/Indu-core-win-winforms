using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Taoyl.Application.TaskProcess.Models;

namespace Taoyl.Application.TaskProcess.Interfaces
{
    /// <summary>
    /// 协调器公开接口（流程基座对外的编排面）。
    /// 基座内实现 = ProcessCoordinator；注册器（IProcessRegistry）不在基座内——它要认识具体流程类。
    /// </summary>
    public interface IProcessCoordinator
    {
        void Register(IProcess process);
        void StartAll();
        void PauseAll();
        void ResumeAll();
        void ResetAll();
        Task StopAllAsync();

        /// <summary>初始化单个流程（轻量级重置，保留实例）</summary>
        Task InitializeProcessAsync(string processName);

        /// <summary>初始化所有流程（彻底重建，重新Resolve实例）</summary>
        Task InitializeAllAsync();

        /// <summary>
        /// 清空并释放所有流程（生命周期终点：逐个 Dispose 后清表）。
        /// 🔴 调用前必须已 StopAllAsync——活线程上释放会留下「订阅已退、looper 还在跑」的僵尸。
        /// 整机重建请走 InitializeAllAsync（内部已按此顺序调用），不要单独调本方法。
        /// </summary>
        void ClearAllProcesses();

        /// <summary>根据名称获取流程实例</summary>
        IProcess GetProcess(string name);

        IReadOnlyCollection<IProcess> Processes { get; }
        SystemProcessState GetSystemState();

        /// <summary>
        /// 流程故障出口（基座对外唯一）。
        /// 触发时机：任一流程经状态机进入 Exception（ProcessBase.Faulted → 本事件），
        /// 以及协调器自身操作失败（E901 暂停流程失败 / E902 批量操作失败）。
        /// 外层订阅后转成报警落库（弹窗 / 查询页）；基座不引报警库，故经此事件交付。
        /// 约定：在触发方线程上同步派发；订阅方异常由基座隔离，不影响流程状态机。
        /// </summary>
        event EventHandler<ProcessFault> ProcessFaulted;

        /// <summary>
        /// 全量重建请求（原 EventBus 事件 → 普通委托，基座不引总线）。
        /// 由外层注册器订阅：收到后重新从容器 Resolve 全部流程并 Register。
        /// 订阅方异常由协调器隔离（不影响 InitializeAllAsync 的收尾日志）。
        /// </summary>
        event Action ProcessesNeedRebuild;
    }
}
