using System.Collections.Generic;
using Taoyl.Application.Audit.Models.Entities;

namespace Taoyl.Application.Audit.Interfaces
{
    /// <summary>
    /// 审计写侧仓储（实体进出）。只供 <see cref="Services.AuditService"/> 调用；
    /// UI / VM 一律走 <see cref="IAuditService"/>。
    /// </summary>
    public interface IAuditRepository
    {
        /// <summary>
        /// 同一事务写入头表 + 明细表：头表插入后取回自增 Id，回填进每条明细再批量插入。
        /// </summary>
        /// <param name="header">头表实体（Id 由数据库自增生成）</param>
        /// <param name="details">明细实体列表（OperationId 由本方法回填）</param>
        void Insert(AuditOperationEntity header, List<AuditFieldChangeEntity> details);
    }
}
