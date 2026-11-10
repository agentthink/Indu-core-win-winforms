using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Mapster;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Alarm.Models.Entities;
using Taoyl.Application.LogDriver;
using Taoyl.Application.Share.Eventing;

namespace Taoyl.Application.Alarm.Services
{
    /// <summary>
    /// 报警服务实现（进程内单例）。
    ///
    /// 写路径不对称是有意设计：
    /// - INSERT 高频 → Raise 入队（满 1000 丢弃 + LogError）+ IsBackground 消费线程唯一落库，
    ///   调用方（looper 线程等）微秒级零 IO，落库后广播已落库记录（DTO 本体，Id 真值）；
    /// - Ack 低频人工动作 → 同步直写，永不抛（Audit 规则：确认失败不反噬 UI，漏记日志可查）。
    ///
    /// 永不抛承诺：Raise / Ack 内部消化全部异常，报警链路故障不反噬业务线程。
    /// 读侧（Query）策略与写侧相反：查询失败必须上抛让 UI 看见。
    /// 退出语义：消费线程 IsBackground 随进程退，退出瞬间未落库的毫秒窗口记录丢失，第一版接受。
    /// </summary>
    public class AlarmService : IAlarmService
    {
        private const int QueueCapacity = 1000;

        private readonly BlockingCollection<AlarmRecordDto> pending =
            new BlockingCollection<AlarmRecordDto>(QueueCapacity);

        private readonly IEventBus eventBus;
        private readonly IAlarmRepository repository;
        private readonly Thread consumer;

        public AlarmService(IEventBus eventBus, IAlarmRepository repository)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));

            consumer = new Thread(ConsumeLoop)
            {
                IsBackground = true,
                Name = "AlarmService.Consumer"
            };
            consumer.Start();
        }

        /// <summary>入队即返回（RaiseTime 入队时刻补）。满容量 / 任何异常一律丢弃 + LogError，绝不上抛。</summary>
        public void Raise(AlarmRecordDto alarm)
        {
            if (alarm == null) return;

            try
            {
                alarm.RaiseTime = DateTime.Now;

                if (!pending.TryAdd(alarm))
                {
                    LogHelp.Error(null, "报警队列已满（容量 " + QueueCapacity + "），报警被丢弃："
                        + "[Source=" + alarm.Source + "][Code=" + alarm.Code + "][Type=" + alarm.Type + "]");
                }
            }
            catch (Exception ex)
            {
                LogHelp.Error(ex, "报警入队失败（已丢弃）");
            }
        }

        /// <summary>同步直写补处理列，永不抛。</summary>
        public bool Ack(AlarmRecordDto record)
        {
            if (record == null || record.Id <= 0) return false;

            try
            {
                return repository.Update(record.Adapt<AlarmRecord>()) > 0;
            }
            catch (Exception ex)
            {
                LogHelp.Error(ex, "报警处理确认落库失败 [Id=" + record.Id + "]");
                return false;
            }
        }

        public List<AlarmRecordDto> Query(AlarmQueryFilter filter)
        {
            return repository.Query(filter).Adapt<List<AlarmRecordDto>>();
        }

        /// <summary>消费循环（唯一 INSERT 路径）：出队 → 落库（Id 回填）→ 抄回原 dto → 广播。</summary>
        private void ConsumeLoop()
        {
            foreach (var dto in pending.GetConsumingEnumerable())
            {
                try
                {
                    var entity = dto.Adapt<AlarmRecord>();
                    repository.Insert(entity);

                    // 抄回原 dto（不是 new）：报警弹窗/查询页与总线订阅方共享该实例，须同步反映落库 Id
                    // （流程域快照已改 ProcessFault，不再共享本实例——B4）
                    dto.Id = entity.Id;

                    // 出现在总线上即已落库（真 Id）；订阅方 handler 必须微秒级返回（入队 / Post）
                    eventBus.Publish(dto);
                }
                catch (Exception ex)
                {
                    // 落库失败：Id 未回填，本条丢弃（不回队列防死循环），线程保活
                    LogHelp.Error(ex, "报警落库失败（已丢弃）[Source=" + dto.Source
                        + "][Code=" + dto.Code + "]");
                }
            }
        }
    }
}
