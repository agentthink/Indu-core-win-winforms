using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace Taoyl.Application.MesDriver.Transport
{
    /// <summary>
    /// 响应读取：流式限量读 + 按声明解码。
    ///
    /// 🔴 解码只认「声明」，不猜：BOM → 响应头 charset → UTF-8。
    /// 声明与实际不符时**不自动换编码**，而是把替换字符（�）如实显示出来并在尾部标注。
    /// 理由：编码错是服务端的问题，驱动替它纠正会抹掉「声明与内容不符」这个排障信号，
    /// 也会让界面看到的文本与流程侧按同一份字节解出来的不一致。
    /// </summary>
    internal static class HttpResponseReader
    {
        private const int MaxBodyBytes = 8 * 1024 * 1024;

        /// <summary>U+FFFD，解码器遇到无法识别的字节时写入的替换字符。</summary>
        private const char Replacement = '\uFFFD';

        /// <summary>流式限量读：超过 8MB 就截断，内存只占上限那么多（不是先全读再截）。</summary>
        internal static string Read(HttpResponseMessage response)
        {
            using (Stream stream = response.Content
                       .ReadAsStreamAsync()
                       .GetAwaiter()
                       .GetResult())
            using (var collected = new MemoryStream())
            {
                var buffer = new byte[8192];
                int total = 0;
                bool truncated = false;

                int read;
                while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
                {
                    if (total + read > MaxBodyBytes)
                    {
                        collected.Write(buffer, 0, MaxBodyBytes - total);
                        truncated = true;
                        break;
                    }

                    collected.Write(buffer, 0, read);
                    total += read;
                }

                string text = Decode(collected.ToArray(), response.Content.Headers.ContentType);
                if (truncated)
                    text += Environment.NewLine + "[响应体超过 " + (MaxBodyBytes / 1024 / 1024) + "MB，已截断]";

                return text;
            }
        }

        /// <summary>
        /// 优先级：BOM → 响应头 charset（服务端声明）→ UTF-8（无声明时的约定）。
        ///
        /// 🔴 BOM 必须排第一，理由不是「更规范」，是两条硬事实：
        ///    ① BOM 是正文开头的三个字节。不剥掉它，正文就以 U+FEFF 开头——
        ///       带 BOM 的 JSON 响应会让 JToken.Parse 直接抛「Unexpected character」；
        ///    ② 带 BOM 的 UTF-16 若按 charset（常被服务端错写成 utf-8）解，得到的是满屏交错乱码。
        ///    BOM 是字节事实，charset 只是声明，冲突时信字节。
        ///
        /// 只解一次。出现替换字符说明字节与声明不符，此时标注而不重试——「失败再换 UTF-8」
        /// 在这里判不出来：宽松解码遇非法字节是替换成 � 而不是抛异常（这正是删掉旧探测的原因）。
        /// </summary>
        private static string Decode(byte[] bytes, MediaTypeHeaderValue contentType)
        {
            string byBom = DecodeByBom(bytes);
            if (byBom != null)
                return byBom;

            string charset = contentType?.CharSet?.Trim('"', ' ');
            string text = ResolveEncoding(charset).GetString(bytes);

            if (text.IndexOf(Replacement) >= 0)
                text += Environment.NewLine
                      + "[按 " + (charset ?? "utf-8") + " 解码存在无法识别的字节，内容可能已损坏]";

            return text;
        }

        /// <summary>无声明用 UTF-8；声明名不认识也回落 UTF-8——那是坏值，没有第二种合法解释。</summary>
        private static Encoding ResolveEncoding(string charset)
        {
            if (string.IsNullOrEmpty(charset))
                return Encoding.UTF8;

            try
            {
                return Encoding.GetEncoding(charset);
            }
            catch (ArgumentException)
            {
                return Encoding.UTF8;
            }
        }

        private static string DecodeByBom(byte[] bytes)
        {
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
                return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);

            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
                return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);

            if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
                return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);

            return null;
        }
    }
}
