using System;

namespace Taoyl.Application.Share.Eventing
{
    /// <summary>
    /// 轻量、线程安全、可自动清理的事件总线。
    /// 订阅方只需持有返回的令牌即可自动管理生命周期，无需手动保存委托引用。
    /// </summary>
    /// <example>
    /// <para><b>订阅方使用示例：</b></para>
    /// <code>
    /// public class MySubscriber : IDisposable
    /// {
    ///     private readonly IDisposable subscription;
    ///
    ///     public MySubscriber(IEventBus bus)
    ///     {
    ///         // 令牌必须保存为字段，不可使用局部变量。
    ///         subscription = bus.Subscribe&lt;MyEvent&gt;(e => OnEvent(e));
    ///     }
    ///
    ///     private void OnEvent(MyEvent e) { ... }
    ///
    ///     public void Dispose()
    ///     {
    ///         // 可选：立即取消订阅，不等待 GC。
    ///         subscription?.Dispose();
    ///     }
    /// }
    /// </code>
    /// <para>当订阅方实例被 GC 回收时，其持有的令牌也会被回收，订阅自动失效，无需担心内存泄漏。</para>
    /// </example>
    public interface IEventBus
    {
        /// <summary>
        /// 订阅指定类型的事件。
        /// </summary>
        /// <typeparam name="TEvent">事件类型，必须为引用类型。</typeparam>
        /// <param name="handler">事件处理委托。由返回的令牌对象内部保持强引用，订阅方无需额外保存。</param>
        /// <returns>一个 <see cref="IDisposable"/> 订阅令牌。调用方必须将其保存为字段（或等同生命周期的成员），
        /// 当令牌被显式释放或随订阅方一起被 GC 回收时，对应的订阅会被精确移除。</returns>
        /// <exception cref="ArgumentNullException">handler 为 null。</exception>
        IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : class;

        /// <summary>
        /// 同步发布事件。按订阅顺序调用所有存活 handler，任何 handler 的异常都会被隔离并记录，不会影响其他 handler。
        /// </summary>
        /// <typeparam name="TEvent">事件类型。</typeparam>
        /// <param name="eventMessage">事件消息体。</param>
        /// <exception cref="ArgumentNullException">eventMessage 为 null。</exception>
        void Publish<TEvent>(TEvent eventMessage) where TEvent : class;

        /// <summary>
        /// 获取指定事件类型当前存活的订阅数量。
        /// </summary>
        int GetHandlerCount<TEvent>() where TEvent : class;

        /// <summary>
        /// 清空指定事件类型的所有订阅。
        /// </summary>
        void Clear<TEvent>() where TEvent : class;

        /// <summary>
        /// 清空所有事件类型的所有订阅。
        /// </summary>
        void ClearAll();
    }
}
