using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.Share.Paths;

namespace Taoyl.Application.AuthService.Services
{
    /// <summary>
    /// RBAC 整机迁移服务：跨机导出 / 导入。
    /// 以 RoleName / Code 为锚点（不依赖自增 ID）；导入前备份失败即中止、异常自动从备份恢复。
    /// 从 UserService 搬出，行为逐字不变。
    /// </summary>
    public class RbacMigrationService : IRbacMigrationService
    {
        private readonly IUserRepository userRepo;
        private readonly IUserRoleRepository userRoleRepo;
        private readonly IRoleRepository roleRepo;
        private readonly IPermissionRepository permissionRepo;
        private readonly IPermissionService permissionService;
        private readonly IUserSession userSession;

        public RbacMigrationService(
            IUserRepository userRepo,
            IUserRoleRepository userRoleRepo,
            IRoleRepository roleRepo,
            IPermissionRepository permissionRepo,
            IPermissionService permissionService,
            IUserSession userSession)
        {
            this.userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            this.userRoleRepo = userRoleRepo ?? throw new ArgumentNullException(nameof(userRoleRepo));
            this.roleRepo = roleRepo ?? throw new ArgumentNullException(nameof(roleRepo));
            this.permissionRepo = permissionRepo ?? throw new ArgumentNullException(nameof(permissionRepo));
            this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
        }

        #region 导入导出（RBAC全量迁移，含Menu指纹校验）

        public bool ExportUsers(string filePath)
        {
            try
            {
                // 计算本机Menu权限指纹
                string menuFingerprint = GetLocalMenuFingerprint();

                var package = new RbacExportPackage
                {
                    ExportTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    MenuVersion = menuFingerprint
                };

                // 1. 导出角色定义 + 手动权限Code
                var allRoles = roleRepo.GetAll();
                foreach (var role in allRoles)
                {
                    var manualPermIds = permissionService.GetRolePermissionIds(role.Id);
                    var permCodes = manualPermIds
                        .Select(id => permissionRepo.GetById(id)?.Code)
                        .Where(c => !string.IsNullOrEmpty(c))
                        .ToList();

                    package.Roles.Add(new RoleExportItem
                    {
                        RoleName = role.RoleName,
                        Level = role.Level,
                        IsBuiltIn = role.IsBuiltIn,
                        Remark = role.Remark,
                        PermissionCodes = permCodes
                    });
                }

                // 2. 导出用户 + 角色名列表
                var allUsers = userRepo.GetAll();
                foreach (var user in allUsers)
                {
                    package.Users.Add(new UserExportItem
                    {
                        Username = user.Username,
                        Password = user.Password,
                        UserNumber = user.UserNumber,
                        CardCode = user.CardCode,
                        AddPeople = user.AddPeople,
                        CreateTime = user.CreateTime,
                        RoleNames = LoadRoleNames(user.UserId)
                    });
                }

                string directory = Path.GetDirectoryName(filePath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                File.WriteAllText(filePath, JsonConvert.SerializeObject(package, Formatting.Indented));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool ImportUsers(string filePath, out string errorMsg)
        {
            errorMsg = null;
            if (!File.Exists(filePath))
            {
                errorMsg = "文件不存在";
                return false;
            }

            // 1. 导入前自动备份（应用级导出，双源兼容）
            string backupDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPathName.AuthBackupDir);
            Directory.CreateDirectory(backupDir);
            string backupPath = Path.Combine(backupDir, $"Rbac_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.json");

            if (!ExportUsers(backupPath))
            {
                errorMsg = "导入前自动备份失败，操作已中止（防止数据丢失）";
                return false;
            }

            try
            {
                var json = File.ReadAllText(filePath);
                var package = JsonConvert.DeserializeObject<RbacExportPackage>(json);
                if (package?.Users == null || !package.Users.Any())
                {
                    errorMsg = "导入文件中无有效用户数据";
                    return false;
                }

                // ===== 强制校验Menu版本（核心门禁） =====
                string localFingerprint = GetLocalMenuFingerprint();
                if (!string.Equals(package.MenuVersion, localFingerprint, StringComparison.OrdinalIgnoreCase))
                {
                    errorMsg = $"导入失败：导出机与本机的菜单配置不一致！\n" +
                               $"导出机菜单指纹：{package.MenuVersion}\n" +
                               $"本机菜单指纹：{localFingerprint}\n\n" +
                               $"请确保两台机软件版本（特别是Menu.json）一致后再导入。";
                    return false;
                }
                // =======================================

                var errors = new List<string>();

                // 2. 清空本地RBAC数据（全量覆盖）
                ClearAllRbacData();

                // 3. 重建角色定义（以RoleName为锚点）
                var roleNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

                foreach (var exportRole in package.Roles ?? new List<RoleExportItem>())
                {
                    if (string.IsNullOrWhiteSpace(exportRole.RoleName)) continue;

                    var localRole = new Role
                    {
                        RoleName = exportRole.RoleName,
                        Level = exportRole.Level,
                        IsBuiltIn = exportRole.IsBuiltIn,
                        Remark = exportRole.Remark
                    };
                    roleRepo.Add(localRole);

                    roleNameToId[exportRole.RoleName] = localRole.Id;

                    var localPermIds = new List<int>();
                    foreach (var code in exportRole.PermissionCodes?.Distinct(StringComparer.OrdinalIgnoreCase) ?? Enumerable.Empty<string>())
                    {
                        var perm = permissionRepo.GetByCode(code);
                        if (perm != null)
                            localPermIds.Add(perm.Id);
                        else
                            errors.Add($"角色[{exportRole.RoleName}]的权限[{code}]在本机不存在，已跳过");
                    }
                    permissionService.SyncManualPermissions(localRole.Id, localPermIds);
                }

                // 4. 重建用户及角色关联
                foreach (var exportUser in package.Users)
                {
                    if (string.IsNullOrWhiteSpace(exportUser.Username))
                    {
                        errors.Add("跳过一条用户名为空的记录");
                        continue;
                    }

                    var localUser = new UserInfo
                    {
                        Username = exportUser.Username,
                        Password = exportUser.Password ?? "123456",
                        UserNumber = exportUser.UserNumber,
                        CardCode = exportUser.CardCode,
                        AddPeople = exportUser.AddPeople ?? userSession.CurrentUser?.Username ?? "系统",
                        CreateTime = exportUser.CreateTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    userRepo.Add(localUser);

                    if (exportUser.RoleNames?.Any() == true)
                    {
                        foreach (var rName in exportUser.RoleNames.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            if (roleNameToId.TryGetValue(rName, out int rid))
                                userRoleRepo.Add(new UserRole { UserId = localUser.UserId, RoleId = rid });
                            else
                                errors.Add($"用户[{exportUser.Username}]的角色[{rName}]未找到，已跳过");
                        }
                    }
                }

                // 5. 强制注销当前会话（当前账号可能已被删或角色已变）
                userSession.ClearCurrentUser();

                if (errors.Any())
                    errorMsg = $"导入完成，存在以下未匹配项：\n{string.Join("\n", errors)}";

                return true;
            }
            catch (Exception ex)
            {
                string restoreError;
                bool restored = TryRestoreFromBackup(backupPath, out restoreError);

                errorMsg = $"导入失败：{ex.Message}。";
                if (restored)
                    errorMsg += " 系统已自动从备份恢复。";
                else
                    errorMsg += $" 自动恢复也失败了：{restoreError}，请手动使用备份文件还原：{backupPath}";

                return false;
            }
        }

        #endregion

        #region 私有辅助

        /// <summary>
        /// 按用户解析角色名列表（导出锚点用名不取 Id）。
        /// </summary>
        private List<string> LoadRoleNames(int userId)
        {
            var names = new List<string>();
            foreach (var ur in userRoleRepo.GetByUserId(userId))
            {
                var role = roleRepo.GetById(ur.RoleId);
                if (role != null) names.Add(role.RoleName);
            }
            return names;
        }

        /// <summary>
        /// 计算Menu.json的权限指纹（只关心Name/Code/Level，DisplayName变了不影响）
        /// 递归排序确保菜单项顺序调整不影响指纹
        /// </summary>
        private string ComputeMenuFingerprint(MenuConfigDto menuConfig)
        {
            if (menuConfig?.MenuItems == null) return "empty";

            var sb = new System.Text.StringBuilder();
            foreach (var item in menuConfig.MenuItems.OrderBy(x => x.Name))
                AppendMenuItemFingerprint(sb, item);
            string fingerprint=string.Empty;
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(sb.ToString());
                fingerprint=BitConverter.ToString(md5.ComputeHash(bytes)).Replace("-", "").Substring(0, 8).ToUpperInvariant();
            }
            return fingerprint;
        }

        private void AppendMenuItemFingerprint(System.Text.StringBuilder sb, MenuItemDto item)
        {
            sb.Append(item.Name).Append(':').Append(item.Level).Append(';');

            if (item.Features != null)
            {
                foreach (var f in item.Features.OrderBy(x => x.Code))
                    sb.Append(f.Code).Append(';');
            }

            if (item.Children != null)
            {
                foreach (var c in item.Children.OrderBy(x => x.Name))
                    AppendMenuItemFingerprint(sb, c);
            }
        }

        /// <summary>
        /// 读取本机Menu.json并计算指纹
        /// </summary>
        private string GetLocalMenuFingerprint()
        {
            try
            {
                string menuPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPathName.MenuFile);
                if (!File.Exists(menuPath)) return "missing";

                var json = File.ReadAllText(menuPath);
                var menuConfig = JsonConvert.DeserializeObject<MenuConfigDto>(json);
                return ComputeMenuFingerprint(menuConfig);
            }
            catch
            {
                return "error";
            }
        }

        /// <summary>
        /// 清空本地所有RBAC数据（用户、角色、关联、手动权限）
        /// </summary>
        private void ClearAllRbacData()
        {
            // 先清空所有角色的手动权限
            var allRoles = roleRepo.GetAll();
            foreach (var r in allRoles)
                permissionService.SyncManualPermissions(r.Id, new List<int>());

            // 清空用户角色关联
            var allUserRoles = userRoleRepo.GetAll();
            foreach (var ur in allUserRoles) userRoleRepo.Delete(ur.Id);

            // 清空用户
            var allUsers = userRepo.GetAll();
            foreach (var u in allUsers) userRepo.Delete(u.UserId);

            // 清空角色
            foreach (var r in allRoles) roleRepo.Delete(r.Id);
        }

        /// <summary>
        /// 从备份包恢复（导入失败时的兜底）
        /// </summary>
        private bool TryRestoreFromBackup(string backupPath, out string errorMsg)
        {
            errorMsg = null;
            try
            {
                if (!File.Exists(backupPath))
                {
                    errorMsg = "备份文件不存在";
                    return false;
                }

                var json = File.ReadAllText(backupPath);
                var package = JsonConvert.DeserializeObject<RbacExportPackage>(json);
                if (package == null) return false;

                // 清残局
                ClearAllRbacData();

                // 重建角色
                var roleNameToId = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (var exportRole in package.Roles ?? new List<RoleExportItem>())
                {
                    if (string.IsNullOrWhiteSpace(exportRole.RoleName)) continue;
                    var localRole = new Role
                    {
                        RoleName = exportRole.RoleName,
                        Level = exportRole.Level,
                        IsBuiltIn = exportRole.IsBuiltIn,
                        Remark = exportRole.Remark
                    };
                    roleRepo.Add(localRole);
                    roleNameToId[exportRole.RoleName] = localRole.Id;

                    var localPermIds = exportRole.PermissionCodes?
                        .Select(c => permissionRepo.GetByCode(c))
                        .Where(p => p != null).Select(p => p.Id).ToList() ?? new List<int>();
                    permissionService.SyncManualPermissions(localRole.Id, localPermIds);
                }

                // 重建用户
                foreach (var exportUser in package.Users ?? new List<UserExportItem>())
                {
                    if (string.IsNullOrWhiteSpace(exportUser.Username)) continue;
                    var localUser = new UserInfo
                    {
                        Username = exportUser.Username,
                        Password = exportUser.Password ?? "123456",
                        UserNumber = exportUser.UserNumber,
                        CardCode = exportUser.CardCode,
                        AddPeople = exportUser.AddPeople ?? "系统",
                        CreateTime = exportUser.CreateTime ?? DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    };
                    userRepo.Add(localUser);

                    if (exportUser.RoleNames?.Any() == true)
                    {
                        foreach (var rName in exportUser.RoleNames.Distinct(StringComparer.OrdinalIgnoreCase))
                        {
                            if (roleNameToId.TryGetValue(rName, out int rid))
                                userRoleRepo.Add(new UserRole { UserId = localUser.UserId, RoleId = rid });
                        }
                        
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                errorMsg = ex.Message;
                return false;
            }
        }

        #endregion
    }

}
