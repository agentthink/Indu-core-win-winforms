using System.Collections.Generic;

namespace Taoyl.Application.Configuration.Services
{
    /// <summary>
    /// 配方索引内部类型，用于序列化/反序列化 RecipeIndex.json。
    /// </summary>
    internal class RecipeIndex
    {
        /// <summary>
        /// 当前激活的配方名称。
        /// </summary>
        public string CurrentRecipeName { get; set; }

        /// <summary>
        /// 所有配方名称列表。
        /// </summary>
        public List<string> RecipeNames { get; set; } = new List<string>();
    }
}
