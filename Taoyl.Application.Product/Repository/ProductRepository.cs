using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using SqlSugar;
using Taoyl.Application.DataBase;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Models;
using Taoyl.Application.Product.Query;

namespace Taoyl.Application.Product.Repository
{
    /// <summary>
    /// 产品表泛型仓储实现（SqlSugar）。所有产品采集表共用：CRUD + 分页执行。
    /// 零反射：不解析主键、不回填自增 ID、不做排序兜底——这些责任在服务层与 SqlSugar 特性。
    /// 排序只收字段名，此处用 ProductExpressionBuilder 转成强类型表达式（不拼 SQL 字符串，避免注入）。
    /// 线程安全：本类无实例可变状态（只持只读 SqlSugarHelper），SqlSugarScope 自身线程安全，
    /// 多流程子类并发调用安全。
    /// </summary>
    public class ProductRepository<TEntity> : IProductRepository<TEntity> where TEntity : class, new()
    {
        private readonly SqlSugarHelper dbHelper;

        public ProductRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        #region 写路径

        public int Insert(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return dbHelper.Db.Insertable(entity).ExecuteCommand();
        }

        public int Update(TEntity entity)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            return dbHelper.Db.Updateable(entity).ExecuteCommand();
        }

        public int DeleteWhere(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return dbHelper.Db.Deleteable<TEntity>().Where(predicate).ExecuteCommand();
        }

        #endregion

        #region 读路径

        public TEntity QueryFirst(Expression<Func<TEntity, bool>> predicate)
        {
            if (predicate == null) throw new ArgumentNullException(nameof(predicate));
            return dbHelper.Db.Queryable<TEntity>().Where(predicate).First();
        }

        public List<TEntity> QueryList(Expression<Func<TEntity, bool>> predicate)
        {
            return BuildQuery(predicate).ToList();
        }

        public List<TEntity> QueryTop(Expression<Func<TEntity, bool>> predicate,
                                      string orderField,
                                      bool descending,
                                      int take)
        {
            var clamped = take < 1
                ? ProductDataLimits.DefaultLiveTake
                : Math.Min(take, ProductDataLimits.MaxLiveTake);

            return ApplyOrder(BuildQuery(predicate), orderField, descending)
                .Take(clamped)
                .ToList();
        }

        public List<TEntity> QueryPage(Expression<Func<TEntity, bool>> predicate,
                                       string orderField,
                                       bool descending,
                                       int pageIndex,
                                       int pageSize,
                                       out int total)
        {
            pageIndex = pageIndex < 1 ? 1 : pageIndex;
            pageSize = pageSize < 1
                ? ProductDataLimits.DefaultQueryPageSize
                : Math.Min(pageSize, ProductDataLimits.MaxQueryTake);

            // Count 与取页各自独立构建查询（不共用 ISugarQueryable 实例，照审计先例）
            total = BuildQuery(predicate).Count();

            return ApplyOrder(BuildQuery(predicate), orderField, descending)
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();
        }

        #endregion

        #region 私有辅助

        private ISugarQueryable<TEntity> BuildQuery(Expression<Func<TEntity, bool>> predicate)
        {
            var query = dbHelper.Db.Queryable<TEntity>();
            return predicate == null ? query : query.Where(predicate);
        }

        /// <summary>字段名 → 强类型排序表达式（不走 SqlSugar 的字符串 OrderBy 重载，避免拼 SQL）。</summary>
        private static ISugarQueryable<TEntity> ApplyOrder(ISugarQueryable<TEntity> query,
                                                           string orderField,
                                                           bool descending)
        {
            if (string.IsNullOrEmpty(orderField)) return query;

            return query.OrderBy(
                ProductExpressionBuilder.BuildOrder<TEntity>(orderField, false),
                descending ? OrderByType.Desc : OrderByType.Asc);
        }

        #endregion
    }
}
