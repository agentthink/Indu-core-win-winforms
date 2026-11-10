using System.Windows.Forms;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver
{
    /// <summary>
    /// 日志网格绑定（一个显示网格的绑定关系 + 渲染状态）
    /// 生命周期与网格同源：Init 创建、Dispose 丢弃，渲染状态随绑定对象走。
    /// 仅在 UI 线程访问（Init = 页面 OnLoad；渲染状态 = 心跳 Tick；绑定关系 = 网格单元格事件）。
    /// </summary>
    internal sealed class LogGridBinding
    {
        /// <summary>显示网格（虚拟模式 DataGridView）</summary>
        public readonly DataGridView Grid;

        /// <summary>本网格显示的日志类别（写入分派与着色共用）</summary>
        public readonly LogCategory Category;

        /// <summary>本网格的数据源缓冲（实例在 LogForm 生命周期内不变）</summary>
        public readonly LogRingBuffer Ring;

        /// <summary>上次心跳见到的写入计数；初值 -1 保证首帧必渲染（初值 0 会被空闲短路吞掉）
        /// —— 保留哨兵：写入计数单调递增，-1 结构上不可能出现</summary>
        public long LastSeenAppends = -1;

        /// <summary>冻结开始时刻（Environment.TickCount 毫秒）；null = 未冻结
        /// —— 用语言原生的「无值」表达状态，替代哨兵 0：0 是 TickCount 的合法取值，
        /// 回绕到 0 时哨兵形态会把「冻结中」误判为「未冻结」</summary>
        public int? FreezeStartTicks;

        /// <summary>冻结期间上帧视口首行，用于检测「还在滚动」；-1 = 尚未采样
        /// —— 保留哨兵：行号 ≥ 0，且 -1 恰是 DGV「无行」语义，语义自洽</summary>
        public int LastFreezeFirstRow = -1;

        /// <summary>视口快照：最近一次渲染帧的窗口条目副本（单元格事件取数的唯一数据源）
        /// —— CellValueNeeded 等取数一律读这里、不直读环：跟尾态快照 = 本帧、冻结态快照 = 定格帧，
        /// 从语义上消除「重绘拉新数据」的内容跳变（点击 / 遮挡 / Resize 三触发器同治）。
        /// 快照由心跳在渲染帧头部整窗拷贝（Ring.CopyTo），生命周期随绑定对象
        /// （Init 重建 / Dispose 丢弃），无专门清理路径。</summary>
        public readonly LogEventArgs[] Snapshot;

        /// <summary>快照实际条数（= 最近一次 CopyTo 返回值）。与 RowCount 是两次独立读环，
        /// 瞬态可能相差 1~2 条，越界索引由取数方拉空串兜底（与直读环时代同款语义）</summary>
        public int SnapshotCount;

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="grid">显示网格</param>
        /// <param name="category">日志类别</param>
        /// <param name="ring">数据源缓冲</param>
        /// <param name="capacity">快照容量（= 环容量，整窗拷贝恒不截断）</param>
        public LogGridBinding(DataGridView grid, LogCategory category, LogRingBuffer ring, int capacity)
        {
            Grid = grid;
            Category = category;
            Ring = ring;
            Snapshot = new LogEventArgs[capacity];
        }
    }
}
