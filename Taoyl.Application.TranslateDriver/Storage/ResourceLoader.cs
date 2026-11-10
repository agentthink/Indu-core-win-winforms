using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Taoyl.Application.TranslateDriver.Core;

namespace Taoyl.Application.TranslateDriver.Storage
{
    public class ResourceLoader : IResourceLoader
    {
        private readonly string _basePath;
        private readonly JsonSerializerSettings _jsonSettings;

        public ResourceLoader(string basePath = "./Language")
        {
            _basePath = basePath;
            _jsonSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.Indented,
                NullValueHandling = NullValueHandling.Include,
                DefaultValueHandling = DefaultValueHandling.Include
            };
        }

        public void LoadAll(IResourceCache cache)
        {
            if (!Directory.Exists(_basePath)) return;
            foreach (ResourceType type in Enum.GetValues(typeof(ResourceType)))
            {
                var typeDir = type.ToString();
                var path = Path.Combine(_basePath, typeDir);
                if (!Directory.Exists(path)) continue;
                foreach (var file in Directory.GetFiles(path, "*.json", SearchOption.AllDirectories))
                    LoadFile(file, cache);
            }
        }

        public void LoadFile(string filePath, IResourceCache cache)
        {
            try
            {
                var json = File.ReadAllText(filePath);
                var entries = JsonConvert.DeserializeObject<List<ResourceEntry>>(json, _jsonSettings);
                if (entries != null)
                {
                    foreach (var entry in entries)
                    {
                        if (string.IsNullOrEmpty(entry.Namespace) || string.IsNullOrEmpty(entry.ClassName))
                        {
                            InferAssemblyClassFromPath(filePath, out var asm, out var cls);
                            entry.Namespace = entry.Namespace ?? asm;
                            entry.ClassName = entry.ClassName ?? cls;
                        }

                        // 键策略：文件已有的 Key 原样保留；缺失时才按 "{Type}.{ControlName|8位Guid}" 兜底生成
                        // （修复原无条件 GenerateKey 导致每次加载重新生成 GUID、键分裂冲突堆积的问题）
                        if (string.IsNullOrEmpty(entry.Key))
                            entry.GenerateKey();

                        cache.AddOrUpdate(entry);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TranslateDriver] 加载资源文件失败 {filePath}: {ex.Message}");
            }
        }

        public bool SaveClassResources(List<ResourceEntry> entries, ResourceType type)
        {
            if (!entries.Any()) return false;
            var first = entries.First();
            var typeDir = type.ToString();
            var dir = Path.Combine(_basePath, typeDir, first.Namespace);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            var filePath = Path.Combine(dir, $"{first.ClassName}.json");
            try
            {
                var json = JsonConvert.SerializeObject(entries, _jsonSettings);
                File.WriteAllText(filePath, json);
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[TranslateDriver] 保存资源文件失败 {filePath}: {ex.Message}");
                return false;
            }
        }

        private void InferAssemblyClassFromPath(string filePath, out string nameSpace, out string className)
        {
            className = Path.GetFileNameWithoutExtension(filePath);
            nameSpace = "Unknown";
            try
            {
                var dir = Path.GetDirectoryName(filePath);
                // 路径结构：BasePath/UI/AssemblyName/ClassName.json
                // 取父目录名作为 AssemblyName
                var parentDir = Path.GetFileName(dir);
                nameSpace = parentDir;
            }
            catch { }
        }
    }
}