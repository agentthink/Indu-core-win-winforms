using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.Share.Extensions;
using Taoyl.Application.Share.Paths;

namespace Taoyl.Application.AuthService.Services
{
    /// <summary>
    /// 权限同步服务
    /// 启动时完成：菜单权限解析、内置角色初始化、内置用户初始化
    /// 自动分配已废弃，改为实时计算
    /// </summary>
    public class PermissionSyncService : IPermissionSyncService
    {
        private readonly IPermissionRepository permissionRepo;
        private readonly IRoleRepository roleRepo;
        private readonly IUserRepository userRepo;
        private readonly IUserRoleRepository userRoleRepo;

        public PermissionSyncService(
            IPermissionRepository permissionRepo,
            IRoleRepository roleRepo,
            IUserRepository userRepo,
            IUserRoleRepository userRoleRepo)
        {
            this.permissionRepo = permissionRepo ?? throw new ArgumentNullException(nameof(permissionRepo));
            this.roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
            this.userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            this.userRoleRepo = userRoleRepo ?? throw new ArgumentNullException(nameof(userRoleRepo));
        }

        public bool SyncOnStartup(out string message)
        {
            message = null;
            try
            {
                // 1. 从 Menu.json 解析全部权限（菜单+功能）
                var permissions = ParseAllPermissions();

                // 2. 增量同步到 Permission 表
                SyncPermissionsToDatabase(permissions);

                // 3. 确保内置角色（RoleType枚举驱动）
                EnsureBuiltInRoles();

                // 4. 确保内置 admin 用户（返回需提示操作者的信息，无则 null）
                message = EnsureBuiltInAdminUser();
                return true;
            }
            catch (Exception ex)
            {
                // 启动同步失败不阻断启动，但必须让操作者看得见（原实现只 Debug.WriteLine = 静默）
                message = ex.Message;
                return false;
            }
        }

        #region 权限解析（合并后 Menu.json）

        private List<Permission> ParseAllPermissions()
        {
            string menuPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPathName.MenuFile);
            if (!File.Exists(menuPath)) return new List<Permission>();

            string json = File.ReadAllText(menuPath);
            var config = JsonConvert.DeserializeObject<MenuConfigDto>(json);
            if (config?.MenuItems == null) return new List<Permission>();

            return ParseMenuItems(config.MenuItems, parentCode: null);
        }

        private List<Permission> ParseMenuItems(List<MenuItemDto> items, string parentCode)
        {
            var result = new List<Permission>();
            foreach (var item in items)
            {
                string pageCode = $"Page:{item.Name}";

                // 菜单权限（Type=1）
                result.Add(new Permission
                {
                    Code = pageCode,
                    Name = item.DisplayName ?? item.Name,
                    Type = 1,
                    ParentCode = parentCode,
                    Sort = item.SortOrder,
                    Level = item.Level,
                    FormClass = item.Class,
                    DllName = item.DllName,
                    IsActive = 1
                });

                // 功能权限（Type=2），直接挂在页面下，Level继承或显式覆盖
                if (item.Features != null)
                {
                    foreach (var feat in item.Features)
                    {
                        result.Add(new Permission
                        {
                            Code = feat.Code,
                            Name = feat.Name,
                            Type = 2,
                            ParentCode = pageCode,
                            Sort = 0,
                            Level = feat.Level ?? item.Level,
                            IsActive = 1
                        });
                    }
                }

                if (item.Children?.Count > 0)
                    result.AddRange(ParseMenuItems(item.Children, pageCode));
            }
            return result;
        }

        #endregion

        #region 增量同步

        private void SyncPermissionsToDatabase(List<Permission> fromJson)
        {
            var existing = permissionRepo.GetAll();
            var jsonCodes = new HashSet<string>(fromJson.Select(p => p.Code.ToLowerInvariant()));

            foreach (var jsonPerm in fromJson)
            {
                var dbPerm = existing.FirstOrDefault(p =>
                    p.Code.Equals(jsonPerm.Code, StringComparison.OrdinalIgnoreCase));

                if (dbPerm == null)
                {
                    permissionRepo.Add(jsonPerm);
                }
                else
                {
                    dbPerm.Name = jsonPerm.Name;
                    dbPerm.ParentCode = jsonPerm.ParentCode;
                    dbPerm.Sort = jsonPerm.Sort;
                    dbPerm.Level = jsonPerm.Level;
                    dbPerm.FormClass = jsonPerm.FormClass;
                    dbPerm.DllName = jsonPerm.DllName;
                    dbPerm.IsActive = 1;
                    permissionRepo.Update(dbPerm);
                }
            }

            // DB中有、JSON中无 → 逻辑删除
            foreach (var dbPerm in existing)
            {
                if (!jsonCodes.Contains(dbPerm.Code.ToLowerInvariant()) && dbPerm.IsActive == 1)
                {
                    dbPerm.IsActive = 0;
                    permissionRepo.Update(dbPerm);
                }
            }

        }

        #endregion

        #region 内置角色（RoleType枚举驱动）

        private void EnsureBuiltInRoles()
        {
            foreach (RoleType roleType in Enum.GetValues(typeof(RoleType)))
            {
                string roleName = roleType.GetDescription();
                int level = (int)roleType;

                var existing = roleRepo.GetByName(roleName);
                if (existing != null)
                {
                    if (existing.IsBuiltIn == 1 && existing.Level != level)
                    {
                        existing.Level = level;
                        roleRepo.Update(existing);
                    }
                    continue;
                }

                roleRepo.Add(new Role
                {
                    RoleName = roleName,
                    Level = level,
                    IsBuiltIn = 1,
                    Remark = level == 0
                        ? "未登录默认角色，仅拥有Level=0权限"
                        : $"系统内置角色，等级{level}"
                });
            }
        }

        #endregion

        #region 内置管理员用户

        /// <summary>
        /// 兜底复活内置管理员。返回需提示操作者的信息（无需干预时 null）。
        /// </summary>
        private string EnsureBuiltInAdminUser()
        {
            // 判据（防锁死④）：系统中已存在任何管理员级用户即无需干预。
            // 原判据"admin 不存在"漏掉"admin 还在、但管理员角色被摘光"的情形 ⇒ 会留下无管理员系统。
            if (HasAnyAdminLevelUser()) return null;

            var adminRole = roleRepo.GetByName(RoleType.Administrator.GetDescription());
            if (adminRole == null) return null;

            var adminUser = userRepo.GetByUsername("admin");
            bool recreated = adminUser == null;
            if (recreated)
            {
                // admin 行已被删：救砖路径——以默认密码重建
                adminUser = new UserInfo
                {
                    Username = "admin",
                    Password = "111",
                    UserNumber = "001",
                    CardCode = "11111111",
                    CreateTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    AddPeople = "系统"
                };
                userRepo.Add(adminUser);
            }

            var userRoles = userRoleRepo.GetByUserId(adminUser.UserId);
            if (!userRoles.Any(ur => ur.RoleId == adminRole.Id))
            {
                userRoleRepo.Add(new UserRole { UserId = adminUser.UserId, RoleId = adminRole.Id });
                
            }

            // 有声：兜底复活必须让操作者看得见，否则系统会静默多出一个默认密码账户。
            // 本模块不引日志类库——只返回提示文本，呈现（落日志）由组合根负责。
            return recreated
                ? "检测到系统无管理员，已重建默认管理员账户 admin（默认密码），请立即修改密码"
                : "检测到系统无管理员，已恢复内置管理员账户 admin 的管理员角色";
        }

        /// <summary>
        /// 是否存在管理员级用户（任一角色 Level ≥ Administrator）。
        /// </summary>
        private bool HasAnyAdminLevelUser()
        {
            var roleLevels = roleRepo.GetAll().ToDictionary(r => r.Id, r => r.Level);
            foreach (var ur in userRoleRepo.GetAll())
            {
                if (roleLevels.TryGetValue(ur.RoleId, out int level) && level >= (int)RoleType.Administrator)
                    return true;
            }
            return false;
        }

        #endregion
    }

}
