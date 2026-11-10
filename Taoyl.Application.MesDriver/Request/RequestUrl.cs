using System;
using System.Collections.Generic;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Request
{
    /// <summary>
    /// 请求地址：查询参数快照 + 拼出最终地址。
    ///
    /// 🔴 拆两步是为了让「鉴权参数先入集合」这个顺序写在调用点上：
    /// CollectQuery（配置参数 + 鉴权参数）→ Build（拼地址）。
    /// </summary>
    internal static class RequestUrl
    {
        /// <summary>
        /// 查询参数快照 + 鉴权参数（若有）。过滤空名、值归空串；不碰配置快照。
        /// 返回值同时用于拼地址和 Render 展示（后者要的是编码前原值）。
        /// </summary>
        internal static List<MesHeader> CollectQuery(List<MesHeader> queryParams, AuthInjection auth)
        {
            var list = new List<MesHeader>();
            if (queryParams != null)
            {
                foreach (MesHeader item in queryParams)
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Name))
                        continue;

                    list.Add(new MesHeader(item.Name, item.Value ?? string.Empty));
                }
            }

            if (auth != null && auth.QueryParam != null)
                list.Add(auth.QueryParam);

            return list;
        }

        /// <summary>
        /// URL + 查询参数。逐项 EscapeDataString——值里含 &amp; / = / 中文 / 空格时不编码会拆坏 URL。
        /// 🔴 与 Form 编码不同：这里空格变 %20，form 里空格变 +。
        /// </summary>
        internal static string Build(string url, List<MesHeader> query)
        {
            string baseUrl = url ?? string.Empty;
            if (query == null || query.Count == 0)
                return baseUrl;

            var parts = new List<string>(query.Count);
            foreach (MesHeader param in query)
            {
                if (param == null || string.IsNullOrWhiteSpace(param.Name))
                    continue;

                parts.Add(Uri.EscapeDataString(param.Name) + "=" + Uri.EscapeDataString(param.Value ?? string.Empty));
            }

            if (parts.Count == 0)
                return baseUrl;

            string joined = string.Join("&", parts);

            if (baseUrl.IndexOf('?') < 0)
                return baseUrl + "?" + joined;

            if (baseUrl.EndsWith("?") || baseUrl.EndsWith("&"))
                return baseUrl + joined;

            return baseUrl + "&" + joined;
        }
    }
}
