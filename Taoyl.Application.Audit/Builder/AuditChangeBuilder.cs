using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models;

namespace Taoyl.Application.Audit.Builder
{
    /// <summary>
    /// 审计字段级变更构建器（各域挂点共用的**唯一门面**）。
    ///
    /// 为什么必须共享一份：全仓已无 diff 引擎（服务层 JSON diff 路线随 R2 重构废弃），
    /// 而配置 / 配方 / PLC 连接配置 / 用户 / 角色五处挂点都要"对象 → 字段级差异"；
    /// 分散实现五份必然漂移（判据、路径格式、null 语义各写各的），到时同一列出现三种写法。
    ///
    /// 实现取 JSON 往返而非反射，与 ConfigBase.Clone / CopyTo 同一契约：
    /// 属性随模型自动跟进（新增成员零改动），且显式 [JsonIgnore] 的计算属性天然不参与比对
    /// （IsDirty / MaxRoleLevel / RoleNamesDisplay 这类派生值本就不该进审计——它们不是"人改的"）。
    ///
    /// 路径 = **模型上的中文显示名**（如 界面语言 / 数据库.连接字符串 / 参数[前称重].最小值），
    /// 翻译规则见 <see cref="AuditPropertyMap"/>。
    ///
    /// **数组按业务 Key 锚定**（不是按下标）：**配对**锚点取 Key / Code / Id / UserId / Name / RoleName，
    /// **展示**段另取人性化名（DisplayName / Name / RoleName / TagName），取不到才用锚点。
    /// 两者必须分开：配对得用稳定键（DisplayName 是用户可编辑字段，拿它配对会把"改显示名"
    /// 误判成"删一条 + 增一条"），展示则要好读。故路径渲染为 参数[前称重].最小值 这种既可读又能定位的形式。
    /// 规则明细与取舍见 <see cref="AuditArrayAnchor"/>。
    /// 为什么必须锚定：下标配对在"列表增删一项"时会全线错位，把一次改动放大成几十条假明细
    /// （曾经的现场故障：配方改一个下限，明细刷出 40+ 条"删除"）；
    /// 数组元素本身的增/删也只落**一条**（不再逐字段展开），审计要能一眼读完。
    ///
    /// <see cref="AuditChangeType"/> 语义：仅在新值侧存在 = 新增（旧值留空）；
    /// 仅在旧值侧存在 = 删除（新值留空）；两侧都有但不等 = 修改（新旧值都记）。
    ///
    /// ── 关于本类与其余五个协作类（2026-09-19 拆分）────────────────────────────
    /// 本类原为 707 行单体，29 个方法、四份硬编码名单全挤在一处，改任何一处都要先读懂全篇。
    /// 按**职责**拆为六个类，边界即为"改一处时只须读一个文件"：
    /// - <see cref="AuditFieldPath"/>：路径渲染（点分 / 下标 / 方括号段）
    /// - <see cref="AuditArrayAnchor"/>：数组元素取名（配对锚点 vs 展示名，两套名单）
    /// - <see cref="AuditPropertyMap"/>：类型反射缓存 + 中文显示名翻译
    /// - <see cref="AuditValueFormatter"/>：值格式化 + 敏感字段脱敏
    /// - <see cref="AuditChangeWalker"/>：递归比对引擎（对象 / 数组 / 标量三种走法）
    /// - 本类：对外门面（三个公开入口），把上面五个按序串起来
    /// 拆分**纯搬家**：判据、顺序、取舍一行未改，行为与拆分前逐字节等价（单测锁定）。
    ///
    /// 📌 **将来若要换声明式字段清单**（显式声明"比哪些字段、怎么显示"取代反射推导）：
    /// 落点就是本类 —— 三个公开入口的签名与语义不必动，
    /// 只须替换 <see cref="AuditChangeWalker"/> 与 <see cref="AuditPropertyMap"/> 的实现。
    /// 调用方（UI 命令层）走 IAuditService 契约，届时零改动。
    /// </summary>
    public static class AuditChangeBuilder
    {
        /// <summary>
        /// 取"零副作用"的变更基线快照（编辑态开始时调一次，Save 时当 before 传入 <see cref="Build"/>）。
        ///
        /// 🔴 **不要用 ConfigBase.Clone&lt;T&gt;() 取基线**：它走裸 JsonConvert（ObjectCreationHandling.Auto），
        /// 集合属性会复用构造函数预置的实例并**追加**——配方模型（RecipeBase 字段初始化器 + 构造函数
        /// 双重预置 6 个参数）克隆出来变 12 项，拿它当基线会产出 42 条假"删除"。
        ///
        /// 本方法只走**序列化方向**（对象 → JToken 树），不实例化目标类型、不执行其构造函数，
        /// 因此天然不受"构造函数预置集合"影响；顺带把快照与业务对象解耦（编辑态继续改实例也不影响基线）。
        /// </summary>
        /// <param name="value">被快照的对象（null 原样返回，Build 侧会当作"整体缺席"）</param>
        public static JToken Snapshot(object value)
        {
            return value == null ? null : JToken.FromObject(value);
        }

        /// <summary>
        /// 比对两个对象，产出字段级变更明细。
        /// 无变化返回**空列表**（不返回 null——调用方免判空，"有没有明细"统一看 Count）。
        /// </summary>
        /// <param name="before">变更前对象（草稿进入编辑态时取快照，或缓存单例原值）</param>
        /// <param name="after">变更后对象（草稿 / 已提交值）</param>
        /// <param name="skipTopLevelNames">
        /// 要跳过的顶层属性名，传 **C# 属性名**（如 nameof(RecipeBase.ModifiedBy)）——内部会换成显示名再比对。
        /// 判据 = FieldPath 精确等于该属性翻译后的显示名；顶层路径是一段裸名（不含点与下标），
        /// 故嵌套层的同名属性不会被误伤。
        /// 为什么需要：审计要回答"人改了什么业务参数"，程序自动盖的元数据章不是操作者的意图。
        /// </param>
        public static List<AuditFieldChange> Build(object before, object after,
            params string[] skipTopLevelNames)
        {
            var result = new List<AuditFieldChange>();
            if (ReferenceEquals(before, after)) return result;

            // 根类型从实体侧推断（两边都可能是 JToken 快照，那就推不出类型、翻译自动退化为原名）
            Type rootType = AuditPropertyMap.RootTypeOf(before, after);

            AuditChangeWalker.Walk(
                AuditValueFormatter.ToToken(before),
                AuditValueFormatter.ToToken(after),
                null, null, rootType, result);

            // 跳过名单按**显示名**比对：FieldPath 已是中文，拿 nameof(ModifiedBy) 直接比会全部失配
            // （表现为配方保存又冒出"变更人员 / 变更时间"两条与操作者意图无关的明细）
            var skip = AuditPropertyMap.TranslateSkipNames(skipTopLevelNames, rootType);
            if (skip != null) result.RemoveAll(c => skip.Contains(c.FieldPath));

            return result;
        }

        /// <summary>
        /// 集合差异**逐项**展开（2026-09-20 改型，原"汇总为一条"的粒度已在第四轮推翻）：
        /// 每个新增 / 移除的元素各占一条明细，权限点一望即知——审计的读者逐条核权限，
        /// 不该拿着一条逗号长串自己拆。两侧都有的成员不产生明细（集合成员无"值"可改）。
        ///
        /// 输出顺序：先新增后移除（正向动作优先叙述，与 <see cref="AuditChangeType"/> 枚举序一致），
        /// 各自按 Ordinal 排序——集合顺序不同但成员相同 → 双向差集皆空 → 不误报。
        /// </summary>
        /// <param name="fieldPath">明细路径（如 手动权限）</param>
        /// <param name="before">旧集合（null = 空集）</param>
        /// <param name="after">新集合（null = 空集）</param>
        /// <returns>无变化返回**空列表**（不返回 null——与 <see cref="Build"/> 的约定一致，调用方直接 AddRange）</returns>
        public static List<AuditFieldChange> BuildSetChanges(string fieldPath,
            IEnumerable<string> before, IEnumerable<string> after)
        {
            var result = new List<AuditFieldChange>();

            var oldItems = Normalize(before);
            var newItems = Normalize(after);
            if (oldItems.Count == 0 && newItems.Count == 0) return result;

            var added = new HashSet<string>(newItems, StringComparer.Ordinal);
            added.ExceptWith(oldItems);
            var removed = new HashSet<string>(oldItems, StringComparer.Ordinal);
            removed.ExceptWith(newItems);

            // 差集经 ExceptWith 后的 HashSet 遍历序不可靠，明细顺序必须显式 Ordinal 排序保证稳定
            foreach (string code in added.OrderBy(v => v, StringComparer.Ordinal))
                result.Add(new AuditFieldChange
                {
                    FieldPath = fieldPath,
                    ChangeType = AuditChangeType.Add,
                    NewValue = code
                });
            foreach (string code in removed.OrderBy(v => v, StringComparer.Ordinal))
                result.Add(new AuditFieldChange
                {
                    FieldPath = fieldPath,
                    ChangeType = AuditChangeType.Delete,
                    OldValue = code
                });

            return result;
        }

        /// <summary>集合入参归一：滤空白 + Ordinal 排序（排序即差集的稳定底座，Add/Delete 明细逐条出）。</summary>
        private static List<string> Normalize(IEnumerable<string> values)
        {
            if (values == null) return new List<string>();

            return values.Where(v => !string.IsNullOrWhiteSpace(v))
                         .OrderBy(v => v, StringComparer.Ordinal)
                         .ToList();
        }
    }
}
