using System;
using System.Collections.Generic;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// 请求正文的入口：形态 → IBodyBuilder 的登记处。
    ///
    /// 🔴 查不到一律抛，绝不 default 兜底——宁可这次调用失败，也不带着错的 Content-Type
    /// 或错的合并语义把请求发出去。
    /// 🔴 与界面侧的 MesBodyTypeUi 键集必须一致，无编译期保障（加形态清单见 MesBodyType 枚举注释）。
    /// </summary>
    internal static class RequestBody
    {
        private static readonly Dictionary<MesBodyType, IBodyBuilder> Builders =
            new Dictionary<MesBodyType, IBodyBuilder>
            {
                { MesBodyType.None, new NoneBodyBuilder() },
                { MesBodyType.Json, new JsonBodyBuilder() },
                { MesBodyType.Form, new FormBodyBuilder() },
                { MesBodyType.Text, new TextBodyBuilder() },
                { MesBodyType.Xml, new XmlBodyBuilder() }
            };

        /// <summary>取某形态的 builder。未登记即抛——理由见类注释。</summary>
        internal static IBodyBuilder For(MesBodyType type)
        {
            IBodyBuilder builder;
            if (Builders.TryGetValue(type, out builder))
                return builder;

            throw new NotSupportedException("未支持的正文形态: " + type);
        }
    }
}
