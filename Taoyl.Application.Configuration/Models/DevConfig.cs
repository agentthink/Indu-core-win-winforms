using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using Taoyl.Application.Share.Config;

namespace Taoyl.Application.Configuration.Models
{
    /// <summary>
    /// DevConfig.json 根配置，包含语言、数据库、认证、自动登录等系统参数。
    /// </summary>
    public class DevConfig : ConfigBase, IAutoLogoutSettings
    {
        private string uiLang = "zh_CN";
        private DBConfig db = new DBConfig();
        private AutoLoginConfig autoLogin = new AutoLoginConfig();
        private int autoLogoutTime = 180;

        /// <summary>
        /// 界面语言代码（如 "zh_CN"、"en_US"）。
        /// </summary>
        [DisplayName("界面语言")]
        [Browsable(false)]
        public string UILanguage
        {
            get => uiLang;
            set => SetProperty(ref uiLang, value);
        }

        /// <summary>
        /// 数据库连接配置。
        /// </summary>
        [DisplayName("数据库")]
        public DBConfig DB
        {
            get => db;
            set => SetProperty(ref db, value);
        }

        /// <summary>
        /// 自动登录相关配置。
        /// </summary>
        [DisplayName("自动登录")]
        [JsonProperty("autoLogin")]
        public AutoLoginConfig AutoLogin
        {
            get => autoLogin;
            set => SetProperty(ref autoLogin, value);
        }

        /// <summary>
        /// 无操作自动登出时间（秒）。
        /// </summary>
        [DisplayName("自动注销时间")]
        [JsonProperty("autoLogoutTime")]
        public int AutoLogoutTime
        {
            get => autoLogoutTime;
            set => SetProperty(ref autoLogoutTime, value);
        }
    }

    /// <summary>
    /// 数据库连接配置，嵌套在 DevConfig 中。
    /// </summary>
    public class DBConfig : ConfigBase
    {
        private DatabaseType dbType = DatabaseType.Sqlite;
        private string connectionString = @"Data Source=Data\tyl_ifa.db;Version=3;Journal Mode=WAL;BusyTimeout=5000;Pooling=True;";

        /// <summary>
        /// 数据库类型。切换类型必须同时改连接字符串——两者不会自动对应。
        /// </summary>
        [DisplayName("数据库类型")]
        [JsonConverter(typeof(StringEnumConverter))]
        public DatabaseType DBType
        {
            get => dbType;
            set => SetProperty(ref dbType, value);
        }

        /// <summary>
        /// 数据库连接字符串。SQLite 的相对路径以程序目录为基准解析，绝对路径原样使用。
        /// </summary>
        [DisplayName("连接字符串")]
        public string ConnectionString
        {
            get => connectionString;
            set => SetProperty(ref connectionString, value);
        }
    }

    /// <summary>
    /// 刷卡自动登录配置，嵌套在 DevConfig 中。
    /// </summary>
    public class AutoLoginConfig : ConfigBase, ICardLoginSettings
    {
        private bool enableCardLogin = true;
        private int cardInputLength = 8;
        private int keyInputTimeout = 100;

        /// <summary>
        /// 是否启用刷卡登录功能。
        /// </summary>
        [DisplayName("启用刷卡登录")]
        public bool EnableCardLogin
        {
            get => enableCardLogin;
            set => SetProperty(ref enableCardLogin, value);
        }

        /// <summary>
        /// 刷卡卡号的固定长度。
        /// </summary>
        [DisplayName("卡号长度")]
        public int CardInputLength
        {
            get => cardInputLength;
            set => SetProperty(ref cardInputLength, value);
        }

        /// <summary>
        /// 刷卡按键输入超时时间（毫秒）。
        /// </summary>
        [DisplayName("按键超时")]
        public int KeyInputTimeout
        {
            get => keyInputTimeout;
            set => SetProperty(ref keyInputTimeout, value);
        }
    }
}
