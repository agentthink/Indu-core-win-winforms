namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// 重试策略。2 字段。
    ///
    /// 🔴 不含单次超时：超时是"这条接口能等多久"（<see cref="MesApiDefinition.TimeoutMs"/>），
    /// 本类只管"失败了再来几次"。两者共同决定最坏总耗时，公式见 MesApiDefinition.Retry。
    ///
    /// 🔴 MaxAttempts 默认 1 = 只发一次。过站上报类接口重试会造重复数据（服务端已处理但响应超时），
    /// 需要重试只能显式配大，且只应配在查询类接口上。
    ///
    /// 为什么没有 RetryOn：决定要不要重试的是「接口是否幂等」，不是「哪种错误」——切出来的每一格
    /// 答案都一样，这个轴不携带决策信息。现场也判断不了「500 该不该重试」。
    /// 为什么没有 BackoffMode：MaxAttempts 一般 2~3 次，指数退避要 5 次以上才有意义。
    /// </summary>
    public class MesRetryPolicy
    {
        /// <summary>总尝试次数（含首次）。1 = 不重试。</summary>
        public int MaxAttempts { get; set; } = MesDefaults.MaxAttempts;

        /// <summary>每次重试前的等待毫秒数，实际等待会加 ±20% 抖动（多流程同时重试会惊群）。</summary>
        public int BackoffMs { get; set; } = MesDefaults.BackoffMs;

        public MesRetryPolicy Clone()
        {
            return new MesRetryPolicy
            {
                MaxAttempts = MaxAttempts,
                BackoffMs = BackoffMs
            };
        }
    }
}
