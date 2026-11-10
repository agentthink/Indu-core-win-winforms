using System.Collections.Generic;
using Taoyl.Application.Alarm.Models.Dtos;

namespace Taoyl.Application.Alarm.Interfaces
{
    /// <summary>
    /// 报警服务：UI 与业务模块的唯一入口。收报警（Raise）、处理确认（Ack）、历史查询。
    /// 写路径（Insert/Update）只在 <see cref="IAlarmRepository"/> 上，UI 拿不到仓储引用。
    /// </summary>
    public interface IAlarmService
    {
        /// <summary>
        /// 报警发生：入队即返回（容量 1000，满则丢弃 + LogError）；RaiseTime 入队时刻补。
        /// 消费线程落库拿到 Id 后广播已落库记录。
        /// </summary>
        void Raise(AlarmRecordDto alarm);

        /// <summary>报警处理确认：按 Id 补处理人员/时间并落库（低频人工动作，永不抛）；
        /// 返回 false = 落库失败（内部已记日志，调用方应提示并保留界面重试）。</summary>
        bool Ack(AlarmRecordDto record);

        /// <summary>条件查询（产生时间倒序；超出最大行数抛异常，由 UI 提示缩小范围）。</summary>
        List<AlarmRecordDto> Query(AlarmQueryFilter filter);
    }
}
