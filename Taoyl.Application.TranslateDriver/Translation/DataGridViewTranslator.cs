using System.Collections.Generic;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// DataGridView列头翻译器
    /// 翻译DataGridView各列的HeaderText属性
    /// 三段式键约定：{类型}.{列名}.{属性名}（列头属性为 HeaderText，如 UI.colStatus.HeaderText）
    /// </summary>
    public class DataGridViewTranslator : IControlTranslator
    {
        public bool CanTranslate(Control control) => control is DataGridView;

        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            if (!(control is DataGridView dgv)) return;

            foreach (DataGridViewColumn column in dgv.Columns)
            {
                if (string.IsNullOrEmpty(column.Name)) continue;

                // 三段式键：{类型}.{列名}.{属性名}
                var entry = cache.Get(namespaceName, className, column.Name + ".HeaderText", ResourceType.UI);
                if (entry?.Type == ResourceType.UI)
                {
                    var text = entry.GetText(lang);
                    if (!string.IsNullOrEmpty(text))
                        column.HeaderText = text;
                }
            }
        }

        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            if (!(control is DataGridView dgv)) yield break;

            foreach (DataGridViewColumn col in dgv.Columns)
            {
                if (!string.IsNullOrEmpty(col.HeaderText))
                {
                    // 三段式键：{类型}.{列名}.{属性名}
                    var entry = ResourceEntry.CreateUI(namespaceName, className, col.Name + ".HeaderText", col.HeaderText);
                    yield return entry;
                }
            }
        }
    }
}