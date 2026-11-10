using Autofac;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Taoyl.Application.Share.Paths;
using Taoyl.UI.FormMain.Models;
using MenuItem = Taoyl.UI.FormMain.Models.MenuItem;

namespace Taoyl.UI.FormMain.Services
{
    /// <summary>
    /// 菜单管理器 - 负责菜单加载和窗体实例化（RBAC改造版）
    /// 新增权限码驱动过滤，保留旧Level接口兼容至批次11
    /// </summary>
    public class MenuManager
    {
        private readonly IComponentContext _context;
        private const string ConfigPath = ConfigPathName.MenuFile;

        private MenuConfig _menuConfig;

        public MenuManager(IComponentContext context)
        {
            _context = context;
            LoadMenuConfig();
        }

        #region 菜单加载

        /// <summary>
        /// 加载菜单配置
        /// </summary>
        public bool LoadMenuConfig()
        {
            try
            {
                if (!File.Exists(ConfigPath))
                {
                    CreateDefaultMenuConfig();
                }

                string json = File.ReadAllText(ConfigPath);
                _menuConfig = JsonConvert.DeserializeObject<MenuConfig>(json);
                return _menuConfig != null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"加载菜单配置失败: {ex.Message}");
                MessageBox.Show($"加载菜单配置失败: {ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }
        }

        /// <summary>
        /// 产品显示名（Menu.json 的 MenuName）：窗口标题与标题栏文字的运行期唯一真相。
        /// 现场改名只需改配置重启，不必重编译——exe 的出厂名走 AssemblyTitle，两者分工不同。
        /// </summary>
        public string MenuName => _menuConfig?.MenuName;

        public IReadOnlyList<MenuItem> GetAllMenuItems()
        {
            if (_menuConfig?.MenuItems == null)
                return new List<MenuItem>();

            return FlattenMenuItems(_menuConfig.MenuItems).ToList();
        }
        private IEnumerable<MenuItem> FlattenMenuItems(List<MenuItem> items)
        {
            foreach (var item in items)
            {
                yield return item;
                if (item.Children?.Count > 0)
                {
                    foreach (var child in FlattenMenuItems(item.Children))
                        yield return child;
                }
            }
        }

        #endregion

        #region 权限过滤（RBAC新版 - 权限码驱动）

        /// <summary>
        /// 根据用户权限码集合获取菜单（RBAC新版）
        /// </summary>
        public List<MenuItem> GetMenuItemsByPermission(HashSet<string> userPermissionCodes)
        {
            if (_menuConfig?.MenuItems == null)
                return new List<MenuItem>();

            var result = new List<MenuItem>();
            foreach (var item in _menuConfig.MenuItems)
            {
                var filteredItem = FilterMenuItemByCodes(item, userPermissionCodes);
                if (filteredItem != null)
                    result.Add(filteredItem);
            }

            result.Sort((a, b) => a.SortOrder.CompareTo(b.SortOrder));
            return result;
        }

        /// <summary>
        /// 递归按权限码过滤菜单项
        /// </summary>
        private MenuItem FilterMenuItemByCodes(MenuItem source, HashSet<string> codes)
        {
            if (codes == null) return null;

            string menuCode = "Page:" + source.Name;
            bool hasSelfPermission = codes.Contains(menuCode);

            var menu = source.Clone();
            menu.Children = new List<MenuItem>();

            if (source.Children?.Count > 0)
            {
                foreach (var child in source.Children)
                {
                    var filteredChild = FilterMenuItemByCodes(child, codes);
                    if (filteredChild != null)
                        menu.Children.Add(filteredChild);
                }
            }

            //自己没权限，但子节点有权限 → 作为导航容器保留
            if (!hasSelfPermission && menu.Children.Count == 0)
                return null;

            return menu;
        }

        #endregion

        #region 窗体实例化

        /// <summary>
        /// 获取窗体实例：直接从容器按名称解析。
        /// 页面注册为 SingleInstance，容器即唯一实例缓存——本类不再另存一份，
        /// 否则窗体被 Dispose 后容器仍持有同一引用，形成僵尸单例。
        /// </summary>
        public Form GetFormInstance(MenuItem menuItem)
        {
            if (menuItem == null || string.IsNullOrEmpty(menuItem.Class) || string.IsNullOrEmpty(menuItem.DllName))
                return null;

            string formKey = string.Join(".", menuItem.DllName, menuItem.Class);

            try
            {
                if (_context.TryResolveNamed(formKey, typeof(Form), out object instance) && instance is Form form)
                    return form;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"从容器解析窗体失败: {formKey}, 错误: {ex.Message}");
            }

            throw new InvalidOperationException(
                $"无法创建窗体 {menuItem.DisplayName}，请确保已在 Bootstrapper 中以名称 {formKey} 注册为 Named<Form>。");
        }

        /// <summary>
        /// 获取默认菜单项
        /// </summary>
        public MenuItem GetDefaultMenuItem(HashSet<string> userPermissionCodes)
        {
            var menus = GetMenuItemsByPermission(userPermissionCodes);

            // 查找标记为默认的
            foreach (var menu in menus)
            {
                if (menu.IsDefault && menu.IsFormNode())
                    return menu;

                var defaultChild = FindDefaultInChildren(menu);
                if (defaultChild != null)
                    return defaultChild;
            }

            // 返回第一个可用的
            return menus.FirstOrDefault(m => m.IsFormNode());
        }

        private MenuItem FindDefaultInChildren(MenuItem menu)
        {
            if (menu.Children == null) return null;

            foreach (var child in menu.Children)
            {
                if (child.IsDefault && child.IsFormNode())
                    return child;

                var found = FindDefaultInChildren(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        #endregion

        #region 启动自检

        /// <summary>
        /// 启动自检：校验菜单全部页面节点是否已有对应 Named&lt;Form&gt; 注册。
        /// 只查容器注册表——IsRegisteredWithName 与 TryResolveNamed 构造同一个
        /// KeyedService(DllName.Class, Form) 查找键，但不实例化任何窗体，零副作用。
        /// 返回缺失清单（DisplayName + formKey），全部已注册则返回空列表。
        /// </summary>
        public List<string> GetUnregisteredPages()
        {
            var missing = new List<string>();
            foreach (var item in GetAllMenuItems())
            {
                if (!item.IsFormNode())
                    continue;

                string formKey = string.Join(".", item.DllName, item.Class);
                if (!_context.IsRegisteredWithName(formKey, typeof(Form)))
                    missing.Add($"{item.DisplayName}（{formKey}）");
            }
            return missing;
        }

        #endregion

        #region 默认配置

        private void CreateDefaultMenuConfig()
        {
            try
            {
                string directory = Path.GetDirectoryName(ConfigPath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var defaultConfig = new MenuConfig
                {
                    MenuName = "IFA 产线控制系统",
                    Version = "2.0.1",
                    Description = "上位机系统",
                    MenuItems = new List<MenuItem>
                    {
                        new MenuItem
                        {
                            Name = "MainPage",
                            DisplayName = "主界面",
                            Class = "Main",
                            DllName = "FormMain",
                            Icon = "主界面",
                            Level = 0,
                            IsDefault = true,
                            SortOrder = 0
                        }
                    }
                };

                string json = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(ConfigPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建默认菜单配置失败: {ex.Message}");
            }
        }

        #endregion
    }
}