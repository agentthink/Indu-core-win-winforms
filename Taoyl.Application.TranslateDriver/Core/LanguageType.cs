using System.ComponentModel;

namespace Taoyl.Application.TranslateDriver.Core
{
    /// <summary>
    /// 支持的语言类型枚举
    /// 格式：语言_区域，对应JSON中的语言代码（自动将_替换为-）
    /// </summary>
    public enum LanguageType
    {
        [Description("中文")]
        zh_CN,

        [Description("英文")]
        en_US,

        [Description("德文")]
        de_DE,

        [Description("匈牙利语")]
        hu_HU,

        [Description("印尼语")]
        id_ID,

        [Description("西班牙语")]
        es_ES
    }

    /// <summary>
    /// 资源类型分类
    /// UI：界面控件文本（Button, Label, DataGridView列头等）
    /// Text：日志、消息、异常文本等非界面文本
    /// </summary>
    public enum ResourceType
    {
        /// <summary>界面控件资源</summary>
        UI,

        /// <summary>文本/日志资源</summary>
        Text
    }
}