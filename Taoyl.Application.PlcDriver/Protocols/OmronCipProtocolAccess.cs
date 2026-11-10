using System.Text;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models;

namespace Taoyl.Application.PlcDriver.Protocols
{
    /// <summary>
    /// 欧姆龙CIP协议访问（对应驱动 OmronCip）
    /// 职责：提供欧姆龙CIP协议特有的读写策略
    /// 协议差异（覆写基类默认实现）：仅 String 读一条——
    /// String 读：ASCII 编码；⚠️ Length 下界是 1（读策略显式拒绝 ≤0）：CIP 没有「0 = 读原生整串」语义（那是 S7 专有），
    ///           放行 0 会被 HSL 走成「成功 + 空数组」→ 每轮静默返回空串且报"成功"
    /// String 写：基类已是 ASCII 原样全写（不填充不截断），零差异，无需覆写
    /// 备注：CIP 是 tag 模型，Read 的 length 一律是"数组元素个数"（非字数、非字节数；length=1 时返回该 tag 的全部字节），
    /// 与基类"统一传输单元语义"天然一致，故 Byte / ByteArray 无协议差异，不需要覆写
    /// </summary>
    public class OmronCipProtocolAccess : BaseProtocolAccess
    {
        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        public OmronCipProtocolAccess(IPlcTransport communication) : base(communication)
        {
        }
        #endregion

        #region 策略初始化（欧姆龙 CIP 特有）
        /// <summary>
        /// 欧姆龙 CIP 读策略覆写：String 用 ASCII（读策略签名含值容器输出槽，String 是引用类型零装箱）
        /// 写策略零差异不覆写（String 写基类已收口为 ASCII 原样全写）
        /// ⚠️ String 的 len ≤ 0 直接报错（不静默降级）：CIP 的 length 是「数组元素个数」，无「0 = 读原生整串」语义。
        ///    若放行 len=0 —— HSL 侧 Read(address, 0) 的 length==1 特判不命中、while 循环零次，
        ///    返回「成功 + 空数组」，剥头分支（content.Length &gt;= 2）也不命中，最终 = 成功 + 空串：
        ///    界面值恒空、"最近结果 = 成功"，属现场最难查的一类静默故障。故在此响亮报错。
        ///    正确用法：CIP 地址表 String 照旧填元素个数（存量惯例的 1 = 该 tag 的全部字节）
        /// </summary>
        protected override void InitializeReadStrategies()
        {
            base.InitializeReadStrategies();

            _readStrategies[PlcDataType.String] = (addr, len, holder) =>
            {
                if (len <= 0)
                {
                    // ErrorCode 用默认 10000（**不可用负数**：CheckConnectionStatus 会把负数判为断线，误触发重连）
                    return ResponseResult.Fail(
                        $"欧姆龙CIP 的 String 长度是元素个数，不支持 {len}（「0 = 读 PLC 原生整串」仅西门子 S7 有定义），请填正整数");
                }
                return ApplyRead(Communication.ReadString(addr, (ushort)len, Encoding.ASCII), holder);
            };
        }
        #endregion
    }
}
