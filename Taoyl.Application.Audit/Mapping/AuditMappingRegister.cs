using Mapster;
using Taoyl.Application.Audit.Models.Dtos;
using Taoyl.Application.Audit.Models.Entities;

namespace Taoyl.Application.Audit.Mapping
{
    /// <summary>
    /// 审计模块映射登记（Mapster IRegister）。
    /// 由组合根 GlobalSettings.Scan(...) 统一装配（Bootstrapper 启动早期）。
    /// 🔴 本文件是全模块唯一的 TypeAdapterConfig 声明点；任何差异（Ignore / Map）
    /// 都必须登记在此，否则守卫脚本（data_access_guard.py 映射核对）报违规。
    /// </summary>
    public class AuditMappingRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 两组均 9/9、6/6 全同构（名 + 类型逐一对应），严格成员校验下自动匹配，无差异声明。
            // 审计只做读侧 Entity→DTO（写侧手工构造实体、无反向 Adapt），故不登记反向——
            // 下列「单向映射」登记即守卫脚本豁免"缺反向登记"检查的依据。
            // 单向映射: AuditOperationEntity
            // 单向映射: AuditFieldChangeEntity
            config.NewConfig<AuditOperationEntity, AuditOperationDto>();
            config.NewConfig<AuditFieldChangeEntity, AuditFieldChangeDto>();
        }
    }
}
