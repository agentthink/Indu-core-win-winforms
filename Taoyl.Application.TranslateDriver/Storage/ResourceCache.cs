using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Taoyl.Application.TranslateDriver.Core;

namespace Taoyl.Application.TranslateDriver.Storage
{
    /// <summary>
    /// 表示一个线程安全的、用于存储和检索本地化资源的内存缓存。
    /// 此缓存采用两级字典结构以实现高效的分层存储。
    /// </summary>
    public class ResourceCache : IResourceCache
    {
        // 第一层键: "{命名空间}.{类名}" (例如: "FormMain.FormMain")
        // 第二层键: "{资源类型}.{控件名或语义键}" (例如: "UI.ButtonLogin" 或 "Text.LoginFailed")
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, ResourceEntry>> _resources =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ResourceEntry>>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// 获取在添加或更新过程中，因尝试覆盖已有原文而检测到的冲突日志集合。
        /// </summary>
        public ConcurrentBag<string> ConflictLogs { get; } = new ConcurrentBag<string>();

        /// <summary>
        /// 当一个新的类（Namespace.ClassName）首次被添加到缓存时触发。
        /// </summary>
        public event EventHandler<ClassChangedEventArgs> ClassChanged;

        /// <summary>
        /// 原子地添加或更新一个资源条目。若条目属于新类，会触发 ClassChanged 事件。
        /// </summary>
        public void AddOrUpdate(ResourceEntry entry)
        {
            if (string.IsNullOrEmpty(entry.Namespace) || string.IsNullOrEmpty(entry.ClassName))
                throw new ArgumentException("资源条目必须拥有一个有效的命名空间和类名。");

            if (string.IsNullOrEmpty(entry.Key))
                entry.GenerateKey();

            var classKey = $"{entry.Namespace}.{entry.ClassName}";
            // 单一事实来源：dict 键与 entry.Key 保持一致（修复键生成分裂）
            var localKey = entry.Key;

            // 利用 GetOrAdd 工厂委托检测是否为新类，并原子获取/创建内层字典
            bool isNewClass = false;
            var classDict = _resources.GetOrAdd(classKey, key =>
            {
                isNewClass = true;
                return new ConcurrentDictionary<string, ResourceEntry>(StringComparer.OrdinalIgnoreCase);
            });

            // 原子地添加或合并条目
            classDict.AddOrUpdate(localKey,
                // 不存在 → 直接添加
                key => entry,
                // 已存在 → 合并翻译
                (key, existing) =>
                {
                    if (existing.OriginalText != entry.OriginalText && !string.IsNullOrEmpty(existing.OriginalText))
                    {
                        var log = $"[原文保留] 类='{classKey}', 局部键='{localKey}', 保留原文='{existing.OriginalText}', 忽略新原文='{entry.OriginalText}'";
                        ConflictLogs.Add(log);
                        System.Diagnostics.Debug.WriteLine(log);
                    }
                    foreach (var translation in entry.Translations)
                        existing.Translations[translation.Key] = translation.Value;
                    return existing;
                });

            // 只有创建了类字典的线程才触发事件，确保准确且不重复
            if (isNewClass)
                OnClassChanged(new ClassChangedEventArgs(entry.Namespace, entry.ClassName));
        }

        /// <summary>
        /// 向缓存中批量添加或更新一系列资源条目。
        /// </summary>
        /// <param name="entries">要添加或更新的资源条目集合。</param>
        public void AddRange(IEnumerable<ResourceEntry> entries)
        {
            foreach (var entry in entries)
            {
                AddOrUpdate(entry);
            }
        }

        /// <summary>
        /// 根据命名空间、类名、控件名和资源类型精确查找一个资源条目。
        /// </summary>
        /// <param name="namespaceName">资源的命名空间。</param>
        /// <param name="className">资源的类名。</param>
        /// <param name="controlName">控件名或语义键。</param>
        /// <param name="type">资源类型 (UI 或 Text)。</param>
        /// <returns>找到的资源条目，如果不存在则返回 null。</returns>
        public ResourceEntry Get(string namespaceName, string className, string controlName, ResourceType type)
        {
            var classKey = $"{namespaceName}.{className}";
            if (_resources.TryGetValue(classKey, out var classDict))
            {
                // 优先精确匹配（常规路径：键 = "{Type}.{ControlName}"）
                var localKey = $"{type}.{controlName}";
                if (classDict.TryGetValue(localKey, out var entry))
                {
                    return entry;
                }

                // 兜底：遍历查找 ControlName 匹配的条目
                // 兼容自定义 Key / GUID Key（空 ControlName）等键与 ControlName 不一致的历史条目
                var typePrefix = $"{type}.";
                foreach (var kvp in classDict)
                {
                    if (kvp.Key.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase)
                        && string.Equals(kvp.Value.ControlName, controlName, StringComparison.OrdinalIgnoreCase))
                    {
                        return kvp.Value;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// 获取指定类的所有资源条目。
        /// </summary>
        /// <param name="namespaceName">类的命名空间。</param>
        /// <param name="className">类名。</param>
        /// <returns>一个包含该类所有资源条目的可枚举集合。</returns>
        public IEnumerable<ResourceEntry> GetByClass(string namespaceName, string className)
        {
            var classKey = $"{namespaceName}.{className}";
            if (_resources.TryGetValue(classKey, out var classDict))
            {
                return classDict.Values.ToList(); // 返回副本以防止外部修改
            }
            return Enumerable.Empty<ResourceEntry>();
        }

        /// <summary>
        /// 获取指定类的特定类型的所有资源条目。
        /// </summary>
        /// <param name="namespaceName">类的命名空间。</param>
        /// <param name="className">类名。</param>
        /// <param name="type">要筛选的资源类型。</param>
        /// <returns>一个包含所有匹配的资源条目的可枚举集合。</returns>
        public IEnumerable<ResourceEntry> GetByClassAndType(string namespaceName, string className, ResourceType type)
        {
            var classKey = $"{namespaceName}.{className}";
            if (_resources.TryGetValue(classKey, out var classDict))
            {
                var typePrefix = $"{type}.";
                // 此处通过键前缀进行筛选，效率很高
                return classDict.Where(kvp => kvp.Key.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase))
                                .Select(kvp => kvp.Value)
                                .ToList(); // 返回副本
            }
            return Enumerable.Empty<ResourceEntry>();
        }

        /// <summary>
        /// 检查缓存中是否存在指定类的任何资源。
        /// </summary>
        /// <param name="namespaceName">类的命名空间。</param>
        /// <param name="className">类名。</param>
        /// <returns>如果存在该类的资源则为 true，否则为 false。</returns>
        public bool ContainsClass(string namespaceName, string className)
        {
            return _resources.ContainsKey($"{namespaceName}.{className}");
        }

        /// <summary>
        /// 检查缓存中是否存在指定类的指定类型（UI/Text）资源。
        /// 用于类型级懒加载判断：UI 已加载不阻塞 Text 的加载。
        /// </summary>
        public bool ContainsClassAndType(string namespaceName, string className, ResourceType type)
        {
            var classKey = $"{namespaceName}.{className}";
            if (_resources.TryGetValue(classKey, out var classDict))
            {
                var typePrefix = $"{type}.";
                return classDict.Keys.Any(k => k.StartsWith(typePrefix, StringComparison.OrdinalIgnoreCase));
            }
            return false;
        }

        /// <summary>
        /// 获取缓存中所有已加载的类键。
        /// </summary>
        /// <returns>一个包含所有类键（格式为 "命名空间.类名"）的可枚举集合。</returns>
        public IEnumerable<string> GetAllClassKeys() => _resources.Keys;

        /// <summary>
        /// 从缓存中移除指定类的所有资源。
        /// </summary>
        /// <param name="namespaceName">类的命名空间。</param>
        /// <param name="className">类名。</param>
        public void ClearClass(string namespaceName, string className)
        {
            _resources.TryRemove($"{namespaceName}.{className}", out _);
        }

        protected virtual void OnClassChanged(ClassChangedEventArgs e)
        {
            ClassChanged?.Invoke(this, e);
        }
    }

    /// <summary>
    /// 类变更事件参数
    /// </summary>
    public class ClassChangedEventArgs : EventArgs
    {
        public string Namespace { get; }
        public string ClassName { get; }

        public ClassChangedEventArgs(string ns, string cls)
        {
            Namespace = ns;
            ClassName = cls;
        }
    }
}