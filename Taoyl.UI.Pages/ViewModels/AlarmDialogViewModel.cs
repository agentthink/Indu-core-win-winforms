using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using Taoyl.Application.Alarm.Interfaces;
using Taoyl.Application.Alarm.Models;
using Taoyl.Application.Alarm.Models.Dtos;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.UI.FormMain.Services;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 弹窗通道展示策略（通道差异的唯一声明处）：配色标题 + 自关规则 + 置顶。
    /// 通道全量差异收敛在 VM 策略字典一行注册上，新增报警类型 = 字典加一行，窗体/呈现器零改动。
    /// </summary>
    public class AlarmChannelPolicy
    {
        /// <summary>标题文案（窗体 Text）</summary>
        public string Title { get; }

        /// <summary>底色（标题栏与窗体底色共用）</summary>
        public Color BackColor { get; }

        /// <summary>空闲自关毫秒数（最后一条追加后无新报警即收窗）；null = 常驻待确认</summary>
        public int? IdleCloseMilliseconds { get; }

        /// <summary>是否置顶（报警窗被主窗盖住即失去存在意义，三通道默认置顶）</summary>
        public bool TopMost { get; }

        /// <summary>全量声明通道展示策略。</summary>
        public AlarmChannelPolicy(string title, Color backColor, int? idleCloseMilliseconds, bool topMost)
        {
            Title = title;
            BackColor = backColor;
            IdleCloseMilliseconds = idleCloseMilliseconds;
            TopMost = topMost;
        }
    }

    /// <summary>弹窗通道状态（策略字典值：展示策略 + 数据源 + 显示标记）。</summary>
    public class AlarmChannelState
    {
        /// <summary>通道展示策略（FormAlarmDialog.Bind 查表应用）</summary>
        public AlarmChannelPolicy Policy { get; }

        /// <summary>该通道数据源（DGV 绑定，增删自动刷新）</summary>
        public BindingList<AlarmRecordDto> Records { get; } = new BindingList<AlarmRecordDto>();

        /// <summary>该通道是否有窗在显示（仅 UI 线程读写）</summary>
        public bool IsShowing { get; set; }

        /// <summary>通道状态 = 策略 + 数据源 + 显示标记，一次注册全量声明。</summary>
        public AlarmChannelState(AlarmChannelPolicy policy)
        {
            Policy = policy ?? throw new ArgumentNullException(nameof(policy));
        }
    }

    /// <summary>
    /// 报警弹窗 ViewModel（单例，按 AlarmType 分通道的策略字典——视图瞬态、状态常驻）：
    /// - 通道策略字典：键 = AlarmType，值 = 展示策略 + 数据源 + IsShowing（新增通道类型 = 字典加一行）；
    ///   AlarmEnumText.Of 文案转换只发生在落库/展示边界，不作内部路由键；
    /// - IsShowing 跨瞬态窗体存活，防重入的追加再弹嵌套窗；
    /// - ConfirmCommand 带通道参数：倒序逐条 Ack，成功即移除、失败留存可重试，全清后发本通道收窗事件
    ///   （三通道共用：提示/警告被人工确认即 Ack 留痕，异常同链）；
    /// - 提示通道空闲自关由窗体调 ClearChannel（清空不 Ack：自关=未及查看，错过看查询页，不做代确认）。
    /// </summary>
    public partial class AlarmDialogViewModel
    {
        private readonly IAlarmService alarmService;
        private readonly IUserSession userSession;
        private readonly IMessageBoxService messageBoxService;

        /// <summary>通道策略字典：键=报警类型，值=策略+数据源+显示标记（三通道全注册；未注册键进入即抛=分诊漏洞哨兵）</summary>
        private readonly Dictionary<AlarmType, AlarmChannelState> channelStates = new Dictionary<AlarmType, AlarmChannelState>
        {
            [AlarmType.Notice]    = new AlarmChannelState(new AlarmChannelPolicy("提示报警", Color.Gray, 5000, topMost: true)),
            [AlarmType.Warning]   = new AlarmChannelState(new AlarmChannelPolicy("警告报警", Color.Orange, null, topMost: true)),
            [AlarmType.Exception] = new AlarmChannelState(new AlarmChannelPolicy("异常报警", Color.Red,    null, topMost: true)),
        };

        /// <summary>追加通知（自关通道窗体订阅：重置空闲自关计时）</summary>
        public event Action<AlarmType> RecordAppended;

        /// <summary>收窗请求（窗体订阅：本通道列表清空即收窗，参数=通道）</summary>
        public event Action<AlarmType> RequestClose;

        public AlarmDialogViewModel(IAlarmService alarmService, IUserSession userSession, IMessageBoxService messageBoxService)
        {
            this.alarmService = alarmService ?? throw new ArgumentNullException(nameof(alarmService));
            this.userSession = userSession;
            this.messageBoxService = messageBoxService;
        }

        /// <summary>通道是否有窗在显示。</summary>
        public bool IsShowing(AlarmType type) => State(type).IsShowing;

        /// <summary>标记通道显示状态（Presenter 显示前置 true，窗体 OnFormClosed 置 false）。</summary>
        public void MarkShowing(AlarmType type, bool value) => State(type).IsShowing = value;

        /// <summary>通道展示策略（FormAlarmDialog.Bind 按类型取用）。</summary>
        public AlarmChannelPolicy PolicyOf(AlarmType type) => State(type).Policy;

        /// <summary>通道数据源（FormAlarmDialog.Bind 按类型取绑）。</summary>
        public BindingList<AlarmRecordDto> RecordsOf(AlarmType type) => State(type).Records;

        /// <summary>追加报警进通道数据源（仅 UI 线程调用，BindingList 变更即刷 DGV）。</summary>
        public void AddRecord(AlarmType type, AlarmRecordDto record)
        {
            State(type).Records.Add(record);
            RecordAppended?.Invoke(type);
        }

        /// <summary>清空通道数据源（提示窗空闲自关用：清空不 Ack）。</summary>
        public void ClearChannel(AlarmType type) => State(type).Records.Clear();

        /// <summary>取通道状态（未注册类型即抛：新增枚举成员必须同步在此注册通道，出现在这里是分诊漏洞）。</summary>
        private AlarmChannelState State(AlarmType type)
        {
            AlarmChannelState state;
            if (!channelStates.TryGetValue(type, out state))
            {
                throw new InvalidOperationException(string.Format("报警通道 {0} 未注册弹窗数据源", type));
            }

            return state;
        }

        /// <summary>确认命令（通道参数化）：补处理人/时间 → 逐条 Ack，成功移除失败留存；全清后请求收窗。</summary>
        [RelayCommand]
        private void Confirm(AlarmType type)
        {
            var records = State(type).Records;
            
            for (int i = records.Count - 1; i >= 0; i--)
            {
                records[i].HandleUser = userSession?.CurrentUser?.Username;
                records[i].HandleTime = DateTime.Now;

                if (alarmService.Ack(records[i]))
                {
                    records.RemoveAt(i);
                }
            }

            if (records.Count == 0)
            {
                RequestClose?.Invoke(type);
            }
            else
            {
                // 落库失败：失败项留存列表可重试（避免「UI 已确认、DB 无记录」）
                messageBoxService?.ShowError("部分报警确认落库失败，请检查数据库后重试");
            }
        }
    }
}
