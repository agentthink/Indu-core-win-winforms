using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using CommunityToolkit.Mvvm.Input;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.MesDriver.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.Pages.Extensions;
using Taoyl.UI.Pages.Models.Mes;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// MES 调试页 ViewModel（编辑会话域）。
    ///
    /// 草稿事务：进入编辑 = 换一个新的 Editing 实例（MesApiEditModel 构造即隔离，
    /// 只读 definition 的值、绝不回写），右栏所有改动只落在这个实例上；
    /// 取消 = 重新装载配置（同样换实例），磁盘与快照零变化。
    ///
    /// 保存链路：TryValidate（整单校验，任一不过留在编辑态）→ Editing.ToDefinition()
    /// → configManager.Save（内部落盘成功后才换快照）→ 退编辑态 → 刷新列表。
    ///
    /// 🔴 接口名允许改，但 Name 是调用方 Invoke 的键：流程里若硬编码接口名，改完必须同步改调用处。
    /// 技术上 SaveCore 会传 originalName 定位旧条目，改名不会留下残留条目，
    /// 也不会变成「新增一条 + 旧条目残留」。
    /// </summary>
    public partial class MesDebugViewModel
    {
        #region 命令

        [RelayCommand]
        private void Add()
        {
            if (!authService.HasPermission(PermissionCodes.BtnMesDebugAdd))
            {
                messageBox.ShowError("权限不足");
                return;
            }

            if (!LeaveCurrentSession())
                return;

            // 草稿灌成空定义即新增语义：Post / 无正文 / 15s / 1 次 / 500ms / Header
            Editing.LoadFrom(null);
            Session = MesDebugSession.Add;
        }

        [RelayCommand]
        private void Edit()
        {
            if (!authService.HasPermission(PermissionCodes.BtnMesDebugEdit))
            {
                messageBox.ShowError("权限不足");
                return;
            }

            if (Session != MesDebugSession.Browse)
                return;

            if (string.IsNullOrEmpty(SelectedApiName))
            {
                messageBox.ShowWarning("请先在左侧选择一个接口");
                return;
            }

            MesApiDefinition definition = configManager.GetApi(SelectedApiName);
            if (definition == null)
            {
                messageBox.ShowError("接口不存在（配置可能已被其它窗口修改），请刷新列表");
                return;
            }

            Editing.LoadFrom(definition);
            Session = MesDebugSession.Edit;
        }

        [RelayCommand]
        private void Save()
        {
            if (!authService.HasPermission(PermissionCodes.BtnMesDebugSave))
            {
                messageBox.ShowError("权限不足");
                return;
            }

            SaveCore();
        }

        [RelayCommand]
        private void Cancel()
        {
            if (!authService.HasPermission(PermissionCodes.BtnMesDebugCancel))
            {
                messageBox.ShowError("权限不足");
                return;
            }

            if (Session == MesDebugSession.Browse)
                return;

            if (!messageBox.ShowConfirm("放弃当前修改？"))
                return;

            CancelCore();
        }

        [RelayCommand]
        private void Delete()
        {
            if (!authService.HasPermission(PermissionCodes.BtnMesDebugDelete))
            {
                messageBox.ShowError("权限不足");
                return;
            }

            if (Session != MesDebugSession.Browse)
            {
                messageBox.ShowWarning("请先保存或取消当前编辑");
                return;
            }

            string name = SelectedApiName;
            if (string.IsNullOrEmpty(name))
            {
                messageBox.ShowWarning("请先在左侧选择一个接口");
                return;
            }

            if (!messageBox.ShowConfirm("确定删除接口「" + name + "」？该操作立即写入 Mes.json（旧文件会备份到 Backup/）。"))
                return;

            if (!configManager.Delete(name))
            {
                messageBox.ShowError("删除失败：" + (configManager.LastSaveError ?? "未知原因"));
                return;
            }

            // 只留痕、不带明细：整条接口已被移除，字段级 diff 没有意义
            auditService.Record(new AuditEntry(AuditCategory.Mes, AuditAction.Delete, name, CurrentActor()));

            ReloadApiList();
        }

        #endregion

        #region 切接口 Guard（三选一）

        /// <summary>
        /// 左栏切换意图的唯一入口。View 的 CellClick / KeyUp 汇合到 TrySwitchTo 后调它；
        /// 返回 false 表示用户选择「留在当前接口」，View 必须把选中项回滚到 SelectedApiName。
        /// 切走前的保存 / 放弃 / 留下三选一交给 <see cref="LeaveCurrentSession"/>。
        /// </summary>
        public bool TrySwitchApi(string target)
        {
            // 🔴 发送中不允许切接口：SendAsync 的 target 是局部变量（已固定为旧接口），
            // 切走之后回填的 Request / Response / 状态条仍是旧接口的，而界面已经显示新接口——
            // 「新接口的配置 + 旧接口的报文」是最容易把人带偏的一种显示。
            if (IsBusy)
                return false;

            if (Session == MesDebugSession.Browse)
            {
                SelectedApiName = target;
                return true;
            }

            if (string.Equals(target, SelectedApiName, StringComparison.Ordinal))
                return true;

            if (!LeaveCurrentSession())
                return false;

            SelectedApiName = target;
            return true;
        }

        /// <summary>
        /// 进入新会话前收尾旧会话（切接口与新增共用）。返回 false = 用户选择留下，调用方应中止。
        /// 三选一用两次二选一实现（IMessageBoxService 只提供 bool 语义的确认框）：
        /// 第一次问「是否保存」→ 是=保存；否=第二次问「是否放弃」→ 是=放弃，否=留下。
        /// </summary>
        private bool LeaveCurrentSession()
        {
            if (Session == MesDebugSession.Browse)
                return true;

            if (messageBox.ShowConfirm("当前接口有未保存的修改，是否保存？"))
                return SaveCore();

            if (messageBox.ShowConfirm("放弃这些修改？选「否」将留在当前编辑。"))
            {
                CancelCore();
                return true;
            }

            return false;
        }

        #endregion

        #region 保存 / 取消

        /// <summary>保存草稿。返回 false = 校验不过或落盘失败，编辑态保留。</summary>
        private bool SaveCore()
        {
            if (Session == MesDebugSession.Browse)
                return true;

            string error;
            if (!TryValidate(out error))
            {
                messageBox.ShowError(error);
                return false;
            }

            // 旧名用于定位要替换的那一条：新增传 null，编辑/改名传当前选中名。
            // 不传的话改名会变成「新增一条 + 旧条目残留」。
            string originalName = IsAdding ? null : SelectedApiName;

            // 审计基线必须在 Save 之前取：Save 成功即热替换快照，旧定义从 Current 中消失。
            // GetApi 返回深拷贝，之后 saved 再怎么变都污染不到它。
            MesApiDefinition baseline = string.IsNullOrEmpty(originalName) ? null : configManager.GetApi(originalName);

            // 保存前的选中项，决定最后要不要手动兜底装载（见下方）。
            // 与上面的 baseline（审计基线）是两回事：这个只是名字，用于判断「有没有改名」
            string selectedBeforeSave = SelectedApiName;

            MesApiDefinition saved = Editing.ToDefinition();

            if (!configManager.Save(originalName, saved))
            {
                messageBox.ShowError("保存失败：" + (configManager.LastSaveError ?? "未知原因"));
                return false;
            }

            string savedName = saved.Name;

            // 留痕须在业务写成功之后（铁律 1：宁漏记不记假账——Save 失败已 return，不产生记录）
            RecordSave(baseline, saved);

            Session = MesDebugSession.Browse;

            // 选中项直接落在新名上，绕开 ReloadApiList 的默认兜底（否则改名时它会先选第一条，
            // 右栏闪一下错的接口）：
            //   改名 / 新增 → SelectedApiName 变化 → OnSelectedApiNameChanged 装载（恰一次）
            //   不改名     → 值相同被 SetProperty 短路 → 不触发 → 用下面的兜底
            ReloadApiList(savedName);

            if (MesApiDefinition.NameEquals(selectedBeforeSave, savedName))
                LoadDisplay(savedName);   // 只有「名字没变」才需要手动兜底装载

            return true;
        }

        private void CancelCore()
        {
            Session = MesDebugSession.Browse;

            // 从配置重新灌草稿，覆盖掉右栏里改了一半的内容
            LoadDisplay(SelectedApiName);
        }

        #endregion

        #region 校验

        /// <summary>整单校验：任一不过即拒存。只读 Editing，不修改它。</summary>
        private bool TryValidate(out string error)
        {
            error = null;

            // 🔴 与 ToDefinition 同规则：落库用的是 Trim 后的名字，查重必须用同一个字符串。
            // 否则「 IF034」查重不撞「IF034」，ToDefinition 却存成「IF034」，
            // 落库侧不查重（见 MesConfigManager.Save）→ 静默追加出重复条目，
            // 后果是 FindApi 只认第一条（改的那条永不生效）、Delete 一次删两条。
            string name = (Editing.Name ?? string.Empty).Trim();

            if (name.Length == 0)
            {
                error = "接口名不能为空（调用方 Invoke 用的就是这个键）";
                return false;
            }

            // 改名允许（与 PLC 设备改名同规则）：Save 传旧名即可替换，不留残条。
            // 新名与其它条目重复由下面的查重拦。

            if (string.IsNullOrWhiteSpace(Editing.Url))
            {
                error = "接口地址不能为空";
                return false;
            }

            Uri uri;
            if (!Uri.TryCreate(Editing.Url, UriKind.Absolute, out uri)
                || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
            {
                error = "接口地址必须是完整的 http/https URL";
                return false;
            }

            // 三个数值字段在 UI 里是字符串，解析与范围一起校验：
            // 解析失败与越界给不同文案，用户才知道是「填了非数字」还是「数字太小」
            int timeoutMs;
            if (!int.TryParse((Editing.TimeoutMsText ?? string.Empty).Trim(), out timeoutMs))
            {
                error = "超时时间必须是整数（毫秒）";
                return false;
            }
            if (timeoutMs <= 0)
            {
                error = "超时时间必须大于 0 毫秒";
                return false;
            }

            int maxAttempts;
            if (!int.TryParse((Editing.MaxAttemptsText ?? string.Empty).Trim(), out maxAttempts))
            {
                error = "尝试次数必须是整数";
                return false;
            }
            if (maxAttempts < 1)
            {
                error = "尝试次数至少为 1（1 = 不重试）";
                return false;
            }

            int backoffMs;
            if (!int.TryParse((Editing.BackoffMsText ?? string.Empty).Trim(), out backoffMs))
            {
                error = "重试间隔必须是整数（毫秒）";
                return false;
            }
            if (backoffMs < 0)
            {
                error = "重试间隔不能为负数";
                return false;
            }

            // 正文：JSON 形态必须是合法 JSON（Text / Xml 不校验，Form 由键值对拼出来必然合法）
            if (Editing.BodyType == MesBodyType.Json && !string.IsNullOrWhiteSpace(Editing.BodyRawText))
            {
                try
                {
                    JToken.Parse(Editing.BodyRawText);
                }
                catch (Exception ex)
                {
                    error = "正文不是合法 JSON：" + ex.Message;
                    return false;
                }
            }

            // 表单：有值没键的行是漏填（空白末行不算——两列都空）
            foreach (MesHeader item in Editing.BodyFormItems)
            {
                if (item == null)
                    continue;

                if (string.IsNullOrWhiteSpace(item.Name)
                    && !string.IsNullOrWhiteSpace(item.Value))
                {
                    error = "表单正文里有「填了值但没填键」的行";
                    return false;
                }
            }

            // 接口名查重（新增时与所有既有条目比；编辑时排除自己）
            foreach (string existing in configManager.GetApiNames())
            {
                if (!MesApiDefinition.NameEquals(existing, name))
                    continue;

                if (!IsAdding && MesApiDefinition.NameEquals(existing, SelectedApiName))
                    continue;   // 就是自己

                error = "接口名「" + name + "」已存在";
                return false;
            }

            return true;
        }

        #endregion

        #region 审计留痕

        /// <summary>
        /// 保存留痕。新增只记「谁在什么时候加了哪条接口」，不带明细；
        /// 编辑走字段级 diff —— 改名会以 Name 旧值→新值的形式自动出现在明细里，不必单独处理。
        /// 🔴 只覆盖界面路径：手改 Mes.json 不产生任何记录（审计落点 = UI 命令层，
        /// 驱动层零依赖、结构上不知道审计存在）。
        /// </summary>
        private void RecordSave(MesApiDefinition baseline, MesApiDefinition saved)
        {
            var changes = new List<AuditFieldChange>();

            // 明细生成失败降级为「无明细」，但留痕本身照发——审计永不高于业务
            if (baseline != null)
            {
                try
                {
                    changes = auditService.BuildChanges(baseline, saved);
                }
                catch
                {
                    // 保持空明细
                }
            }

            auditService.Record(new AuditEntry(
                AuditCategory.Mes,
                baseline == null ? AuditAction.Add : AuditAction.Save,
                saved.Name,
                CurrentActor()) { Changes = changes });
        }

        /// <summary>
        /// 当前操作者快照。CurrentUser 未登录时是访客（用户名为空），
        /// AuditEntry 会把空用户名归一为「访客」——不借用他人身份（宁漏记不记假账）。
        /// </summary>
        private AuditActor CurrentActor()
        {
            var user = userSession.CurrentUser;
            return user == null ? null : user.ToAuditActor();
        }

        #endregion
    }
}
