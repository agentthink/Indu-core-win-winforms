using System;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.LogDriver;

namespace Taoyl.Application.Business.Devices
{
    /// <summary>IDeviceLogSink 的 LogDriver 实现（实现层绑定，设备库只认端口）。</summary>
    public sealed class LogHelpDeviceLogSink : IDeviceLogSink
    {
        /// <inheritdoc />
        public void Error(Exception exception, string message)
        {
            LogHelp.Error(exception, message);
        }
    }
}
