using Taoyl.Application.Configuration.Models;

namespace Taoyl.Application.Configuration.Interfaces
{
    /// <summary>
    /// 通用配置管理服务接口，提供泛型配置的获取、保存和批量保存能力。
    /// 同一类型配置在内存中只有一份共享实例。
    /// </summary>
    public interface IConfigService
    {
        /// <summary>
        /// 获取指定类型的配置单例，首次访问时自动从文件加载。
        /// </summary>
        /// <typeparam name="T">配置类型，必须继承 ConfigBase 且有无参构造函数</typeparam>
        /// <returns>配置实例</returns>
        T Get<T>() where T : ConfigBase, new();

        /// <summary>
        /// 保存指定类型配置到文件。仅当配置的 IsDirty 为 true 时才执行写入。
        /// 写入采用原子操作（.bak 备份 → .tmp 写入 → 替换原文件）。
        /// </summary>
        /// <typeparam name="T">配置类型</typeparam>
        void Save<T>() where T : ConfigBase;

        /// <summary>
        /// 批量保存所有已加载的脏配置。
        /// </summary>
        void SaveAll();
    }
}
