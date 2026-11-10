using Autofac;
using System;
using System.Threading;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.Share.Eventing;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages.Components
{
    /// <summary>
    /// 报警弹窗呈现器（自足单例）：订阅总线已落库报警，文案反查类型后按通道追加进 VM 单例数据源。
    /// - 三通道共用 AppendShow：追加进各自数据源（展示中即实时进列表），无窗在显示则起非模态置顶窗；
    ///   通道差异（配色/标题/自关/置顶）收敛在 VM 通道策略字典，本类零通道特判；
    ///   异常通道的安全兜底在流程层（状态机异常联动暂停），窗体仅承担展示与确认；
    /// - 未登记文案直接丢弃（理论不可达：文案必经 AlarmEnumText.Of 落库，此处仅防脏数据）。
    /// handler 重量纪律：OnAlarmPersisted 只做 Post 切 UI 线程，微秒级返回
    /// （总线分发跑在 AlarmService 消费线程上，不能拖落库节奏）。
    /// 释放规则：三通道均非模态 Show，窗体 Close → 自动 Dispose（禁 using，Show 立即返回）；
    /// 「通道有窗在显示」标记由 VM 单例持有（IsShowing/MarkShowing），跨瞬态窗体存活防重复起窗。
    /// 激活规则：Bootstrapper.InitComponents 主线程显式 Resolve（Program.Main 已先扶正主线程上下文），
    /// 捕获时机与 MainViewModel 同构——若在上下文扶正前捕获，会拿到 null 或基类
    /// SynchronizationContext（Post 静默 no-op / 甩线程池），弹窗静默失效。
    /// </summary>
    public class AlarmPopupPresenter
    {
        private readonly IEventBus eventBus;
        private readonly IComponentContext componentContext;
        private readonly AlarmDialogViewModel viewModel;

        /// <summary>订阅令牌（必须存字段：局部变量被 GC 回收会导致订阅失效）</summary>
        private readonly IDisposable subscription;

        /// <summary>UI 线程上下文（捕获主线程 WFSC；Program.Main 启动期已扶正，与 MainViewModel 同构）</summary>
        private readonly SynchronizationContext uiContext;

        public AlarmPopupPresenter(IEventBus eventBus, IComponentContext componentContext)
        {
            this.eventBus = eventBus ?? throw new ArgumentNullException(nameof(eventBus));
            this.componentContext = componentContext ?? throw new ArgumentNullException(nameof(componentContext));

            // 通道数据源宿主（VM 单例跨瞬态窗体存活）
            viewModel = componentContext.Resolve<AlarmDialogViewModel>();

            // 捕获主线程同步上下文（与 MainViewModel 同构写法）：本类由 Bootstrapper.InitComponents
            // 主线程 Resolve，此前 Program.Main 已把 Current 扶正为绑定主线程消息泵的 WFSC，Post 必达 UI 线程
            uiContext = SynchronizationContext.Current;

            subscription = eventBus.Subscribe<AlarmRecordDto>(OnAlarmPersisted);
        }

        private void OnAlarmPersisted(AlarmRecordDto record)
        {
            // 判空规则与 MainViewModel 同构：捕获失败时不抛、不在工作线程直执行，丢弃防反噬总线线程
            if (uiContext == null) return;

            // 回调已处于捕获线程（泵内重入场景）直接执行，免再绕一圈 Post
            if (uiContext == SynchronizationContext.Current)
            {
                ShowOnUi(record);
                return;
            }

            uiContext.Post(ShowOnUi, record);
        }

        /// <summary>UI 线程分诊：落库文案反查报警类型后按通道追加（record.Type 为落库文案，边界解析经 AlarmEnumText）。</summary>
        private void ShowOnUi(object state)
        {
            var record = state as AlarmRecordDto;
            if (record == null) return;

            AlarmType type;
            if (AlarmEnumText.TryParse(record.Type, out type))
            {
                AppendShow(type, record);
            }
        }

        /// <summary>
        /// 通道追加（三通道统一）：追加进数据源（展示中即实时进列表），无窗在显示则起非模态窗。
        /// 释放：Show 立即返回禁 using，窗体 Close → 自动 Dispose。
        /// </summary>
        private void AppendShow(AlarmType type, AlarmRecordDto record)
        {
            viewModel.AddRecord(type, record);
            if (viewModel.IsShowing(type)) return;

            viewModel.MarkShowing(type, true);
            var dialog = componentContext.Resolve<FormAlarmDialog>();
            dialog.Bind(type);
            dialog.Show();
        }
    }
}
