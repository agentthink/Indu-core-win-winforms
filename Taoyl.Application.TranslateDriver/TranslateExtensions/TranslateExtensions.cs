using System;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Management;

namespace Taoyl.Application.TranslateDriver.TranslateExtensions
{
    public static class TranslateExtensions
    {
        /// <summary>
        /// 实例方法翻译，context 传入 this。若资源不存在，则自动创建并注册（fallback 作为原文）。
        /// </summary>
        /// <param name="fallback">原文，当无翻译时回退显示。</param>
        /// <param name="context">上下文对象（通常传 this）。</param>
        /// <param name="key">语义键（可选，默认等于 fallback）。</param>
        /// <param name="args">格式化参数。</param>
        public static string ToTranslate(this string fallback, object context, string key = null, params object[] args)
        {
            if (context == null) throw new ArgumentNullException(nameof(context));
            return fallback.ToTranslate(context.GetType(), key ?? fallback, args);
        }

        /// <summary>
        /// 泛型静态方法翻译，适用于无法获取实例的静态上下文。
        /// </summary>
        /// <typeparam name="T">当前类类型</typeparam>
        /// <param name="fallback">原文</param>
        /// <param name="key">语义键</param>
        /// <param name="args">格式化参数</param>
        public static string ToTranslate<T>(this string fallback, string key, params object[] args) where T : class
        {
            return fallback.ToTranslate(typeof(T), key ?? fallback, args);
        }

        /// <summary>
        /// 通过 Type 指定类型的静态翻译方法。
        /// </summary>
        /// <param name="fallback">原文</param>
        /// <param name="contextType">上下文类型（通过 typeof 获取）</param>
        /// <param name="key">语义键</param>
        /// <param name="args">格式化参数</param>
        public static string ToTranslate(this string fallback, Type contextType, string key = null, params object[] args)
        {
            if (contextType == null) throw new ArgumentNullException(nameof(contextType));
            return ToTranslateCore(fallback, contextType, key ?? fallback, args);
        }

        private static string ToTranslateCore(string fallback, Type contextType, string key, object[] args)
        {
            var namespaceName = contextType.Namespace ?? "Default";
            var className = contextType.Name;

            // 调用 TransManager 翻译
            string translated = TransManager.Current.TranslateText(namespaceName, className, key, ResourceType.Text);

            // 未命中：按开关决定是否自动注册（默认 true 保持兼容；关闭后仅返回原文，无写副作用）
            if (translated == null)
            {
                if (TransManager.Current.AutoRegisterOnMiss)
                {
                    var entry = ResourceEntry.CreateText(namespaceName, className, key, fallback);
                    TransManager.Current.UpdateResource(entry);
                    translated = entry.GetText(TransManager.Current.CurrentLanguage);
                }
                else
                {
                    // 未命中仅返回原文；输出调试日志便于发现缺失翻译（静默回退会让缺翻译不可发现）
                    System.Diagnostics.Debug.WriteLine(
                        $"[TranslateDriver] 翻译未命中 {namespaceName}.{className} [{key}] 语言={TransManager.Current.CurrentLanguage}，回退原文");
                    translated = fallback;
                }
            }

            // 参数格式化（安全处理）
            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(translated, args);
                }
                catch (FormatException)
                {
                    // 格式化失败，返回未格式化的文本
                    return translated;
                }
            }

            return translated;
        }
    }
}