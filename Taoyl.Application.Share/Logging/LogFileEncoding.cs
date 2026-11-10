namespace Taoyl.Application.Share.Logging
{
    /// <summary>
    /// 日志文件写入编码。
    /// 从日志模块迁入（可配置化：Share 中继契约需要此类型），成员与数值同旧类型一致。
    /// </summary>
    public enum LogFileEncoding
    {
        /// <summary>UTF-8 带 BOM（默认：Excel/Notepad 自动识别中文）</summary>
        Utf8Bom = 0,

        /// <summary>UTF-8 无 BOM（兼容严格按 UTF-8 规范解析的工具）</summary>
        Utf8 = 1,

        /// <summary>GBK（兼容老版 Excel 2003/2007 与本地化工具）</summary>
        Gbk = 2
    }
}
