using System;
using System.IO;
using Taoyl.Application.FileUtil;
using Taoyl.Application.MesDriver.Interfaces;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.Business.Mes
{
    /// <summary>
    /// IMesLogSink 的落盘实现：每次 MES 调用写一条完整记录到
    /// `{日志根目录}/Api/{接口名}/{接口名}_{yyyyMMdd}.csv`。
    ///
    /// 🔴 一个接口一个目录、一天一个文件：定位快（找某接口只进它自己的目录），单文件也不会无限增长。
    /// 🔴 保留期**不在本类、也不在日志清理器**：LogArchiveCleaner 只管日志类库那 5 个通道，
    /// 这棵树目前没有自动清理，会一直长——要治在别处做，别往日志清理器里塞。
    /// 🔴 与 `LogHelp.MES` 通道**互不相干**：通道是给流程侧记「特质化 MES 日志」的（人写一句话，上屏可见）；
    /// 本类是完整交互记录（9 列）。本类不再往通道写摘要——要上屏的自己调 `LogHelp.MES`。
    /// 🔴 写失败一律吞掉：日志不得改变调用结果（与 AuditService 同一条铁律）。
    /// 🔴 根目录由组合根传入（`LogConfig.RootPath`），本类不认识配置系统——它只需要一个字符串。
    /// </summary>
    public sealed class MesLogSink : IMesLogSink
    {
        private readonly ICsvService csvService;
        private readonly string rootPath;

        /// <param name="logRootPath">
        /// 日志根目录，来自 <c>LogConfig.RootPath</c>。可能是相对路径（如 "logs"），
        /// 这里按 BaseDirectory 展开——与 NLogLogService 同一规则。
        /// </param>
        public MesLogSink(ICsvService csvService, string logRootPath)
        {
            this.csvService = csvService ?? throw new ArgumentNullException(nameof(csvService));

            string root = string.IsNullOrWhiteSpace(logRootPath) ? "logs" : logRootPath.Trim();

            rootPath = Path.IsPathRooted(root)
                ? root
                : Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, root));
        }

        public void Write(MesResult result)
        {
            if (result == null)
                return;

            try
            {
                csvService.Record(ToRecord(result), ResolvePath(result.ApiName));
            }
            catch
            {
                // 日志落盘失败不改变调用结果（磁盘满 / 文件被占用 / 接口名极端超长）
            }
        }

        /// <summary>交互记录的顶层目录名：<c>{RootPath}/Api/{接口名}/…</c>。本类独占，不外借。</summary>
        private const string RecordsDirectory = "Api";

        /// <summary>
        /// `{RootPath}/Api/{接口名}/{接口名}_{yyyyMMdd}.csv`。
        /// 🔴 接口名必须先净化：它来自配置，可能含 `/ \ : * ?` 等非法字符，
        /// 不处理会直接被当成子目录或写失败。
        /// </summary>
        private string ResolvePath(string apiName)
        {
            string name = SafeFileName(apiName);
            string dir = Path.Combine(rootPath, RecordsDirectory, name);

            return Path.Combine(dir, string.Format("{0}_{1:yyyyMMdd}.csv", name, DateTime.Now));
        }

        private static string SafeFileName(string apiName)
        {
            string name = string.IsNullOrWhiteSpace(apiName) ? "未命名" : apiName.Trim();

            foreach (char c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }

        private static MesCallRecord ToRecord(MesResult result)
        {
            return new MesCallRecord
            {
                StartTime = result.StartTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                EndTime = result.EndTime.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                ElapsedMs = result.ElapsedMs,
                ApiName = result.ApiName,
                HttpStatus = result.HttpStatus.HasValue ? result.HttpStatus.Value.ToString() : string.Empty,
                // 成功行不给「无」这种噪音，留空即可
                Failure = result.Failure == MesFailureKind.None ? string.Empty : MesFailureText.Of(result.Failure),
                Attempts = result.Attempts,
                Request = result.Request,
                Response = result.Response
            };
        }
    }
}
