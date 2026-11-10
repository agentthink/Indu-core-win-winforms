using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using Taoyl.Application.MesDriver.Interfaces;
using Taoyl.Application.MesDriver.Internal;
using Taoyl.Application.MesDriver.Models;
using Taoyl.Application.MesDriver.Request;
using Taoyl.Application.MesDriver.Transport;

namespace Taoyl.Application.MesDriver
{
    /// <summary>
    /// MES 调用实现。无状态——除注入的配置管理器与日志端口外不持可变字段，
    /// 多个流程在各自专用线程上并发调用是安全的。
    ///
    /// 一次 Invoke：
    /// Invoke（兜底，永不抛）→ Execute（查定义 → RequestTemplate.Create 装配 → 发送 → 组装）
    /// → SendWithRetry（每次重试按模板重建 request）→ Compose（唯一的 MesResult 组装点）。
    /// </summary>
    public class MesClient : IMesClient
    {
        private readonly IMesConfigManager config;
        private readonly IMesLogSink logSink;

        public MesClient(IMesConfigManager configManager, IMesLogSink logSink = null)
        {
            config = configManager ?? throw new ArgumentNullException(nameof(configManager));
            this.logSink = logSink;
        }

        public MesResult Invoke(string apiName, MesCallOptions options = null, CancellationToken ct = default)
        {
            // 查定义放在计时之外：ElapsedMs 只反映通信耗时，不含配置遍历
            return InvokeOnce(apiName, FindApi(config.Current, apiName), options, ct);
        }

        public MesResult Invoke(MesApiDefinition definition, CancellationToken ct = default)
        {
            // 临时定义自带 Body，不需要覆盖层
            return InvokeOnce(definition == null ? null : definition.Name, definition, null, ct);
        }

        /// <summary>
        /// 两个入口共用的外壳：计时 → 前置校验 → 装配发送 → 兜底 → 写日志。
        /// 🔴 日志放在这里而不是 Execute 里：任何路径（含前置失败与意外异常）都必须留痕。
        /// </summary>
        private MesResult InvokeOnce(string apiName, MesApiDefinition definition, MesCallOptions options, CancellationToken ct)
        {
            DateTime started = DateTime.Now;
            var watch = Stopwatch.StartNew();
            MesResult result;

            try
            {
                // Name 只读这一次：调用方给的定义是活对象，同一次调用读两次可能读到两个值。
                // 定义为 null 时退回 apiName（= 用户输入的原文，可能也是 null）。
                string name = definition == null ? apiName : definition.Name;

                // 生效的鉴权只算这一次：覆盖层赢，下游（AuthInjection / 校验）都认同一个值。
                // 🔴 校验必须查生效值而不是配置值：配置里 Auth.Type 写坏了、调用方用 options.Auth
                // 给了合法值，此时拦配置那条就是假警报——而「用覆盖层救一条坏配置」正是它的用途之一。
                MesAuthConfig auth = options?.Auth ?? definition?.Auth;

                if (definition == null)
                {
                    result = Compose(apiName, started, watch, string.Empty,
                        "接口未定义: " + (name ?? "(空定义)"), null, 1, MesFailureKind.ConfigError);
                }
                else if (string.IsNullOrWhiteSpace(name))
                {
                    // 配置侧加载时已剔除空白名条目，这条只挡 Invoke(definition)。
                    // Name 是 MesResult 唯一的归因键：允许空名等于允许日志里出现一条查不到主人的记录。
                    result = Compose(name, started, watch, string.Empty,
                        "接口名为空：临时定义也必须带 Name（用作日志归因）", null, 1, MesFailureKind.ConfigError);
                }
                else if (string.IsNullOrWhiteSpace(definition.Url))
                {
                    result = Compose(name, started, watch, string.Empty,
                        "接口地址为空: " + name, null, 1, MesFailureKind.ConfigError);
                }
                else if (auth != null && !Enum.IsDefined(typeof(MesAuthType), auth.Type))
                {
                    // 配置侧 Normalize 把非法 Auth.Type 静默抹成 None；定义重载不跑 Normalize，
                    // 而 AuthInjection.From 的 default 分支返回 null（= 鉴权不生效），
                    // 请求会带着「以为配了鉴权」的错觉裸奔发出。这里改成显式失败。
                    // 来源进文案：配置写错要改文件，调用方传错要改代码，动作不同。
                    string origin = options?.Auth != null ? "（调用覆盖）" : string.Empty;
                    result = Compose(name, started, watch, string.Empty,
                        "鉴权类型非法" + origin + ": " + (int)auth.Type, null, 1, MesFailureKind.ConfigError);
                }
                else
                {
                    result = Execute(name, definition, options, ct, started, watch);
                }
            }
            catch (Exception ex)
            {
                // 硬约束：Invoke 永不抛。ProcessBase 的 catch 会把异常转成 E999 异常态，等于停线。
                result = Compose(apiName, started, watch, string.Empty, "驱动异常: " + ExceptionText.Innermost(ex), null, 1, MesFailureKind.Unknown);
            }

            WriteLog(result);
            return result;
        }

        /// <summary>
        /// 装配 → 发送 → 组装。进到这里时 Name / Url 已保证非空。
        /// apiName 由 InvokeOnce 一次读出后传进来——不在这里再读 definition.Name：
        /// 调用方给的定义是活对象，同一次调用读两次可能读到两个值。
        /// </summary>
        private MesResult Execute(string apiName, MesApiDefinition definition, MesCallOptions options, CancellationToken ct, DateTime started, Stopwatch watch)
        {
            RequestTemplate template;
            try
            {
                template = RequestTemplate.Create(definition, options);
            }
            catch (Exception ex)
            {
                // 与 BuildAndSend 的 catch 同分类：两处都是「装配不出请求」，
                // 差别只是发生在装配阶段还是 HttpRequestMessage 构造。分类不一致会让同一个原因显示成两种文案。
                return Compose(apiName, started, watch, string.Empty, "请求构造失败: " + ExceptionText.Innermost(ex), null, 1, MesFailureKind.ConfigError);
            }

            string requestText = template.Render();
            Attempt outcome = SendWithRetry(template, ct);

            return Compose(apiName, started, watch, requestText, outcome.Body, outcome.Status, outcome.Attempts, outcome.Kind);
        }

        /// <summary>
        /// 唯一的 MesResult 组装点。
        /// attempts：未发起尝试的路径由调用方传 1；
        /// failure：拿到响应传 Attempt.Kind（必为 None），前置失败传 ConfigError，兜底传 Unknown。
        /// </summary>
        private static MesResult Compose(
            string apiName,
            DateTime started,
            Stopwatch watch,
            string request,
            string body,
            int? status,
            int attempts,
            MesFailureKind failure)
        {
            watch.Stop();

            // 🔴 EndTime 由 StartTime 推算，不取 DateTime.Now：
            // 工控现场 NTP 对时一跳，Now 与 Stopwatch 就打架，EndTime - StartTime ≠ ElapsedMs
            return new MesResult(
                apiName,
                started,
                started.AddMilliseconds(watch.ElapsedMilliseconds),
                watch.ElapsedMilliseconds,
                request ?? string.Empty,
                body ?? string.Empty,
                status,
                attempts,
                failure);
        }

        private static MesApiDefinition FindApi(MesConfig snapshot, string apiName)
        {
            if (snapshot == null || snapshot.Apis == null || string.IsNullOrEmpty(apiName))
                return null;

            foreach (MesApiDefinition api in snapshot.Apis)
            {
                if (api != null && MesApiDefinition.NameEquals(api.Name, apiName))
                    return api;
            }
            return null;
        }

        #region 发送与重试

        /// <summary>一次尝试的结果。只在 MesClient 内部流转，不进公开契约。</summary>
        private sealed class Attempt
        {
            public int? Status;
            public string Body;
            public MesFailureKind Kind;

            /// <summary>本次 outcome 是第几次尝试（1 起）。由 SendWithRetry 在每次 TryOnce 后写入。</summary>
            public int Attempts;
        }

        private Attempt SendWithRetry(RequestTemplate template, CancellationToken ct)
        {
            int max = template.MaxAttempts < 1 ? 1 : template.MaxAttempts;

            for (int attempt = 1; ; attempt++)
            {
                Attempt outcome = BuildAndSend(template, ct);

                // 计数在这里落地：下面两条 return（不再重试 / 退避被取消）都会带上真实尝试次数
                outcome.Attempts = attempt;

                if (attempt >= max || !ShouldRetry(outcome))
                    return outcome;

                // 退避等待可被取消打断：不用 Thread.Sleep
                int delay = GetDelayMs(template.BackoffMs);
                if (delay > 0 && ct.WaitHandle.WaitOne(delay))
                    return outcome;
            }
        }

        /// <summary>
        /// 构造 + 发送一次。构造失败也必须变成一个 Attempt，不能让它冒泡。
        /// 🔴 template.Build() 在 using 的资源表达式里求值——它抛异常时 TryOnce 根本不会被调用，
        /// 异常会冒到 Invoke 的兜底 catch，那里 request 传 string.Empty，
        /// Execute 里已算好的 template.Render() 白丢（调试页「请求」框空白，用户不知道发了什么）。
        /// 典型来源：手改 Mes.json 写了非法 URL，HttpRequestMessage 构造抛 UriFormatException。
        /// </summary>
        private Attempt BuildAndSend(RequestTemplate template, CancellationToken ct)
        {
            try
            {
                // 🔴 每次重试都要重建 request：.NET 不允许同一个 HttpRequestMessage 发送两次
                using (HttpRequestMessage request = template.Build())
                {
                    return TryOnce(request, template, ct);
                }
            }
            catch (Exception ex)
            {
                // 🔴 归 ConfigError 不归 Unknown：构造失败是配置问题（非法 URL / 非法 Method / 非法头名），
                // 重试多少次都一样，ShouldRetry 对 ConfigError 显式返回 false。
                return new Attempt
                {
                    Status = null,
                    Body = "请求构造失败: " + ExceptionText.Innermost(ex),
                    Kind = MesFailureKind.ConfigError
                };
            }
        }

        private Attempt TryOnce(HttpRequestMessage request, RequestTemplate template, CancellationToken ct)
        {
            try
            {
                HttpResult result = HttpTransport.Send(request, template.TimeoutMs, ct);
                return new Attempt { Status = result.StatusCode, Body = result.Body, Kind = MesFailureKind.None };
            }
            catch (HttpTransportException ex)
            {
                return new Attempt { Status = null, Body = ex.Message, Kind = ex.Kind };
            }
            catch (Exception ex)
            {
                // 兜住 Transport 之外的意外（ObjectDisposed / InvalidOperation 等）。
                // 同 BuildAndSend：不兜就会丢掉已渲染的请求文本。
                return new Attempt
                {
                    Status = null,
                    Body = "发送异常: " + ExceptionText.Innermost(ex),
                    Kind = MesFailureKind.Unknown
                };
            }
        }

        /// <summary>
        /// 重试条件。Timeout / NetworkError / 5xx 都重试——能不能重试由 MaxAttempts 表达，
        /// 不按错误类型二次筛选（见 MesRetryPolicy）。
        /// 🔴 ConfigError 必须显式列出，不能靠 default 兜：default 返回 true（给 Timeout / Network 用的），
        /// 而配置错误重试多少次结果都一样。
        /// </summary>
        private static bool ShouldRetry(Attempt outcome)
        {
            switch (outcome.Kind)
            {
                case MesFailureKind.Cancelled:
                case MesFailureKind.Unknown:
                case MesFailureKind.ConfigError:
                    return false;

                case MesFailureKind.None:
                    return outcome.Status.HasValue && outcome.Status.Value >= 500;

                default:   // Timeout / Network
                    return true;
            }
        }

        #endregion

        #region 收尾

        /// <summary>退避基准上限：防止 BackoffMs 配得过大把流程步序卡死。</summary>
        private const int MaxBackoffMs = 30000;

        /// <summary>±20% 抖动：多个流程同时重试会惊群，同时打向 MES。</summary>
        private const double JitterRatio = 0.2;

        [ThreadStatic]
        private static Random rng;

        /// <summary>
        /// 重试等待毫秒数：固定间隔 ±20% 抖动，基准值先夹到 30 秒（实际最长 36 秒）。
        /// 固定而非指数——MaxAttempts 一般配 2~3 次，指数退避要 5 次以上才有意义。
        /// ThreadStatic 防多流程并发损坏 Random；Guid 做种子防同一 tick 产生相同序列。
        /// 🔴 backoffMs ≤ 0 一律回落 MesDefaults.BackoffMs：配 0 并不会「不等待」，而是等一个默认间隔。
        /// </summary>
        private static int GetDelayMs(int backoffMs)
        {
            int baseDelay = backoffMs > 0 ? backoffMs : MesDefaults.BackoffMs;
            if (baseDelay > MaxBackoffMs)
                baseDelay = MaxBackoffMs;

            if (rng == null)
                rng = new Random(Guid.NewGuid().GetHashCode());

            double jitter = 1.0 + (rng.NextDouble() * 2.0 - 1.0) * JitterRatio;
            int result = (int)(baseDelay * jitter);
            return result < 0 ? 0 : result;
        }

        private void WriteLog(MesResult result)
        {
            if (logSink == null)
                return;

            try
            {
                logSink.Write(result);
            }
            catch
            {
                // 日志实现出错不能改变调用结果
            }
        }

        #endregion
    }
}
