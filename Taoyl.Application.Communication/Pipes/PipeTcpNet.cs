using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Taoyl.Application.Communication.Interfaces;

namespace Taoyl.Application.Communication.Pipes
{
    public class PipeTcpNet : IPipe, IDisposable
    {
        private TcpClient tcpClient;
        private NetworkStream networkStream;
        private string ipAddress;
        private int port;
        private bool isConnected;
        private bool disposed;
        private SemaphoreSlim streamLock;
        private object connectLock;

        public bool IsConnected => isConnected;

        public PipeTcpNet(string ipAddress, int port)
        {
            this.ipAddress = ipAddress;
            this.port = port;
            streamLock = new SemaphoreSlim(1, 1);
            connectLock = new object();
        }

        public OperateResult Connect()
        {
            lock (connectLock)
            {
                try
                {
                    Disconnect();
                    tcpClient = new TcpClient { NoDelay = true };
                    tcpClient.Connect(ipAddress, port);
                    networkStream = tcpClient.GetStream();
                    isConnected = true;
                    return OperateResult.Success();
                }
                catch (Exception ex)
                {
                    isConnected = false;
                    return OperateResult.Fail($"TCP连接失败: {ex.Message}");
                }
            }
        }

        public async Task<OperateResult> ConnectAsync()
        {
            try
            {
                Disconnect();
                tcpClient = new TcpClient { NoDelay = true };
                await tcpClient.ConnectAsync(ipAddress, port);
                networkStream = tcpClient.GetStream();
                isConnected = true;
                return OperateResult.Success();
            }
            catch (Exception ex)
            {
                isConnected = false;
                return OperateResult.Fail($"TCP异步连接失败: {ex.Message}");
            }
        }

        public OperateResult Disconnect()
        {
            lock (connectLock)
            {
                try
                {
                    networkStream?.Close();
                    tcpClient?.Close();
                    return OperateResult.Success();
                }
                catch (Exception ex)
                {
                    return OperateResult.Fail($"断开异常: {ex.Message}");
                }
                finally
                {
                    networkStream = null;
                    tcpClient = null;
                    isConnected = false;
                }
            }
        }

        public async Task<OperateResult> DisconnectAsync()
        {
            return await Task.Run(() => Disconnect());
        }

        public OperateResult<byte[]> SendAndReceive(byte[] sendData, int timeout = 3000)
        {
            if (!isConnected)
                return OperateResult<byte[]>.Fail("管道未连接");

            streamLock.Wait();
            try
            {
                ClearReceiveBuffer();
                networkStream.Write(sendData, 0, sendData.Length);

                byte[] buffer = new byte[4096];
                int totalRead = 0;
                var timeoutAt = DateTime.Now.AddMilliseconds(timeout);
                while (DateTime.Now < timeoutAt)
                {
                    if (networkStream.DataAvailable)
                    {
                        int read = networkStream.Read(buffer, totalRead, buffer.Length - totalRead);
                        if (read == 0) break;
                        totalRead += read;

                        if (totalRead >= 6)
                        {
                            ushort pduLength = (ushort)((buffer[4] << 8) | buffer[5]);
                            int frameLength = 6 + pduLength;

                            if (totalRead >= frameLength)
                            {
                                byte[] frame = new byte[frameLength];
                                Array.Copy(buffer, 0, frame, 0, frameLength);
                                return OperateResult<byte[]>.Success(frame);
                            }
                        }
                    }
                    else
                    {
                        Thread.Sleep(2);
                    }
                }

                return OperateResult<byte[]>.Fail(totalRead > 0 ? "响应不完整" : "接收超时");
            }
            catch (Exception ex)
            {
                isConnected = false;
                return OperateResult<byte[]>.Fail($"通信异常: {ex.Message}");
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

        private void ClearReceiveBuffer()
        {
            if (networkStream == null || !networkStream.DataAvailable) return;
            byte[] discard = new byte[4096];
            while (networkStream.DataAvailable)
            {
                networkStream.Read(discard, 0, discard.Length);
            }
        }

        public bool CheckConnectionState()
        {
            if (!isConnected || tcpClient == null||!tcpClient.Connected) return false;
            try
            {
                var socket = tcpClient.Client;
                if (socket == null) return false;
                return socket.Send(new byte[0], 0, SocketFlags.None) == 0;
            }
            catch
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                Disconnect();
                streamLock?.Dispose();
                disposed = true;
            }
        }
    }
}