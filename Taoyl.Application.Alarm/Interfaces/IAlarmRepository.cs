using System.Collections.Generic;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Alarm.Models.Entities;

namespace Taoyl.Application.Alarm.Interfaces
{
    /// <summary>
    /// 报警仓储：插入 / 更新 / 查询。只供 AlarmService 内部使用，UI 一律走 IAlarmService。
    /// </summary>
    public interface IAlarmRepository
    {
        /// <summary>插入一条记录，返回受影响行数；自增 Id 回填进实体。</summary>
        int Insert(AlarmRecord record);

        /// <summary>按实体更新，返回受影响行数。</summary>
        int Update(AlarmRecord record);

        /// <summary>条件查询（产生时间倒序；超出最大行数抛异常，由 UI 提示缩小范围）。</summary>
        List<AlarmRecord> Query(AlarmQueryFilter filter);
    }
}
