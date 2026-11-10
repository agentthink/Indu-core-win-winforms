using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.UI.Pages.Models.Mes
{
    /// <summary>
    /// 正文形态 → UI 知识的唯一登记处：提示文案、格式化按钮。
    ///
    /// 🔴 没有「编辑器种类」这一层：一种形态 = 一个编辑器 = 一个缓冲区（见 MesApiEditModel）。
    /// 「未登记形态回落文本编辑器」这条知识因此落在两个 switch 的 default 分支里。
    /// 🔴 驱动侧登记表是 RequestBody，两张表键集必须一致（无编译期保障，
    /// 加形态的完整清单见 MesBodyType 枚举注释）。
    /// </summary>
    public static class MesBodyTypeUi
    {
        /// <summary>一种正文形态的全部界面侧知识。</summary>
        public sealed class UiInfo
        {
            /// <summary>正文面板下方的形态提示文案。</summary>
            public string Hint;

            /// <summary>格式化按钮是否显示（文本类编辑器显示；表格与无正文不显示）。</summary>
            public bool ShowFormatButton;

            /// <summary>
            /// 格式化实现：解析 + 重新序列化回写编辑器。null = 该形态不支持（按钮显示但禁用）。
            /// 抛异常即「正文非法」，消息自带形态名，直接给用户看。
            /// </summary>
            public Func<string, string> Format;

            /// <summary>格式化按钮是否可用。由 <see cref="Format"/> 派生，不要单独赋值。</summary>
            public bool CanFormat { get { return Format != null; } }
        }

        private static readonly Dictionary<MesBodyType, UiInfo> Table = new Dictionary<MesBodyType, UiInfo>
        {
            {
                MesBodyType.None,
                new UiInfo
                {
                    Hint = "无正文 · 发送时不携带任何内容（空 POST 为 Content-Length: 0）",
                    ShowFormatButton = false
                }
            },
            {
                MesBodyType.Json,
                new UiInfo
                {
                    Hint = "JSON · 发送前自动压缩，与配置 Body 合并",
                    ShowFormatButton = true,
                    Format = FormatJson
                }
            },
            {
                MesBodyType.Form,
                new UiInfo
                {
                    Hint = "表单 · 发送前转 k=v&…（键值自动 form 编码），与配置 Body 合并",
                    ShowFormatButton = false
                }
            },
            {
                MesBodyType.Text,
                new UiInfo
                {
                    Hint = "文本 · 原样发送，不与配置 Body 合并（流程传了就整个替换）",
                    ShowFormatButton = true
                }
            },
            {
                MesBodyType.Xml,
                new UiInfo
                {
                    Hint = "XML · 原样发送（SOAP 用这个）· 正文按 UTF-8 编码发送 · Content-Type 自动补 text/xml；SOAP 1.1 还需手填 SOAPAction",
                    ShowFormatButton = true,
                    Format = FormatXml
                }
            }
        };

        /// <summary>未登记形态的兜底文案：正文仍按文本处理，不提供格式化。</summary>
        private static readonly UiInfo Unsupported = new UiInfo
        {
            Hint = "未识别的正文形态 · 已按文本原样处理，请检查程序与配置版本是否匹配",
            ShowFormatButton = false
        };

        /// <summary>
        /// 🔴 查不到回落「文本 + 原文」而不是抛：界面宁可显示得粗糙，
        /// 也必须让用户看得到配置里的正文并原样存回去——这里丢数据比显示不准严重得多。
        /// 与之相对，驱动侧 RequestBody 查不到是抛（宁可这次调用失败，也不瞎发）。
        /// </summary>
        public static UiInfo For(MesBodyType type)
        {
            UiInfo info;
            if (Table.TryGetValue(type, out info))
                return info;

            return Unsupported;
        }

        /// <summary>JSON 格式化：解析失败即抛，消息自带形态名（直接给用户看）。</summary>
        private static string FormatJson(string text)
        {
            try
            {
                return JToken.Parse(text).ToString(Formatting.Indented);
            }
            catch (Exception ex)
            {
                throw new FormatException("正文不是合法 JSON：" + ex.Message);
            }
        }

        /// <summary>
        /// XML 格式化。XDocument 保留声明 / 注释 / CDATA / 命名空间前缀；
        /// 默认 DtdProcessing=Prohibit，不吃外部实体（XXE）。
        /// </summary>
        private static string FormatXml(string text)
        {
            try
            {
                return XDocument.Parse(text).ToString();
            }
            catch (Exception ex)
            {
                throw new FormatException("正文不是合法 XML：" + ex.Message);
            }
        }
    }
}
