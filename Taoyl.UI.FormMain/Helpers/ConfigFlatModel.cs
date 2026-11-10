using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using Taoyl.Application.Configuration.Models;

namespace Taoyl.UI.FormMain.Helpers
{
    /// <summary>
    /// 配置区行类型。渲染层据此分派——行类型是一等语义，不再靠"某字段是否为 null"的组合来推断种类。
    /// </summary>
    public enum ConfigRowKind
    {
        /// <summary>可编辑属性行（带 Target/Property 供 DataBindings；OptionsSource 可选）</summary>
        Property,
        /// <summary>分组标题行（Name = 分组名）</summary>
        GroupHeader,
        /// <summary>字典属性区段行（带 Dict 引用；整段交由 ConfigDictSection 自持渲染）</summary>
        DictGroupHeader,
    }

    /// <summary>
    /// 配置属性的扁平化视图模型。将嵌套的 ConfigBase 树 / 普通 POCO 递归拍平为一维列表：
    /// - 嵌套 ConfigBase 属性 → GroupHeader + 递归子属性；
    /// - IDictionary 属性 → 单个 DictGroupHeader（携带字典引用；键值行由渲染层现场从字典取）；
    /// - 标量属性 → Property（带 Target/Property，编辑器走 DataBindings/事件写回）。
    /// 仅扫描带 [DisplayName] 且未标 [JsonIgnore]/[Browsable(false)] 的属性。
    ///
    /// 关键设计：字典是"变长"数据，键值行不进扁平快照——快照里只留一个区段头行，行内容由渲染层现场读字典。
    /// 因此 Flatten 是纯快照（不随字典增删而失效），渲染层也不需要维护"UI 行序 ↔ 行索引"的双份状态。
    /// 只读展示场景由调用方对渲染结果整区 SetReadOnly，不需要额外的只读行类型。
    /// </summary>
    public class ConfigFlatRow
    {
        /// <summary>行类型（渲染层按此分派）。</summary>
        public ConfigRowKind Kind { get; set; }

        /// <summary>显示名（属性行 = [DisplayName]；分组/字典区段头 = 类或属性 [DisplayName]）。</summary>
        public string Name { get; set; }

        /// <summary>缩进层级。</summary>
        public int Level { get; set; }

        /// <summary>数据绑定目标（仅属性行有效 = 真实 ConfigBase/POCO 实例）。</summary>
        public object Target { get; set; }

        /// <summary>数据绑定路径：属性元数据（仅属性行有效）。</summary>
        public PropertyInfo Property { get; set; }

        /// <summary>
        /// 字典引用（仅 DictGroupHeader 行有效 = 该字典属性实例）。
        /// 供渲染层把整段交给 ConfigDictSection 自持渲染（组头 + 添加栏 + 键值行）。
        /// </summary>
        public IDictionary Dict { get; set; }

        /// <summary>
        /// 可编辑 string 属性行的候选选项源（仅属性行有效；null = 自由文本编辑）。
        /// 供渲染层将该行渲为下拉（如 PlcConfig.Protocol → PlcProtocol 规范串列表）；由调用方 Flatten 后按需对目标行注入。
        /// </summary>
        public IReadOnlyList<string> OptionsSource { get; set; }

        /// <summary>
        /// 将配置对象递归展开为扁平列表。支持两类输入：
        /// 1) ConfigBase 树：嵌套 ConfigBase 属性 → 分组标题 + 递归子属性（原有行为，零回归）；
        /// 2) 任意 POCO（如 PlcConfig）：标量属性直出属性行；IDictionary 属性 → 一个区段头行（挂 Dict 引用，键值行由渲染层现场取自字典）。
        /// </summary>
        public static List<ConfigFlatRow> Flatten(object config, int level = 0)
        {
            var list = new List<ConfigFlatRow>();
            if (config == null) return list;

            foreach (var prop in config.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null) continue;
                if (prop.GetCustomAttribute<BrowsableAttribute>()?.Browsable == false) continue;

                var disp = prop.GetCustomAttribute<DisplayNameAttribute>();
                if (disp == null) continue;

                object val = prop.GetValue(config);

                if (val is ConfigBase nested)
                {
                    // 嵌套 ConfigBase → 分组标题 + 递归子属性
                    list.Add(new ConfigFlatRow
                    {
                        Kind = ConfigRowKind.GroupHeader,
                        Name = disp.DisplayName,
                        Level = level,
                    });
                    list.AddRange(Flatten(nested, level + 1));
                }
                else if (val is IDictionary dict)
                {
                    // 字典属性（如 PlcConfig.ProtocolParams）→ 单个区段头行：键值行是变长数据，不进快照，渲染时现场取
                    list.Add(new ConfigFlatRow
                    {
                        Kind = ConfigRowKind.DictGroupHeader,
                        Name = disp.DisplayName,
                        Level = level,
                        Dict = dict,
                    });
                }
                else
                {
                    list.Add(new ConfigFlatRow
                    {
                        Kind = ConfigRowKind.Property,
                        Name = disp.DisplayName,
                        Level = level,
                        Target = config,
                        Property = prop,
                    });
                }
            }
            return list;
        }
    }
}
