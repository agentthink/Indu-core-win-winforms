using System;
using System.Collections.Generic;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.AuthService.Repositories
{
    /// <summary>
    /// 数据库角色权限关联仓储实现
    /// </summary>
    public class DbRolePermissionRepository : IRolePermissionRepository
    {
        private readonly SqlSugarHelper dbHelper;

        public DbRolePermissionRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public List<RolePermission> GetAll()
        {
            return dbHelper.Db.Queryable<RolePermission>().ToList();
        }

        public List<RolePermission> GetByRoleId(int roleId)
        {
            return dbHelper.Db.Queryable<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ToList();
        }

        public List<RolePermission> GetByPermissionId(int permissionId)
        {
            return dbHelper.Db.Queryable<RolePermission>()
                .Where(rp => rp.PermissionId == permissionId)
                .ToList();
        }

        public bool Exists(int roleId, int permissionId)
        {
            return dbHelper.Db.Queryable<RolePermission>()
                .Any(rp => rp.RoleId == roleId && rp.PermissionId == permissionId);
        }

        public void Add(RolePermission rolePermission)
        {
            rolePermission.Id = dbHelper.Db.Insertable(rolePermission).ExecuteReturnIdentity();
        }

        public void Delete(int id)
        {
            dbHelper.Db.Deleteable<RolePermission>()
                .Where(rp => rp.Id == id)
                .ExecuteCommand();
        }

        public void DeleteByRoleId(int roleId)
        {
            dbHelper.Db.Deleteable<RolePermission>()
                .Where(rp => rp.RoleId == roleId)
                .ExecuteCommand();
        }

    }

}
