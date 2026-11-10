using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MiniExcelLibs;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Share.Paths;
using Taoyl.Application.TranslateDriver.Core;
using Taoyl.Application.TranslateDriver.Interfaces;
using Taoyl.Application.TranslateDriver.Storage;
using Taoyl.UI.Pages.Models;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 多语言资源配置页 ViewModel
    ///
    /// 核心设计：
    /// 1. 树双来源：Menu.json（配置项 + 友好 DisplayName）+ Language 目录扫描（已有资源类），合并去重 config 优先
    /// 2. 树分层：VM 只产出纯数据模型 ResourceTreeNode（不依赖 System.Windows.Forms.TreeNode），
    ///    View 负责渲染 TreeNode（参照 FormRoleManager 权限树模式：VM 产数据 → View 渲染）
    /// 3. 后台加载：读配置/扫描目录在 Task.Run 中执行，UI 线程只做数据模型构建与 DataTable 生成
    /// 4. 网格编辑：DGV 保持 DataTable 绑定（编辑顺手），保存时经 ResourceMapper.ToList 回写
    /// 5. 保存合并两页：修复原实现只保存当前页导致另一页缓存被清空的问题
    /// 6. 搜索过滤：DataTable.DefaultView.RowFilter 过滤当前网格，不新建表、不丢编辑
    /// </summary>
    public partial class ResourceConfigViewModel : ObservableObject
    {
        #region 依赖注入

        private readonly IResourceManager resourceManager;
        private readonly IMessageBoxService messageBox;
        private readonly IUserSession userSession;
        private readonly IAuditService auditService;

        #endregion

        #region 树构建状态

        private readonly object treeLock = new object();
        private readonly Dictionary<string, ResourceTreeNode> nodeMap = new Dictionary<string, ResourceTreeNode>();
        private readonly List<ResourceTreeNode> rootNodes = new List<ResourceTreeNode>();
        private MenuConfigDto menuConfig = new MenuConfigDto();

        /// <summary>树结构变更事件（View 订阅后重渲染 TreeView；可能从后台线程触发，View 需自行切线程）</summary>
        public event EventHandler TreeStructureChanged;

        /// <summary>
        /// 树根节点快照（线程安全：锁内复制，后台线程 BuildMenuTree/EnsureTreeNodeForClass 与 UI 渲染并发安全）
        /// </summary>
        public ResourceTreeNode[] RootNodes
        {
            get { lock (treeLock) return rootNodes.ToArray(); }
        }

        #endregion

        #region 当前类状态

        private string currentNamespace = "";
        private string currentClassName = "";
        private List<ResourceEntry> currentEntries = new List<ResourceEntry>();
        private ResourceType activeGridType;
        private DataTable activeDataTable;

        /// <summary>
        /// 类未落盘变更标记（类键 "ns.cls" → 有未保存变更）。
        /// 提取/编辑/导入置位，保存成功清除；确认丢弃（切类确认框点"确认"）清除——
        /// 用户确认即接受"未保存修改丢失"，切回不再反复提示；
        /// 跨节点切换持久（FrmResourceConfig 无标题栏，未保存提醒由切类确认框承担）
        /// </summary>
        private readonly Dictionary<string, bool> classDirtyMap =
            new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);

        /// <summary>当前网格类型（保存基于当前页数据）</summary>
        public ResourceType ActiveGridType => activeGridType;

        #endregion

        #region 可观察属性

        [ObservableProperty]
        private string moduleName = "";

        [ObservableProperty]
        private string className = "";

        [ObservableProperty]
        private string searchKeyword = "";

        [ObservableProperty]
        private bool isModified;

        [ObservableProperty]
        private DataTable uiGridTable;

        [ObservableProperty]
        private DataTable textGridTable;

        #endregion

        public ResourceConfigViewModel(IResourceManager resourceManager, IMessageBoxService messageBox,
            IUserSession userSession, IAuditService auditService)
        {
            this.resourceManager = resourceManager ?? throw new ArgumentNullException(nameof(resourceManager));
            this.messageBox = messageBox ?? throw new ArgumentNullException(nameof(messageBox));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.auditService = auditService ?? throw new ArgumentNullException(nameof(auditService));
            // 订阅缓存变更：运行时动态新增类（手动注册控件）时自动补树节点
            this.resourceManager.ClassChanged += OnCacheClassChanged;
        }

        #region 初始化

        /// <summary>
        /// 初始化：后台读取 Menu.json + 扫描 Language 目录，UI 线程构建树
        /// </summary>
        public async Task InitializeAsync()
        {
            await Task.Run(() => LoadMenuConfig());
            BuildMenuTree();
        }

        private void LoadMenuConfig()
        {
            try
            {
                var menuPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPathName.MenuFile);
                if (File.Exists(menuPath))
                {
                    var json = File.ReadAllText(menuPath);
                    menuConfig = JsonConvert.DeserializeObject<MenuConfigDto>(json) ?? new MenuConfigDto();
                }
            }
            catch
            {
                menuConfig = new MenuConfigDto();
            }
        }

        private void OnCacheClassChanged(object sender, ClassChangedEventArgs e)
        {
            EnsureTreeNodeForClass(e.Namespace, e.ClassName);
        }

        #endregion

        #region 树构建（双来源：Menu.json + 目录扫描）

        /// <summary>
        /// 合并配置与扫描结果，重建整树（初始化时调用）
        /// </summary>
        public void BuildMenuTree()
        {
            lock (treeLock)
            {
                rootNodes.Clear();
                nodeMap.Clear();

                var configInfos = GetMenuInfosFromConfig();
                var scanInfos = GetMenuInfosFromFileScan();

                // 合并去重：config 在前，优先保留配置中的 DisplayName
                var mergedMenuInfos = configInfos
                    .Concat(scanInfos)
                    .GroupBy(info => info.Key)
                    .Select(g => g.First())
                    .ToList();

                foreach (var group in mergedMenuInfos.GroupBy(info => info.Namespace))
                {
                    var namespaceNode = CreateOrGetNamespaceNode(group.Key);
                    foreach (var menuInfo in group)
                    {
                        CreateClassNode(namespaceNode, menuInfo);
                    }
                }
            }

            TreeStructureChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 运行时动态新增类时确保树节点存在（新增成功会触发 TreeStructureChanged）
        /// </summary>
        public void EnsureTreeNodeForClass(string ns, string cls)
        {
            bool added = false;
            lock (treeLock)
            {
                string fullName = $"{ns}.{cls}";
                if (nodeMap.ContainsKey(fullName)) return;

                var menuItem = FindMenuItemByFullName(fullName);
                var displayName = menuItem?.DisplayName ?? cls;
                var fromConfig = menuItem != null;

                var namespaceNode = CreateOrGetNamespaceNode(ns);
                var classNode = new ResourceTreeNode
                {
                    Key = fullName,
                    DisplayName = displayName,
                    Namespace = ns,
                    ClassName = cls,
                    FromConfig = fromConfig
                };

                namespaceNode.Children.Add(classNode);
                nodeMap[fullName] = classNode;
                added = true;
            }

            if (added)
                TreeStructureChanged?.Invoke(this, EventArgs.Empty);
        }

        private IEnumerable<MenuInfo> GetMenuInfosFromConfig()
        {
            if (menuConfig?.MenuItems == null) yield break;

            foreach (var item in FlattenMenuItems(menuConfig.MenuItems))
            {
                if (!string.IsNullOrEmpty(item.DllName) && !string.IsNullOrEmpty(item.Class))
                {
                    yield return new MenuInfo
                    {
                        Namespace = item.DllName,
                        ClassName = item.Class,
                        DisplayName = item.DisplayName ?? item.Class,
                        Source = MenuInfoSource.Config
                    };
                }
            }
        }

        private IEnumerable<MenuItemDto> FlattenMenuItems(IEnumerable<MenuItemDto> items)
        {
            foreach (var item in items)
            {
                yield return item;
                if (item.HasChildren)
                {
                    foreach (var child in FlattenMenuItems(item.Children))
                        yield return child;
                }
            }
        }

        /// <summary>
        /// 扫描本地语言目录，根据存在的 JSON 文件提取 MenuInfo 项
        /// </summary>
        private IEnumerable<MenuInfo> GetMenuInfosFromFileScan()
        {
            var basePath = resourceManager.BasePath;
            var classSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (ResourceType resourceType in Enum.GetValues(typeof(ResourceType)))
            {
                var dir = Path.Combine(basePath, resourceType.ToString());
                if (!Directory.Exists(dir)) continue;

                try
                {
                    foreach (var nsDir in Directory.GetDirectories(dir))
                    {
                        var ns = Path.GetFileName(nsDir);
                        foreach (var file in Directory.GetFiles(nsDir, "*.json"))
                        {
                            var cls = Path.GetFileNameWithoutExtension(file);
                            classSet.Add($"{ns}.{cls}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FrmResourceConfig] 扫描目录失败 {dir}: {ex.Message}");
                }
            }

            foreach (var fullName in classSet)
            {
                int lastDotIndex = fullName.LastIndexOf('.');
                if (lastDotIndex > 0)
                {
                    yield return new MenuInfo
                    {
                        Namespace = fullName.Substring(0, lastDotIndex),
                        ClassName = fullName.Substring(lastDotIndex + 1),
                        DisplayName = fullName.Substring(lastDotIndex + 1),
                        Source = MenuInfoSource.Scan
                    };
                }
                else
                {
                    yield return new MenuInfo
                    {
                        Namespace = "Default",
                        ClassName = fullName,
                        DisplayName = fullName,
                        Source = MenuInfoSource.Scan
                    };
                }
            }
        }

        /// <summary>
        /// 创建或获取一级命名空间节点（调用方需已持有 treeLock）
        /// </summary>
        private ResourceTreeNode CreateOrGetNamespaceNode(string namespaceName)
        {
            if (nodeMap.TryGetValue(namespaceName, out var cachedNode))
                return cachedNode;

            var namespaceNode = rootNodes.FirstOrDefault(n => n.Key == namespaceName);
            if (namespaceNode == null)
            {
                namespaceNode = new ResourceTreeNode
                {
                    Key = namespaceName,
                    DisplayName = namespaceName
                };
                rootNodes.Add(namespaceNode);
            }

            nodeMap[namespaceName] = namespaceNode;
            return namespaceNode;
        }

        /// <summary>
        /// 在指定命名空间节点下创建类节点（调用方需已持有 treeLock）
        /// </summary>
        private void CreateClassNode(ResourceTreeNode parentNode, MenuInfo menuInfo)
        {
            string fullKey = menuInfo.Key;
            if (nodeMap.ContainsKey(fullKey)) return;

            var classNode = new ResourceTreeNode
            {
                Key = fullKey,
                DisplayName = menuInfo.DisplayName,
                Namespace = menuInfo.Namespace,
                ClassName = menuInfo.ClassName,
                // 用来源标记区分：黑色来自配置，蓝色来自扫描（颜色由 View 渲染时决定）
                FromConfig = menuInfo.Source == MenuInfoSource.Config
            };

            parentNode.Children.Add(classNode);
            nodeMap[fullKey] = classNode;
        }

        private MenuItemDto FindMenuItemByFullName(string fullName)
        {
            if (menuConfig?.MenuItems == null) return null;
            return FindMenuItemRecursive(menuConfig.MenuItems, fullName);
        }

        private MenuItemDto FindMenuItemRecursive(List<MenuItemDto> items, string fullName)
        {
            foreach (var item in items)
            {
                var full = !string.IsNullOrEmpty(item.DllName) && !string.IsNullOrEmpty(item.Class)
                    ? $"{item.DllName}.{item.Class}"
                    : null;
                if (full == fullName) return item;

                if (item.HasChildren)
                {
                    var found = FindMenuItemRecursive(item.Children, fullName);
                    if (found != null) return found;
                }
            }
            return null;
        }

        #endregion

        #region 类资源加载

        /// <summary>
        /// 选中类：后台加载资源（首次读文件，后续走缓存），UI 线程生成两个 DataTable
        /// </summary>
        public async Task SelectClassAsync(string ns, string cls)
        {
            currentNamespace = ns;
            currentClassName = cls;
            ModuleName = ns;
            ClassName = cls;

            await Task.Run(() => LoadResources(ns, cls));
            RefreshGridTables();
            // 恢复该类未落盘变更标记：提取/编辑后未保存切走再切回，切类确认框恢复提醒保存
            IsModified = classDirtyMap.TryGetValue($"{ns}.{cls}", out var dirty) && dirty;
        }

        /// <summary>
        /// 标记当前类有未落盘变更（提取/网格编辑/导入后调用）。
        /// 写入 classDirtyMap 跨节点切换保留，保存成功清除；View 的网格编辑事件转调此方法
        /// </summary>
        public void MarkCurrentClassDirty()
        {
            IsModified = true;
            classDirtyMap[$"{currentNamespace}.{currentClassName}"] = true;
        }

        /// <summary>
        /// 用户确认丢弃当前类未保存修改（切类确认框点"确认"后由 View 调用）：
        /// 清除未落盘标记——用户确认即接受"未保存修改丢失"，切回不再反复提示。
        /// （提取/导入的缓存条目本身仍在，切回仍可见可保存，只是不再弹提醒）
        /// </summary>
        public void DiscardCurrentClassEdits()
        {
            var key = $"{currentNamespace}.{currentClassName}";
            classDirtyMap[key] = false;
            IsModified = false;
        }

        private void LoadResources(string ns, string cls)
        {
            // 从缓存获取资源（若缓存中没有，EnsureClassLoaded 会触发懒加载读取 JSON）
            currentEntries = resourceManager.GetClassResourcesMerged(ns, cls).ToList();

            // 极端情况：文件存在但缓存为空（JSON 格式问题），直接从文件加载兜底
            if (!currentEntries.Any())
            {
                var uiPath = Path.Combine(resourceManager.BasePath, "UI", ns, $"{cls}.json");
                var textPath = Path.Combine(resourceManager.BasePath, "Text", ns, $"{cls}.json");
                currentEntries.AddRange(resourceManager.LoadEntriesFromFile(uiPath));
                currentEntries.AddRange(resourceManager.LoadEntriesFromFile(textPath));
                resourceManager.AddRange(currentEntries);
            }
        }

        private void RefreshGridTables()
        {
            UiGridTable = BuildTable(currentEntries, ResourceType.UI);
            TextGridTable = BuildTable(currentEntries, ResourceType.Text);
        }

        private static DataTable BuildTable(List<ResourceEntry> entries, ResourceType type)
        {
            return ResourceMapper.ToDataTable(entries.Where(e => e.Type == type).ToList());
        }

        /// <summary>
        /// 导航到指定类（导入/提取后跳转），确保树节点存在并加载资源
        /// </summary>
        public async Task NavigateToClassAsync(string ns, string cls)
        {
            EnsureTreeNodeForClass(ns, cls);
            await SelectClassAsync(ns, cls);
        }

        /// <summary>
        /// 设置当前活动网格（View 在 Tab 切换 / 数据刷新后调用，供保存使用）
        /// </summary>
        public void SetActiveGrid(ResourceType type)
        {
            activeGridType = type;
            activeDataTable = type == ResourceType.UI ? UiGridTable : TextGridTable;
        }

        public DataTable GetGridTable(ResourceType type)
        {
            return type == ResourceType.UI ? UiGridTable : TextGridTable;
        }

        /// <summary>当前类是否有可导出数据（View 控制导出对话框前检查）</summary>
        public bool HasDataToExport() => currentEntries.Count > 0;

        #endregion

        #region 命令

        /// <summary>
        /// 保存当前页数据（合并另一页缓存，避免跨页编辑丢失）
        /// </summary>
        [RelayCommand]
        private void Save()
        {
            if (activeDataTable == null)
            {
                messageBox.ShowWarning("没有可保存的数据");
                return;
            }

            // 两页都从对应 DataTable 物化（修复跨 Tab 编辑丢失：另一页 Tab 编辑过但未切回，
            // 原实现用 currentEntries 缓存旧值导致改动静默丢失）
            var newEntries = MaterializeEntries(activeDataTable);
            var otherTable = activeGridType == ResourceType.UI ? TextGridTable : UiGridTable;
            var otherEntries = otherTable == null ? new List<ResourceEntry>() : MaterializeEntries(otherTable);
            currentEntries = newEntries.Concat(otherEntries).ToList();

            resourceManager.ClearClass(currentNamespace, currentClassName);
            resourceManager.AddRange(currentEntries);

            var uiEntries = currentEntries.Where(p => p.Type == ResourceType.UI).ToList();
            var textEntries = currentEntries.Where(p => p.Type == ResourceType.Text).ToList();
            bool saveOk = true;
            if (uiEntries.Any())
                saveOk &= resourceManager.SaveClassResources(uiEntries, ResourceType.UI);
            if (textEntries.Any())
                saveOk &= resourceManager.SaveClassResources(textEntries, ResourceType.Text);

            if (saveOk)
            {
                IsModified = false;
                // 保存成功清除该类未落盘标记（切回时不再提示）
                classDirtyMap[$"{currentNamespace}.{currentClassName}"] = false;
                messageBox.ShowSuccess("保存成功");
            }
            else
            {
                messageBox.ShowError("保存失败，请检查文件权限");
            }

            // saveOk 是三段落盘的与结果（UI 页 + Text 页任一失败即为假），直接拿它判成败
            auditService.Record(new AuditEntry(AuditCategory.Resource, AuditAction.Save, ClassTarget(),
                userSession.CurrentUser.ToAuditActor(), saveOk ? AuditResult.Success : AuditResult.Fail));
        }

        /// <summary>
        /// 把网格 DataTable 物化成可落盘资源条目：补命名空间/类名归属、空 Key 兜底生成、
        /// 从缓存回填原文（网格编辑只改翻译，原文以缓存为准）。
        /// </summary>
        private List<ResourceEntry> MaterializeEntries(DataTable table)
        {
            var entries = ResourceMapper.ToList(table);
            var oldDict = currentEntries.ToDictionary(p => p.Key);

            foreach (var entry in entries)
            {
                entry.Namespace = currentNamespace;
                entry.ClassName = currentClassName;
                // 仅空 Key 时兜底生成，保留用户在网格中编辑过的 Key（修复保存覆盖 Key 问题）
                if (string.IsNullOrEmpty(entry.Key))
                    entry.GenerateKey();
                if (oldDict.TryGetValue(entry.Key, out var oldEntry))
                    entry.OriginalText = oldEntry.OriginalText;
            }
            return entries;
        }

        /// <summary>
        /// 导出当前类全部资源为 Excel
        /// </summary>
        [RelayCommand]
        private void Export(string filePath)
        {
            if (currentEntries.Count == 0)
            {
                messageBox.ShowWarning("没有可导出的数据");
                return;
            }

            bool ok = false;
            try
            {
                var dt = ResourceMapper.ToDataTable(currentEntries);
                MiniExcel.SaveAs(filePath, dt, overwriteFile: true);
                ok = true;
                messageBox.ShowSuccess("导出成功");
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"导出失败: {ex.Message}");
            }

            auditService.Record(new AuditEntry(AuditCategory.Resource, AuditAction.Export, FileTarget(filePath),
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
        }

        /// <summary>
        /// 从 Excel 导入资源到缓存（合并翻译，保留原文），导入后导航到第一个类
        /// </summary>
        [RelayCommand]
        private async Task ImportAsync(string filePath)
        {
#pragma warning disable 0618 // MiniExcel.QueryAsDataTable 已过时（一次性载入内存）；资源导入数据量小，可接受
            bool ok = false;
            try
            {
                var dt = MiniExcel.QueryAsDataTable(filePath);
                if (dt.Rows.Count == 0)
                {
                    messageBox.ShowWarning("Excel 文件中没有数据");
                    return;
                }

                var importedEntries = ResourceMapper.ToList(dt);
                if (importedEntries.Count == 0)
                {
                    messageBox.ShowWarning("未解析到任何资源条目");
                    return;
                }

                // 清洗类标识：外部编辑的 Excel 可能带入换行/尾随空格，空类标识无法归属，直接过滤
                foreach (var entry in importedEntries)
                {
                    entry.Namespace = entry.Namespace?.Trim();
                    entry.ClassName = entry.ClassName?.Trim();
                }
                importedEntries = importedEntries
                    .Where(x => !string.IsNullOrEmpty(x.Namespace) && !string.IsNullOrEmpty(x.ClassName))
                    .ToList();
                if (importedEntries.Count == 0)
                {
                    messageBox.ShowWarning("导入数据缺少有效的命名空间/类名，无法导入");
                    return;
                }

                var groups = importedEntries.GroupBy(p => new { p.Namespace, p.ClassName }).ToList();
                string firstNs = groups.First().Key.Namespace;
                string firstCls = groups.First().Key.ClassName;
                bool singleClass = groups.Count == 1;

                foreach (var group in groups)
                {
                    string ns = group.Key.Namespace;
                    string cls = group.Key.ClassName;
                    var entries = group.ToList();

                    var existingEntries = resourceManager.GetClassResourcesMerged(ns, cls).ToList();
                    var merged = MergeImportedEntries(existingEntries, entries);

                    resourceManager.ClearClass(ns, cls);
                    resourceManager.AddRange(merged);
                    // 导入只进缓存未落盘：标记该类 dirty，切回时切类确认框提醒保存
                    classDirtyMap[$"{ns}.{cls}"] = true;
                }

                if (!singleClass)
                {
                    messageBox.ShowInfo($"已导入 {importedEntries.Count} 条记录，涉及 {groups.Count} 个类。当前显示第一个类，请手动保存所需类。");
                }

                await NavigateToClassAsync(firstNs, firstCls);
                ok = true;
                messageBox.ShowInfo($"成功导入 {importedEntries.Count} 条记录到缓存，请检查后点击“保存”以持久化。");
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"导入失败：{ex.Message}");
            }

            // 上面几处 early return 不留痕：那是"数据不合格、动作没发起"（与门禁被拒同一规则）；
            // 只有真的执行了导入（成功 / 抛异常）才落一条
            auditService.Record(new AuditEntry(AuditCategory.Resource, AuditAction.Import, FileTarget(filePath),
                userSession.CurrentUser.ToAuditActor(), ok ? AuditResult.Success : AuditResult.Fail));
#pragma warning restore 0618
        }

        /// <summary>
        /// 按关键字过滤当前网格（DataTable.DefaultView.RowFilter，空关键字清除过滤）
        /// </summary>
        [RelayCommand]
        private void Search()
        {
            var keyword = SearchKeyword?.Trim();
            ApplyFilter(UiGridTable, keyword);
            ApplyFilter(TextGridTable, keyword);
        }

        private static void ApplyFilter(DataTable table, string keyword)
        {
            if (table == null) return;

            if (string.IsNullOrEmpty(keyword))
            {
                table.DefaultView.RowFilter = "";
                return;
            }

            var escaped = keyword.Replace("'", "''");
            var conditions = table.Columns.Cast<DataColumn>()
                .Select(c => $"[{c.ColumnName}] LIKE '%{escaped}%'");
            table.DefaultView.RowFilter = string.Join(" OR ", conditions);
        }

        /// <summary>
        /// 从已打开的窗体实例提取 UI 资源（手动输入或树选中指定目标类）
        /// </summary>
        [RelayCommand]
        private async Task ExtractUiAsync()
        {
            // 1. 确定目标命名空间和类名（优先手动输入，降级当前树选中）
            string targetNamespace = ModuleName?.Trim();
            string targetClassName = ClassName?.Trim();

            if (string.IsNullOrEmpty(targetNamespace) || string.IsNullOrEmpty(targetClassName))
            {
                targetNamespace = currentNamespace;
                targetClassName = currentClassName;
            }

            if (string.IsNullOrEmpty(targetNamespace) || string.IsNullOrEmpty(targetClassName))
            {
                messageBox.ShowWarning("请先在左侧树中选择一个类，或手动输入命名空间和类名");
                return;
            }

            // 目标类全名（提示文案与审计 Target 共用，避免两处各拼一次）
            string target = $"{targetNamespace}.{targetClassName}";

            // 2. 执行 UI 资源提取（不依赖缓存，直接从已打开窗体实例提取）
            var entries = resourceManager.ExtractUIResources(targetNamespace, targetClassName);
            if (entries.Count == 0)
            {
                messageBox.ShowWarning($"未找到已打开的窗体 [{target}]，请先打开该窗体再尝试");
                // 动作已发起（真的去扫过已打开窗体）却一条都没提到 ⇒ 记失败。
                // 上面两处"没选类 / 没输入类名"的 return 不留痕——那是参数缺失、动作没发起，
                // 与"门禁被拒不留痕"同一规则。
                auditService.Record(new AuditEntry(AuditCategory.Resource, AuditAction.Extract, target,
                    userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
                return;
            }

            // 3. 检查该类是否为新类（不在当前菜单树中）
            bool isNewClass = !nodeMap.ContainsKey($"{targetNamespace}.{targetClassName}");

            // 4. 加载该类现有资源（如果存在），合并（提取更新原文，翻译不动）
            var existingEntries = resourceManager.GetClassResourcesMerged(targetNamespace, targetClassName).ToList();
            var mergedEntries = MergeExtractedEntries(existingEntries, entries);

            // 5. 更新缓存并标记为未落盘（提取产物在缓存，切回仍提示保存）
            resourceManager.ClearClass(targetNamespace, targetClassName);
            resourceManager.AddRange(mergedEntries);
            MarkCurrentClassDirty();

            // 6. 当前显示的就是目标类则刷新网格，否则导航过去
            if (currentNamespace == targetNamespace && currentClassName == targetClassName)
            {
                currentEntries = mergedEntries;
                RefreshGridTables();
            }
            else
            {
                await NavigateToClassAsync(targetNamespace, targetClassName);
            }

            // 7. 新类自动补树节点并提示
            if (isNewClass)
            {
                EnsureTreeNodeForClass(targetNamespace, targetClassName);
                messageBox.ShowInfo($"提取成功！发现新窗体 [{targetClassName}]，已添加到左侧树中。共提取 {entries.Count} 条UI资源。");
            }
            else
            {
                messageBox.ShowInfo($"提取成功！共提取/更新 {entries.Count} 条UI资源。");
            }

            // 提取只进内存缓存、不落盘（落盘另有"保存"一条），故此处记的是"抓取动作"而非"数据变更"
            auditService.Record(new AuditEntry(AuditCategory.Resource, AuditAction.Extract, target,
                userSession.CurrentUser.ToAuditActor()));
        }

        #endregion

        #region 合并逻辑

        /// <summary>
        /// 基础合并：existing 为基准，incoming 按 Key 匹配，按开关更新原文/翻译
        /// </summary>
        private static List<ResourceEntry> MergeBase(List<ResourceEntry> existing, List<ResourceEntry> incoming,
            bool updateOriginal, bool updateTranslations)
        {
            var dict = existing.ToDictionary(e => e.Key, StringComparer.OrdinalIgnoreCase);
            foreach (var inc in incoming)
            {
                if (dict.TryGetValue(inc.Key, out var exist))
                {
                    if (updateOriginal && !string.IsNullOrEmpty(inc.OriginalText))
                        exist.OriginalText = inc.OriginalText;
                    if (updateTranslations)
                        foreach (var t in inc.Translations)
                            exist.Translations[t.Key] = t.Value;
                }
                else
                {
                    dict[inc.Key] = inc;
                }
            }
            return dict.Values.ToList();
        }

        /// <summary>导入合并：只更新翻译，原文保留不变</summary>
        private static List<ResourceEntry> MergeImportedEntries(List<ResourceEntry> existing, List<ResourceEntry> imported)
        {
            return MergeBase(existing, imported, false, true);
        }

        /// <summary>提取合并：更新原文，翻译不动</summary>
        private static List<ResourceEntry> MergeExtractedEntries(List<ResourceEntry> existing, List<ResourceEntry> extracted)
        {
            return MergeBase(existing, extracted, true, false);
        }

        #endregion

        #region 审计留痕

        // 本页在 Menu.json 中**只有页面码 Page:TranslateViewer、没有按钮码**，
        // 四个命令也都不做权限门禁（"能进页面即能操作"，与 PLC 调试页同规则）
        // ⇒ 这里不存在"门禁被拒"的场景，动作一旦真的发起就留痕。

        /// <summary>
        /// 当前类全名（"命名空间.类名"）作 Target。
        /// 两者都为空时拼出来是 "."，给个占位符——审计页上"."没法看出是"没选类"还是"类名异常"。
        /// </summary>
        private string ClassTarget()
        {
            string fullName = $"{currentNamespace}.{currentClassName}";
            return fullName == "." ? "(未选择类)" : fullName;
        }

        /// <summary>
        /// 文件类动作（导入 / 导出）的 Target：取纯文件名。
        /// 不取全路径——Target 列有 100 字上限（AuditService.Truncate），全路径极易被截断成半截脏数据
        /// （同 RecipeEditVM.FileTarget 的判据）。
        /// </summary>
        private static string FileTarget(string filePath)
        {
            return string.IsNullOrWhiteSpace(filePath) ? "(未指定)" : Path.GetFileName(filePath);
        }

        #endregion

        #region 树构建辅助模型（VM 内部私有）

        private class MenuInfo
        {
            public string Namespace { get; set; }
            public string ClassName { get; set; }
            public string DisplayName { get; set; }
            public MenuInfoSource Source { get; set; }
            public string Key => $"{Namespace}.{ClassName}";
        }

        private enum MenuInfoSource
        {
            Config,
            Scan
        }

        #endregion
    }

    /// <summary>
    /// 资源树节点数据模型（VM 产出，View 渲染为 TreeNode）。
    /// 纯数据类，不依赖 System.Windows.Forms.TreeNode——VM 与 UI 树控件类型解耦（参照 FormRoleManager 权限树模式）。
    /// </summary>
    public class ResourceTreeNode
    {
        /// <summary>节点唯一键：命名空间节点=命名空间名，类节点="Namespace.ClassName"</summary>
        public string Key { get; set; }

        /// <summary>节点显示文本（命名空间名 / 类友好名）</summary>
        public string DisplayName { get; set; }

        /// <summary>类节点所属命名空间（命名空间节点为空）</summary>
        public string Namespace { get; set; }

        /// <summary>类节点类名（命名空间节点为空）</summary>
        public string ClassName { get; set; }

        /// <summary>来源标记：true=来自 Menu.json（黑色），false=来自目录扫描（蓝色），View 渲染时决定颜色</summary>
        public bool FromConfig { get; set; }

        /// <summary>子节点（命名空间节点含类节点，类节点无子节点）</summary>
        public List<ResourceTreeNode> Children { get; set; } = new List<ResourceTreeNode>();

        /// <summary>是否类节点（可加载资源）</summary>
        public bool IsClassNode => !string.IsNullOrEmpty(Namespace) && !string.IsNullOrEmpty(ClassName);
    }
}
