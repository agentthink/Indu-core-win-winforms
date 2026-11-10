using System;
using System.Collections.Generic;
using System.Linq;
using SqlSugar;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models.Dtos;
using Taoyl.Application.Audit.Models.Entities;
using Taoyl.Application.DataBase;

namespace Taoyl.Application.Audit.Repository
{
    /// <summary>
    /// 审计读侧仓储实现（SqlSugar）。只回答"怎么取"，实体→DTO 转换在服务层。
    /// </summary>
    public class AuditQueryRepository : IAuditQueryRepository
    {
        /// <summary>单次导出的头表行数上限——防止"不限时间范围"时先把头表全量 ToList 拉爆内存。</summary>
        private const int MaxFlatHeaders = 50000;

        /// <summary>单次导出的展开行数上限（头 × 明细），边拼边卡，最多分配这么多行。</summary>
        private const int MaxFlatRows = 200000;

        /// <summary>SQLite 变量上限 999，留足余量取 500。</summary>
        private const int IdBatchSize = 500;

        /// <summary>
        /// 单页行数上限——**收在实现里的结构保证，不靠调用方自觉**。
        /// 本表是服务端分页（SQL Skip/Take），而审计表长期只增不减；调用方一旦传个 int.MaxValue
        /// （"想看全部"的天真写法），就是把这堆留档一次性拉进内存。
        /// 看全量有专门的出口：导出（受 MaxFlatHeaders / MaxFlatRows 双上限保护，且独立权限码 + 留痕）。
        /// 取 1000 的依据：UI 正常档位是 10/20/50/100，留一个数量级余量给特殊排查场景。
        /// </summary>
        private const int MaxPageSize = 1000;

        /// <summary>每页行数为 0 / 负数时的默认值（与 <see cref="AuditQueryFilter.PageSize"/> 的初值一致）。</summary>
        private const int DefaultPageSize = 200;

        private readonly SqlSugarHelper dbHelper;

        public AuditQueryRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        #region 头表分页查询

        /// <summary>按五维筛选条件分页查询头表（时间倒序：最新操作在最上）。</summary>
        public AuditOperationPage Query(AuditQueryFilter filter)
        {
            filter = filter ?? new AuditQueryFilter();

            var db = dbHelper.Db;
            var pageIndex = filter.PageIndex < 1 ? 1 : filter.PageIndex;
            var pageSize = ClampPageSize(filter.PageSize);

            // Count 与取页各自独立构建查询（不共用同一个 ISugarQueryable 实例）：
            // 共用时 Count() 的聚合改写可能污染后续取页语句，独立构建只多一行成本，换风险归零。
            int totalCount = BuildHeaderQuery(db, filter).Count();

            var items = BuildHeaderQuery(db, filter)
                .OrderBy(o => o.Time, OrderByType.Desc)
                .OrderBy(o => o.Id, OrderByType.Desc)   // 同一秒内多条时保证顺序稳定
                .Skip((pageIndex - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new AuditOperationPage
            {
                TotalCount = totalCount,
                Items = items
            };
        }

        #endregion

        #region 明细钻取

        /// <summary>取指定操作的全部字段级变更明细。</summary>
        public List<AuditFieldChangeEntity> GetChanges(long operationId)
        {
            if (operationId <= 0) return new List<AuditFieldChangeEntity>();

            return dbHelper.Db.Queryable<AuditFieldChangeEntity>()
                .Where(c => c.OperationId == operationId)
                .OrderBy(c => c.Id)
                .ToList();
        }

        #endregion

        #region 展开长表（导出用）

        /// <summary>
        /// 按筛选条件取展开长表的原始材料（头表实体 + 分组明细）；不分页，超双层上限抛异常。
        /// </summary>
        public AuditFlatSource QueryFlat(AuditQueryFilter filter)
        {
            filter = filter ?? new AuditQueryFilter();

            var db = dbHelper.Db;

            var headers = BuildHeaderQuery(db, filter)
                .OrderBy(o => o.Time, OrderByType.Desc)
                .OrderBy(o => o.Id, OrderByType.Desc)
                .ToList();

            var source = new AuditFlatSource { Headers = headers };
            if (headers.Count == 0) return source;

            if (headers.Count > MaxFlatHeaders)
                throw new InvalidOperationException(
                    $"命中 {headers.Count} 条操作记录，超过单次导出上限 {MaxFlatHeaders} 条，请缩小时间范围后重试。");

            var details = LoadDetailsBatched(db, headers.Select(h => h.Id).ToList());
            source.Details = details;

            // 展开行数预检：头表行数不足以判定内存量（一个配置保存可产生上百条明细），
            // 故按"无明细头 ×1 + 有明细头 ×N"边算边抛
            int expandedRows = 0;
            foreach (var h in headers)
            {
                List<AuditFieldChangeEntity> changes;
                bool hasChanges = details.TryGetValue(h.Id, out changes) && changes.Count > 0;
                expandedRows += hasChanges ? changes.Count : 1;

                if (expandedRows > MaxFlatRows)
                    throw new InvalidOperationException(
                        $"导出展开行数超过上限 {MaxFlatRows} 行，请缩小时间范围后重试。");
            }

            return source;
        }

        /// <summary>
        /// 分批拉取明细并按 OperationId 分组。
        /// 明细查询分批（每批 IdBatchSize 个）：SQLite 的 SQL 变量数上限为 999，
        /// 若把导出范围的全部头表 Id 塞进一个 IN 子句，导出的时间范围一拉宽就会直接抛异常。
        /// 批次之间重复的 OperationId 用 TryGetValue 合并（同一 OperationId 不会跨批出现，此处仅为防御）。
        /// </summary>
        private static Dictionary<long, List<AuditFieldChangeEntity>> LoadDetailsBatched(
            SqlSugarScope db, List<long> operationIds)
        {
            var map = new Dictionary<long, List<AuditFieldChangeEntity>>();

            for (int i = 0; i < operationIds.Count; i += IdBatchSize)
            {
                var batch = operationIds.Skip(i).Take(IdBatchSize).ToList();

                var batchRows = db.Queryable<AuditFieldChangeEntity>()
                    .Where(c => batch.Contains(c.OperationId))
                    .OrderBy(c => c.Id)
                    .ToList();

                foreach (var row in batchRows)
                {
                    List<AuditFieldChangeEntity> list;
                    if (!map.TryGetValue(row.OperationId, out list))
                    {
                        list = new List<AuditFieldChangeEntity>();
                        map[row.OperationId] = list;
                    }
                    list.Add(row);
                }
            }

            return map;
        }

        #endregion

        #region 私有辅助

        // 🔴 红线：本 region 的静态方法**禁止**出现在 SqlSugar 表达式树里
        // （如 .Where(o => IsValid(o.Category))）——SQL 翻译阶段会直接抛异常。
        // 表达式树里只允许出现成员访问与闭包变量（见 BuildHeaderQuery 的写法）。

        /// <summary>
        /// 每页行数夹取：下界 1（0 / 负数按默认 200 处理），上界 <see cref="MaxPageSize"/>。
        /// </summary>
        private static int ClampPageSize(int value)
        {
            if (value < 1) return DefaultPageSize;
            return value > MaxPageSize ? MaxPageSize : value;
        }

        /// <summary>
        /// 五维筛选的公共查询构造（时间范围 / 页面 / 操作者 / 结果 / 动作）。
        /// </summary>
        private static ISugarQueryable<AuditOperationEntity> BuildHeaderQuery(
            SqlSugarScope db, AuditQueryFilter filter)
        {
            var query = db.Queryable<AuditOperationEntity>();

            if (filter.TimeFrom.HasValue)
            {
                var from = filter.TimeFrom.Value.Date;
                query = query.Where(o => o.Time >= from);
            }

            if (filter.TimeTo.HasValue)
            {
                // 结束时间含当天全天：日期选择器给的是 00:00，加一天再取 <，避免漏掉当天下午的记录
                var toExclusive = filter.TimeTo.Value.Date.AddDays(1);
                query = query.Where(o => o.Time < toExclusive);
            }

            // 页面 / 结果 / 动作三维收枚举：先翻成落库文案存进**局部变量**，再进表达式树——
            // 静态方法（AuditEnumText.Of）禁入表达式树，SQL 翻译阶段会直接抛异常（见本 region 顶部红线）。
            if (filter.Category.HasValue)
            {
                string category = AuditEnumText.Of(filter.Category.Value);
                query = query.Where(o => o.Category == category);
            }

            if (!string.IsNullOrWhiteSpace(filter.UserName))
            {
                string userName = filter.UserName.Trim();
                query = query.Where(o => o.UserName == userName);
            }

            if (filter.Result.HasValue)
            {
                string result = AuditEnumText.Of(filter.Result.Value);
                query = query.Where(o => o.Result == result);
            }

            if (filter.Action.HasValue)
            {
                string action = AuditEnumText.Of(filter.Action.Value);
                query = query.Where(o => o.Action == action);
            }

            return query;
        }

        #endregion
    }
}
