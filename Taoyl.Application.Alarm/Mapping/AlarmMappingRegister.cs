using Mapster;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Alarm.Models.Entities;

namespace Taoyl.Application.Alarm.Mapping
{
    /// <summary>
    /// 报警模块映射登记（Mapster IRegister）。由组合根 GlobalSettings.Scan(...) 统一装配。
    /// 本文件是全模块唯一的 TypeAdapterConfig 声明点；任何差异（Ignore / Map）都必须登记在此，
    /// 否则守卫脚本（data_access_guard.py 映射核对）报违规。
    /// </summary>
    public class AlarmMappingRegister : IRegister
    {
        public void Register(TypeAdapterConfig config)
        {
            // 双向登记：读侧 Query 走 Entity→DTO，写侧 ConsumeLoop / Ack 走 DTO→Entity。
            // 🔴 RequireExplicitMapping 下反向不登记即抛「no configuration exists」——只登单向会让整条写侧链路失效。
            // 两组均 9/9 全同构（名 + 类型逐一对应），严格成员校验下自动匹配，无差异声明。
            config.NewConfig<AlarmRecord, AlarmRecordDto>();
            config.NewConfig<AlarmRecordDto, AlarmRecord>();
        }
    }
}
