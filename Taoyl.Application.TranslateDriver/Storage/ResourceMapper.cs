using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Reflection;
using Taoyl.Application.TranslateDriver.Core;

namespace Taoyl.Application.TranslateDriver.Storage
{
    /// <summary>
    /// 资源条目与 DataTable 的双向映射（网格编辑 / Excel 导入导出的交换格式）。
    /// 列名规则：固定属性列使用 DisplayName（中文），语言列使用语言代码（如 zh-CN）。
    /// 仅依赖 System.Data（net472 内置），不依赖任何 UI 类型，故下沉至应用层。
    /// </summary>
    public static class ResourceMapper
    {
        // 缓存属性信息（排除 Translations 字典）
        private static readonly PropertyInfo[] Props = typeof(ResourceEntry)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.Name != nameof(ResourceEntry.Translations))
            .ToArray();

        // 预计算 DisplayName -> PropertyInfo 映射（包括无 DisplayName 时使用属性名）
        private static readonly Dictionary<string, PropertyInfo> DisplayNameToProp;

        static ResourceMapper()
        {
            DisplayNameToProp = new Dictionary<string, PropertyInfo>();
            foreach (var prop in Props)
            {
                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name;
                DisplayNameToProp[displayName] = prop;
            }
        }

        /// <summary>
        /// 将 DataTable 转换为 List&lt;ResourceEntry&gt;
        /// 要求 DataTable 的列名与 ResourceEntry 的 DisplayName 一致（或属性名）
        /// </summary>
        public static List<ResourceEntry> ToList(DataTable dt)
        {
            var list = new List<ResourceEntry>();

            foreach (DataRow row in dt.Rows)
            {
                var entry = new ResourceEntry();
                foreach (DataColumn col in dt.Columns)
                {
                    var val = row[col];

                    // 根据列名查找对应的属性（通过 DisplayName 映射）
                    if (DisplayNameToProp.TryGetValue(col.ColumnName, out var prop))
                    {
                        // 固定属性列空值跳过（保留默认值）
                        if (val == DBNull.Value) continue;
                        if (prop.PropertyType.IsEnum)
                            prop.SetValue(entry, Enum.Parse(prop.PropertyType, val.ToString()));
                        else
                            prop.SetValue(entry, Convert.ChangeType(val, prop.PropertyType));
                    }
                    else
                    {
                        // 非固定属性列，作为翻译字典的语言列。
                        // Excel 空单元格（null/DBNull）写入空串：用户在 Excel 清空某语言列 = 删除该翻译
                        //（合并时覆盖旧值；列不存在则不处理，外部 Excel 只含部分语言列时不会误清其他语言）
                        entry.Translations[col.ColumnName] =
                            val == null || val == DBNull.Value ? "" : val.ToString();
                    }
                }
                list.Add(entry);
            }
            return list;
        }

        /// <summary>
        /// 将 List&lt;ResourceEntry&gt; 转换为 DataTable
        /// 固定列名使用 DisplayName（中文），语言列名使用语言代码（如 zh-CN, en-US）
        /// </summary>
        public static DataTable ToDataTable(List<ResourceEntry> list)
        {
            DataTable dt = new DataTable();

            // 1. 固定属性列：列名 = DisplayName，数据类型根据属性类型决定（枚举转为 string）
            foreach (var prop in Props)
            {
                var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name;
                var columnType = prop.PropertyType == typeof(ResourceType) ? typeof(string) : prop.PropertyType;
                dt.Columns.Add(displayName, columnType);
            }

            // 2. 动态语言列：从 LanguageType 枚举生成，列名 = 语言代码（如 zh-CN）
            var allLangCodes = Enum.GetNames(typeof(LanguageType))
                                   .Select(l => l.Replace('_', '-'))
                                   .ToList();
            foreach (var lang in allLangCodes)
            {
                dt.Columns.Add(lang, typeof(string));
            }

            // 3. 填充数据行
            foreach (var item in list)
            {
                DataRow row = dt.NewRow();

                // 填充固定属性
                foreach (var prop in Props)
                {
                    var displayName = prop.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName ?? prop.Name;
                    var val = prop.GetValue(item);
                    row[displayName] = val ?? DBNull.Value;
                }

                // 填充语言列
                foreach (var lang in allLangCodes)
                {
                    // item.Translations 的键就是语言代码（如 zh-CN），直接匹配
                    item.Translations.TryGetValue(lang, out var text);
                    row[lang] = text ?? string.Empty;
                }

                dt.Rows.Add(row);
            }

            return dt;
        }
    }
}
