using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Taoyl.Application.TranslateDriver.Core
{
    /// <summary>
    /// 资源条目实体
    /// 键生成策略："{ResourceType}.{ControlName|语义键|8位Guid}"（与 ResourceCache 的子键格式保持一致）
    /// </summary>
    public class ResourceEntry
    {
        /// <summary>
        /// 资源唯一键（自动生成）
        /// 格式：{Type}.{ControlName|Guid}
        /// </summary>
        [DisplayName("资源键")]
        public string Key { get; set; }

        /// <summary>所属程序集（命名空间）</summary>
        [DisplayName("命名空间")]
        public string Namespace { get; set; }

        /// <summary>所属类名</summary>
        [DisplayName("类名")]
        public string ClassName { get; set; }

        /// <summary>关联控件名（UI资源时有值）</summary>
        [DisplayName("控件名/语义键")]
        public string ControlName { get; set; }

        /// <summary>资源类型（UI/Text）</summary>
        [DisplayName("资源类型")]
        public ResourceType Type { get; set; }

        /// <summary>
        /// 当前基准源文本（允许被提取/导入更新）。
        /// 最佳实践：始终在基准语言（如中文）环境下进行首次提取，以保证原文正确。
        /// </summary>
        [DisplayName("原文")]
        public string OriginalText { get; set; }

        /// <summary>多语言翻译字典</summary>
        public Dictionary<string, string> Translations { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// 生成规范的资源键
        /// 规则：{Type}.{ControlName}，ControlName 为空时用 8 位 Guid
        /// </summary>
        public void GenerateKey()
        {
            var typePrefix = Type.ToString(); // UI 或 Text
            var controlPart = string.IsNullOrEmpty(ControlName)
                ? Guid.NewGuid().ToString("N").Substring(0, 8)
                : ControlName;
            Key = $"{typePrefix}.{controlPart}";
        }

        /// <summary>
        /// 获取指定语言的翻译
        /// </summary>
        public string GetText(LanguageType lang)
        {
            var langKey = lang.ToString().Replace("_", "-");
            return Translations.TryGetValue(langKey, out var text) && !string.IsNullOrEmpty(text) ? text : OriginalText;
        }

        /// <summary>
        /// 设置翻译文本
        /// </summary>
        public void SetText(LanguageType lang, string text)
        {
            var langKey = lang.ToString().Replace("_", "-");
            Translations[langKey] = text;
        }

        // 移除 GetFullKey 方法

        #region 工厂方法

        /// <summary>
        /// 创建UI资源（自动生键）
        /// </summary>
        public static ResourceEntry CreateUI(string assembly, string className, string controlName, string originalText)
        {
            var entry = new ResourceEntry
            {
                Namespace = assembly,
                ClassName = className,
                ControlName = controlName,
                Type = ResourceType.UI,
                OriginalText = originalText ?? string.Empty
            };
            entry.GenerateKey();
            return entry;
        }

        /// <summary>
        /// 创建文本资源（自动生键）
        /// </summary>
        public static ResourceEntry CreateText(string assembly, string className, string semanticKey, string originalText)
        {
            var entry = new ResourceEntry
            {
                Namespace = assembly,
                ClassName = className,
                ControlName = semanticKey,
                Type = ResourceType.Text,
                OriginalText = originalText ?? string.Empty
            };
            entry.GenerateKey();
            return entry;
        }

        #endregion
    }
}