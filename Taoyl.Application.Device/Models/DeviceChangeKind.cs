namespace Taoyl.Application.Device.Models
{
    /// <summary>设备变更种类（变更通知的载荷之一）</summary>
    public enum DeviceChangeKind
    {
        /// <summary>新增：此前没有同名实例</summary>
        Added,

        /// <summary>替换：同名实例被重建，旧实例已释放</summary>
        Replaced,

        /// <summary>移除：实例已释放（配置是否保留，取决于调用方走的是 RemoveDevice 还是 DeleteDevice）</summary>
        Removed
    }
}
