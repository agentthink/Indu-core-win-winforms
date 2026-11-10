// 对应类库：Taoyl.Application.Configuration
using Autofac;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Serialization;
using Taoyl.Application.Configuration.Services;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 配置模块：序列化器、通用配置服务、配方服务（开放泛型）
    /// </summary>
    public class ConfigModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // 序列化器
            builder.RegisterType<JsonConfigSerializer>()
                   .As<IConfigSerializer>()
                   .SingleInstance();

            // 通用配置服务
            builder.RegisterType<ConfigService>()
                   .As<IConfigService>()
                   .SingleInstance();

            // 配方服务（开放泛型，具体机型项目的 RecipeConfig 子类自动匹配）
            builder.RegisterGeneric(typeof(RecipeService<>))
                   .As(typeof(IRecipeService<>))
                   .SingleInstance();
        }
    }
}
