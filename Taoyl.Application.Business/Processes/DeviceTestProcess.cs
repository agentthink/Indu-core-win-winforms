using System;
using System.Linq;
using System.Threading;
using Taoyl.Application.Business.ProcessHosting;
using Taoyl.Application.Device.Models;
using Taoyl.Application.LogDriver;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.TaskProcess.Models;
using Taoyl.Application.TaskProcess.Services;

namespace Taoyl.Application.Business.Processes
{
    /// <summary>
    /// 设备通讯测试流程：按地址表的标签周期读一遍，验「取实例 → 读标签 → 拿结果」整条链路。
    ///
    ///   步序 10 · 单个 Bool（就绪.设备就绪1）
    ///   步序 20 · 单个 Short（触发.启动电芯1扫码）+ 整组读（重量）
    ///   步序 30 · Float[]（压力.压力1）+ Bool[]（状态位.报警位）
    ///   步序 40 · 回步序 10
    ///
    /// 🔴 组名 / 标签名来自地址表（PlcTagPolling_PLC_OmronCip_R.xlsx），不是 Devices.json。
    /// 🔴 ReadTag / ReadGroup 失败不抛异常，Content 是默认值（false / 0 / 空数组），
    ///    日志上看起来像读到了 0 —— 要判成败必须看 IsSuccess。
    /// 🔴 默认禁用：测的时候在流程监控页选中 → 启用 → 启动；重启恢复禁用。
    ///
    /// 🔴 **设备引用模型（本流程是全项目唯一的示范写法）**：
    ///    持有实例 + 订阅 DeviceChanged 失效 + OnDispose 退订。
    ///    不采用「每拍现取」是为了让「设备变了」当拍就知道，不必等下一次 CheckHealth；
    ///    不采用「每拍刷新缓存」是因为那让 CheckHealth 兼职刷新，语义混浊。
    /// </summary>
    internal class DeviceTestProcess : ProcessBase
    {
        /// <summary>被测设备名（Devices.json 的 name 字段）</summary>
        private const string DeviceName = "PLC_OmronCip_R";

        /// <summary>地址表里的组名 / 标签名，每个步序重新指定</summary>
        private string groupName = "就绪";
        private string tagName = "设备就绪1";

        private readonly IProcessContext context;

        /// <summary>
        /// 当前设备实例引用（null = 未取 / 已被变更通知置空，下一拍由 CheckHealth 重取）。
        /// 🔴 volatile：OnDeviceChanged 在**触发方线程**（UI 保存 = 工作线程）写 null，
        ///    looper 线程读——引用赋值本身原子，但不加 volatile 就没有可见性保证。
        /// 🔴 三种变更（Added/Replaced/Removed）对本流程等价：旧引用一律作废。
        /// </summary>
        private volatile IPlc plc;

        /// <summary>构造自注入：子类只收一个上下文，设备从上下文取（基座零依赖）。</summary>
        public DeviceTestProcess(IProcessContext context)
        {
            this.context = context ?? throw new ArgumentNullException(nameof(context));
            Category = ProcessCategory.Production;

            // 只在本类构造里关：不动基类默认值，其余流程照旧启用
            BehaviorConfig.IsEnabled = false;

            // 设备不通不该牵连其他流程
            BehaviorConfig.PauseOthersOnException = false;

            // 订阅放构造：每实例只跑一次；放 InitProcess 会随每次 Initialize 重入累积处理器。
            // 对应退订在 OnDispose（两者成对，缺一即泄漏——订阅源是单例设备管理器）。
            context.DeviceManager.DeviceChanged += OnDeviceChanged;
        }

        public override void InitProcess()
        {
            ProcessName = "测试-设备通讯";
            WaitTime = 2000;
        }

        /// <summary>
        /// 设备变更：只认自己那台，置空引用让下一拍重取。
        /// 🔴 回调跑在触发方线程，这里**只做引用赋值**，不碰设备——在这重连会把 UI 保存线程卡住。
        ///    要重连请另起 Task，且失败要自己判（Task.Run 的异常不会冒泡到调用方）。
        /// </summary>
        private void OnDeviceChanged(object sender, DeviceChangedEventArgs e)
        {
            if (e.Name != DeviceName) return;
            plc = null;
        }

        /// <summary>退订设备变更（与构造里的订阅成对）。</summary>
        protected override void OnDispose()
        {
            context.DeviceManager.DeviceChanged -= OnDeviceChanged;
        }

        protected override ProcessStepResult ExecuteStep(int step, CancellationToken token)
        {
            // CheckHealth 放行到这里的这一拍之间仍可能被设备变更置空（触发方线程），这里兜住
            if (plc == null)
            {
                LogHelp.Warn(string.Format("[{0}] 步序[{1}] 取不到设备 {2}，本轮跳过", ProcessName, step, DeviceName));
                return ProcessStepResult.Stay();
            }

            switch (step)
            {
                case 10:
                    groupName = "就绪";
                    tagName = "设备就绪1";
                    var response = plc.ReadTag<bool>(groupName, tagName);
                    LogHelp.Info(string.Format("[{0}] 步序[{1}] 读标签 {2}.{3} = {4}", ProcessName, step, groupName, tagName, response.Content));

                    return ProcessStepResult.Next();

                case 20:
                    groupName = "触发";
                    tagName = "启动电芯1扫码";
                    var response1 = plc.ReadTag<short>(groupName, tagName);
                    LogHelp.Info(string.Format("[{0}] 步序[{1}] 读标签 {2}.{3} = {4}", ProcessName, step, groupName, tagName, response1.Content));

                    // 整组读：重量组 29 个标签，一行铺开（日志会长，正常现象）
                    groupName = "重量";
                    var response2 = plc.ReadGroup(groupName);
                    LogHelp.Info(string.Format("[{0}] 步序[{1}] 读标签组 {2}.{3}", ProcessName, step, groupName, string.Join(", ", response2.Content.Select(kv => $"{kv.Key}={kv.Value}"))));

                    return ProcessStepResult.Next();

                case 30:
                    groupName = "压力";
                    tagName = "压力1";
                    var response3 = plc.ReadTag<float[]>(groupName, tagName);
                    LogHelp.Info(string.Format("[{0}] 步序[{1}] 读标签 {2}.{3} = {4}", ProcessName, step, groupName, tagName, string.Join(", ", response3.Content)));

                    groupName = "状态位";
                    tagName = "报警位";
                    var response4 = plc.ReadTag<bool[]>(groupName, tagName);
                    LogHelp.Info(string.Format("[{0}] 步序[{1}] 读标签 {2}.{3} = {4}", ProcessName, step, groupName, tagName, string.Join(", ", response4.Content)));

                    return ProcessStepResult.Next();

                default:
                    return ProcessStepResult.Jump(10);
            }
        }

        /// <summary>
        /// 健康检查：引用为空（首次 / 刚被变更通知置空）才重取，再判连接。
        /// 🔴 取实例只在 looper 线程（本方法）做：变更回调跑在触发方线程，
        ///    在那取实例等于让 UI 保存线程摸设备。
        /// 🔴 新实例刚建出来还没 Connect，这里返回 false 会进 E001——设备变更后需要有人连它，
        ///    这是设备侧的职责，不是流程的（流程只负责不拿旧实例）。
        /// </summary>
        protected override bool CheckHealth()
        {
            if (plc == null)
            {
                plc = context.DeviceManager.GetDevice(DeviceName)?.GetRawDev<IPlc>();
            }

            return plc != null && plc.IsConnected;
        }
    }
}
