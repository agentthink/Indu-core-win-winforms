using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Taoyl.Application.Configuration.Models.Params
{
    /// <summary>
    /// 配方基类（抽象），定义配方元数据和参数列表骨架。
    /// </summary>
    public abstract class RecipeBase : ConfigBase
    {
        private List<RecipeParameter> parameters = new List<RecipeParameter>();
        private string recipeName;
        private string modifiedBy;
        private string version;
        private DateTime modifiedTime;
        private string comment;

        /// <summary>配方名称。</summary>
        [Description("配方名称")]
        public string RecipeName
        {
            get => recipeName;
            set => SetProperty(ref recipeName, value);
        }

        /// <summary>最后变更人员。</summary>
        [Description("变更人员")]
        public string ModifiedBy
        {
            get => modifiedBy;
            set => SetProperty(ref modifiedBy, value);
        }

        /// <summary>配方版本号。</summary>
        [Description("版本")]
        public string Version
        {
            get => version;
            set => SetProperty(ref version, value);
        }

        /// <summary>最后变更时间。</summary>
        [Description("变更时间")]
        public DateTime ModifiedTime
        {
            get => modifiedTime;
            set => SetProperty(ref modifiedTime, value);
        }

        /// <summary>配方注释说明。</summary>
        [Description("注释")]
        public string Comment
        {
            get => comment;
            set => SetProperty(ref comment, value);
        }

        /// <summary>配方参数列表。容器引用不调用 SetProperty——个体参数自己跟踪脏标记。</summary>
        [Description("参数")]
        public List<RecipeParameter> Parameters
        {
            get => parameters;
            set => parameters = value;
        }

        /// <summary>根据 Key 获取配方参数。</summary>
        [JsonIgnore]
        public RecipeParameter this[string key]
        {
            get { return Parameters?.FirstOrDefault(p => p.Key == key); }
        }
    }
}
