using System.Collections.Generic;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// 控件翻译器接口
    /// 实现类负责特定类型控件的翻译逻辑
    /// </summary>
    public interface IControlTranslator
    {
        /// <summary>
        /// 判断是否能处理指定控件
        /// </summary>
        /// <param name="control">待翻译控件</param>
        /// <returns>是否支持</returns>
        bool CanTranslate(Control control);

        /// <summary>
        /// 执行翻译
        /// </summary>
        /// <param name="control">控件实例</param>
        /// <param name="cache">资源缓存</param>
        /// <param name="assembly">程序集名</param>
        /// <param name="className">类名</param>
        /// <param name="lang">目标语言</param>
        void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang);

        /// <summary>
        /// 从控件中提取需要翻译的资源条目（UI资源专用）
        /// </summary>
        /// <returns>资源条目列表，若未实现则返回空集合</returns>
        IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className);
        
    }
}