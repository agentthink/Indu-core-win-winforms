using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.IO;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.Pages.Extensions;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// PlcDebugViewModel 分部 · 运行时交互（连接 / 导入导出 / 读写测试）
    ///
    /// 本文件 = 与设备实例的运行时交互通道（不涉地址表编辑事务——那在 TagTable 分部）：
    /// - 连接/断开：Connect/Disconnect（ConnectionBusy 防抖，IoWorker 接管自动重连）；
    /// - 导入/导出地址表：LoadTagPollingConfigForPlc 覆盖内存（不落盘，落盘经保存按钮；仅编辑会话内可用——导入=整体替换属
    ///   变更操作，统一门禁；导入成功保持编辑态，可保存固化或选组继续）/ SaveTagPollingConfigToExcel 纯读导出（浏览态可用）；
    ///   （文件对话框在 View，路径传入）；
    /// - 读写测试：统一 ExecuteIo 通用原语（值自动回填容器，UI 零 switch；PlcTextValue.Parse 文本解析收驱动）。
    /// </summary>
    public partial class PlcDebugViewModel
    {
        #region 连接/断开

        // 权限门禁：连接 / 断开各用独立码（不改配置不落盘，但仍属"对现场设备施加动作"）
        // 与读写测试分开编码，便于现场「可看可读但禁操作」分档

        [RelayCommand(CanExecute = nameof(CanConnect))]
        private async Task ConnectAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugConnect))
            { messageBox.ShowError("权限不足"); return; }
            if (SelectedDevice == null) return;
            string name = SelectedDevice.Name;
            var dev = orchestration.GetDevice(name);
            if (dev == null)
            {
                messageBox.ShowError($"实例 [{name}] 不存在，可能已被移除");
                return;
            }

            ConnectionBusy = true;
            string errMessage = null;
            bool ok = false;
            try
            {
                ok = await Task.Run(() =>
                {
                    try { return dev.Connect(); }
                    catch (Exception ex) { errMessage = ex.Message; return false; }
                });
            }
            finally
            {
                ConnectionBusy = false;
            }
            if (!ok)
            {
                messageBox.ShowError(errMessage != null
                    ? $"[{name}] 连接异常: {errMessage}"
                    : $"[{name}] 连接失败（IoWorker 已接管自动重连）");
                // 留痕：连接成败都是现场追溯重点，失败走 Result=失败态（是"做了但没成"，不是"没发生"）
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Connect,
                    AuditTargetName(name), userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
                return;
            }

            auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Connect,
                AuditTargetName(name), userSession.CurrentUser.ToAuditActor()));
        }

        [RelayCommand(CanExecute = nameof(CanDisconnect))]
        private async Task DisconnectAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugDisconnect))
            { messageBox.ShowError("权限不足"); return; }
            if (SelectedDevice == null) return;
            string name = SelectedDevice.Name;
            var dev = orchestration.GetDevice(name);
            if (dev == null)
            {
                messageBox.ShowError($"实例 [{name}] 不存在，可能已被移除");
                return;
            }

            ConnectionBusy = true;
            string errMessage = null;
            try
            {
                await Task.Run(() =>
                {
                    try { dev.Disconnect(); }
                    catch (Exception ex) { errMessage = ex.Message; }
                });
            }
            finally
            {
                ConnectionBusy = false;
            }
            if (errMessage != null)
            {
                messageBox.ShowError($"[{name}] 断开异常: {errMessage}");
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Disconnect,
                    AuditTargetName(name), userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
                return;
            }

            // 留痕：断开 = 让现场设备脱离上位机（产线可能就此停），是设备域最需要追溯的动作之一
            auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Disconnect,
                AuditTargetName(name), userSession.CurrentUser.ToAuditActor()));
        }

        #endregion

        #region 导入/导出地址表（文件对话框在 View，路径传入）

        // 权限门禁：导入 / 导出 / 保存落盘各用独立码——
        // 导入 = 地址表变更操作（整体替换内存地址表，仅在编辑会话内可用）；
        // 导出 = 纯读零副作用（现场可只读放开）；保存落盘 = 不可逆写文件。

        [RelayCommand(CanExecute = nameof(CanImport))]
        private void Import(string excelPath)
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugImport))
            { messageBox.ShowError("权限不足"); return; }
            if (SelectedDevice == null || string.IsNullOrEmpty(excelPath)) return;

            // 覆盖导入前确认未保存修改（保存/放弃后放行；取消中止——导入整体替换组表对象，工作集作废）
            if (!ConfirmDiscardPending())
                return;

            string name = SelectedDevice.Name;
            try
            {
                if (!configManager.LoadTagPollingConfigForPlc(name, excelPath))
                {
                    messageBox.ShowError($"[{name}] 导入失败（无该实例地址表/IO 异常），请查看日志");
                    auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Import,
                        AuditTargetName(name), userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
                    return;
                }
                // 成功静默：内存替换即生效（重启回退原配置——导入不落盘）
                // 先清选中组行再刷组工作集（与切实例钩子同款顺序教训）：SelectedGroupRow=null 先行 →
                // 钩子派生 SelectedGroupName=null → RebuildTagRows 清旧标签工作集（含脏/删除清单）。
                // 其后 LoadGroupRows 重建组工作集——新地址表组结构未知，等用户从组 DGV 手动选择：
                // 无选中组行 = 无当前组 = 标签 DGV 空（无选中=无标签展示）
                SelectedGroupRow = null;
                LoadGroupRows(name);
                // 导入在编辑会话内执行（CanImport=Session==TagTableEdit）：成功后保持编辑态——
                // 可直接点保存固化（实例级落盘含导入全部组）或选组继续微调；取消退出但不回滚导入的内存覆盖

                // 留痕：导入 = 整体替换内存地址表（落盘经保存按钮）。Target 取设备名而非文件名——
                // 设备域所有动作以实例为锚，文件名会挤掉"查这台设备被做了什么"这条主线
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Import,
                    AuditTargetName(name), userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"[{name}] 导入异常: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanExport))]
        private void Export(string excelPath)
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugExport))
            { messageBox.ShowError("权限不足"); return; }
            if (SelectedDevice == null || string.IsNullOrEmpty(excelPath)) return;
            string name = SelectedDevice.Name;
            try
            {
                if (!configManager.SaveTagPollingConfigToExcel(name, excelPath))
                {
                    messageBox.ShowError($"[{name}] 导出失败（无该实例地址表/IO 异常），请查看日志");
                    auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Export,
                        AuditTargetName(name), userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
                    return;
                }
                // 成功静默（保存对话框即反馈）
                // 留痕：数据出库是受控动作（Target 同导入取设备名）
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Export,
                    AuditTargetName(name), userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"[{name}] 导出异常: {ex.Message}");
            }
        }

        [RelayCommand(CanExecute = nameof(CanSaveTag))]
        private void SaveTagConfig()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugSaveTag))
            { messageBox.ShowError("权限不足"); return; }
            // 提交 + 落盘三段式（成功静默；失败/落盘告警内部弹窗——R3 反馈契约不破）
            // 审计留痕在三段式内部（TrySaveAll）：明细必须在组/标签提交清脏态之前采集，此处拿不到
            TrySaveAll();
        }

        #endregion

        #region 读写测试（统一 ExecuteIo 通用原语：值自动回填容器，UI 零 switch；不强制连接，失败自然反馈）

        // 权限门禁：读 / 写各用独立码（同一动作域的两个方向——读只是观察，写会改 PLC 现场状态，
        // 分开编码后现场可直接做到"可读禁写"）

        [RelayCommand(CanExecute = nameof(CanReadWrite))]
        private async Task ReadAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugRead))
            { messageBox.ShowError("权限不足"); return; }
            var row = SelectedRow;
            if (row == null || row.Source == null || SelectedDevice == null || string.IsNullOrEmpty(SelectedGroupName)) return;
            var plc = GetPlcOrNull();
            if (plc == null) return;

            var tag = row.Source; // 驱动真值（CanReadWrite 已禁草稿行 → 组表条目有效）
            string group = SelectedGroupName;
            string tagName = tag.TagName;

            // 读请求：值经 tag.TypedValue 输出槽自动回填，展示列 1s 快照即见
            var request = new IoRequest(IoRequestKind.Read, tag.Address, tag.DataType, tag.Length, null, group, tagName);

            // 留痕目标：组.标签名（与地址表明细同一路径规则）
            string auditTarget = group + "." + tagName;
            try
            {
                ResponseResult result = await Task.Run(() => plc.ExecuteIo(request));
                if (!result.IsSuccess)
                {
                    messageBox.ShowError($"读取 [{tagName}] 失败: [{result.ErrorCode}] {result.Message}");
                    auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.ReadTag,
                        auditTarget, userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
                    return;
                }
                // 成功静默
                // 留痕：只记"谁读了哪个标签"，不记读到的值——读不改数据（明细层语义），
                // 且记值会被调试期的高频点击灌满审计表
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.ReadTag,
                    auditTarget, userSession.CurrentUser.ToAuditActor()));
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"读取 [{tagName}] 异常: {ex.Message}");
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.ReadTag,
                    auditTarget, userSession.CurrentUser.ToAuditActor(), AuditResult.Fail));
            }
        }

        [RelayCommand(CanExecute = nameof(CanReadWrite))]
        private async Task WriteAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugWrite))
            { messageBox.ShowError("权限不足"); return; }
            var row = SelectedRow;
            if (row == null || row.Source == null || SelectedDevice == null || string.IsNullOrEmpty(SelectedGroupName)) return;
            var plc = GetPlcOrNull();
            if (plc == null) return;

            var tag = row.Source; // 驱动真值（CanReadWrite 已禁草稿行）
            string group = SelectedGroupName;
            string tagName = tag.TagName;
            string text = row.WriteValue; // 点动写输入缓冲（行副本，编辑不置脏）

            if (string.IsNullOrWhiteSpace(text))
            {
                messageBox.ShowError($"请先在 [{tagName}] 的「写入值」列填写要写入的值");
                return;
            }

            // 文本解析收进驱动（与导出 FormatValueForExport 对称），UI 零 switch
            var parsed = PlcTextValue.Parse(tag.DataType, text);
            if (!parsed.IsSuccess)
            {
                messageBox.ShowError($"[{tagName}] 值解析失败: {parsed.Message}");
                return;
            }

            var request = new IoRequest(IoRequestKind.Write, tag.Address, tag.DataType, tag.Length, parsed.Content, group, tagName);

            // 明细：Action「写标签」+ ChangeType「修改」（两层语义分属动作与数据，勿混）。
            // 旧值留空 = 不可知（AuditFieldChange 定稿）：写前读到的缓存值既非操作者所见、也非 PLC 真值，记了就是半假账
            string auditTarget = group + "." + tagName;
            var auditChanges = new List<AuditFieldChange>
            {
                new AuditFieldChange
                {
                    FieldPath = auditTarget,
                    ChangeType = AuditChangeType.Update,
                    OldValue = null,
                    NewValue = Convert.ToString(parsed.Content, CultureInfo.InvariantCulture)
                }
            };

            try
            {
                ResponseResult result = await Task.Run(() => plc.ExecuteIo(request));
                if (!result.IsSuccess)
                {
                    messageBox.ShowError($"写入 [{tagName}] 失败: [{result.ErrorCode}] {result.Message}");
                    auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.WriteTag,
                        auditTarget, userSession.CurrentUser.ToAuditActor(), AuditResult.Fail) { Changes = auditChanges });
                    return;
                }
                // 成功静默（写成功值已回填容器）
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.WriteTag,
                    auditTarget, userSession.CurrentUser.ToAuditActor()) { Changes = auditChanges });
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"写入 [{tagName}] 异常: {ex.Message}");
                auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.WriteTag,
                    auditTarget, userSession.CurrentUser.ToAuditActor(), AuditResult.Fail) { Changes = auditChanges });
            }
        }

        /// <summary>现取选中实例的 IPlc（GetRawDev 失败/实例消失返回 null，内部已弹窗）</summary>
        private IPlc GetPlcOrNull()
        {
            if (SelectedDevice == null) return null;
            var dev = orchestration.GetDevice(SelectedDevice.Name);
            if (dev == null)
            {
                messageBox.ShowError($"实例 [{SelectedDevice.Name}] 不存在，可能已被移除");
                return null;
            }
            try
            {
                return dev.GetRawDev<IPlc>();
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"获取 PLC 驱动失败: {ex.Message}");
                return null;
            }
        }

        #endregion

        #region 队列深度节拍（1s：现取选中实例 IoWorker 队列挂起数，值变才通知）

        /// <summary>
        /// 现取选中实例的 IO 队列深度（View pollTimer 1s 调 + OnSelectedDeviceChanged 切实例即时调）：
        /// 编排口 GetDevice 现取——DeviceRow 剥离 PLC 专属字段后（09-09 决策：通用设备行零 PLC 依赖，可复用于后续
        /// Scanner/Scale 等设备列表），本方法为此运行值唯一入口，旧引用失效问题经现取天然规避；
        /// GetRawDev 类型不符抛 InvalidCastException（IDevice 契约文档）→ catch 置 0。
        /// 节拍内静默容错不弹窗：重建间隙/驱动缺失 = 队列本就清空，置 0 语义成立；值变才赋值触发通知，无变化零重绘。
        /// </summary>
        public void RefreshPlcQueueDepth()
        {
            int value = 0;
            if (SelectedDevice != null)
            {
                try
                {
                    var dev = orchestration.GetDevice(SelectedDevice.Name);
                    value = dev != null ? dev.GetRawDev<IPlc>().QueueDepth : 0;
                }
                catch
                {
                    value = 0;
                }
            }
            if (value == CurrentQueueDepth) return;
            CurrentQueueDepth = value; // [ObservableProperty] setter 自动通知绑定
        }

        #endregion
    }
}
