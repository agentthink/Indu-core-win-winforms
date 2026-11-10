using System;
using System.Windows.Forms;
using Taoyl.Application.Share.Config;

namespace Taoyl.UI.FormMain.Services
{

    /// <summary>
    /// 用户活动监控接口
    /// </summary>
    public interface IUserActivityMonitor
    {
        int TimeoutSeconds { get; }
        bool IsMonitoring { get; }
        event Action<int> CountdownUpdated;
        event Action AutoLogoutRequired;
        void Start();
        void Stop();
        void RecordActivity();
        void ResetTimer();
        int GetRemainingSeconds();
    }

    /// <summary>
    /// 用户活动监控 — 由Autofac管理生命周期
    /// </summary>
    public sealed class UserActivityMonitor : IUserActivityMonitor, IDisposable
    {
        private readonly IAutoLogoutSettings settings;
        private readonly Timer monitorTimer;
        private DateTime lastActivityTime;
        private bool disposed;
        private bool isMonitoring;
        private UserActivityMessageFilter messageFilter;

        /// <summary>
        /// 自动注销时间（秒，实时读配置，非法值回退 180）。
        /// </summary>
        public int TimeoutSeconds =>
            settings.AutoLogoutTime > 0 ? settings.AutoLogoutTime : 180;

        public bool IsMonitoring => isMonitoring;

        public event Action<int> CountdownUpdated;
        public event Action AutoLogoutRequired;

        public UserActivityMonitor(IAutoLogoutSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));

            monitorTimer = new Timer { Interval = 1000 };
            monitorTimer.Tick += OnMonitorTimerTick;
            lastActivityTime = DateTime.Now;
        }

        public void Start()
        {
            if (disposed || isMonitoring) return;
            ResetTimer();
            monitorTimer.Start();
            messageFilter = new UserActivityMessageFilter(this);
            System.Windows.Forms.Application.AddMessageFilter(messageFilter);
            isMonitoring = true;
        }

        public void Stop()
        {
            if (!isMonitoring) return;
            monitorTimer.Stop();
            if (messageFilter != null)
            {
                System.Windows.Forms.Application.RemoveMessageFilter(messageFilter);
                messageFilter = null;
            }
            isMonitoring = false;
        }

        public void RecordActivity() => lastActivityTime = DateTime.Now;
        public void ResetTimer() => lastActivityTime = DateTime.Now;

        public int GetRemainingSeconds()
        {
            var remaining = TimeoutSeconds - (int)(DateTime.Now - lastActivityTime).TotalSeconds;
            return Math.Max(0, remaining);
        }

        private void OnMonitorTimerTick(object sender, EventArgs e)
        {
            try
            {
                var remaining = GetRemainingSeconds();
                CountdownUpdated?.Invoke(remaining);
                if (remaining <= 0)
                {
                    Stop();
                    AutoLogoutRequired?.Invoke();
                }
            }
            catch
            {
                Stop();
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            Stop();
            monitorTimer?.Dispose();
            disposed = true;
            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 用户活动消息过滤器 - 捕获鼠标和键盘活动
    /// </summary>
    public sealed class UserActivityMessageFilter : IMessageFilter
    {
        private readonly UserActivityMonitor _monitor;

        public UserActivityMessageFilter(UserActivityMonitor monitor)
        {
            _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        }

        public bool PreFilterMessage(ref Message m)
        {
            // 定义要捕获的消息类型
            switch (m.Msg)
            {
                // 鼠标消息
                //case 0x0200: // WM_MOUSEMOVE - 鼠标移动          !!!移除，避免高频消息风暴
                case 0x0201: // WM_LBUTTONDOWN - 鼠标左键按下
                case 0x0202: // WM_LBUTTONUP - 鼠标左键释放
                case 0x0203: // WM_LBUTTONDBLCLK - 鼠标左键双击
                case 0x0204: // WM_RBUTTONDOWN - 鼠标右键按下
                case 0x0205: // WM_RBUTTONUP - 鼠标右键释放
                case 0x0206: // WM_RBUTTONDBLCLK - 鼠标右键双击
                case 0x0207: // WM_MBUTTONDOWN - 鼠标中键按下
                case 0x0208: // WM_MBUTTONUP - 鼠标中键释放
                case 0x0209: // WM_MBUTTONDBLCLK - 鼠标中键双击
                case 0x020A: // WM_MOUSEWHEEL - 鼠标滚轮
                case 0x020E: // WM_MOUSEHWHEEL - 鼠标横向滚轮

                // 键盘消息
                case 0x0100: // WM_KEYDOWN - 按键按下
                case 0x0101: // WM_KEYUP - 按键释放
                case 0x0102: // WM_CHAR - 字符输入
                case 0x0104: // WM_SYSKEYDOWN - 系统键按下
                case 0x0105: // WM_SYSKEYUP - 系统键释放

                // 触摸消息（如果有触摸屏）
                case 0x0240: // WM_TOUCH - 触摸事件

                // 窗口激活消息（用户切换窗口也算活动）
                case 0x0006: // WM_ACTIVATE - 窗口激活
                    _monitor.RecordActivity();
                    break;
            }

            // 返回false让消息继续传递
            return false;
        }
    }
}