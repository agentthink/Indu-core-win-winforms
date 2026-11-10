using System.Collections.Generic;

namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// MES 配置根模型，对应 Config/Mes.json。
    /// </summary>
    public class MesConfig
    {
        /// <summary>配置格式版本，为将来格式演进留口子。当前恒为 1。</summary>
        public int Version { get; set; } = 1;

        /// <summary>
        /// 接口清单。
        /// 🔴 配置粒度是「工序 × 接口」而非「接口」：同一接口在不同工序下
        /// 固定字段（equip_code / process_code）取值不同，应配成多条独立记录
        /// （如 IF034_一次注液 / IF034_二次注液），而不是靠流程覆盖。
        /// </summary>
        public List<MesApiDefinition> Apis { get; set; } = new List<MesApiDefinition>();
    }
}
