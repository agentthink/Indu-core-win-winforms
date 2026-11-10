using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// TreeView 翻译器 (支持无限层级递归)
    /// </summary>
    public class TreeViewTranslator : IControlTranslator
    {
        public bool CanTranslate(Control control) => control is TreeView;

        /// <summary>
        /// 公共入口：启动对 TreeView 节点的翻译过程。
        /// 批量修改节点文本必须抑制中间态重绘（BeginUpdate/EndUpdate），否则逐节点赋值触发多次绘制，
        /// 构建中间态出现菜单/树重影与相互遮挡（FormMain 菜单切用户/切语言重影的根因）。
        /// </summary>
        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            if (control is TreeView treeView)
            {
                treeView.BeginUpdate();
                try
                {
                    TranslateNodesRecursive(treeView.Nodes, cache, namespaceName, className, lang);
                }
                finally
                {
                    treeView.EndUpdate();
                }
            }
        }

        /// <summary>
        /// 私有辅助方法：递归地翻译节点集合。
        /// </summary>
        private void TranslateNodesRecursive(TreeNodeCollection nodes, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        { 
            foreach (TreeNode node in nodes)
            {
                // 节点的 Name 属性是生成 Key 的关键，如果为空则跳过
                if (string.IsNullOrEmpty(node.Name)) continue;

                // 三段式键：{类型}.{节点名}.{属性名}（节点文本属性为 Text，如 UI.menuName.Text）
                var entry = cache.Get(namespaceName, className, node.Name + ".Text", ResourceType.UI);
                if (entry?.Type == ResourceType.UI)
                {
                    var text = entry.GetText(lang);
                    if (!string.IsNullOrEmpty(text))
                        node.Text = text;
                }

                // 如果当前节点有子节点，则递归调用自身
                if (node.Nodes.Count > 0)
                {
                    TranslateNodesRecursive(node.Nodes, cache, namespaceName, className, lang);
                }
            }
        }

        /// <summary>
        /// 公共入口：启动对 TreeView 节点的资源提取过程。
        /// </summary>
        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            if (control is TreeView treeView)
            {
                return ExtractNodeEntriesRecursive(treeView.Nodes, namespaceName, className);
            }
            return Enumerable.Empty<ResourceEntry>();
        }

        /// <summary>
        /// 私有辅助方法：递归地提取节点集合中的资源。
        /// </summary>
        private IEnumerable<ResourceEntry> ExtractNodeEntriesRecursive(TreeNodeCollection nodes, string namespaceName, string className)
        {
            var entries = new List<ResourceEntry>();
            foreach (TreeNode node in nodes)
            {
                // 同时检查 Text 和 Name，确保能生成有效的资源条目
                if (!string.IsNullOrEmpty(node.Text) && !string.IsNullOrEmpty(node.Name))
                {
                    // 三段式键：{类型}.{节点名}.{属性名}
                    var entry = ResourceEntry.CreateUI(namespaceName, className, node.Name + ".Text", node.Text);
                    entries.Add(entry);
                }

                // 如果有子节点，则递归提取并添加到结果列表中
                if (node.Nodes.Count > 0)
                {
                    entries.AddRange(ExtractNodeEntriesRecursive(node.Nodes, namespaceName, className));
                }
            }
            return entries;
        }
    }
}