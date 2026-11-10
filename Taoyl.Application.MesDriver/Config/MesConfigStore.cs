using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Config
{
    /// <summary>
    /// Mes.json 的文件读写。只管「字节 ↔ 模型」，不做快照、不做同步（由 MesConfigManager 持锁调用）。
    ///
    /// 落盘顺序照 ConfigService.AtomicWrite 的既有范式：
    /// 备份到 Backup/（多版本不删）→ 写 .tmp → File.Replace 原子替换（首次保存走 File.Move）。
    /// 加载失败：主文件解析不了就按时间戳倒序翻 Backup，全都不行才按空配置启动。
    /// 全程不抛——配置文件坏了不能让整个 MES 驱动不可用。
    /// </summary>
    internal class MesConfigStore
    {
        private const string BackupFolderName = "Backup";
        private const string BackupFilePrefix = "Mes_";

        private readonly string filePath;

        public MesConfigStore(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                throw new ArgumentException("MES 配置文件路径不能为空", nameof(filePath));

            this.filePath = filePath;
        }

        public string FilePath => filePath;

        /// <summary>最近一次 Load 的异常描述（成功为 null）。供组合根写启动日志。</summary>
        public string LastLoadError { get; private set; }

        /// <summary>最近一次 Save 的异常描述（成功为 null）。</summary>
        public string LastSaveError { get; private set; }

        #region 加载

        public MesConfig Load()
        {
            LastLoadError = null;

            if (!File.Exists(filePath))
                return new MesConfig();   // 首次启动：空配置，界面可新建并保存

            try
            {
                MesConfig config = Deserialize(File.ReadAllText(filePath), out string duplicateNote);
                LastLoadError = duplicateNote;   // null = 无重复，保持「加载成功」语义
                return config;
            }
            catch (Exception ex)
            {
                return LoadFromBackup("主配置解析失败（" + ex.Message + "）");
            }
        }

        /// <summary>按时间戳倒序翻备份，取第一份能解析的；全坏则按空配置启动。</summary>
        private MesConfig LoadFromBackup(string reason)
        {
            string dir = Path.Combine(GetConfigDirectory(), BackupFolderName);
            if (!Directory.Exists(dir))
                return EmptyStart(reason, "无备份目录");

            string[] files = Directory.GetFiles(dir, BackupFilePrefix + "*.json");
            if (files.Length == 0)
                return EmptyStart(reason, "备份目录为空");

            // 文件名带时间戳，Ordinal 升序即时间序；倒序从最新开始试
            Array.Sort(files, StringComparer.Ordinal);
            for (int i = files.Length - 1; i >= 0; i--)
            {
                try
                {
                    MesConfig config = Deserialize(File.ReadAllText(files[i]), out string duplicateNote);
                    LastLoadError = reason + "；已回退到备份 " + Path.GetFileName(files[i])
                                  + (duplicateNote == null ? string.Empty : "；" + duplicateNote);
                    return config;
                }
                catch
                {
                    // 这份也坏了，继续往前翻
                }
            }

            return EmptyStart(reason, "备份文件全部无法解析");
        }

        private MesConfig EmptyStart(string reason, string detail)
        {
            LastLoadError = reason + "；" + detail + "，已按空配置启动";
            return new MesConfig();
        }

        /// <summary>
        /// 反序列化 + 逐层兜底。
        /// duplicateNote：同名条目被丢弃时的描述（无重复为 null），由调用方并入 LastLoadError。
        /// </summary>
        private static MesConfig Deserialize(string text, out string duplicateNote)
        {
            MesConfig config = string.IsNullOrWhiteSpace(text)
                ? new MesConfig()
                : JsonConvert.DeserializeObject<MesConfig>(text) ?? new MesConfig();

            config.Apis = config.Apis ?? new List<MesApiDefinition>();
            config.Apis.RemoveAll(api => api == null || string.IsNullOrWhiteSpace(api.Name));

            duplicateNote = RemoveDuplicateNames(config.Apis);
            config.Apis.ForEach(Normalize);
            return config;
        }

        /// <summary>
        /// 同名条目只保留第一条，其余丢弃并把名字返回给调用方。
        /// 🔴 加载侧也要管：FindApi 取第一条、Delete 删全部，所以「重复条目存在」的真实后果是
        /// 「改的那条永不生效、删一次删两条」。写侧 Save 已查重，但手改 JSON 复制一条忘改名
        /// 只有加载侧看得到——那是最后一道。保留首条与 FindApi 的取值规则一致。
        /// 🔴 只丢不报等于配置静默少条目，所以被丢弃的名字必须带出去（调用方写进 LastLoadError）。
        /// </summary>
        private static string RemoveDuplicateNames(List<MesApiDefinition> apis)
        {
            if (apis.Count < 2)
                return null;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            var kept = new List<MesApiDefinition>(apis.Count);
            var dropped = new List<string>();

            foreach (MesApiDefinition api in apis)
            {
                if (seen.Add(api.Name))
                    kept.Add(api);
                else
                    dropped.Add(api.Name);
            }

            if (dropped.Count == 0)
                return null;

            apis.Clear();
            apis.AddRange(kept);

            return "丢弃重复接口名 " + dropped.Count + " 条（保留首条）: " + string.Join(", ", dropped.ToArray());
        }

        /// <summary>单条定义逐层兜底：手改 JSON 缺项回落默认值，非法枚举回落默认值。</summary>
        private static void Normalize(MesApiDefinition api)
        {
            api.QueryParams = api.QueryParams ?? new List<MesHeader>();
            api.Headers = api.Headers ?? new List<MesHeader>();
            api.Auth = api.Auth ?? new MesAuthConfig();
            api.Retry = api.Retry ?? new MesRetryPolicy();

            if (!Enum.IsDefined(typeof(MesHttpMethod), api.Method))
                api.Method = MesHttpMethod.Post;
            if (!Enum.IsDefined(typeof(MesBodyType), api.BodyType))
                api.BodyType = MesBodyType.Json;

            // 🔴 Auth.Type 必须校验：ResolveAuth 只认枚举里这四个值，未识别的 Type 落进 default →
            // 返回 null（= 鉴权不生效）→ 调用方既不摘头也不注入，请求带着使用者手配的 Authorization 发出。
            // 归 None 而不是留原值：让"非法值"在配置层就被抹平，跑起来与 None 完全一致且结果可见。
            if (!Enum.IsDefined(typeof(MesAuthType), api.Auth.Type))
                api.Auth.Type = MesAuthType.None;
            if (!Enum.IsDefined(typeof(MesKeyIn), api.Auth.KeyIn))
                api.Auth.KeyIn = MesKeyIn.Header;

            if (api.TimeoutMs <= 0)
                api.TimeoutMs = MesDefaults.TimeoutMs;
            if (api.Retry.MaxAttempts <= 0)
                api.Retry.MaxAttempts = MesDefaults.MaxAttempts;
            if (api.Retry.BackoffMs < 0)
                api.Retry.BackoffMs = MesDefaults.BackoffMs;
        }

        #endregion

        #region 保存

        public bool Save(MesConfig config)
        {
            if (config == null)
                return false;

            try
            {
                Directory.CreateDirectory(GetConfigDirectory());
                BackupExisting();

                File.WriteAllText(filePath + ".tmp", JsonConvert.SerializeObject(config, Formatting.Indented));

                // 原子替换（备份已在 BackupExisting 完成）：写一半断电时磁盘上要么新文件要么旧文件
                if (File.Exists(filePath))
                    File.Replace(filePath + ".tmp", filePath, null);
                else
                    File.Move(filePath + ".tmp", filePath);

                LastSaveError = null;
                return true;
            }
            catch (Exception ex)
            {
                LastSaveError = ex.Message;
                TryDeleteTemp();
                return false;
            }
        }

        /// <summary>多版本备份 Mes_yyyyMMddHHmmss.json，不删旧的；同秒连点加序号而不是覆盖。</summary>
        private void BackupExisting()
        {
            if (!File.Exists(filePath))
                return;

            string dir = Path.Combine(GetConfigDirectory(), BackupFolderName);
            Directory.CreateDirectory(dir);

            string stamp = DateTime.Now.ToString("yyyyMMddHHmmss");
            string path = Path.Combine(dir, BackupFilePrefix + stamp + ".json");

            for (int seq = 1; File.Exists(path); seq++)
                path = Path.Combine(dir, BackupFilePrefix + stamp + "_" + seq + ".json");

            File.Copy(filePath, path);
        }

        private void TryDeleteTemp()
        {
            try
            {
                string tmpPath = filePath + ".tmp";
                if (File.Exists(tmpPath))
                    File.Delete(tmpPath);
            }
            catch
            {
                // 临时文件删不掉不影响正确性，下次保存会覆盖
            }
        }

        private string GetConfigDirectory()
        {
            string dir = Path.GetDirectoryName(filePath);
            return string.IsNullOrEmpty(dir) ? "." : dir;
        }

        #endregion
    }
}
