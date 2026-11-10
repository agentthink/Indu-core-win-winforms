using System;
using System.Collections.Generic;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Request
{
    /// <summary>
    /// 请求头：配置头 + 鉴权注入 → 最终请求头集合；顺带把 Content-Type 分离出来。
    ///
    /// 🔴 纯函数，不修改传入的配置集合。
    /// 🔴 Content-Type 不属于请求头集合（只能挂在 HttpContent.Headers 上）：分离出来交给
    /// RequestTemplate.ContentType，Build 时按有没有内容段决定发不发——留在集合里就只能靠
    /// Build 事后特判，且无正文时 Render 会显示一个不会发出的头。
    /// 🔴 「鉴权赢」只在这里实现一次，前提是鉴权确实生效（auth 非 null）：鉴权页漏填字段时
    /// 不能把使用手配的 Authorization 摘掉，否则会变成「配了头却不生效、Render 里也看不到」。
    /// ⚠️ 值不做校验（不拦 CRLF）：含换行的值会发出畸形报文。
    /// </summary>
    internal static class RequestHeaders
    {
        /// <summary>头列表 → 最终请求头集合；Content-Type 走 out 参数单独带出（可能为 null）。</summary>
        internal static List<MesHeader> Build(List<MesHeader> headers, AuthInjection auth, out string contentType)
        {
            contentType = null;

            var list = new List<MesHeader>();
            if (headers != null)
            {
                foreach (MesHeader item in headers)
                {
                    MesHeader normalized = Normalize(item);
                    if (normalized == null)
                        continue;

                    // 内容头分离。重名取第一条——Content-Type 只有一个，重复项没有第二种合法解释。
                    if (string.Equals(normalized.Name, "Content-Type", StringComparison.OrdinalIgnoreCase))
                    {
                        if (contentType == null)
                            contentType = normalized.Value;
                        continue;
                    }

                    list.Add(normalized);
                }
            }

            if (auth == null)
                return list;

            list.RemoveAll(header => IsAuthorization(header.Name));

            if (auth.Header != null)
                list.Add(auth.Header);

            return list;
        }

        /// <summary>该头名是否 Authorization（大小写不敏感）。「鉴权赢」规则的唯一判据。</summary>
        private static bool IsAuthorization(string name)
        {
            return string.Equals(name, "Authorization", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>过滤空名、值归空串。返回 null 表示这条无效。</summary>
        private static MesHeader Normalize(MesHeader header)
        {
            if (header == null || string.IsNullOrWhiteSpace(header.Name))
                return null;

            return new MesHeader(header.Name, header.Value ?? string.Empty);
        }
    }
}
