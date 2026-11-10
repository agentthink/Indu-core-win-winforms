using System.Windows.Forms;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// 控件树生命周期小工具：配置区「整段重建」时摘除旧控件树的唯一正确姿势。
    ///
    /// 为什么必须有它（反编译 System.Windows.Forms / SunnyUI 3.9.1 实证，勿凭直觉删）：
    /// - <c>Control.ControlCollection.Clear()</c> = <c>while (Count != 0) RemoveAt(Count - 1)</c>，
    ///   **只摘除、不 Dispose**，且被摘下的控件**句柄依然存活**（脱离父集合并不销毁 HWND）；
    /// - 被摘下的控件失去全部外部引用后会被 GC 放进终结器队列，在**终结器线程**上跑
    ///   <c>Sunny.UI.UITextBox.Dispose(bool disposing)</c>；
    /// - 而 SunnyUI 的该重写里 <c>edit?.Dispose()</c> / <c>bar?.Dispose()</c> / <c>btn?.Dispose()</c>
    ///   **写在 <c>if (disposing)</c> 之外**，即终结器路径也会把内层原生 <c>UIEdit</c>（<c>: TextBox</c>）
    ///   按 <c>disposing = true</c> 释放 ⇒ <c>Control.Dispose(true)</c> → <c>if (IsHandleCreated) DestroyHandle()</c>
    ///   → <c>InternalHandle</c> → <c>Handle</c> getter；
    /// - <c>Handle</c> getter 首行即跨线程检查（<c>checkForIllegalCrossThreadCalls &amp;&amp; InvokeRequired</c> ⇒ throw），
    ///   而 <c>checkForIllegalCrossThreadCalls</c> 初值 = <c>Debugger.IsAttached</c>
    ///   ⇒ **F5 调试时必抛**「跨线程操作无效」（堆栈：<c>Component.Finalize</c> → <c>UITextBox.Dispose</c>
    ///   → <c>DestroyHandle</c> → <c>get_InternalHandle</c> → <c>get_Handle</c>）。
    ///
    /// ⇒ 结论：**丢弃控件树必须在此显式 Dispose，绝不能依赖终结器**。
    /// （Release 无调试器时该检查关闭、不抛异常，但那只是「静默地跨线程销毁主线程创建的 HWND」，错误照样存在。）
    /// </summary>
    public static class UiControlHelper
    {
        /// <summary>
        /// 摘除并释放宿主容器的全部子控件（整段重建前的统一清理入口）。
        /// 调用后宿主 Controls 为空，旧控件树含全部孙级已级联释放——
        /// <c>Control.Dispose(bool)</c> 内部会遍历自身子控件逐个 <c>Dispose()</c>，故只释放顶层即够。
        /// </summary>
        /// <param name="host">承载容器（渲染宿主 / 区段自身）</param>
        public static void DisposeAndClear(Control host)
        {
            if (host == null) return;

            Control.ControlCollection children = host.Controls;
            if (children.Count == 0) return;

            // 先整体摘下再逐个释放：Dispose 内部会把自己从父集合移除，边遍历边改集会破坏枚举；
            // 摘下后 parent 已为 null，Dispose 里的 parent.Controls.Remove(this) 自然跳过，无副作用。
            Control[] olds = new Control[children.Count];
            children.CopyTo(olds, 0);
            children.Clear();

            for (int i = 0; i < olds.Length; i++)
                olds[i]?.Dispose();
        }
    }
}
