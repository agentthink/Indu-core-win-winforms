using CsvHelper;
using CsvHelper.Configuration;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Taoyl.Application.FileUtil
{
    /// <summary>
    /// CSV 读写实现。
    ///
    /// 合并说明（10-18）：原 CSVTools 与本类是一层纯转发（6 个方法全是 <c>=&gt; _csvTools.X()</c>），已合并为一个类。
    ///
    /// 修正说明（10-18）：
    /// 1. <see cref="ReadAll{T}"/> 在 reader 存活期内物化——原为惰性返回，枚举时 reader 已释放（必抛）。
    /// 2. 去掉静默吞异常——写失败 / 读失败原样上抛，由调用方决定提示文案（原空 catch 与 return null 都让失败不可见）。
    /// 3. 带 maxRows 的重载改用 CsvHelper 解析——原手写 Split(',')，字段含逗号会列错位。
    /// 4. 锁按文件路径分——原三个 static 锁让所有文件、所有实例串行。
    /// 5. 目标目录为空串时不再 CreateDirectory——原会抛异常并被空 catch 吞掉（相对路径导出 = 静默不写）。
    /// </summary>
    public class CsvService : ICsvService
    {
        /// <summary>按文件路径分锁：同一文件串行、不同文件并行。键为规范化绝对路径。</summary>
        private static readonly ConcurrentDictionary<string, object> FileLocks =
            new ConcurrentDictionary<string, object>(StringComparer.OrdinalIgnoreCase);

        private static object LockFor(string path)
        {
            return FileLocks.GetOrAdd(Path.GetFullPath(path), _ => new object());
        }

        /// <summary>
        /// 写入多个数据（追加写：文件已存在时不写表头）。
        /// 🔴 写失败会抛异常——调用方须处理（不再静默吞掉）。
        /// </summary>
        public void Records<T>(IEnumerable<T> data, string filePath)
        {
            if (data == null)
            {
                return;
            }

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = true // 自动表头生成
            };

            var dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            if (File.Exists(filePath))
            {
                config.HasHeaderRecord = false; // 追加写不重复写表头
            }

            lock (LockFor(filePath))
            {
                // true = 追加；BOM 由 UTF8Encoding(true) 保证（Excel 打开无 BOM 的 UTF-8 CSV 会乱码）
                using (var writer = new StreamWriter(filePath, true, new UTF8Encoding(true)))
                using (var csv = new CsvWriter(writer, config))
                {
                    csv.WriteRecords(data);
                }
            }
        }

        /// <summary>
        /// 写入单个数据
        /// </summary>
        public void Record<T>(T data, string filePath)
        {
            if (data == null)
            {
                return;
            }

            Records(new[] { data }, filePath);
        }

        /// <summary>
        /// 读取所有记录。
        /// 🔴 立即物化：CsvHelper 的 GetRecords 是惰性迭代，直接返回会在 reader 释放后枚举失败。
        /// </summary>
        public IEnumerable<T> ReadAll<T>(string path)
        {
            lock (LockFor(path))
            {
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    return csv.GetRecords<T>().ToList();
                }
            }
        }

        /// <summary>
        /// 读取所有记录并转为 DataTable（CsvHelper 解析，支持引号内的逗号 / 换行）
        /// </summary>
        public DataTable ReadAllToDataTable(string path)
        {
            return ReadAllToDataTable(path, 0);
        }

        /// <summary>
        /// 读取记录并转为 DataTable；maxRows &gt; 0 时只取最后 N 行。
        /// 🔴 读失败会抛异常（不再返回 null——null 与「没有数据」无法区分）。
        /// </summary>
        public DataTable ReadAllToDataTable(string path, int maxRows = 0)
        {
            lock (LockFor(path))
            {
                using (var reader = new StreamReader(path))
                using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
                {
                    var dt = new DataTable();

                    // 空文件：返回空表（不是错误）
                    if (!csv.Read())
                    {
                        return dt;
                    }

                    csv.ReadHeader();

                    var heads = csv.HeaderRecord ?? new string[0];
                    foreach (var head in heads)
                    {
                        dt.Columns.Add(new DataColumn(head));
                    }

                    var rows = new List<string[]>();
                    while (csv.Read())
                    {
                        var row = new string[heads.Length];
                        for (int i = 0; i < heads.Length; i++)
                        {
                            row[i] = csv.GetField(heads[i]);
                        }
                        rows.Add(row);
                    }

                    // 只取最后 maxRows 行
                    if (maxRows > 0 && rows.Count > maxRows)
                    {
                        rows = rows.Skip(rows.Count - maxRows).ToList();
                    }

                    foreach (var row in rows)
                    {
                        var dr = dt.NewRow();
                        for (int i = 0; i < heads.Length; i++)
                        {
                            dr[i] = row[i] ?? string.Empty;
                        }
                        dt.Rows.Add(dr);
                    }

                    return dt;
                }
            }
        }
    }
}
