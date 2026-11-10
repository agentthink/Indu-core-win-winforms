using NModbus;
using NModbus.Serial;
using System;
using System.IO.Ports;
using System.Threading.Tasks;

namespace Taoyl.Application.Communication
{
    /// <summary>
    /// Modbus RTU 主站客户端，基于 ModbusMasterBase 实现连接适配。
    /// 所有读写逻辑、重试策略、强类型转换、连接状态机已收敛到基类，
    /// 本类仅负责串口物理连接的创建、状态判断与释放。
    /// </summary>
    public class ModbusRtuMaster : ModbusMasterBase
    {
        private SerialPort serialPort;
        private IModbusSerialMaster nativeMaster;

        /// <summary>获取 RTU 连接参数</summary>
        public RtuConnectionParams Parameters => (RtuConnectionParams)parameters;

        protected override bool IsConnectedCore => serialPort?.IsOpen ?? false;

        protected override IModbusMaster NativeMaster => nativeMaster;

        public ModbusRtuMaster(RtuConnectionParams parameters) : base(parameters) { }

        protected override OperateResult ConnectCore()
        {
            try
            {
                serialPort = new SerialPort(Parameters.PortName, Parameters.BaudRate, Parameters.Parity, Parameters.DataBits, Parameters.StopBits);
                serialPort.ReadTimeout = Parameters.Timeout;
                serialPort.WriteTimeout = Parameters.Timeout;
                serialPort.Open();

                //  正确使用工厂构造，仅传入 Logger（可为 null）
                var factory = new ModbusFactory(logger: this.Logger);
                nativeMaster = factory.CreateRtuMaster(serialPort);
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                return OperateResult.Fail(ex.Message);
            }
        }

        protected override async Task<OperateResult> ConnectCoreAsync()
        {
            try
            {
                serialPort = new SerialPort(Parameters.PortName, Parameters.BaudRate, Parameters.Parity, Parameters.DataBits, Parameters.StopBits);
                serialPort.ReadTimeout = Parameters.Timeout;
                serialPort.WriteTimeout = Parameters.Timeout;
                await Task.Run(() => serialPort.Open()).ConfigureAwait(false);

                var factory = new ModbusFactory(logger: this.Logger);
                nativeMaster = factory.CreateRtuMaster(serialPort);
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                return OperateResult.Fail(ex.Message);
            }
        }

        protected override void DisconnectCore()
        {
            nativeMaster?.Dispose();
            nativeMaster = null;
            serialPort?.Close();
            serialPort = null;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                nativeMaster?.Dispose();
                serialPort?.Close();
            }
            base.Dispose(disposing);
        }
    }
}