using System;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using Taoyl.Application.Communication.Interfaces;

namespace Taoyl.Application.Communication.Pipes
{
    public class PipeSerialPort : IPipe
    {
        private SerialPort serialPort;
        private string portName;
        private int baudRate;
        private Parity parity;
        private int dataBits;
        private StopBits stopBits;
        private SemaphoreSlim streamLock;

        public bool IsConnected => serialPort?.IsOpen ?? false;

        public PipeSerialPort(string portName, int baudRate = 9600, Parity parity = Parity.None, int dataBits = 8, StopBits stopBits = StopBits.One)
        {
            this.portName = portName;
            this.baudRate = baudRate;
            this.parity = parity;
            this.dataBits = dataBits;
            this.stopBits = stopBits;
            streamLock = new SemaphoreSlim(1, 1);
        }

        public OperateResult Connect()
        {
            try
            {
                serialPort = new SerialPort(portName, baudRate, parity, dataBits, stopBits)
                {
                    ReadTimeout = 3000,
                    WriteTimeout = 3000
                };
                serialPort.Open();
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                return OperateResult.Fail($"串口打开失败: {ex.Message}");
            }
        }

        public async Task<OperateResult> ConnectAsync()
        {
            return await Task.Run(() => Connect());
        }

        public OperateResult Disconnect()
        {
            try
            {
                serialPort?.Close();
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                return OperateResult.Fail($"串口关闭异常: {ex.Message}");
            }
            finally
            {
                serialPort = null;
            }
        }

        public async Task<OperateResult> DisconnectAsync()
        {
            return await Task.Run(() => Disconnect());
        }

        public OperateResult<byte[]> SendAndReceive(byte[] sendData, int timeout = 3000)
        {
            if (!IsConnected)
                return OperateResult<byte[]>.Fail("串口未打开");

            streamLock.Wait();
            try
            {
                serialPort.DiscardInBuffer();
                serialPort.Write(sendData, 0, sendData.Length);

                byte[] buffer = new byte[4096];
                int totalRead = 0;
                DateTime start = DateTime.Now;

                while ((DateTime.Now - start).TotalMilliseconds < timeout)
                {
                    if (serialPort.BytesToRead > 0)
                    {
                        int read = serialPort.Read(buffer, totalRead, serialPort.BytesToRead);
                        totalRead += read;
                        Thread.Sleep(50);
                    }
                }

                if (totalRead == 0)
                    return OperateResult<byte[]>.Fail("接收超时");

                byte[] result = new byte[totalRead];
                Array.Copy(buffer, 0, result, 0, totalRead);
                return OperateResult<byte[]>.Success(result);
            }
            catch (Exception ex)
            {
                return OperateResult<byte[]>.Fail($"串口通信异常: {ex.Message}");
            }
            finally
            {
                streamLock.Release();
            }
        }

        public async Task<OperateResult<byte[]>> SendAndReceiveAsync(byte[] sendData, int timeout = 3000)
        {
            return await Task.Run(() => SendAndReceive(sendData, timeout));
        }
    }
}