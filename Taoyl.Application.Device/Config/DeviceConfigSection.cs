using System;
using System.Collections;
using Taoyl.Application.Device.Interfaces;

namespace Taoyl.Application.Device.Config
{
    /// <summary>
    /// 通用设备配置节：把「设备类型 → 根模型里的哪个列表」这条映射参数化。
    /// 根模型由泛型参数给出（TModel），底座不认识 DevicesConfigFile 之类的具体模型；
    /// 具体映射由实现层在注册时用 lambda 给出（listOf / nameOf）。
    /// 用途：DeviceManager 配置域（查/增/改/删）与编排域（类型校验/名称提取/本类型节查重）统一经
    /// IDeviceConfigSection 访问节，消除「加设备类型漏改分支 = 静默丢配置/静默无效」隐患。
    /// 新增设备类型 = 根模型加一个列表 + 本类实例注册一行（DeviceManager 主体零改动）。
    /// ⚠️ 节内增删改仍由编排方（DeviceManager）在锁内操作后 Save 持久化（本类只管映射与操作封装，不落盘）。
    /// </summary>
    /// <typeparam name="TModel">根模型类型（如 DevicesConfigFile）</typeparam>
    public sealed class DeviceConfigSection<TModel> : IDeviceConfigSection where TModel : class
    {
        private readonly Func<TModel, IList> _listOf;
        private readonly Func<object, string> _nameOf;

        public DeviceConfigSection(string deviceType, string configNode, Type configType,
            Func<TModel, IList> listOf, Func<object, string> nameOf)
        {
            DeviceType = deviceType ?? throw new ArgumentNullException(nameof(deviceType));
            ConfigNode = configNode ?? throw new ArgumentNullException(nameof(configNode));
            ConfigType = configType ?? throw new ArgumentNullException(nameof(configType));
            _listOf = listOf ?? throw new ArgumentNullException(nameof(listOf));
            _nameOf = nameOf ?? throw new ArgumentNullException(nameof(nameOf));
        }

        /// <summary>设备类型标识（由实现层定义，如 "PLC"）</summary>
        public string DeviceType { get; }

        /// <summary>配置文件中的节点名（JSON 键 / Excel 表名）</summary>
        public string ConfigNode { get; }

        /// <summary>设备配置 CLR 类型（PlcConfig / ScannerConfig / ScaleConfig…）</summary>
        public Type ConfigType { get; }

        /// <summary>校验 config 与本设备类型匹配并取配置名（类型不符抛自解释 ArgumentException）</summary>
        public string GetName(object config)
        {
            return _nameOf(config);
        }

        // ---------- 强类型实现（类内私有：契约侧一律 object，避免把 object 重载暴露成公开 API） ----------

        /// <summary>取根模型的对应节列表（节属性已初始化，非空）</summary>
        private IList GetList(TModel model)
        {
            return _listOf(model);
        }

        /// <summary>节内按名称查是否存在（Ordinal，与存储原值比较）</summary>
        private bool Contains(TModel model, string name)
        {
            var list = GetList(model);
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(GetName(list[i]), name, StringComparison.Ordinal))
                    return true;
            }
            return false;
        }

        /// <summary>节内 upsert：同名命中原位替换（保列表顺序稳定）；未命中追加</summary>
        private void Upsert(TModel model, object config)
        {
            var list = GetList(model);
            string name = GetName(config);
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(GetName(list[i]), name, StringComparison.Ordinal))
                {
                    list[i] = config;
                    return;
                }
            }
            list.Add(config);
        }

        /// <summary>节内按名称移除（返回是否命中）</summary>
        private bool Remove(TModel model, string name)
        {
            var list = GetList(model);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                if (string.Equals(GetName(list[i]), name, StringComparison.Ordinal))
                {
                    list.RemoveAt(i);
                    return true;
                }
            }
            return false;
        }

        /// <summary>根模型类型校验（契约侧 object → 实现侧强类型）</summary>
        private static TModel RequireModel(object model)
        {
            if (!(model is TModel typed))
                throw new ArgumentException($"设备配置根模型必须是 {typeof(TModel).Name} 对象");
            return typed;
        }

        // ---------- IDeviceConfigSection 显式实现（契约侧根模型是 object） ----------

        IList IDeviceConfigSection.GetList(object model)
        {
            return GetList(RequireModel(model));
        }

        bool IDeviceConfigSection.Contains(object model, string name)
        {
            return Contains(RequireModel(model), name);
        }

        void IDeviceConfigSection.Upsert(object model, object config)
        {
            Upsert(RequireModel(model), config);
        }

        bool IDeviceConfigSection.Remove(object model, string name)
        {
            return Remove(RequireModel(model), name);
        }
    }
}
