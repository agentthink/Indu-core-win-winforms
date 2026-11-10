using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using Taoyl.Application.Share.Eventing;

namespace Taoyl.Application.Business.Eventing
{
    /// <summary>
    /// 基于弱引用与订阅令牌的事件总线实现。
    /// 
    /// <b>设计要点：</b>
    /// - 总线内部仅持有 handler 的弱引用，因此不会阻止订阅者被 GC 回收。
    /// - 每个订阅分配唯一标识，令牌独立，同一委托实例多次订阅不会互相影响。
    /// - 订阅令牌持有 handler 强引用，确保在令牌存活期间订阅始终有效。
    /// - 添加订阅时自动清理已失效弱引用，防止不活跃事件类型积累死引用。
    /// - 发布时自动清理已失效弱引用，保证列表精简。
    /// - 递归发布保护：同一线程递归深度超过 10 次时中止。
    /// - 异常隔离：单个 handler 抛出异常不影响其他 handler。
    /// - 提供管理接口：获取存活订阅数、清空指定或全部事件类型。
    /// </summary>
    /// <example>
    /// <para><b>订阅方完整示例：</b></para>
    /// <code>
    /// public class SensorViewModel : IDisposable
    /// {
    ///     private readonly IDisposable dataSubscription;
    ///
    ///     public SensorViewModel(IEventBus eventBus)
    ///     {
    ///         // 使用字段保存令牌，不可用局部变量。
    ///         dataSubscription = eventBus.Subscribe&lt;SensorData&gt;(data => OnDataReceived(data));
    ///     }
    ///
    ///     private void OnDataReceived(SensorData data)
    ///     {
    ///         // 处理数据
    ///     }
    ///
    ///     public void Dispose()
    ///     {
    ///         // 主动取消订阅（可选），避免等待 GC。
    ///         dataSubscription?.Dispose();
    ///     }
    /// }
    /// </code>
    /// <para>当 <c>SensorViewModel</c> 实例被释放或垃圾回收时，订阅会自动从总线移除。</para>
    /// </example>
    public class EventBus : IEventBus
    {
        // 事件类型 -> 订阅列表
        private readonly ConcurrentDictionary<Type, HandlerList> handlers =
            new ConcurrentDictionary<Type, HandlerList>();

        // 递归深度计数，每个发布线程独立
        [ThreadStatic]
        private static int recursionDepth;

        private const int MaxRecursionDepth = 10;

        /// <inheritdoc />
        public IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class
        {
            if (handler == null) throw new ArgumentNullException(nameof(handler));

            var eventType = typeof(TEvent);
            var entry = handlers.GetOrAdd(eventType, _ => new HandlerList());
            return entry.Add(handler, this, eventType);
        }

        /// <inheritdoc />
        public void Publish<TEvent>(TEvent eventMessage) where TEvent : class
        {
            if (eventMessage == null) throw new ArgumentNullException(nameof(eventMessage));

            // 递归深度检查
            if (recursionDepth >= MaxRecursionDepth)
            {
                LogError($"[EventBus] 递归发布深度超过 {MaxRecursionDepth}，已中止事件：{typeof(TEvent).Name}");
                return;
            }

            var eventType = typeof(TEvent);
            if (!handlers.TryGetValue(eventType, out var entry))
                return;

            var liveHandlers = entry.GetLiveHandlers<TEvent>();
            if (liveHandlers.Count == 0)
                return;

            recursionDepth++;
            try
            {
                foreach (var handler in liveHandlers)
                {
                    try
                    {
                        handler.Invoke(eventMessage);
                    }
                    catch (Exception ex)
                    {
                        LogError($"[EventBus] Handler 执行异常 [{eventType.Name}]: {ex}");
                    }
                }
            }
            finally
            {
                recursionDepth--;
            }
        }

        /// <inheritdoc />
        public int GetHandlerCount<TEvent>() where TEvent : class
        {
            var eventType = typeof(TEvent);
            if (handlers.TryGetValue(eventType, out var entry))
            {
                return entry.GetLiveCount();
            }
            return 0;
        }

        /// <inheritdoc />
        public void Clear<TEvent>() where TEvent : class
        {
            var eventType = typeof(TEvent);
            if (handlers.TryGetValue(eventType, out var entry))
            {
                entry.Clear();
            }
        }

        /// <inheritdoc />
        public void ClearAll()
        {
            foreach (var kvp in handlers)
            {
                kvp.Value.Clear();
            }
        }

        /// <summary>
        /// 根据唯一标识移除指定事件类型中的某个订阅（由令牌 Dispose 时调用）。
        /// </summary>
        private void UnsubscribeById(Type eventType, int subscriptionId)
        {
            if (handlers.TryGetValue(eventType, out var entry))
            {
                entry.RemoveById(subscriptionId);
            }
        }

        private void LogError(string message)
        {
            // 生产环境请替换为 NLog、log4net 等
            System.Diagnostics.Trace.TraceError(message);
        }

        // ============================================================
        // 内部类：单个事件类型的订阅列表（读写锁 + 弱引用集合 + 唯一ID）
        // ============================================================
        private class HandlerList
        {
            // 订阅条目：唯一标识 + 弱引用
            private struct Entry
            {
                public readonly int Id;
                public readonly WeakReference<Delegate> WeakRef;

                public Entry(int id, WeakReference<Delegate> weakRef)
                {
                    Id = id;
                    WeakRef = weakRef;
                }
            }

            private readonly ReaderWriterLockSlim rwLock = new ReaderWriterLockSlim();
            private readonly List<Entry> entries = new List<Entry>();
            private int nextId;

            /// <summary>
            /// 添加一个 handler（弱引用），返回独立的订阅令牌。
            /// 同时自动清理所有已失效的弱引用，防止死引用累积。
            /// </summary>
            public IDisposable Add<T>(Action<T> handler, EventBus bus, Type eventType)
            {
                var weakRef = new WeakReference<Delegate>(handler);
                int id;

                rwLock.EnterWriteLock();
                try
                {
                    // 添加前清理所有死引用，保证列表精简
                    entries.RemoveAll(entry => !entry.WeakRef.TryGetTarget(out _));

                    id = nextId++;
                    entries.Add(new Entry(id, weakRef));
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }

                // 令牌持有强引用，保证 handler 不会在令牌存活期间被回收
                return new SubscriptionToken(bus, eventType, id, handler);
            }

            /// <summary>
            /// 根据唯一标识精确移除订阅，并同时清理已失效的弱引用。
            /// </summary>
            public void RemoveById(int id)
            {
                rwLock.EnterWriteLock();
                try
                {
                    entries.RemoveAll(entry =>
                        entry.Id == id || !entry.WeakRef.TryGetTarget(out _));
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }

            /// <summary>
            /// 获取所有存活的、类型匹配的 handler 副本，并清理已失效弱引用。
            /// </summary>
            public List<Action<T>> GetLiveHandlers<T>()
            {
                var result = new List<Action<T>>();
                bool hasDead = false;

                rwLock.EnterUpgradeableReadLock();
                try
                {
                    foreach (var entry in entries)
                    {
                        if (entry.WeakRef.TryGetTarget(out var target) && target is Action<T> action)
                        {
                            result.Add(action);
                        }
                        else
                        {
                            hasDead = true;
                        }
                    }

                    if (hasDead)
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            entries.RemoveAll(entry => !entry.WeakRef.TryGetTarget(out _));
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    rwLock.ExitUpgradeableReadLock();
                }

                return result;
            }

            /// <summary>
            /// 获取当前存活的订阅数量（同时清理死引用）。
            /// </summary>
            public int GetLiveCount()
            {
                int count = 0;
                bool hasDead = false;

                rwLock.EnterUpgradeableReadLock();
                try
                {
                    foreach (var entry in entries)
                    {
                        if (entry.WeakRef.TryGetTarget(out _))
                        {
                            count++;
                        }
                        else
                        {
                            hasDead = true;
                        }
                    }

                    if (hasDead)
                    {
                        rwLock.EnterWriteLock();
                        try
                        {
                            entries.RemoveAll(entry => !entry.WeakRef.TryGetTarget(out _));
                        }
                        finally
                        {
                            rwLock.ExitWriteLock();
                        }
                    }
                }
                finally
                {
                    rwLock.ExitUpgradeableReadLock();
                }

                return count;
            }

            /// <summary>
            /// 清空所有订阅（写锁）。
            /// </summary>
            public void Clear()
            {
                rwLock.EnterWriteLock();
                try
                {
                    entries.Clear();
                }
                finally
                {
                    rwLock.ExitWriteLock();
                }
            }
        }

        // ============================================================
        // 内部类：订阅令牌，持有 handler 强引用，通过唯一 ID 精确移除
        // ============================================================
        private sealed class SubscriptionToken : IDisposable
        {
            private readonly EventBus bus;
            private readonly Type eventType;
            private readonly int subscriptionId;
            private Delegate handlerRef;  // 强引用，保证 handler 不被 GC
            private bool disposed;

            public SubscriptionToken(EventBus bus, Type eventType, int subscriptionId, Delegate handler)
            {
                this.bus = bus;
                this.eventType = eventType;
                this.subscriptionId = subscriptionId;
                this.handlerRef = handler;
            }

            /// <summary>
            /// 精确移除本令牌对应的订阅，可重入。
            /// 调用后此令牌不再有效。
            /// </summary>
            public void Dispose()
            {
                if (disposed) return;
                disposed = true;

                if (handlerRef != null)
                {
                    bus.UnsubscribeById(eventType, subscriptionId);
                    handlerRef = null; // 释放强引用
                }
            }
        }
    }
}