using System.Collections.Generic;
using Newtonsoft.Json;

namespace Taoyl.UI.Pages.Models
{
    /// <summary>
    /// 专供多语言配置窗体（FrmResourceConfig）使用的 Menu.json 轻量 DTO。
    /// 只取树构建所需字段（DllName/Class/DisplayName），不依赖 FormMain 的导航模型，
    /// 避免多语言工具与主窗体导航模型耦合（导航模型将来抽工具库时不受影响）。
    /// </summary>
    public class MenuConfigDto
    {
        [JsonProperty("MenuItems")]
        public List<MenuItemDto> MenuItems { get; set; } = new List<MenuItemDto>();
    }

    /// <summary>
    /// 菜单项 DTO（支持嵌套 Children，用于展平提取所有窗体类）
    /// </summary>
    public class MenuItemDto
    {
        [JsonProperty("DllName")]
        public string DllName { get; set; }

        [JsonProperty("Class")]
        public string Class { get; set; }

        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        [JsonProperty("Children")]
        public List<MenuItemDto> Children { get; set; } = new List<MenuItemDto>();

        /// <summary>是否有子菜单</summary>
        public bool HasChildren => Children != null && Children.Count > 0;
    }
}
