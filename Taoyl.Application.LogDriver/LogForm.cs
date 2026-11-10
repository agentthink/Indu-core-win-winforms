using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.LogDriver.Services;
using Taoyl.Application.Share.Logging;
using Timer = System.Windows.Forms.Timer;

namespace Taoyl.Application.LogDriver
{
    /// <summary>
    /// 日志显示管理器（单例，实现 <see cref="ILogDisplay"/>；与进程同寿，不随页面销毁）
    /// 定稿规则（日志栏 = 实时状态指示灯）：写路径只把日志事件存入所属类别缓冲（业务线程，永不抛）；
    /// UI 心跳按帧对可见网格做「快照同步 / 行数同步 / 钉底 / 重绘」，行文本由虚拟模式按索引
    /// 从渲染帧快照就地拉取并格式化为单行文本（格式化次数 = 可见行数，与写入速率解耦）；
    /// 一类别一环形缓冲，绑定哪些类别由 UI Init 决定（未绑定 = 只落盘不显示）；
    /// 历史入口 = 落盘文件目录（不做回看/暂停/过滤，完整性由文件负责）。
    /// </summary>
    public class LogForm : ILogDisplay
    {
        #region 单例
        private static readonly Lazy<LogForm> _instance = new Lazy<LogForm>(() => new LogForm());

        /// <summary>
        /// 共享单例（Autofac 注册与页面属性注入使用此实例）
        /// </summary>
        public static LogForm Instance => _instance.Value;
        #endregion

        #region 字段与常量
        /// <summary>
        /// 视口行容量：一个数字三处含义 ——
        /// ① 每类别缓冲保留条数（环形缓冲容量）
        /// ② 网格可滚行数（虚拟模式 RowCount 必须等于缓冲条数）
        /// ③ 稳态时间跨度（峰值 1000 行/秒下约 0.5 秒交接窗口）
        /// 三者必须相等的原因：网格行数 &gt; 缓冲条数 ⇒ 滚出范围全是空白行
        /// （虚拟模式按索引就地拉取，越界即空行）。
        /// </summary>
        private const int ViewLineCapacity = 500;

        /// <summary>冻结自动恢复时限：视口离开底部后停止滚动超过此时长即恢复钉底跟随</summary>
        /// <remarks>状态灯职能不能依赖使用者记得拖回底部（结构保证优于纪律保证）</remarks>
        private const int FreezeTimeoutMs = 10_000;

        /// <summary>默认心跳间隔（毫秒）；与框架 Timer 构造函数默认值一致，此处显式声明为唯一真相源</summary>
        private const int DefaultIntervalMs = 100;

        // 类别环形缓冲表（与 LogCategory 一一对应，构造函数按枚举值全量建立）
        private readonly Dictionary<LogCategory, LogRingBuffer> _rings =
            new Dictionary<LogCategory, LogRingBuffer>();

        // 网格绑定表（网格 → 绑定关系与渲染状态）
        private readonly Dictionary<DataGridView, LogGridBinding> _bindings =
            new Dictionary<DataGridView, LogGridBinding>();

        // 类别颜色表（内置静态色保证白底可读且不撞色；SetCategoryColor 可覆盖）
        private readonly Dictionary<LogCategory, Color> _categoryColors = new Dictionary<LogCategory, Color>();

        // 日志服务（共享单例：与 LogHelp 转发、DI 实例同一来源，保证事件不分裂）
        private readonly ILogService _logService = NLogLogService.Instance;

        // 渲染心跳定时器：Tick 由 WM_TIMER 经「创建 HWND 的线程」的消息泵派发（框架反编译实证），
        // 故创建点收敛到 EnsureTimerAlive，而该方法仅由 Init 调用 ⇒ 创建必在 UI 线程；
        // 非 UI 线程首访单例不会再造成「定时器永不触发且不报错」。
        private Timer _renderTimer;

        // 心跳间隔（毫秒）= 唯一真相源：定时器创建时取初值，运行中改值也回写到此
        private int _intervalMs = DefaultIntervalMs;
        #endregion

        #region 构造与初始化
        /// <summary>
        /// 私有构造函数
        /// </summary>
        private LogForm()
        {
            InitializeRings();
            InitializeCategoryColors();

            // 构造即订阅：组合根在 InitConfigs 提前触发构造，早于主页面绑定的日志也能入环；
            // 与 Init 的重复调用幂等（SubscribeToLogs 先退订再订阅）
            SubscribeToLogs();
        }

        /// <summary>
        /// 按枚举值全量建立类别缓冲（遍历枚举而非逐项硬编码：新增类别自动跟随，不会漏建而静默丢日志）
        /// </summary>
        private void InitializeRings()
        {
            foreach (LogCategory category in Enum.GetValues(typeof(LogCategory)))
            {
                _rings[category] = new LogRingBuffer(ViewLineCapacity);
            }
        }

        /// <summary>
        /// 初始化类别颜色表（保证白底可读且各类别不撞色）
        /// </summary>
        private void InitializeCategoryColors()
        {
            _categoryColors[LogCategory.Debug] = Color.Gray;
            _categoryColors[LogCategory.Info] = Color.Black;
            _categoryColors[LogCategory.Warn] = Color.DarkOrange;
            _categoryColors[LogCategory.Error] = Color.Red;
            _categoryColors[LogCategory.MES] = Color.Teal;
        }

        /// <summary>
        /// 确保定时器存活并可启用（Dispose 后再次 Init 可恢复）
        /// 仅由 Init 调用（Init 必在 UI 线程）：控件 Timer 的 Tick 由 WM_TIMER 经「创建 HWND 的
        /// 线程」的消息泵派发，创建点收敛到此处 ⇒ 非 UI 线程首访不会再让日志栏永久静默且不报错。
        /// </summary>
        private void EnsureTimerAlive()
        {
            if (_renderTimer == null)
            {
                _renderTimer = new Timer();
                _renderTimer.Tick += OnRenderTick;
            }

            ApplyInterval();
            _renderTimer.Enabled = true;
        }

        /// <summary>
        /// 把当前间隔应用到定时器（Interval 的唯一赋值点）
        /// </summary>
        private void ApplyInterval()
        {
            if (_renderTimer != null)
            {
                _renderTimer.Interval = _intervalMs;
            }
        }

        /// <summary>
        /// 初始化日志显示配置（可重复调用，内部保证定时器存活与事件订阅幂等）
        /// 字典键 = 日志类别（一一对应，所见即所绑）；未绑定的类别只落盘不显示，
        /// 将来要显示时 Init 加一条绑定即可。
        /// </summary>
        /// <param name="gridMappings">类别 → 显示网格（值为 null 的项跳过）</param>
        public void Init(Dictionary<LogCategory, DataGridView> gridMappings = null)
        {
            EnsureTimerAlive();
            _bindings.Clear();

            if (gridMappings != null)
            {
                foreach (var kvp in gridMappings)
                {
                    if (kvp.Value == null)
                    {
                        continue; // null 不能作字典键（绑定表以网格为键）
                    }

                    LogRingBuffer ring;
                    if (!_rings.TryGetValue(kvp.Key, out ring))
                    {
                        continue; // 类别值非法：无对应缓冲，不建立绑定
                    }

                    ConfigureVirtualGrid(kvp.Value);
                    _bindings[kvp.Value] = new LogGridBinding(kvp.Value, kvp.Key, ring, ViewLineCapacity);
                }
            }

            SubscribeToLogs();
        }

        /// <summary>
        /// 配置虚拟模式网格并挂接取数/着色事件（先退订再订阅，保证重复 Init 幂等）
        /// 结构属性由 Designer 设置，此处兜底关键项，防设计器属性被删后虚拟模式静默失效
        /// </summary>
        private void ConfigureVirtualGrid(DataGridView grid)
        {
            grid.VirtualMode = true;
            grid.ReadOnly = true;
            grid.AllowUserToAddRows = false;
            grid.AllowUserToDeleteRows = false;
            grid.RowHeadersVisible = false;
            grid.ColumnHeadersVisible = false;

            // 虚拟模式要求至少一列；Designer/resx 未定义列，此处是列的唯一来源
            if (grid.ColumnCount == 0)
            {
                grid.ColumnCount = 1;
                grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            }

            grid.CellValueNeeded -= OnGridCellValueNeeded;
            grid.CellValueNeeded += OnGridCellValueNeeded;
            grid.CellFormatting -= OnGridCellFormatting;
            grid.CellFormatting += OnGridCellFormatting;
        }

        /// <summary>
        /// 订阅日志写入事件（先退订再订阅，保证幂等）
        /// </summary>
        private void SubscribeToLogs()
        {
            _logService.LogWritten -= OnLogWritten;
            _logService.LogWritten += OnLogWritten;
        }
        #endregion

        #region 写路径（业务线程：只允许「存事件」，禁任何 Invoke）
        /// <summary>
        /// 日志写入事件处理（业务线程调用，峰值 1000 次/秒）
        /// 只入库事件引用、不做格式化：格式化移到读路径（拉取时按可见行格式化）
        /// ⇒ 格式化次数与写入速率解耦，写路径仅一次字典查找 + 一次入缓冲。
        /// </summary>
        private void OnLogWritten(object sender, LogEventArgs e)
        {
            try
            {
                LogRingBuffer ring;
                if (_rings.TryGetValue(e.Category, out ring))
                {
                    ring.Append(e);
                }
            }
            catch
            {
                // 写路径永不抛：显示层故障不允许影响日志落盘主流程
            }
        }
        #endregion

        #region 读路径（UI 心跳：只渲染可见网格，不可见 Tab 只写不刷）
        /// <summary>
        /// 渲染心跳：逐绑定执行「可渲染 → 冻结判定 → 空闲短路 → 同步渲染」
        /// </summary>
        private void OnRenderTick(object sender, EventArgs e)
        {
            foreach (var binding in _bindings.Values)
            {
                if (!IsRenderable(binding.Grid))
                {
                    continue; // 不可见 Tab 只写不刷（数据持续入缓冲，渲染零成本）
                }

                if (ShouldSkipForFreeze(binding))
                {
                    continue; // 冻结：画面保持静止（数据照常入缓冲，永不积压）
                }

                if (!HasNewData(binding))
                {
                    continue; // 空闲：无新写入则本帧零动作
                }

                SyncToLatest(binding);
            }
        }

        /// <summary>
        /// 网格是否可渲染（句柄就绪、未释放、当前可见）
        /// </summary>
        private bool IsRenderable(DataGridView grid)
        {
            return !grid.IsDisposed && grid.IsHandleCreated && grid.Visible;
        }

        /// <summary>
        /// 冻结判定：视口不在底部即视为用户要看清当前画面，本帧保持静止；
        /// 计时锚点 = 最后一次视口位置变化（滚动途中持续重置），停止滚动超过
        /// <see cref="FreezeTimeoutMs"/> 自动恢复钉底跟随。
        /// </summary>
        /// <returns>true = 本帧跳过渲染</returns>
        private bool ShouldSkipForFreeze(LogGridBinding binding)
        {
            DataGridView grid = binding.Grid;
            int first = grid.FirstDisplayedScrollingRowIndex;

            // 【1】贴底判定用 RowCount - 1，不是 RowCount：
            //   钉底是「传越界值 + 让 DGV 夹取」（见 SyncToLatest），夹取结果与理论最大偏移可差 1 行；
            //   若改成严格相等，会出现「明明在底部却判不在底部」⇒ 冻结接管 ⇒ 每 10 秒一轮强制钉底
            //   ⇒ 画面周期性抖动。
            //   副作用（有意保留的保守容差）：往上滚 1 行不触发冻结，滚 2 行及以上才冻结。
            bool atBottom = first < 0
                || first + grid.DisplayedRowCount(false) >= grid.RowCount - 1;

            if (atBottom)
            {
                binding.FreezeStartTicks = null; // 贴底即结束冻结
                return false;
            }

            // 【2】只在「非底部」路径采样首行：计时锚点是「视口位置变化」；
            //   贴底时视口随新日志自然下移，那不是用户滚动，不能算作「还在滚动」。
            bool moved = binding.LastFreezeFirstRow != first;
            binding.LastFreezeFirstRow = first;

            // 【3】moved || !HasValue 保护的是「贴底清零后首帧必须重新计时」。
            //   4 帧推演（末帧 moved == false）：
            //     ① 用户滚到 50          ⇒ moved = true，计时起点 T1，采样 LastFreezeFirstRow = 50
            //     ② 用户拖回底部          ⇒ atBottom ⇒ FreezeStartTicks = null
            //                              （贴底路径在第【2】步采样之前就返回 ⇒ LastFreezeFirstRow 仍为 50）
            //     ③ 新日志持续推进、仍贴底 ⇒ 依旧走贴底分支 ⇒ LastFreezeFirstRow 仍为 50
            //     ④ 用户再滚回 50        ⇒ first 恰好等于 LastFreezeFirstRow(50) ⇒ moved == false
            //   此刻只有 !HasValue 能救，删掉第二个条件会有两种走坏方式：
            //     · 直接读 .Value：HasValue == false 时抛 InvalidOperationException（在心跳里抛 = 炸）；
            //     · 为绕开异常改写成 ?? 0 / GetValueOrDefault()：退化为哨兵语义，TickCount - 0 实际必然
            //       不小于 FreezeTimeoutMs ⇒ 当帧判「已超时」并直落渲染段 ⇒ 把用户刚滚开的画面直接拽回底部。
            if (moved || !binding.FreezeStartTicks.HasValue)
            {
                binding.FreezeStartTicks = Environment.TickCount; // 刚滚开或滚动中：重置计时，本帧静止
                return true;
            }

            // 【4】unchecked 依赖环绕语义正确（间隔 < 10 秒时差值恒正确）；
            //   若 csproj 打开 CheckForOverflowUnderflow，本行在回绕点会抛 —— 在心跳里抛 = 炸。
            //   注：.Value 的安全性由【3】的 HasValue 保护提供，两者是同一条命。
            if (unchecked(Environment.TickCount - binding.FreezeStartTicks.Value) < FreezeTimeoutMs)
            {
                return true; // 冻结中（未超时）
            }

            // 【5】超时恢复必须放行本帧去渲染（返回 false，严禁改成跳过）：
            //   本帧清零后，下一帧 !HasValue ⇒ 走【3】重新计时；若本帧也跳过，则「清零 + 重新计时」
            //   会在下一帧立刻发生 ⇒ 恢复永不发生（死循环根因，2026-09-23 实锤）。
            //   LastSeenAppends 置 -1 是绕过空闲短路（冻结期间往往无新写入，appends 未变会被拦下）。
            binding.FreezeStartTicks = null;
            binding.LastSeenAppends = -1;
            return false;
        }

        /// <summary>
        /// 本类别缓冲自上次渲染后有无新写入
        /// （不能用行数判断：写满后行数恒定，画面会永久冻结）
        /// </summary>
        private bool HasNewData(LogGridBinding binding)
        {
            long appends = binding.Ring.TotalAppends;
            if (binding.LastSeenAppends == appends)
            {
                return false;
            }

            binding.LastSeenAppends = appends;
            return true;
        }

        /// <summary>
        /// 同步网格到最新窗口：帧头整窗拷快照、行数跟随缓冲、钉底、只重画可见行
        /// </summary>
        private void SyncToLatest(LogGridBinding binding)
        {
            DataGridView grid = binding.Grid;
            int count = binding.Ring.Count; // 先读行数：环只增不减 ⇒ 后拷的快照条数 ≥ 行数，杜绝尾部空行帧

            // 渲染帧头部整窗拷快照：本帧所有单元格取数（含悬停全文触发的取数）统一从快照读，
            // 帧内一致；冻结期本方法整帧跳过 ⇒ 快照定格，重绘（点击/遮挡/Resize/滚动）不拉新数据
            // （读路径换源的完整语义见 LogGridBinding.Snapshot 注释）
            binding.SnapshotCount = binding.Ring.CopyTo(binding.Snapshot);

            if (grid.RowCount != count)
            {
                grid.RowCount = count; // 结构变化（含清零）由网格自行重绘
            }

            // 空缓冲时钉底会传 -1 越界，故仅有行时执行
            if (count > 0)
            {
                grid.FirstDisplayedScrollingRowIndex = count - 1; // 钉底，系统自动夹取
                grid.Invalidate(); // 只重画可见行
            }
        }
        #endregion

        #region 网格事件（虚拟模式拉取与着色）
        /// <summary>
        /// 虚拟模式取数：按行索引从渲染帧快照就地拉取（数据不搬进控件，也不直读环）
        /// </summary>
        private void OnGridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e)
        {
            e.Value = TryGetLineText(sender as DataGridView, e.RowIndex);
        }

        /// <summary>
        /// 按行索引读取行文本：从渲染帧快照取条目并格式化（取不到一律空串：
        /// RowCount 与 SnapshotCount 是两次独立读环，存在瞬态窗口差）
        /// </summary>
        private string TryGetLineText(DataGridView grid, int rowIndex)
        {
            LogGridBinding binding;
            if (grid == null || rowIndex < 0 || !_bindings.TryGetValue(grid, out binding))
            {
                return string.Empty;
            }

            LogEventArgs entry = rowIndex < binding.SnapshotCount ? binding.Snapshot[rowIndex] : null;
            return entry != null ? FormatLine(entry, binding.Category) : string.Empty;
        }

        /// <summary>
        /// 把日志事件格式化为单行显示文本：[HH:mm:ss.fff][类别] 消息
        /// 只允许读取「快照型字段」：Timestamp / Message / Category / Level；
        /// 禁止读非快照成员（活对象会被业务侧在写日志之后继续改写，读它会导致同一行两次
        /// 绘制内容不同，非确定性显示）。
        /// </summary>
        /// <param name="entry">日志事件（环形缓冲中持有的引用）</param>
        /// <param name="category">本行所属显示栏的类别：取自 binding.Category，不从 entry.Category 取
        /// —— 两者恒等（写入按 e.Category 分派、绑定同源），但读路径只需 (entry, category) 两个输入、
        /// 来源单一；binding 才是「这行属哪个显示栏」的权威。</param>
        private string FormatLine(LogEventArgs entry, LogCategory category)
        {
            // 类别用 enum.ToString()：命名值返回内部缓存实例（零字符串分配），
            // 且结构跟随枚举定义（静态标签表靠下标对齐，插枚举值即错位）
            return string.Concat("[", entry.Timestamp.ToString("HH:mm:ss.fff"), "][",
                category.ToString(), "] ", entry.Message);
        }

        /// <summary>
        /// 逐行着色：按网格所绑类别查色表（只设 ForeColor，不创建 GDI 对象）
        /// </summary>
        private void OnGridCellFormatting(object sender, DataGridViewCellFormattingEventArgs e)
        {
            var grid = sender as DataGridView;
            LogGridBinding binding;
            if (grid == null || e.RowIndex < 0 || !_bindings.TryGetValue(grid, out binding))
            {
                return;
            }

            Color color;
            if (_categoryColors.TryGetValue(binding.Category, out color))
            {
                e.CellStyle.ForeColor = color;
            }
        }
        #endregion

        #region 公共 API
        /// <summary>
        /// 设置类别颜色（覆盖内置色表）
        /// </summary>
        public void SetCategoryColor(LogCategory category, Color color)
        {
            _categoryColors[category] = color;
        }

        /// <summary>
        /// 设置 UI 刷新心跳间隔（毫秒，10~1000 内生效）
        /// 定时器已创建则立即生效（框架 Interval setter 内部会重启原生定时器）；
        /// 尚未创建时只记入 _intervalMs，由 Init 创建定时器时取用 ⇒ 值不会丢失。
        /// </summary>
        public void SetUpdateInterval(int milliseconds)
        {
            // 钳制是硬需求（不是防御）：Timer.Interval setter 在 value < 1 时抛 ArgumentOutOfRangeException
            if (milliseconds < 50)
            {
                milliseconds = 50;
            }

            if (milliseconds > 1000)
            {
                milliseconds = 1000;
            }

            _intervalMs = milliseconds;
            ApplyInterval();
        }

        /// <summary>
        /// 清空指定类别的日志显示（未绑定的类别只清缓冲）
        /// </summary>
        public void ClearCategory(LogCategory category)
        {
            LogRingBuffer ring;
            if (!_rings.TryGetValue(category, out ring))
            {
                return;
            }

            ring.Clear();

            foreach (var binding in _bindings.Values)
            {
                if (binding.Category == category && !binding.Grid.IsDisposed)
                {
                    binding.Grid.RowCount = 0;
                }
            }
        }

        /// <summary>
        /// 清空所有类别的日志显示
        /// </summary>
        public void ClearAll()
        {
            foreach (var ring in _rings.Values)
            {
                ring.Clear();
            }

            foreach (var binding in _bindings.Values)
            {
                if (!binding.Grid.IsDisposed)
                {
                    binding.Grid.RowCount = 0;
                }
            }
        }
        #endregion

        #region 清理资源（停止并复位，单例可再次 Init）
        /// <summary>
        /// 释放资源（仅停止并复位，不销毁定时器对象；单例可再次 Init 恢复）
        /// 🔴 单例与进程同寿：不随页面销毁而调用（全仓零调用方，明确约定 2026-10-07）——
        /// 页面关闭后心跳空转无实害（IsRenderable 挡住），页面重开 Init 幂等恢复；
        /// 本方法仅作运行中显式重置用。
        /// </summary>
        public void Dispose()
        {
            _renderTimer.Enabled = false;
            _logService.LogWritten -= OnLogWritten;

            _bindings.Clear();
            foreach (var ring in _rings.Values)
            {
                ring.Clear();
            }
        }
        #endregion
    }
}
