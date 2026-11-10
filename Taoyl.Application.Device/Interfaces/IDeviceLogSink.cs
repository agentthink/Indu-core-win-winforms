using System;

namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备域日志端口：设备库零 ProjectReference（K8 边界），不能引用 LogDriver。
    /// 唯一消费点：DeviceManager.LoadFromConfigFile 的逐设备容错（单设备装配失败降级为「跳过 + 记录」，不拖垮启动）。
    /// 实现留外层（LogHelpDeviceLogSink），与 IProcessFaultSink 同构：框架定义端口，外层决定出口。
    /// </summary>
    public interface IDeviceLogSink
    {
        void Error(Exception exception, string message);
    }
}
