using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// 空操作翻译器，用于排除不需要翻译/提取的控件类型
    /// </summary>
    internal class NoOpTranslator : IControlTranslator
    {
        public bool CanTranslate(Control control) => true;

        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            // 无操作
        }

        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            return Enumerable.Empty<ResourceEntry>();
        }
    }
}
