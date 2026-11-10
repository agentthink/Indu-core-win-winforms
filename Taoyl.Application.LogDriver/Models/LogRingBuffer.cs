using System;

namespace Taoyl.Application.LogDriver
{
    /// <summary>
    /// 日志环形缓冲（固定容量，写满后覆盖最老条目）
    /// 语义定位：「100 写线程 → 1 UI 读线程」的交接点，不是历史记录本——
    /// 覆盖式写入使队列结构上不可能积压（UI 心跳停摆多久都只丢最老条目，恢复瞬间跳最新窗口）；
    /// 历史完整性由 NLog 落盘文件负责，与本缓冲无关。
    /// 元素为日志事件（<see cref="LogEventArgs"/>）的引用：存引用、不拷贝、不格式化，
    /// 显示文本由读路径拉取时才格式化；类别由所属缓冲决定（一类别一缓冲，条目上不冗余存类别）。
    /// 读路径经 <see cref="CopyTo"/> 整窗拷贝构造渲染帧快照，单元格事件统一从快照取数
    /// （不直读环：消除冻结期重绘拉新数据的内容跳变，见 <see cref="LogGridBinding.Snapshot"/>）。
    /// 持引用意味着被引用对象（含 <see cref="LogEventArgs.Exception"/> 与 Entity）的最短寿命 = 本缓冲窗口，
    /// 故读侧只允许使用快照型字段（约束详见 LogForm 的格式化入口注释）。
    /// 若将来启用带实体的日志入口（泛型 MES 重载 / 带上下文重载），须先重评该持有策略：
    /// 低频场景下 500 条窗口可能跨越数小时，会长期钉住业务实体。
    /// 线程安全：单锁保护，写入 O(1)、整窗拷贝 O(n)（n = 容量），临界区均为微秒级以内
    /// （写侧 1000 次/秒 + 读侧 ~10 次/秒——每帧一次整窗拷贝，锁占用率 &lt;0.1%，无竞争风险）。
    /// </summary>
    internal sealed class LogRingBuffer
    {
        #region 私有字段
        private readonly LogEventArgs[] _slots;
        private readonly int _capacity;
        private readonly object _lock = new object();

        // 下一个写入槽位；缓冲写满后 _head 即最老条目位置
        private int _head;
        private int _count;

        // 累计写入条数（只增不减；满载后行数恒定，UI 心跳靠它判断本帧有无新数据）
        private long _totalAppends;
        #endregion

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capacity">缓冲容量（条数），必须大于 0</param>
        public LogRingBuffer(int capacity)
        {
            if (capacity <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), "环形缓冲容量必须大于 0");
            }

            _capacity = capacity;
            _slots = new LogEventArgs[capacity];
        }

        #region 公共属性
        /// <summary>
        /// 当前行数（写满后恒等于容量——稳态下 UI 行数不再变化，连滚动条重算都没有）
        /// </summary>
        public int Count
        {
            get
            {
                lock (_lock)
                {
                    return _count;
                }
            }
        }

        /// <summary>
        /// 累计写入条数（写满后仍持续递增）
        /// 与 <see cref="Count"/> 是两次独立加锁读取，不构成同一时刻快照：调用方只可分别
        /// 判断「有无新写入」与「要显示多少行」，不可据此做增量一致性推理。
        /// </summary>
        public long TotalAppends
        {
            get
            {
                lock (_lock)
                {
                    return _totalAppends;
                }
            }
        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 追加一条日志事件（写满则覆盖最老条目，O(1)）
        /// 只存引用：写路径成本最低（连字符串拼接都省），格式化延后到读路径按可见行执行。
        /// </summary>
        /// <param name="entry">日志事件（由日志服务统一构造，写入后不被改写）</param>
        public void Append(LogEventArgs entry)
        {
            lock (_lock)
            {
                _slots[_head] = entry;
                _head = (_head + 1) % _capacity;
                if (_count < _capacity)
                {
                    _count++;
                }
                _totalAppends++;
            }
        }

        /// <summary>
        /// 把当前窗口全量拷贝到目标数组（index 0 = 最老；写满后同一 index 始终指向最新窗口中的
        /// 对应条目——滑动窗口语义）
        /// 供读路径构造渲染帧快照：单次加锁两段复制，临界区内零格式化零字符串操作；
        /// 目标数组不足时只拷最老的 target.Length 条（正常调用方传容量 = 环容量，恒不截断）。
        /// </summary>
        /// <param name="target">目标数组（调用方持有，本方法只写入，不清空超出条数的槽位）</param>
        /// <returns>实际拷贝条数（= min(Count, target.Length)）</returns>
        public int CopyTo(LogEventArgs[] target)
        {
            if (target == null || target.Length == 0)
            {
                return 0;
            }

            lock (_lock)
            {
                int n = Math.Min(_count, target.Length);
                if (n <= 0)
                {
                    return 0;
                }

                // 【槽位换算】环是物理绕回的定长数组：_head 指向下一个写槽位，逻辑上第 0 条（最老）
                // 的物理槽位 = _head - _count——逆着写入方向退 _count 步；结果可能为负（最老条目在
                // 数组尾部、较新条目已绕回头部），+ 容量即绕回后的真实槽位。
                // 例（容量 5，_head = 2，写满）：
                //   物理槽位:  [0]   [1]   [2]   [3]   [4]
                //   逻辑序号:    3     4     0     1     2     （0 = 最老，4 = 最新）
                //   first = 2 - 5 = -3 ⇒ -3 + 5 = 2，即最老条目在物理槽 2。
                int first = _head - _count;
                if (first < 0)
                {
                    first += _capacity;
                }

                // 【两段复制】窗口从 first 起连续 n 条，物理上被数组末尾切成两段：
                //   第一段：[first, 数组末尾) —— 长度 = min(n, 容量 - first)；未满载且不绕回时
                //             窗口不达末尾，此段即全部 n 条
                //   第二段：[0, 绕回剩余)     —— 仅绕回时非空；第一段已取满 n 条时长度为 0 跳过
                // 两段拼接顺序 = 逻辑窗口顺序（最老 → 最新），与 index 语义一致。
                int tailLen = Math.Min(n, _capacity - first);
                Array.Copy(_slots, first, target, 0, tailLen);
                if (n > tailLen)
                {
                    Array.Copy(_slots, 0, target, tailLen, n - tailLen);
                }

                return n;
            }
        }

        /// <summary>
        /// 清空缓冲
        /// </summary>
        public void Clear()
        {
            lock (_lock)
            {
                _head = 0;
                _count = 0;
                _totalAppends = 0;
                Array.Clear(_slots, 0, _capacity); // 引用数组：同时解除对日志事件的持有
            }
        }
        #endregion
    }
}
