using System;
using System.Collections.Generic;
using System.IO;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.Configuration.Serialization;

namespace Taoyl.Application.Configuration.Services
{
    /// <summary>
    /// 通用配置管理服务实现，维护配置单例缓存，
    /// 提供首次加载、原子写入保存和批量保存能力。
    /// </summary>
    public class ConfigService : IConfigService
    {
        #region 构造函数

        private readonly IConfigSerializer serializer;
        private readonly Dictionary<string, ConfigBase> cache = new Dictionary<string, ConfigBase>();

        /// <summary>
        /// 构造配置服务实例。
        /// </summary>
        /// <param name="serializer">配置序列化器</param>
        public ConfigService(IConfigSerializer serializer)
        {
            this.serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
        }

        #endregion

        #region 配置获取

        /// <inheritdoc />
        public T Get<T>() where T : ConfigBase, new()
        {
            string key = typeof(T).Name;
            if (cache.TryGetValue(key, out ConfigBase cached))
                return (T)cached;

            string filePath = GetFilePath<T>();
            T config;

            if (File.Exists(filePath))
            {
                string json = File.ReadAllText(filePath);
                config = serializer.Deserialize<T>(json);
            }
            else
            {
                config = new T();
            }

            config.MarkClean();
            cache[key] = config;
            return config;
        }

        #endregion

        #region 配置保存

        /// <inheritdoc />
        public void Save<T>() where T : ConfigBase
        {
            string key = typeof(T).Name;
            if (!cache.TryGetValue(key, out ConfigBase config))
                return;

            if (!config.IsDirty)
                return;

            string json = serializer.Serialize(config);
            string filePath = GetFilePath<T>();
            AtomicWrite(filePath, json);

            config.MarkClean();
        }

        /// <inheritdoc />
        public void SaveAll()
        {
            foreach (var kvp in cache)
            {
                if (!kvp.Value.IsDirty)
                    continue;

                string filePath = GetFilePath(kvp.Value.GetType());
                string json = serializer.Serialize(kvp.Value);
                AtomicWrite(filePath, json);

                kvp.Value.MarkClean();
            }
        }

        #endregion

        #region 文件操作辅助

        /// <summary>
        /// 根据配置类型获取对应的 JSON 文件路径。
        /// </summary>
        private static string GetFilePath<T>() where T : ConfigBase
        {
            return GetFilePath(typeof(T));
        }

        /// <summary>
        /// 根据配置类型获取对应的 JSON 文件路径。
        /// 特殊映射：DevConfig → Config/DevConfig.json，BaseConfig → Config/BaseConfig.json。
        /// </summary>
        private static string GetFilePath(Type type)
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;

            if (type == typeof(DevConfig))
                return Path.Combine(baseDir, "Config", "DevConfig.json");
            if (type == typeof(BaseConfig))
                return Path.Combine(baseDir, "Config", "BaseConfig.json");

            return Path.Combine(baseDir, "Config", type.Name + ".json");
        }

        /// <summary>
        /// 原子写入文件：先备份旧文件到 Backup/ 目录（带时间戳），再写 .tmp，
        /// 删除原文件后将 .tmp 重命名为正式文件名。
        /// </summary>
        /// <param name="filePath">目标文件路径</param>
        /// <param name="json">要写入的 JSON 字符串</param>
        private static void AtomicWrite(string filePath, string json)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            // 1. 备份旧文件到 Backup/（多版本保留）
            if (File.Exists(filePath))
            {
                string backupDir = Path.Combine(dir, "Backup");
                if (!Directory.Exists(backupDir))
                    Directory.CreateDirectory(backupDir);
                string fileName = Path.GetFileNameWithoutExtension(filePath);
                string ts = DateTime.Now.ToString("yyyyMMddHHmmss");
                string backupPath = Path.Combine(backupDir, $"{fileName}_{ts}.json");
                File.Copy(filePath, backupPath);
            }

            // 2. 写入临时文件
            File.WriteAllText(filePath + ".tmp", json);

            // 3. 删除原文件
            if (File.Exists(filePath))
                File.Delete(filePath);

            // 4. 重命名临时文件
            File.Move(filePath + ".tmp", filePath);
        }

        #endregion
    }
}
