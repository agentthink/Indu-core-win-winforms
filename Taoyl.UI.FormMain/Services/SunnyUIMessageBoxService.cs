using Sunny.UI;
using System;

namespace Taoyl.UI.FormMain.Services
{


    /// <summary>
    /// 消息框服务抽象
    /// 解耦 ViewModel 对具体 UI 消息框的依赖，
    /// </summary>
    public interface IMessageBoxService
    {
        bool Show(string message, string title = "", UIStyle style = UIStyle.Blue, UIMessageBoxButtons buttons = UIMessageBoxButtons.OK, bool showMask = false, int delay = 0);
        bool ShowAsk(string message, bool showMask = false);
        bool ShowConfirm(string message, bool showMask = false);
        bool ShowConfirm(string message, string title, bool showMask = false, UIStyle style = UIStyle.Blue);
        void ShowError(string message, bool showMask = false, int delay = 0);
        void ShowErrorDetail(string message, string detail, bool showMask = false, int delay = 0);
        void ShowException(Exception ex, bool showMask = false, int delay = 0);
        void ShowInfo(string message, bool showMask = false, int delay = 0);
        void ShowSuccess(string message, bool showMask = false, int delay = 0);
        void ShowWarning(string message, bool showMask = false, int delay = 0);
    }

    /// <summary>
    /// SunnyUI 消息框服务实现
    /// </summary>
    public class SunnyUIMessageBoxService : IMessageBoxService
    {
        /// <summary>
        /// 显示错误提示
        /// </summary>
        public  void ShowError(string message, bool showMask = false, int delay = 0)
        {
            UIMessageBox.ShowError(message, showMask, delay);
        }

        /// <summary>
        /// 显示成功提示
        /// </summary>
        public  void ShowSuccess(string message, bool showMask = false, int delay = 0)
        {
            UIMessageBox.ShowSuccess(message, showMask, delay);
        }

        /// <summary>
        /// 显示信息提示
        /// </summary>
        public  void ShowInfo(string message, bool showMask = false, int delay = 0)
        {
            UIMessageBox.ShowInfo(message, showMask, delay);
        }

        /// <summary>
        /// 显示警告提示
        /// </summary>
        public  void ShowWarning(string message, bool showMask = false, int delay = 0)
        {
            UIMessageBox.ShowWarning(message, showMask, delay);
        }

        /// <summary>
        /// 显示确认对话框
        /// </summary>
        public  bool ShowConfirm(string message, bool showMask = false)
        {
            return UIMessageBox.ShowAsk(message, showMask);
        }

        /// <summary>
        /// 显示带标题的确认对话框
        /// </summary>
        public  bool ShowConfirm(string message, string title, bool showMask = false, UIStyle style = UIStyle.Blue)
        {
            return UIMessageBox.Show(message, title, style, UIMessageBoxButtons.OKCancel, showMask, 0);
        }

        /// <summary>
        /// 显示询问对话框
        /// </summary>
        public  bool ShowAsk(string message, bool showMask = false)
        {
            return UIMessageBox.ShowAsk(message, showMask);
        }

        /// <summary>
        /// 显示异常信息
        /// </summary>
        public  void ShowException(Exception ex, bool showMask = false, int delay = 0)
        {
            string message = $"错误信息: {ex.Message}\n错误类型: {ex.GetType().Name}";
            UIMessageBox.ShowError(message, showMask, delay);
        }

        /// <summary>
        /// 显示带详细信息的错误
        /// </summary>
        public  void ShowErrorDetail(string message, string detail, bool showMask = false, int delay = 0)
        {
            string fullMessage = $"{message}\n\n详细信息:\n{detail}";
            UIMessageBox.ShowError(fullMessage, showMask, delay);
        }

        /// <summary>
        /// 显示自定义消息框
        /// </summary>
        public  bool Show(string message, string title = "", UIStyle style = UIStyle.Blue,
            UIMessageBoxButtons buttons = UIMessageBoxButtons.OK, bool showMask = false, int delay = 0)
        {
            return UIMessageBox.Show(message, title, style, buttons, showMask, delay);
        }

    }
}