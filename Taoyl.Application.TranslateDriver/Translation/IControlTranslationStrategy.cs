using System;
using System.Windows.Forms;

namespace Taoyl.Application.TranslateDriver.Translation
{
    /// <summary>
    /// 控件翻译策略中心契约。
    /// 三级策略：实例翻译器 → 精确类型映射 → 默认通用翻译器；排除类型优先于一切。
    /// </summary>
    public interface IControlTranslationStrategy
    {
        /// <summary>
        /// 获取最适合指定控件的翻译器。
        /// </summary>
        IControlTranslator GetTranslator(Control control);

        /// <summary>
        /// 注册或覆盖某控件类型的精确翻译器。
        /// </summary>
        void RegisterExactTranslator(Type controlType, IControlTranslator translator);

        /// <summary>
        /// 泛型便捷注册某控件类型的精确翻译器。
        /// </summary>
        void RegisterExactTranslator<T>(IControlTranslator translator) where T : Control;

        /// <summary>
        /// 移除某控件类型的精确翻译器（恢复默认行为）。
        /// </summary>
        bool UnregisterExactTranslator(Type controlType);

        /// <summary>
        /// 批量添加排除类型（及其所有子类）。
        /// </summary>
        void AddExcludedTypes(params Type[] types);

        /// <summary>
        /// 移除某类型的排除状态（重新参与翻译）。
        /// </summary>
        bool RemoveExcludedType(Type controlType);

        /// <summary>
        /// 注册一个手动控件翻译器（基于委托的实例翻译器）。
        /// </summary>
        void RegisterManualTranslator(Control control, Action<Control, string> applyTranslation, Func<Control, string> extractText);
    }
}
