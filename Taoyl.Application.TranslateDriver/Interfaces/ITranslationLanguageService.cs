using System;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Management;

namespace Taoyl.Application.TranslateDriver.Interfaces
{
    /// <summary>
    /// 翻译语言契约（面向 VM / 非 UI 消费方）。
    /// 只暴露语言状态与切换事件，不含任何 WinForms 类型——
    /// ViewModel 订阅 LanguageChanged 重算动态文本（ToTranslate 动态文本链路）用。
    /// 由 TransManager 实现（与 ITranslationService 同一单例）。
    /// </summary>
    public interface ITranslationLanguageService
    {
        /// <summary>
        /// 获取或设置当前的显示语言。设置新语言后会自动触发全局刷新。
        /// </summary>
        LanguageType CurrentLanguage { get; set; }

        /// <summary>
        /// 当前语言发生改变时触发的事件。
        /// </summary>
        event EventHandler<LanguageChangedEventArgs> LanguageChanged;

        /// <summary>
        /// ToTranslate 未命中资源时是否自动注册新条目（默认 false：读路径无副作用，翻译提取统一走 FrmResourceConfig"获取UI资源"）。
        /// 需要时（如海量文本全球化场景）可手动开启。
        /// </summary>
        bool AutoRegisterOnMiss { get; set; }
    }
}
