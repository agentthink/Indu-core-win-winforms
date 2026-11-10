using System.ComponentModel;

namespace Taoyl.Application.Configuration.Models.Params
{
    /// <summary>
    /// 配方中的单个参数单元，包含标识、显示信息、范围和当前值。
    /// Key 为只读业务锚点；Value / Min / Max 使用 double 类型。
    /// 继承 ConfigBase 以支持 INotifyPropertyChanged 和脏标记，
    /// 便于 DataGridView 双向绑定。
    /// </summary>
    public class RecipeParameter : ConfigBase
    {
        private string displayName;
        private string unit;
        private string comment;
        private double min;
        private double max;
        private double paramValue;

        /// <summary>
        /// 参数唯一标识键，运行时业务锚点，不可在 UI 上修改。
        /// </summary>
        [Description("Key")]
        public string Key { get; set; }

        /// <summary>
        /// 参数的显示名称，用户可在 UI 上编辑。
        /// </summary>
        [Description("显示名称")]
        public string DisplayName
        {
            get => displayName;
            set => SetProperty(ref displayName, value);
        }

        /// <summary>
        /// 参数的单位（如 "°C"、"秒"、"MPa"），用户可编辑。
        /// </summary>
        [Description("单位")]
        public string Unit
        {
            get => unit;
            set => SetProperty(ref unit, value);
        }

        /// <summary>
        /// 参数的注释说明，用户可编辑。
        /// </summary>
        [Description("注释")]
        public string Comment
        {
            get => comment;
            set => SetProperty(ref comment, value);
        }

        /// <summary>
        /// 参数允许的最小值。
        /// </summary>
        [Description("最小值")]
        public double Min
        {
            get => min;
            set => SetProperty(ref min, value);
        }

        /// <summary>
        /// 参数允许的最大值。
        /// </summary>
        [Description("最大值")]
        public double Max
        {
            get => max;
            set => SetProperty(ref max, value);
        }

        /// <summary>
        /// 参数当前设定值。
        /// </summary>
        [Description("设定值")]
        public double Value
        {
            get => paramValue;
            set => SetProperty(ref paramValue, value);
        }
    }
}
