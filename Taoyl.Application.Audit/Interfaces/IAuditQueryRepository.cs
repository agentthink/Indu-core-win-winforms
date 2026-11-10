using System.Collections.Generic;
using Taoyl.Application.Audit.Models.Dtos;
using Taoyl.Application.Audit.Models.Entities;

namespace Taoyl.Application.Audit.Interfaces
{
    /// <summary>
    /// 审计读侧仓储（实体进出）。只读：结构上不给删除 / 修改 / 清空的口子。
    /// 只供 <see cref="Services.AuditQueryService"/> 调用；UI / VM 一律走 <see cref="IAuditQueryService"/>。
    /// </summary>
    public interface IAuditQueryRepository
    {
        /// <summary>按五维筛选条件分页查询头表（时间倒序），每页行数由实现夹到 [1, 单页上限]。</summary>
        AuditOperationPage Query(AuditQueryFilter filter);

        /// <summary>取指定操作的全部字段级变更明细（按 Id 升序）；入参 &lt;= 0 返回空列表。</summary>
        List<AuditFieldChangeEntity> GetChanges(long operationId);

        /// <summary>
        /// 取展开长表的原始材料（命中的头表实体 + 按 OperationId 分组的明细）；
        /// 不分页，超双层上限抛 <see cref="System.InvalidOperationException"/>。
        /// 聚合组装（头 × 明细 → 展开行）留服务层。
        /// </summary>
        AuditFlatSource QueryFlat(AuditQueryFilter filter);
    }

    /// <summary>
    /// 头表分页查询容器：当前页实体行 + 满足条件的总行数。
    /// 🔴 承载实体、非 DTO，勿迁 Models。
    /// </summary>
    public class AuditOperationPage
    {
        /// <summary>当前页头表实体（时间倒序）</summary>
        public List<AuditOperationEntity> Items { get; set; } = new List<AuditOperationEntity>();

        /// <summary>满足条件的总行数</summary>
        public int TotalCount { get; set; }
    }

    /// <summary>
    /// 展开长表原始材料容器：头表实体 + 按 OperationId 分组的明细实体。
    /// 🔴 承载实体、非 DTO，勿迁 Models。
    /// </summary>
    public class AuditFlatSource
    {
        /// <summary>命中的头表实体（时间倒序）</summary>
        public List<AuditOperationEntity> Headers { get; set; } = new List<AuditOperationEntity>();

        /// <summary>明细实体按 OperationId 分组（无明细的 Id 不在字典里）</summary>
        public Dictionary<long, List<AuditFieldChangeEntity>> Details { get; set; }
            = new Dictionary<long, List<AuditFieldChangeEntity>>();
    }
}
