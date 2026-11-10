using CsvHelper.Configuration.Attributes;

namespace Taoyl.Application.Business.Mes
{
    /// <summary>
    /// 一次 MES 调用的落盘记录：字段与 <c>MesResult</c> 一一对应，
    /// 只把枚举与可空值转成人能读的字符串。表头由 CsvHelper 的 <c>[Name]</c> 生成。
    ///
    /// 🔴 Request / Response 原样存全文，不截断——追溯优先。代价是超大响应会让单行很长
    /// （HttpResponseReader 上限 8MB），Excel 打开会慢，但数据是完整的。
    /// 🔴 用 string 装时间而不是 DateTime：CSV 是给人看的，固定格式比 Excel 自动猜测可靠
    /// （自动解析会把 "2026-11-02 14:30:00.123" 变成不同区域格式）。
    /// </summary>
    public sealed class MesCallRecord
    {
        [Name("开始时间")]
        public string StartTime { get; set; }

        [Name("结束时间")]
        public string EndTime { get; set; }

        [Name("耗时(ms)")]
        public long ElapsedMs { get; set; }

        [Name("接口")]
        public string ApiName { get; set; }

        /// <summary>空 = 没拿到响应（此时看「失败分类」）。</summary>
        [Name("状态码")]
        public string HttpStatus { get; set; }

        /// <summary>中文分类；成功（None）时留空，不给成功行添噪音。</summary>
        [Name("失败分类")]
        public string Failure { get; set; }

        [Name("尝试次数")]
        public int Attempts { get; set; }

        [Name("请求报文")]
        public string Request { get; set; }

        [Name("响应报文")]
        public string Response { get; set; }
    }
}
