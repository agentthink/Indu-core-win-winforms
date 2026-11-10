using System;

namespace Taoyl.Application.TaskProcess.Models
{
    /// <summary>
    /// 步序执行结果，由子类的 ExecuteStep 返回，指导基类如何切换步序或状态。
    /// 
    /// 设计原则：
    /// 1. Next/Stay/Jump/Complete 是正常业务流转
    /// 2. Exception 是统一异常入口（替代原有的 Alarm/Fault）
    ///    异常信息外化：通过 ExceptionCode + Message 描述
    /// </summary>
    public class ProcessStepResult
    {
        public enum StepAction
        {
            /// <summary>步序正常推进（+10）</summary>
            Next,

            /// <summary>保持当前步序，下次循环重试</summary>
            Stay,

            /// <summary>跳转到指定步序</summary>
            Jump,

            /// <summary>触发异常，进入 Exception 子状态（复位后可恢复）</summary>
            Exception,

            /// <summary>流程完成，优雅退出</summary>
            Complete
        }

        public StepAction Action { get; }
        public int? TargetStep { get; }
        public string Message { get; }
        public string ExceptionCode { get; }

        private ProcessStepResult(StepAction action, int? targetStep = null,
            string message = null, string exceptionCode = null)
        {
            Action = action;
            TargetStep = targetStep;
            Message = message;
            ExceptionCode = exceptionCode;
        }

        // ---------- 工厂方法 ----------

        public static ProcessStepResult Next()
        {
            return new ProcessStepResult(StepAction.Next);
        }

        public static ProcessStepResult Stay()
        {
            return new ProcessStepResult(StepAction.Stay);
        }

        public static ProcessStepResult Jump(int step)
        {
            return new ProcessStepResult(StepAction.Jump, targetStep: step);
        }

        public static ProcessStepResult Complete()
        {
            return new ProcessStepResult(StepAction.Complete);
        }

        /// <summary>
        /// 统一异常入口（替代原有的 Alarm/Fault）
        /// 异常信息外化：通过 code + message 描述
        /// </summary>
        /// <param name="code">故障码（如 "E101"）</param>
        /// <param name="message">异常描述</param>
        public static ProcessStepResult Exception(string code, string message)
        {
            return new ProcessStepResult(StepAction.Exception,
                exceptionCode: code, message: message);
        }
    }
}