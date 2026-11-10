using NModbus;
using NModbus.Logging;
using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// 将 Modbus 原始报文异步写入本地 .txt 文件，并支持事件转发。
    /// 使用生产者-消费者队列避免阻塞业务线程。
    /// </summary>
    public class FileModbusLogger : ModbusLogger, IDisposable
    {
        private readonly string pathTemplate;
        private readonly BlockingCollection<string> queue = new BlockingCollection<string>();
        private readonly Task _writerTask;
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private bool _disposed;

        /// <summary>
        /// 日志事件：当有日志产生时触发，外部可订阅获取实时原始报文。
        /// </summary>
        public event EventHandler<string> LogReceived;

        /// <summary>
        /// 初始化文件日志器。
        /// </summary>
        /// <param name="pathTemplate">日志文件路径模板，支持 string.Format 占位符，如 @"Logs\ModbusLogs\dev{0:yyyyMMdd}.txt"（相对路径按程序目录展开，绝对路径原样使用）</param>
        /// <param name="minimumLoggingLevel">最低日志级别，建议 LoggingLevel.Trace 以捕获原始报文</param>
        public FileModbusLogger(string pathTemplate, LoggingLevel minimumLoggingLevel = LoggingLevel.Trace)
            : base(minimumLoggingLevel)
        {
            if (string.IsNullOrEmpty(pathTemplate))
                throw new ArgumentException("路径模板不能为空", nameof(pathTemplate));

            // 相对路径按程序目录展开：CurrentDirectory 会被 OpenFileDialog 改到上次打开的文件夹，
            // 原样存会把报文日志散到别处且不报错。绝对路径原样使用。
            this.pathTemplate = Path.IsPathRooted(pathTemplate)
                ? pathTemplate
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathTemplate);

            _writerTask = Task.Run(WriterLoop);
        }

        protected override void LogCore(LoggingLevel level, string message)
        {
            // 1. 触发事件（外部订阅者可以拿到原始报文）
            LogReceived?.Invoke(this, message);

            // 2. 异步写入队列
            string line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} [{level}] {message}";
            try
            {
                queue.Add(line);
            }
            catch (InvalidOperationException)
            {
                // 队列已关闭，忽略
            }
        }

        private void WriterLoop()
        {
            foreach (string line in queue.GetConsumingEnumerable(cts.Token))
            {
                try
                {
                    string filePath = string.Format(pathTemplate, DateTime.Now);
                    string dir = Path.GetDirectoryName(filePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    File.AppendAllText(filePath, line + Environment.NewLine, Encoding.UTF8);
                }
                catch (Exception)
                {
                    // 写文件失败不干扰主流程，仅记录（可选用Trace输出）
                }
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                cts.Cancel();
                queue.CompleteAdding();
                try { _writerTask.Wait(TimeSpan.FromSeconds(2)); } catch { }
                cts.Dispose();
                queue.Dispose();
            }
            _disposed = true;
        }
    }
}