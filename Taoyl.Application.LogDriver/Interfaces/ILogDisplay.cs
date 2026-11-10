using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Taoyl.Application.Share.Logging;

namespace Taoyl.Application.LogDriver.Interfaces
{
    /// <summary>
    /// 日志 UI 显示契约（由 <see cref="LogForm"/> 单例实现）
    /// 第三轮重构定稿：日志栏 = 实时状态指示灯（非排障工具）——
    /// 写路径只入类库侧环形缓冲（永不堆积），UI 心跳按帧刷新，
    /// 显示容器 = 虚拟模式 DataGridView（VirtualMode + CellValueNeeded 按索引拉取，数据不搬进控件）；
    /// 不提供滚动回看/暂停/搜索/过滤，历史入口 = 落盘文件目录。
    /// </summary>
    public interface ILogDisplay
    {
        /// <summary>
        /// 初始化日志显示配置（可重复调用，内部保证定时器存活与事件订阅幂等）
        /// 字典键 = 日志类别（<see cref="LogCategory"/>，一一对应，所见即所绑）；
        /// 未绑定的类别只落盘不显示（显式决策）。
        /// 值类型即显示网格（虚拟化拉取模型）：编译期约束取代运行时类型转换。
        /// </summary>
        /// <param name="gridMappings">类别 → 显示网格（值为 null 的项跳过）</param>
        void Init(Dictionary<LogCategory, DataGridView> gridMappings = null);

        /// <summary>设置 UI 刷新心跳间隔（毫秒，10~1000 内生效；定稿默认 100ms）</summary>
        void SetUpdateInterval(int milliseconds);

        /// <summary>设置类别颜色（覆盖内置静态色表，白底可读性已内置）</summary>
        void SetCategoryColor(LogCategory category, Color color);

        /// <summary>清空指定类别的日志显示</summary>
        void ClearCategory(LogCategory category);

        /// <summary>清空所有类别的日志显示</summary>
        void ClearAll();
    }
}
