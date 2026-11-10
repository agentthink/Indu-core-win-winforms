using System.Collections.Generic;

namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// 一次调用的覆盖层：只写「这次和配置不一样」的部分，其余沿用 Mes.json。
    /// 与 <see cref="MesApiDefinition"/> 的四个域一一对应，没有「哪个域不能覆盖」的例外。
    ///
    /// 🔴 合并语义：同名替换整条、新名追加；返回新列表，永不改配置快照。
    /// 🔴 没有「整表替换」开关。那会让每次调用都要重抄配置里的固定参数——漏抄是纯手工错误，
    /// 编译期、运行期都不拦，而覆盖层最常见的用法恰恰是「只补一个条码」。
    /// 需要清空重填或删掉某一项时，走 Invoke(MesApiDefinition, ct)：两个入口语义各自纯粹，
    /// 按名 = 打补丁，按定义 = 全权。
    ///
    /// 🔴 本对象不被驱动复制，调用期间仍是调用方的。别在并发路径上复用同一个实例
    /// （发出去的参数 / 头 / 正文可能来自不同版本，且不会有任何报错）。
    /// </summary>
    public sealed class MesCallOptions
    {
        /// <summary>
        /// 正文覆盖层。语义同原 Invoke 的 body 参数：形态决定合并还是替换
        /// （Json / Form 对象合并、数组整体替换；Text / Xml 整体替换）。
        /// </summary>
        public object Body { get; set; }

        /// <summary>
        /// 查询参数覆盖项。GET 的动态参数（条码、工单号）落这里。
        /// 🔴 参数名**大小写敏感**：barcode 与 BarCode 是两个不同的参数。
        /// HTTP 没有规定参数名的大小写规则，由服务端框架决定（主流实现默认敏感），
        /// 这里保守处理——读写方不一致时宁可多发一个，也不要把配置的值顶掉。
        /// </summary>
        public List<MesHeader> QueryParams { get; } = new List<MesHeader>();

        /// <summary>
        /// 请求头覆盖项。签名、时间戳这类运行期才算得出的头落这里。
        /// 🔴 头名**大小写不敏感**（RFC 7230 明确规定），content-type 与 Content-Type 视为同一个。
        /// 🔴 不要在这里传 Authorization：鉴权页生效时会先摘掉它再注入（「鉴权赢」规则），
        /// 传了也会被覆盖。动态 token 用下面的 <see cref="Auth"/>。
        /// </summary>
        public List<MesHeader> Headers { get; } = new List<MesHeader>();

        /// <summary>
        /// 鉴权覆盖。非 null 时**整体替换**配置里的 Auth。
        /// 🔴 不做字段级合并：鉴权是原子单元，半合并会得到「用户名换了、密码没换」这种怪状态。
        /// </summary>
        public MesAuthConfig Auth { get; set; }
    }
}
