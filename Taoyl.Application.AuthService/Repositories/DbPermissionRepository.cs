using System;
using System.Collections.Generic;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.AuthService.Repositories
{
    /// <summary>
    /// 数据库权限仓储实现
    /// </summary>
    public class DbPermissionRepository : IPermissionRepository
    {
        private readonly SqlSugarHelper dbHelper;

        public DbPermissionRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public List<Permission> GetAll()
        {
            return dbHelper.Db.Queryable<Permission>().ToList();
        }

        public Permission GetById(int id)
        {
            return dbHelper.Db.Queryable<Permission>().InSingle(id);
        }

        public Permission GetByCode(string code)
        {
            return dbHelper.Db.Queryable<Permission>()
                .First(p => p.Code == code);
        }

        public List<Permission> GetByType(int type)
        {
            return dbHelper.Db.Queryable<Permission>()
                .Where(p => p.Type == type)
                .ToList();
        }

        public List<Permission> GetActiveMenus()
        {
            return dbHelper.Db.Queryable<Permission>()
                .Where(p => p.Type == 1 && p.IsActive == 1)
                .ToList();
        }

        public List<Permission> GetActiveFeatures()
        {
            return dbHelper.Db.Queryable<Permission>()
                .Where(p => p.Type == 2 && p.IsActive == 1)
                .ToList();
        }

        public void Add(Permission permission)
        {
            permission.Id = dbHelper.Db.Insertable(permission).ExecuteReturnIdentity();
        }

        public void Update(Permission permission)
        {
            dbHelper.Db.Updateable(permission).ExecuteCommand();
        }

        public void Delete(int id)
        {
            // 逻辑删除：标记为禁用
            dbHelper.Db.Updateable<Permission>()
                .SetColumns(p => new Permission { IsActive = 0 })
                .Where(p => p.Id == id)
                .ExecuteCommand();
        }

    }

}
