using HslCommunication.Core.Device;
using HslCommunication.Core.Pipe;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using System;
using System.Threading.Tasks;

namespace Taoyl.Application.PlcDriver.Transports
{
    /// <summary>
    /// HSL通讯库TCP传输适配器（形态：DeviceTcpNet 家族）
    /// 连接动作：ConnectServer / ConnectClose（串口形态为 Open / Close）
    /// 附加职责：AttachTcpPipe —— 各协议驱动共用的 TCP 管道装配（IP/端口兜底/超时/关管策略）
    /// </summary>
    public class HslTcpTransport : HslBaseTransport
    {
        /// <summary>TCP设备（强类型引用；基类按 DeviceCommunication 做读写转发）</summary>
        private readonly DeviceTcpNet _tcpDevice;

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="device">HSL库的TCP设备对象（如 OmronConnectedCipNet，is-a DeviceTcpNet）</param>
        public HslTcpTransport(DeviceTcpNet device) : base(device)
        {
            _tcpDevice = device;
        }
        #endregion

        #region 管道装配（各协议驱动共用）
        /// <summary>默认连接超时（ms）：PlcConfig.ConnectTimeOut 未配置时使用</summary>
        private const int DefaultConnectTimeOut = 5000;

        /// <summary>
        /// 为 HSL 设备装配 TCP 管道（IP / 端口兜底 / 两个超时 / 关管策略的唯一装配点）
        /// 管道参数是通讯层通用参数、与协议无关，故各协议驱动（CIP / S7 / FINS / ModbusTcp）共用：
        /// 驱动只传机型专属参数（默认端口），装配细节不重复落两份
        /// </summary>
        /// <param name="device">HSL TCP设备（is-a DeviceTcpNet）</param>
        /// <param name="plcConfig">PLC 配置（读顶层属性 Ip / Port / ConnectTimeOut / ReceiveTimeOut）</param>
        /// <param name="defaultPort">协议默认端口（CIP 44818 / S7 102 / FINS 9600 / Modbus 502），Port 未配置时使用</param>
        public static void AttachTcpPipe(DeviceTcpNet device, PlcConfig plcConfig, int defaultPort)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (plcConfig == null)
                throw new ArgumentNullException(nameof(plcConfig));

            device.CommunicationPipe = new PipeTcpNet()
            {
                IpAddress = plcConfig.Ip,
                Port = int.TryParse(plcConfig.Port, out var port) ? port : defaultPort,
                ConnectTimeOut = int.TryParse(plcConfig.ConnectTimeOut, out var connectTimeout) ? connectTimeout : DefaultConnectTimeOut,
                ReceiveTimeOut = int.TryParse(plcConfig.ReceiveTimeOut, out var receiveTimeout) ? receiveTimeout : DefaultReceiveTimeOut,
                // 接收超时不交 HSL 关管（HSL 默认 1 = 首次超时即 CloseCommunication）：否则 HSL 在背后关管后，
                // 下一次读写又静默 OpenCommunication 重开，等于绕过 PlcConnectionState 的 N=3 防抖与指数退避，
                // 形成两套不同步的重连。关管决策唯一归 PlcConnectionState：
                // PipeTcpNet.Receive 仅「软超时」分支（Content == int.MaxValue）看此值；
                // 真实断线（拔网线/RST）走 else 分支无条件 CloseCommunication，自愈不受影响；
                // 且 ConnectServer() 首行即 CloseCommunication()，重连必拿干净 socket。
                CloseOnRecvTimeOutTick = -1,
            };
        }
        #endregion

        #region 连接管理
        /// <summary>
        /// 连接PLC设备（去反射，直接调用 ConnectServer）
        /// </summary>
        public override ResponseResult Connect()
        {
            try
            {
                return ConvertResult(_tcpDevice.ConnectServer());
            }
            catch (Exception ex)
            {
                return ResponseResult.Fail($"连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 连接PLC设备（异步版：HSL DeviceTcpNet.ConnectServerAsync 为真异步——
        /// await 管道级 Close/OpenCommunicationAsync + InitializationOnConnectAsync，反编译实证 12.5.3）
        /// </summary>
        public override async Task<ResponseResult> ConnectAsync()
        {
            try
            {
                return ConvertResult(await _tcpDevice.ConnectServerAsync().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                return ResponseResult.Fail($"连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开PLC连接（去反射，直接调用 ConnectClose）
        /// </summary>
        public override void Close()
        {
            try
            {
                _tcpDevice.ConnectClose();
            }
            catch (Exception)
            {
                // 忽略关闭异常
            }
        }

        /// <summary>
        /// 断开PLC连接（异步版，与 Close 语义一致：忽略关闭结果与异常）
        /// </summary>
        public override async Task CloseAsync()
        {
            try
            {
                await _tcpDevice.ConnectCloseAsync().ConfigureAwait(false);
            }
            catch (Exception)
            {
                // 忽略关闭异常
            }
        }
        #endregion

        #region 连接状态判定
        /// <summary>
        /// 检查操作结果并判断连接状态（TCP 形态补充）
        /// base 三分法之后，ErrorCode 非负但物理链路已断的消息仍判为断开
        /// </summary>
        public override bool CheckConnectionStatus(ResponseResult result)
        {
            if (!base.CheckConnectionStatus(result))
                return false;

            // 积极拒绝 / 强迫关闭：ErrorCode 可能非负但 socket 已不可用（TCP 专属，串口不会产生）
            if (!string.IsNullOrEmpty(result.Message)
                && (result.Message.Contains("由于目标计算机积极拒绝，无法连接")
                    || result.Message.Contains("强迫关闭")))
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
