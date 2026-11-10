using System;
using Taoyl.Application.AuthService.Models.Dtos;
using Taoyl.Application.Share.Auth;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 认证服务接口（RBAC改造版）
    /// </summary>
    public interface IAuthService
    {
        event Action<string> CardLoginAttempted;

        /// <summary>
        /// 认证事实已发生（登录成功 / 登录失败 / 注销）。
        ///
        /// 登录域只负责广播，不关心有无消费方 —— 无人订阅时为空条件调用，安全无副作用。
        /// 消费方**必须**使用事件携带的身份快照，绝不可回头读 IUserSession：
        /// 登出事件发出后会话随即被清空，回读只会得到空或下一位用户。
        /// </summary>
        event Action<AuthFact> AuthFactRecorded;

        (bool success, string message) Login(string username, string password);
        (bool success, string message, UserDto user) LoginWithCard(string cardCode);
        void Logout();

        /// <summary>
        /// 检查当前用户是否拥有指定权限码
        /// 示例：HasPermission("Btn:Process_Start") / HasPermission("Page:ProductLive")
        /// </summary>
        bool HasPermission(string permissionCode);
    }

}
