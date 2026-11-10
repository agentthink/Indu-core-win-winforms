using System;
using System.ComponentModel;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.UI.Pages.Models
{
    /// <summary>
    /// 设备列表通用行（纯 POCO 无 INotify：连接状态由 VM RefreshDeviceRows 节拍写 IsConnected + ResetItem 行级刷新；
    /// 持 IDevice 引用仅供名称与构造初值，不长期用于驱动调用——重建/移除后旧引用失效，VM 每次现取编排口。
    /// 09-09 决策：零 PLC 依赖通用模型（QueueDepth 等设备专属运行指标不入本行，走 VM 节拍现取 + 页面顶栏 label 展示，
    /// 本行可复用于后续 Scanner/Scale 等设备列表）
    /// </summary>
    public class DeviceRow
    {
        /// <summary>连接状态列文本（已连接/已断开）——ConnectionText 产出与 View 上色判定同源，改文案只改此处</summary>
        public const string ConnectionOkText = "已连接";
        public const string ConnectionLostText = "已断开";

        private readonly IDevice device;

        public DeviceRow(IDevice device)
        {
            this.device = device ?? throw new ArgumentNullException(nameof(device));
            IsConnected = device.IsConnected; // 首帧真值（免首开 1s 显示假断开；此后由 VM RefreshDeviceRows 现取刷新）
        }

        /// <summary>连接状态（VM RefreshDeviceRows 现取编排口更新；行级 ResetItem 通知 DGV）</summary>
        public bool IsConnected { get; internal set; }

        /// <summary>连接状态列文本（绑定显示；View CellFormatting 按本行状态上色，文本与色同拍）</summary>
        [Description("连接状态")]
        public string ConnectionText => IsConnected ? ConnectionOkText : ConnectionLostText;

        /// <summary>实例名（[Description] 供 DGV 列头反射）</summary>
        [Description("设备名称")]
        public string Name => device.Name;
    }
}
