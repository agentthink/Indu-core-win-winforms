using System;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Taoyl.Application.Audit.Builder
{
    /// <summary>
    /// 值的**格式化**与**脱敏** —— 明细旧值 / 新值落库前的最后一道加工。
    ///
    /// 两个方向的取舍相反，都写在这里是因为它们服务同一件事：**这条明细进库后能不能给人看**。
    /// - <see cref="Format"/>：跨机器可读（InvariantCulture），不受现场区域设置影响；
    /// - <see cref="IsSensitive"/>：fail-closed，宁可多脱敏一个值，不可漏一个凭据。
    /// </summary>
    internal static class AuditValueFormatter
    {
        /// <summary>
        /// 敏感字段**关键字**（大小写不敏感、与层级无关）：末段属性名**包含**任一关键字即命中，
        /// 命中则**只记变更事实、不记值**（新旧值都留空）。
        ///
        /// 审计表是长期留档、且查询页可导出 CSV——明文口令进表 = 把凭据写进一个可自由导出的明文资产。
        /// 留空的语义与 AuditFieldChange.OldValue 的既有约定自洽：
        /// 值既然被脱敏，对读者而言就是"不可知"，审计员据这条记录只知道"该字段被改过"。
        ///
        /// 🔴 **刻意用"包含"而非"精确相等"**（2026-09-18 收紧）：精确匹配只罩得住名字正好叫 Password 的字段，
        /// 而同类字段现实中更常带前后缀——NewPassword / ConfirmPassword / OldPwd / ApiToken / ClientSecret
        /// 全部漏网，且漏得**静默无声**（值是明文、照样成功落库、没有任何人会注意到）。
        /// 敏感判定与锚点判定的取向相反：锚点求稳（宁可退化成粗粒度），此处求 **fail-closed**——
        /// 匹配偏宽只少记一个值（可接受），匹配偏窄就是凭据进表（不可接受）。
        /// 误伤面（名字含关键字但值不敏感，如 TokenTimeout）在上述不对称下是可接受的代价。
        ///
        /// 为什么不改成模型上的标记特性：敏感与否是**跨模型的安全策略**，不是单个模型的业务知识。
        /// 特性化之后"忘了标记"就是明文落库且无提示——把安全兜底交给逐个模型的自觉，
        /// 恰恰是这个集中名单存在的反面。它是最后一道墙，不可撤。
        /// </summary>
        private static readonly string[] SensitiveKeywords =
        {
            "Password", "Pwd", "ConnectionString", "Token", "Secret"
        };

        /// <summary>对象 / 快照 → JToken（已是 JToken 的原样返回，null 原样返回）。</summary>
        internal static JToken ToToken(object value)
        {
            if (value == null) return null;
            var token = value as JToken;
            return token ?? JToken.FromObject(value);
        }

        /// <summary>缺席判据：C# null / JSON null / Undefined 一律视为"没这个值"（三者对审计是同一件事）。</summary>
        internal static bool IsAbsent(JToken token)
        {
            return token == null || token.Type == JTokenType.Null || token.Type == JTokenType.Undefined;
        }

        /// <summary>
        /// 末段属性名是否命中敏感关键字（DB.ConnectionString → 含 ConnectionString 即中；
        /// Tags[0].NewPassword → 含 Password 即中）。
        /// 传**原名**路径：显示名可能已把 Password 翻成"密码"，翻译后再比对必然漏掉。
        ///
        /// 包含匹配的取舍见 <see cref="SensitiveKeywords"/> 注释（fail-closed：宁可多脱敏一个值，不可漏一个凭据）。
        /// </summary>
        internal static bool IsSensitive(string rawPath)
        {
            if (string.IsNullOrEmpty(rawPath)) return false; // 顶层整体替换时无末段可比

            int start = rawPath.LastIndexOf('.') + 1;
            int bracket = rawPath.IndexOf('[', start);
            string name = bracket >= 0
                ? rawPath.Substring(start, bracket - start)
                : rawPath.Substring(start);

            foreach (var keyword in SensitiveKeywords)
            {
                if (name.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0) return true;
            }
            return false;
        }

        /// <summary>
        /// JToken → 审计文本。标量取字面量（InvariantCulture：审计要跨机器可读，不受现场区域设置影响）；
        /// 对象/数组取压缩 JSON（整块被替换时才走到这里）。
        /// </summary>
        internal static string Format(JToken token)
        {
            if (IsAbsent(token)) return null;

            if (token is JValue)
                return Convert.ToString(((JValue)token).Value, CultureInfo.InvariantCulture);

            return token.ToString(Formatting.None);
        }
    }
}
