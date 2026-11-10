using System.Collections.Concurrent;
using System.ComponentModel;

namespace Taoyl.Application.PlcDriver.Models
{
    /// <summary>
    /// PLC标签组（地址表的值容器，2026-09-08 实体化：加 GroupName/GroupDesc/GroupID）
    /// 组身份三重一致不变量（CD Key = GroupName = 组内每个 TagItem.GroupName）：
    /// CD Key（ConfigManager 字典键）= 运行时权威；GroupName 为组实体业务名（UI 编辑缓冲/展示）；
    /// 写入点收敛保证：加载建组 / UpdateTagGroup 改名 / AddTag 三处同步，UI 只碰工作行缓冲不直写本对象。
    /// 组内可混数据类型
    /// </summary>
    public class TagGroup
    {
        /// <summary>
        /// 组名称（业务字段，与 CD Key 一致；UI 改名经 UpdateTagGroup 先挂后摘同步）
        /// </summary>
        [Description("组名")]
        public string GroupName { get; set; }

        /// <summary>
        /// 组描述（2026-09-08 新增：组语义备注，随地址表 Excel 组内行冗余持久化）
        /// </summary>
        [Description("组描述")]
        public string GroupDesc { get; set; }

        /// <summary>
        /// 组序号（组间展示/落盘顺序；旧 Excel 缺列加载时按组首现序自增兜底）
        /// </summary>
        [Description("组序号")]
        public int GroupID { get; set; }

        /// <summary>
        /// 标签字典（标签名 -> 标签项，线程安全）
        /// </summary>
        public ConcurrentDictionary<string, TagItem> Tags { get; set; } = new ConcurrentDictionary<string, TagItem>();
    }
}
