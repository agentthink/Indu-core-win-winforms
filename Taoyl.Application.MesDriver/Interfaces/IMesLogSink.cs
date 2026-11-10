using Taoyl.Application.MesDriver.Models;

namespace Taoyl.Application.MesDriver.Interfaces
{
    /// <summary>
    /// MES 调用日志端口。由组合根注入实现（NLog 适配器），基座不依赖任何日志库。
    /// 实现须线程安全——多个流程在各自专用线程上并发调用。
    /// </summary>
    public interface IMesLogSink
    {
        /// <summary>
        /// 记录一次调用结果。驱动不裁剪内容，记什么由实现决定
        /// （Request / Response 可能多行，CSV 落盘需由实现在适配层转义，
        /// 驱动侧绝不改写报文原文——Request 的价值就是能粘进 Postman 复现）。
        /// </summary>
        void Write(MesResult result);
    }
}
