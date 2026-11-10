using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Cache;
using System;

namespace Taoyl.Application.PlcDriver.Protocols
{
    /// <summary>
    /// PLC协议访问接口（协议层：怎么解释数据类型）
    /// 职责：抽象不同PLC协议的数据类型解释
    /// 含 Read/Write 策略（TryGetXxxStrategy 按需查询），String 长度/编码等协议差异由子类覆写
    /// 执行链路：PlcRuntime → IoExecutor.ExecuteSession → TryGetXxxStrategy → Transport 原语
    /// 注：原 ParseAddress 已于 2026-09-12 删除（全库零调用点，属预抽象死代码）
    /// 注：原 ProtocolType（协议家族短名 "EIP"/"S7"）已于 2026-09-13 删除——全库零消费者，
    /// 且与 IPlc.ProtocolType（配置原始串 = PlcProtocol 成员名）同名不同义易误用；协议身份一律用 IPlc.ProtocolType
    /// </summary>
    public interface IPlcProtocolAccess
    {
        #region 核心属性
        /// <summary>
        /// 获取通讯传输层实例
        /// </summary>
        IPlcTransport Communication { get; }
        #endregion

        #region 策略查询（协议差异容器）
        /// <summary>
        /// 尝试获取读取策略（未注册的数据类型返回 false，不抛异常）
        /// 委托签名：(地址, 长度, 值容器输出槽) -> 结果；长度=数组元素个数或字符串长度（单值类型忽略）
        /// 值容器输出槽：非 raw 模式为标签 TypedValue 引用，读成功时值在泛型闭包内强类型写入（零装箱）；
        /// 容器为 null（raw 模式）时仅返回结果不写值
        /// </summary>
        /// <param name="dataType">PLC数据类型</param>
        /// <param name="strategy">读取策略</param>
        /// <returns>true=命中</returns>
        bool TryGetReadStrategy(PlcDataType dataType, out Func<string, int, ITypedValue, ResponseResult> strategy);

        /// <summary>
        /// 尝试获取写入策略（未注册的数据类型返回 false，不抛异常）
        /// 委托签名：(地址, 值, 长度) -> 结果；长度=数组元素个数或字符串长度（单值类型忽略）
        /// </summary>
        /// <param name="dataType">PLC数据类型</param>
        /// <param name="strategy">写入策略</param>
        /// <returns>true=命中</returns>
        bool TryGetWriteStrategy(PlcDataType dataType, out Func<string, object, int, ResponseResult> strategy);
        #endregion
    }
}
