using System.IO.Ports;
using System.Text;

namespace Taoyl.Application.Communication
{

    /// <summary>
    /// 设备连接状态机。
    /// 所有状态变更由内部心跳/重连任务统一管理，外部只读。
    /// </summary>
    public enum ConnectionState
    {
        /// <summary>当前无有效连接，也无正在进行的连接操作</summary>
        Disconnected,

        /// <summary>正在进行首次连接</summary>
        Connecting,

        /// <summary>连接已建立，心跳正常，可进行数据读写</summary>
        Connected,

        /// <summary>连接已失效，正在按退避策略执行重连</summary>
        Reconnecting,

        /// <summary>用户主动关闭中，禁止自动重连</summary>
        Closing
    }

    /// <summary>
    /// 多字节数据的字节序格式。
    /// </summary>
    public enum DataFormat
    {
        /// <summary>标准大端模式（Modbus 默认）</summary>
        ABCD,
        /// <summary>字内字节交换，字序不变</summary>
        BADC,
        /// <summary>字序交换，字内字节不变</summary>
        CDAB,
        /// <summary>完全小端模式</summary>
        DCBA
    }

    /// <summary>
    /// 连接参数基类，包含通用通讯与重连策略。
    /// </summary>
    public abstract class ConnectionParams
    {
        /// <summary>从站地址（Modbus slave ID）</summary>
        public byte SlaveId { get; set; } = 1;

        /// <summary>多字节数据的字节序</summary>
        public DataFormat DataFormat { get; set; } = DataFormat.ABCD;

        /// <summary>单次读写超时时间（毫秒）</summary>
        public int Timeout { get; set; } = 1000;

        /// <summary>建立物理连接的超时时间（毫秒）</summary>
        public int ConnectTimeout { get; set; } = 3000;

        /// <summary>是否在断线时自动重连</summary>
        public bool AutoReconnect { get; set; } = true;

        /// <summary>
        /// 自动重连最大尝试次数（0 表示无限重试，直到成功）。
        /// </summary>
        public int MaxReconnectRetries { get; set; } = 0;

        /// <summary>
        /// 自动重连的初始退避时间（毫秒）。
        /// 首次重连失败后等待此时间再试，之后按指数退避增长。
        /// </summary>
        public int ReconnectDelayMs { get; set; } = 1000;

        /// <summary>
        /// 自动重连的最大退避时间（毫秒）。
        /// 退避时间不会无限增长，达到此上限后保持固定。
        /// </summary>
        public int MaxReconnectDelayMs { get; set; } = 30000;

        /// <summary>单次读写操作因通讯失败时的重试次数（0 表示不重试）</summary>
        public int MaxRetries { get; set; } = 0;

        /// <summary>
        /// 单次读写重试间隔（毫秒），默认 200ms。
        /// 仅用于 MaxRetries > 0 时的重试等待，与自动重连的退避策略无关。
        /// </summary>
        public int ReadRetryIntervalMs { get; set; } = 200;

        /// <summary>
        /// 连续两条 Modbus 命令之间的最小间隔（毫秒）。
        /// 某些串口设备处理速度较慢，需要喘息时间，默认 0 表示不延迟。
        /// </summary>
        public int DelayBetweenMessages { get; set; } = 0;

        #region 应用层心跳参数（新增）

        /// <summary>
        /// 是否启用应用层心跳检测。
        /// 启用后，后台心跳任务会周期性读取指定寄存器以验证设备业务层存活。
        /// 若未启用，仅依赖 tcpClient.Connected / serialPort.IsOpen 判断物理连接。
        /// </summary>
        public bool EnableHeartbeat { get; set; } = false;

        /// <summary>
        /// 心跳检测间隔（毫秒）。
        /// 心跳任务每此间隔执行一次伪心跳读取。
        /// </summary>
        public int HeartbeatIntervalMs { get; set; } = 5000;

        /// <summary>
        /// 心跳超时时间（毫秒）。
        /// 超过此时间未收到心跳响应，则判定连接已失效，进入重连流程。
        /// </summary>
        public int HeartbeatTimeoutMs { get; set; } = 15000;

        /// <summary>
        /// 伪心跳读取的起始寄存器地址。
        /// 心跳任务通过读取此地址验证设备响应能力。
        /// </summary>
        public ushort HeartbeatAddress { get; set; } = 0;

        /// <summary>
        /// 伪心跳读取的寄存器数量。
        /// </summary>
        public ushort HeartbeatLength { get; set; } = 1;

        public Encoding TextEncoding { get; set; } = Encoding.ASCII;

        #endregion

        #region 日志配置

        /// <summary>
        /// 原始报文日志文件路径模板，支持 string.Format 占位符，例如：
        /// @"D:\Logs\ModbusLogs\dev{0:yyyyMMdd}.txt"
        /// 或 @"D:\Logs\ModbusLogs\dev{0:yyyyMMdd HH}.txt"
        /// 为空或 null 则禁用日志记录。
        /// </summary>
        public string LogPathTemplate { get; set; }
        #endregion
    }

    /// <summary>
    /// Modbus TCP 连接参数。
    /// </summary>
    public class TcpConnectionParams : ConnectionParams
    {
        /// <summary>目标 IP 地址</summary>
        public string Ip { get; set; }

        /// <summary>端口号（默认 502）</summary>
        public int Port { get; set; } = 502;
    }

    /// <summary>
    /// Modbus RTU 串口连接参数。
    /// </summary>
    public class RtuConnectionParams : ConnectionParams
    {
        /// <summary>串口名称，例如 COM3</summary>
        public string PortName { get; set; }

        /// <summary>波特率</summary>
        public int BaudRate { get; set; } = 9600;

        /// <summary>数据位</summary>
        public int DataBits { get; set; } = 8;

        /// <summary>校验位</summary>
        public Parity Parity { get; set; } = Parity.None;

        /// <summary>停止位</summary>
        public StopBits StopBits { get; set; } = StopBits.One;
    }
}