// 对应类库：Taoyl.Application.TranslateDriver + Taoyl.Application.FileUtil
// （ICsvService + CsvService 均在 Taoyl.Application.FileUtil；原 CSVTools 已并入 CsvService）
using Autofac;
using Taoyl.Application.FileUtil;
using Taoyl.Application.TranslateDriver.Interfaces;
using Taoyl.Application.TranslateDriver.Management;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 共享基座模块：零依赖纯工具类库的注册归并（翻译引擎、CSV）
    /// </summary>
    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // CSV 读写（CSVTools 已并入 CsvService：接口 + 实现在 FileUtil）
            builder.RegisterType<CsvService>().As<ICsvService>().SingleInstance();

            // 翻译服务（与 TransManager 同一单例；FrmResourceConfig 属性注入 IResourceManager；
            // VM 注入窄接口 ITranslationLanguageService 只消费语言状态/事件，不持有 WinForms 方法）
            builder.Register(c => TransManager.Current)
                   .As<ITranslationService>()
                   .As<ITranslationLanguageService>()
                   .As<IResourceManager>()
                   .SingleInstance();
        }
    }
}
