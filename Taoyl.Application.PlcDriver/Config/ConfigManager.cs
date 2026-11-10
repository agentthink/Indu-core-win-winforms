
using MiniExcelLibs;
using Newtonsoft.Json;
using Taoyl.Application.PlcDriver.Logging;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Cache;
using Taoyl.Application.PlcDriver.Models.Config;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taoyl.Application.Share.Paths;

namespace Taoyl.Application.PlcDriver.Config
{
    /// <summary>
    /// PLC配置管理器（单例）
    /// 负责加载和验证PLC配置、标签配置
    /// 支持多PLC实例配置隔离
    /// 地址表唯一持有（两层 CD：plcName → 组表 → 标签组）；PlcRuntime/IoExecutor 不缓存引用，每次 GetTagTable 现取
    /// 保留静态单例 Instance 过渡兼容旧调用，新代码走 IPlcConfigManager 注入
    /// </summary>
    public class ConfigManager : IPlcConfigManager
    {
        private static readonly Lazy<ConfigManager> _instance = new Lazy<ConfigManager>(() => new ConfigManager());
        public static ConfigManager Instance => _instance.Value;

        // 按PLC名称存储配置，支持多实例隔离
        private readonly ConcurrentDictionary<string, PlcConfig> _plcConfigs = new ConcurrentDictionary<string, PlcConfig>();

        // 地址表（两层 CD）：PLC名称 -> 组表（组名 -> 标签组）
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, TagGroup>> _plcTagGroups = new ConcurrentDictionary<string, ConcurrentDictionary<string, TagGroup>>();
        private readonly IPlcLogger _logger;

        private ConfigManager()
        {
            // 创建配置管理器专用日志（独立实例，非全局单例）
            // 目录解析与 PlcRuntime 同规则：相对默认挂程序所在盘根 → {盘根}/Logs/PLC/ConfigManager（HSL 在目录内生成 Logs_*.txt）
            var logDirectory = PlcLogger.ResolveDirectory(null, Path.Combine(ConfigPathName.PLCLogFile, nameof(ConfigManager)));
            Directory.CreateDirectory(logDirectory);

            _logger = new PlcLogger(logDirectory, true);

            _logger.WriteInfo("═══════════════════════════════════════");
            _logger.WriteInfo("PLC配置管理器初始化完成");
            _logger.WriteInfo($"日志目录: {logDirectory}");
            _logger.WriteInfo("═══════════════════════════════════════");
        }

        #region PLC主配置加载 - 多实例版本

        /// <summary>
        /// 从JSON字符串加载PLC设备配置，如果缓存中已存在同名PLC配置，则返回缓存中的配置，否则加载新配置并存储到缓存中
        /// </summary>
        /// <param name="jsonString">PLC配置json字符串</param>
        /// <returns>PLC配置对象</returns>
        /// <exception cref="Exception">解析异常</exception>
        public PlcConfig LoadJsonConfigForPlc(string jsonString)
        {
            try
            {
                _logger.WriteInfo($"加载配置，从json字符串:{jsonString}");

                var plcConfig = JsonConvert.DeserializeObject<PlcConfig>(jsonString);
                if (plcConfig == null)
                {
                    string error = $"PLC配置Json字符串为空或格式错误,反序列化异常: {jsonString}";
                    _logger.WriteError(error);
                    throw new Exception(error);
                }

                if (_plcConfigs.ContainsKey(plcConfig.Name))
                {
                    return _plcConfigs[plcConfig.Name];
                }

                ValidateDevConfig(plcConfig);
                _plcConfigs[plcConfig.Name] = plcConfig;

                // 顺带加载地址表（有 PlcTagsPath 才加载；失败仅告警，不阻断主配置）
                TryLoadTagTable(plcConfig);
                _logger.WriteDebug($"加载PLC配置: {plcConfig.Name}, 端点={plcConfig.EndpointText}");

                _logger.WriteInfo($"PLC配置加载成功: {jsonString}");
                return _plcConfigs[plcConfig.Name];
            }
            catch (Exception ex)
            {
                string errorMsg = $"加载PLC配置失败: {jsonString}";
                _logger.WriteException(errorMsg, ex);
                throw new Exception(errorMsg, ex);
            }
        }

        /// <summary>
        /// 更新PLC设备配置（覆盖式写入，不走 LoadJsonConfigForPlc 的同名缓存短路）
        /// 供运行时修改配置后重建实例使用：调试界面改配置 → UpdatePlcConfig 保存 → BuildDevice（CreateDevice 经 LoadJsonConfigForPlc 短路拿到新配置）
        /// 地址表不在此处理（独立通道 ReloadTagConfig）；TryLoadTagTable 顺带：PlcTagsPath 变更时地址表随主配置一起换
        /// </summary>
        /// <param name="plcConfig">PLC配置对象（外部已编辑好的完整配置）</param>
        /// <returns>更新后的PLC配置对象</returns>
        /// <exception cref="Exception">配置无效</exception>
        public PlcConfig UpdatePlcConfig(PlcConfig plcConfig)
        {
            try
            {
                if (plcConfig == null)
                {
                    string error = "PLC配置对象为空";
                    _logger.WriteError(error);
                    throw new Exception(error);
                }

                _logger.WriteInfo($"更新配置: {plcConfig.Name}");

                ValidateDevConfig(plcConfig);
                _plcConfigs[plcConfig.Name] = plcConfig;

                // 顺带加载地址表（有 PlcTagsPath 才加载；失败仅告警，不阻断主配置）
                TryLoadTagTable(plcConfig);
                _logger.WriteDebug($"更新PLC配置: {plcConfig.Name}, 端点={plcConfig.EndpointText}");

                _logger.WriteInfo($"PLC配置更新成功: {plcConfig.Name}");
                return plcConfig;
            }
            catch (Exception ex)
            {
                string errorMsg = $"更新PLC配置失败: {plcConfig?.Name ?? "<null>"}";
                _logger.WriteException(errorMsg, ex);
                throw new Exception(errorMsg, ex);
            }
        }

        /// <summary>
        /// 获取指定PLC的设备配置
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <returns>PLC配置对象，不存在则返回null</returns>
        public PlcConfig GetPlcConfig(string plcName)
        {
            return _plcConfigs.TryGetValue(plcName, out var config) ? config : null;
        }

        /// <summary>
        /// 移除指定PLC的设备配置
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemovePlcConfig(string plcName)
        {
            bool removed = _plcConfigs.TryRemove(plcName, out _);
            if (removed)
            {
                _logger.WriteInfo($"已移除PLC[{plcName}]的设备配置");
            }
            return removed;
        }
        #endregion

        #region 标签配置加载（Excel格式）- 多实例版本
        /// <summary>
        /// 从Excel文件加载标签轮询配置，并关联到指定PLC名称
        /// 加载流程：按 DataType 逐条建 TagItem（模板15列，含 TagDesc；组属性取组首行，GroupID 缺省首现自增）→ 全建完再按组名分组 → 装表
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="excelPath">标签配置文件路径</param>
        /// <param name="externalLogger">外部日志记录器（可选）</param>
        /// <returns>加载是否成功</returns>
        public bool LoadTagPollingConfigForPlc(string plcName, string excelPath, IPlcLogger externalLogger = null)
        {
            var log = externalLogger ?? _logger;

            try
            {
                log.WriteInfo($"为PLC[{plcName}]加载标签配置: {excelPath}");

                if (!File.Exists(excelPath))
                {
                    log.WriteWarn($"标签配置文件不存在: {excelPath}");
                    return false;
                }

                // 使用MiniExcel加载标签配置（模板15列；旧 12 列文件缺新列加载=默认空，向后兼容）
                var rows = MiniExcel.Query<ExcelTagConfigRow>(
                    excelPath,
                    sheetName: "Sheet1",
                    startCell: "A1"
                ).ToList();

                if (rows == null || rows.Count == 0)
                {
                    log.WriteWarn($"标签配置文件为空: {excelPath}");
                    return false;
                }

                log.WriteInfo($"从Excel读取到 {rows.Count} 行标签数据");

                // 记录旧表标签引用（重载/替换时迁移运行时值到新表同名标签，避免全部清空需重新轮询；组名+标签名组合键）
                // 存 TagItem 引用而非 object 值：值在强类型容器 TypedValueHolder 内，需经旧容器读取（含 HasValue 状态）
                var oldTags = new Dictionary<string, TagItem>();
                var oldTable = GetTagTable(plcName);
                if (oldTable != null)
                {
                    foreach (var g in oldTable)
                    {
                        foreach (var t in g.Value.Tags)
                        {
                            oldTags[GetTagKey(g.Key, t.Key)] = t.Value;
                        }
                    }
                }

                // 按 DataType 逐条建 TagItem（15列全量填充）；同组组属性（组序号/组描述）取该组首行（行式冗余一致，H3 约定）
                var tagItems = new List<TagItem>();
                var groupFirstRows = new Dictionary<string, ExcelTagConfigRow>(StringComparer.Ordinal);
                foreach (var row in rows)
                {
                    // 跳过无效行
                    if (string.IsNullOrWhiteSpace(row.GroupName) ||
                        string.IsNullOrWhiteSpace(row.TagName) ||
                        string.IsNullOrWhiteSpace(row.Address))
                    {
                        log.WriteWarn($"跳过无效标签行: 组名/标签名/地址为空");
                        continue;
                    }

                    // 解析数据类型
                    if (!Enum.TryParse<PlcDataType>(row.DataType?.Trim(), true, out var dataType))
                    {
                        log.WriteWarn($"行[{row.TagName}]的数据类型[{row.DataType}]无效，跳过");
                        continue;
                    }

                    string groupName = row.GroupName.Trim();
                    if (!groupFirstRows.ContainsKey(groupName))
                        groupFirstRows[groupName] = row; // 组首行（组属性权威；Excel 冗余填写不一致以首行为准）

                    // 刷新频率解析：-1 = 不刷新（替代旧 Autoread 标志）
                    int refreshFreq = int.TryParse(row.RefreshFreq, out var freq) ? freq : -1;

                    // 长度解析（下界兜底：多数类型 Length<=0 无意义取 1；
                    // String 例外，放开到 0 —— 0 在部分协议上有明确定义，故是允许值而非"无意义值"。
                    // 配置层刻意不做协议感知校验：0 的合法性与含义由协议层判定与报错，见各协议访问类注释）
                    int tagLength = int.TryParse(row.Length, out var length)
                        ? (dataType == PlcDataType.String ? Math.Max(0, length) : Math.Max(1, length))
                        : 1;

                    // 一致性校验（只告警不阻断，存量配置照常可用）：
                    // 标量类型下 Length 会被读策略静默忽略——策略对标量走单值原语 ReadXxx(address)，根本不消费 len，
                    // 于是"Int + 长度5"既不报错也读不成数组，配置与行为不一致。想要数组请改用对应的 XxxArray 类型。
                    // String 例外：其 Length 是读取跨度（ReadString(address, length)），有实际用途——
                    // 具体取值含义由协议解释，见 IsScalarType 与各协议访问类注释。
                    if (tagLength > 1 && IsScalarType(dataType))
                    {
                        log.WriteWarn($"标签[{groupName}.{row.TagName}]数据类型[{dataType}]为标量，长度[{tagLength}]将被忽略；如需数组请改用 {dataType}Array");
                    }

                    var tagItem = new TagItem
                    {
                        GroupName = groupName,
                        TagID = int.TryParse(row.TagID, out var sortId) ? sortId : 0,
                        RefreshFreq = refreshFreq,
                        TagName = row.TagName.Trim(),
                        Address = row.Address.Trim(),
                        DataType = dataType,
                        RWTimes = 0,
                        Length = tagLength,
                        TagDesc = row.TagDesc?.Trim(),
                        LastOperationTime = DateTime.MinValue,
                        LastOperationResult = false,
                        LastOperationMsg = string.Empty
                    };
                    tagItem.EnsureTypedValue();   // 定型：按 DataType 建值容器（初始未刷新，_hasValue=false）
                    tagItems.Add(tagItem);
                }

                // 全建完再分组（组内混类型）；装表前同名标签迁移旧值（旧容器已赋值 且 DataType 未变才迁；
                // object 低频写经 tagItem.TypedValue.SetObject（重载低频装箱可接受）；类型变了=未刷新等首轮轮询）
                foreach (var tagItem in tagItems)
                {
                    if (oldTags.TryGetValue(GetTagKey(tagItem.GroupName, tagItem.TagName), out var oldTag)
                        && oldTag.DataType == tagItem.DataType
                        && oldTag.TypedValue != null && oldTag.TypedValue.HasValue)
                    {
                        tagItem.TypedValue.SetObject(oldTag.TypedValue.GetObject());
                    }
                }

                // 全建完再分组（组内可混类型）；组属性：组序号缺列/非法 → 按组首现序自增兜底（H2），组描述取组首行
                var tagGroups = new Dictionary<string, TagGroup>();
                int groupSeq = 0;
                foreach (var group in tagItems.GroupBy(t => t.GroupName))
                {
                    var firstRow = groupFirstRows.TryGetValue(group.Key, out var fr) ? fr : null;
                    int groupId = 0;
                    if (firstRow != null && int.TryParse(firstRow.GroupID, out var gid) && gid > 0)
                        groupId = gid;
                    if (groupId <= 0)
                        groupId = ++groupSeq; // 缺列/非法 = 组首现序自增（稳定不跳号，保存后写回文件固化）

                    var tagGroup = new TagGroup
                    {
                        GroupName = group.Key,
                        GroupDesc = firstRow?.GroupDesc?.Trim() ?? string.Empty,
                        GroupID = groupId,
                    };
                    foreach (var tagItem in group)
                    {
                        // 检查标签名重复
                        if (!tagGroup.Tags.TryAdd(tagItem.TagName, tagItem))
                        {
                            log.WriteError($"组[{group.Key}]标签名重复: {tagItem.TagName}，跳过");
                        }
                    }
                    tagGroups[group.Key] = tagGroup;
                    log.WriteInfo($"加载标签组[{group.Key}]: 标签数={tagGroup.Tags.Count}, 组序号={groupId}");
                }

                // 存储到地址表（两层CD：plcName -> 组表）
                _plcTagGroups[plcName] = new ConcurrentDictionary<string, TagGroup>(tagGroups);

                log.WriteInfo($"标签配置加载完成: 共{tagGroups.Count}个标签组");
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"加载标签配置失败: {excelPath}";
                log.WriteException(errorMsg, ex);
                return false;
            }
        }

        /// <summary>
        /// 将指定PLC的当前内存地址表导出为Excel（15列模板，与加载模板同构可回导）
        /// 导出规则：组外层按组序号（GroupID）升序（同序号组名 Ordinal 兜底），组内按组内序号（TagID）升序；
        /// Value 导当前容器值（数组以逗号分隔展开；未刷新/无值为空串）；LastOperationTime=MinValue（未操作过）导空串
        /// 空表（有地址表但零标签行）同样导出——生成仅含表头行的空白模板，供手工填行后回导
        /// 数据一致性：只读快照（容器 GetObject 低频弱一致可接受），不打断 IoWorker 轮询
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="excelPath">导出文件路径（.xlsx，已存在则覆盖）</param>
        /// <param name="externalLogger">外部日志记录器（可选）</param>
        /// <returns>是否导出成功（false=无地址表/IO异常）</returns>
        public bool SaveTagPollingConfigToExcel(string plcName, string excelPath, IPlcLogger externalLogger = null)
        {
            var log = externalLogger ?? _logger;

            try
            {
                log.WriteInfo($"为PLC[{plcName}]导出标签配置: {excelPath}");

                var table = GetTagTable(plcName);
                if (table == null)
                {
                    log.WriteWarn($"PLC[{plcName}]无地址表，导出取消");
                    return false;
                }

                // 组外层按组序号（GroupID）升序（同序号组名 Ordinal 兜底）；组内按组内序号（TagID）升序（仅影响导出可读性/再加载顺序）
                var rows = new List<ExcelTagConfigRow>();
                foreach (var groupPair in table
                    .OrderBy(g => g.Value.GroupID)
                    .ThenBy(g => g.Key, StringComparer.Ordinal))
                {
                    var group = groupPair.Value;
                    foreach (var tag in group.Tags.Values.OrderBy(t => t.TagID))
                    {
                        rows.Add(new ExcelTagConfigRow
                        {
                            GroupName = groupPair.Key,
                            TagID = tag.TagID.ToString(),
                            RefreshFreq = tag.RefreshFreq.ToString(),
                            TagName = tag.TagName ?? string.Empty,
                            Address = tag.Address ?? string.Empty,
                            DataType = tag.DataType.ToString(),
                            RWTimes = tag.RWTimes.ToString(),
                            Length = tag.Length.ToString(),
                            Value = FormatValueForExport(tag.Value),
                            LastOperationTime = tag.LastOperationTime == DateTime.MinValue
                                ? string.Empty
                                : tag.LastOperationTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            LastOperationResult = tag.LastOperationResult.ToString(),
                            LastOperationMsg = tag.LastOperationMsg ?? string.Empty,
                            GroupID = group.GroupID > 0 ? group.GroupID.ToString() : string.Empty,
                            GroupDesc = group.GroupDesc ?? string.Empty,
                            TagDesc = tag.TagDesc ?? string.Empty,
                        });
                    }
                }

                if (rows.Count == 0)
                {
                    log.WriteInfo($"PLC[{plcName}]地址表为空，导出仅含表头的空白模板");
                }

                // 目录不存在则创建（保存对话框通常给已存在目录，兜底防 IO 异常）
                var directory = Path.GetDirectoryName(excelPath);
                if (!string.IsNullOrEmpty(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                MiniExcel.SaveAs(excelPath, rows, overwriteFile: true);
                log.WriteInfo($"PLC[{plcName}]标签配置导出成功: {excelPath}, 共{rows.Count}行");
                return true;
            }
            catch (Exception ex)
            {
                string errorMsg = $"导出PLC[{plcName}]标签配置失败: {excelPath}";
                log.WriteException(errorMsg, ex);
                return false;
            }
        }

        /// <summary>
        /// 将指定PLC的当前内存地址表保存到其配置的标签文件（PlcTagsPath，供调试界面"保存"落盘）
        /// 保存规则：解析 PlcTagsPath（绝对优先；相对拼程序基目录，与 ValidateDevConfig 同规则）→
        /// 目标文件已存在则先复制 .bak（保留最近一版）→ 复用 SaveTagPollingConfigToExcel 整表覆盖写入
        /// 保存后重启生效（内存地址表不随本方法变动，始终为当前编辑态；落盘仅供下次启动加载）
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="externalLogger">外部日志记录器（可选）</param>
        /// <returns>是否保存成功（false=无该PLC配置/未配置标签文件/IO异常）</returns>
        public bool SaveTagPollingConfigToConfiguredFile(string plcName, IPlcLogger externalLogger = null)
        {
            var log = externalLogger ?? _logger;

            try
            {
                var plc = GetPlcConfig(plcName);
                if (plc == null)
                {
                    log.WriteWarn($"PLC[{plcName}]配置不存在，保存取消");
                    return false;
                }

                if (string.IsNullOrWhiteSpace(plc.PlcTagsPath))
                {
                    log.WriteWarn($"PLC[{plcName}]未配置标签文件路径(PlcTagsPath)，保存取消");
                    return false;
                }

                string fullPath = Path.IsPathRooted(plc.PlcTagsPath)
                    ? plc.PlcTagsPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, plc.PlcTagsPath);

                // 旧文件先 .bak 备份（覆盖式保留最近一版，供回滚）
                if (File.Exists(fullPath))
                {
                    string backupPath = fullPath + ".bak";
                    File.Copy(fullPath, backupPath, overwrite: true);
                    log.WriteInfo($"PLC[{plcName}]标签文件已备份: {backupPath}");
                }

                return SaveTagPollingConfigToExcel(plcName, fullPath, externalLogger);
            }
            catch (Exception ex)
            {
                string errorMsg = $"保存PLC[{plcName}]标签配置到配置文件失败: {ex.Message}";
                log.WriteException(errorMsg, ex);
                return false;
            }
        }

        /// <summary>
        /// 导出值格式化：null 导空串；数组以逗号分隔展开（如 BoolArray → "True,False"），与读写测试数组逗号解析对称；
        /// 其余走 ToString
        /// </summary>
        private static string FormatValueForExport(object value)
        {
            if (value == null)
            {
                return string.Empty;
            }

            if (value is Array array)
            {
                var parts = new string[array.Length];
                for (int i = 0; i < array.Length; i++)
                {
                    parts[i] = array.GetValue(i)?.ToString() ?? string.Empty;
                }
                return string.Join(",", parts);
            }

            return value.ToString();
        }

        /// <summary>
        /// 顺带加载指定PLC的地址表（配置加载阶段调用）
        /// 规则：有 PlcTagsPath 才加载；失败仅告警，不阻断主配置加载；幂等（LoadTagPollingConfigForPlc 覆盖式加载）
        /// </summary>
        /// <param name="plc">PLC配置（须含 Name/PlcTagsPath）</param>
        private void TryLoadTagTable(PlcConfig plc)
        {
            if (plc == null || string.IsNullOrWhiteSpace(plc.PlcTagsPath))
                return;

            if (!LoadTagPollingConfigForPlc(plc.Name, plc.PlcTagsPath, _logger))
            {
                _logger.WriteWarn($"PLC[{plc.Name}]地址表加载失败或为空: {plc.PlcTagsPath}（不影响实例创建，运行时读写将失败）");
            }
        }

        /// <summary>
        /// 获取指定PLC的地址表（组名 -> 标签组），不存在则返回 null
        /// 返回实时引用（无 Reload 机制，改即生效），每次现取不缓存
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        public ConcurrentDictionary<string, TagGroup> GetTagTable(string plcName)
        {
            return _plcTagGroups.TryGetValue(plcName, out var groups) ? groups : null;
        }

        /// <summary>
        /// 移除指定PLC的标签组
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <returns>是否成功移除</returns>
        public bool RemovePlcTagGroups(string plcName)
        {
            bool removed = _plcTagGroups.TryRemove(plcName, out _);
            if (removed)
            {
                _logger.WriteInfo($"已移除PLC[{plcName}]的标签组");
            }
            return removed;
        }
        #endregion

        #region 调试接口（CD 原子操作，运行时动态增删改：Add/Update/Remove 组与标签）
        /// <summary>
        /// 动态添加标签组（原子；组身份不变量：tagGroup.GroupName 与 groupName 参数强制一致，组内 TagItem.GroupName 同步）
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="groupName">组名（CD Key 权威；tagGroup.GroupName 被强制同步）</param>
        /// <param name="tagGroup">标签组</param>
        public void AddTagGroup(string plcName, string groupName, TagGroup tagGroup)
        {
            if (string.IsNullOrEmpty(plcName) || string.IsNullOrEmpty(groupName) || tagGroup == null)
                return;

            // 组身份不变量（写入点收敛）：CD Key = GroupName = 组内每个 TagItem.GroupName
            string key = groupName.Trim();
            tagGroup.GroupName = key;
            foreach (var tag in tagGroup.Tags.Values)
            {
                tag.GroupName = key;
                tag.EnsureTypedValue(); // 防御：进地址表的 TagItem 必须已定型
            }

            var table = _plcTagGroups.GetOrAdd(plcName, _ => new ConcurrentDictionary<string, TagGroup>());
            table[key] = tagGroup;
            _logger.WriteInfo($"调试接口: 添加标签组[{key}]到PLC[{plcName}]");
        }

        /// <summary>
        /// 动态更新标签组（原子：整组替换，含改名先挂后摘 + 组属性/组内 TagItem.GroupName 同步）。
        /// 语义（2026-09-08 定稿）：UpdateTagGroup=整组 upsert（同名=覆盖替换；异名=改名迁移），与 AddTagGroup（仅按参数组名添加）
        /// 区分——Add 用于新建组（不含改名语义），Update 用于编辑保存（可能带新组名）。
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="oldGroupName">原组名（CD Key；null/空=无原组，等价 AddTagGroup）</param>
        /// <param name="tagGroup">整组新对象（GroupName=新组名；改名时与 oldGroupName 不同）</param>
        /// <returns>是否成功（false=参数无效/目标新组名与其它组冲突）</returns>
        public bool UpdateTagGroup(string plcName, string oldGroupName, TagGroup tagGroup)
        {
            if (string.IsNullOrEmpty(plcName) || tagGroup == null)
                return false;

            string newName = (tagGroup.GroupName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(newName))
                return false;

            var table = _plcTagGroups.GetOrAdd(plcName, _ => new ConcurrentDictionary<string, TagGroup>());
            string oldKey = string.IsNullOrWhiteSpace(oldGroupName) ? null : oldGroupName.Trim();

            // 同名覆盖：无 Key 迁移（组身份一致即可）
            if (string.Equals(oldKey, newName, StringComparison.Ordinal))
            {
                tagGroup.GroupName = newName;
                foreach (var tag in tagGroup.Tags.Values)
                {
                    tag.GroupName = newName;
                    tag.EnsureTypedValue();
                }
                table[newName] = tagGroup;
                _logger.WriteInfo($"调试接口: 更新标签组[{newName}]到PLC[{plcName}]");
                return true;
            }

            // 改名：新名占用检查（被其它组占用=冲突失败），通过则先挂后摘
            if (table.ContainsKey(newName))
            {
                _logger.WriteWarn($"调试接口: 更新标签组[{oldKey}]失败，新组名[{newName}]已被占用");
                return false;
            }

            tagGroup.GroupName = newName;
            foreach (var tag in tagGroup.Tags.Values)
            {
                tag.GroupName = newName;
                tag.EnsureTypedValue();
            }
            table[newName] = tagGroup;         // 先挂新名
            if (oldKey != null)
                table.TryRemove(oldKey, out _); // 后摘旧名（弱一致窗口：新旧并存一拍，IoWorker 差一拍自愈先例）
            _logger.WriteInfo($"调试接口: 更新标签组[{oldKey}]→[{newName}]到PLC[{plcName}]");
            return true;
        }

        /// <summary>
        /// 动态移除标签组（原子）
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="groupName">组名</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveTagGroup(string plcName, string groupName)
        {
            var table = GetTagTable(plcName);
            if (table == null || !table.TryRemove(groupName, out _))
                return false;

            _logger.WriteInfo($"调试接口: 移除PLC[{plcName}]标签组[{groupName}]");
            return true;
        }

        /// <summary>
        /// 动态添加标签到指定组（原子；组身份不变量：tag.GroupName 强制同步为 groupName）
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="groupName">组名</param>
        /// <param name="tag">标签项</param>
        public void AddTag(string plcName, string groupName, TagItem tag)
        {
            if (string.IsNullOrEmpty(plcName) || string.IsNullOrEmpty(groupName) || tag == null)
                return;

            // 防御：外部构造的标签可能未定型值容器，确保按 DataType 创建（否则高频读退化为 object 兜底）；组身份同步
            tag.GroupName = groupName;
            tag.EnsureTypedValue();

            var table = _plcTagGroups.GetOrAdd(plcName, _ => new ConcurrentDictionary<string, TagGroup>());
            var group = table.GetOrAdd(groupName, _ => new TagGroup { GroupName = groupName });
            group.Tags[tag.TagName] = tag;
            _logger.WriteInfo($"调试接口: 添加标签[{tag.TagName}]到PLC[{plcName}]组[{groupName}]");
        }

        /// <summary>
        /// 动态更新标签（原子：整体替换，含改名先挂后摘 + 组身份同步）。
        /// 语义（2026-09-08 定稿）：UpdateTag=整标签 upsert（同名=覆盖属性；异名=改名迁移），AddTag=仅添加新标签。
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="groupName">组名</param>
        /// <param name="oldTagName">原标签名（CD Key；null/空=无原标签，等价 AddTag）</param>
        /// <param name="tag">整标签新对象（TagName=新标签名；改名时与 oldTagName 不同）</param>
        /// <returns>是否成功（false=参数无效/组不存在/新标签名与组内其它标签冲突）</returns>
        public bool UpdateTag(string plcName, string groupName, string oldTagName, TagItem tag)
        {
            if (string.IsNullOrEmpty(plcName) || string.IsNullOrEmpty(groupName) || tag == null)
                return false;

            string newTagName = (tag.TagName ?? string.Empty).Trim();
            if (string.IsNullOrEmpty(newTagName))
                return false;

            var table = GetTagTable(plcName);
            if (table == null || !table.TryGetValue(groupName, out var group))
                return false;

            // 同名覆盖：无键迁移
            if (string.Equals(oldTagName, newTagName, StringComparison.Ordinal) || string.IsNullOrEmpty(oldTagName))
            {
                tag.GroupName = groupName;
                tag.EnsureTypedValue();
                group.Tags[newTagName] = tag;
                _logger.WriteInfo($"调试接口: 更新标签[{newTagName}]到PLC[{plcName}]组[{groupName}]");
                return true;
            }

            // 改名：新名占用检查
            if (group.Tags.ContainsKey(newTagName))
            {
                _logger.WriteWarn($"调试接口: 更新标签[{oldTagName}]失败，新标签名[{newTagName}]已被占用");
                return false;
            }

            tag.GroupName = groupName;
            tag.EnsureTypedValue();
            group.Tags[newTagName] = tag;         // 先挂新名
            group.Tags.TryRemove(oldTagName, out _); // 后摘旧名
            _logger.WriteInfo($"调试接口: 更新标签[{oldTagName}]→[{newTagName}]到PLC[{plcName}]组[{groupName}]");
            return true;
        }

        /// <summary>
        /// 动态移除标签（原子）
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="groupName">组名</param>
        /// <param name="tagName">标签名</param>
        /// <returns>是否成功移除</returns>
        public bool RemoveTag(string plcName, string groupName, string tagName)
        {
            var table = GetTagTable(plcName);
            if (table == null || !table.TryGetValue(groupName, out var group))
                return false;

            bool removed = group.Tags.TryRemove(tagName, out _);
            if (removed)
            {
                _logger.WriteInfo($"调试接口: 移除PLC[{plcName}]组[{groupName}]标签[{tagName}]");
            }
            return removed;
        }
        #endregion

        #region 配置验证
        /// <summary>
        /// 规范化PLC配置（Excel 加载后调用）：Trim + 通用默认值兜底
        /// 原 ExcelPlcConfigRow 手工转换逻辑收敛于此，字段默认值与旧行为一致；
        /// 例外：Port / Protocol 于 2026-09-13 起不再兜底（原兜底值是 CIP 专用，会静默污染其他协议）
        /// </summary>
        private static void NormalizePlcConfig(PlcConfig plc)
        {
            plc.Name = plc.Name?.Trim();
            plc.Type = string.IsNullOrWhiteSpace(plc.Type) ? "PLC" : plc.Type.Trim();
            // Ip 是连接端点（网络=IP / 串口=COM 号）：空值不兜底 127.0.0.1——
            // 兜底会把串口设备的空端点静默填成 IP，错误被藏到连接期才爆，交由 ValidateDevConfig 响亮报错
            plc.Ip = plc.Ip?.Trim();
            // Port / Protocol 不做兜底（2026-09-13 协议扩展时删除）：
            // 原兜底值 "44818" / "EIP" 都是 CIP 专用 —— 会把西门子 S7 配置静默改成欧姆龙端口（连不上且难查），
            // 而 "EIP" 根本不是 PlcProtocol 成员，留空必抛 NotSupportedException。
            // Port 留空是有意义值：= 用驱动层该协议的默认端口（OmronCip=44818 / SiemensS7=102，见各驱动私有 DefaultPort），
            // 故仅 Trim 不兜底；Protocol 留空则由 ValidateDevConfig 与协议白名单"响亮报错"，不猜协议语义。
            plc.Port = plc.Port?.Trim();
            plc.Protocol = plc.Protocol?.Trim();
            plc.PlcTagsPath = string.IsNullOrWhiteSpace(plc.PlcTagsPath) ? null : plc.PlcTagsPath.Trim();
            plc.PollingTime = string.IsNullOrWhiteSpace(plc.PollingTime) ? "100" : plc.PollingTime.Trim();
            plc.RetryConnect = string.IsNullOrWhiteSpace(plc.RetryConnect) ? "0" : plc.RetryConnect.Trim();
            plc.LogPath = string.IsNullOrWhiteSpace(plc.LogPath) ? $"./Logs/PLC/{plc.Name}" : plc.LogPath.Trim();
        }

        /// <summary>
        /// 验证PLC设备配置的有效性
        /// </summary>
        private void ValidateDevConfig(PlcConfig plc)
        {

            if (plc == null)
            {
                string error = "设备配置无效：PLC节点为空";
                _logger.WriteError(error);
                throw new Exception(error);
            }


            if (string.IsNullOrWhiteSpace(plc.Name))
            {
                string error = "PLC配置错误：Name不能为空";
                _logger.WriteError(error);
                throw new Exception(error);
            }

            if (string.IsNullOrWhiteSpace(plc.Protocol))
            {
                string error = $"PLC[{plc.Name}]配置错误：Protocol不能为空";
                _logger.WriteError(error);
                throw new Exception(error);
            }

            if (string.IsNullOrWhiteSpace(plc.Ip))
            {
                string error = $"PLC[{plc.Name}]配置错误：连接端点不能为空（网络协议填 IP 地址，串口协议填串口号，如 COM3）";
                _logger.WriteError(error);
                throw new Exception(error);
            }

            // 端口可留空 = 用驱动层该协议默认端口（OmronCip=44818 / SiemensS7=102）；
            // 填了则必须合法（非数字 / 越界仍抛），不猜协议语义
            if (!string.IsNullOrWhiteSpace(plc.Port)
                && (!int.TryParse(plc.Port, out int port) || port <= 0 || port > 65535))
            {
                string error = $"PLC[{plc.Name}]配置错误：Port[{plc.Port}]无效（留空 = 用协议默认端口）";
                _logger.WriteError(error);
                throw new Exception(error);
            }

            // 检查标签配置文件路径
            if (!string.IsNullOrWhiteSpace(plc.PlcTagsPath))
            {
                string fullPath = Path.IsPathRooted(plc.PlcTagsPath)
                    ? plc.PlcTagsPath
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, plc.PlcTagsPath);

                if (!File.Exists(fullPath))
                {
                    _logger.WriteWarn($"PLC[{plc.Name}]标签配置文件不存在: {fullPath}");
                }
            }

            _logger.WriteDebug($"PLC配置验证通过: {plc.Name}");

        }
        #endregion

        #region 工具方法
        /// <summary>
        /// 是否为「读策略忽略 Length」的标量类型（加载期一致性校验用）
        /// 判据：非数组（数组类型一律以 Array 结尾，与 PlcDataType 命名约定一致）且非 String——
        /// String 的 Length 是 HSL 读取跨度 ReadString(address, length)，有实际用途，不参与本校验
        /// </summary>
        private static bool IsScalarType(PlcDataType dataType)
        {
            if (dataType == PlcDataType.String)
                return false;
            return !dataType.ToString().EndsWith("Array", StringComparison.Ordinal);
        }

        /// <summary>
        /// 标签唯一键（组名+标签名，跨组重名标签不混淆）
        /// </summary>
        private static string GetTagKey(string groupName, string tagName)
        {
            return groupName + "\u0001" + tagName;
        }

        /// <summary>
        /// 清空所有配置缓存
        /// </summary>
        public void Clear()
        {
            _plcConfigs.Clear();
            _plcTagGroups.Clear();
            _logger.WriteInfo("配置缓存已清空");
        }

        /// <summary>
        /// 清空指定PLC的所有配置缓存
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        public void ClearPlcConfig(string plcName)
        {
            RemovePlcConfig(plcName);
            RemovePlcTagGroups(plcName);
            _logger.WriteInfo($"已清空PLC[{plcName}]的所有配置缓存");
        }

        /// <summary>
        /// 重新加载指定PLC的标签配置
        /// </summary>
        /// <param name="plcName">PLC名称</param>
        /// <param name="excelPath">标签配置文件路径，如果为空则使用原有路径</param>
        /// <returns>加载是否成功</returns>
        public bool ReloadTagConfig(string plcName, string excelPath = null)
        {
            if (!_plcConfigs.TryGetValue(plcName, out var plc) || plc == null)
                return false;

            string path = excelPath ?? plc.PlcTagsPath;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            return LoadTagPollingConfigForPlc(plcName, path, _logger);
        }
        #endregion
    }
}