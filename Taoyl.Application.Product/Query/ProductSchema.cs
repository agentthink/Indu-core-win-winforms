using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using Taoyl.Application.Product.Models;

namespace Taoyl.Application.Product.Query
{
    /// <summary>
    /// 列元数据反射（全库唯一反射点）+ 导出投影。
    /// 只读 DTO 的 [Description]，零业务依赖、零特性发明；列序 = DTO 声明顺序 = 实体列序。
    /// 主路径（页面绑 IList）不经过 ToDataTable——DataTable 只在点导出时出现一次（MiniExcel 认 DataTable，列头可直接写中文）。
    /// </summary>
    public static class ProductSchema
    {
        /// <summary>反射一次取全：列元数据 + 对应 PropertyInfo（导出投影复用，全库唯一反射点名副其实）。</summary>
        private sealed class Entry
        {
            public ProductColumn[] Columns;
            public PropertyInfo[] Properties;
        }

        private static readonly ConcurrentDictionary<Type, Entry> cache
            = new ConcurrentDictionary<Type, Entry>();

        /// <summary>取某 DTO 的列元数据（顺序 = 声明顺序）。返回副本——缓存数组是全局共享态，调用方改脏会污染所有页面。</summary>
        public static ProductColumn[] Of<TDto>()
        {
            return (ProductColumn[])cache.GetOrAdd(typeof(TDto), Build).Columns.Clone();
        }

        private static Entry Build(Type dtoType)
        {
            var props = dtoType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            var columns = props
                .Select(p => new ProductColumn
                {
                    Name = p.Name,
                    Header = HeaderOf(p),
                    Type = Nullable.GetUnderlyingType(p.PropertyType) ?? p.PropertyType,
                })
                .ToArray();
            return new Entry { Columns = columns, Properties = props };
        }

        /// <summary>列头：[Description] 优先，缺省退回属性名。</summary>
        private static string HeaderOf(PropertyInfo property)
        {
            var attr = property.GetCustomAttribute<DescriptionAttribute>();
            return attr != null && !string.IsNullOrWhiteSpace(attr.Description)
                ? attr.Description
                : property.Name;
        }

        /// <summary>
        /// 导出投影：IList（元素为该表 DTO）→ DataTable（列头 = 中文）。
        /// 低频（点导出一次）；空结果返回只有表头的空表（调用方决定空结果提示）。
        /// </summary>
        public static DataTable ToDataTable(IList rows, IReadOnlyList<ProductColumn> columns)
        {
            var table = new DataTable();
            foreach (var column in columns)
            {
                table.Columns.Add(column.Header, column.Type);
            }

            if (rows == null || rows.Count == 0)
            {
                return table;
            }

            var props = cache.GetOrAdd(rows[0].GetType(), Build).Properties
                .ToDictionary(p => p.Name);
            foreach (var row in rows)
            {
                var values = new object[columns.Count];
                for (int i = 0; i < columns.Count; i++)
                {
                    var value = props[columns[i].Name].GetValue(row);
                    values[i] = value ?? DBNull.Value;
                }
                table.Rows.Add(values);
            }
            return table;
        }
    }
}
