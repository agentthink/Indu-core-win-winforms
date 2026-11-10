namespace Taoyl.Application.LogDriver.Services
{
    /// <summary>
    /// 日志目录布局的唯一真相：写入方（LogTargetConfigurator）与清理方（LogArchiveCleaner）共用，
    /// 改格式只改这里，两边同时生效。
    /// 🔴 反过来：改这几个常量等于改磁盘上的目录结构——老文件会留在旧布局里，
    /// 清理器不认也就不会删，生产上要当成一次迁移处理。
    /// </summary>
    internal static class LogArchiveLayout
    {
        /// <summary>
        /// 目录段格式，下标 = 层级 - 1（第 1 层年、第 2 层月、第 3 层日）。
        /// 🔴 必须是纯字母格式且字符数 = 数字位数（yyyy / MM / dd 满足）——解析侧用 format.Length 判位数；
        /// 换成 yyyy-MM 这类带分隔符的写法会失效，失效方式是「不删」而不是「误删」。
        /// </summary>
        public static readonly string[] PeriodFormats = { "yyyy", "MM", "dd" };

        /// <summary>文件名日期段：按天 8 位 / 按小时 10 位。</summary>
        public const string DayStampFormat = "yyyyMMdd";
        public const string HourStampFormat = "yyyyMMddHH";

        /// <summary>文件名形状：{前缀}{分隔符}{日期段}{扩展名}。</summary>
        public const char StampSeparator = '_';
        public const string CsvExtension = ".csv";
    }
}
