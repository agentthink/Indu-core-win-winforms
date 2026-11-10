using System;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;

namespace Taoyl.Application.TranslateDriver.Interfaces
{
    /// <summary>
    /// 翻译服务契约（面向 UI 运行时全量能力）。
    /// 继承语言窄契约 ITranslationLanguageService（VM 只注入窄接口，不持有 WinForms 方法），
    /// 额外提供窗体翻译/手动注册/排除配置等 UI 运行时能力，供窗体层注入使用。
    /// 由 TransManager 实现。
    /// </summary>
    public interface ITranslationService : ITranslationLanguageService
    {
        /// <summary>
        /// 翻译一个指定的窗体及其所有子控件。
        /// </summary>
        void TranslateForm(Form form);

        /// <summary>
        /// 翻译一个指定的文本资源。
        /// 注意：当资源条目不存在时返回 null，调用方需自行处理回退逻辑。
        /// </summary>
        string TranslateText(string namespaceName, string className, string key, ResourceType resourceType = ResourceType.Text);

        /// <summary>
        /// 手动注册一个控件，为其提供定制化的翻译和提取逻辑。
        /// </summary>
        void RegisterControl(Control control, string controlName,
            Action<Control, string> applyTranslation, Func<Control, string> extractText);

        /// <summary>
        /// 一键添加推荐排除类型（滚动条、进度条、输入控件等）。
        /// </summary>
        void UseRecommendedExclusions();
    }
}
