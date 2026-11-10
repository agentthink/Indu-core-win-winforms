using SqlSugar;
using System;
using System.Data.SQLite;
using System.IO;
using Taoyl.Application.Share.Config;

namespace Taoyl.Application.DataBase
{
    /// <summary>
    /// 数据库连接配置
    /// </summary>
    public class DbConnectionConfig
    {
        public DatabaseType DbType { get; set; }
        public string ConnectionString { get; set; }
    }

    /// <summary>
    /// SqlSugar数据库助手 — 配置驱动，支持多数据库类型切换
    /// </summary>
    public class SqlSugarHelper
    {
        private readonly DbConnectionConfig config;
        private readonly Lazy<SqlSugarScope> lazyDb;

        public SqlSugarScope Db => lazyDb.Value;

        public SqlSugarHelper(DbConnectionConfig dbConfig)
        {
            config = dbConfig ?? throw new ArgumentNullException(nameof(dbConfig));
            lazyDb = new Lazy<SqlSugarScope>(CreateDb, true);
        }

        private SqlSugarScope CreateDb()
        {
            var connConfig = new ConnectionConfig
            {
                ConnectionString = ResolveConnectionString(config.DbType, config.ConnectionString),
                DbType = MapDbType(config.DbType),
                IsAutoCloseConnection = true,
                InitKeyType = InitKeyType.Attribute
            };

            var db = new SqlSugarScope(connConfig);

            db.Aop.OnLogExecuting = (sql, pars) =>
            {
                // 需要调试 SQL 时在此挂日志（推荐 GetNativeSql，性能 OK；GetSqlString 大参数量有性能影响，仅排查用）
            };

            return db;
        }

        /// <summary>
        /// 业务枚举 → SqlSugar 枚举。新增 DatabaseType 成员必须同步补分支。
        /// </summary>
        private static DbType MapDbType(DatabaseType dbType)
        {
            switch (dbType)
            {
                case DatabaseType.MySql: return DbType.MySql;
                case DatabaseType.Sqlite: return DbType.Sqlite;
                default:
                    throw new NotSupportedException($"不支持的数据库类型: {dbType}");
            }
        }

        /// <summary>
        /// 解析 SQLite 连接串：Data Source 是相对路径时，按 exe 目录（<see cref="AppDomain.BaseDirectory"/>）
        /// 拼成绝对路径，并确保所在目录存在；**绝对路径原样返回**。其他数据库类型不处理。
        /// 🔴 不能依赖 <see cref="Environment.CurrentDirectory"/>——OpenFileDialog 默认 RestoreDirectory=false，
        /// 打开一次文件对话框就会把当前目录改到上次的文件夹，此后相对路径会连到别处的空库。
        /// </summary>
        private static string ResolveConnectionString(DatabaseType dbType, string connectionString)
        {
            if (dbType != DatabaseType.Sqlite || string.IsNullOrWhiteSpace(connectionString))
                return connectionString;

            var builder = new SQLiteConnectionStringBuilder(connectionString);
            if (string.IsNullOrWhiteSpace(builder.DataSource))
                return connectionString;

            // 绝对路径：用户指定到哪就是哪，不碰
            if (!Path.IsPathRooted(builder.DataSource))
                builder.DataSource = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, builder.DataSource);

            // SqlSugar 官方说明：带目录的 Data Source 不会自动建文件夹，必须手动建
            string directory = Path.GetDirectoryName(builder.DataSource);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            return builder.ToString();
        }
    }
}
