using System.Threading.Tasks;


namespace Taoyl.Application.Communication.Interfaces
{
    /// <summary>
    /// 通信管道接口，定义物理层的连接、断开及原始报文收发能力。
    /// </summary>
    public interface IPipe
    {
        /// <summary>
        /// 是否处于连接状态
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 同步建立连接
        /// </summary>
        OperateResult Connect();

        /// <summary>
        /// 异步建立连接
        /// </summary>
        Task<OperateResult> ConnectAsync();

        /// <summary>
        /// 同步断开连接
        /// </summary>
        OperateResult Disconnect();

        /// <summary>
        /// 异步断开连接
        /// </summary>
        Task<OperateResult> DisconnectAsync();

        /// <summary>
        /// 同步发送报文并接收完整响应
        /// </summary>
        /// <param name="sendData">要发送的字节数组</param>
        /// <param name="timeout">接收超时毫秒数</param>
        /// <returns>接收到的字节数组</returns>
        OperateResult<byte[]> SendAndReceive(byte[] sendData, int timeout = 3000);

        /// <summary>
        /// 异步发送报文并接收完整响应
        /// </summary>
        /// <param name="sendData">要发送的字节数组</param>
        /// <param name="timeout">接收超时毫秒数</param>
        /// <returns>接收到的字节数组</returns>
        Task<OperateResult<byte[]>> SendAndReceiveAsync(byte[] sendData, int timeout = 3000);
    }
}