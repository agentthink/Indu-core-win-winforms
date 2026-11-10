using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Taoyl.Application.Audit.Builder;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Interfaces;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.Audit.Models.Entities;

namespace Taoyl.Application.Audit.Services
{
    /// <summary>
    /// 审计服务实现（写侧）。
    ///
    /// 主通道：经 <see cref="IAuditRepository"/> 写头表 + 明细表（同一事务，保证头表存在则明细不丢）。
    /// 降级通道：DB 写失败时落本地 JSONL 文件（每行一条完整记录），**仅在失败时写**，平时零文件写入。
    /// 为什么降级不走 ILogService：①避免审计域反向依赖 LogDriver
    /// ②NLog 的 FormatMessage 在有 args 时会执行 string.Format，审计 JSON 里的花括号会直接抛 FormatException
    /// （本项目已踩过的坑）。
    ///
    /// 三条铁律的落地方式：
    /// 1. 宁漏记不记假账——Record 内部消化所有异常；调用方负责在业务写成功后投递。
    /// 2. 审计永不高于业务——不阻断、不弹窗、不重试、不回填；两条通道都失败就如实漏记。
    /// 3. 同步直写——挂点全在操作完成路径，方法返回即代表已落地或已降级。
    ///
    /// 依赖说明（结构约束，勿加）：**本类只依赖仓储**，不读会话、不读身份提供者、不读任何业务服务。
    /// ①身份快照由 AuditEntry 构造时传入（<see cref="AuditActor"/>，本模块自有类型，
    /// 不认识 AuthService 的 UserInfo）；②仓储内注入 SqlSugarHelper（其 Db 是 Lazy），构造期不触发 DB 创建。
    /// </summary>
    public class AuditService : IAuditService
    {
        private readonly IAuditRepository repository;

        /// <summary>降级文件目录（相对工作目录，与"刷卡日志"同惯例）</summary>
        private const string FallbackDir = "审计日志";

        public AuditService(IAuditRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        /// <summary>
        /// 投递一条审计记录。内部消化所有异常，永不上抛。
        /// </summary>
        public void Record(AuditEntry entry)
        {
            if (entry == null) return;

            try
            {
                var header = BuildHeader(entry);
                var changes = BuildChangeEntities(entry);

                try
                {
                    repository.Insert(header, changes);
                }
                catch (Exception dbEx)
                {
                    // 审计永不高于业务：DB 挂不阻断业务，改落本地文件兜底
                    WriteToFallbackFile(entry, header, changes, dbEx);
                }
            }
            catch
            {
                // 最外层兜底：连降级都失败则如实漏记，绝不把异常抛给业务方
            }
        }

        #region 变更采集（契约转发）

        /// <summary>
        /// 取模型基线快照。调用方只持有不透明的 <see cref="AuditBaseline"/>，
        /// 内部是 JToken 还是别的形式属实现事务。
        /// </summary>
        public AuditBaseline Snapshot(object model)
        {
            if (model == null) return null;
            return new AuditBaseline(AuditChangeBuilder.Snapshot(model));
        }

        /// <summary>
        /// 比对前后两态，产出字段级明细。
        ///
        /// 采集为何收敛到本契约：调用方（UI 命令层）过去直接 static 调用 AuditChangeBuilder，
        /// 被迫理解其实现细节（"快照必须在原地覆盖之前取"、"不取快照则前后是同一对象"），
        /// 这类注释散落在 6 个 VM 里 = 抽象泄漏。收敛后调用方只认识本契约，
        /// 换实现调用方零改动（详见契约注释）。
        ///
        /// 🔴 时序约束仍在调用方：<paramref name="before"/> 必须在业务代码原地覆盖之前取好 ——
        /// 这是业务时序，不是实现细节，契约无法代劳。
        /// </summary>
        public List<AuditFieldChange> BuildChanges(object before, object after, params string[] except)
        {
            if (before == null || after == null) return new List<AuditFieldChange>();

            // 基线取内部载荷；模型实例直接透传（Builder 自身判定走序列化还是反射路径）
            if (before is AuditBaseline baseline)
                return AuditChangeBuilder.Build(baseline.Payload, after, except);

            return AuditChangeBuilder.Build(before, after, except);
        }

        /// <summary>
        /// 比对两个集合，**逐项**展开差异明细（每个新增 / 移除的元素各占一条）。
        /// 用于 [JsonIgnore] 集合（角色列表、权限码集合等）—— 它们走 JSON 会被整个丢掉，
        /// 是通用 diff 的盲区（角色与权限差异恰恰是最该留痕的动作）。
        /// </summary>
        public List<AuditFieldChange> BuildSetChanges(string fieldPath, IEnumerable<string> before, IEnumerable<string> after)
        {
            return AuditChangeBuilder.BuildSetChanges(fieldPath, before, after);
        }

        #endregion

        #region 实体组装

        /// <summary>
        /// 投递请求 → 头表实体。
        ///
        /// 🔴 **枚举 → 落库文案的唯一转换点**：三个维度到此为止还是枚举，进了实体列就是中文文案
        /// （[Description]，见 <see cref="AuditEnumText"/>）。反过来读侧永不解析回枚举——
        /// 历史文案原样展示，改型前的旧数据 / 将来删掉的成员都不会读不出来。
        /// </summary>
        private static AuditOperationEntity BuildHeader(AuditEntry entry)
        {
            return new AuditOperationEntity
            {
                Time = DateTime.Now,
                UserName = Truncate(entry.ActorName, 50),
                RoleNames = Truncate(entry.ActorRoles, 200),
                RoleLevel = entry.ActorLevel,
                Category = AuditEnumText.Of(entry.Category),
                Action = AuditEnumText.Of(entry.Action),
                Target = Truncate(entry.Target, 100),
                Result = AuditEnumText.Of(entry.Result)
            };
        }

        /// <summary>投递请求 → 明细实体列表（实体组装；与对外契约的 BuildChanges 采集入口无关，勿混）</summary>
        private static List<AuditFieldChangeEntity> BuildChangeEntities(AuditEntry entry)
        {
            var list = new List<AuditFieldChangeEntity>();
            if (entry.Changes == null || entry.Changes.Count == 0) return list;

            foreach (var c in entry.Changes)
            {
                if (c == null) continue;

                list.Add(new AuditFieldChangeEntity
                {
                    // OperationId 在写库、拿到自增 Id 后回填
                    FieldPath = Truncate(c.FieldPath, 100),
                    ChangeType = AuditEnumText.Of(c.ChangeType),
                    OldValue = c.OldValue,
                    NewValue = c.NewValue
                });
            }

            return list;
        }

        /// <summary>
        /// 头表有长度约束的列做防御性截断（明细的 Old/New 是 TEXT，不截断——截断即失真）。
        /// </summary>
        private static string Truncate(string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) return value;
            return value.Length <= maxLength ? value : value.Substring(0, maxLength);
        }

        #endregion

        #region 降级通道 — 写本地文件

        /// <summary>
        /// DB 写失败时落本地 JSONL（一行一条记录，追加写）。
        /// 文件本身也失败就放弃（真漏，如实漏，不造假账）。
        /// </summary>
        private static void WriteToFallbackFile(AuditEntry entry, AuditOperationEntity header,
            List<AuditFieldChangeEntity> changes, Exception dbEx)
        {
            try
            {
                if (!Directory.Exists(FallbackDir))
                    Directory.CreateDirectory(FallbackDir);

                var payload = new
                {
                    time = header.Time.ToString("yyyy-MM-dd HH:mm:ss"),
                    userName = header.UserName,
                    roleNames = header.RoleNames,
                    roleLevel = header.RoleLevel,
                    // 降级文件与主通道**同规则**：都写字面中文文案，人工修表时两边可直接对照
                    category = AuditEnumText.Of(entry.Category),
                    action = AuditEnumText.Of(entry.Action),
                    target = entry.Target,
                    result = AuditEnumText.Of(entry.Result),
                    changes = BuildFallbackChanges(changes),
                    dbError = dbEx.Message
                };

                string line = JsonConvert.SerializeObject(payload) + Environment.NewLine;
                string file = Path.Combine(FallbackDir,
                    $"audit_fallback_{DateTime.Now:yyyyMMdd}.jsonl");

                File.AppendAllText(file, line, new UTF8Encoding(false));
            }
            catch
            {
                // 兜底通道也失败 = 真漏记。不抛、不重试、不回填。
            }
        }

        private static List<object> BuildFallbackChanges(List<AuditFieldChangeEntity> changes)
        {
            var list = new List<object>();
            foreach (var c in changes)
            {
                list.Add(new
                {
                    fieldPath = c.FieldPath,
                    changeType = c.ChangeType,
                    oldValue = c.OldValue,
                    newValue = c.NewValue
                });
            }
            return list;
        }

        #endregion
    }
}
