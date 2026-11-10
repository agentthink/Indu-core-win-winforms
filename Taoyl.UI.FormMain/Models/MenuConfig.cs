using System.Collections.Generic;
using Newtonsoft.Json;

namespace Taoyl.UI.FormMain.Models
{
    /// <summary>
    /// 菜单配置模型
    /// </summary>
    public class MenuConfig
    {
        /// <summary>
        /// 菜单名称
        /// </summary>
        [JsonProperty("MenuName")]
        public string MenuName { get; set; }

        /// <summary>
        /// 版本
        /// </summary>
        [JsonProperty("Version")]
        public string Version { get; set; }

        /// <summary>
        /// 描述
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; }

        /// <summary>
        /// 菜单项列表
        /// </summary>
        [JsonProperty("MenuItems")]
        public List<MenuItem> MenuItems { get; set; } = new List<MenuItem>();
    }
}