using System;
using System.Collections.Generic;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.AuthService.Repositories
{
    /// <summary>
    /// 数据库角色仓储实现
    /// </summary>
    public class DbRoleRepository : IRoleRepository
    {
        private readonly SqlSugarHelper dbHelper;

        public DbRoleRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public List<Role> GetAll()
        {
            return dbHelper.Db.Queryable<Role>().ToList();
        }

        public Role GetById(int id)
        {
            return dbHelper.Db.Queryable<Role>().InSingle(id);
        }

        public Role GetByName(string roleName)
        {
            return dbHelper.Db.Queryable<Role>()
                .First(r => r.RoleName.ToLower() == roleName.ToLower());
        }

        public void Add(Role role)
        {
            role.Id = dbHelper.Db.Insertable(role).ExecuteReturnIdentity();
        }

        public void Update(Role role)
        {
            dbHelper.Db.Updateable(role).ExecuteCommand();
        }

        public void Delete(int id)
        {
            dbHelper.Db.Deleteable<Role>()
                .Where(r => r.Id == id)
                .ExecuteCommand();
        }

        public bool DeleteWithRelations(int roleId)
        {
            var db = dbHelper.Db;
            db.Ado.BeginTran();
            try
            {
                db.Deleteable<RolePermission>()
                    .Where(rp => rp.RoleId == roleId)
                    .ExecuteCommand();

                db.Deleteable<UserRole>()
                    .Where(ur => ur.RoleId == roleId)
                    .ExecuteCommand();

                db.Deleteable<Role>()
                    .Where(r => r.Id == roleId)
                    .ExecuteCommand();

                db.Ado.CommitTran();
                return true;
            }
            catch
            {
                db.Ado.RollbackTran();
                return false;
            }
        }

        public bool IsAssignedToUser(int roleId, int userId)
        {
            return dbHelper.Db.Queryable<UserRole>()
                .Any(ur => ur.RoleId == roleId && ur.UserId == userId);
        }

        
    }

}
