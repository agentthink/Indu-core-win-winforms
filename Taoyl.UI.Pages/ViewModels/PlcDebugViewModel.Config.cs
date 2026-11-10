using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.Device.Interfaces;
using Taoyl.Application.PlcDriver.Contracts;
using Taoyl.Application.PlcDriver.Models.Config;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.Extensions;
using Taoyl.UI.Pages.Models;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// PlcDebugViewModel 分部 · 连接配置域（设备管理：新增/编辑/保存/取消/删除/重建）
    ///
    /// 本文件 = 设备管理 6 键全部内容（命令 + 会话 Guard + 草稿事务 + 编排调用）：
    /// - 会话互斥（编辑会话全局唯一）：进配置会话先收尾地址表会话（EnsureNoTagSession，脏→保存/放弃）；
    ///   进地址表编辑先收尾配置会话（EnsureNoConfigSession，脏→确认放弃）；
    /// - 草稿事务：编辑对象 = PlcConfig 深拷贝副本（ConfigDraft），字段直绑草稿（DataBindings/字典行写回）；
    ///   保存 = UI 预检（Name 必填 + §6.3 标签文件缺失警告放行）→ IDeviceOrchestration 编排（Task.Run 异步——
    ///   编排内实例释放可能阻塞至旧实例 IO 超时，禁 UI 线程直调）→ 成功收敛（ExitConfigSession + 列表重取选中新行）；
    ///   取消 = 有脏确认放弃 → ExitConfigSession(restoreRows: true) 弃副本还原浏览态（盘/缓存/实例零变化）；
    ///   退会话统一走 ExitConfigSession（三处共用，收敛弃草稿样板）；
    /// - 新增走编排口 AddDevice / 编辑保存走 UpdateDevice / 删除走 DeleteDevice / 重建走 BuildDevice（不落盘手动重置）；
    /// - 删除 = 编排口全链（实例释放 + 驱动缓存清 + Devices.json 删节落盘）；地址表 xlsx 不删（现场资产提示）；
    ///   门禁 = 配置编辑会话内且编辑子态（CanDeletePlc）——删除对象为正在编辑的实例，成功后先退会话再重取列表（2026-09-10 收紧）。
    /// - R3 例外：保存/删除/重建成功均弹提示——重建后连接态 = 断开（重建不含连接，连接按钮显式执行，补丁二决策）。
    /// </summary>
    public partial class PlcDebugViewModel
    {
        #region 会话互斥 Guard（进入任一会话先收尾另一会话）

        /// <summary>配置会话有未保存修改（草稿 vs 进入时快照逐字段比对）</summary>
        public bool IsConfigDirty => ConfigDraft != null && configSnapshot != null && !ConfigsEqual(ConfigDraft, configSnapshot);

        /// <summary>
        /// 进入配置会话前收尾地址表会话（btnPlcAdd/btnPlcEdit 前置）：
        /// 有脏走 ConfirmDiscardPending（先保存/放弃/取消）；放行后统一退地址表会话（无脏同样要退——会话互斥）。
        /// </summary>
        private bool EnsureNoTagSession()
        {
            if (!IsEditing) return true;
            if (!ConfirmDiscardPending()) return false;
            Session = PlcDebugSession.None; // 退地址表会话（Session 单点，互斥由枚举保证）
            return true;
        }

        /// <summary>
        /// 进入地址表编辑前收尾配置会话（EditTag 前置）：配置脏确认放弃；放行后退配置会话并还原浏览态配置区。
        /// </summary>
        public bool EnsureNoConfigSession()
        {
            if (!IsConfigEditing) return true;
            if (IsConfigDirty && !messageBox.ShowAsk("当前连接配置有未保存修改，确定放弃并编辑地址表？"))
                return false;

            ExitConfigSession(restoreRows: true);
            return true;
        }

        /// <summary>
        /// 切实例前配置脏拦截（View 设备列表 SelectionChanged 前置）：确认放弃才放行；
        /// 放行后由 OnSelectedDeviceChanged 钩子统一清会话与草稿。无配置会话/无脏直接放行。
        /// </summary>
        public bool ConfirmConfigDiscard()
        {
            if (!IsConfigEditing) return true;
            if (IsConfigDirty && !messageBox.ShowAsk("当前连接配置有未保存修改，确定放弃？"))
                return false;
            return true;
        }

        #endregion

        #region 设备管理命令（新增/编辑/保存/取消/删除/重建）

        // 权限门禁：各命令各用独立码（见 PermissionCodes.BtnPLCDebug*），档位见 Menu.json 该页 Features。
        // 门禁置于各命令体首行（先于业务判空）：无权限直接拒绝，不泄漏"当前是否选中实例"等业务态。

        [RelayCommand(CanExecute = nameof(CanAddPlc))]
        private void AddPlc()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugAddPlc))
            { messageBox.ShowError("权限不足"); return; }
            if (!EnsureNoTagSession()) return; // 地址表会话收尾（脏确认；取消中止）
            BeginConfigSession(CreateDefaultConfig(), isAdding: true);
            // 不留痕（2026-09-20）：新增此刻只进草稿会话，真正的账由 SavePlcAsync 编排成功后记
        }

        [RelayCommand(CanExecute = nameof(CanEditPlc))]
        private void EditPlc()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugEditPlc))
            { messageBox.ShowError("权限不足"); return; }
            var row = SelectedDevice;
            if (row == null) return;
            if (!EnsureNoTagSession()) return;

            var cfg = configManager.GetPlcConfig(row.Name); // 现取缓存配置做副本源（启动/保存后与磁盘一致）
            if (cfg == null)
            {
                messageBox.ShowError($"实例 [{row.Name}] 无配置（可能已被移除），无法编辑");
                return;
            }
            BeginConfigSession(CloneOf(cfg), isAdding: false);
            // 不留痕（2026-09-20）：进入编辑态此刻盘/缓存/实例零变化，真正的账由 SavePlcAsync 记
        }

        [RelayCommand(CanExecute = nameof(CanSavePlc))]
        private async Task SavePlcAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugSavePlc))
            { messageBox.ShowError("权限不足"); return; }
            var draft = ConfigDraft;
            if (draft == null) return;
            string oldName = IsPlcAdding ? null : SelectedDevice?.Name; // 旧名取于编排前（编排会释放/重建实例）

            string name = draft.Name?.Trim();
            draft.Name = name; // Trim 写回（校验方法只读不改，副作用留调用方）

            // 保存前配置校验（硬校验：任一不过留在编辑会话，草稿不丢、补填即可重存）
            if (!TryValidateDraft(draft, out string errMsg))
            {
                messageBox.ShowError(errMsg);
                return;
            }

            // §6.3 UI 预检：PlcTagsPath 变更且新文件不存在 → 警告放行（驱动侧 UpdatePlcConfig 重载失败留旧表/空表仅告警，不阻断保存）
            // 软警告 ≠ 硬校验：置于 TryValidateDraft 之后，硬校验不过时不必再问一遍
            if (!ConfirmTagsPathWarning(draft)) return;

            // 明细基线取于提交前：ExitConfigSession 会清 configSnapshot，提交后再取就没了
            // （新增子态下快照 = 默认模板，diff 出的是用户相对模板填的字段，语义同样成立）
            var changes = auditService.BuildChanges(configSnapshot, draft);

            ConfigBusy = true;
            string error = null;
            IDevice created = null;
            try
            {
                created = await Task.Run(() => IsPlcAdding
                    ? orchestration.AddDevice(DeviceTypeNames.Plc, draft)
                    : orchestration.UpdateDevice(DeviceTypeNames.Plc, oldName, draft));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                ConfigBusy = false;
            }

            if (error != null)
            {
                messageBox.ShowError($"保存失败: {error}");
                return; // 会话保持（草稿不丢，可修正重试；落盘失败等中途态已在编排语义内弹错收敛）
            }

            // 成功收敛：先退会话、后列表重取选新行（顺序铁律——LoadDevices 内 Devices.Clear 触发 DGV 选择联动，
            // View 拦截读会话态：若会话未退，草稿脏检测会误弹"放弃修改"确认；先退则 ConfirmConfigDiscard 直放行）
            ExitConfigSession(restoreRows: false);
            ReloadAndSelectDevice(created.Name); // 列表重取（DeviceRow 重挂新实例）→ 选中新行（钩子重载三块）

            // 留痕：编排成功（配置已落盘 + 实例已重建）才投递——铁律 1 宁漏记不记假账
            auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Save,
                AuditTargetName(created.Name), userSession.CurrentUser.ToAuditActor()) { Changes = changes });

            // R3 例外必要告知：重建后连接态 = 断开（重建不含连接）
            messageBox.ShowSuccess($"[{created.Name}] 配置已保存生效并重建实例；如原已连接，请重新点「连接」");
        }

        [RelayCommand(CanExecute = nameof(CanCancelPlc))]
        private void CancelPlc()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugCancelPlc))
            { messageBox.ShowError("权限不足"); return; }
            if (!ConfirmConfigDiscard()) return; // 有脏确认放弃（复用 Guard 统一确认——原内联 if 逐字重复）

            // 弃草稿还原浏览态（草稿弃 = 盘/缓存/实例零变化，副本隔离）
            // 不留痕：存在切实例 / 关窗体等不经本命令的退出路径（ConfirmConfigDiscard → OnSelectedDeviceChanged），
            // 记了覆盖不全，反会让"有 Edit 无 Cancel"被误读成"没放弃"——宁漏记不记假账
            ExitConfigSession(restoreRows: true);
        }

        [RelayCommand(CanExecute = nameof(CanDeletePlc))]
        private async Task DeletePlcAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugDeletePlc))
            { messageBox.ShowError("权限不足"); return; }
            // 删除对象 = 正在编辑的实例（门禁 = ConfigEdit 且非新增子态；EditPlc 不切列表选中，SelectedDevice 即被编辑设备）
            var row = SelectedDevice;
            if (row == null) return;
            if (!ConfirmDiscardPending()) return; // 防御：配置会话内地址表会话必为 None（会话互斥），理论上无脏

            string name = row.Name;
            if (!messageBox.ShowAsk($"确认删除设备 [{name}]？\n将释放实例并从配置文件移除（Devices.json 将 .bak 备份）；地址表 Excel 文件保留（现场资产）。"))
                return;

            ConfigBusy = true;
            string error = null;
            bool removed = false;
            try
            {
                removed = await Task.Run(() => orchestration.DeleteDevice(DeviceTypeNames.Plc, name));
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                ConfigBusy = false;
            }

            if (error != null)
            {
                messageBox.ShowError($"删除失败: {error}");
                return;
            }
            if (!removed)
            {
                messageBox.ShowError($"[{name}] 删除失败：设备不存在");
                return;
            }

            // 成功收敛：先退配置会话（顺序铁律——LoadDevices 内 Devices.Clear 触发 DGV 选择联动，
            // View 拦截读会话态：若会话未退，草稿脏检测会误弹"放弃修改"确认），再列表重取 + 选中邻行
            // （邻行由本方法显式计算，不依赖 DGV 自动补选——自动落行发生在 RaiseListChangedEvents=false 的
            //  中间态，用它驱动配置区刷新靠副作用确定，脆）
            ExitConfigSession(restoreRows: false);
            ReloadAndSelectAdjacent(row);
            // 留痕：编排成功（实例已释放 + 配置已从文件移除）才投递
            auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Delete,
                AuditTargetName(name), userSession.CurrentUser.ToAuditActor()));
            messageBox.ShowSuccess($"[{name}] 已删除（配置已从文件移除）");
        }

        [RelayCommand(CanExecute = nameof(CanRebuild))]
        private async Task RebuildPlcAsync()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugRebuildPlc))
            { messageBox.ShowError("权限不足"); return; }
            var row = SelectedDevice;
            if (row == null) return;

            string name = row.Name;
            if (!messageBox.ShowAsk($"[{name}] 重建将释放并重建当前实例（连接将断开，需重新点「连接」）。不改配置不落盘，确定？"))
                return;

            ConfigBusy = true;
            string error = null;
            IDevice created = null;
            try
            {
                created = await Task.Run(() =>
                {
                    var cfg = configManager.GetPlcConfig(name); // 现取缓存配置（重建键 = 不改配置的手动实例重置）
                    if (cfg == null)
                        throw new InvalidOperationException("实例配置不存在（可能已被移除）");
                    return orchestration.BuildDevice(DeviceTypeNames.Plc, name, cfg);
                });
            }
            catch (Exception ex)
            {
                error = ex.Message;
            }
            finally
            {
                ConfigBusy = false;
            }

            if (error != null)
            {
                messageBox.ShowError($"重建失败: {error}");
                return;
            }

            ReloadAndSelectDevice(name); // 列表重取（行重挂新实例）+ 选中保持
            // 留痕：重建 = 对现场设备施加操作（释放并重建实例，连接随之断开、产线可能停）——
            // 与连接/断开同源（WriteTag 已定稿要记，断连后果更重），必须同记
            auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Rebuild,
                AuditTargetName(name), userSession.CurrentUser.ToAuditActor()));
            messageBox.ShowSuccess($"[{name}] 已重建（配置不变）；如需连接请点「连接」");
        }

        #endregion

        #region 会话装配与收敛

        /// <summary>进入配置会话公共段：草稿 + 快照 + 子态 + 渲染源（行先换、会话位后置——View 刷新读正确渲染源）</summary>
        private void BeginConfigSession(PlcConfig draft, bool isAdding)
        {
            ConfigDraft = draft;
            configSnapshot = CloneOf(draft);
            IsPlcAdding = isAdding;
            ConfigRows = FlattenEditable(draft);
            Session = PlcDebugSession.ConfigEdit;
        }

        /// <summary>
        /// 退配置会话统一收敛（保存成功/取消/切实例前置 Guard 共用）：弃草稿/快照/新增子态 → Session 回 None。
        /// restoreRows=true：还原浏览态配置区（现取选中实例缓存配置；行先换、会话位后置——View 刷新读正确渲染源）；
        /// restoreRows=false：不还原（保存成功路径——随后 ReloadAndSelectDevice 重载列表与选中行，行会由钩子重刷）。
        /// </summary>
        private void ExitConfigSession(bool restoreRows)
        {
            ConfigDraft = null;
            configSnapshot = null;
            IsPlcAdding = false;
            if (restoreRows)
            {
                if (SelectedDevice != null) LoadPlcConfigRows(SelectedDevice.Name);
                else ConfigRows = new List<ConfigFlatRow>();
            }
            Session = PlcDebugSession.None;
        }

        /// <summary>列表重取 + 选中指定行（保存/删除/重建后收敛：DeviceRow 重挂新实例，旧引用作废——重新 GetDevice 惯例）</summary>
        private void ReloadAndSelectDevice(string devName)
        {
            LoadDevices();
            PlcSelectionLocked = false; // 先解锁：赋值 → View 跟随选中时走正常联动路径（不再被闸门拦）
            var target = Devices.FirstOrDefault(r => string.Equals(r.Name, devName, StringComparison.Ordinal));
            SelectedDevice = target; // 触发钩子：复位会话 + 现取三块（target=null 时清空态）
        }

        /// <summary>
        /// 列表重取 + 选中被删行的邻行（删除成功收敛专用）：删除对象仍为入参 removed 行（SelectedDevice 此刻仍指它）。
        /// 选中落点 = 删除前索引处（中间行 ⇒ 原下一行）→ 索引越界（删末行）⇒ 末行；列表空 ⇒ null（配置区走无选中提示）。
        /// 顺序铁律：先退会话、后重取（LoadDevices 清列表会触发 DGV 选择联动，View 拦截读会话态）。
        /// 显式收敛而非依赖 DGV 自动补选：重载期内 PlcSelectionLocked 拦住中间轮误选（Devices[0]，
        /// 会让配置区先重绘错误行再被覆盖 —— 残留"请选中实例"观感 + 闪烁），本方法赋值是唯一收敛点。
        /// </summary>
        private void ReloadAndSelectAdjacent(DeviceRow removed)
        {
            int idx = removed != null ? Devices.IndexOf(removed) : -1;
            LoadDevices();
            PlcSelectionLocked = false; // 先解锁：赋值 → View 跟随选中时走正常联动路径（不再被闸门拦）
            // 删除前索引即删除后的"下一行"落点；删末行/索引失效（-1）由 Math.Min 退到末行
            SelectedDevice = Devices.Count == 0
                ? null
                : Devices[Math.Min(idx < 0 ? Devices.Count - 1 : idx, Devices.Count - 1)];
        }

        /// <summary>草稿可编辑扁平行：Flatten 后给 Protocol 行注入候选选项源（协议白名单 = PlcProtocol 枚举规范串；浏览态行不注——只读渲染不出现下拉）</summary>
        private List<ConfigFlatRow> FlattenEditable(PlcConfig cfg)
        {
            var rows = ConfigFlatRow.Flatten(cfg);
            string[] options = Enum.GetNames(typeof(PlcProtocol));
            foreach (var r in rows)
            {
                if (r.Property != null && r.Property.Name == nameof(PlcConfig.Protocol))
                    r.OptionsSource = options;
            }
            return rows;
        }

        /// <summary>
        /// §6.3 保存前标签文件缺失警告（仅 PlcTagsPath 变更时检查）：新路径文件不存在 → 确认放行
        /// （驱动侧地址表重载失败仅告警留旧表/空表——"先保存配置后补表"工作流），取消则中止保存。
        /// 空值不可达：SavePlcAsync 前置必填校验已拦截（此处仅处理非空新路径的文件存在性）。
        /// </summary>
        private bool ConfirmTagsPathWarning(PlcConfig draft)
        {
            string oldPath = configSnapshot?.PlcTagsPath;
            if (string.Equals(oldPath ?? string.Empty, draft.PlcTagsPath ?? string.Empty, StringComparison.Ordinal))
                return true; // 未变更

            string fullPath = Path.IsPathRooted(draft.PlcTagsPath)
                ? draft.PlcTagsPath
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, draft.PlcTagsPath);
            if (File.Exists(fullPath))
                return true;

            return messageBox.ShowAsk(
                $"标签文件不存在: {draft.PlcTagsPath}\n保存后该实例地址表将为空（或沿用旧表），可先保存配置、后补地址表。是否继续？");
        }

        #endregion

        #region 保存前校验（硬校验：不过则留在编辑会话，草稿不丢）

        /// <summary>
        /// 配置草稿保存前整体校验（SavePlcAsync 前置）：校验 PlcConfig 全部公共成员，返回 false 时 errMsg = 首条失败文案。
        /// 为什么放在 UI 层：驱动侧 PlcDeviceFactory.TryValidateConfig 只覆盖 Name/Ip/Port/Protocol/PlcTagsPath 五项，
        /// 其余数值项（ConnectTimeOut/ReceiveTimeOut/PollingTime/RetryConnect）在驱动侧是 int.TryParse 静默回退默认
        /// ——错了不报错、静默用默认值，属"静默错"；且既有实现靠"编排抛异常 → 保存失败红叉"暴露 Ip/Port/Protocol 非法，
        /// 观感即 bug。本方法把这两类都提前成"响亮报错 + 明确文案"。
        ///
        /// 跳过项：ProtocolParams 字典**内容**（协议差异参数，各协议自读自校验——SiemensS7.ParsePlcType/ApplyRackSlot
        /// 已响亮抛错，工厂注释明确不重复校验）；仅校验字典本身非 null（防渲染/驱动 NRE 的最后防线）。
        /// 留空语义：Port/ConnectTimeOut/ReceiveTimeOut/PollingTime/RetryConnect/LogPath 留空一律放过，交驱动兜底
        /// （Port 空 = 协议默认端口；LogPath 空 = ./Logs/PLC/{Name}），与既有"留空 = 用默认"约定一致。
        /// 只读不改：不做任何 Trim/写回，副作用由 SavePlcAsync 负责（方法名 Try 须名副其实）。
        /// </summary>
        /// <param name="draft">配置草稿（ConfigDraft，编辑对象副本）</param>
        /// <param name="errMsg">首条失败文案（通过时为 null）</param>
        private static bool TryValidateDraft(PlcConfig draft, out string errMsg)
        {
            errMsg = null;
            if (draft == null)
            {
                errMsg = "配置草稿不存在，请重新进入编辑";
                return false;
            }

            // ① 设备名称：非空 + 不含非法文件名字符（设备名进 Devices.json 键与日志文件名）
            string name = draft.Name?.Trim();
            if (string.IsNullOrEmpty(name))
            {
                errMsg = "设备名称不能为空";
                return false;
            }
            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                errMsg = $"设备名称不能包含下列字符：{new string(Path.GetInvalidFileNameChars())}";
                return false;
            }

            // ② 设备类型：非空（固定 PLC）
            if (string.IsNullOrWhiteSpace(draft.Type))
            {
                errMsg = "设备类型不能为空";
                return false;
            }

            // ③ 连接端点：非空（网络协议=IP，串口协议=COMx）
            // 只校验非空：格式按协议形态分叉会把「协议知识」散进 UI，端点填错由驱动创建/连接阶段响亮报错
            if (string.IsNullOrWhiteSpace(draft.Ip))
            {
                errMsg = "连接端点不能为空（网络协议填 IP 地址，串口协议填串口号，如 COM3）";
                return false;
            }

            // ④ 端口：留空 = 协议默认端口；填了必须合法
            if (!string.IsNullOrWhiteSpace(draft.Port)
                && (!int.TryParse(draft.Port.Trim(), out int port) || port <= 0 || port > 65535))
            {
                errMsg = $"端口号无效: {draft.Port}（留空 = 用协议默认端口）";
                return false;
            }

            // ⑤ 协议：非空 + PlcProtocol 白名单（空协议被拦在此，给出明确文案，不走编排"不支持的通讯协议"异常通道）
            if (string.IsNullOrWhiteSpace(draft.Protocol))
            {
                errMsg = "通讯协议不能为空：请从下拉列表选择协议";
                return false;
            }
            if (!Enum.TryParse<PlcProtocol>(draft.Protocol.Trim(), ignoreCase: true, out _))
            {
                errMsg = $"不支持的通讯协议: {draft.Protocol}（可选：{string.Join(" / ", Enum.GetNames(typeof(PlcProtocol)))}）";
                return false;
            }

            // ⑥ 地址表路径：非空（§6.3 先保存后补表——只校验非空，文件存在性走 ConfirmTagsPathWarning 软警告）
            if (string.IsNullOrWhiteSpace(draft.PlcTagsPath))
            {
                errMsg = "地址表路径（PlcTagsPath）不能为空：请填写标签 Excel 路径后保存（可先填路径、后补表文件）";
                return false;
            }

            // ⑦⑧ 连接/接收超时：留空 = 驱动回退默认；填了必须为正整数
            if (!IsBlankOrPositiveInt(draft.ConnectTimeOut))
            {
                errMsg = $"连接超时时间无效: {draft.ConnectTimeOut}（留空 = 用默认值；需为正整数毫秒）";
                return false;
            }
            if (!IsBlankOrPositiveInt(draft.ReceiveTimeOut))
            {
                errMsg = $"接收超时时间无效: {draft.ReceiveTimeOut}（留空 = 用默认值；需为正整数毫秒）";
                return false;
            }

            // ⑨ 轮询间隔：留空 = 驱动回退默认；填了须 >= 20（对齐 PlcRuntime 的 Math.Max(20, ...) 下限）
            if (!string.IsNullOrWhiteSpace(draft.PollingTime)
                && (!int.TryParse(draft.PollingTime.Trim(), out int polling) || polling < 20))
            {
                errMsg = $"轮询间隔无效: {draft.PollingTime}（留空 = 用默认值；最小 20 ms）";
                return false;
            }

            // ⑩ 重连次数：留空 = 驱动回退默认；<=0 表无限重连，属合法语义，仅校验可解析为整数
            if (!string.IsNullOrWhiteSpace(draft.RetryConnect)
                && !int.TryParse(draft.RetryConnect.Trim(), out _))
            {
                errMsg = $"重连次数无效: {draft.RetryConnect}（留空 = 用默认值；需为整数，≤0 表示无限重连）";
                return false;
            }

            // ⑪ 日志目录：留空 = 驱动兜底 ./Logs/PLC/{Name}；不校验内容（不猜路径语义）
            // ⑫ 协议差异参数：字典内容跳过（各协议自读自校验）；仅保证字典非 null
            if (draft.ProtocolParams == null)
                draft.ProtocolParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            return true;
        }

        /// <summary>留空或正整数（数值项"留空 = 用默认值"的共用判据）</summary>
        private static bool IsBlankOrPositiveInt(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return true;
            return int.TryParse(text.Trim(), out int value) && value > 0;
        }

        #endregion

        #region 草稿工具（深拷贝 / 逐字段相等 / 新增默认模板）

        /// <summary>
        /// PlcConfig 深拷贝（JSON 往返：字段随模型自动跟进，杜绝"新增成员漏写"——原分字段构造即漏了
        /// ConnectTimeOut/ReceiveTimeOut，导致编辑态草稿这两个字段为 null、显示空且保存即丢）。
        /// 必须用默认 JsonConvert（ObjectCreationHandling = Auto，与 ConfigBase.Clone&lt;T&gt; 同契约）：
        /// Auto 模式下字典属性复用属性初始化器实例再填充 → PlcConfig.ProtocolParams 的
        /// OrdinalIgnoreCase 比较器得以保留（驱动侧 GetProtocolParam 以 PascalCase 键硬编码取值，依赖它）。
        /// </summary>
        private static PlcConfig CloneOf(PlcConfig src)
        {
            if (src == null) return null;

            var clone = JsonConvert.DeserializeObject<PlcConfig>(JsonConvert.SerializeObject(src));
            // 兜底：JSON 中 protocolParams 为 null 时（显式 null 令牌会覆盖初始化器）反序列化得 null 字典，
            // 后续 Flatten/渲染/驱动读取都按"非空字典"约定——此处补回，且同样带 OrdinalIgnoreCase
            if (clone.ProtocolParams == null)
                clone.ProtocolParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            return clone;
        }

        /// <summary>
        /// PlcConfig 内容相等（脏检测 = 草稿 vs 快照 !ConfigsEqual）。
        /// 走 JToken.DeepEquals：按属性名比对、与键序无关，且随模型自动覆盖新成员（原逐字段比对同样漏了
        /// ConnectTimeOut/ReceiveTimeOut → 改这两个字段不置脏，保存与放弃的语义都会错）。
        /// 注意 DeepEquals 认为 null ≠ ""（安全侧：宁可多弹一次"放弃修改"确认，不可漏判丢失修改）。
        /// </summary>
        private static bool ConfigsEqual(PlcConfig a, PlcConfig b)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a == null || b == null) return false;

            return JToken.DeepEquals(JToken.FromObject(a), JToken.FromObject(b));
        }

        /// <summary>
        /// 新增默认模板（Type=PLC/IP=127.0.0.1/**协议留空**/ConnectTimeOut=5000/
        /// ReceiveTimeOut=10000/PollingTime=100/RetryConnect=0；Name 留空待填）。
        /// 协议留空 = 强制用户显式选择：空协议被 TryValidateDraft 拦截并给出"请从下拉列表选择协议"，
        /// 不预设欧姆龙默认值（预设会让用户漏选时静默建成错误协议的实例）。
        /// Port 留空 = 用驱动层默认端口（欧姆龙 44818 / 西门子 102），故换协议无需改端口，
        /// 新建任何协议都不用管这一格；现场非标准端口（如 NAT 映射）才需填。
        /// 超时两项必须给出：驱动侧对空值走 int.TryParse 失败回退默认，但界面留空会与"用户清空"无法区分，
        /// 且早前实现漏给 → 新增设备表单里这两项显示空。
        /// </summary>
        private static PlcConfig CreateDefaultConfig()
        {
            return new PlcConfig
            {
                Name = string.Empty,
                Type = DeviceTypeNames.Plc,
                Ip = "127.0.0.1",
                Port = string.Empty,
                Protocol = string.Empty,
                ConnectTimeOut = "5000",
                ReceiveTimeOut = "10000",
                PlcTagsPath = null,
                PollingTime = "100",
                RetryConnect = "0",
                LogPath = "./Logs/PLC",
                ProtocolParams = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase),
            };
        }

        #endregion
    }
}
