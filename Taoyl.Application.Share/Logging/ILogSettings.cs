namespace Taoyl.Application.Share.Logging
{
    /// <summary>
    /// 日志系统全局设置契约（Share 中继：Configuration 侧的 LogConfig 实现它，
    /// LogDriver 侧的 LogTargetConfigurator 消费它）。
    /// 实现类用固定属性而非列表（编译期保证通道一个不少 + 递归判脏天然生效），
    /// 本接口用 GetPolicy(LogCategory) 查询式暴露 ⇒ 加类别不改接口。
    /// </summary>
    public interface ILogSettings
    {
        /// <summary>日志根目录（如 D:\Logs；每类别子目录挂其下）。</summary>
        string RootPath { get; }

        /// <summary>日志文件写入编码（全局统一，默认 Utf8Bom）。</summary>
        LogFileEncoding Encoding { get; }

        /// <summary>获取指定通道的落盘策略（实现类内部映射到固定属性）。</summary>
        ILogCategoryPolicy GetPolicy(LogCategory category);
    }
}
