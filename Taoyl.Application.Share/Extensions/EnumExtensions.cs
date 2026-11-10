using System;
using System.ComponentModel;
using System.Reflection;

namespace Taoyl.Application.Share.Extensions
{
    /// <summary>
    /// 枚举扩展方法（跨领域通用纯工具）。
    /// </summary>
    public static class EnumExtensions
    {
        /// <summary>
        /// 获取枚举成员的显示名：优先读取 [Description] 特性，无则回退到枚举名。
        /// </summary>
        public static string GetDescription(this Enum value)
        {
            var field = value.GetType().GetField(value.ToString());
            var attr = field?.GetCustomAttribute<DescriptionAttribute>();
            return attr?.Description ?? value.ToString();
        }
    }
}
