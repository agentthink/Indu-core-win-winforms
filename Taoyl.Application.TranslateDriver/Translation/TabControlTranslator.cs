using System.Collections.Generic;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// TabControl页签翻译器
    /// 通过 TabControl 基类处理，兼容 SunnyUI 的 UITabControl（其继承自 TabControl）
    /// 三段式键约定：{类型}.{页名}.{属性名}（页签标题属性为 Text，如 UI.tp_UI.Text）
    /// </summary>
    public class TabControlTranslator : IControlTranslator
    {
        public bool CanTranslate(Control control) =>
            control is TabControl;

        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            var tabControl = control as TabControl;
            if (tabControl?.TabPages == null) return;

            foreach (TabPage page in tabControl.TabPages)
            {
                if (string.IsNullOrEmpty(page.Name)) continue;

                // 三段式键：{类型}.{页名}.{属性名}
                var entry = cache.Get(namespaceName, className, page.Name + ".Text", ResourceType.UI);
                if (entry?.Type == ResourceType.UI)
                {
                    var text = entry.GetText(lang);
                    if (!string.IsNullOrEmpty(text))
                        page.Text = text;
                }
            }
        }

        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            var tabControl = control as TabControl;
            if (tabControl?.TabPages == null) yield break;

            foreach (TabPage page in tabControl.TabPages)
            {
                if (!string.IsNullOrEmpty(page.Text))
                {
                    // 三段式键：{类型}.{页名}.{属性名}
                    var entry = ResourceEntry.CreateUI(namespaceName, className, page.Name + ".Text", page.Text);
                    yield return entry;
                }
            }
        }
    }
}
