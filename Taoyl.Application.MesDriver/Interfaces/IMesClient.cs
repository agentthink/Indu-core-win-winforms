using System.Threading;
using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Interfaces
{
    /// <summary>
    /// MES 调用端口。供流程子类注入使用。
    /// </summary>
    public interface IMesClient
    {
        /// <summary>
        /// 按接口名发起一次调用：配置打底，只传「这次和配置不一样」的部分。
        /// 🔴 覆盖层只放每次调用都会变的值（条码、时间戳、签名、动态 token）；凡是所有调用方
        ///    都一样的固定值留在配置里。挪进代码容易、挪回配置没人做——配置一旦空心化，
        ///    换个地址就要改代码重编译。
        /// 🔴 要改结构（Url / Method / BodyType）或配置里根本没这条接口时，用另一个重载
        ///    Invoke(MesApiDefinition, ct)。两个入口的边界就是「打补丁」与「全权」。
        /// </summary>
        /// <param name="apiName">接口名，须与配置中的 MesApiDefinition.Name 完全一致（区分大小写）</param>
        /// <param name="options">
        /// 覆盖层，null = 原样发配置。四个域与 MesApiDefinition 一一对应：
        /// Body 走形态的合并规则；QueryParams / Headers 同名替换、新名追加
        /// （参数名大小写敏感，头名不敏感）；Auth 非 null 时整体替换配置的鉴权。
        /// </param>
        /// <param name="ct">
        /// 取消令牌。流程侧把 ExecuteStep 的 token 直接传进来——操作工点停止即可中断等待。
        /// </param>
        /// <returns>
        /// 调用记录。🔴 永不抛异常：任何失败（配置缺失、超时、连不上、取消、序列化出错）都是
        /// HttpStatus == null + Failure 分类 + Response 描述。区分原因读 Failure，
        /// 给人看的文案用 MesFailureText.Of。这是硬约束——ProcessBase 的 catch 会把异常转成
        /// E999 异常态，等于停线。
        /// 🔴 HTTP 层成功判据是 <c>HttpStatus == 200</c>，不是 <c>!= null</c>：驱动不跟随重定向，
        /// 302 会原样返回且 Failure 仍为 None，只判 null 会当成成功。
        /// ⚠️ 这只是传输层判据：MES 常见的「200 + 正文里的业务错误码」驱动不解析，由调用方判断。
        /// </returns>
        MesResult Invoke(string apiName, MesCallOptions options = null, CancellationToken ct = default);

        /// <summary>
        /// 用调用方给的定义发一次——不进配置、不落盘，装配 / 重试 / 日志与按名调用完全同路。
        /// 适用于「临时发一条没配过的接口」，或需要运行期才算得出请求头（签名 / 时间戳）的场合。
        /// 🔴 临时接口**不留审计**（审计只记 Mes.json 的变更）。生产路径请走配置。
        /// 🔴 Name 必填：它是日志里唯一的归因键，空名会在前置校验被拒（配置侧加载时同样剔除空名条目）。
        /// 🔴 定义里的 BodyType 默认是 Json：发 XML 等非 JSON 正文必须显式设置。
        /// 🔴 驱动不复制也不校验这个对象，调用期间它仍是调用方的。别在并发路径上复用同一个实例
        /// （发出去的 URL / 头 / 正文可能来自不同版本，且不会有任何报错）。
        /// </summary>
        /// <param name="definition">请求定义。除 Name / Url 外各字段均有可用默认值。</param>
        /// <param name="ct">取消令牌。语义同按名调用。</param>
        MesResult Invoke(MesApiDefinition definition, CancellationToken ct = default);
    }
}
