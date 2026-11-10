namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// 纯文本：流程传了就整个替换，否则原样发配置的内容。
    /// Xml 形态的成型方式与本类逐字相同，差别只有 Content-Type——是两个类而不是一个，
    /// 因为「登记值必须与内容对得上」。
    /// </summary>
    internal sealed class TextBodyBuilder : IBodyBuilder
    {
        public string ContentType { get { return "text/plain; charset=utf-8"; } }

        public string Build(string configText, object invokeBody)
        {
            return BodyText.ToText(invokeBody) ?? configText;
        }
    }
}
