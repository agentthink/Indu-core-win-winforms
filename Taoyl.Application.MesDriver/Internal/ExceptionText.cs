using System;

namespace Taoyl.Application.MesDriver.Internal
{
    /// <summary>
    /// 异常文本工具：取最内层异常的消息。外层包装（Aggregate / TargetInvoke）只会污染失败原因，
    /// 现场该看到的是「无法解析主机」而不是「Exception has been thrown by the target of an invocation」。
    /// 不放 Taoyl.Application.Share——那会给本库引入第一个 ProjectReference，
    /// 而它只是 7 行的实现细节（internal，不出程序集）。
    /// </summary>
    internal static class ExceptionText
    {
        /// <summary>
        /// 取最内层异常的消息——外层包装（Aggregate / TargetInvoke）只会污染失败原因。
        /// 现场该看到的是「无法解析主机」这类根因，不是
        /// 「Exception has been thrown by the target of an invocation」。
        /// </summary>
        internal static string Innermost(Exception ex)
        {
            Exception current = ex;
            while (current.InnerException != null)
                current = current.InnerException;

            return current.Message;
        }
    }
}
