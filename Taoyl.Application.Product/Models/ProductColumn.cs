using System;

namespace Taoyl.Application.Product.Models
{
    /// <summary>
    /// 一列的展示元数据（反射 DTO 一次缓存，见 ProductSchema）。
    /// Name 即 DataPropertyName（来自属性名，不会写错），Header 即中文列头，Type 决定
    /// 列格式（时间列 yyyy-MM-dd HH:mm:ss、数值列右对齐）与该列可用操作符（string 列才有 Like）。
    /// 只有三个字段——没有范围、没有配方、没有取值委托，页面要什么就是什么。
    /// </summary>
    public sealed class ProductColumn
    {
        /// <summary>属性名 = DataTable 列名 = DataPropertyName。</summary>
        public string Name { get; set; }

        /// <summary>中文列头（DTO [Description]，缺省退回属性名）。</summary>
        public string Header { get; set; }

        /// <summary>展示类型（可空包装已拆为底层类型，如 DateTime? → DateTime）。</summary>
        public Type Type { get; set; }
    }
}
