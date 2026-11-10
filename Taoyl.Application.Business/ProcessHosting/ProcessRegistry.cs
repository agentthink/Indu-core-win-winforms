using Autofac;
using System;
using Taoyl.Application.Business.Processes;
using Taoyl.Application.LogDriver;
using Taoyl.Application.TaskProcess.Interfaces;

namespace Taoyl.Application.Business.ProcessHosting
{
    /// <summary>
    /// 流程注册器实现，负责创建和注册所有流程实例。
    ///
    /// 🔴 构造即注册：Init() 在构造末尾自调——流程"出生即就绪"，软件启动后监控页即可见全部流程（待命态）。
    /// 触发链：Bootstrapper.InitComponents() 显式 Resolve IProcessRegistry → 本构造执行 → 全部流程注册完成。
    /// ⚠️ Bootstrapper.InitComponents() 是唯一注册触发点：未 Resolve 本类（或删掉该调用）= 流程不注册，
    /// 表现为监控页空表、启动无流程，且不报错——改动启动序列前必读此注释。
    /// 全量初始化（Coordinator.InitializeAllAsync）走 ProcessesNeedRebuild 重建路径，同样调用本类 Init()，两路径共用。
    /// </summary>
    public class ProcessRegistry : IProcessRegistry
    {
        private readonly ILifetimeScope scope;
        private readonly IProcessCoordinator coordinator;

        public ProcessRegistry(ILifetimeScope scope, IProcessCoordinator coordinator)
        {
            this.scope = scope;
            this.coordinator = coordinator ?? throw new ArgumentNullException(nameof(coordinator));

            // 订阅全量重建请求：基座已断总线，改普通委托（注册器为单例长活对象，无须退订）
            this.coordinator.ProcessesNeedRebuild += OnProcessesNeedRebuild;

            // 构造即注册：新实例全为待命态，InitializeAsync 无真挂起点，同步完成无死锁风险
            Init();
        }

        /// <summary>
        /// 初始注册所有流程。
        /// 从容器 Resolve 实例（子类新增构造依赖时由 Autofac 自动组装，本类零改动），
        /// Register 时自动调用 InitializeAsync() 完成初始化。
        /// </summary>
        public void Init()
        {
            // 自检流程：五个都在各自构造里设了 BehaviorConfig.IsEnabled = false（默认禁用），
            // 不参与 StartAll 与系统状态聚合——现场「一键启动」不会顺手把测试数据灌进日志、报警库和产品库。
            // 测哪个就在流程监控页选中 → 启用 → 启动。该开关只改运行时值、不落盘，重启即恢复禁用。
            coordinator.Register(scope.Resolve<LogTestProcess>());
            coordinator.Register(scope.Resolve<AlarmTestProcess>());
            coordinator.Register(scope.Resolve<MesTestProcess>());
            coordinator.Register(scope.Resolve<DeviceTestProcess>());
            coordinator.Register(scope.Resolve<ProductTestProcess>());

            // 新流程在此添加...
        }


        /// <summary>
        /// 全量重建处理器。
        /// Coordinator 已清空所有流程，这里重新从容器 Resolve 并注册。
        /// </summary>
        private void OnProcessesNeedRebuild()
        {
            LogHelp.Info("收到全量初始化请求，重新从容器创建所有流程");
            Init();
        }
    }
}
