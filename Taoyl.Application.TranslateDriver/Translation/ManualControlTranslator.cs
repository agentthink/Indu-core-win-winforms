using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// 一个为特定控件实例定制的翻译器。
    /// 它通过构造函数接收具体的翻译和提取逻辑。
    /// </summary>
    public class ManualControlTranslator : IControlTranslator
    {
        private readonly Action<Control, string> _applyTranslation;
        private readonly Func<Control, string> _extractText;

        /// <summary>
        /// 初始化一个手动控件翻译器的新实例。
        /// </summary>
        /// <param name="applyTranslation">一个委托，定义了如何将翻译文本应用到控件上。</param>
        /// <param name="extractText">一个委托，定义了如何从控件中提取原文文本。</param>
        public ManualControlTranslator(Action<Control, string> applyTranslation, Func<Control, string> extractText)
        {
            _applyTranslation = applyTranslation ?? throw new ArgumentNullException(nameof(applyTranslation));
            _extractText = extractText ?? throw new ArgumentNullException(nameof(extractText));
        }

        /// <summary>
        /// 确定此翻译器是否能处理给定的控件。
        /// </summary>
        /// <returns>总是返回 true，因为它被设计为处理任何通过策略注册的控件。</returns>
        public bool CanTranslate(Control control) => true;

        /// <summary>
        /// 使用通过构造函数传入的自定义逻辑来执行翻译。
        /// 三段式键约定：{类型}.{控件名}.{属性名}（手动注册控件默认属性为 Text，如 UI.myCtrl.Text）
        /// </summary>
        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            var entry = cache.Get(namespaceName, className, control.Name + ".Text", ResourceType.UI);
            if (entry != null)
            {
                var text = entry.GetText(lang);
                if (!string.IsNullOrEmpty(text))
                {
                    _applyTranslation(control, text);
                }
            }
        }

        /// <summary>
        /// 使用通过构造函数传入的自定义逻辑来提取需要翻译的资源条目。
        /// </summary>
        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            var originalText = _extractText(control);
            if (!string.IsNullOrEmpty(originalText))
            {
                // 三段式键：{类型}.{控件名}.{属性名}
                yield return ResourceEntry.CreateUI(namespaceName, className, control.Name + ".Text", originalText);
            }
        }
    }
}
