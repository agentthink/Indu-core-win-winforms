using System;
using System.Collections.Generic;
using Mapster;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models.Dtos;
using Taoyl.Application.Audit.Models.Entities;

namespace Taoyl.Application.Audit.Services
{
    /// <summary>
    /// 审计查询服务实现（读侧）。
    ///
    /// 与写侧策略相反：查询失败**必须上抛**让调用方（UI）看见——
    /// 否则页面一片空白会被误判成"这段时间没有记录"，那是比报错更危险的误导。
    ///
    /// 只读铁律：本类只有 Query / GetChanges / QueryFlat 三个方法，
    /// 结构上不存在任何删除 / 修改 / 清空的口子（防篡改第一道墙）。
    /// </summary>
    public class AuditQueryService : IAuditQueryService
    {
        private readonly IAuditQueryRepository repository;

        public AuditQueryService(IAuditQueryRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region 头表分页查询

        /// <summary>按五维筛选条件分页查询头表（时间倒序：最新操作在最上）。</summary>
        public AuditPageResult Query(AuditQueryFilter filter)
        {
            var page = repository.Query(filter);

            return new AuditPageResult
            {
                TotalCount = page.TotalCount,
                Items = page.Items.Adapt<List<AuditOperationDto>>()
            };
        }

        #endregion

        #region 明细钻取

        /// <summary>取指定操作的全部字段级变更明细。</summary>
        public List<AuditFieldChangeDto> GetChanges(long operationId)
        {
            if (operationId <= 0) return new List<AuditFieldChangeDto>();

            return repository.GetChanges(operationId).Adapt<List<AuditFieldChangeDto>>();
        }

        #endregion

        #region 展开长表（导出用）

        /// <summary>
        /// 按筛选条件取展开长表：每行一条明细；动作型操作（无明细）= 头信息一行、明细列留空。
        /// 受仓储侧双层上限保护，超限抛 <see cref="InvalidOperationException"/> 由 UI 提示缩小范围。
        /// </summary>
        public List<AuditFlatRowDto> QueryFlat(AuditQueryFilter filter)
        {
            var source = repository.QueryFlat(filter);

            var rows = new List<AuditFlatRowDto>();
            foreach (var h in source.Headers)
            {
                List<AuditFieldChangeEntity> changes;
                bool hasChanges = source.Details.TryGetValue(h.Id, out changes) && changes.Count > 0;

                if (!hasChanges)
                {
                    // 动作型操作：一行头信息，明细列留空
                    rows.Add(BuildFlatRow(h, null));
                }
                else
                {
                    foreach (var c in changes)
                    {
                        rows.Add(BuildFlatRow(h, c));
                    }
                }
            }

            return rows;
        }

        #endregion

        #region 私有辅助

        /// <summary>头实体 + 明细实体（可空）→ 展开行 DTO。手工组装：聚合组装不是平移映射。</summary>
        private static AuditFlatRowDto BuildFlatRow(AuditOperationEntity h, AuditFieldChangeEntity c)
        {
            return new AuditFlatRowDto
            {
                Time = h.Time,
                UserName = h.UserName,
                RoleNames = h.RoleNames,
                RoleLevel = h.RoleLevel,
                Category = h.Category,
                Action = h.Action,
                Target = h.Target,
                Result = h.Result,
                FieldPath = c?.FieldPath,
                ChangeType = c?.ChangeType,
                OldValue = c?.OldValue,
                NewValue = c?.NewValue
            };
        }

        #endregion
    }
}
