using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Taoyl.Application.Product.Models;

namespace Taoyl.Application.Product.Query
{
    /// <summary>
    /// 字段名 → 表达式树（筛选 + 排序）。运行期字段名的唯一落点：
    /// 树全部经 Expression API 构建，不含任何字符串 SQL 片段（禁手拼红线，方言由 ORM 内部处理）。
    /// 字符串列的 &lt; &lt;= &gt; &gt;= 经 CompareTo 落树（C# 表达式树里 string 不存在 &lt; 运算符）；
    /// SqlSugar 将其翻译为 (case when A&gt;B then 1 when A=B then 0 else -1 end) 再与 0 比较，
    /// 等价 A&gt;B 字典序（5.1.4.207 dll 字符串表 + 上游 DefaultDbMethod.CompareTo 双重实证）。
    /// 查询失败必须上抛让 UI 看见（照报警读侧规则），故参数错误一律抛异常而非静默吞掉。
    /// </summary>
    public static class ProductExpressionBuilder
    {
        /// <summary>
        /// 组装筛选表达式：单条件（字段+操作符+关键字）AndAlso 时间范围。
        /// 两者皆空返回 null（不加任何 Where）。
        /// </summary>
        public static Expression<Func<T, bool>> BuildFilter<T>(ProductQuerySpec spec, string timeField)
        {
            if (spec == null)
            {
                return null;
            }

            var param = Expression.Parameter(typeof(T), "r");
            var single = BuildSingleBody<T>(spec, param);
            var time = BuildTimeRangeBody<T>(spec, timeField, param);

            Expression body;
            if (single != null && time != null)
            {
                body = Expression.AndAlso(single, time);
            }
            else
            {
                body = single ?? time;
            }

            return body == null ? null : Expression.Lambda<Func<T, bool>>(body, param);
        }

        /// <summary>排序表达式（属性名 → Func&lt;T,object&gt;，SqlSugar 会剥掉 Convert 节点）。</summary>
        public static Expression<Func<T, object>> BuildOrder<T>(string sortField, bool descending)
        {
            var property = RequireProperty<T>(sortField);
            var param = Expression.Parameter(typeof(T), "r");
            var body = Expression.Convert(Expression.Property(param, property), typeof(object));
            return Expression.Lambda<Func<T, object>>(body, param);
        }

        /// <summary>单条件正文；字段/操作符/关键字任一为空 = 不筛。</summary>
        private static Expression BuildSingleBody<T>(ProductQuerySpec spec, ParameterExpression param)
        {
            if (string.IsNullOrEmpty(spec.FieldName) || spec.Operator == null || string.IsNullOrWhiteSpace(spec.Keyword))
            {
                return null;
            }

            var property = RequireProperty<T>(spec.FieldName);
            var valueType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (valueType != typeof(string) && spec.Operator.Value == QueryOperator.Like)
            {
                throw new NotSupportedException("Like 仅支持字符串列: " + spec.FieldName);
            }

            var left = Expression.Property(param, property);
            var keyword = Expression.Constant(ConvertKeyword(valueType, spec.Keyword), property.PropertyType);

            switch (spec.Operator.Value)
            {
                case QueryOperator.Like:
                    return Expression.Call(left, "Contains", null, keyword);   // → LIKE '%kw%'
                case QueryOperator.Equal:
                    return Expression.Equal(left, keyword);
                case QueryOperator.NotEqual:
                    return Expression.NotEqual(left, keyword);
                case QueryOperator.LessThan:
                    return BuildComparison(left, keyword, valueType, ExpressionType.LessThan);
                case QueryOperator.LessThanOrEqual:
                    return BuildComparison(left, keyword, valueType, ExpressionType.LessThanOrEqual);
                case QueryOperator.GreaterThan:
                    return BuildComparison(left, keyword, valueType, ExpressionType.GreaterThan);
                case QueryOperator.GreaterThanOrEqual:
                    return BuildComparison(left, keyword, valueType, ExpressionType.GreaterThanOrEqual);
                default:
                    throw new NotSupportedException("未知操作符: " + spec.Operator.Value);
            }
        }

        /// <summary>
        /// 比较四兄弟：数值/日期直接落运算符节点（真数值/时间语义）；
        /// 字符串换成 CompareTo 与 0 比较（字典序语义，设计决策接受）——两路只差「比较左/右」两操作数。
        /// </summary>
        private static Expression BuildComparison(Expression left, Expression keyword, Type valueType, ExpressionType op)
        {
            if (valueType == typeof(string))
            {
                left = Expression.Call(left, "CompareTo", null, keyword);
                keyword = Expression.Constant(0);
            }
            return Expression.MakeBinary(op, left, keyword);
        }

        /// <summary>时间范围正文（作用于 TimeField 列，含端点）；无时间列或两端皆空 = 不筛。</summary>
        private static Expression BuildTimeRangeBody<T>(ProductQuerySpec spec, string timeField, ParameterExpression param)
        {
            if (string.IsNullOrEmpty(timeField) || (!spec.TimeFrom.HasValue && !spec.TimeTo.HasValue))
            {
                return null;
            }

            var property = RequireProperty<T>(timeField);
            var left = Expression.Property(param, property);
            var constantType = property.PropertyType;

            Expression body = null;
            if (spec.TimeFrom.HasValue)
            {
                body = Expression.GreaterThanOrEqual(left, Expression.Constant(spec.TimeFrom.Value, constantType));
            }
            if (spec.TimeTo.HasValue)
            {
                var to = Expression.LessThanOrEqual(left, Expression.Constant(spec.TimeTo.Value, constantType));
                body = body == null ? to : Expression.AndAlso(body, to);
            }
            return body;
        }

        private static PropertyInfo RequireProperty<T>(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException(nameof(name), "字段名不能为空");
            }

            var property = typeof(T).GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            if (property == null)
            {
                throw new InvalidOperationException(typeof(T).Name + " 不存在字段: " + name);
            }
            return property;
        }

        /// <summary>关键字 → 字段类型。字符串原样；数值不变式解析；时间先按 yyyy-MM-dd HH:mm:ss 精确解析再退宽松。</summary>
        private static object ConvertKeyword(Type valueType, string keyword)
        {
            if (valueType == typeof(string))
            {
                return keyword;
            }

            var text = keyword.Trim();
            if (valueType == typeof(int)) return int.Parse(text, CultureInfo.InvariantCulture);
            if (valueType == typeof(long)) return long.Parse(text, CultureInfo.InvariantCulture);
            if (valueType == typeof(decimal)) return decimal.Parse(text, CultureInfo.InvariantCulture);
            if (valueType == typeof(DateTime))
            {
                DateTime parsed;
                if (DateTime.TryParseExact(text, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture,
                        DateTimeStyles.None, out parsed))
                {
                    return parsed;
                }
                return DateTime.Parse(text, CultureInfo.InvariantCulture);
            }

            throw new NotSupportedException("不支持的字段类型: " + valueType.Name);
        }
    }
}
