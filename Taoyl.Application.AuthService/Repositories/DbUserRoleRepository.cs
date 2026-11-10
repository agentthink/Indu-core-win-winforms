using System;
using System.Collections.Generic;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.AuthService.Repositories
{
    /// <summary>
    /// 数据库用户角色关联仓储实现
    /// </summary>
    public class DbUserRoleRepository : IUserRoleRepository
    {
        private readonly SqlSugarHelper dbHelper;

        public DbUserRoleRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public List<UserRole> GetAll()
        {
            return dbHelper.Db.Queryable<UserRole>().ToList();
        }

        public List<UserRole> GetByUserId(int userId)
        {
            return dbHelper.Db.Queryable<UserRole>()
                .Where(ur => ur.UserId == userId)
                .ToList();
        }

        public List<UserRole> GetByRoleId(int roleId)
        {
            return dbHelper.Db.Queryable<UserRole>()
                .Where(ur => ur.RoleId == roleId)
                .ToList();
        }

        public void Add(UserRole userRole)
        {
            userRole.Id = dbHelper.Db.Insertable(userRole).ExecuteReturnIdentity();
        }

        public void Delete(int id)
        {
            dbHelper.Db.Deleteable<UserRole>()
                .Where(ur => ur.Id == id)
                .ExecuteCommand();
        }

        public void DeleteByUserId(int userId)
        {
            dbHelper.Db.Deleteable<UserRole>()
                .Where(ur => ur.UserId == userId)
                .ExecuteCommand();
        }

    }

}
