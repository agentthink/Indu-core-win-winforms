using System;
using System.Collections.Generic;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Storage;

namespace Taoyl.Application.TranslateDriver.Interfaces
{
    /// <summary>
    /// 资源管理契约（面向翻译资源配置工具，如 FrmResourceConfig）。
    /// 由 TransManager 实现，供资源管理界面注入使用。
    /// </summary>
    public interface IResourceManager
    {
        /// <summary>
        /// 获取或设置存储资源文件的根目录路径。
        /// </summary>
        string BasePath { get; set; }

        /// <summary>
        /// 当一个新的类资源被添加到缓存时触发（用于界面树刷新）。
        /// </summary>
        event EventHandler<ClassChangedEventArgs> ClassChanged;

        /// <summary>
        /// 获取指定类的全部资源条目（含与本地文件的合并逻辑）。
        /// </summary>
        IEnumerable<ResourceEntry> GetClassResourcesMerged(string namespaceName, string className);

        /// <summary>
        /// 从 JSON 文件读取资源条目列表。
        /// </summary>
        List<ResourceEntry> LoadEntriesFromFile(string filePath);

        /// <summary>
        /// 批量添加或更新资源条目。
        /// </summary>
        void AddRange(IEnumerable<ResourceEntry> entries);

        /// <summary>
        /// 清空指定类的缓存资源。
        /// </summary>
        void ClearClass(string namespaceName, string className);

        /// <summary>
        /// 保存资源条目到 JSON 文件。
        /// </summary>
        bool SaveClassResources(List<ResourceEntry> entries, ResourceType type);

        /// <summary>
        /// 从已打开的窗体实例中提取所有可翻译的 UI 资源。
        /// </summary>
        List<ResourceEntry> ExtractUIResources(string namespaceName, string className);
    }
}
