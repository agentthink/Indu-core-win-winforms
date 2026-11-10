using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.Configuration.Models
{
    /// <summary>
    /// LogConfig.json 根配置 —— 日志系统独立顶层配置（与 DevConfig/BaseConfig 并列）。
    /// 🔴 5 个固定属性（不用列表）：编译期保证 5 个通道一个不少，
    /// 且子属性均为 ConfigBase ⇒ ConfigBase 的递归判脏天然生效。
    /// 🔴 任何改动一律要求进程重启生效（不做热生效）；
    /// 落盘链路由 ConfigService.GetFilePath 按类型名解析到 Config/LogConfig.json。
    /// </summary>
    public class LogConfig : ConfigBase, ILogSettings
    {
        /// <summary>
        /// 日志根目录默认值（字段初始化基准；模板 LogConfig.json 同值分发）。
        /// </summary>
        public const string DefaultRootPath = "D:\\Logs";

        private string rootPath = DefaultRootPath;
        private LogFileEncoding encoding = LogFileEncoding.Utf8Bom;
        private LogCategoryPolicyConfig debug = CreatePolicy(LogCategory.Debug);
        private LogCategoryPolicyConfig info = CreatePolicy(LogCategory.Info);
        private LogCategoryPolicyConfig warn = CreatePolicy(LogCategory.Warn);
        private LogCategoryPolicyConfig error = CreatePolicy(LogCategory.Error);
        private LogCategoryPolicyConfig mes = CreatePolicy(LogCategory.MES);

        /// <summary>
        /// 日志根目录（如 D:\Logs；每类别子目录挂其下）。
        /// </summary>
        [DisplayName("日志根目录")]
        public string RootPath
        {
            get => rootPath;
            set => SetProperty(ref rootPath, value);
        }

        /// <summary>
        /// 日志文件写入编码（全局统一，默认 Utf8Bom：CSV + Excel 中文友好的前提）。
        /// </summary>
        [DisplayName("文件编码")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogFileEncoding Encoding
        {
            get => encoding;
            set => SetProperty(ref encoding, value);
        }

        /// <summary>
        /// Debug 通道策略。
        /// </summary>
        [DisplayName("Debug 通道")]
        public LogCategoryPolicyConfig Debug
        {
            get => debug;
            set => SetProperty(ref debug, value);
        }

        /// <summary>
        /// Info 通道策略。
        /// </summary>
        [DisplayName("Info 通道")]
        public LogCategoryPolicyConfig Info
        {
            get => info;
            set => SetProperty(ref info, value);
        }

        /// <summary>
        /// Warn 通道策略。
        /// </summary>
        [DisplayName("Warn 通道")]
        public LogCategoryPolicyConfig Warn
        {
            get => warn;
            set => SetProperty(ref warn, value);
        }

        /// <summary>
        /// Error 通道策略。
        /// </summary>
        [DisplayName("Error 通道")]
        public LogCategoryPolicyConfig Error
        {
            get => error;
            set => SetProperty(ref error, value);
        }

        /// <summary>
        /// MES 通道策略。
        /// </summary>
        [DisplayName("MES 通道")]
        public LogCategoryPolicyConfig MES
        {
            get => mes;
            set => SetProperty(ref mes, value);
        }

        /// <summary>
        /// 按通道取落盘策略（ILogSettings 契约：switch 到固定属性）。
        /// 加类别时此处新增 case 会因枚举穷尽检查而显式暴露，default 保持 fail fast。
        /// </summary>
        public ILogCategoryPolicy GetPolicy(LogCategory category)
        {
            switch (category)
            {
                case LogCategory.Debug: return Debug;
                case LogCategory.Info: return Info;
                case LogCategory.Warn: return Warn;
                case LogCategory.Error: return Error;
                case LogCategory.MES: return MES;
                default:
                    throw new ArgumentOutOfRangeException(nameof(category), category, "未知的日志通道");
            }
        }

        /// <summary>
        /// 构造带默认通道标识的策略实例（仅用于字段初始化器；JSON 反序列化按 Replace 语义覆盖）。
        /// </summary>
        private static LogCategoryPolicyConfig CreatePolicy(LogCategory category)
        {
            return new LogCategoryPolicyConfig { Category = category };
        }
    }
}
