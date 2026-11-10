namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// 权限同步服务接口
    /// 负责启动时从配置文件同步权限数据到持久化存储
    /// </summary>
    public interface IPermissionSyncService
    {
        /// <summary>
        /// 执行完整的启动同步流程。
        /// 返回 true = 同步完成，<paramref name="message"/> 为**需提示操作者的信息**（无则 null）；
        /// 返回 false = 同步过程异常，<paramref name="message"/> 为异常说明。
        /// 本模块不引日志类库——提示如何呈现（落日志 / 状态栏 / 弹窗）由组合根决定。
        /// </summary>
        bool SyncOnStartup(out string message);
    }

}
