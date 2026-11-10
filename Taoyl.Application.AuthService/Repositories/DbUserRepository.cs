using System;
using System.Collections.Generic;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.AuthService.Repositories
{
    /// <summary>
    /// 数据库用户仓储实现
    /// </summary>
    public class DbUserRepository : IUserRepository
    {
        private readonly SqlSugarHelper dbHelper;

        public DbUserRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public List<UserInfo> GetAll()
        {
            return dbHelper.Db.Queryable<UserInfo>()
                .ToList();
                
        }

        public UserInfo GetById(int userId)
        {
            return dbHelper.Db.Queryable<UserInfo>().InSingle(userId);
        }

        public UserInfo GetByUsername(string username)
        {
            // 注：使用 ToLower() 是为了兼容 PostgreSQL 等大小写敏感的数据库。
            // MySQL 若使用 utf8mb4_general_ci Collation 可不转小写，能走索引。
            return dbHelper.Db.Queryable<UserInfo>()
                .Where(u => u.Username.ToLower() == username.ToLower())
                .First();
        }

        public UserInfo GetByCardCode(string cardCode)
        {
            if (string.IsNullOrWhiteSpace(cardCode)) return null;
            return dbHelper.Db.Queryable<UserInfo>()
                .Where(u => u.CardCode.ToLower() == cardCode.ToLower())
                .First();
        }

        public bool CardCodeExists(string cardCode, int? excludeUserId = null)
        {
            if (string.IsNullOrWhiteSpace(cardCode)) return false;

            var query = dbHelper.Db.Queryable<UserInfo>()
                .Where(u => u.CardCode.ToLower() == cardCode.ToLower());

            if (excludeUserId.HasValue)
                query = query.Where(u => u.UserId != excludeUserId.Value);

            return query.Any();
        }

        public void Add(UserInfo user)
        {
            user.UserId = dbHelper.Db.Insertable(user).ExecuteReturnIdentity();
        }

        public void Update(UserInfo user)
        {
            dbHelper.Db.Updateable(user).ExecuteCommand();
        }

        public bool UpdateEditableFields(UserInfo user, bool updatePassword)
        {
            // 只写可编辑资料列；CreateTime / LastLoginTime / AddPeople 由服务维护，绝不随编辑写回。
            var update = dbHelper.Db.Updateable<UserInfo>()
                .SetColumns(u => new UserInfo
                {
                    Username = user.Username,
                    UserNumber = user.UserNumber,
                    CardCode = user.CardCode
                });

            if (updatePassword)
                update = update.SetColumns(u => new UserInfo { Password = user.Password });

            return update.Where(u => u.UserId == user.UserId).ExecuteCommand() > 0;
        }

        public bool UpdateLastLoginTime(int userId, string lastLoginTime)
        {
            return dbHelper.Db.Updateable<UserInfo>()
                .SetColumns(u => new UserInfo { LastLoginTime = lastLoginTime })
                .Where(u => u.UserId == userId)
                .ExecuteCommand() > 0;
        }

        public void Delete(int userId)
        {
            dbHelper.Db.Deleteable<UserInfo>()
                .Where(u => u.UserId == userId)
                .ExecuteCommand();
        }

        
    }

}
