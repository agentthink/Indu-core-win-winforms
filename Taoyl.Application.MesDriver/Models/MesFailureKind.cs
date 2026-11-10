namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// 一次调用为什么失败。与 HttpStatus 正交：拿到响应（含 4xx / 5xx）时 HttpStatus 有值、
    /// 本枚举为 None；没拿到响应时 HttpStatus 为 null，由本枚举说明原因。
    ///
    /// 🔴 存在的理由：分类原本算完只喂「要不要重试」就丢了，调用方只能靠 Response 文案做字符串判断
    /// ——文案一改调用方就瞎。现在由驱动一次性分类，消费方只读枚举。
    /// </summary>
    public enum MesFailureKind
    {
        /// <summary>拿到响应（含 4xx / 5xx——"服务端说不行"也算拿到响应，不算传输失败）</summary>
        None = 0,

        /// <summary>请求超时（覆盖整个请求生命周期，含读响应体阶段）</summary>
        Timeout = 1,

        /// <summary>连不上，或响应读到一半被对端掐断</summary>
        Network = 2,

        /// <summary>调用方取消</summary>
        Cancelled = 3,

        /// <summary>配置或装配问题：接口未定义 / 地址为空 / 请求构造失败</summary>
        ConfigError = 4,

        /// <summary>其它未归类异常</summary>
        Unknown = 5
    }

    /// <summary>
    /// MesFailureKind 的展示文案。
    /// 单点维护：调试页状态条与通道日志共用这一份，避免两处各写一套 switch。
    /// ⚠️ 本枚举不落库，文案不构成存储契约——改文案只影响显示。
    /// </summary>
    public static class MesFailureText
    {
        public static string Of(MesFailureKind kind)
        {
            switch (kind)
            {
                case MesFailureKind.None: return "有响应";
                case MesFailureKind.Timeout: return "超时";
                case MesFailureKind.Network: return "连接失败";
                case MesFailureKind.Cancelled: return "已取消";
                case MesFailureKind.ConfigError: return "配置错误";
                case MesFailureKind.Unknown: return "驱动异常";
                default: return "未知";
            }
        }
    }
}
