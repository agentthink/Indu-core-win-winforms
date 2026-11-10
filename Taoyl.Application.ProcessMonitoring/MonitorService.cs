using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Interfaces;

namespace Taoyl.Application.ProcessMonitoring
{
    /// <summary>
    /// 监控服务实现
    /// </summary>
    public class MonitorService : IMonitorService
    {
        private readonly IProcessCoordinator coordinator;

        public MonitorService(IProcessCoordinator coordinator)
        {
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));
        }

        // --- 批量控制 ---
        public void StartProcess()=> coordinator.StartAll();
        public void PauseProcess() => coordinator.PauseAll();
        public void ResumeProcess() => coordinator.ResumeAll();
        public void ResetProcess() => coordinator.ResetAll();
        public async Task InitializeProcessAsync() =>await coordinator.InitializeAllAsync();

        // --- 单流程控制 ---

        public bool StartProcess(string processName)
        {
            var process = coordinator.GetProcess(processName);
            return process?.Start() ?? false;
        }

        public bool PauseProcess(string processName)
        {
            var process = coordinator.GetProcess(processName);
            return process?.Pause() ?? false;
        }

        public bool ResumeProcess(string processName)
        {
            var process = coordinator.GetProcess(processName);
            return process?.Resume() ?? false;
        }

        public bool ResetProcess(string processName)
        {
            var process = coordinator.GetProcess(processName);
            return process?.Reset() ?? false;
        }

        public async Task InitializeProcessAsync(string processName)
        {
            await coordinator.InitializeProcessAsync(processName);
        }

        // --- 监控 ---

        public SystemProcessState GetSystemState() => coordinator.GetSystemState();

        public IReadOnlyCollection<ProcessStatus> GetAllProcessStatus()
        {
            return coordinator.Processes
                .Select(p => p.GetStatusSnapshot())
                .ToList()
                .AsReadOnly();
        }

        // --- 流程配置 ---

        public bool SetProcessEnabled(string processName, bool enabled)
        {
            var process = coordinator.GetProcess(processName);
            if (process == null) return false;
            process.BehaviorConfig.IsEnabled = enabled;
            return true;
        }

        public bool SetProcessPauseOthersOnException(string processName, bool value)
        {
            var process = coordinator.GetProcess(processName);
            if (process == null) return false;
            process.BehaviorConfig.PauseOthersOnException = value;
            return true;
        }

        public ProcessBehaviorConfig GetProcessConfig(string processName)
        {
            return coordinator.GetProcess(processName)?.BehaviorConfig;
        }

    }
}