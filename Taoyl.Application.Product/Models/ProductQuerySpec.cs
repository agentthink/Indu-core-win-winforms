using System;

namespace Taoyl.Application.Product.Models
{
    /// <summary>查询操作符。Like 仅字符串列可用（页面按 Column.Type 过滤下拉，仓储侧再防御一道）。</summary>
    public enum QueryOperator
    {
        Like = 0,
        Equal = 1,
        NotEqual = 2,
        LessThan = 3,
        LessThanOrEqual = 4,
        GreaterThan = 5,
        GreaterThanOrEqual = 6,
    }

    /// <summary>
    /// 产品查询条件（单条件：字段 + 操作符 + 关键字；时间范围可勾选，作用于该表 TimeField 列）。
    /// 全字段可空 = 不筛。FieldName 必须是目标 DTO 的属性名（页面下拉取自 Columns，不会拼错）。
    /// </summary>
    public class ProductQuerySpec
    {
        /// <summary>筛选字段（属性名）；null = 无单条件筛选。</summary>
        public string FieldName { get; set; }

        /// <summary>操作符；FieldName 为 null 时忽略。</summary>
        public QueryOperator? Operator { get; set; }

        /// <summary>关键字（原样入库比较，不做通配符转义）；null/空白 = 不筛。</summary>
        public string Keyword { get; set; }

        /// <summary>时间起始（含，作用于 TimeField 列）；null = 不限。</summary>
        public DateTime? TimeFrom { get; set; }

        /// <summary>时间结束（含端点，作用于 TimeField 列）；null = 不限。</summary>
        public DateTime? TimeTo { get; set; }
    }
}
