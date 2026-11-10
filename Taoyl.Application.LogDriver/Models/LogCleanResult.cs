using System;
using System.Collections.Generic;

namespace Taoyl.Application.LogDriver
{
    /// <summary>
    /// 一轮归档清理的统计。清理器只产出这个对象、自己不写日志，
    /// 由调用方（NLogLogService）决定记不记、怎么记——避免清理路径反过来依赖日志写入。
    /// 命名空间与 LogEventArgs / LogGridBinding 一致放根下：Services 层直接用，不必引 Models
    ///（引了会和 NLog.LogLevel 抢 LogLevel 这个名字）。
    /// </summary>
    internal sealed class LogCleanResult
    {
        /// <summary>清单最多记这么多个：够定位是哪一批，又不至于把一条日志刷成几千字符。</summary>
        private const int MaxRecordedNames = 20;

        private readonly List<string> deletedNames = new List<string>();

        /// <summary>已删除的过期文件数（含未进清单的那些）。</summary>
        public int FilesDeleted { get; private set; }

        /// <summary>删除失败数（被其它进程占用 / 无权限，下次清理会再试）。</summary>
        public int FilesFailed { get; set; }

        /// <summary>已删除的空目录数。</summary>
        public int DirsDeleted { get; set; }

        /// <summary>已删除的文件名（最多 20 个；总数看 <see cref="FilesDeleted"/>）。</summary>
        public IReadOnlyList<string> DeletedNames => deletedNames;

        /// <summary>本轮是否动过东西（调用方据此决定要不要记一条日志，免得每天刷一条空记录）。</summary>
        public bool HasChanges => FilesDeleted + FilesFailed + DirsDeleted > 0;

        /// <summary>
        /// 第一个「没预料到」的异常（IO 类故障在清理器内部就消化了，到不了这里）。
        /// 只留第一个：同一个缺陷会在 N 个目录上重复触发，记 N 条只会把 Error 通道刷满。
        /// </summary>
        public Exception Error { get; private set; }

        /// <summary>记下第一个未预期异常，后续不覆盖。</summary>
        public void RecordError(Exception ex)
        {
            if (Error == null && ex != null)
                Error = ex;
        }

        /// <summary>记一个已删文件：计数必加，清单按上限收。</summary>
        public void AddDeletedFile(string fileName)
        {
            FilesDeleted++;
            if (deletedNames.Count < MaxRecordedNames)
                deletedNames.Add(fileName);
        }
    }
}
