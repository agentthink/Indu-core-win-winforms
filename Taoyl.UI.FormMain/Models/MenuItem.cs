using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace Taoyl.UI.FormMain.Models
{
    /// <summary>
    /// UI菜单项模型（仅页面层级）。
    /// 功能权限(Features)由PermissionSyncService通过MenuItemDto解析入库，不在UI菜单树中渲染。
    /// </summary>
    public class MenuItem
    {
        /// <summary>
        /// 菜单项唯一标识
        /// </summary>
        [JsonProperty("Name")]
        public string Name { get; set; }

        /// <summary>
        /// 显示名称
        /// </summary>
        [JsonProperty("DisplayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// 描述信息
        /// </summary>
        [JsonProperty("Description")]
        public string Description { get; set; }

        /// <summary>
        /// 权限等级（单整数，百进制，替代原Levels数组）
        /// </summary>
        [JsonProperty("Level")]
        public int Level { get; set; }

        /// <summary>
        /// 对应的窗体类名
        /// </summary>
        [JsonProperty("Class")]
        public string Class { get; set; }

        /// <summary>
        /// 所在的DLL名称
        /// </summary>
        [JsonProperty("DllName")]
        public string DllName { get; set; }

        /// <summary>
        /// 图标名称
        /// </summary>
        [JsonProperty("Icon")]
        public string Icon { get; set; }

        /// <summary>
        /// 是否为默认显示窗体
        /// </summary>
        [DefaultValue(false)]
        [JsonProperty("IsDefault")]
        public bool IsDefault { get; set; }

        /// <summary>
        /// 排序顺序
        /// </summary>
        [DefaultValue(0)]
        [JsonProperty("SortOrder")]
        public int SortOrder { get; set; }

        /// <summary>
        /// 子菜单项
        /// </summary>
        [JsonProperty("Children")]
        public List<MenuItem> Children { get; set; } = new List<MenuItem>();

        /// <summary>
        /// 是否可见（运行时计算）
        /// </summary>
        [JsonIgnore]
        public bool IsVisible { get; set; }

        /// <summary>
        /// 是否是窗体节点（有对应的窗体类）
        /// </summary>
        public bool IsFormNode()
        {
            return !string.IsNullOrEmpty(Class) && !string.IsNullOrEmpty(DllName);
        }

        /// <summary>
        /// 是否是父节点（有子菜单）
        /// </summary>
        public bool IsParentNode()
        {
            return Children != null && Children.Count > 0;
        }

        /// <summary>
        /// 深拷贝（权限过滤时需要保持源数据不变）
        /// </summary>
        public MenuItem Clone()
        {
            return new MenuItem
            {
                Name = this.Name,
                DisplayName = this.DisplayName,
                Description = this.Description,
                Class = this.Class,
                DllName = this.DllName,
                Icon = this.Icon,
                Level = this.Level,
                IsDefault = this.IsDefault,
                SortOrder = this.SortOrder,
                Children = this.Children?.Select(c => c.Clone()).ToList() ?? new List<MenuItem>()
            };
        }
    }
}