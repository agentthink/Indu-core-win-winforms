using Sunny.UI;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.Application.LogDriver;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.Share.Logging;
using Taoyl.UI.FormMain.Helpers;
using System.ComponentModel;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    /// <summary>
    /// 主页面
    /// 负责设备状态监控、MES日志显示等核心业务界面的渲染
    /// 所有业务逻辑已移至MainPageViewModel
    /// </summary>
    public partial class FormMainPage : UIPage
    {
        #region 依赖注入
        public MainPageViewModel ViewModel { get; set; }
        public ILogDisplay LogDisplay { get; set; }
        #endregion

        #region 私有字段
        private Timer uiRefreshTimer;
        private ImageList connStateImageList;
        private ListView devConnListView;
        #endregion

        #region 构造函数
        public FormMainPage()
        {
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            DpiZoomControl();
        }
        #endregion

        private void DpiZoomControl()
        {
            tabControlLog.ItemSize = new Size(UiDpi.Scale(150), UiDpi.Scale(35));
            dashedLinePanel1.LineWidth = UiDpi.Scale(5);
            dashedLinePanel1.SegmentGap = UiDpi.Scale(4);
            dashedLinePanel1.SegmentHeight = UiDpi.Scale(24);
        }

        #region 窗体生命周期
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            ValidateInjection();

            // 抑制布局 + 递归双缓冲：初始化/建表/建列表期间不逐次重绘，配合父窗体 WS_EX_COMPOSITED 消除子页面切换抖动
            SuspendLayout();
            try
            {
                UiDpi.EnableDoubleBuffered(mainTableLayoutPanel);
                ConfigureLogForm();
                InitializeControls();
                BindViewModel();
                BridgeEvents();
                ViewModel.OnViewLoaded();
            }
            finally
            {
                ResumeLayout(false);
            }
        }
        
        protected override void OnClosed(EventArgs e)
        {
            // 心跳 Timer 生命周期归 View（窗体关必停，防泄漏）；VM 不持 Timer（原双 Timer 各跑一遍命令 → 已删 VM 侧）
            uiRefreshTimer?.Stop();
            uiRefreshTimer?.Dispose();
            base.OnClosed(e);
        }

        private void ValidateInjection()
        {
            if (ViewModel == null)
                throw new InvalidOperationException("MainPageViewModel未注入，请检查Autofac配置");
            if (LogDisplay == null)
                throw new InvalidOperationException("LogDisplay未注入，请检查Autofac配置");
        }
        #endregion

        #region 初始化
    /// <summary>
    /// 日志显示配置：类别 → 网格（虚拟模式 UIDataGridView，一一对应）；
    /// 页签顺序 = Info / Warn / Error / MES
    /// 着色由类库静态色表内置，此处不再逐类别调色
    /// </summary>
    private void ConfigureLogForm()
    {
        var gridMappings = new Dictionary<LogCategory, DataGridView>
        {
            { LogCategory.Info, gvInfoLog },
            { LogCategory.Warn, gvWarnLog },
            { LogCategory.Error, gvErrorLog },
            { LogCategory.MES, gvMESLog }
        };

            LogDisplay.Init(gridMappings);
            LogDisplay.SetUpdateInterval(100); // 心跳 100ms（峰值 1000 行/秒下每帧约 100 行）
        }

        private void InitializeControls()
        {
            InitializeDeviceList();
        }
        #endregion

        #region 绑定映射
        private void BindViewModel()
        {
            
            // 设备列表：订阅集合变更事件（View 据此分派重建/增量）
            ViewModel.DeviceStatuses.ListChanged += OnDeviceStatusesListChanged;

            // 首帧：装订后立即渲染一次当前快照（VM 首次拉取可能早于本订阅，防首帧空列表）
            RebuildDeviceGroups();
        }

        /// <summary>
        /// 设备状态集合变更分派：Reset（结构变：增删设备/类型）→ 重建 Groups+Items；
        /// ItemChanged（连接态翻转，VM 逐行 ResetItem）→ 只改该行图标（不碰其它行、不重建）。
        /// </summary>
        private void OnDeviceStatusesListChanged(object sender, ListChangedEventArgs e)
        {
            switch (e.ListChangedType)
            {
                case ListChangedType.Reset:
                    RebuildDeviceGroups();
                    break;
                case ListChangedType.ItemChanged:
                    UpdateDeviceItemImage(e.NewIndex);
                    break;
            }
        }
        #endregion

        #region 事件桥接
        private void BridgeEvents()
        {
            StartRefreshTimer();
        }

        /// <summary>
        /// UI 心跳（唯一 Timer：VM 侧原另有一个同频 Timer 各跑一遍相同命令 → 已删，避免双跑）。
        /// 250ms：连接状态是秒级事件，250ms 足够跟手。
        /// </summary>
        private void StartRefreshTimer()
        {
            uiRefreshTimer = new Timer { Interval = 250, Enabled = true };
            uiRefreshTimer.Tick += (s, e) => OnRefreshTimerTick();
        }

        private void OnRefreshTimerTick()
        {
            ViewModel.RefreshDeviceStatusCommand.Execute(null);
        }
        #endregion

        #region 设备状态列表
        private void InitializeDeviceList()
        {
            connStateImageList = new ImageList
            {
                ImageSize = new Size(UiDpi.Scale(24), UiDpi.Scale(24)),
                ColorDepth = ColorDepth.Depth32Bit
            };

            connStateImageList.Images.Add(Properties.Resources.异常状态亮灯);
            connStateImageList.Images.Add(Properties.Resources.正常状态亮灯);

            devConnListView = new ListView
            {
                Name = "devConn_listView",
                BackColor = Color.FromArgb(235, 243, 255),
                BorderStyle = BorderStyle.None,
                Dock = DockStyle.Fill,
                Font = new Font("Microsoft YaHei", 12, FontStyle.Regular),
                ForeColor = Color.FromArgb(64, 64, 64),
                SmallImageList = connStateImageList,
                View = View.SmallIcon,
                ShowGroups = true
            };

            if (dashedLinePanel1?.ContentPanel != null)
            {
                dashedLinePanel1.ContentPanel.Controls.Add(devConnListView);
                dashedLinePanel1.ContentPanel.Padding = new Padding(15, 5, 15, 5);
            }

            UiDpi.EnableDoubleBuffered(devConnListView);
        }

        /// <summary>
        /// 重建组与行（ListChanged.Reset 时调，含首帧）：数据源是 VM 已分组排序好的行（GroupName 连续同值成组），
        /// 故此处只按 GroupName 连续段建组，不重做分组逻辑（分组归属 VM）。
        /// ListViewItem.Name 用 (Type,Name) 复合键 —— ListView 的键是**全局唯一**（无组作用域），
        /// 设备名跨类型理论可重名，用裸名会撞键导致 UpdateDeviceItemImage 改错行。
        /// </summary>
        private void RebuildDeviceGroups()
        {
            if (devConnListView == null) return;

            devConnListView.BeginUpdate();
            try
            {
                devConnListView.Items.Clear();
                devConnListView.Groups.Clear();

                ListViewGroup currentGroup = null;
                string currentGroupName = null;

                foreach (var row in ViewModel.DeviceStatuses)
                {
                    if (currentGroup == null || !string.Equals(currentGroupName, row.GroupName, StringComparison.Ordinal))
                    {
                        currentGroupName = row.GroupName;
                        currentGroup = new ListViewGroup(currentGroupName, HorizontalAlignment.Center)
                        {
                            Name = currentGroupName
                        };
                        devConnListView.Groups.Add(currentGroup);
                    }

                    var item = new ListViewItem(row.Name, row.IsConnected ? 1 : 0)
                    {
                        Group = currentGroup,
                        Name = StructureKey(row)
                    };
                    devConnListView.Items.Add(item);
                }
            }
            finally
            {
                devConnListView.EndUpdate();
                // 重建后同样可能 partial redraw 丢 item，重绘一次
                devConnListView.Invalidate(true);
            }
        }

        
        /// <summary>
        /// 增量更新单行图标（ListChanged.ItemChanged 时调）：按 (Type,Name) 键 O(1) 定位
        /// （ListViewItemCollection 键索引走哈希）。
        /// ⚠️ 键不存在时键索引**抛 ArgumentException**（非返回 null）→ 必须预检，
        /// 否则数据/视图瞬时不同步会抛进 ListChanged 回调链。
        /// 取不到行即兜底整表重建，保证不出现"图标永久不刷新"。
        /// </summary>
        private void UpdateDeviceItemImage(int index)
        {
            if (devConnListView == null) return;
            if (index < 0 || index >= ViewModel.DeviceStatuses.Count)
            {
                RebuildDeviceGroups();
                return;
            }

            var row = ViewModel.DeviceStatuses[index];
            var item = TryGetListViewItem(StructureKey(row));
            if (item == null)
            {
                RebuildDeviceGroups(); // 键不存在（结构已变但未收到 Reset）→ 兜底重建
                return;
            }
            item.ImageIndex = row.IsConnected ? 1 : 0;
            // 🔴 分组视图下 LVM_SETITEM 改图标后，该 item 偶发被 native 增量重绘丢画（"隐身"）
            // ——实测隐身行均为刚翻转的行。改完立即全量重绘；翻转是秒级低频事件，成本可忽略。
            devConnListView.Invalidate(true);
        }

        /// <summary>安全按键取 ListView 行（键索引不命中会抛 ArgumentException，此处预检+兜 catch 返回 null）</summary>
        private ListViewItem TryGetListViewItem(string key)
        {
            try
            {
                return devConnListView.Items.ContainsKey(key) ? devConnListView.Items[key] : null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>视图键（Type + \u0001 + Name）：与 VM 侧 ItemKey 同构，分隔符惯例同 ConfigManager.GetTagKey</summary>
        private static string StructureKey(DeviceStatusItem row)
        {
            return row.Type + "\u0001" + row.Name;
        }
        #endregion

    }
}