// 对应类库：全业务模块（组合根装配）——Mapster 全局配置的唯一固化点
using Mapster;
using Taoyl.Application.Alarm.Mapping;
using Taoyl.Application.Audit.Mapping;
using Taoyl.Application.AuthService.Mapping;
using Taoyl.Application.Product.Mapping;

namespace Taoyl.IFALaunch.Mapping
{
    /// <summary>
    /// Mapster 全局装配：开关 → Scan → Compile，三步顺序不可换，且必须在任何 Adapt 调用之前执行。
    /// 与 Modules/ 下各 Module 文件的分工：Module 管 DI 注册，本文件管映射配置，互不交叉。
    /// </summary>
    internal static class MappingBootstrapper
    {
        internal static void Configure()
        {
            var cfg = TypeAdapterConfig.GlobalSettings;

            // 两开关分工：前者查"已登记类型对内部漏字段"，后者查"该登记没登记"（含 Register 程序集漏扫）。
            cfg.RequireDestinationMemberSource = true;
            cfg.RequireExplicitMapping = true;

            // Scan 清单逐模块显式加行。
            cfg.Scan(
                typeof(AuditMappingRegister).Assembly,
                typeof(AlarmMappingRegister).Assembly,
                typeof(AuthMappingRegister).Assembly,
                typeof(ProductCellInfoMappingRegister).Assembly);

            cfg.Compile();   // ⚠️ 此后禁再改任何 TypeAdapterConfig。
        }
    }
}
