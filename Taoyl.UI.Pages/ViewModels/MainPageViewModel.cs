using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.UI.Pages.Models;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 主页面ViewModel
    /// 承担FormMainPage的所有业务逻辑
    /// </summary>
    public partial class MainPageViewModel : ObservableObject
    {
        #region 依赖服务
        /// <summary>
        /// 设备管理器**窄口**（只读设备清单 AllDevices）。
        /// ⚠️ 刻意不用宽口 IDeviceOrchestration：本页只需"看有哪些设备"，
        /// 增删改（AddDevice/UpdateDevice/DeleteDevice）与启动装配（LoadFromConfigFile）一概不用
        /// ⇒ 按 IDeviceOrchestration「单域能力用窄口」约定注入窄口，避免宽口退化成上帝接口。
        /// （需跨域"配置→实例联动"的页面如 PlcDebugViewModel 才注入宽口。）
        /// </summary>
        private readonly IDeviceManager deviceManager;
        private readonly ILogService logService;
        #endregion

        #region 可观察属性
        /// <summary>
        /// 设备状态行（分组化）。BindingList：结构变化走 ResetBindings（→ ListChanged.Reset），
        /// 连接态翻转走 ResetItem(索引)（→ ListChanged.ItemChanged，View 只改该行图标）。
        /// 变更语义载体是 BindingList 本身，View 不持有结构快照（单一真值）。
        /// </summary>
        [ObservableProperty]
        private BindingList<DeviceStatusItem> deviceStatuses = new BindingList<DeviceStatusItem>();

        [ObservableProperty]
        private bool isRefreshing;
        #endregion

        #region 设备分组定义

        /// <summary>
        /// 设备类型 → 展示组名（3b 方案：保留原观感 + 新类型兜底）。
        /// 未登记的类型回退显示**原始类型名**（不丢组）；分组维度 = 设备管理器中实际存在的类型
        /// （从 IDevice.DeviceType 派生，非硬编码清单）。
        /// </summary>
        private static readonly Dictionary<string, string> DeviceTypeDisplayNames =
            new Dictionary<string, string>(StringComparer.Ordinal)
            {
                [DeviceTypeNames.Plc] = "PLC连接状态",
                [DeviceTypeNames.Scanner] = "扫码枪连接状态",
                [DeviceTypeNames.Scale] = "秤体连接状态",
            };

        /// <summary>类型名 → 展示组名（未登记回退原始类型名）</summary>
        private static string GetGroupDisplayName(string deviceType)
        {
            return DeviceTypeDisplayNames.TryGetValue(deviceType, out var displayName)
                ? displayName
                : deviceType;
        }

        #endregion

        #region 构造函数
        public MainPageViewModel(IDeviceManager deviceManager, ILogService logService)
        {
            this.deviceManager = deviceManager ?? throw new ArgumentNullException(nameof(deviceManager));
            this.logService = logService ?? throw new ArgumentNullException(nameof(logService));
        }
        #endregion

        #region 命令
        /// <summary>
        /// 刷新设备状态（View 250ms 心跳调）：从设备管理器现取实例 → 按类型分组 → 交 SyncDeviceStatuses 做增量同步。
        /// 分组维度 = 实际存在的设备类型（非硬编码清单）；组内按设备名 Ordinal 排序，组间按类型名 Ordinal 排序
        /// （都取稳定序，避免字典序抖动导致"结构误判为变化"→ 无谓重建）。
        /// ⚠️ 只展示**有运行时实例**的设备：无实例工厂的类型（如 Scanner，DeviceManager 装配注释"仅镜像不建实例"）
        /// 不出现——"设备连接状态"面板只能展示有连接概念的实例。这不是缺陷，是数据源的真实边界。
        /// 数据源走 IDeviceManager.AllDevices 直读（不经监控服务中转）：设备清单属设备域，
        /// 曾有 IMonitorService.GetAllDeviceStatus() 把同一批 IDevice 投影成 DeviceStatus 副本再交 UI，
        /// 属纯冗余搬运（已删）；连带本 VM 对 IMonitorService 的依赖一并摘除（构造 3 → 2 参）。
        /// AllDevices 每次访问新建列表壳，但元素是复用实例、无对象分配。
        /// </summary>
        [RelayCommand]
        private void RefreshDeviceStatus()
        {
            IsRefreshing = true;
            try
            {
                // 单次取列表（每次访问都新建列表壳，勿在 LINQ 里重复枚举）
                var devices = deviceManager.AllDevices;

                var newData = devices
                    .GroupBy(d => d.DeviceType ?? string.Empty, StringComparer.Ordinal)
                    .OrderBy(g => g.Key, StringComparer.Ordinal)          // 组间：类型名 Ordinal（稳定）
                    .SelectMany(g => g
                        .OrderBy(d => d.Name, StringComparer.Ordinal)     // 组内：设备名 Ordinal（稳定）
                        .Select(d => new DeviceStatusItem
                        {
                            Type = d.DeviceType ?? string.Empty,
                            GroupName = GetGroupDisplayName(d.DeviceType ?? string.Empty),
                            Name = d.Name,
                            IsConnected = d.IsConnected,
                        }))
                    .ToList();

                SyncDeviceStatuses(newData);
            }
            catch (Exception ex)
            {
                logService.Error(ex, "刷新设备状态失败");
            }
            finally
            {
                IsRefreshing = false;
            }
        }

        /// <summary>
        /// 同步设备行（三层判定，α 方案 = 结构不变时**复用行对象**，只就地改 IsConnected）：
        /// ① 结构判定：行数不等 → 变；否则 (Type,Name) 集合 SetEquals → 不等则变；
        /// ② 结构变 → 整表重建 + ResetBindings（View 收 ListChanged.Reset → 重建 Groups+Items）；
        /// ③ 结构不变 → 按 (Type,Name) 查表复用旧行对象，仅对 **IsConnected 真翻转**的行 ResetItem(i)
        ///    （View 收 ListChanged.ItemChanged → 只改该行图标，不碰其它行）。
        ///
        /// ⚠️ α 用**值比对**而非 ReferenceEquals：复用对象后引用恒相同，判不出变化——
        /// 这与 ProcessMonitor.SyncProcessRows 的引用比对实现不同（那边每 tick 全新对象）。
        /// 主键用 (Type, Name) 而非仅 Name：设备名跨类型理论可重名（GetDevice 跨类型字典查找即为此语义）。
        /// </summary>
        private void SyncDeviceStatuses(List<DeviceStatusItem> newData)
        {
            // ① 结构判定
            bool structureChanged = newData.Count != DeviceStatuses.Count;
            if (!structureChanged)
            {
                var newKeys = new HashSet<string>(newData.Select(ItemKey), StringComparer.Ordinal);
                var oldKeys = new HashSet<string>(DeviceStatuses.Select(ItemKey), StringComparer.Ordinal);
                structureChanged = !newKeys.SetEquals(oldKeys);
            }

            // ② 结构变了：整表重建（RaiseListChangedEvents 关闭期间改集合，收尾 ResetBindings 通知一次 Reset）
            if (structureChanged)
            {
                DeviceStatuses.RaiseListChangedEvents = false;
                try
                {
                    DeviceStatuses.Clear();
                    foreach (var item in newData)
                        DeviceStatuses.Add(item);
                }
                finally
                {
                    DeviceStatuses.RaiseListChangedEvents = true;
                    DeviceStatuses.ResetBindings();
                }
                return;
            }

            // ③ 结构不变：α 复用行对象，只对连接态真翻转的行发通知
            var newDict = new Dictionary<string, DeviceStatusItem>(StringComparer.Ordinal);
            foreach (var item in newData)
                newDict[ItemKey(item)] = item;

            var changedIndices = new List<int>();
            for (int i = 0; i < DeviceStatuses.Count; i++)
            {
                var oldItem = DeviceStatuses[i];
                if (!newDict.TryGetValue(ItemKey(oldItem), out var newItem)) continue;

                if (oldItem.IsConnected != newItem.IsConnected)
                {
                    oldItem.IsConnected = newItem.IsConnected; // 就地更新（复用对象，恒同引用）
                    changedIndices.Add(i);
                }
            }

            foreach (var idx in changedIndices.OrderBy(x => x))
                DeviceStatuses.ResetItem(idx);
        }

        /// <summary>行主键（Type + \u0001 + Name）：同 ConfigManager.GetTagKey 的分隔符惯例，防跨类型重名撞键</summary>
        private static string ItemKey(DeviceStatusItem item)
        {
            return item.Type + "\u0001" + item.Name;
        }
        #endregion

        #region 公共方法
        public void OnViewLoaded()
        {
            RefreshDeviceStatusCommand.Execute(null); // 首次拉取（心跳由 View 的 uiRefreshTimer 驱动，见 FormMainPage）
        }
        #endregion
    }

    #region 数据模型
    /// <summary>
    /// 设备状态行（设备连接状态面板）。
    /// Type = 原始设备类型（结构比对主键的一半，勿用中文展示名做键——改映射会误判结构变化）；
    /// GroupName = 展示组名（由 Type 经 DeviceTypeDisplayNames 映射，未登记类型回退原始类型名）。
    /// </summary>
    public class DeviceStatusItem
    {
        /// <summary>原始设备类型（DeviceTypes 常量值，结构比对主键）</summary>
        public string Type { get; set; }

        /// <summary>展示组名（中文映射后的组标题）</summary>
        public string GroupName { get; set; }

        public string Name { get; set; }

        public bool IsConnected { get; set; }
    }
    #endregion
}
