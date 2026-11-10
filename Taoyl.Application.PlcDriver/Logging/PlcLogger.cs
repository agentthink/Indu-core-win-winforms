using HslCommunication.LogNet;
using System;
using System.IO;

namespace Taoyl.Application.PlcDriver.Logging
{
    /// <summary>
    /// PLC日志记录器（实例级）
    /// 职责：包装 HSL LogNetDateTime，按小时切分文件，实例独立
    /// 构造参数为日志目录（HSL 在目录内生成 Logs_yyyyMMdd_HH.txt，非文件路径）；
    /// 目录解析统一走静态 ResolveDirectory（绝对配置优先，否则挂程序所在盘根）
    /// 内部实现使用 HSL 日志库，公共 API（IPlcLogger）零 HSL 类型
    /// 释放自防御：Dispose 置 _disposed，之后 Write* 静默丢弃——
    /// 覆盖 IoWorker 停等待时限内残留线程写日志的竞态（不阻塞等待线程退出）
    /// </summary>
    public class PlcLogger : IPlcLogger, IDisposable
    {
        private readonly ILogNet _logNet;
        private volatile bool _disposed = false;

        /// <summary>
        /// 解析日志目录（绝对路径配置优先；未配置/相对路径统一挂程序所在盘根）
        /// 规则：configuredPath 为绝对路径 → 原样使用；为空或相对 → Path.Combine(盘根, 相对路径)
        /// 盘根 = Path.GetPathRoot(BaseDirectory)（如 "D:\"）；取不到时回退 BaseDirectory 本身
        /// 用途：PlcRuntime.CreateLogger（配置 LogPath）与 ConfigManager（默认 Logs/PLC/...）共用同一规则
        /// </summary>
        /// <param name="configuredPath">配置的日志路径（可为 null/空白/相对/绝对）</param>
        /// <param name="defaultRelative">未配置时的默认相对路径（如 "Logs/PLC/PLC1"）</param>
        /// <returns>绝对日志目录</returns>
        public static string ResolveDirectory(string configuredPath, string defaultRelative)
        {
            string path = string.IsNullOrWhiteSpace(configuredPath) ? defaultRelative : configuredPath;
            if (Path.IsPathRooted(path))
                return path;

            string root = Path.GetPathRoot(AppDomain.CurrentDomain.BaseDirectory);
            if (string.IsNullOrEmpty(root))
                root = AppDomain.CurrentDomain.BaseDirectory;

            // GetFullPath 规范化 "./" 等相对前缀
            return Path.GetFullPath(Path.Combine(root, path));
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="logDirectory">日志存放目录（HSL LogNetDateTime 在目录内按小时生成 Logs_yyyyMMdd_HH.txt——
        /// 构造参数即目录而非文件路径，勿再拼文件名；目录不存在会自动创建）</param>
        /// <param name="debug">是否输出调试级别日志（false 默认 INFO）</param>
        public PlcLogger(string logDirectory, bool debug = false)
        {
            if (string.IsNullOrWhiteSpace(logDirectory))
                throw new ArgumentNullException(nameof(logDirectory));

            // 日志目录确保存在（HSL LogNetDateTime 内部按 GenerateMode 生成 Logs_yyyyMMdd_HH.txt 文件）
            Directory.CreateDirectory(logDirectory);

            _logNet = new LogNetDateTime(logDirectory, GenerateMode.ByEveryHour);
            _logNet.SetMessageDegree(debug ? HslMessageDegree.DEBUG : HslMessageDegree.INFO);
        }

        /// <summary>
        /// 记录调试日志
        /// </summary>
        public void WriteDebug(string message)
        {
            if (_disposed) return;
            _logNet.WriteDebug(message);
        }

        /// <summary>
        /// 记录信息日志
        /// </summary>
        public void WriteInfo(string message)
        {
            if (_disposed) return;
            _logNet.WriteInfo(message);
        }

        /// <summary>
        /// 记录警告日志
        /// </summary>
        public void WriteWarn(string message)
        {
            if (_disposed) return;
            _logNet.WriteWarn(message);
        }

        /// <summary>
        /// 记录错误日志
        /// </summary>
        public void WriteError(string message)
        {
            if (_disposed) return;
            _logNet.WriteError(message);
        }

        /// <summary>
        /// 记录异常日志
        /// </summary>
        public void WriteException(string message, Exception ex)
        {
            if (_disposed) return;
            _logNet.WriteException(message, ex);
        }

        /// <summary>
        /// 释放日志资源（置 _disposed：此后 Write* 静默丢弃，防残留线程写已释放句柄）
        /// </summary>
        public void Dispose()
        {
            _disposed = true;
            _logNet?.Dispose();
        }
    }
}
