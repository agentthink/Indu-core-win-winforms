using NModbus;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// Modbus TCP 主站客户端，基于 ModbusMasterBase 实现连接适配。
    /// 所有读写逻辑、重试策略、强类型转换、连接状态机已收敛到基类，
    /// 本类仅负责 TCP 物理连接的创建、状态判断与释放。
    /// </summary>
    /// </summary>
    public class ModbusTcpMaster : ModbusMasterBase
    {
        private TcpClient tcpClient;
        private IModbusMaster nativeMaster;

        public TcpConnectionParams Parameters => (TcpConnectionParams)parameters;

        protected override bool IsConnectedCore => tcpClient?.Connected ?? false;

        protected override IModbusMaster NativeMaster => nativeMaster;

        public ModbusTcpMaster(TcpConnectionParams parameters) : base(parameters) { }

        protected override OperateResult ConnectCore()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.ReceiveTimeout = Parameters.Timeout;
                tcpClient.SendTimeout = Parameters.Timeout;
                IAsyncResult ar = tcpClient.BeginConnect(Parameters.Ip, Parameters.Port, null, null);
                if (ar.AsyncWaitHandle.WaitOne(TimeSpan.FromMilliseconds(Parameters.ConnectTimeout)))
                {
                    tcpClient.EndConnect(ar);

                    var factory = new ModbusFactory(logger: this.Logger);
                    nativeMaster = factory.CreateMaster(tcpClient);
                    return OperateResult.Success();
                }
                else
                {
                    try { tcpClient.EndConnect(ar); } catch { }
                    tcpClient.Close();
                    tcpClient = null;
                    return OperateResult.Fail("TCP 连接超时。");
                }
            }
            catch (Exception ex)
            {
                tcpClient?.Close();
                tcpClient = null;
                return OperateResult.Fail(ex.Message);
            }
        }

        protected override async Task<OperateResult> ConnectCoreAsync()
        {
            try
            {
                tcpClient = new TcpClient();
                tcpClient.ReceiveTimeout = Parameters.Timeout;
                tcpClient.SendTimeout = Parameters.Timeout;
                var connectTask = tcpClient.ConnectAsync(Parameters.Ip, Parameters.Port);
                if (await Task.WhenAny(connectTask, Task.Delay(Parameters.ConnectTimeout)).ConfigureAwait(false) == connectTask)
                {
                    await connectTask.ConfigureAwait(false);

                    var factory = new ModbusFactory(logger: this.Logger);
                    nativeMaster = factory.CreateMaster(tcpClient);
                    return OperateResult.Success();
                }
                else
                {
                    tcpClient.Close();
                    tcpClient = null;
                    return OperateResult.Fail("TCP 连接超时。");
                }
            }
            catch (Exception ex)
            {
                tcpClient?.Close();
                tcpClient = null;
                return OperateResult.Fail(ex.Message);
            }
        }

        protected override void DisconnectCore()
        {
            nativeMaster?.Dispose();
            nativeMaster = null;
            tcpClient?.Close();
            tcpClient = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                nativeMaster?.Dispose();
                tcpClient?.Close();
            }
            base.Dispose(disposing);
        }
    }
}