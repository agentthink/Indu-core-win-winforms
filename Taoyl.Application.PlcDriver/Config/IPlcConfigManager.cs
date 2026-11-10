using Taoyl.Application.PlcDriver.Logging;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Config;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Taoyl.Application.PlcDriver.Config
{
    /// <summary>
    /// PLC配置管理器接口
    /// 职责：抽象PLC主配置与标签配置的加载、验证、缓存与移除
    /// 地址表唯一持有（两层 CD：plcName → 组表）；PlcRuntime/IoExecutor 不缓存地址表引用，每次经 GetTagTable 现取（引用即见，无 Reload 机制）
    /// 注入使用：新代码构造注入本接口；ConfigManager 保留静态单例 Instance 过渡兼容旧调用
    /// </summary>
    public interface IPlcConfigManager
    {
        #region PLC主配置
        /// <summary>
        /// 从JSON字符串加载PLC设备配置
        /// </summary>
        PlcConfig LoadJsonConfigForPlc(string jsonString);

        /// <summary>
        /// 更新PLC设备配置（覆盖式写入，绕开 LoadJsonConfigForPlc 的同名缓存短路）
        /// 供运行时修改配置后重建实例使用：调试界面改配置 → UpdatePlcConfig 保存 → BuildDevice（CreateDevice 经 LoadJsonConfigForPlc 短路拿到新配置）
        /// 地址表不在此处理：地址表独立通道（ReloadTagConfig/LoadTagPollingConfigForPlc）
        /// </summary>
        /// <param name="plcConfig">PLC配置对象（外部已编辑好的完整配置）</param>
        /// <returns>更新后的PLC配置对象</returns>
        PlcConfig UpdatePlcConfig(PlcConfig plcConfig);

        /// <summary>
        /// 获取指定PLC的设备配置
        /// </summary>
        PlcConfig GetPlcConfig(string plcName);

        /// <summary>
        /// 移除指定PLC的设备配置
        /// </summary>
        bool RemovePlcConfig(string plcName);
        #endregion

        #region 标签配置
        /// <summary>
        /// 从Excel文件加载标签轮询配置（解析并整体替换指定PLC的地址表）
        /// 说明：地址表在配置加载阶段（LoadJsonConfigForPlc）顺带加载，本方法供手动重载/调试用
        /// </summary>
        bool LoadTagPollingConfigForPlc(string plcName, string excelPath, IPlcLogger externalLogger = null);

        /// <summary>
        /// 将指定PLC的当前内存地址表导出为Excel（15列模板，与 LoadTagPollingConfigForPlc 同构可回导）
        /// 导出规则：组外层按组序号（GroupID）升序（同序号组名 Ordinal 兜底），组内按组内序号（TagID）升序；
        /// Value 导当前容器值（数组以逗号分隔展开，未刷新为空串）；LastOperationTime=MinValue（未操作过）导空串；
        /// 空表（有地址表但零标签行）同样导出——生成仅含表头的空白模板
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="excelPath">导出文件路径（.xlsx，已存在则覆盖）</param>
        /// <param name="externalLogger">外部日志记录器（可选）</param>
        /// <returns>是否导出成功（false=无地址表/IO异常）</returns>
        bool SaveTagPollingConfigToExcel(string plcName, string excelPath, IPlcLogger externalLogger = null);

        /// <summary>
        /// 将指定PLC的当前内存地址表保存到其配置的标签文件（PlcTagsPath，供调试界面"保存"落盘）
        /// 保存规则：解析 PlcTagsPath（绝对路径优先；相对路径拼程序基目录，与配置验证同规则）→
        /// 目标文件已存在则先复制 .bak 备份 → 复用 SaveTagPollingConfigToExcel 整表覆盖写入
        /// 保存后重启生效（本方法不重载内存，内存地址表始终为当前编辑态）
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="externalLogger">外部日志记录器（可选）</param>
        /// <returns>是否保存成功（false=无该PLC配置/未配置标签文件/IO异常）</returns>
        bool SaveTagPollingConfigToConfiguredFile(string plcName, IPlcLogger externalLogger = null);

        /// <summary>
        /// 获取指定PLC的地址表（组名 -> 标签组），不存在则返回 null
        /// 注意：返回的是实时引用，每次现取（无 Reload 机制，改即生效）
        /// </summary>
        ConcurrentDictionary<string, TagGroup> GetTagTable(string plcName);

        /// <summary>
        /// 移除指定PLC的标签组
        /// </summary>
        bool RemovePlcTagGroups(string plcName);

        #region 调试接口（CD 原子操作，运行时动态增删改）
        /// <summary>
        /// 动态添加标签组（原子；新增组专用——组身份不变量：tagGroup.GroupName 与 groupName 参数强制一致，组内 TagItem.GroupName 同步）。
        /// 同名已存在 = 覆盖（upsert 语义）；改名/整组属性更新请走 UpdateTagGroup。
        /// </summary>
        void AddTagGroup(string plcName, string groupName, TagGroup tagGroup);

        /// <summary>
        /// 动态更新标签组（原子：整组替换，含改名先挂后摘 + 组属性/组内 TagItem.GroupName 同步）。
        /// 语义（2026-09-08 定稿）：UpdateTagGroup=整组 upsert（同名=覆盖替换；异名=改名迁移），与 AddTagGroup 区分。
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="oldGroupName">原组名（CD Key；null/空=无原组，等价 AddTagGroup）</param>
        /// <param name="tagGroup">整组新对象（GroupName=新组名；改名时与 oldGroupName 不同）</param>
        /// <returns>是否成功（false=参数无效/目标新组名与其它组冲突）</returns>
        bool UpdateTagGroup(string plcName, string oldGroupName, TagGroup tagGroup);

        /// <summary>
        /// 动态移除标签组（原子）
        /// </summary>
        bool RemoveTagGroup(string plcName, string groupName);

        /// <summary>
        /// 动态添加标签到指定组（原子；组身份不变量：tag.GroupName 强制同步为 groupName）
        /// </summary>
        void AddTag(string plcName, string groupName, TagItem tag);

        /// <summary>
        /// 动态更新标签（原子：整体替换，含改名先挂后摘 + 组身份同步）。
        /// 语义（2026-09-08 定稿）：UpdateTag=整标签 upsert（同名=覆盖属性；异名=改名迁移），AddTag=仅添加新标签。
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="groupName">组名</param>
        /// <param name="oldTagName">原标签名（CD Key；null/空=无原标签，等价 AddTag）</param>
        /// <param name="tag">整标签新对象（TagName=新标签名；改名时与 oldTagName 不同）</param>
        /// <returns>是否成功（false=参数无效/组不存在/新标签名与组内其它标签冲突）</returns>
        bool UpdateTag(string plcName, string groupName, string oldTagName, TagItem tag);

        /// <summary>
        /// 动态移除标签（原子）
        /// </summary>
        bool RemoveTag(string plcName, string groupName, string tagName);
        #endregion
        #endregion

        #region 工具方法
        /// <summary>
        /// 清空所有配置缓存
        /// </summary>
        void Clear();

        /// <summary>
        /// 清空指定PLC的所有配置缓存
        /// </summary>
        void ClearPlcConfig(string plcName);

        /// <summary>
        /// 重新加载指定PLC的标签配置
        /// </summary>
        bool ReloadTagConfig(string plcName, string excelPath = null);
        #endregion
    }
}
