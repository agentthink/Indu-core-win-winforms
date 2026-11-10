using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Taoyl.Application.Product.Interfaces
{
    /// <summary>
    /// 产品表泛型仓储：纯数据访问，所有产品采集表共用。
    /// 🔴 本接口没有主键概念（不回填自增 ID、不按主键查删）、不认识 UI 查询语言、不做排序兜底——
    /// 筛选条件由服务层翻译成表达式传入；排序只收字段名，由仓储内部转表达式执行（字段名是 SQL 词汇，不是 UI 契约）。
    /// 新增产品时，Autofac 注册具体仓储即可，不需要再写通用 CRUD。
    /// </summary>
    public interface IProductRepository<TEntity> where TEntity : class, new()
    {
        /// <summary>插入一条记录，返回受影响行数。</summary>
        int Insert(TEntity entity);

        /// <summary>按实体主键更新（主键识别由 SqlSugar 依 [SugarColumn] 完成），返回受影响行数。</summary>
        int Update(TEntity entity);

        /// <summary>按表达式条件删除，返回受影响行数。</summary>
        int DeleteWhere(Expression<Func<TEntity, bool>> predicate);

        /// <summary>按表达式条件查单条；无匹配返回 null。</summary>
        TEntity QueryFirst(Expression<Func<TEntity, bool>> predicate);

        /// <summary>按表达式条件查列表（predicate null = 全表）。</summary>
        List<TEntity> QueryList(Expression<Func<TEntity, bool>> predicate);

        /// <summary>取前 take 行（上限收口 MaxLiveTake）；orderField 空 = 不加排序。</summary>
        List<TEntity> QueryTop(Expression<Func<TEntity, bool>> predicate,
                               string orderField,
                               bool descending,
                               int take);

        /// <summary>服务端分页（Count 与取页独立构建）；orderField 空 = 不加排序；total = 命中总数。</summary>
        List<TEntity> QueryPage(Expression<Func<TEntity, bool>> predicate,
                                string orderField,
                                bool descending,
                                int pageIndex,
                                int pageSize,
                                out int total);
    }
}
