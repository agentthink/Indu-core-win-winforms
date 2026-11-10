namespace Taoyl.Application.Share.Paths
{
    /// <summary>
    /// 配置文件路径名称。
    /// 注意：这是路径约定（实现细节），非跨模块契约，将来应逐步收归各负责模块私有。
    /// </summary>
    public class ConfigPathName
    {
        ///<summary>认证模块备份目录</summary>
        public const string AuthBackupDir = "Config/Auth/Backup";

        ///<summary>设备配置文件路径，包含设备的参数和连接信息</summary>
        public const string DevicesFile = "Config/Devices.json";

        ///<summary>MES 接口配置文件路径（接口定义 + 报文底稿），备份在同目录 Backup/ 下</summary>
        public const string MesFile = "Config/Mes.json";

        ///<summary>菜单配置文件路径，定义软件的功能菜单结构和权限控制</summary>
        public const string MenuFile = "Config/Menu.json";

        /// <summary>日志根路径文件夹名称 </summary>
        public const string LogsFolder = "Logs";

        ///<summary>PLC通讯日志路径</summary>
        public const string PLCLogFile = "Logs/PLC";

        /// <summary>PLC配置管理器日志文件夹，记录PLC加载配置文件的过程、错误信息等，便于调试和维护</summary>
        public const string ConfigManagerLogFile = "ConfigManager.log";
    }
}
