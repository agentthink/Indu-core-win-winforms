using System;
using System.Collections.Generic;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.Application.Audit.Constants
{
    /// <summary>
    /// 审计维度枚举 ↔ **落库文案** 的转换中枢。
    ///
    /// 写侧落库前把枚举翻成中文文案（列宽 20 的 string 列，见 AuditOperationEntity /
    /// AuditFieldChangeEntity 的 SugarColumn）；读侧拿到的本来就是中文文案、不解析回枚举
    /// ——所以"读回来的未知文案怎么办"在本设计里**不存在**（历史文案永远原样展示）。
    ///
    /// 🔴 转换结果**带缓存**：落库是逐明细循环的热路径（一次配置保存可出上百条明细），
    /// 每行都走反射取特性不可接受。缓存由静态构造一次建好，之后零反射、纯查表。
    ///
    /// ⚠️ 缺 [Description] 时降级为枚举名（对齐 AlarmEnumText），不做启动期断言。
    /// 已知代价：落库文案从此靠新增成员时手工维护——漏写 Description 会写进英文枚举名，
    /// 与已有中文文案并存，筛选/统计会分裂且不报错。新增枚举成员务必带 [Description]。
    /// </summary>
    public static class AuditEnumText
    {
        private static readonly Dictionary<AuditCategory, string> CategoryText =
            BuildMap<AuditCategory>();

        private static readonly Dictionary<AuditAction, string> ActionText =
            BuildMap<AuditAction>();

        private static readonly Dictionary<AuditResult, string> ResultText =
            BuildMap<AuditResult>();

        private static readonly Dictionary<AuditChangeType, string> ChangeTypeText =
            BuildMap<AuditChangeType>();

        /// <summary>页面 → 落库文案（用户管理 / 角色管理 / …）</summary>
        public static string Of(AuditCategory value)
        {
            return CategoryText[value];
        }

        /// <summary>命令级动词 → 落库文案（新建 / 修改 / 导出 / …）</summary>
        public static string Of(AuditAction value)
        {
            return ActionText[value];
        }

        /// <summary>结果两态 → 落库文案（成功 / 失败）</summary>
        public static string Of(AuditResult value)
        {
            return ResultText[value];
        }

        /// <summary>变更类型 → 落库文案（新增 / 修改 / 删除）</summary>
        public static string Of(AuditChangeType value)
        {
            return ChangeTypeText[value];
        }

        /// <summary>建"枚举 → 文案"表；缺 [Description] 降级为枚举名。</summary>
        private static Dictionary<TEnum, string> BuildMap<TEnum>()
            where TEnum : struct, Enum
        {
            var values = (TEnum[])Enum.GetValues(typeof(TEnum));
            var map = new Dictionary<TEnum, string>(values.Length);

            foreach (var value in values)
            {
                string text = ((Enum)(object)value).GetDescription();
                if (string.IsNullOrWhiteSpace(text))
                    text = value.ToString();

                map[value] = text;
            }

            return map;
        }
    }
}
