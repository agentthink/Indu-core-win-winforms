using System.Collections;
using System.Collections.Generic;
using Taoyl.Application.Product.Models;

namespace Taoyl.Application.Product.Interfaces
{
    /// <summary>
    /// 产品数据源契约：产品实时页 / 产品查询页的唯一依赖。
    /// 每张产品表的服务各自实现本契约，Autofac 以 IEnumerable&lt;IProductDataSource&gt; 集合注入页面 VM
    /// （DeviceManager 同范式：注册多个、一次全给）——没有门面类、没有 slot 注册表。
    /// 页面只见本契约 + ProductColumn 元数据，不见任何具体表名/字段名/实体/DTO 类型。
    /// 🔴 读侧契约：查询失败必须上抛让 UI 看见（照报警读侧规则）。
    /// </summary>
    public interface IProductDataSource
    {
        /// <summary>表名（= 落库表名，如 product_cellinfo），下拉 Value。</summary>
        string Key { get; }

        /// <summary>下拉显示名（如「电芯注液信息」）。</summary>
        string Title { get; }

        /// <summary>时间列属性名（如 EntryTime）；null = 该表无时间维度，页面时间筛选与默认时间排序禁用。</summary>
        string TimeField { get; }

        /// <summary>列元数据（列头/列序/类型）。反射缓存，顺序 = DTO 声明顺序。</summary>
        IReadOnlyList<ProductColumn> Columns { get; }

        /// <summary>
        /// 实时页取最新 N 行（take 由仓储按 ProductDataLimits.MaxLiveTake 收口）。
        /// sortField = 排序属性名；null = 退回该表 TimeField 倒序（三态排序的「取消」态）。
        /// 返回 IList，元素类型 = 该表 DTO（页面 dgv.DataSource 直用）。
        /// </summary>
        IList QueryLatest(int take, string sortField, bool descending);

        /// <summary>
        /// 查询页服务端分页（Count 与取页独立构建，照审计先例；pageSize 仓储收口 MaxQueryTake）。
        /// total = 命中总行数。
        /// </summary>
        IList QueryPage(ProductQuerySpec spec, int pageIndex, int pageSize, out int total);
    }
}
