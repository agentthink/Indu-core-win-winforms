using System;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// SunnyUI UISwitch 运行时扩展翻译器（引擎不编译期依赖 SunnyUI）。
    /// UISwitch 继承 UIControl（无 WinForms 文本基类可依赖），开关两态文本
    /// ActiveText/InactiveText 是专属属性，默认翻译器无法覆盖。
    /// 通过 Type.GetType 运行时解析类型 + 反射属性读写，维持引擎零 SunnyUI 编译期依赖，
    /// 版本升级不受牵连（属性缺失时静默跳过）。
    /// 资源键约定：{控件Name}.Active / {控件Name}.Inactive 两条 UI 资源。
    /// </summary>
    public class UISwitchTranslator : IControlTranslator
    {
        /// <summary>UISwitch 类型（运行时解析；宿主无 SunnyUI 时为空，翻译器自动失效）</summary>
        private static readonly Type SwitchType = Type.GetType("Sunny.UI.UISwitch, SunnyUI");

        /// <summary>
        /// ActiveText/InActiveText 属性（反射缓存，属性缺失时为空则跳过）。
        /// 注意：SunnyUI 属性名是 InActiveText（I-n-A-ctive，中间大写 A），写错则反射不到、Inactive 分支静默失效
        /// </summary>
        private static readonly PropertyInfo ActiveTextProp = SwitchType?.GetProperty("ActiveText");
        private static readonly PropertyInfo InactiveTextProp = SwitchType?.GetProperty("InActiveText");

        public bool CanTranslate(Control control)
        {
            return SwitchType != null && SwitchType.IsInstanceOfType(control);
        }

        public void Translate(Control control, IResourceCache cache, string namespaceName, string className, LanguageType lang)
        {
            if (!CanTranslate(control) || string.IsNullOrEmpty(control.Name)) return;

            SetText(control, cache, namespaceName, className, lang, control.Name + ".Active", ActiveTextProp);
            SetText(control, cache, namespaceName, className, lang, control.Name + ".Inactive", InactiveTextProp);
        }

        public IEnumerable<ResourceEntry> ExtractEntries(Control control, string namespaceName, string className)
        {
            if (!CanTranslate(control) || string.IsNullOrEmpty(control.Name)) yield break;

            string activeText = ActiveTextProp?.GetValue(control) as string;
            string inactiveText = InactiveTextProp?.GetValue(control) as string;

            if (!string.IsNullOrEmpty(activeText))
                yield return ResourceEntry.CreateUI(namespaceName, className, control.Name + ".Active", activeText);

            if (!string.IsNullOrEmpty(inactiveText))
                yield return ResourceEntry.CreateUI(namespaceName, className, control.Name + ".Inactive", inactiveText);
        }

        /// <summary>按资源键翻译单个专属属性（未命中/属性缺失静默跳过）</summary>
        private static void SetText(Control control, IResourceCache cache, string namespaceName, string className,
            LanguageType lang, string key, PropertyInfo prop)
        {
            if (prop == null) return;

            var entry = cache.Get(namespaceName, className, key, ResourceType.UI);
            if (entry?.Type == ResourceType.UI)
            {
                var text = entry.GetText(lang);
                if (!string.IsNullOrEmpty(text))
                    prop.SetValue(control, text);
            }
        }
    }
}
