using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.Configuration.Models
{
    /// <summary>
    /// 单个日志通道的落盘策略配置。
    /// 🔴 必须继承 ConfigBase（F6 硬结论）：否则递归判脏静默失效
    /// （ConfigBase.IsDirty 只递归 ConfigBase 与 IEnumerable&lt;ConfigBase&gt;，
    /// ConfigService.Save 只认 IsDirty）⇒ POCO 会导致「改了不保存且无任何提示」。
    /// </summary>
    public class LogCategoryPolicyConfig : ConfigBase, ILogCategoryPolicy
    {
        private LogCategory category;
        private LogSplitPeriod splitPeriod = LogSplitPeriod.Day;
        private int keepDays = 180;
        private string fileNamePrefix;

        /// <summary>
        /// 本策略对应的日志通道。
        /// </summary>
        [DisplayName("日志通道")]
        [Browsable(false)]   // 界面跳过：分组头已表达通道，行内下拉纯冗余；JSON 序列化不受影响（Browsable 只挡 ConfigFlatRow.Flatten 的 UI 扫描）
        [JsonConverter(typeof(StringEnumConverter))]
        public LogCategory Category
        {
            get => category;
            set => SetProperty(ref category, value);
        }

        /// <summary>
        /// 文件切分粒度（默认 Day；Hour 为临时排障模式，单目录 ≈ 24 × KeepDays 文件）。
        /// </summary>
        [DisplayName("切分粒度")]
        [JsonConverter(typeof(StringEnumConverter))]
        public LogSplitPeriod SplitPeriod
        {
            get => splitPeriod;
            set => SetProperty(ref splitPeriod, value);
        }

        /// <summary>
        /// 保留天数（默认 180；-1 = 不清理；≥ 1 时超期文件由 LogArchiveCleaner 按文件名里的日期清理）。
        /// </summary>
        [DisplayName("保留天数(-1=不清理)")]
        public int KeepDays
        {
            get => keepDays;
            set => SetProperty(ref keepDays, value);
        }

        /// <summary>
        /// 文件名前缀（仅允许字母/数字/下划线；为空时回退为通道名）。
        /// </summary>
        [DisplayName("文件名前缀")]
        public string FileNamePrefix
        {
            get => fileNamePrefix;
            set => SetProperty(ref fileNamePrefix, value);
        }
    }
}
