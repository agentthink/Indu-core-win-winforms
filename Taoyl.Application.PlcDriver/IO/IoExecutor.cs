using Taoyl.Application.PlcDriver.Config;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Logging;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Cache;
using Taoyl.Application.PlcDriver.Protocols;
using System;
using System.Collections.Generic;

namespace Taoyl.Application.PlcDriver.IO
{
    /// <summary>
    /// IO执行器（唯一执行入口）
    /// 职责：会话模式 ExecuteSession（锁内 Execute → 锁外连接状态更新 → 锁外 Commit）+ 轮询生成 + IoQueue 内封
    /// 持：_ioLock/_directBusy/协议层访问（TryGetXxxStrategy 按需现查，不缓存字典引用）
    /// 地址表不缓存引用：每次经 IPlcConfigManager.GetTagTable(plcName) 现取（改即生效，无 Reload）
    /// 调用方：直接读写 = ExecuteSession(req, true) 同步返；IoWorker 循环 = ExecuteSession(req, false)
    /// </summary>
    public class IoExecutor
    {
        #region 成员变量
        private readonly string _plcName;
        private readonly IPlcConfigManager _configManager;
        private readonly PlcConnectionState _connectionState;
        private readonly IPlcLogger _logger;

        /// <summary>协议层访问（读写策略按需现查 TryGetXxxStrategy；不缓存字典引用，不再暴露内部字典）</summary>
        private readonly IPlcProtocolAccess _protocolAccess;

        /// <summary>IO单队列（内封，缓存读写入队/出队唯一入口）</summary>
        private readonly IoQueue _ioQueue = new IoQueue();

        /// <summary>通讯互斥锁（锁只包 Execute，临界区最小）</summary>
        private readonly object _ioLock = new object();

        /// <summary>直接读写进行中标志（期间 IoWorker 暂停出队）</summary>
        private volatile bool _directBusy;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="plcName">PLC名称（地址表 GetTagTable 键）</param>
        /// <param name="protocolAccess">协议层访问（策略字典 + Communication 传输原语）</param>
        /// <param name="configManager">配置管理器（地址表唯一持有）</param>
        /// <param name="connectionState">连接状态机（锁外更新）</param>
        /// <param name="logger">实例级日志</param>
        public IoExecutor(string plcName, IPlcProtocolAccess protocolAccess, IPlcConfigManager configManager,
            PlcConnectionState connectionState, IPlcLogger logger)
        {
            if (protocolAccess == null)
                throw new ArgumentNullException(nameof(protocolAccess));
            if (protocolAccess.Communication == null)
                throw new ArgumentNullException(nameof(protocolAccess));

            _plcName = plcName ?? throw new ArgumentNullException(nameof(plcName));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
            _connectionState = connectionState ?? throw new ArgumentNullException(nameof(connectionState));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // 协议层访问（怎么解释数据类型，含 String 编码差异覆写；transport 由策略闭包持有）
            _protocolAccess = protocolAccess;
        }
        #endregion

        #region 会话模式（唯一执行入口）
        /// <summary>
        /// 执行IO会话（消除手动五步：_directBusy/锁/策略/状态更新/Commit 统一收口）
        /// 流程：门禁（未连接即拒）→ 锁内 Execute（查协议层策略字典→transport原语）→ 锁外 connectionState.UpdateFromResult（防抖）→ 锁外 Commit（Value+三件套+RWTimes≥9999归0）
        /// </summary>
        /// <param name="request">IO请求</param>
        /// <param name="syncDirect">true=直接读写（同步返，_directBusy 置位暂停出队）；false=IoWorker 会话（出队执行）</param>
        /// <returns>操作结果</returns>
        public ResponseResult ExecuteSession(IoRequest request, bool syncDirect)
        {
            if (request == null)
                return ResponseResult.Fail("IO请求为空");

            // 【断开门禁】主动关闭后拒绝任何读写：
            // HSL 的 ReadFromCoreServer 每次调用都会先 CommunicationPipe.OpenCommunication()，
            // 未开管则静默开管并执行 InitializationOnConnect()（CIP = RegisterSession + LargeForwardOpen）——
            // 即 Close() 之后任何一次直接读写都会把连接偷偷拉回来，绕过状态机与退避，白占 PLC 侧 CIP 连接槽。
            // 判据用 IsConnected（不另设"用户主动关闭"标志）：
            //   ① IoWorker.RunAsync 主循环本就以 IsConnected 为门禁，重连走 ShouldAttemptReconnect/TryReconnectAsync
            //      独立分支（不经 Execute）→ 不会掐断"断线后自动自愈"；
            //   ② Connect() 成功时 UpdateConnectionFromResult 即置 true（N=3 防抖只作用于断开方向）→ 不影响刚连上后的读写。
            // 此处不调 UpdateConnectionFromResult：这是前置条件不满足，不是 IO 结果，不应参与断线防抖计数。
            if (!_connectionState.IsConnected)
                return ResponseResult.Fail("PLC 未连接，拒绝读写", -1);

            if (syncDirect)
                _directBusy = true;
            try
            {
                // 锁内执行（直接读写与 IoWorker 会话天然互斥）；标签锁内一次查出、带出锁外供提交复用
                // （原先 Execute 与 Commit 各查一次地址表 → 单次 IO 查 3 次；且锁内外可能拿到不同实例）
                ResponseResult result;
                TagItem tag;
                lock (_ioLock)
                {
                    result = Execute(request, out tag);
                }

                // 锁外：状态更新（所有来源一致）+ 提交
                _connectionState.UpdateConnectionFromResult(result);
                Commit(request, tag, result);

                return result;
            }
            finally
            {
                if (syncDirect)
                    _directBusy = false;
            }
        }
        #endregion

        #region 轮询生成
        /// <summary>
        /// 遍历地址表（现取），生成到期的缓存读请求
        /// </summary>
        public List<IoRequest> GeneratePollRequests()
        {
            var tagTable = _configManager.GetTagTable(_plcName);
            if (tagTable == null)
                return null;

            var requests = new List<IoRequest>();
            DateTime now = DateTime.Now;
            foreach (var groupKv in tagTable)
            {
                foreach (var tag in groupKv.Value.Tags.Values)
                {
                    if (tag.RefreshFreq > 0 && tag.IsDue(now))
                    {
                        requests.Add(new IoRequest(IoRequestKind.Read, tag.Address, tag.DataType, tag.Length, null, groupKv.Key, tag.TagName));
                    }
                }
            }
            return requests;
        }
        #endregion

        #region IoQueue 封装（缓存读写唯一通道）
        /// <summary>
        /// 入队（缓存读按 Kind+组+标签去重，写不去重；直接读写不入队）
        /// </summary>
        public bool TryEnqueue(IoRequest request)
        {
            return _ioQueue.TryEnqueue(request);
        }

        /// <summary>
        /// 出队（阻塞等待指定毫秒）
        /// </summary>
        public bool TryDequeue(out IoRequest request, int millisecondsTimeout)
        {
            return _ioQueue.TryDequeue(out request, millisecondsTimeout);
        }

        /// <summary>
        /// 标记队列完成（优雅关闭，仅 Dispose 调用）
        /// </summary>
        public void CompleteAdding()
        {
            _ioQueue.CompleteAdding();
        }

        /// <summary>
        /// 清空队列（Close 会话终结调用：丢弃本会话残留请求；队列可复用，下次 Connect 干净起步）
        /// </summary>
        public void ClearQueue()
        {
            _ioQueue.Clear();
        }

        /// <summary>
        /// 是否有直接读写正在执行（直接读写期间 IoWorker 暂停出队）
        /// </summary>
        public bool IsDirectBusy => _directBusy;

        /// <summary>
        /// 当前队列深度（缓存读写在队请求数，调试界面/监控用）
        /// </summary>
        public int QueueDepth => _ioQueue.Count;
        #endregion

        #region 私有辅助
        /// <summary>
        /// 锁内执行：校验标签（raw 模式跳过）→ 查协议层策略 → 传输层原语（调用方保证锁内）
        /// tag 经 out 带出，供锁外提交复用（免重复查表，且保证锁内外是同一实例）
        /// </summary>
        private ResponseResult Execute(IoRequest request, out TagItem tag)
        {
            // raw 模式（ReadRaw/WriteRaw：组名/标签名为空）跳过标签校验，直接按请求执行
            tag = null;
            if (!string.IsNullOrEmpty(request.GroupName) || !string.IsNullOrEmpty(request.TagName))
            {
                tag = FindTag(request.GroupName, request.TagName);
                if (tag == null)
                    return ResponseResult.Fail($"标签不存在: {request.GroupName}.{request.TagName}");
            }

            // 请求自包含执行要素（地址/数据类型/长度），按 Kind 查策略
            // 读策略带值容器输出槽：tag.TypedValue（raw 模式 tag=null → 容器 null，策略仅返回结果不写值）
            // 未注册的数据类型返回 Fail（旧实现直接索引字典 → 抛 KeyNotFoundException）
            if (request.Kind == IoRequestKind.Read)
            {
                if (!_protocolAccess.TryGetReadStrategy(request.DataType, out var readStrategy))
                    return ResponseResult.Fail($"协议不支持的数据类型: {request.DataType}");

                return readStrategy(request.Address, request.Length, tag?.TypedValue);
            }

            if (!_protocolAccess.TryGetWriteStrategy(request.DataType, out var writeStrategy))
                return ResponseResult.Fail($"协议不支持的数据类型: {request.DataType}");

            return writeStrategy(request.Address, request.Value, request.Length);
        }

        /// <summary>
        /// 锁外提交：读成功值已由策略闭包强类型写入 tag.TypedValue（零装箱，此处不再碰值）；
        /// 写成功保留写入值（低频 object 视图）+ 三件套（LastOperationTime/Result/Msg + RWTimes≥9999归0）
        /// </summary>
        /// <param name="request">IO请求</param>
        /// <param name="tag">标签（Execute 锁内带出；null = raw 模式或标签不存在）</param>
        /// <param name="result">操作结果</param>
        private void Commit(IoRequest request, TagItem tag, ResponseResult result)
        {
            if (tag == null)
                return;

            // 写成功保留写入值（object 低频写统一经容器 SetObject；读成功值已进容器，勿再写以免无谓装箱）
            if (result.IsSuccess && request.Kind == IoRequestKind.Write)
            {
                tag.TypedValue.SetObject(request.Value);
            }

            CommitToTag(tag, result);
        }

        /// <summary>
        /// 按组名+标签名查找标签项（地址表现取）
        /// </summary>
        private TagItem FindTag(string groupName, string tagName)
        {
            if (string.IsNullOrEmpty(groupName) || string.IsNullOrEmpty(tagName))
                return null;

            var tagTable = _configManager.GetTagTable(_plcName);
            if (tagTable == null)
                return null;

            if (tagTable.TryGetValue(groupName, out var group))
            {
                if (group.Tags.TryGetValue(tagName, out var tag))
                {
                    return tag;
                }
            }
            return null;
        }

        /// <summary>
        /// 提交三件套到标签项（LastOperationTime/Result/Msg + RWTimes≥9999归0）
        /// </summary>
        private void CommitToTag(TagItem tag, ResponseResult result)
        {
            tag.LastOperationTime = DateTime.Now;
            tag.LastOperationResult = result.IsSuccess;
            tag.LastOperationMsg = result.Message ?? string.Empty;
            tag.RWTimes = tag.RWTimes >= 9999 ? 1 : tag.RWTimes + 1;
        }
        #endregion
    }
}
