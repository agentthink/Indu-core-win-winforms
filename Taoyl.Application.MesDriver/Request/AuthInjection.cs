using System;
using System.Text;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Request
{
    /// <summary>
    /// 鉴权注入项：从鉴权配置算出「往请求头塞什么 / 往查询参数塞什么」。
    /// From 返回 null = 鉴权不生效（无鉴权 / 字段不齐），调用方什么都不要做。
    ///
    /// 🔴 凭证只能进头或查询串，所以天生两个去向；但"往集合里放"由调用方执行（RequestHeaders /
    /// RequestUrl），放哪、什么时机都是看得见的数据流。「是否生效」由本类回答，
    /// 「摘掉手配的 Authorization」只在生效时发生。
    /// 🔴 四分支 switch 不做策略模式：每支 1~3 行、无算法、四家 MES 只这四种。
    /// </summary>
    internal sealed class AuthInjection
    {
        /// <summary>要注入请求头的项（Authorization / 自定义 key）。null = 没有。</summary>
        public MesHeader Header;

        /// <summary>要注入查询串的项（KeyIn = Query）。null = 没有。</summary>
        public MesHeader QueryParam;

        /// <summary>
        /// 鉴权配置 → 注入项。null = 不生效，调用方不摘头、不注入。
        /// 🔴 Bearer 前缀在这里拼，配置里的 Token 不含 "Bearer "。
        /// </summary>
        internal static AuthInjection From(MesAuthConfig auth)
        {
            if (auth == null || auth.Type == MesAuthType.None)
                return null;

            switch (auth.Type)
            {
                case MesAuthType.Bearer:
                    if (string.IsNullOrEmpty(auth.Token))
                        return null;

                    return new AuthInjection
                    {
                        Header = new MesHeader("Authorization", "Bearer " + auth.Token)
                    };

                case MesAuthType.Basic:
                    // 用 UTF8 而非标准 Latin1：工控账号密码都是 ASCII，两者等价；万一有中文，UTF8 更宽容
                    string raw = (auth.UserName ?? string.Empty) + ":" + (auth.Password ?? string.Empty);
                    return new AuthInjection
                    {
                        Header = new MesHeader("Authorization",
                            "Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes(raw)))
                    };

                case MesAuthType.ApiKey:
                    if (string.IsNullOrWhiteSpace(auth.KeyName))
                        return null;

                    return auth.KeyIn == MesKeyIn.Query
                        ? new AuthInjection { QueryParam = new MesHeader(auth.KeyName, auth.KeyValue ?? string.Empty) }
                        : new AuthInjection { Header = new MesHeader(auth.KeyName, auth.KeyValue ?? string.Empty) };

                default:
                    // 🔴 枚举加了新值、这里忘了加 case：必须显式失败。
                    // 返回 null 会变成「静默不鉴权」——请求裸奔发出，且没有任何报错。
                    throw new NotSupportedException("未实现的鉴权类型: " + auth.Type);
            }
        }
    }
}
