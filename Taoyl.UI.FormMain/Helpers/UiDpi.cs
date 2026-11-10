using Sunny.UI;
using System;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// UI 层 DPI 缩放辅助（System aware 下窗体按物理像素布局，显式 Font/尺寸不随 DPI 缩放）。
    /// 全局缩放系数由 Program.cs 入口按 Graphics.DpiX 计算到 UIStyles.GlobalFontScale，此处统一取用，
    /// 避免各处重复计算且保证系数来源一致（不要用 Control.DeviceDpi——实测返回 96 不可靠）。
    /// </summary>
    public static class UiDpi
    {
        /// <summary>缩放系数：100%→1.0，150%→1.5，200%→2.0</summary>
        public static float DpiScale => UIStyles.GlobalFontScale / 100f;

        /// <summary>按 DPI 缩放整数（四舍五入），如 UiDpi.Scale(24) 在 200% 屏返回 48</summary>
        public static int Scale(int value) => (int)Math.Round(value * DpiScale);

        /// <summary>
        /// 递归启用控件树双缓冲（反射设置 DoubleBuffered，覆盖自绘/嵌套容器，避免重绘闪烁）。
        /// Control.DoubleBuffered=true 等价于 SetStyle(OptimizedDoubleBuffer | AllPaintingInWmPaint, true)
        /// </summary>
        public static void EnableDoubleBuffered(Control control)
        {
            if (control == null) return;

            var prop = control.GetType().GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            prop?.SetValue(control, true, null);

            foreach (Control child in control.Controls)
                EnableDoubleBuffered(child);
        }


        /// <summary>
        /// WM_SETREDRAW 禁绘屏障：整树递归禁绘直到首帧显示——装载路径上每个中间态（按钮使能级联、
        /// 列表填充、组联动、选中清空重装、配置区多轮渲染）一帧都不画，恢复后一次画终态。
        /// 与双缓冲/Composited 正交（它们解决"绘制快慢"，本屏障解决"中间态不该出现"）：
        /// 嵌入式 UIPage OnLoad 时页面已可绘（FormMain Add+Show 后 paint 与装载交错），SuspendLayout 只压布局不压绘制。
        /// 屏障起点必须在 base.OnLoad(e) 之前：UIPage 基类装载期的样式应用/布局也纳入禁绘，否则基类段与装载段分帧成画。
        /// 页面实例 MenuManager 缓存常驻，本 OnLoad 每实例仅执行一次（切走切回复用实例不再装载），屏障成本一次摊销。
        /// ⚠️ 勿在禁绘前强制建全树句柄（09-07 实验证伪）：提前建句柄使控件按 Designer 初始尺寸先布局，
        /// 容器尺寸收敛时二次重排 = 首开抖动 + DGV 高度错乱之源。句柄懒建是 WinForms 特性——页面不可见期
        /// 后建句柄的控件不参与绘制，禁绘无需覆盖；正常路径不在 OnLoad 恢复，由 OnShown 统一恢复成帧。
        /// 
        /// 递归禁绘/恢复（控件树批量更新防中间态成帧）：WM_SETREDRAW=false 期间控件不重绘，
        /// 恢复 + Invalidate 后一次画终态。句柄未建的控件直接跳过（首载页面不可见期后建句柄的控件
        /// 不参与绘制，无需覆盖——实测无需先行强制建句柄；09-07 教训见 OnLoad 注释）。
        /// </summary>
        public static void SetRedraw(Control control, bool enable)
        {
            if (control == null || control.IsDisposed) return;
            if (control.IsHandleCreated)
                SendMessage(control.Handle, WM_SETREDRAW, enable ? (IntPtr)1 : IntPtr.Zero, IntPtr.Zero);
            foreach (Control child in control.Controls)
                SetRedraw(child, enable);
        }

        [DllImport("user32.dll")]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

        private const int WM_SETREDRAW = 0x000B;
    }
}
