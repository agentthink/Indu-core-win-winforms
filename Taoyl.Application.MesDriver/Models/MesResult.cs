using System;

namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// 一次接口调用的完整记录。9 字段，无嵌套类、无方法。
    /// 驱动只负责把「发了什么、收到什么、为什么失败」如实装进来，不判断业务成功与否。
    /// </summary>
    public class MesResult
    {
        /// <summary>调用的接口名（即 MesApiDefinition.Name）。</summary>
        public string ApiName { get; }

        /// <summary>首次尝试的开始时间（含重试时仍为第一次）。</summary>
        public DateTime StartTime { get; }

        /// <summary>结束时间。</summary>
        public DateTime EndTime { get; }

        /// <summary>总耗时（毫秒，含重试退避等待）。</summary>
        public long ElapsedMs { get; }

        /// <summary>
        /// 完整请求信息，六段结构化 JSON：method / url（最终地址）/ query（编码前原值）/ headers /
        /// contentType（null = 这条不发）/ body。由 RequestTemplate.Render 从同一份装配结果生成。
        /// 取舍：body 是原文字符串，本身是 JSON 时会转义成单行；不含框架自动追加的头。
        /// </summary>
        public string Request { get; }

        /// <summary>
        /// 响应正文，原样，不解析、不裁剪（超过 8MB 上限时截断并在尾部标注）。
        /// HttpStatus 为 null 时，本字段承载驱动侧的失败描述
        /// （「接口未定义: Xxx」「超时 15000ms」「连接失败: ...」「取消」）。
        /// </summary>
        public string Response { get; }

        /// <summary>
        /// HTTP 状态码。null = 没拿到响应（超时 / 连不上 / 接口名不存在 / 已取消）。
        /// </summary>
        public int? HttpStatus { get; }

        /// <summary>
        /// 失败归类。HttpStatus 有值时为 <see cref="MesFailureKind.None"/>（拿到响应了，4xx / 5xx 也算）；
        /// HttpStatus 为 null 时由本字段说明原因——不必再解析 Response 里的文案。
        /// </summary>
        public MesFailureKind Failure { get; }

        /// <summary>
        /// 总尝试次数（含首次）。1 = 一次成功/失败，&gt;1 = 经过重试。
        /// 未发起任何尝试的路径（接口未定义 / 地址为空 / 构造失败 / 驱动异常）统一记 1，
        /// 语义是「一次调用机会」——UI 只在 &gt;1 时展示，与 0 等效。
        /// </summary>
        public int Attempts { get; }

        public MesResult(
            string apiName,
            DateTime startTime,
            DateTime endTime,
            long elapsedMs,
            string request,
            string response,
            int? httpStatus,
            int attempts,
            MesFailureKind failure)
        {
            ApiName = apiName;
            StartTime = startTime;
            EndTime = endTime;
            ElapsedMs = elapsedMs;
            Request = request;
            Response = response;
            HttpStatus = httpStatus;
            Attempts = attempts;
            Failure = failure;
        }
    }
}
