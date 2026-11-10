// 对应类库：本项目（组合根）——唯一定义启动序列与注册清单的文件
//
// Taoyl.IFA —— 产线设备上位机框架 · https://gitee.com/taoyanl/Taoyl.IFA （MIT）
using Autofac;
using System;
using System.IO;
using Taoyl.Application.Alarm.Models.Entities;
using Taoyl.Application.Audit.Models.Entities;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.AuthService.Models.Entities;
using Taoyl.Application.Product.Models.Entities;
using Taoyl.Application.Configuration.Interfaces;
using Taoyl.Application.Configuration.Models;
using Taoyl.Application.Business.ProcessHosting;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.DataBase;
using Taoyl.Application.LogDriver.Interfaces;
using Taoyl.Application.Share.Paths;
using Taoyl.IFALaunch.Mapping;
using Taoyl.IFALaunch.Modules;
using Taoyl.UI.Pages.Components;
using ProcessModule = Taoyl.IFALaunch.Modules.ProcessModule;

namespace Taoyl.IFALaunch
{
    /// <summary>
    /// 系统启动器 - 负责容器构建、服务注册、配置预加载、设备配置装载
    /// </summary>
    public static class Bootstrapper
    {
        public static IContainer Container { get; private set; }

        /// <summary>
        /// 初始化：按「类库 ↔ Module 文件一一对应」的规则分块注册（Modules/ 下每个文件头标注对应类库）。
        /// 注册顺序即启动叙事：基础（日志/配置/数据库/共享基座）→ 业务域（认证/审计）→ 设备 → 流程 → 界面。
        ///
        /// 扩展操作路径：
        /// 1. 新增类库：在类库实现接口 → Modules/ 新建 XxxModule（文件头标注对应类库）→ 下方 RegisterModule 加一行；
        /// 2. 新增页面：FormModule 加一行页面注册（Named 名 = 类型全名字符串）→
        ///    Menu.json 加节点（DllName = 命名空间、Class = 类名，拼接即 Named 名）。
        ///
        /// 注意：仓储与 SqlSugarHelper 均为延迟首 Resolve——必须先走 InitConfigs 预加载，
        /// 才能拿到内存缓存配置（ConfigService.Get 自愈 + 默认 Json 安全侧）。
        /// </summary>
        public static void Initialize()
        {
            // Mapster 全局装配（开关 → Scan → Compile），明细见 MappingBootstrapper。
            MappingBootstrapper.Configure();

            var builder = new ContainerBuilder();

            // 基础层
            builder.RegisterModule<LogModule>();
            builder.RegisterModule<ConfigModule>();
            builder.RegisterModule<DataBaseModule>();
            builder.RegisterModule<SharedModule>();

            // 业务域
            builder.RegisterModule<AuthModule>();
            builder.RegisterModule<AuditModule>();
            builder.RegisterModule<AlarmModule>();
            builder.RegisterModule<ProductModule>();

            // 设备与流程
            builder.RegisterModule<DeviceModule>();
            builder.RegisterModule<MesModule>();
            builder.RegisterModule<ProcessModule>();

            // 界面层
            builder.RegisterModule<FormModule>();

            Container = builder.Build();

            // 主线程同步上下文由 Program.Main 在本方法之前扶正（进程级 UI 基建，见 Program.Main 第 3 步）
            InitConfigs();
            InitDatabase();
            LoadDeviceConfig();
            InitComponents();
        }

        /// <summary>
        /// 预加载所有配置到内存——业务逻辑层在界面未打开时也能访问。
        /// </summary>
        private static void InitConfigs()
        {
            var cfg = Container.Resolve<IConfigService>();
            cfg.Get<DevConfig>();
            cfg.Get<BaseConfig>();
            var log = cfg.Get<LogConfig>();

            // 用日志策略初始化日志系统（NLog overlay 由 LogTargetConfigurator 执行）
            Container.Resolve<ILogService>().Initialize(log);

            // Resolve 即激活：构造即订阅——启动期日志（如内置管理员兜底复活的 Warn）才能入环、
            // 随主页面绑定上屏；删掉本行即静默退化（不报错），勿删
            Container.Resolve<ILogDisplay>();

            Container.Resolve<IRecipeService<MachineARecipeConfig>>().LoadAll();
        }

        /// <summary>
        /// 数据库CodeFirst初始化 — 在容器构建后执行，传入所有数据库实体类型
        /// </summary>
        private static void InitDatabase()
        {
            try
            {
                var dbHelper = Container.Resolve<SqlSugarHelper>();
                dbHelper.Db.CodeFirst.InitTables(
                    typeof(UserInfo),
                    typeof(Role),
                    typeof(Permission),
                    typeof(UserRole),
                    typeof(RolePermission),
                    typeof(AuditOperationEntity),
                    typeof(AuditFieldChangeEntity),
                    typeof(AlarmRecord),
                    typeof(ProductCellInfo));

                System.Diagnostics.Debug.WriteLine($"[CodeFirst] 已初始化实体表");

                var syncService = Container.Resolve<IPermissionSyncService>();
                bool synced = syncService.SyncOnStartup(out string message);
                if (!synced)
                    Container.Resolve<ILogService>().Warn($"[Auth] 启动同步异常：{message}");
                else if (!string.IsNullOrEmpty(message))
                    Container.Resolve<ILogService>().Warn($"[Auth] {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Bootstrapper] 数据库初始化失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 加载所有设备配置（启动装配：配置镜像载入 + 批量构建实例——跨域动作，走编排宽口）。
        /// 设备连接由 FormMain 启动后经 IDeviceManager.ConnectAllAsync 触发。
        /// </summary>
        public static void LoadDeviceConfig()
        {
            var orchestration = Container.Resolve<IDeviceOrchestration>();

            string jsonConfigPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ConfigPathName.DevicesFile);
            if (File.Exists(jsonConfigPath))
                orchestration.LoadFromConfigFile(jsonConfigPath);
        }

        /// <summary>
        /// 启动期组件解析——Resolve 即激活，每个都是唯一触发点，删掉即对应链路静默死亡（不报错）：
        /// - IProcessRegistry：构造尾完成全部流程注册（监控页数据源）；
        /// - AlarmPopupPresenter：构造时订阅报警总线（弹窗呈现链路）；
        /// - ProcessFaultRouter：构造时订阅 ProcessFaulted → AlarmService.Raise（报警落库）。
        /// 放在启动序列末尾：依赖配置/数据库就绪，且先于 FormMain 构造（天然满足时序）。
        /// </summary>
        private static void InitComponents()
        {
            Container.Resolve<ProcessFaultRouter>();

            Container.Resolve<IProcessRegistry>();
            Container.Resolve<AlarmPopupPresenter>();

            // MES 配置：Resolve 即加载 Mes.json。坏文件回退到 Backup/ 时把原因写进启动日志——
            // 否则现场只会看到「接口全部未定义」，不知道是配置坏了还是根本没配
            var mesConfig = Container.Resolve<Taoyl.Application.MesDriver.Interfaces.IMesConfigManager>();
            if (!string.IsNullOrEmpty(mesConfig.LastLoadError))
                Container.Resolve<ILogService>().Warn("[MES] 配置加载异常：" + mesConfig.LastLoadError);
        }
    }
}
