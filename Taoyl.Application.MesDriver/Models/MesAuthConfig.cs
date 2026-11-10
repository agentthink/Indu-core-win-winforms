namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>鉴权方式。四家 MES 只出现这四种，不做策略模式。</summary>
    public enum MesAuthType
    {
        /// <summary>无鉴权（如新余赣锋，仅靠 body 里的 EID 标识）</summary>
        None = 0,

        /// <summary>Authorization: Bearer {Token}（如蓝京 FMS）</summary>
        Bearer = 1,

        /// <summary>Authorization: Basic base64(user:password)</summary>
        Basic = 2,

        /// <summary>自定义名值对，按 KeyIn 决定放请求头还是查询串</summary>
        ApiKey = 3
    }

    /// <summary>ApiKey 的放置位置。Body 分支刻意不提供——见 MesApiDefinition.Body 的说明。</summary>
    public enum MesKeyIn
    {
        Header = 0,
        Query = 1
    }

    /// <summary>
    /// 鉴权配置。7 字段扁平，四种类型共用，不用的字段留空即可。
    /// 🔴 Token 不含 "Bearer " 前缀——前缀由驱动在发送时拼接，
    /// 否则配置里带前缀、代码再拼一次会变成 "Bearer Bearer xxx"。
    /// </summary>
    public class MesAuthConfig
    {
        public MesAuthType Type { get; set; } = MesAuthType.None;

        /// <summary>Bearer 用。不含 "Bearer " 前缀。</summary>
        public string Token { get; set; }

        /// <summary>Basic 用。</summary>
        public string UserName { get; set; }

        /// <summary>Basic 用。</summary>
        public string Password { get; set; }

        /// <summary>ApiKey 用：头名或参数名。</summary>
        public string KeyName { get; set; }

        /// <summary>ApiKey 用：值。</summary>
        public string KeyValue { get; set; }

        /// <summary>ApiKey 用：放请求头还是查询串。</summary>
        public MesKeyIn KeyIn { get; set; } = MesKeyIn.Header;

        public MesAuthConfig Clone()
        {
            return new MesAuthConfig
            {
                Type = Type,
                Token = Token,
                UserName = UserName,
                Password = Password,
                KeyName = KeyName,
                KeyValue = KeyValue,
                KeyIn = KeyIn
            };
        }
    }
}
