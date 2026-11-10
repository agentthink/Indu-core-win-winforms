using System;
using System.Globalization;
using System.IO;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver.Services
{
    /// <summary>
    /// 归档清理器：按「文件名里的日期」判龄，删过期 .csv 与因此空掉的目录。
    ///
    /// 🔴 为什么需要它：目录分层（{类别}/{yyyy}/{MM}[/{dd}]）之后，NLog 的 MaxArchiveDays 只扫
    /// 「当前文件所在的那一层」（取当前文件目录 + GetFiles，不递归），旧月份永远轮不到它，而且失效
    /// 是静默的。所以 LogTargetConfigurator 把 MaxArchiveDays 覆盖成 0，删除权整体移交本类——
    /// 单一删除者，磁盘上少了文件只可能有一个来源。
    ///
    /// 🔴 「删不到不该删的」靠结构，不靠小心：
    ///    ① 只删「本通道前缀 + 日期段 + .csv」这一个形状——别人的文件、被人加过后缀的文件一律不动；
    ///    ② 只进「名字是合法日期段、且该段起点 ≤ cutoff」的目录，整段都在保留窗内的不进；
    ///    ③ 只删空目录：先查空再删，删除调用本身非递归；
    ///    ④ 正在写的目录永远够不到 cutoff；KeepDays = -1 的通道整段跳过。
    ///
    /// 🔴 异常政策（单出口）：IO 类故障（权限 / 占用 / 并发删除）就地接住，是预期内的，绝不中断整轮；
    /// 其余异常只可能来自代码缺陷，一律冒到 <see cref="Clean"/> 顶层记一条带堆栈的 Error——
    /// 缺陷让清理停摆必须留痕，但不能靠"吞掉一切"来换取表面安稳。
    /// </summary>
    internal sealed class LogArchiveCleaner
    {
        /// <summary>最大下钻层数 = 目录格式段数；加一段格式就自动多一层，不用改这里。</summary>
        private static readonly int MaxDepth = LogArchiveLayout.PeriodFormats.Length;

        private static readonly string[] EmptyDirectories = new string[0];

        /// <summary>
        /// 清理一轮。返回统计供调用方记录；预期故障在内部消化，未预期异常记进 <see cref="LogCleanResult.Error"/>。
        /// </summary>
        public LogCleanResult Clean(ILogSettings settings)
        {
            var result = new LogCleanResult();
            if (settings == null)
                return result;

            try
            {
                string root = LogTargetConfigurator.ResolveRoot(settings.RootPath);

                foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
                {
                    ILogCategoryPolicy policy = settings.GetPolicy(category);
                    if (policy == null || policy.KeepDays < 1)
                        continue;   // -1（或任何非正值）= 该通道不清理。🔴 必须挡在算 cutoff 之前：
                                    // -(-1) 会把 cutoff 推到明天，等于「全部删光」

                    var scope = new CleanScope
                    {
                        // 前缀连分隔符一起带上：既做 GetFiles 的文件名过滤，也做归属判断
                        Prefix = LogTargetConfigurator.ResolvePrefix(policy) + LogArchiveLayout.StampSeparator,
                        Cutoff = DateTime.Today.AddDays(-policy.KeepDays),
                        Result = result
                    };

                    // 类别层没有日期段，periodStart 用不到（第 1 层只认四位年份）
                    CleanTree(Path.Combine(root, category.ToString()), 0, default(DateTime), scope);
                }
            }
            catch (Exception ex)
            {
                // 唯一的记账点：IO 类故障已在各层就地接住，能到这里的只可能是「没预料到」的——
                // 配置不合前提或代码缺陷。不静默：由调用方写成一条带堆栈的 Error。
                result.RecordError(ex);
            }

            return result;
        }

        #region 主通道树

        /// <summary>
        /// 处理一层：删本层过期文件 → 只对「可能含过期文件」的子目录下钻 → 空了就把自己删掉。
        /// 🔴 本方法刻意不包 try：读目录的预期故障由 <see cref="GetDirectoriesOrEmpty"/> 接，
        /// 别的一律冒到顶层记账（一个缺陷在 N 个目录上重复，记 N 条没有意义）。
        /// </summary>
        private void CleanTree(string directory, int depth, DateTime periodStart, CleanScope scope)
        {
            if (!Directory.Exists(directory))
                return;

            DeleteExpiredFiles(directory, scope);

            if (depth < MaxDepth)
            {
                foreach (string sub in GetDirectoriesOrEmpty(directory))
                {
                    DateTime childPeriod;
                    if (TryGetPeriodStart(Path.GetFileName(sub), depth + 1, periodStart, out childPeriod)
                        && childPeriod <= scope.Cutoff)
                    {
                        CleanTree(sub, depth + 1, childPeriod, scope);
                    }
                }
            }

            TryDeleteEmptyDirectory(directory, depth, scope.Result);
        }

        /// <summary>
        /// 读子目录；读不了就是空——权限 / 被占用 / 并发被删都是预期故障，不该中断整轮清理。
        /// 用数组而不是惰性枚举：调用方会删掉枚举到的子目录本身，惰性枚举器踩到已消失的项
        /// 在不同文件系统上行为不一，取值后再遍历最稳。
        /// </summary>
        private static string[] GetDirectoriesOrEmpty(string directory)
        {
            try
            {
                return Directory.GetDirectories(directory);
            }
            catch (IOException)
            {
                return EmptyDirectories;
            }
            catch (UnauthorizedAccessException)
            {
                return EmptyDirectories;
            }
        }

        /// <summary>
        /// 目录名 → 该目录代表的日期段起点，解析与「该不该进」一次完成。
        /// 🔴 这里是唯一的剪枝点：名字不像日期、或起点晚于 cutoff 的目录，里面所有文件的日期都不早于
        /// 起点、不可能过期，一律不进——少了这一道，每次启动都要把整个保留窗口的目录走一遍，
        /// 只为发现「没有要删的」。
        /// </summary>
        private static bool TryGetPeriodStart(string name, int depth, DateTime parentPeriodStart, out DateTime periodStart)
        {
            periodStart = default(DateTime);
            if (string.IsNullOrEmpty(name))
                return false;

            // 位数由格式段自身长度决定（yyyy=4 / MM=2 / dd=2），格式改了几位这里自动跟着
            string format = LogArchiveLayout.PeriodFormats[depth - 1];
            int value;
            if (name.Length != format.Length
                || !int.TryParse(name, NumberStyles.None, CultureInfo.InvariantCulture, out value)
                || value < 1)
            {
                return false;
            }

            if (depth == 1)
            {
                if (value < 2000 || value > 2999)
                    return false;

                periodStart = new DateTime(value, 1, 1);
                return true;
            }

            if (depth == 2)
            {
                if (value > 12)
                    return false;

                periodStart = new DateTime(parentPeriodStart.Year, value, 1);
                return true;
            }

            if (value > DateTime.DaysInMonth(parentPeriodStart.Year, parentPeriodStart.Month))
                return false;

            periodStart = parentPeriodStart.AddDays(value - 1);
            return true;
        }

        /// <summary>类别根不删（那是 NLog 的落盘根，删了下一行日志还得重建）；其余只删空的。</summary>
        private static void TryDeleteEmptyDirectory(string directory, int depth, LogCleanResult result)
        {
            if (depth < 1)
                return;

            // 先查空再删，不拿 IOException 当「非空」用：异常做控制流会让每次启动在调试器里
            // 抛一串 first-chance（开了「引发此异常时中断」就每次断下来），目录越多越难看
            try
            {
                if (Directory.GetFileSystemEntries(directory).Length != 0)
                    return;

                Directory.Delete(directory);
                result.DirsDeleted++;
            }
            catch (IOException)
            {
                // 目录非空（查空与删除之间的竞态）/ 被占用——都是正常情况
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        #endregion

        #region 删除与判龄

        /// <summary>
        /// 删本层过期的 .csv。判龄只认文件名里的日期：拷贝、恢复备份、改系统时间都骗不过它
        ///（NLog 那一套用的是文件创建时间，恰好是它的短板）。
        /// </summary>
        private static void DeleteExpiredFiles(string directory, CleanScope scope)
        {
            string[] files;
            try
            {
                // 先按「前缀*.csv」在文件系统层过滤，再看形状——别把目录里所有文件都取回来
                files = Directory.GetFiles(directory, scope.Prefix + "*" + LogArchiveLayout.CsvExtension);
            }
            catch (IOException)
            {
                return;   // 目录被并发删 / 设备错误
            }
            catch (UnauthorizedAccessException)
            {
                return;
            }

            foreach (string file in files)
            {
                DateTime date;
                string name = Path.GetFileName(file);
                if (!TryParseStamp(name, scope.Prefix, out date) || date >= scope.Cutoff)
                    continue;

                try
                {
                    File.Delete(file);
                    scope.Result.AddDeletedFile(name);
                }
                catch
                {
                    // 🔴 这里刻意不分类：任何异常都等价于「这一次没删掉」，计数后继续是对的。
                    // 而且失败数会出现在 Info 行（失败 N 个），不存在静默。
                    scope.Result.FilesFailed++;
                }
            }
        }

        /// <summary>
        /// 文件名 → 日期。只认「本通道前缀 + 日期段 + .csv」这一种形状：
        /// 前缀不符（别人的文件）、形状不符（被加了 _bak、或改成别的写法）一律不动。
        /// 🔴 判据是归属而不是「看起来像日期」——宁可留下垃圾，也不误删别人的东西。
        /// </summary>
        private static bool TryParseStamp(string fileName, string prefix, out DateTime date)
        {
            date = default(DateTime);

            if (string.IsNullOrEmpty(fileName)
                || !fileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                || !fileName.EndsWith(LogArchiveLayout.CsvExtension, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            string stamp = fileName.Substring(prefix.Length,
                fileName.Length - prefix.Length - LogArchiveLayout.CsvExtension.Length);

            if (!DateTime.TryParseExact(stamp, LogArchiveLayout.DayStampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date)
                && !DateTime.TryParseExact(stamp, LogArchiveLayout.HourStampFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out date))
            {
                return false;
            }

            // 早于 2000 年 = 那串数字不是日期；晚于今天 = 写错或时区错，不动
            if (date.Year < 2000 || date.Date > DateTime.Today)
            {
                date = default(DateTime);
                return false;
            }

            return true;
        }

        #endregion

        /// <summary>一次清理在一棵树上的公共参数：前缀 / 截止日 / 统计。</summary>
        private sealed class CleanScope
        {
            /// <summary>本通道的「前缀 + 分隔符」，如 <c>Info_</c>。</summary>
            public string Prefix;

            /// <summary>早于它的文件判过期。</summary>
            public DateTime Cutoff;

            public LogCleanResult Result;
        }
    }
}
