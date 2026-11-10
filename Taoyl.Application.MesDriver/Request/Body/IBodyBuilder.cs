namespace Taoyl.Application.MesDriver.Request.Body
{
    /// <summary>
    /// 一种正文形态的全部知识：默认 Content-Type + 成型方式。
    ///
    /// 🔴 只有两个成员——形态之间真正不同的就这两件事。合并语义写在各自实现里：
    /// Json 与 Form 在"覆盖层不是对象"时的兜底规则不同（Json 压缩后发 / Form 原样发），
    /// 抽到接口外只能靠传标志位。
    ///
    /// 加一种形态 = 实现本接口 + 在 RequestBody 登记表加一行。
    /// </summary>
    internal interface IBodyBuilder
    {
        /// <summary>该形态的默认 Content-Type。null = 不补（None 形态，实发报文里就没有这个头）。</summary>
        string ContentType { get; }

        /// <summary>配置正文 + 流程覆盖层 → 实际发出去的正文。</summary>
        string Build(string configText, object invokeBody);
    }
}
