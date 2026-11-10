using System;

namespace Taoyl.Application.PlcDriver.Contracts
{
    /// <summary>
    /// PLC操作结果（自定义镜像，对标 HSL OperateResult）
    /// 职责：全驱动统一操作结果类型，替代 HSL OperateResult，公共 API 零 HSL 类型
    /// 语义：
    /// | 状态 | ErrorCode | 含义 |
    /// | ---- | --------- | ---- |
    /// | 成功 | 0 | 操作成功 |
    /// | 失败 | &lt; 0 | Socket/连接/超时等网络问题 |
    /// | 失败 | 10000（默认） | 库内部未分类错误，看 Message |
    /// | 失败 | &gt; 0 且 ≠ 10000 | PLC/设备返回的原生错误码 |
    /// </summary>
    public class ResponseResult
    {
        /// <summary>
        /// 是否成功（ErrorCode == 0）
        /// </summary>
        public bool IsSuccess => ErrorCode == 0;

        /// <summary>
        /// 错误码（0=成功，&lt;0=网络类错误，10000=默认未知错误）
        /// </summary>
        public int ErrorCode { get; }

        /// <summary>
        /// 结果描述信息
        /// </summary>
        public string Message { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        protected ResponseResult(int errorCode, string message)
        {
            ErrorCode = errorCode;
            Message = message ?? string.Empty;
        }

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static ResponseResult Success()
        {
            return new ResponseResult(0, string.Empty);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        /// <param name="message">错误描述</param>
        /// <param name="errorCode">错误码（默认10000，与 HSL OperateResult 一致）</param>
        public static ResponseResult Fail(string message, int errorCode = 10000)
        {
            return new ResponseResult(errorCode, message ?? string.Empty);
        }

        /// <summary>
        /// 结果文本描述（用于日志）
        /// </summary>
        public override string ToString()
        {
            return IsSuccess ? "操作成功" : $"操作失败[ErrorCode={ErrorCode}]: {Message}";
        }
    }

    /// <summary>
    /// 泛型结果的内容访问接口（非泛型视图）
    /// 用途：以 object 方式读取泛型结果内容，消除反射（高频轮询回写 tag.Value 等场景）
    /// 实现：ResponseResult&lt;T&gt; 显式实现，公共 API 不暴露 ContentObject，业务层仍用强类型 Content
    /// </summary>
    public interface IResponseResultContent
    {
        /// <summary>
        /// 内容（object 视图，值类型一次装箱）
        /// </summary>
        object ContentObject { get; }
    }

    /// <summary>
    /// 泛型操作结果（带内容）
    /// </summary>
    /// <typeparam name="T">内容类型</typeparam>
    public class ResponseResult<T> : ResponseResult, IResponseResultContent
    {
        /// <summary>
        /// 操作结果内容（强类型，业务层直接使用）
        /// </summary>
        public T Content { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ResponseResult(int errorCode, string message, T content)
            : base(errorCode, message)
        {
            Content = content;
        }

        /// <summary>
        /// 内容（object 视图，显式实现；值类型装箱一次，引用类型零装箱）
        /// </summary>
        object IResponseResultContent.ContentObject => Content;

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static  ResponseResult<T> Success(T content)
        {
            return new ResponseResult<T>(0, string.Empty, content);
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static new ResponseResult<T> Fail(string message, int errorCode = 10000)
        {
            return new ResponseResult<T>(errorCode, message ?? string.Empty, default);
        }
    }
}
