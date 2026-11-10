using System;

namespace Taoyl.Application.PlcDriver.Contracts
{
    /// <summary>
    /// PLC主接口（组合接口）
    /// 继承：连接管理 + 读写操作
    /// 说明：已删除 Enable/DisableReconnection（重连由 PlcRuntime 内部 IoWorker 循环自动管理）
    /// 已删除 EnableCacheMode/DisableCacheMode（缓存模式为连接生命周期内在部分：Connect 首次触发启动 IoWorker，
    /// Close/Dispose 停止；IoWorker 同时是唯一自动重连通道，故 Connect 无论成败均启动，外部零启停入口）
    /// </summary>
    public interface IPlc : IPlcReadWrite, IDisposable
    {
        // ===== 身份标识 =====

        /// <summary>
        /// PLC名称（实例身份标识，构造时定格；地址表 GetTagTable 键、日志文件名等）
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 协议类型（配置原始串，与 Devices.json 的 protocol 字段、UI 协议下拉同源，
        /// 即 PlcProtocol 枚举成员名；供外部诊断/日志识别实例协议）
        /// 消费者：项目内当前无直调点（2026-09-13 评估后保留）——作为对外诊断契约使用，
        ///        调试界面/日志接入时直接读 IPlc 即可，无需向下转型到具体驱动类
        /// 注：协议家族短名版本（原 IPlcProtocolAccess.ProtocolType，值 "EIP"/"S7"）已于 2026-09-13 删除，
        ///    全库协议身份标识只此一处，不存在混用风险
        /// </summary>
        string ProtocolType { get; }

        // ===== 连接状态 =====

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 缓存读写队列深度（在队未执行的缓存读/写请求数，调试界面/监控用）
        /// </summary>
        int QueueDepth { get; }


        // ===== 连接管理 =====

        /// <summary>
        /// 连接PLC
        /// </summary>
        /// <returns>连接是否成功</returns>
        bool Connect();

        /// <summary>
        /// 断开PLC连接（停止轮询/IoWorker → 关闭通讯 → 清空待执行队列；再 Connect 重新启动）
        /// </summary>
        void Close();
    }
}
