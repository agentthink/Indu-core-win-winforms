// 对应类库：Taoyl.Application.AuthService
using Autofac;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Repositories;
using Taoyl.Application.AuthService.Services;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.DataBase;
using Taoyl.Application.Share.Config;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 认证授权模块：用户/角色/权限仓储（SqlSugar 单后端）、启动同步、
    /// 认证会话、权限服务、RBAC 迁移、刷卡登录及其配置视图
    /// </summary>
    public class AuthModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            RegisterRepositories(builder);
            RegisterServices(builder);
        }

        /// <summary>
        /// 仓储注册：SqlSugar 单后端，延迟到首次 Resolve 时构造（Db 为 Lazy，构造期不触发建库）
        /// </summary>
        private static void RegisterRepositories(ContainerBuilder builder)
        {
            builder.Register<IUserRepository>(c =>
                new DbUserRepository(c.Resolve<SqlSugarHelper>())).SingleInstance();

            builder.Register<IRoleRepository>(c =>
                new DbRoleRepository(c.Resolve<SqlSugarHelper>())).SingleInstance();

            builder.Register<IPermissionRepository>(c =>
                new DbPermissionRepository(c.Resolve<SqlSugarHelper>())).SingleInstance();

            builder.Register<IUserRoleRepository>(c =>
                new DbUserRoleRepository(c.Resolve<SqlSugarHelper>())).SingleInstance();

            builder.Register<IRolePermissionRepository>(c =>
                new DbRolePermissionRepository(c.Resolve<SqlSugarHelper>())).SingleInstance();

            // 注册启动同步服务
            builder.RegisterType<PermissionSyncService>()
                   .As<IPermissionSyncService>()
                   .SingleInstance();
        }

        /// <summary>
        /// 认证授权服务注册
        /// </summary>
        private static void RegisterServices(ContainerBuilder builder)
        {
            builder.RegisterType<UserService>()
                    .As<IUserSession>()
                    .As<IUserManager>()
                    .SingleInstance();

            builder.RegisterType<RbacMigrationService>()
                   .As<IRbacMigrationService>()
                   .SingleInstance();

            builder.RegisterType<AuthService>()
                   .As<IAuthService>()
                   .SingleInstance();

            builder.RegisterType<RoleService>()
                   .As<IRoleService>()
                   .SingleInstance();

            builder.RegisterType<PermissionService>()
                   .As<IPermissionService>()
                   .SingleInstance();

            builder.RegisterType<KeyboardHookService>()
                   .As<ICardLoginProvider>()
                   .SingleInstance();

            // 注册两个软配置窄接口，指向同一个 DevConfig 内存单例，消费方实时读取实现热更新
            builder.Register(c => c.Resolve<IConfigService>().Get<DevConfig>().AutoLogin)
                   .As<ICardLoginSettings>().SingleInstance();
            builder.Register(c => c.Resolve<IConfigService>().Get<DevConfig>())
                   .As<IAutoLogoutSettings>().SingleInstance();
        }
    }
}
