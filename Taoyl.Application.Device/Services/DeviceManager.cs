using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.Device.Models;

namespace Taoyl.Application.Device.Services
{
    /// <summary>
    /// 设备管理器：单类多契约组合实现——设备实例纯域（IDeviceManager）+ 设备配置纯域（IDeviceConfigManager）+
    /// 组合编排域（IDeviceOrchestration，继承前两者并叠加业务用例）。
    /// 能力划分（2026-09-07 架构定稿 + 22:3x 复盘收敛）：
    /// - IDeviceManager（实例纯域）：实例注册表的 构建(BuildDevice)/获取/移除/连接，零文件零镜像（LoadFromConfigFile 已移编排域）；
    /// - IDeviceConfigManager（配置纯域）：Devices.json 镜像节的 查/增/改/删 + 落盘(.bak)，不创建/释放实例；
    /// - IDeviceOrchestration（编排域，UI 与启动装配默认入口）：跨两域的原子业务用例——AddDevice（按配置新增）/
    ///   UpdateDevice（保存生效：释放旧实例→驱动缓存同步→配置 upsert→构建）/ DeleteDevice（释放+缓存清理+删节）/
    ///   LoadFromConfigFile（启动装配：配置镜像载入 + 按清单批量构建）。组合用例内部 = 校验/查重/驱动缓存同步
    ///   + 按序调两纯域段，原子性靠单一 _deviceLock 保持（单类不拆的原因）。
    /// 设备类型扩展：配置模型 ↔ 节 映射经 IDeviceConfigSection 注入（新增类型=模型+节+注册一行）；
    /// 编排的"驱动缓存同步"为设备类型特有段（PLC=缓存型需先同步缓存；Scale/Scanner=直读型无此步），
    /// 现收敛为 IDeviceConfigSyncHook 端口（原 `if (deviceType == PLC)` 分支已搬到 PlcConfigSyncHook）。
    /// 🔴 本类零驱动、零日志依赖：不引 PlcDriver、不引 LogDriver——日志走 IDeviceLogSink，
    ///    配置节走 IDeviceConfigSection，驱动缓存同步走 IDeviceConfigSyncHook。
    ///    这是它能整体搬进「零 ProjectReference」的 Device 库的前提（K8 边界）。
    /// 注意：编排类方法操作后旧 IDevice 引用失效，持有方必须重新 GetDevice（见 IDeviceManager 头注）。
    /// </summary>
    public class DeviceManager : IDeviceOrchestration
    {
        // 类型 -> 名称 -> 设备
        private readonly ConcurrentDictionary<string, ConcurrentDictionary<string, IDevice>> _devices
            = new ConcurrentDictionary<string, ConcurrentDictionary<string, IDevice>>();

        private readonly IEnumerable<IDeviceFactory> _factories;
        private readonly IEnumerable<IDeviceConfigLoader> _configLoaders;
        private readonly IDeviceConfigStore _configStore;
        private readonly IEnumerable<IDeviceConfigSection> _configSections;
        private readonly IEnumerable<IDeviceConfigSyncHook> _configSyncHooks;
        private readonly IDeviceLogSink _logSink;

        // 设备生命周期操作互斥锁（BuildDevice 释放旧+建新 原子性；配置编排 校验→缓存→落盘→构建 原子性；LoadFromConfigFile 启动加载单线程，与构建不并发）
        private readonly object _deviceLock = new object();

        public DeviceManager(
            IEnumerable<IDeviceFactory> factories,
            IEnumerable<IDeviceConfigLoader> configLoaders,
            IDeviceConfigStore configStore,
            IEnumerable<IDeviceConfigSection> configSections,
            IEnumerable<IDeviceConfigSyncHook> configSyncHooks,
            IDeviceLogSink logSink)
        {
            _factories = factories ?? throw new ArgumentNullException(nameof(factories));
            _configLoaders = configLoaders ?? throw new ArgumentNullException(nameof(configLoaders));
            _configStore = configStore ?? throw new ArgumentNullException(nameof(configStore));
            _configSections = configSections ?? throw new ArgumentNullException(nameof(configSections));
            _configSyncHooks = configSyncHooks ?? throw new ArgumentNullException(nameof(configSyncHooks));
            _logSink = logSink ?? throw new ArgumentNullException(nameof(logSink));
        }

        #region 设备实例纯域（IDeviceManager）：注册表 构建/获取/移除/连接，不触碰配置文件

        public IReadOnlyList<IDevice> AllDevices
        {
            get
            {
                return _devices.Values.SelectMany(dict => dict.Values).ToList().AsReadOnly();
            }
        }

        public IReadOnlyList<IDevice> GetDevicesByType(string deviceType)
        {
            if (_devices.TryGetValue(deviceType, out var dict))
                return dict.Values.ToList().AsReadOnly();
            return new List<IDevice>().AsReadOnly();
        }

        public T GetDevice<T>(string name) where T : class, IDevice
        {
            foreach (var typeDict in _devices.Values)
            {
                if (typeDict.TryGetValue(name, out var device) && device is T typed)
                    return typed;
            }
            return null;
        }

        public IDevice GetDevice(string name)
        {
            foreach (var typeDict in _devices.Values)
            {
                if (typeDict.TryGetValue(name, out var device))
                    return device;
            }
            return null;
        }

        /// <summary>
        /// 构建（登记）指定设备——实例 upsert 的唯一入口：先用配置创建新实例，成功后再完成键切换（摘旧键 → 占位新 → 释放旧）
        /// 并放回管理器。无旧实例 = 首次构建（编排 AddDevice 尾部）；有旧实例 = 重建（保存生效 / 调试「重建」键）。
        /// 五条不变式（改它之前逐条读）：
        /// ① 唯一实例创建入口（绕过它直接建实例 = 传新配置却建出旧实例）；
        /// ② 先建新、成功才换旧（CreateDevice 抛异常 → 旧实例未释放、实例表未动）；
        /// ③ 摘旧 → 立刻占位 → 最后释放，三步不许换序（Dispose 夹中间会让键空窗至 IoWorker 退出，上限 2s）；
        /// ④ Dispose 锁内同步（PLC 并发连接数限制，异步会出现新旧双连 → 拒连）；
        /// ⑤ 不落盘、不同步缓存（手动调 = 纯重置；让配置生效走 UpdateDevice）。
        /// ⚠️ 实例配置源 = 驱动缓存（PLC：工厂 CreateDevice 经同名缓存短路取最新，非入参 config 直接生效）——
        /// 修改配置后的保存生效必须先经编排（UpdateDevice 内置缓存同步），手动调用 = 纯重置不落盘（调试页「重建」键语义）。
        /// 注意：构建/重建后旧 IDevice 引用失效，持有方必须重新 GetDevice（将返回 null），或订阅 DeviceChanged 自行重取。
        /// </summary>
        public IDevice BuildDevice(string deviceType, string name, object config)
        {
            if (string.IsNullOrEmpty(deviceType) || string.IsNullOrEmpty(name))
                throw new ArgumentException("设备类型与名称不能为空");

            var factory = FindFactory(deviceType);
            if (factory == null)
                throw new NotSupportedException($"未找到设备工厂: {deviceType}");

            IDevice device;
            bool replaced;
            lock (_deviceLock)
            {
                // 1. 先用新配置创建（调用方须先经编排使驱动缓存为最新；CreateDevice 经 LoadJsonConfigForPlc 短路取最新）
                //    失败直接抛 → 旧实例未被释放、实例表未被改动
                device = factory.CreateDevice(name, config);

                // 2. 键切换与资源释放解耦：摘旧 → 立刻占位 → 留到 Step 3 释放
                //    同名 upsert 是同一个字典槽位（容器装不下两个同名设备），必然是「摘旧→占位」两步；
                //    关键是 Dispose 不得夹在两步之间——否则键空窗至 IoWorker 退出（StopIoWorker 上限 2s），
                //    期间 GetDevice(name) 返回 null（心跳探活等消费者已容错，但这是不该存在的中间态）
                var dict = _devices.GetOrAdd(deviceType, _ => new ConcurrentDictionary<string, IDevice>());
                dict.TryRemove(name, out var old); // 摘旧：拿到引用，键即刻腾出
                dict[name] = device; // 占位：空窗缩至两次字典操作之间
                replaced = old != null;

                // 3. 释放旧实例（旧 IPlc 随 IDevice.Dispose 链完整释放：Disconnect → Close → PlcRuntime.Dispose）
                //    必须留在锁内同步执行：PLC 侧常限制并发连接数（小型机可能只允许 1~2 个客户端），
                //    若挪出锁或异步释放，会出现「新实例已连接、旧连接仍挂着」→ PLC 拒连
                try { old?.Dispose(); } catch { }
            }

            NotifyDeviceChanged(deviceType, name, replaced ? DeviceChangeKind.Replaced : DeviceChangeKind.Added);
            return device;
        }

        /// <summary>
        /// 移除指定设备：锁内从管理器移除并完整释放实例（释放链与 BuildDevice 一致：Dispose → Disconnect → Close → 底层驱动 Dispose）
        /// 仅移除运行实例，不触碰任何配置文件（配置节删除走 IDeviceConfigManager.RemoveConfig；业务删除走 IDeviceOrchestration.DeleteDevice）
        /// </summary>
        public bool RemoveDevice(string deviceType, string name)
        {
            if (string.IsNullOrEmpty(deviceType) || string.IsNullOrEmpty(name))
                return false;

            bool removed = false;
            lock (_deviceLock)
            {
                if (_devices.TryGetValue(deviceType, out var typeDict) &&
                    typeDict.TryRemove(name, out var old))
                {
                    try { old.Dispose(); } catch { }
                    removed = true;
                }
            }

            if (removed)
                NotifyDeviceChanged(deviceType, name, DeviceChangeKind.Removed);
            return removed;
        }

        // ---------- 连接管理 ----------
        public async Task<bool> ConnectAllAsync()
        {
            bool allSuccess = true;
            var allDevs = AllDevices;
            foreach (var device in allDevs)
            {
                try
                {
                    await Task.Run(() =>
                    {
                        if (!device.IsConnected)
                            return device.Connect();
                        return true;
                    });
                }
                catch
                {
                    allSuccess = false;
                }
            }
            return allSuccess;
        }

        public async Task DisconnectAllAsync()
        {
            foreach (var device in AllDevices)
            {
                try { await Task.Run(() => device.Disconnect()); }
                catch { }
            }
        }

        #endregion

        #region 设备变更通知（实例新增 / 替换 / 移除）

        /// <inheritdoc />
        public event EventHandler<DeviceChangedEventArgs> DeviceChanged;

        /// <summary>待发变更队列（持有设备锁时入队，锁外统一派发；FIFO，改名路径的两条通知保持顺序）</summary>
        private readonly ConcurrentQueue<DeviceChangedEventArgs> _pendingChanges
            = new ConcurrentQueue<DeviceChangedEventArgs>();

        /// <summary>
        /// 记一次变更：持有设备锁时入队（由最外层统一派发），不持锁时立刻派发。
        /// 🔴 持锁时绝不能直接派发——订阅方会跑任意代码（可能同步等 UI 线程），会把整个设备管理器卡住。
        /// </summary>
        private void NotifyDeviceChanged(string deviceType, string name, DeviceChangeKind kind)
        {
            _pendingChanges.Enqueue(new DeviceChangedEventArgs(deviceType, name, kind));
            if (!Monitor.IsEntered(_deviceLock))
                FlushDeviceChanges();
        }

        /// <summary>
        /// 派发并清空队列：逐个订阅方 try/catch（一个处理器出错不影响设备操作本身，异常记入 IDeviceLogSink）。
        /// 队列是 FIFO——改名路径的「Added(新名) → Removed(旧名)」保持记录顺序。
        /// </summary>
        private void FlushDeviceChanges()
        {
            var handler = DeviceChanged;
            while (_pendingChanges.TryDequeue(out var args))
            {
                if (handler == null) continue;
                foreach (EventHandler<DeviceChangedEventArgs> subscriber in handler.GetInvocationList())
                {
                    try { subscriber(this, args); }
                    catch (Exception ex)
                    {
                        _logSink.Error(ex, $"设备变更通知处理器异常: [{args.DeviceType}] {args.Name} {args.Kind}");
                    }
                }
            }
        }

        #endregion

        #region 设备配置纯域（IDeviceConfigManager）：Devices.json 镜像节 查/增/改/删 + 落盘，零实例副作用

        // 本域方法操作磁盘配置镜像（_configStore.Model，契约侧是 object），三节访问收敛于 IDeviceConfigSection 注入集合
        // （新增设备类型 = 实现层加模型/节/注册一行，本域零改动）。
        // 与实例/驱动缓存完全解耦——"配置→实例生效"的联动在编排域（下方 region）完成。

        /// <inheritdoc />
        public IReadOnlyList<T> GetDeviceConfigs<T>() where T : class
        {
            EnsureStoreLoaded();
            var section = RequireSectionByConfigType(typeof(T));
            return section.GetList(_configStore.Model).Cast<T>().ToList().AsReadOnly();
        }

        /// <inheritdoc />
        public void AddConfig(string deviceType, object config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_deviceLock)
            {
                EnsureStoreLoaded();
                var section = RequireSection(deviceType);
                string name = section.GetName(config)?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("设备名称不能为空");

                // 配置节内同名查重（本域自包含；跨域查重 = 编排域前置）
                if (section.Contains(_configStore.Model, name))
                    throw new ArgumentException($"配置文件中已存在设备 [{name}]，不能重复");

                section.GetList(_configStore.Model).Add(config);

                // 落盘失败抛 → 调用方中止（内存镜像已加节，重试保存即收敛）
                _configStore.Save();
            }
        }

        /// <inheritdoc />
        public void UpdateConfig(string deviceType, string oldName, object config)
        {
            if (config == null)
                throw new ArgumentNullException(nameof(config));

            lock (_deviceLock)
            {
                EnsureStoreLoaded();
                var section = RequireSection(deviceType);
                string name = section.GetName(config)?.Trim();
                if (string.IsNullOrWhiteSpace(name))
                    throw new ArgumentException("设备名称不能为空");
                string oldTrim = oldName?.Trim();
                bool renaming = !string.IsNullOrEmpty(oldTrim)
                    && !string.Equals(oldTrim, name, StringComparison.Ordinal);

                // 改名先删旧节项（保列表顺序稳定）；同名编辑 = 原位替换。旧名不在配置节（异常态）删除即 false，无害继续 upsert
                if (renaming)
                    section.Remove(_configStore.Model, oldTrim);
                section.Upsert(_configStore.Model, config);

                _configStore.Save();
            }
        }

        /// <inheritdoc />
        public bool RemoveConfig(string deviceType, string name)
        {
            if (string.IsNullOrWhiteSpace(deviceType) || string.IsNullOrWhiteSpace(name))
                return false;

            lock (_deviceLock)
            {
                EnsureStoreLoaded();
                var section = RequireSection(deviceType);

                if (section.Remove(_configStore.Model, name))
                {
                    // 落盘失败抛：内存模型已删、磁盘未删（下次启动设备复活）——UI 弹错提示检查磁盘后重试
                    _configStore.Save();
                    return true;
                }
                return false;
            }
        }

        #endregion

        #region 设备编排域（IDeviceOrchestration）：跨两域原子业务用例（UI 设备管理与启动装配默认入口）

        // ⚠️ 编排骨架 = 校验/查重/驱动缓存同步 + 调两纯域段，对全部已注册类型通用：
        // - 驱动缓存同步为设备类型特有段（PLC=缓存型需先同步缓存；Scale/Scanner=直读型无此步），
        //   收敛于 IDeviceConfigSyncHook 端口（实现层按设备类型各自实现，本域只问 CanHandle）。
        // - 无实例工厂的类型（Scanner）仅配置镜像，运行时新增/更新被 RequireFactory 挡下（DeleteDevice 仍可用）。
        // 顺序铁律（UpdateDevice 保存生效）：先建新 → 再拆旧（仅改名）→ 落盘殿后；三步同在单一 _deviceLock 内。
        // 失败语义（编排内逐步中止）：校验失败 → 零副作用；实例创建失败 → 旧实例/旧缓存/磁盘全未动
        // （BuildDevice 先建新后换旧，改名摘除后置、落盘殿后），异常消息直接弹 UI，改对重存即可；
        // 落盘失败 → 缓存/实例已是新态、磁盘未持久化（弹错提示重试保存，内存模型为最终态，重试即收敛；
        // 改名场景若放弃，下次启动回退磁盘旧态）。

        /// <inheritdoc />
        public void LoadFromConfigFile(string configFilePath)
        {
            if (!File.Exists(configFilePath))
                throw new FileNotFoundException($"设备配置文件未找到: {configFilePath}");

            var loader = _configLoaders.FirstOrDefault(l => l.CanLoad(configFilePath));
            if (loader == null)
                throw new NotSupportedException($"没有找到能处理配置文件的加载器: {configFilePath}");

            // 配置侧文件镜像载入（Devices.json 全量模型：后续配置管理编排基于本模型增删改 + 落盘）
            _configStore.Load(configFilePath);

            var requests = loader.Load(configFilePath);

            foreach (var req in requests)
            {
                if (string.IsNullOrEmpty(req.DeviceType) || string.IsNullOrEmpty(req.DeviceName))
                    continue;

                // 无实例工厂的设备类型（如 Scanner）仅镜像不建实例（原装配行为保留：跳过）
                if (FindFactory(req.DeviceType) == null)
                    continue;

                // 唯一实例创建入口纪律：启动装配统一走 BuildDevice（重复加载同名 = 换新；启动仅一次，无实际影响）
                // 逐设备容错（与 ConnectAllAsync 的逐设备容忍风格对齐）：单个设备配置损坏不得拖垮整个启动。
                // 无兜底时——任一设备建实例抛异常（如西门子 PLCType 拼错）会一路上抛到 Bootstrapper，
                // 应用起不来、界面都进不去，只能手改 Devices.json。此处降级为「跳过 + 记日志」，
                // 其余设备照常装配；该设备在调试界面修正配置后保存即可重建（走 UpdateDevice → BuildDevice 事务序）。
                try
                {
                    BuildDevice(req.DeviceType, req.DeviceName, req.Config);
                }
                catch (Exception ex)
                {
                    _logSink.Error(ex, $"启动装配设备失败，已跳过: [{req.DeviceType}] {req.DeviceName}（请在调试界面修正配置后保存重建）");
                }
            }
        }

        /// <inheritdoc />
        public IDevice AddDevice(string deviceType, object config)
        {
            // 编排预检（锁外，失败零副作用）：类型校验+取名称（注册表统一）、工厂存在性、配置校验
            var section = RequireSection(deviceType);
            string name = section.GetName(config)?.Trim();
            var factory = RequireFactory(deviceType);
            ThrowIfConfigInvalid(factory, config);

            IDevice device;
            try
            {
                lock (_deviceLock)
                {
                    EnsureStoreLoaded();
                    EnsureNameFree(name, null, section); // 新增：全名域查重（实例表全类型 + 配置节本类型）

                    // 驱动缓存同步（设备类型特有：PLC 缓存型须先登记缓存，CreateDevice 短路即取新配置）
                    SyncRuntimeConfig(deviceType, config);

                    // 配置纯域段：配置节加项 + 落盘（.bak）
                    AddConfig(deviceType, config);

                    // 实例纯域段：构建实例放回（此刻驱动缓存已含新配置/直读型即取入参）
                    // 变更通知由 BuildDevice 记入队列，本方法在锁外统一派发
                    device = BuildDevice(deviceType, name, config);
                }
            }
            finally
            {
                FlushDeviceChanges();
            }
            return device;
        }

        /// <inheritdoc />
        public IDevice UpdateDevice(string deviceType, string oldName, object config)
        {
            // 编排预检（锁外）：必填 / 格式 / 协议白名单的 UX 前置提示；正确性不依赖它（见 BuildDevice 事务序）
            var section = RequireSection(deviceType);
            string newName = section.GetName(config)?.Trim();
            if (string.IsNullOrWhiteSpace(newName))
                throw new ArgumentException("设备名称不能为空");
            var factory = RequireFactory(deviceType);
            ThrowIfConfigInvalid(factory, config);

            string oldTrim = oldName?.Trim();
            bool renaming = !string.IsNullOrEmpty(oldTrim)
                && !string.Equals(oldTrim, newName, StringComparison.Ordinal);

            IDevice device;
            try
            {
                lock (_deviceLock)
                {
                    EnsureStoreLoaded();
                    EnsureNameFree(newName, oldTrim, section); // 编辑/改名：剔除自身后查重

                    // 驱动缓存同步（PLC：CreateDevice 短路数据源，须先于实例构建；内含地址表顺带重载：PlcTagsPath 变更即换内存表，缺表仅 Warn 留旧表/空表——§6.3 警告放行）
                    // 纯内存写：即便后续失败，旧实例仍在、磁盘未动；缓存为新配置与用户意图一致，无需回滚
                    SyncRuntimeConfig(deviceType, config);

                    // 实例纯域段：先建新（事务序——Create 失败则旧实例完好、旧缓存完好、磁盘未动，异常上抛由 UI 弹窗显示原因）
                    // ⚠️ 改名时 newName 与 oldTrim 是两个不同字典键，BuildDevice 只处理 newName → 旧键由下方收尾摘除
                    device = BuildDevice(deviceType, newName, config);

                    // 改名收尾：新实例已就位，才摘旧名实例 + 清旧驱动缓存
                    // （设备类型特有：PLC 清主配置+地址表组。旧地址表文件不删——现场资产，改名后需手工同步 PlcTagsPath/LogPath 或复制文件）
                    // ⚠️ 位置铁律=「先建新、再拆旧」：若前置到 BuildDevice 之前，改名 + 新配置非法时旧实例已死、旧缓存已清，
                    //    而磁盘与 UI 仍是旧配置 —— 运行时设备凭空消失（改名恰是最容易顺手改配置的操作）。
                    //    同名保存不进此分支：新旧名是同一个字典键，摘旧键 + 释放旧实例（含 TCP 断开）由 BuildDevice 内部的键切换承担
                    //    （见其 Step 2/3，且发生在 CreateDevice 成功之后）—— 此处不可对同名再调一次 RemoveDevice，那时摘掉的是刚建好的新实例；
                    //    本分支只处理跨键的旧名（BuildDevice 只认 newName，够不着旧键），故必须显式摘。
                    if (renaming)
                    {
                        RemoveDevice(deviceType, oldTrim); // 未命中（实例表无此名）返回 false 无害
                        ClearRuntimeConfig(deviceType, oldTrim);
                    }

                    // 配置纯域段：配置节 upsert + 落盘（置于实例创建成功之后，保证非法配置不落盘）
                    UpdateConfig(deviceType, oldTrim, config);
                }
            }
            finally
            {
                FlushDeviceChanges();   // 建新 + 改名摘旧，两条变更按序派发
            }
            return device;
        }

        /// <inheritdoc />
        public bool DeleteDevice(string deviceType, string name)
        {
            if (string.IsNullOrWhiteSpace(deviceType) || string.IsNullOrWhiteSpace(name))
                return false;

            bool any = false;
            try
            {
                lock (_deviceLock)
                {
                    EnsureStoreLoaded();

                    any |= RemoveDevice(deviceType, name); // 实例纯域：实例释放；无实例类型（Scanner）返回 false 忽略
                    any |= ClearRuntimeConfig(deviceType, name); // 设备类型特有：清驱动缓存（PLC 清主配置+地址表组；直读型 false）
                    any |= RemoveConfig(deviceType, name); // 配置纯域：删节落盘
                }
            }
            finally
            {
                FlushDeviceChanges();   // RemoveDevice 记入队列的变更，在锁外派发
            }
            return any;
        }

        // ---------- 编排私有辅助 ----------

        /// <summary>驱动缓存同步：按注册顺序问钩子，命中即同步（PLC=缓存型须先登记驱动缓存；Scale/Scanner=直读型无钩子）。
        /// 设备类型知识已移出本类——新增缓存型设备 = 新增一个 IDeviceConfigSyncHook 实现，本类零改动</summary>
        private void SyncRuntimeConfig(string deviceType, object config)
        {
            foreach (var hook in _configSyncHooks)
            {
                if (hook.CanHandle(deviceType))
                    hook.Sync(deviceType, config);
            }
        }

        /// <summary>驱动缓存清理：按注册顺序问钩子，命中即清。返回是否命中任何清理动作
        /// （无钩子命中 = false，与原直读型分支语义一致）</summary>
        private bool ClearRuntimeConfig(string deviceType, string name)
        {
            bool any = false;
            foreach (var hook in _configSyncHooks)
            {
                if (hook.CanHandle(deviceType))
                    any |= hook.Clear(deviceType, name);
            }
            return any;
        }

        private IDeviceFactory FindFactory(string deviceType)
        {
            return _factories.FirstOrDefault(f => f.DeviceType.Equals(deviceType, StringComparison.OrdinalIgnoreCase));
        }

        private IDeviceFactory RequireFactory(string deviceType)
        {
            var factory = FindFactory(deviceType);
            if (factory == null)
                throw new NotSupportedException($"设备类型 [{deviceType}] 没有可用的设备工厂，运行时新增/更新未启用");
            return factory;
        }

        /// <summary>按设备类型标识取配置节（未注册抛 NotSupported——统一终结"加类型漏分支=静默"隐患）</summary>
        private IDeviceConfigSection RequireSection(string deviceType)
        {
            if (string.IsNullOrWhiteSpace(deviceType))
                throw new ArgumentException("设备类型不能为空", nameof(deviceType));
            var section = _configSections.FirstOrDefault(
                s => s.DeviceType.Equals(deviceType, StringComparison.OrdinalIgnoreCase));
            if (section == null)
                throw new NotSupportedException($"不支持的设备类型: {deviceType}（已注册: {string.Join("/", _configSections.Select(s => s.DeviceType))}）");
            return section;
        }

        /// <summary>按配置 CLR 类型取配置节（GetDeviceConfigs&lt;T&gt; 用；未注册抛 NotSupported）</summary>
        private IDeviceConfigSection RequireSectionByConfigType(Type configType)
        {
            if (configType == null)
                throw new ArgumentNullException(nameof(configType));
            var section = _configSections.FirstOrDefault(s => s.ConfigType == configType);
            if (section == null)
                throw new NotSupportedException($"不支持的设备配置类型: {configType.Name}（已注册: {string.Join("/", _configSections.Select(s => s.ConfigType.Name))}）");
            return section;
        }

        /// <summary>预检：配置校验失败抛（失败=零副作用，盘/缓存/实例全未动）</summary>
        private static void ThrowIfConfigInvalid(IDeviceFactory factory, object config)
        {
            if (!factory.TryValidateConfig(config, out string error))
                throw new ArgumentException(error);
        }

        /// <summary>确保存储已加载（未 Load 过 = 启动链路异常/直接调用，防御抛错）</summary>
        private void EnsureStoreLoaded()
        {
            if (_configStore.FilePath == null)
                throw new InvalidOperationException("设备配置文件尚未加载（LoadFromConfigFile 未执行），无法进行配置管理操作");
        }

        /// <summary>名称查重：实例表（全类型）+ 配置节（本类型 section）；selfName=自身旧名（编辑不改名场景剔除自身）</summary>
        private void EnsureNameFree(string name, string selfName, IDeviceConfigSection section)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("设备名称不能为空");

            foreach (var typeDict in _devices.Values)
            {
                if (typeDict.ContainsKey(name) && !string.Equals(name, selfName, StringComparison.Ordinal))
                    throw new ArgumentException($"设备 [{name}] 已存在，不能重复");
            }

            if (section.Contains(_configStore.Model, name) && !string.Equals(name, selfName, StringComparison.Ordinal))
                throw new ArgumentException($"配置文件中已存在设备 [{name}]，不能重复");
        }

        #endregion
    }
}
