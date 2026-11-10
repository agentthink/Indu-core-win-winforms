// 对应类库：Taoyl.Application.DataBase
using Autofac;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.DataBase;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 数据库模块：SqlSugarHelper 单例注册
    /// </summary>
    public class DataBaseModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // 延迟到首次 Resolve 时才读取配置，保证 InitConfigs 预加载后拿到内存缓存值
            builder.Register(c =>
            {
                var dev = c.Resolve<IConfigService>().Get<DevConfig>();
                return new SqlSugarHelper(new DbConnectionConfig
                {
                    DbType = dev.DB.DBType,
                    ConnectionString = dev.DB.ConnectionString
                });
            }).As<SqlSugarHelper>().SingleInstance();
        }
    }
}
