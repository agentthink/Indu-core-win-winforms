using System.Collections.Generic;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// 标准控件翻译器
    /// 处理通用控件（Button, Label, TextBox等）的Text属性翻译
    /// 作为默认策略兜底
    /// 三段式键约定：{类型}.{控件名}.{属性名}（普通控件属性为 Text，如 UI.btnStart.Text）
    /// </summary>
    public class StandardControlTranslator : IControlTranslator
    {
        /// <summary>
        /// 默认支持所有控件（作为兜底策略）
        /// </summary>
        public bool CanTranslate(Control control) => true;

        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            if (string.IsNullOrEmpty(control?.Name)) return;

            // 输入类控件不参与翻译（约定：输入框 Text 是数据不是界面文本，参数值保护）
            // 双保险：即使 RecommendedExcludedTypes 未被启用，此处也兜底跳过
            if (control is TextBoxBase) return;

            // 三段式键：{类型}.{控件名}.{属性名}
            var entry = cache.Get(namespaceName, className, control.Name + ".Text", ResourceType.UI);
            if (entry?.Type == ResourceType.UI)
            {
                var text = entry.GetText(lang);
                if (!string.IsNullOrEmpty(text))
                    control.Text = text;
            }
        }

        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            // 输入类控件不参与提取（同上，避免参数初始值被提取成 UI 资源）
            if (control is TextBoxBase) yield break;

            if (!string.IsNullOrEmpty(control.Text))
            {
                if (!string.IsNullOrEmpty(control.Name))
                {
                    // 三段式键：{类型}.{控件名}.{属性名}
                    var entry = ResourceEntry.CreateUI(namespaceName, className, control.Name + ".Text", control.Text);
                    yield return entry;
                }

            }
        }
    }
}