using System;
using System.Collections.Generic;
using Taoyl.Application.Share.Extensions;

namespace Taoyl.Application.Alarm.Models
{
    /// <summary>
    /// 报警类型枚举 → 落库文案（记录 / 提示 / 异常）。静态建表缓存，之后零反射。
    /// 文案即存储契约：投产不得改 [Description]，只可新增枚举成员。
    /// </summary>
    public static class AlarmEnumText
    {
        private static readonly Dictionary<AlarmType, string> TypeText = BuildMap();

        /// <summary>落库文案 → 报警类型反向表（从正向表派生，弹窗分诊边界用）。</summary>
        private static readonly Dictionary<string, AlarmType> TextType = BuildReverseMap();

        /// <summary>报警类型 → 落库文案；缺 [Description] 时降级为枚举名。</summary>
        public static string Of(AlarmType value)
        {
            return TypeText[value];
        }

        /// <summary>落库文案 → 报警类型；未登记文案返回 false（文案必经 Of 落库，false 仅防脏数据）。</summary>
        public static bool TryParse(string text, out AlarmType value)
        {
            return TextType.TryGetValue(text, out value);
        }

        private static Dictionary<AlarmType, string> BuildMap()
        {
            var values = (AlarmType[])Enum.GetValues(typeof(AlarmType));
            var map = new Dictionary<AlarmType, string>(values.Length);

            foreach (var value in values)
            {
                string text = value.GetDescription();
                if (string.IsNullOrEmpty(text))
                    text = value.ToString();

                map[value] = text;
            }

            return map;
        }

        private static Dictionary<string, AlarmType> BuildReverseMap()
        {
            var map = new Dictionary<string, AlarmType>(TypeText.Count);
            foreach (var pair in TypeText)
            {
                map[pair.Value] = pair.Key;
            }

            return map;
        }
    }
}
