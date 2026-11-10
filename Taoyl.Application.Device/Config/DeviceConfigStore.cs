using Newtonsoft.Json;
using System;
using System.IO;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Device.Config
{
    /// <summary>
    /// 通用设备配置文件存储（JSON 全量模型镜像）。
    /// 根模型由调用方以泛型参数给出（TModel）——底座不认识任何具体设备配置模型，
    /// 只负责「读文件 → 反序列化成 TModel → 内存镜像 → 落盘」这条与格式相关的链路。
    /// 用途：第二个使用方写自己的根模型 POCO 即可复用本类，无须重写存储。
    /// Load = 反序列化（未知节点/键忽略——模型外键保存即丢，边界由 TModel 的形状决定）；
    /// Save = 先 .bak 备份现有文件 → JsonConvert Indented 整文件重写。
    /// 线程模型：编排方（DeviceManager）持锁调用，本类不做内部同步。
    /// </summary>
    /// <typeparam name="TModel">根模型类型（如 DevicesConfigFile），须有无参构造</typeparam>
    public class DeviceConfigStore<TModel> : IDeviceConfigStore where TModel : class, new()
    {
        private string filePath;

        /// <summary>根模型（具体类型由泛型参数决定；对外经 IDeviceConfigStore.Model 以 object 暴露）</summary>
        private TModel model = new TModel();

        /// <inheritdoc />
        public string FilePath => filePath;

        /// <inheritdoc />
        public object Model => model;

        /// <inheritdoc />
        public void Load(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("设备配置文件路径不能为空", nameof(path));

            filePath = path;
            if (!File.Exists(path))
            {
                // 文件不存在 → 空模型（首次 Save 即新建）；不抛——启动期缺失由调用方（DeviceManager.LoadFromConfigFile）先行判错
                model = new TModel();
                return;
            }

            string text = File.ReadAllText(path);
            model = string.IsNullOrWhiteSpace(text)
                ? new TModel()
                : JsonConvert.DeserializeObject<TModel>(text) ?? new TModel();
        }

        /// <inheritdoc />
        public void Save()
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new InvalidOperationException("设备配置文件尚未加载（LoadFromConfigFile 未执行），无法落盘");

            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory))
                Directory.CreateDirectory(directory);

            // 现有文件先 .bak 备份（覆盖式保留最近一版，供回滚）
            if (File.Exists(filePath))
                File.Copy(filePath, filePath + ".bak", overwrite: true);

            string json = JsonConvert.SerializeObject(model, Formatting.Indented);
            File.WriteAllText(filePath, json);
        }
    }
}
