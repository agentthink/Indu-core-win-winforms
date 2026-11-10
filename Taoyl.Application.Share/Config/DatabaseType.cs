using System.ComponentModel;

namespace Taoyl.Application.Share.Config
{
    /// <summary>
    /// 数据库类型。
    /// [Description] 供配置界面下拉显示（ConfigFormRenderer.MakeCombo 走 GetDescription）。
    /// 🔴 只列**已装驱动包**的类型——列了却没装包，用户选中就是启动即炸。
    /// 新增成员必须同步：①引驱动包 ②补 SqlSugarHelper.MapDbType 分支 ③更新 DataBase/README 对照表。
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// MySQL（5.7 / 8.x）。
        /// 示例：server=127.0.0.1;port=3306;database=tyl_ifa;uid=root;pwd=xxx;charset=utf8mb4;
        /// </summary>
        [Description("MySQL")]
        MySql = 0,

        /// <summary>
        /// SQLite 单文件库（默认：免安装，开箱即用）。
        /// 示例：Data Source=Data\tyl_ifa.db;Version=3;Journal Mode=WAL;BusyTimeout=5000;Pooling=True;
        /// 相对路径以程序目录为基准，绝对路径原样使用。
        /// </summary>
        [Description("SQLite")]
        Sqlite = 1,
    }
}
