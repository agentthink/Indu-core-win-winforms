namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备配置文件存储（Devices.json 全量模型加载/落盘；实现层内部接口——抽象留在实现程序集，
    /// 编排方为 DeviceManager。若将来出现第二个编排方（如批量导入工具）需文件级操作，再把接口提升到契约层）。
    /// 节内增删改由编排方（DeviceManager）在锁内操作 Model 后调 Save 持久化——本存储只管文件镜像一致性。
    /// 🔴 Model 一律 object：编排方不认识具体根模型，其类型由泛型参数（DeviceConfigStore&lt;TModel&gt;）决定，
    ///    节访问统一经 IDeviceConfigSection 转换（K9：磁盘格式不动，只退化接口类型）。
    /// </summary>
    public interface IDeviceConfigStore
    {
        /// <summary>当前配置文件路径（未 Load 过为 null；写操作前必须已 Load）</summary>
        string FilePath { get; }

        /// <summary>当前内存根模型（Load 后持有；文件不存在 → 空模型）。具体类型由实现层决定，编排方只当不透明句柄用</summary>
        object Model { get; }

        /// <summary>
        /// 载入配置文件到内存根模型（已加载则重新加载覆盖——启动加载与重载共用）。
        /// 文件不存在 → 空模型 + 记录路径（后续 Save 即新建文件）。
        /// </summary>
        void Load(string filePath);

        /// <summary>
        /// 落盘：现有文件先 .bak 备份 → 全模型整文件重写（Indented）。
        /// ⚠️ 整文件重写 = 三节键归一为模型契约（.bak 兜底；loader 解析大小写不敏感，功能零影响）。
        /// </summary>
        void Save();
    }
}
