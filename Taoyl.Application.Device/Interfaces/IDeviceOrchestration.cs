namespace Taoyl.Application.Device.Interfaces
{
    /// <summary>
    /// 设备编排聚合接口（设备管理门面）：继承设备纯域（IDeviceManager）+ 配置纯域（IDeviceConfigManager），
    /// 并叠加"配置→实例联动"的业务用例（新增/保存生效/删除/启动装配）。
    /// 定位：**需要跨域联动（配置→实例）或需要增删改的"设备管理 UI"**（如 PLC 调试页）与启动装配方注入本宽口
    /// ——列表/连接/配置读写/增删改全走一个入口，无需自行判断"列表问设备口、配置问配置口"（认知负担收敛）；
    /// 同时窄口隔离不丢：DI 仍分别注册 IDeviceManager / IDeviceConfigManager 窄口。
    /// ⚠️ 约定（防退化）：**按"实际需要的能力"选口，不是默认宽口**——
    /// ① 需要跨域联动或增删改 ⇒ 拿宽口（本接口）；
    /// ② 只需单域只读能力 ⇒ 拿窄口（如主页"设备连接状态"面板只需设备清单，
    ///    注入 <see cref="IDeviceManager"/> 读 AllDevices，用宽口就是纯噪音，会把宽口喂成上帝接口）。
    /// </summary>
    public interface IDeviceOrchestration : IDeviceManager, IDeviceConfigManager
    {
        /// <summary>
        /// 新增设备（业务用例：按配置建档 + 建实例放回）：
        /// 校验 → 跨域查重（实例表全类型 + 配置节本类型）→ 驱动缓存同步（设备类型特有：PLC 走 UpdatePlcConfig）→
        /// 配置文件加节落盘(.bak) → 建实例放回，返回新实例。同名已存在视为失败。
        /// 无实例工厂的设备类型（如 Scanner）新增未启用（抛异常）。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="config">设备配置对象（各设备类型的具体配置模型）</param>
        /// <returns>新建的设备实例；校验/落盘失败抛异常（错误信息携带失败原因）</returns>
        IDevice AddDevice(string deviceType, object config);

        /// <summary>
        /// 更新设备并保存生效（业务用例：配置持久化 + 实例重建）——编辑/改名保存统一入口：
        /// 顺序铁律 =「先建新 → 再拆旧 → 落盘殿后」：校验 → 剔除自身查重 → 驱动缓存同步（设备类型特有：PLC 走 UpdatePlcConfig）→
        /// 构建新实例放回（事务序：Create 失败则旧实例/旧缓存/磁盘全未动）→ [仅改名] 摘旧名实例 + 清旧驱动缓存 →
        /// 配置文件 upsert 落盘(.bak)。任一步失败即中止抛异常（校验失败 / 新建失败时盘/缓存/实例零改动）。
        /// 改名时旧名与新名是两个不同字典键：BuildDevice 只处理新名，旧键由本方法在**新实例就位后**显式摘除
        /// （不可前置——否则改名 + 新配置非法时旧实例已死而磁盘/UI 仍是旧配置，运行时设备凭空消失）；
        /// 同名保存的键切换在 BuildDevice 内部完成（同一字典键，容器装不下两个同名设备）。
        /// 保存后旧 IDevice 引用失效，持有方必须重新 GetDevice。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="oldName">原设备名（新增态改名传 null/空串——调用方应转 AddDevice）</param>
        /// <param name="config">编辑后的完整配置对象（含新 Name）</param>
        /// <returns>更新后的设备实例；失败抛异常</returns>
        IDevice UpdateDevice(string deviceType, string oldName, object config);

        /// <summary>
        /// 删除设备（业务用例：释放实例 + 清驱动缓存 + 配置文件删节落盘(.bak)）。
        /// 不删除设备关联的数据文件（如地址表 xlsx，属现场资产）。
        /// 删除后旧 IDevice 引用失效，持有方必须重新 GetDevice（将返回 null）。
        /// </summary>
        /// <param name="deviceType">设备类型标识（由实现层定义，如 "PLC"）</param>
        /// <param name="name">设备名称</param>
        /// <returns>是否成功（false=设备不存在或参数无效；落盘失败抛异常）</returns>
        bool DeleteDevice(string deviceType, string name);

        /// <summary>
        /// 启动装配用例：从配置文件（Devices.json 等，可多次调用支持多种格式）载入配置镜像 + 按清单构建全部设备实例。
        /// 跨域动作（配置镜像载入 + 实例批量构建），故归属编排口而非实例纯域（IDeviceManager 保持"零文件零镜像"语义）。
        /// 无实例工厂的设备类型（如 Scanner）仅镜像不建实例（跳过）；重复加载同名设备 = 换新构建（BuildDevice 语义）。
        /// </summary>
        /// <param name="configFilePath">配置文件路径</param>
        void LoadFromConfigFile(string configFilePath);
    }
}
