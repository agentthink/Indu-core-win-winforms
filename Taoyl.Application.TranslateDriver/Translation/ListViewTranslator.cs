using System.Collections.Generic;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// ListView 翻译器：支持 Group 标题和 Item 文本
    /// </summary>
    public class ListViewTranslator : IControlTranslator
    {
        public bool CanTranslate(Control control) => control is ListView;

        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            if (!(control is ListView lv)) return;

            // 1. 翻译 Group Header
            foreach (ListViewGroup group in lv.Groups)
            {
                string controlName = GetGroupKey(group);
                // 三段式键：{类型}.{组名}.{属性名}（组标题属性为 Header）
                var entry = cache.Get(namespaceName, className, controlName + ".Header", ResourceType.UI);
                if (entry != null)
                {
                    string text = entry.GetText(lang);
                    if (!string.IsNullOrEmpty(text))
                        group.Header = text;
                }
            }

            // 2. 翻译 Item Text
            foreach (ListViewItem item in lv.Items)
            {
                string controlName = GetItemKey(item);
                // 三段式键：{类型}.{项名}.{属性名}（项文本属性为 Text）
                var entry = cache.Get(namespaceName, className, controlName + ".Text", ResourceType.UI);
                if (entry != null)
                {
                    string text = entry.GetText(lang);
                    if (!string.IsNullOrEmpty(text))
                        item.Text = text;
                }
            }
        }

        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            if (!(control is ListView lv)) yield break;

            // 提取 Group
            foreach (ListViewGroup group in lv.Groups)
            {
                if (!string.IsNullOrEmpty(group.Header))
                {
                    // 三段式键：{类型}.{组名}.{属性名}
                    var entry = ResourceEntry.CreateUI(
                        namespaceName,
                        className,
                        GetGroupKey(group) + ".Header",
                        group.Header);
                    yield return entry;
                }
            }

            // 提取 Item
            foreach (ListViewItem item in lv.Items)
            {
                if (!string.IsNullOrEmpty(item.Text))
                {
                    // 三段式键：{类型}.{项名}.{属性名}
                    var entry = ResourceEntry.CreateUI(
                        namespaceName,
                        className,
                        GetItemKey(item) + ".Text",
                        item.Text);
                    yield return entry;
                }
            }
        }

        /// <summary>生成稳定的 Group 键</summary>
        private static string GetGroupKey(ListViewGroup group)
        {
            // 优先使用 Name 属性（设计器可设置）
            if (!string.IsNullOrEmpty(group.Name))
                return group.Name;

            // 降级：使用索引生成固定前缀
            var lv = group.ListView;
            if (lv != null)
            {
                int index = lv.Groups.IndexOf(group);
                if (index >= 0)
                    return $"Group_{index}";
            }
            // 极端情况：无宿主 ListView，使用 Header 哈希
            return $"Group_{group.Header.GetHashCode():X8}";
        }

        /// <summary>生成稳定的 Item 键</summary>
        private static string GetItemKey(ListViewItem item)
        {
            // 优先使用 Name 属性
            if (!string.IsNullOrEmpty(item.Name))
                return item.Name;

            // 降级：索引前缀
            var lv = item.ListView;
            if (lv != null)
            {
                int index = lv.Items.IndexOf(item);
                if (index >= 0)
                    return $"Item_{index}";
            }
            // 无宿主时用 Text 哈希
            return $"Item_{item.Text?.GetHashCode():X8}";
        }
    }
}