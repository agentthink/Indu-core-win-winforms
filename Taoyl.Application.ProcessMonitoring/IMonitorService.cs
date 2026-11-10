using System.Collections.Generic;
using System.Threading.Tasks;
using Taoyl.Application.TaskProcess.Models;

namespace Taoyl.Application.ProcessMonitoring
{
    /// <summary>
    /// UI 层的监控和控制服务（门面接口）
    /// </summary>
    public interface IMonitorService
    {
        // --- 批量控制（FormMain 按钮）---
        void StartProcess();
        void PauseProcess();
        void ResumeProcess();
        void ResetProcess();
        Task InitializeProcessAsync();

        // --- 单流程控制（流程监控页面）---
        bool StartProcess(string processName);
        bool PauseProcess(string processName);
        bool ResumeProcess(string processName);
        bool ResetProcess(string processName);
        Task InitializeProcessAsync(string processName);

        // --- 监控能力 ---
        // 注：设备状态查询（设备清单 + 连接态）**不属本门面**——本接口定位"监控与控制"（流程态 + MES 信息）；
        // 设备清单属设备域，UI 直接注入 IDeviceManager 窄口读 AllDevices（只读列表，无需编排能力）。
        SystemProcessState GetSystemState();
        IReadOnlyCollection<ProcessStatus> GetAllProcessStatus();

        // --- 流程配置（运行时内存修改）---
        bool SetProcessEnabled(string processName, bool enabled);
        bool SetProcessPauseOthersOnException(string processName, bool value);
        ProcessBehaviorConfig GetProcessConfig(string processName);

        // 注：原 GetMesInfoIn / GetMesInfoOut 已删（09-14）。MES 交互日志统一走 ILogService.MES() 通道
        // （→ LogWritten 事件 → LogDisplay → rtbMESLog），队列通道已废弃。
    }
}