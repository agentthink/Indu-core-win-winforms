// 对应类库：本项目（组合根）——进程入口：单实例管理、UI 样式初始化、启动序列
using System;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using Autofac;
using Sunny.UI;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.UI.FormMain;

namespace Taoyl.IFALaunch
{
    internal static class Program
    {
        /// <summary>
        /// 应用程序主入口：防重复启动、系统初始化、启动主窗体
        /// </summary>
        [STAThread]
        static void Main()
        {
            // 1. 检查是否已有实例运行，是则激活并退出
            Process instance = RunningInstance();
            if (instance != null)
            {
                HandleRunningInstance(instance);
                return;
            }

            // 2. UI 样式初始化（DPI 感知 + 全局字体）
            InitializeUiStyles();
            System.Windows.Forms.Application.EnableVisualStyles();

            // 3. 扶正主线程同步上下文（进程级 UI 基建，任何 UI 工作之前执行）：WFSC 本应由
            //    Application.Run / 第一个 Control 构造的 AutoInstall 隐式安装，但在那之前主线程
            //    Current 为 null（Post 静默 no-op）或为基类 SynchronizationContext（Post 甩线程池，
            //    回调脱离 UI 消息泵）。此处显式提前安装，此后任何主线程捕获（Bootstrapper.InitComponents
            //    的 AlarmPopupPresenter、FormMain 解析链各 VM）都拿到绑定主线程消息泵的正规上下文
            //    （WFSC.InstallIfNeeded 是 internal，SetSynchronizationContext 为公开 API 等效写法）
            var syncCurrent = System.Threading.SynchronizationContext.Current;
            if (syncCurrent == null || syncCurrent.GetType() == typeof(System.Threading.SynchronizationContext))
            {
                System.Threading.SynchronizationContext.SetSynchronizationContext(
                    new System.Windows.Forms.WindowsFormsSynchronizationContext());
            }

            // 4. 构建容器并初始化（配置预加载 / 数据库 / 设备装载）
            Bootstrapper.Initialize();

            // 5. 从容器解析主窗体（所有依赖已就绪）并启动应用程序
            var mainForm = Bootstrapper.Container.Resolve<FormMain>();
            System.Windows.Forms.Application.Run(mainForm);

            // 6. 退出 flush：日志异步队列残留落盘（K2 连锁——AsyncTargetWrapper 的 flush 需经
            //    LogManager.Shutdown 触发，退出不调用会丢行）；消息循环结束后、进程退出前执行一次
            Bootstrapper.Container.Resolve<ILogService>().Shutdown();
        }

        /// <summary>
        /// UI 样式初始化：开启 SunnyUI DPI 缩放 + 全局字体，按系统 DPI 动态补偿字号
        /// </summary>
        private static void InitializeUiStyles()
        {
            // DPI 感知已由 app.manifest 的 dpiAware=true 声明，此处让 SunnyUI 按缩放倍率缩放字体
            UIStyles.DPIScale = true;
            UIStyles.GlobalFont = true;
            UIStyles.GlobalFontName = "微软雅黑";
            // System aware 下窗体按物理像素布局，Designer 显式 Font 会覆盖 DPIScale 的字号放大，
            // 用 GlobalFontScale 按系统 DPI 动态补偿（100%→100，150%→150，200%→200），保证各缩放比例下字号正常
            float dpiX;
            using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
            {
                dpiX = g.DpiX;
            }
            UIStyles.GlobalFontScale = (int)Math.Round(dpiX / 96f * 100f);
            UIStyles.SetDPIScale();
        }

        #region 单实例管理

        public static Process RunningInstance()
        {
            Process currentProcess = Process.GetCurrentProcess();
            Process[] processes = Process.GetProcessesByName(currentProcess.ProcessName);
            foreach (Process process in processes)
            {
                if (process.Id != currentProcess.Id &&
                    Assembly.GetExecutingAssembly().Location.Replace("/", "\\")
                    == currentProcess.MainModule.FileName)
                {
                    return process;
                }
            }
            return null;
        }

        private static void HandleRunningInstance(Process instance)
        {
            ShowWindowAsync(instance.MainWindowHandle, SW_SHOWNOMAL);
            SetForegroundWindow(instance.MainWindowHandle);
        }

        [DllImport("User32.dll")]
        private static extern bool ShowWindowAsync(IntPtr hWnd, int cmdShow);
        private const int SW_SHOWNOMAL = 1;

        [DllImport("User32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        #endregion
    }
}
