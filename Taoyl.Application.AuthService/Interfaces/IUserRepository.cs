using System.Collections.Generic;
using Taoyl.Application.AuthService.Models.Entities;

namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 用户数据仓储接口
    /// 负责用户数据的持久化操作，与具体存储介质无关
    /// </summary>
    public interface IUserRepository
    {
        /// <summary>
        /// 获取所有用户
        /// </summary>
        List<UserInfo> GetAll();

        /// <summary>
        /// 根据用户ID获取用户
        /// </summary>
        UserInfo GetById(int userId);

        /// <summary>
        /// 根据用户名获取用户
        /// </summary>
        UserInfo GetByUsername(string username);

        /// <summary>
        /// 根据卡号获取用户
        /// </summary>
        UserInfo GetByCardCode(string cardCode);

        /// <summary>
        /// 检查卡号是否已被绑定
        /// </summary>
        /// <param name="cardCode">卡号（空字符串或null视为未绑定，不参与校验）</param>
        /// <param name="excludeUserId">排除的用户ID（用于编辑时排除自己）</param>
        bool CardCodeExists(string cardCode, int? excludeUserId =null);

        /// <summary>
        /// 新增用户（内部自动生成自增ID）
        /// </summary>
        void Add(UserInfo user);

        /// <summary>
        /// 更新用户信息
        /// </summary>
        void Update(UserInfo user);

        /// <summary>
        /// 窄更新：只更新可编辑资料字段（Username / UserNumber / CardCode），
        /// updatePassword 为 true 时一并更新 Password；不触碰 CreateTime / LastLoginTime / AddPeople。
        /// </summary>
        bool UpdateEditableFields(UserInfo user, bool updatePassword);

        /// <summary>
        /// 窄更新：只更新最后登录时间，并检查影响行数。
        /// </summary>
        bool UpdateLastLoginTime(int userId, string lastLoginTime);

        /// <summary>
        /// 删除用户
        /// </summary>
        void Delete(int userId);

    }

}
