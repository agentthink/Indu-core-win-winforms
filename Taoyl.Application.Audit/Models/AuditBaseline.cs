using Newtonsoft.Json.Linq;

namespace Taoyl.Application.Audit.Models
{
    /// <summary>
    /// 审计基线句柄 —— 由 <see cref="Interfaces.IAuditService.Snapshot"/> 产出，
    /// 由 <see cref="Interfaces.IAuditService.BuildChanges"/> 消费。
    ///
    /// 定位：diff 实现的**不透明包装**。调用方只需"取到 → 持有 → 回传"，
    /// 不感知内部是 JToken 序列化还是别的形式 ——
    /// 将来把反射式 diff 换成声明式字段清单，调用方零改动。
    ///
    /// 内部载荷刻意 internal：调用方（UI 层）与本类不同程序集，结构上拿不到 Token，
    /// 无法绕过契约自己去比对（避免"契约形同虚设、实现细节再次泄漏回调用方"）。
    /// </summary>
    public sealed class AuditBaseline
    {
        /// <summary>内部载荷（当前实现为 JToken；不对外暴露）</summary>
        internal JToken Payload { get; private set; }

        internal AuditBaseline(JToken payload)
        {
            Payload = payload;
        }
    }
}
