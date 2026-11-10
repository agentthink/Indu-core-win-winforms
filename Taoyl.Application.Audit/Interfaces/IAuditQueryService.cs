using System;
using System.Collections.Generic;
using Taoyl.Application.Audit.Models.Dtos;

namespace Taoyl.Application.Audit.Interfaces
{
    /// <summary>
    /// 审计查询契约（读侧，与 <see cref="IAuditService"/> 写侧分离）。
    ///
    /// 为什么读写分两个接口而不是合成一个：
    /// 写侧铁律是"永不抛异常、内部降级"（审计永不高于业务）；
    /// 读侧铁律恰恰相反——查询失败必须让用户看见（否则页面空白会让人误判"没有记录"）。
    /// 一个接口同时扛两种相反的异常策略，实现方迟早写歪。
    ///
    /// 只读铁律（方案 §4）：本契约**不提供**任何删除 / 修改 / 清空方法——
    /// 结构上不给口子，是防篡改第一道墙。
    /// </summary>
    public interface IAuditQueryService
    {
        /// <summary>
        /// 按五维筛选条件分页查询头表（时间倒序：最新操作在最上）。
        /// 查询动作本身不记审计。
        /// </summary>
        /// <param name="filter">筛选条件（五维 + 分页）</param>
        /// <returns>头表行 + 满足条件的总行数</returns>
        AuditPageResult Query(AuditQueryFilter filter);

        /// <summary>
        /// 取指定操作的全部字段级变更明细（主从钻取，按 OperationId）。
        /// </summary>
        /// <param name="operationId">头表 Id</param>
        List<AuditFieldChangeDto> GetChanges(long operationId);

        /// <summary>
        /// 按筛选条件取展开长表（每行一条明细；动作型操作 = 头信息一行、明细列留空）。
        /// 导出专用：不分页，全量命中结果（受单次导出上限保护，超限抛异常由 UI 提示缩小范围）。
        /// </summary>
        /// <param name="filter">筛选条件（分页字段忽略）</param>
        List<AuditFlatRowDto> QueryFlat(AuditQueryFilter filter);
    }
}
