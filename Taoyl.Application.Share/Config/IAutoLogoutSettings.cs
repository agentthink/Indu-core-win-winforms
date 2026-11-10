namespace Taoyl.Application.Share.Config
{
    /// <summary>
    /// 自动注销设置契约，对应 DevConfig.AutoLogoutTime。
    /// </summary>
    public interface IAutoLogoutSettings
    {
        /// <summary>无操作自动注销时间（秒）。</summary>
        int AutoLogoutTime { get; }
    }
}
