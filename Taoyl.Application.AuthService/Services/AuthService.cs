using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.Share;
using Taoyl.Application.Share.Auth;

namespace Taoyl.Application.AuthService.Services
{
    /// <summary>
    /// 认证服务 — 无状态，只负责登录/登出/刷卡/事件
    /// 权限查询完全委托给 IPermissionService
    ///
    /// 认证事实广播（2026-09-19 解耦）：登录域是**唯一**需要服务层留痕的域，原因有二：
    /// ①刷卡登录由键盘钩子直达本类，UI 层结构上不可观测（CardLoginAttempted 零订阅）；
    /// ②登录成功须在 SetCurrentUser 之后取身份、登出须在 ClearCurrentUser 之前取身份，
    ///   这两个时机只有本类知道。
    /// 本类通过 AuthFactRecorded 事件把身份**快照**广播出去，自身不持有任何审计类型
    /// （不再 new AuditEntry、不再依赖 IAuditService），消费方在 UI 层接线。
    /// 其余域（配置/配方/设备/用户 CRUD）的留痕落点一律在 UI 命令层，与本类无关。
    /// </summary>
    public class AuthService : IAuthService, IDisposable
    {
        private readonly IUserSession userSession;
        private readonly IUserManager userManager;
        private readonly IPermissionService permissionService;
        private readonly ICardLoginProvider cardLoginProvider;
        private bool disposed;


        public event Action<string> CardLoginAttempted;

        /// <summary>认证事实已发生。无人订阅时为空条件调用，安全无副作用</summary>
        public event Action<AuthFact> AuthFactRecorded;

        public AuthService(IUserSession userSession, IUserManager userManager,
            IPermissionService permissionService,
            ICardLoginProvider cardLoginProvider)
        {
            this.userSession = userSession ?? throw new ArgumentNullException(nameof(userSession));
            this.userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            this.permissionService = permissionService ?? throw new ArgumentNullException(nameof(permissionService));
            this.cardLoginProvider = cardLoginProvider ?? throw new ArgumentNullException(nameof(cardLoginProvider));

            cardLoginProvider.CardRead += OnCardRead;
            cardLoginProvider.Start();
        }

        #region 刷卡登录

        private void OnCardRead(string cardCode)
        {
            CardLoginAttempted?.Invoke(cardCode);
            if(cardLoginProvider==null||!cardLoginProvider.IsEnabled) return;
            PerformCardLogin(cardCode);
        }

        private void PerformCardLogin(string cardCode)
        {
            var result = LoginWithCard(cardCode);
            RecordCardLoginLog(cardCode, result.user?.Username, result.success, result.success ? null : result.message);
        }

        #endregion

        #region 用户认证

        public (bool success, string message) Login(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username))
                return (false, "请输入用户名");
            if (string.IsNullOrWhiteSpace(password))
                return (false, "请输入密码");

            var user = userManager.GetByUsername(username);
            if (user == null || user.Password != password)
            {
                RecordLoginFailed(username);
                return (false, "用户名或密码错误");
            }

            // 角色已由 UserService.ToDto 装配；此处只取权限码
            var permissionCodes = permissionService.GetUserPermissionCodes(user.UserId);

            // 登录只更新时间字段，不走用户编辑校验，也不覆盖其它列
            if (userManager.TouchLastLogin(user.UserId, out string touchError))
            {
                user.LastLoginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                userSession.SetCurrentUser(user, permissionCodes);
                RecordCardLoginLog("手动登录", username, true, null);
                RecordLoginSuccess(user);
                return (true, "登录成功");
            }
            else
            {
                RecordLoginFailed(username);
                return (false, $"登录状态更新失败: {touchError}");
            }
        }

        public (bool success, string message, UserDto user) LoginWithCard(string cardCode)
        {
            if (string.IsNullOrWhiteSpace(cardCode))
                return (false, "卡号无效", null);

            var user = userManager.GetByCardCode(cardCode);
            if (user == null)
            {
                RecordLoginFailed(null);
                return (false, "卡号无效或未绑定用户", null);
            }

            // 角色已由 UserService.ToDto 装配；此处只取权限码
            var permissionCodes = permissionService.GetUserPermissionCodes(user.UserId);

            if (userManager.TouchLastLogin(user.UserId, out string touchError))
            {
                user.LastLoginTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                userSession.SetCurrentUser(user, permissionCodes);
                RecordLoginSuccess(user);
                return (true, "刷卡登录成功", user);
            }
            else
            {
                RecordLoginFailed(user.Username);
                return (false, $"登录状态更新失败: {touchError}", null);
            }
        }

        public void Logout()
        {
            // 手动登出与超时登出一视同仁——都是会话结束，不区分 Action
            RecordLogout();
            RecordCardLoginLog("注销", userSession.CurrentUser?.Username, true, null);
            userSession.ClearCurrentUser();
        }

        public bool HasPermission(string permissionCode)
        {
            // 未登录时 CurrentUser 为 Guest(UserId=0)，PermissionService 会返回访客权限
            return permissionService.HasPermission(
                (int)(userSession.CurrentUser?.UserId ?? 0),
                permissionCode);
        }

        #endregion

        #region 认证事实广播

        /// <summary>
        /// 广播登录成功。
        /// 调用时机：必须在 SetCurrentUser 之后 —— 此时 user 已带完整角色快照，且与生效会话一致。
        /// </summary>
        private void RecordLoginSuccess(UserDto user)
        {
            AuthFactRecorded?.Invoke(new AuthFact
            {
                Kind = AuthFactKind.LoginSucceeded,
                Username = user.Username,
                RoleNames = RoleNamesOf(user),
                MaxRoleLevel = user.MaxRoleLevel,
            });
        }

        /// <summary>
        /// 广播登录失败。
        ///
        /// 身份须显式构造"本次尝试者"快照：登录失败时会话未建立（或仍是上一位在线用户），
        /// 若借用会话身份，会把上一位用户的角色按到本次尝试头上 —— 那是假账。
        /// 角色 / 等级留空是诚实的：身份确实未经验证。
        /// </summary>
        private void RecordLoginFailed(string username)
        {
            AuthFactRecorded?.Invoke(new AuthFact
            {
                Kind = AuthFactKind.LoginFailed,
                Username = string.IsNullOrWhiteSpace(username) ? "未知用户" : username,
                RoleNames = new List<string>(),
                MaxRoleLevel = 0,
            });
        }

        /// <summary>
        /// 广播注销。
        ///
        /// 手动登出与超时登出共用本方法（不区分类型 —— 都是会话结束）。
        /// 调用时机：必须在 ClearCurrentUser 之前 —— 身份快照须在会话清空前采集。
        /// </summary>
        private void RecordLogout()
        {
            var current = userSession.CurrentUser;
            if (current == null || current.MaxRoleLevel <= 0) return; // 未登录状态的注销不产生事实

            AuthFactRecorded?.Invoke(new AuthFact
            {
                Kind = AuthFactKind.Logout,
                Username = current.Username,
                RoleNames = RoleNamesOf(current),
                MaxRoleLevel = current.MaxRoleLevel,
            });
        }

        /// <summary>角色名快照（新建集合，不与 UserDto 共享实例 —— 防事后就地改写追溯失真）</summary>
        private static List<string> RoleNamesOf(UserDto user)
        {
            return user?.Roles == null
                ? new List<string>()
                : user.Roles.Where(r => r != null).Select(r => r.RoleName).ToList();
        }

        #endregion

        #region 日志

        private void RecordCardLoginLog(string cardCode, string username, bool success, string errorMessage)
        {
            try
            {
                string dateStr = DateTime.Now.ToString("yyyyMMdd");
                string timeStr = DateTime.Now.ToString("HH:mm:ss");
                string logDir = "刷卡日志";
                if (!Directory.Exists(logDir)) Directory.CreateDirectory(logDir);

                string logContent = $"{dateStr}{timeStr} {(success ? "成功" : "失败")} " +
                    $"卡号:{cardCode} 用户:{username ?? "未找到"} " +
                    $"{(errorMessage != null ? $"错误:{errorMessage}" : "")}\n";

                File.AppendAllText(Path.Combine(logDir, $"{dateStr}.txt"), logContent);
            }
            catch { }
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (disposed) return;
            cardLoginProvider?.Stop();
            disposed = true;
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}