namespace Taoyl.Application.Share.Config
{
    /// <summary>
    /// 刷卡登录设置契约，对应 AutoLoginConfig 的配置切片。
    /// </summary>
    public interface ICardLoginSettings
    {
        /// <summary>是否启用刷卡登录。</summary>
        bool EnableCardLogin { get; }

        /// <summary>刷卡卡号的固定长度。</summary>
        int CardInputLength { get; }

        /// <summary>刷卡按键输入超时时间（毫秒）。</summary>
        int KeyInputTimeout { get; }
    }
}
