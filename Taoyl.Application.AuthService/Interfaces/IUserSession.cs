using System;
using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Dtos;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>用户会话接口，提供当前登录用户信息和权限状态</summary>
    public interface IUserSession
    {
        /// <summary>
        /// 当前登录用户（未登录时返回访客）
        /// </summary>
        UserDto CurrentUser { get; }
        /// <summary>
        /// 是否已登录
        /// </summary>
        bool IsLoggedIn { get; }
        /// <summary>
        /// 当前用户权限等级（最高角色等级）
        /// </summary>
        int CurrentLevel { get; }
        /// <summary>
        /// 当前用户拥有的权限码集合
        /// </summary>
        HashSet<string> CurrentPermissionCodes { get; }
        /// <summary>
        /// 用户会话变更事件（登录、登出、自身信息修改时触发）
        /// </summary>
        event Action<UserDto> UserChanged;
        /// <summary>
        /// 设置当前登录用户（登录成功时调用，携带权限码）
        /// </summary>
        void SetCurrentUser(UserDto user, HashSet<string> permissionCodes);
        /// <summary>
        /// 设置当前登录用户（兼容旧调用，无权限码）
        /// </summary>
        void SetCurrentUser(UserDto user);
        /// <summary>
        /// 清除当前会话（登出时调用）
        /// </summary>
        void ClearCurrentUser();
        /// <summary>
        /// 重新加载当前登录用户的完整信息并触发 UserChanged（统一刷新入口）
        /// </summary>
        void ReloadCurrentUser();
    }

}
