using System;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// 操作结果基类，用于表示成功或失败及其错误信息。
    /// </summary>
    public class OperateResult
    {
        /// <summary>是否成功</summary>
        public bool IsSuccess { get; set; }
        /// <summary>错误信息（成功时为 null）</summary>
        public string ErrorMessage { get; set; }

        /// <summary>创建一个成功的结果</summary>
        public static OperateResult Success()
        {
            return new OperateResult { IsSuccess = true };
        }

        /// <summary>创建一个失败的结果</summary>
        /// <param name="errorMessage">错误描述</param>
        public static OperateResult Fail(string errorMessage)
        {
            return new OperateResult { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }

    /// <summary>
    /// 带返回数据的操作结果。
    /// </summary>
    /// <typeparam name="T">返回数据的类型</typeparam>
    public class OperateResult<T> : OperateResult
    {
        /// <summary>返回的数据（操作成功时有效）</summary>
        public T Data { get; set; }

        /// <summary>创建一个带数据的成功结果</summary>
        /// <param name="data">要返回的数据</param>
        public static OperateResult<T> Success(T data)
        {
            return new OperateResult<T> { IsSuccess = true, Data = data };
        }

        /// <summary>创建一个带错误信息的失败结果</summary>
        /// <param name="errorMessage">错误描述</param>
        public new static OperateResult<T> Fail(string errorMessage)
        {
            return new OperateResult<T> { IsSuccess = false, ErrorMessage = errorMessage };
        }
    }
}