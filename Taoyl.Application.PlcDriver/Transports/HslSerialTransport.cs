using HslCommunication.Core.Device;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using System;
using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Taoyl.Application.PlcDriver.Transports
{
    /// <summary>
    /// HSL通讯库串口传输适配器（形态：DeviceSerialPort 家族）
    /// 连接动作：Open / Close（TCP 形态为 ConnectServer / ConnectClose）
    /// 异步说明：HSL 串口无真异步（DeviceSerialPort 仅同步 Open/Close），用 Task.Run 包装释放调用线程
    /// </summary>
    public class HslSerialTransport : HslBaseTransport
    {
        /// <summary>串口设备（强类型引用；基类按 DeviceCommunication 做读写转发）</summary>
        private readonly DeviceSerialPort _serialDevice;

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="device">HSL库的串口设备对象（如 ModbusRtu，is-a DeviceSerialPort）</param>
        public HslSerialTransport(DeviceSerialPort device) : base(device)
        {
            _serialDevice = device;
        }
        #endregion

        #region 串口装配（各串口协议驱动共用）
        /// <summary>
        /// 为 HSL 串口设备装配串口参数（串口参数解析的唯一落点）
        /// 参数来源：端点取 PlcConfig.Ip（如 COM3）；其余 4 项取 ProtocolParams，留空回退默认、填错抛异常
        /// 时序约束：必须在 Open() 之前调用——PipeSerialPort.SerialPortInni 在串口已打开时静默忽略（反编译实证 12.5.3）
        /// 抛出由 DeviceManager.BuildDevice 的事务序兜住（先建新、成功才换旧），不产生任何副作用
        /// </summary>
        /// <param name="device">HSL 串口设备</param>
        /// <param name="plcConfig">PLC 配置（Ip=串口号；ProtocolParams 读 BaudRate/DataBits/StopBits/Parity）</param>
        public static void AttachSerialPipe(DeviceSerialPort device, PlcConfig plcConfig)
        {
            if (device == null)
                throw new ArgumentNullException(nameof(device));
            if (plcConfig == null)
                throw new ArgumentNullException(nameof(plcConfig));

            string portName = NormalizePortName(plcConfig.Ip);
            int baudRate = ParseIntParam(plcConfig, "BaudRate", "9600", 1, int.MaxValue);
            int dataBits = ParseIntParam(plcConfig, "DataBits", "8", 5, 8);
            StopBits stopBits = ParseStopBits(plcConfig.GetProtocolParam("StopBits", "One"));
            Parity parity = ParseParity(plcConfig.GetProtocolParam("Parity", "None"));

            device.SerialPortInni(portName, baudRate, dataBits, stopBits, parity);
            device.ReceiveTimeOut = int.TryParse(plcConfig.ReceiveTimeOut, out var receiveTimeout)
                ? receiveTimeout
                : DefaultReceiveTimeOut;
        }

        /// <summary>
        /// 端点校验：串口协议要求 COMx（大小写不敏感，归一为大写）；非法抛异常而非静默回落
        /// 端点错在现场表现为「连不上且难查」，启动即失败优于运行期猜测
        /// </summary>
        private static string NormalizePortName(string endpoint)
        {
            string portName = endpoint?.Trim();
            if (string.IsNullOrEmpty(portName) || !Regex.IsMatch(portName, @"^COM\d+$", RegexOptions.IgnoreCase))
            {
                throw new ArgumentException(
                    $"串口设备端点非法: \"{endpoint}\"（IP 地址属性对串口协议应填串口号，如 COM3）");
            }
            return portName.ToUpperInvariant();
        }

        /// <summary>
        /// 解析整型协议参数（留空回退默认；非数字或越界抛异常，paramName 仅用于报错定位）
        /// </summary>
        private static int ParseIntParam(PlcConfig plcConfig, string key, string defaultValue, int min, int max)
        {
            string valueText = plcConfig.GetProtocolParam(key, defaultValue);
            if (int.TryParse(valueText?.Trim(), out var value) && value >= min && value <= max)
                return value;

            throw new ArgumentException(
                $"串口参数非法: \"{valueText}\"（应为 {min}~{max} 的整数），请检查 ProtocolParams.{key}");
        }

        /// <summary>
        /// 解析停止位（One / Two / OnePointFive；None 为串口非法值，显式拒绝）
        /// </summary>
        private static StopBits ParseStopBits(string valueText)
        {
            if (Enum.TryParse(valueText?.Trim(), ignoreCase: true, out StopBits stopBits)
                && stopBits != StopBits.None)
            {
                return stopBits;
            }

            throw new ArgumentException(
                $"串口停止位非法: \"{valueText}\"（可选 One/Two/OnePointFive），请检查 ProtocolParams.StopBits");
        }

        /// <summary>
        /// 解析校验位（None / Odd / Even / Mark / Space）
        /// </summary>
        private static Parity ParseParity(string valueText)
        {
            if (Enum.TryParse(valueText?.Trim(), ignoreCase: true, out Parity parity))
                return parity;

            throw new ArgumentException(
                $"串口校验位非法: \"{valueText}\"（可选 None/Odd/Even/Mark/Space），请检查 ProtocolParams.Parity");
        }
        #endregion

        #region 连接管理
        /// <summary>
        /// 打开串口（Pipeline.OpenCommunication：未打开则 Open 并回 true，触发 InitializationOnConnect）
        /// </summary>
        public override ResponseResult Connect()
        {
            try
            {
                return ConvertResult(_serialDevice.Open());
            }
            catch (Exception ex)
            {
                return ResponseResult.Fail($"连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 打开串口（异步版：串口打开是本地阻塞调用，Task.Run 释放调用线程）
        /// </summary>
        public override async Task<ResponseResult> ConnectAsync()
        {
            return await Task.Run(() => Connect()).ConfigureAwait(false);
        }

        /// <summary>
        /// 关闭串口（DeviceSerialPort.Close：已打开才关，忽略关闭异常）
        /// </summary>
        public override void Close()
        {
            try
            {
                _serialDevice.Close();
            }
            catch (Exception)
            {
                // 忽略关闭异常
            }
        }

        /// <summary>
        /// 关闭串口（异步版，与 Close 语义一致）
        /// </summary>
        public override async Task CloseAsync()
        {
            await Task.Run(() => Close()).ConfigureAwait(false);
        }
        #endregion

        #region 连接状态判定
        /// <summary>
        /// 检查操作结果并判断连接状态（串口形态补充）
        /// base 三分法之后，Open 失败的固定前缀仍判为断开
        /// </summary>
        public override bool CheckConnectionStatus(ResponseResult result)
        {
            if (!base.CheckConnectionStatus(result))
                return false;

            // 串口打开失败：PipeSerialPort.OpenCommunication 返回 ErrorCode=10000（默认未知码）+ 固定前缀，
            // 非负码会骗过 base 的三分法，不拦则 UI 显示已连接但读写全失败
            if (!string.IsNullOrEmpty(result.Message)
                && result.Message.Contains("OpenCommunication failed"))
            {
                return false;
            }

            return true;
        }
        #endregion
    }
}
