using System.Collections.Generic;
using Taoyl.Application.TranslateDriver.Core;

namespace Taoyl.Application.TranslateDriver.Storage
{
    /// <summary>
    /// 资源缓存核心操作接口
    /// </summary>
    public interface IResourceCache
    {
        /// <summary>添加或更新一个资源条目</summary>
        void AddOrUpdate(ResourceEntry entry);

        /// <summary>批量添加或更新</summary>
        void AddRange(IEnumerable<ResourceEntry> entries);

        /// <summary>精确查找一个资源条目</summary>
        ResourceEntry Get(string namespaceName, string className, string controlName, ResourceType type);

        /// <summary>获取指定类的所有资源</summary>
        IEnumerable<ResourceEntry> GetByClass(string namespaceName, string className);

        /// <summary>判断指定类是否存在资源</summary>
        bool ContainsClass(string namespaceName, string className);

        /// <summary>判断指定类是否存在指定类型（UI/Text）的资源</summary>
        bool ContainsClassAndType(string namespaceName, string className, ResourceType type);

        /// <summary>获取所有已加载的类键</summary>
        IEnumerable<string> GetAllClassKeys();

        /// <summary>清除指定类的所有资源</summary>
        void ClearClass(string namespaceName, string className);
    }

    /// <summary>
    /// 资源文件加载/保存接口（面向可测试性）
    /// </summary>
    public interface IResourceLoader
    {
        /// <summary>加载所有语言资源文件到缓存</summary>
        void LoadAll(IResourceCache cache);

        /// <summary>加载指定文件到缓存</summary>
        void LoadFile(string filePath, IResourceCache cache);

        /// <summary>保存指定类型的一批条目到文件</summary>
        bool SaveClassResources(List<ResourceEntry> entries, ResourceType type);
    }
}