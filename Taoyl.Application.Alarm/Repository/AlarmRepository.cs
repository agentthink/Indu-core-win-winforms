using System;
using System.Collections.Generic;
using SqlSugar;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Alarm.Models.Entities;
using Taoyl.Application.DataBase;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.Application.Alarm.Repository
{
    /// <summary>
    /// 报警仓储实现（SqlSugar）。
    /// </summary>
    public class AlarmRepository : IAlarmRepository
    {
        /// <summary>单次查询行数上限，超限抛异常由 UI 提示缩小范围。</summary>
        private const int MaxQueryRows = 50000;

        private readonly SqlSugarHelper dbHelper;

        public AlarmRepository(SqlSugarHelper dbHelper)
        {
            // 注入 SqlSugarHelper 而非 SqlSugarScope：前者 Db 是 Lazy，构造期不触发 DB 创建
            this.dbHelper = dbHelper ?? throw new ArgumentNullException(nameof(dbHelper));
        }

        public int Insert(AlarmRecord record)
        {
            // 自增 Id 回填进实体（总线广播 / Ack 按 Id 命中依赖真值）；
            // ExecuteReturnBigIdentity 而非 ExecuteReturnIdentity：主键 long，后者返回 int 溢出截断
            record.Id = dbHelper.Db.Insertable(record).ExecuteReturnBigIdentity();
            return 1;
        }

        public int Update(AlarmRecord record)
        {
            return dbHelper.Db.Updateable(record).ExecuteCommand();
        }

        public List<AlarmRecord> Query(AlarmQueryFilter filter)
        {
            var rows = BuildQuery(dbHelper.Db, filter)
                .OrderBy(r => r.RaiseTime, OrderByType.Desc)
                .OrderBy(r => r.Id, OrderByType.Desc)   // 同一秒内多条时保证顺序稳定
                .ToList();

            if (rows.Count > MaxQueryRows)
                throw new InvalidOperationException(
                    $"命中 {rows.Count} 条报警记录，超过单次查询上限 {MaxQueryRows} 条，请缩小时间范围后重试。");

            return rows;
        }

        /// <summary>
        /// 条件拼接（全部可空 = 不筛）。🔴 静态方法禁入表达式树：Type 文案先用局部变量算好再进表达式
        /// （审计同款红线）。
        /// </summary>
        private static ISugarQueryable<AlarmRecord> BuildQuery(SqlSugarScope db, AlarmQueryFilter filter)
        {
            var query = db.Queryable<AlarmRecord>();

            if (filter == null) return query;

            if (filter.TimeFrom.HasValue)
                query = query.Where(r => r.RaiseTime >= filter.TimeFrom.Value);

            if (filter.TimeTo.HasValue)
                query = query.Where(r => r.RaiseTime <= filter.TimeTo.Value);

            if (filter.Type.HasValue)
            {
                string typeText = filter.Type.Value.GetDescription();
                query = query.Where(r => r.Type == typeText);
            }

            return query;
        }
    }
}
