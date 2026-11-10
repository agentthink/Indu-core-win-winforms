using Newtonsoft.Json;
using System.Collections.Generic;

namespace Taoyl.Application.AuthService.Models
{
    /// <summary>
    /// 菜单配置DTO（解析Menu.json，避免AuthService层依赖UI层）
    /// </summary>
    public class MenuConfigDto
    {
        [JsonProperty("MenuName")]
        public string MenuName { get; set; }

        [JsonProperty("Version")]
        public string Version { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("MenuItems")]
        public List<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
    }

    /// <summary>
    /// 菜单项DTO（解析Menu.json）
    /// </summary>
    public class MenuItemDto
    {
        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("Description")]
        public string Description { get; set; }

        [JsonProperty("Level")]
        public int Level { get; set; }

        [JsonProperty("Class")]
        public string Class { get; set; }

        [JsonProperty("DllName")]
        public string DllName { get; set; }

        [JsonProperty("Icon")]
        public string Icon { get; set; }

        [JsonProperty("IsDefault")]
        public bool IsDefault { get; set; }

        [JsonProperty("SortOrder")]
        public int SortOrder { get; set; }

        /// <summary>
        /// 功能权限叶子节点（可选，默认继承父页面Level）
        /// </summary>
        [JsonProperty("Features")]
        public List<FeatureItemDto> Features { get; set; } = new List<FeatureItemDto>();

        [JsonProperty("Children")]
        public List<MenuItemDto> Children { get; set; } = new List<MenuItemDto>();
    }

    /// <summary>
    /// 功能权限项DTO（嵌套在MenuItemDto中）
    /// </summary>
    public class FeatureItemDto
    {
        [JsonProperty("Code")]
        public string Code { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>
        /// 可选：显式覆盖父页面的Level；null表示继承父页面
        /// </summary>
        [JsonProperty("Level")]
        public int? Level { get; set; }
    }

}
