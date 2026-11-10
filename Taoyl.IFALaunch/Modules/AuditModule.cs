// 对应类库：Taoyl.Application.Audit
using Autofac;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Repository;
using Taoyl.Application.Audit.Services;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 审计留痕模块：仓储 + 服务（读写两侧各一套契约），均单例。
    /// UI 与业务只注入 IAuditService / IAuditQueryService，**不认识仓储**。
    /// </summary>
    public class AuditModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // 写侧仓储：头 + 明细同事务收口（实体进出）
            builder.RegisterType<AuditRepository>()
                   .As<IAuditRepository>()
                   .SingleInstance();

            // 读侧仓储：分页 / 钻取 / 展开取数（实体进出）
            builder.RegisterType<AuditQueryRepository>()
                   .As<IAuditQueryRepository>()
                   .SingleInstance();

            // 写侧服务：经仓储写两表，DB 失败降级本地 jsonl
            builder.RegisterType<AuditService>()
                   .As<IAuditService>()
                   .SingleInstance();

            // 读侧服务：与写侧分契约（读失败必须上抛）；负责实体→DTO 转换与展开长表聚合
            builder.RegisterType<AuditQueryService>()
                   .As<IAuditQueryService>()
                   .SingleInstance();
        }
    }
}
