using CommunityToolkit.Mvvm.Input;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Taoyl.Application.Audit.Constants;
using Taoyl.Application.Audit.Models;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.Share.Auth;
using Taoyl.UI.Pages.Extensions;
using Taoyl.UI.Pages.Models;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// PlcDebugViewModel 分部 · 地址表域（工作集编辑 + 保存事务）
    ///
    /// 本文件 = 对地址表增删改保存的全部内容（行操作命令 + 脏态通知 + 三段式保存），View 只做转发与视觉：
    /// - 工作集节拍与通知：1s 快照（RefreshTagRowValues 行级 ResetItem）、差异置脏统一入口（row.HasRealChanges()——归位 Model，改回原值自动清假脏）；
    /// - 地址表行操作：增/删/上移/下移/序号重排/改名（行命令全落 TagRows 标记，保存才提交组表）。
    ///   区结构 = 共用命令入口（按 IsGroupScope 分流）→「组级行操作」子 region（GroupRows）→「子级行操作」子 region（TagRows），两轨成对对称；
    /// - 刷新频率：设置当前（选中行）/ 设置所有（当前组）；
    /// - 未保存确认与保存三段式：ConfirmDiscardPending（两问组合三选）+ TrySaveAll（预检→ApplyEdits→落盘）。
    ///
    /// 跨文件依赖（partial 类成员直接可见）：TagRows/SelectedRow/_removedTagNames/_editingRow 声明在主文件；
    /// RebuildTagRows（保存后收敛重快照）/RaiseDirtyRelated 定义见下或主文件。
    /// </summary>
    public partial class PlcDebugViewModel
    {
        #region 工作集节拍与通知

        /// <summary>
        /// 1s 节拍：遍历工作集刷新展示列（读 Source 拼预格式化字符串），仅值变化行 ResetItem 行级通知（ProcessMonitor 先例粒度，非全表 Invalidate）。
        /// 只碰展示列永不碰编辑列；豁免正在编辑的行（ResetItem 经 CurrencyManager 会干扰未提交单元格）——该行编辑结束后下轮补刷。
        /// 调用方：View pollTimer Tick（UI 线程；组内上千行再评估 Task.Run）。
        /// </summary>
        public void RefreshTagRowValues()
        {
            for (int i = 0; i < TagRows.Count; i++)
            {
                var row = TagRows[i];
                if (row == _editingRow) continue;
                if (row.RefreshSnapshot())
                    TagRows.ResetItem(i);
            }
        }

        /// <summary>
        /// 行编辑提交（View CellValueChanged 调）：属性列改动按真实差异置脏（WriteValue 缓冲列由 View 排除不调此方法）。
        /// 差异判定：编辑值改回与组表 Source 一致 → 自动清脏（防"改名撞重名回滚/手改回原值"后假脏明黄）。
        /// </summary>
        public void NotifyRowEdited(TagItemRow row)
        {
            if (row != null && !row.IsNew)
                row.IsDirty = row.HasRealChanges(); // 差异判定归位 Model：改回与组表 Source 一致 → 自动清脏
            RaiseDirtyRelated();
        }

        /// <summary>行进入/结束编辑（View CellBeginEdit/CellEndEdit 调）：1s 快照豁免登记（null=无编辑）</summary>
        public void NotifyRowEditing(TagItemRow row)
        {
            _editingRow = row;
        }

        /// <summary>脏态相关通知：读/写/设频率按钮（草稿行禁）+ 保存按钮计数文案。
        /// CanSetAll 不通知——其表达式不依赖脏态（仅 IsGroupScope/选中设备/有组/Session，全由 setter [NotifyPropertyChangedFor] 转发，2026-09-10 收窄）</summary>
        private void RaiseDirtyRelated()
        {
            OnPropertyChanged(nameof(CanReadWrite));
            OnPropertyChanged(nameof(CanSetCurrent));
            OnPropertyChanged(nameof(SaveButtonText));
        }

        // ---- 组工作集（TagGroupRow）差异/编辑辅助：与标签工作集同构 ----

        /// <summary>组行编辑提交（View 组 DGV CellValueChanged 调）：组名/描述列改动按真实差异置脏</summary>
        public void NotifyGroupRowEdited(TagGroupRow row)
        {
            if (row != null && !row.IsNew)
                row.IsDirty = row.HasRealChanges();
            RaiseDirtyRelated();
        }

        /// <summary>组行进入/结束编辑（View 组 DGV CellBeginEdit/CellEndEdit 调）：
        /// ⚠️ 预留声明（2026-09-10 定稿）：组 DGV 暂无展示列节拍，_editingGroupRow 当前无消费者（组行四列全静态/实时计算属性）；
        /// 将来组级表格扩展动态展示列（对标标签侧 RefreshTagRowValues 的 ResetItem 豁免）时，节拍方法读本字段跳过编辑中行。
        /// 登记链（View BeginEdit/EndEdit 写入）已就绪——勿按死码误删，注释勿回退。</summary>
        public void NotifyGroupRowEditing(TagGroupRow row)
        {
            _editingGroupRow = row;
        }

        /// <summary>行视觉刷新（回滚/置脏后同步行色/文本；非编辑管线内调用）——组/标签工作集共用泛型（原 ResetRowVisual/ResetGroupRowVisual 逐字同构合并）</summary>
        private static void ResetItemIfPresent<T>(BindingList<T> list, T row) where T : class
        {
            if (row == null) return;
            int idx = list.IndexOf(row);
            if (idx >= 0) list.ResetItem(idx);
        }

        #endregion

        #region 编辑会话门禁（浏览态 ↔ 编辑态）

        // 权限门禁：EditTag = 地址表编辑会话总闸（BtnPLCDebug_TagEdit）——未授权者进不了会话，
        // 信息列恒只读、行操作按钮恒灰，故 DGV 编辑管线回调（RenameTag/NotifyRowEdited/OnTagIdEdited）无需再设权限。
        // 行操作/设频率（Add/Remove/MoveUp/MoveDown/SetCurrentFreq/SetAllFreq）同码：它们全在 TagTableEdit 内可达，
        // 复用同一码避免"取消编辑要不要权限"这类噪音码；现场若要细分须先在 Menu.json 拆码。

        /// <summary>
        /// 进入编辑会话（btnTagEdit）：先收尾连接配置会话（EnsureNoConfigSession——会话互斥：脏确认放弃）；
        /// 浏览态恒无脏（脏源=信息列/增删移/设频率全被门禁锁死）→ 工作集即干净快照，
        /// 无需重快照，直接置位即可安全编辑。置位通知转发全部标签域 CanXxx → View 订阅 Session 切换 DGV 列只读。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanEditTag))]
        private void EditTag()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugEditTag))
            { messageBox.ShowError("权限不足"); return; }
            if (!EnsureNoConfigSession()) return; // 配置会话收尾（脏确认放弃/取消中止）——编辑会话全局唯一
            Session = PlcDebugSession.TagTableEdit;
            // 不留痕（2026-09-20）：进入编辑会话此刻盘/缓存/实例零变化；
            // 会话内的增删改移 / 设频率也不单独记——它们的变化已被保存时的明细整体聚合，
            // 逐条记等于同一变化记两次（且上千标签的表会被行操作刷屏）
        }

        /// <summary>
        /// 取消编辑会话（btnTagCancel）：有脏先确认放弃；无脏（如导入后未改动想退出）直接退。
        /// 丢弃实现 = 组工作集 + 标签工作集从组表重快照（删行恢复/新增消失/改名还原一次性全清）。
        /// 取消 = 留在当前组（不跳首组）：重快照整体换新组行对象 → DGV 重绑自动落首行 → 按组表键回选原组
        /// （组行对象已换新，须按 SourceGroupName 重定位；原组已删/当前组为空则无选中，语义正确）。
        /// ⚠️ 边界：导入的内存覆盖不随取消回滚（无导入前快照，驱动已热用）；反悔窗=重导正确文件或未保存重启回退。
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanCancelTag))]
        private void CancelTagEdit()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugCancelTag))
            { messageBox.ShowError("权限不足"); return; }
            if (HasPendingChanges && !messageBox.ShowAsk($"当前地址表有 {PendingCount} 处未保存修改，确定放弃？"))
                return;

            // 不留痕：取消 = 什么都没发生（工作集整体重快照，盘/缓存/实例零变化）；
            // 且存在切实例 / 切组等不经本命令的丢弃路径，记了覆盖不全——宁漏记不记假账
            string keepGroup = SelectedGroupName; // 取消前当前组键（重绑后须回选）
            LoadGroupRows(SelectedDevice.Name); // 组工作集重快照（丢弃组编辑：删组恢复/新增消失/改名还原）
            _removedGroupNames.Clear();
            // 回选原组：组行对象已整体换新 → 按 SourceGroupName 重定位（同值不改 SelectedGroupName，故下方 RebuildTagRows 仍需显式调）
            SelectedGroupRow = string.IsNullOrEmpty(keepGroup)
                ? null
                : GroupRows.FirstOrDefault(g => string.Equals(g.SourceGroupName, keepGroup, StringComparison.Ordinal));
            RebuildTagRows();                    // 标签工作集重快照（丢弃标签编辑）
            Session = PlcDebugSession.None;
        }

        #endregion

        #region 地址表行操作（radio 双轨：组级=操作组 DGV（GroupRows）；子级=操作标签 DGV（TagRows）。命令入口按 IsGroupScope 分流，逻辑各自下沉）
        // 结构：共用命令入口（Add/Remove/MoveUp/MoveDown 四分流 + NextDefaultName 取名尾段）→ 组级行操作子 region → 子级行操作子 region。
        // 两轨方法刻意保持成对对称（新增/删除/移动/改名/重名检查各一对）——改动一侧须同步核对另一侧（序号收紧、置脏判定等曾因单侧缺项出过现场缺陷）。

        // ---- 共用：命令入口（按 IsGroupScope 分流到两轨，逻辑各自下沉）+ 默认取名尾段 ----

        [RelayCommand(CanExecute = nameof(CanAddRow))]
        private void AddRow()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugAddRow))
            { messageBox.ShowError("权限不足"); return; }
            if (IsGroupScope)
            {
                AddGroupCore();
                return;
            }
            AddTagRowCore();
        }

        [RelayCommand(CanExecute = nameof(CanRemoveRow))]
        private void RemoveRow()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugRemoveRow))
            { messageBox.ShowError("权限不足"); return; }
            if (IsGroupScope)
            {
                RemoveGroupCore();
                return;
            }
            RemoveTagRowCore();
        }

        [RelayCommand(CanExecute = nameof(CanMoveRow))]
        private void MoveUp()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugMoveUp))
            { messageBox.ShowError("权限不足"); return; }
            if (IsGroupScope) { MoveGroup(-1); return; }
            MoveRow(-1);
        }

        [RelayCommand(CanExecute = nameof(CanMoveRow))]
        private void MoveDown()
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugMoveDown))
            { messageBox.ShowError("权限不足"); return; }
            if (IsGroupScope) { MoveGroup(1); return; }
            MoveRow(1);
        }

        /// <summary>通用默认名尾段：给定占用名集合与前缀，从 数量+1 起递增生成不重名（原两方法 do-while 合并）</summary>
        private static string NextDefaultName(ICollection<string> occupied, string prefix)
        {
            int seq = occupied.Count + 1;
            string name;
            do
            {
                name = prefix + seq++;
            }
            while (occupied.Contains(name));
            return name;
        }

        #region 组级行操作（组 DGV 工作集 GroupRows：新增 / 删除 / 移动 / 改名 / 即时重名检查）

        /// <summary>组级·新增组：新建空组草稿行（GroupID=全表最大+1，默认名 组N 不重名；空组落盘即丢警告在保存侧）。
        /// ⚠️ 不抢占选中（保留当前组行）：新组行需用户点击切换当前组——走 View 切组脏拦截（当前组标签脏先确认），防静默丢弃工作集。</summary>
        private void AddGroupCore()
        {
            if (SelectedDevice == null) return;
            int maxId = 0;
            if (GroupRows.Count > 0)
                maxId = GroupRows.Max(g => g.GroupID);
            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (table != null && table.Count > 0)
            {
                int tableMax = table.Values.Max(g => g.GroupID);
                if (tableMax > maxId) maxId = tableMax;
            }

            var row = TagGroupRow.CreateNew(maxId + 1, NewGroupName());
            GroupRows.Add(row);
            // 无当前组时才自动选中新行（有当前组则留给用户点击，防绕过脏拦截）
            if (SelectedGroupRow == null)
                SelectedGroupRow = row;
            RaiseDirtyRelated();
        }

        /// <summary>生成全表（工作集 + 组表）不重名的默认组名（组1/组2...）；占用收集后交 NextDefaultName 取名</summary>
        private string NewGroupName()
        {
            var taken = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in GroupRows)
            {
                if (!string.IsNullOrEmpty(r.GroupName))
                    taken.Add(r.GroupName.Trim());
            }
            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (table != null)
            {
                foreach (var k in table.Keys)
                    taken.Add(k);
            }
            return NextDefaultName(taken, "组");
        }

        /// <summary>组级·删除组：存量组记删除清单 + 移除行 + 序号收紧（镜像标签侧）；新增组（IsNew）直接弃。
        /// ⚠️ 删除当前组（SelectedGroupRow 即当前选中组）→ 工作集该组标签全体作废（SelectedGroupName=null 联动清空标签 DGV）。</summary>
        private void RemoveGroupCore()
        {
            var row = SelectedGroupRow;
            if (row == null || SelectedDevice == null) return;

            string display = string.IsNullOrWhiteSpace(row.GroupName) ? "(未命名)" : row.GroupName;
            if (!messageBox.ShowAsk($"确认删除组 [{display}]？\n组内 {row.ChildCount} 个标签将一并删除。"))
                return;

            if (row.IsNew)
            {
                GroupRows.Remove(row); // 草稿组直接丢弃（组表本无此组，序号不占位无需收紧）
            }
            else
            {
                // 存量组：记组表键 + 移除 + 序号连续收紧——组间序号保持 1..N 无空洞（镜像标签侧 RemoveTagRowCore）：
                // 被删行之后（GroupID 更大）的组各 -1 补位，非草稿置脏随本单保存一起提交
                // （例：删组6 → 原组7 收紧为 6；否则组表/Excel 落盘留洞跳号）
                _removedGroupNames.Add(row.SourceGroupName);
                int removedSort = row.GroupID;
                GroupRows.Remove(row);
                for (int i = 0; i < GroupRows.Count; i++)
                {
                    var r = GroupRows[i];
                    if (r.GroupID <= removedSort) continue;
                    r.GroupID -= 1;
                    if (!r.IsNew) r.IsDirty = r.HasRealChanges(); // HasRealChanges 含 GroupID → 收紧即置脏 → UpdateTagGroup 落盘
                    GroupRows.ResetItem(i); // 序号列直绑已改值，行级刷新序号文本（按钮事件在编辑管线外，ResetItem 安全）
                }
            }

            if (ReferenceEquals(SelectedGroupRow, row))
                SelectedGroupRow = null; // 钩子联动 SelectedGroupName=null → 标签工作集清空（组没了，标签无家可归）
            RaiseDirtyRelated();
        }

        /// <summary>
        /// 组级·上移/下移：交换相邻组行 GroupID + 集合内移动（组间序号=GroupID，展示序=GroupRows 序，落盘按 GroupID）。
        /// 双方置脏（序号交换 = 未保存修改）；驱动在保存前仍按旧序号跑
        /// </summary>
        private void MoveGroup(int offset)
        {
            var row = SelectedGroupRow;
            if (row == null || GroupRows.Count < 2) return;
            int idx = GroupRows.IndexOf(row);
            int target = idx + offset;
            if (idx < 0 || target < 0 || target >= GroupRows.Count) return;

            var neighbour = GroupRows[target];
            int sort = row.GroupID;
            row.GroupID = neighbour.GroupID;
            neighbour.GroupID = sort;

            GroupRows.RemoveAt(idx);
            GroupRows.Insert(target, row);
            // 差异置脏：交换后 GroupID 与源一致（上移又移回原位）自动恢复非脏
            if (!row.IsNew) row.IsDirty = row.HasRealChanges();
            if (!neighbour.IsNew) neighbour.IsDirty = neighbour.HasRealChanges();
            SelectedGroupRow = row;
            RaiseDirtyRelated();
        }

        /// <summary>
        /// 组改名（组 DGV GroupName 列编辑提交后由 View 延迟调用；row.GroupName 已被直绑写为新值）：
        /// 只置脏不改组表——CD Key 迁移统一移交保存 ApplyGroupEdits（先挂后摘）；
        /// 即时校验空名/重名（工作集 + 组表现存键，自身旧键除外），冲突回滚行副本并提示。
        /// 当前组改名：SelectedGroupRow 仍在 → SelectedGroupName 保持旧键（组表未动），标签 DGV 仍显示旧组，保存时随 commitGroupKey 迁移。
        /// </summary>
        public void RenameGroup(TagGroupRow row, string oldName)
        {
            if (row == null || !GroupRows.Contains(row) || SelectedDevice == null) return;
            if (string.IsNullOrEmpty(oldName)) return;

            string newName = row.GroupName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(newName))
            {
                row.GroupName = oldName;
                if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 回滚后无真实差异 → 清假脏
                ResetItemIfPresent(GroupRows, row);
                RaiseDirtyRelated();
                messageBox.ShowError("组名不能为空");
                return;
            }
            row.GroupName = newName; // 规范化（去首尾空格）
            if (string.Equals(newName, oldName, StringComparison.Ordinal))
                return; // 仅空格差异，无改名

            if (IsGroupNameTaken(row, newName))
            {
                row.GroupName = oldName;
                if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 回滚后清假脏
                ResetItemIfPresent(GroupRows, row);
                RaiseDirtyRelated();
                messageBox.ShowError($"已存在同名组 [{newName}]，改名失败");
                return;
            }

            if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 成功改名：恒真置脏
            RaiseDirtyRelated();
        }

        /// <summary>组改名即时重名检查：工作集其它组行 + 组表现存键（自身旧键 SourceGroupName 除外——改回原名场景）</summary>
        private bool IsGroupNameTaken(TagGroupRow self, string name)
        {
            foreach (var r in GroupRows)
            {
                if (!ReferenceEquals(r, self)
                    && string.Equals(r.GroupName, name, StringComparison.Ordinal))
                    return true;
            }

            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (table != null && table.ContainsKey(name)
                && !string.Equals(name, self.SourceGroupName, StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        #endregion

        #region 子级行操作（当前组标签工作集 TagRows：新增 / 删除 / 移动 / 序号编辑 / 改名 / 即时重名检查）

        /// <summary>子级·新增标签行（原 AddRow 主体）</summary>
        private void AddTagRowCore()
        {
            if (SelectedDevice == null || string.IsNullOrEmpty(SelectedGroupName)) return;

            int maxSort = 0;
            if (TagRows.Count > 0)
                maxSort = TagRows.Max(r => r.TagID);
            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (table != null && table.TryGetValue(SelectedGroupName, out var group) && group.Tags.Values.Count > 0)
            {
                int tableMax = group.Tags.Values.Max(t => t.TagID);
                if (tableMax > maxSort) maxSort = tableMax;
            }

            var row = TagItemRow.CreateNew(maxSort + 1, NewTagName());
            TagRows.Add(row);
            SelectedRow = row; // 选中新行便于直接编辑（IsNew 禁读/写/设频率——草稿）
            RaiseDirtyRelated();
        }

        /// <summary>生成组内（工作集 + 组表）不重名的默认标签名（Tag1/Tag2...）；占用收集后交 NextDefaultName 取名</summary>
        private string NewTagName()
        {
            var taken = new HashSet<string>(StringComparer.Ordinal);
            foreach (var r in TagRows)
            {
                if (!string.IsNullOrEmpty(r.TagName))
                    taken.Add(r.TagName.Trim());
            }
            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (table != null && table.TryGetValue(SelectedGroupName, out var group))
            {
                foreach (var k in group.Tags.Keys)
                    taken.Add(k);
            }
            return NextDefaultName(taken, "Tag");
        }

        /// <summary>子级·删除标签行（原 RemoveRow 主体）</summary>
        private void RemoveTagRowCore()
        {
            var row = SelectedRow;
            if (row == null || SelectedDevice == null || string.IsNullOrEmpty(SelectedGroupName)) return;

            string display = string.IsNullOrWhiteSpace(row.TagName) ? "(未命名)" : row.TagName;
            if (!messageBox.ShowAsk($"确认删除标签 [{display}]？"))
                return;

            if (row.IsNew)
            {
                TagRows.Remove(row); // 草稿行直接丢弃（组表本无此标签，序号不占位无需收紧）
            }
            else
            {
                // 存量行：记组表键 + 移除 + 序号连续收紧——组内序号保持 1..N 无空洞：
                // 被删行之后（TagID 更大）的行各 -1 补位，非草稿置脏随本单保存一起提交
                // （例：30 与 31 互换后删 30，原 31 应收紧回 30；否则组表/Excel 落盘留洞跳号）
                _removedTagNames.Add(row.SourceTagName);
                int removedSort = row.TagID;
                TagRows.Remove(row);
                for (int i = 0; i < TagRows.Count; i++)
                {
                    var r = TagRows[i];
                    if (r.TagID <= removedSort) continue;
                    r.TagID -= 1;
                    if (!r.IsNew) r.IsDirty = r.HasRealChanges();
                    TagRows.ResetItem(i); // 编辑列直绑已改值，行级刷新序号文本（按钮事件在编辑管线外，ResetItem 安全）
                }
            }

            if (ReferenceEquals(SelectedRow, row))
                SelectedRow = null; // 先清防悬空；DGV 会自动选邻行（SelectionChanged 驱动）
            RaiseDirtyRelated();
        }

        /// <summary>
        /// 子级·上移/下移：交换相邻行 TagID + 集合内移动（行对象稳定，按钮点击时不在编辑管线，直接操作安全）
        /// 双方置脏（序号交换 = 未保存修改）；驱动在保存前仍按旧序号跑
        /// </summary>
        private void MoveRow(int offset)
        {
            var row = SelectedRow;
            if (row == null || TagRows.Count < 2) return;
            int idx = TagRows.IndexOf(row);
            int target = idx + offset;
            if (idx < 0 || target < 0 || target >= TagRows.Count) return;

            var neighbour = TagRows[target];
            int sort = row.TagID;
            row.TagID = neighbour.TagID;
            neighbour.TagID = sort;

            TagRows.RemoveAt(idx);
            TagRows.Insert(target, row);
            // 差异置脏：交换后与源一致（上移又移回原位）自动恢复非脏
            if (!row.IsNew) row.IsDirty = row.HasRealChanges();
            if (!neighbour.IsNew) neighbour.IsDirty = neighbour.HasRealChanges(); // 邻居序号被交换同样未保存
            SelectedRow = row;
            RaiseDirtyRelated();
        }

        /// <summary>
        /// 标签序号列编辑提交（TagID 已由直绑写回；View CellEndEdit 延迟调用）：
        /// 置脏 + 按新序号在集合内重排（行对象稳定直接移动；编辑管线结束后执行，安全）
        /// </summary>
        public void OnTagIdEdited(TagItemRow row)
        {
            if (row == null || !TagRows.Contains(row)) return;
            if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 手编回原值自动清脏

            // 按 TagID 升序找插入位（稳定：跳过自身后首个大于者之前；同序号保持当前相对序）
            TagRows.Remove(row);
            int insertAt = TagRows.Count;
            for (int i = 0; i < TagRows.Count; i++)
            {
                if (TagRows[i].TagID > row.TagID)
                {
                    insertAt = i;
                    break;
                }
            }
            TagRows.Insert(insertAt, row);
            SelectedRow = row;
            RaiseDirtyRelated();
        }

        /// <summary>
        /// 标签改名（TagName 列编辑提交后由 View 延迟调用；row.TagName 已被直绑写为新值）：
        /// 只置脏不改组表——键迁移统一移交保存 ApplyEdits（先挂后摘防队列残留旧名请求丢写）；
        /// 即时校验空名/重名（工作集 + 组表），冲突回滚行副本并提示
        /// </summary>
        public void RenameTag(TagItemRow row, string oldName)
        {
            if (row == null || !TagRows.Contains(row) || SelectedDevice == null) return;
            if (string.IsNullOrEmpty(oldName)) return;

            string newName = row.TagName?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(newName))
            {
                row.TagName = oldName;
                if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 回滚后无真实差异 → 清 CellValueChanged 先行置的假脏
                ResetItemIfPresent(TagRows, row);
                RaiseDirtyRelated();
                messageBox.ShowError("标签名不能为空");
                return;
            }
            row.TagName = newName; // 规范化（去首尾空格）
            if (string.Equals(newName, oldName, StringComparison.Ordinal))
                return; // 仅空格差异，无改名

            if (IsTagNameTaken(row, newName))
            {
                row.TagName = oldName;
                if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 同空名：回滚后清假脏（改回原名 = 无差异）
                ResetItemIfPresent(TagRows, row);
                RaiseDirtyRelated();
                messageBox.ShowError($"组内已存在同名标签 [{newName}]，改名失败");
                return;
            }

            if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 成功改名：TagName 已变恒真；统一差异判定入口
            RaiseDirtyRelated();
        }

        /// <summary>
        /// 改名即时重名检查：工作集其它行 + 组表现存键（自身旧键 SourceTagName 除外——改回原名场景）；
        /// 保存预检（ValidatePending 占用集）为最终防线
        /// </summary>
        private bool IsTagNameTaken(TagItemRow self, string name)
        {
            foreach (var r in TagRows)
            {
                if (!ReferenceEquals(r, self)
                    && string.Equals(r.TagName, name, StringComparison.Ordinal))
                    return true;
            }

            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (table != null && table.TryGetValue(SelectedGroupName, out var group)
                && group.Tags.ContainsKey(name)
                && !string.Equals(name, self.SourceTagName, StringComparison.Ordinal))
            {
                return true;
            }
            return false;
        }

        #endregion

        #endregion

        #region 刷新频率（设置当前 = 选中行标签；设置所有 = 当前组全部标签）

        [RelayCommand(CanExecute = nameof(CanSetCurrent))]
        private void SetCurrentFreq(string freqText)
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugSetCurrentFreq))
            { messageBox.ShowError("权限不足"); return; }
            var row = SelectedRow;
            if (row == null) return; // CanSetCurrent 已禁草稿行（IsNew/IsDirty）

            if (!int.TryParse(freqText?.Trim(), out int freq) || freq < -1)
            {
                messageBox.ShowError("请输入有效的刷新频率（正整数，毫秒）");
                return;
            }

            row.RefreshFreq = freq;
            if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // 频率属配置字段；设成与原值相同自动不脏
            RaiseDirtyRelated();
            ResetItemIfPresent(TagRows, row); // 行色即时变（明黄）——按钮事件在编辑管线外，ResetItem 安全
        }

        [RelayCommand(CanExecute = nameof(CanSetAll))]
        private void SetAllFreq(string freqText)
        {
            if (!authService.HasPermission(PermissionCodes.BtnPLCDebugSetAllFreq))
            { messageBox.ShowError("权限不足"); return; }
            if (SelectedDevice == null || string.IsNullOrEmpty(SelectedGroupName)) return;

            if (!int.TryParse(freqText?.Trim(), out int freq) || freq <-1)
            {
                messageBox.ShowError("请输入有效的刷新频率（正整数，毫秒）");
                return;
            }

            foreach (var row in TagRows)
            {
                row.RefreshFreq = freq;
                if (!row.IsNew) row.IsDirty = row.HasRealChanges(); // IsNew 行保存时随 EnsureSource 落新值
            }

            RaiseDirtyRelated();
            for (int i = 0; i < TagRows.Count; i++)
                TagRows.ResetItem(i); // 全行行色即时更新
        }

        #endregion

        #region 未保存确认与保存三段式

        /// <summary>
        /// 未保存修改确认（切实例/导入拦截）：无脏直接放行；有脏先问「是否先保存」——
        /// 保存成功（内存提交）放行 / 保存失败中止；不保存再问「是否放弃」——放弃放行 / 否则中止。
        /// 两问组合覆盖"保存/放弃/取消"三选（IMessageBoxService 无三按钮 API，不改 FormMain 库）。
        /// </summary>
        /// <returns>true=允许离开当前实例/放弃当前会话上下文</returns>
        public bool ConfirmDiscardPending()
        {
            if (!HasPendingChanges)
                return true;

            bool saveFirst = messageBox.ShowAsk($"当前地址表有 {PendingCount} 处未保存修改，是否先保存？");
            if (saveFirst)
            {
                // 保存（含预检；预检失败返回 false = 中止离开；落盘失败仅告警但内存已提交 → 放行）
                return TrySaveAll();
            }

            return messageBox.ShowAsk("确定放弃未保存的修改？");
        }

        /// <summary>
        /// 切组（组 DGV 换行/切实例入口 View 拦截）专用确认：只拦"当前组标签工作集"脏——
        /// 组工作集跨组保留（GroupRows 全组快照不随切组重建），切组丢的只有当前组标签编辑。
        /// 放弃 = 仅 RebuildTagRows（标签还原）；组编辑仍在工作集（下轮保存/取消统一处理）。
        /// </summary>
        /// <returns>true=允许切换当前组</returns>
        public bool ConfirmDiscardTagScope()
        {
            if (!HasPendingTagChanges)
                return true;

            bool saveFirst = messageBox.ShowAsk($"当前组标签有 {PendingTagCount} 处未保存修改，是否先保存？");
            if (saveFirst)
            {
                return TrySaveAll();
            }

            if (!messageBox.ShowAsk("确定放弃当前组标签的未保存修改？"))
                return false;
            RebuildTagRows(); // 标签工作集从组表还原（组编辑保留——跨组不丢）
            return true;
        }

        /// <summary>
        /// 保存三段式（SaveTagConfigCommand / 确认离开共用）——双工作集提交（组 GroupRows + 当前组标签 TagRows 一次落盘）：
        /// ① 预检（组预检 ValidateGroups + 标签预检 ValidatePending，整单拒存弹窗定位）
        /// → ② ApplyGroupEdits（组删除/改名/属性/新增，UpdateTagGroup/AddTagGroup/RemoveTagGroup）
        /// → ③ ApplyEdits（当前组标签行 + 删除清单，提交组名=当前组改名后的新键）
        /// → ④ 落盘（.bak+覆盖，失败告警不回滚）
        /// 成功收敛：清脏态 → 重快照（组工作集 + 标签工作集，组表为最新 UI 对齐含排序）→ 恢复选中
        /// </summary>
        private bool TrySaveAll()
        {
            if (SelectedDevice == null)
                return true; // 无实例无可存

            string plcName = SelectedDevice.Name;

            // ① 预检：组工作集 + 当前组标签工作集
            string error = ValidateGroups();
            if (error != null)
            {
                messageBox.ShowError(error);
                return false; // 整单拒存：保持 dirty 可改
            }

            // 组预检通过后取提交组名：当前组若被改名（SelectedGroupRow.GroupName≠组表旧键）→ 标签提交/落盘用新键；
            // 标签预检必须用组表当前态键（SelectedGroupName）——组提交尚未执行，组表仍挂旧键
            string commitGroupKey = SelectedGroupName;
            var curGroupRow = SelectedGroupRow;
            bool curGroupRenamed = curGroupRow != null && !curGroupRow.IsNew
                && !string.IsNullOrEmpty(curGroupRow.GroupName?.Trim())
                && !string.Equals(SelectedGroupName, curGroupRow.GroupName.Trim(), StringComparison.Ordinal);
            if (curGroupRenamed)
                commitGroupKey = curGroupRow.GroupName.Trim(); // 当前组改名：提交目标随新键

            // 标签工作集需提交 = 有当前组 且（有标签行 或 有删除清单）——删除清单独立于标签行，不可被 TagRows 空门禁吞掉。
            // （2026-09-10 修复：删空组内标签时 _removedTagNames 残留未被提交/清空 → 脏计数恒 >0 →
            //  ReloadAfterSave 重绑组 DGV 触发切组确认 → "是否先保存"死循环 + 删除不生效）
            bool needTagCommit = !string.IsNullOrEmpty(SelectedGroupName)
                && (TagRows.Count > 0 || _removedTagNames.Count > 0);
            if (needTagCommit)
            {
                error = ValidatePending(SelectedGroupName); // 预检用旧键（组表此刻仍挂旧键）
                if (error != null)
                {
                    messageBox.ShowError(error);
                    return false;
                }
            }

            // ①.5 审计明细基线：必须在组/标签提交之前采集——ApplyGroupEdits 会清 _removedGroupNames，
            //      ApplyEdits 会清 _removedTagNames 并 CommitState 清脏态，提交后这些"操作者意图"就再也读不出来
            var auditChanges = BuildTagAuditChanges(commitGroupKey);

            // ② 组提交（删除 → 存量改名/属性 → 新增；组表先行——标签 IsNew 行随后 AddTag 用新键）
            try
            {
                ApplyGroupEdits(plcName);
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"组提交失败（未保存，可修正后重试）: {ex.Message}");
                return false;
            }

            // ③ 当前组标签提交（仅当组仍存在且需提交：有标签行或删除清单）
            if (needTagCommit)
            {
                try
                {
                    ApplyEdits(plcName, commitGroupKey);
                }
                catch (Exception ex)
                {
                    messageBox.ShowError($"标签提交失败（未保存，可修正后重试）: {ex.Message}");
                    return false;
                }
            }

            // ④ 落盘（内存已生效；失败仅告警——重启将丢失，已提交部分不回滚）
            try
            {
                if (!configManager.SaveTagPollingConfigToConfiguredFile(plcName))
                    messageBox.ShowError($"[{plcName}] 修改已生效，但文件保存失败（未配置标签文件/IO 异常），重启后将丢失");
            }
            catch (Exception ex)
            {
                messageBox.ShowError($"[{plcName}] 修改已生效，但文件保存异常: {ex.Message}，重启后将丢失");
            }

            // ⑤ 收敛：组表 = 工作集 → 组工作集重快照（含排序）+ 标签工作集重快照（组表为最新），恢复组选中
            string keepGroup = commitGroupKey; // 组提交后键（组删除了则空/找不到）
            ReloadAfterSave(keepGroup);
            // 保存 = 会话终结点（v3）：成功即退出编辑态。导入后无当前组时 ③ 跳过仅 ②④ 实例级组落盘
            // 固化导入内容，同样退编辑态；切组两问"先保存"同走此路径 → 提交后回浏览态（保存即退出，钉死）
            Session = PlcDebugSession.None;
            RaiseDirtyRelated();

            // 留痕：内存提交 + 落盘路径走完才投递（落盘失败属告警级、不回滚已提交的内存部分，故同样记账）
            auditService.Record(new AuditEntry(AuditCategory.Device, AuditAction.Save,
                AuditTargetName(plcName), userSession.CurrentUser.ToAuditActor()) { Changes = auditChanges });
            return true;
        }

        /// <summary>
        /// 组工作集保存预检：组名非空 + 全表组名占用集查重（新增/改名 vs 组表现存键 − 删除清单）。
        /// 删除当前组边界已在 RemoveGroupCore 处理（SelectedGroupRow=null → 标签工作集清空）。
        /// </summary>
        private string ValidateGroups()
        {
            var table = configManager.GetTagTable(SelectedDevice.Name);
            if (GroupRows.Count == 0 && _removedGroupNames.Count == 0)
                return null;

            var remaining = new HashSet<string>(StringComparer.Ordinal);
            if (table != null)
            {
                foreach (var k in table.Keys)
                    remaining.Add(k);
            }
            foreach (var n in _removedGroupNames)
                remaining.Remove(n);

            var occupy = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in GroupRows)
            {
                string name = (row.GroupName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                    return "组名不能为空（请检查修改/新增组行）";
                if (!occupy.Add(name))
                    return $"组名 [{name}] 重复（与另一新增/改名组冲突），请改名后保存";
            }

            // 与存量键比对：仅 新增行 / 改名行 的名字不得撞 remaining（未变化存量行的名字与 remaining
            // 重叠是天然合法——它就是那个组）；remaining 已扣除 _removedGroupNames（删掉的组名可复用）
            foreach (var row in GroupRows)
            {
                string name = (row.GroupName ?? string.Empty).Trim();
                if (row.IsNew || !string.Equals(row.SourceGroupName, name, StringComparison.Ordinal))
                {
                    if (remaining.Contains(name))
                        return $"组名 [{name}] 与现有组重复，请改名后保存";
                }
            }
            return null;
        }

        /// <summary>
        /// 组提交执行（预检已过）：固定顺序——① 删除（RemoveTagGroup）→ ② 存量组脏/改名 → UpdateTagGroup
        /// （整组替换含改名先挂后摘 + 组内 TagItem.GroupName 同步）→ ③ 新增组 AddTagGroup。
        /// 提交后行 Source 引用被驱动替换为新组对象 → 由 ReloadAfterSave 重快照（本方法不改行状态）。
        /// </summary>
        private void ApplyGroupEdits(string plcName)
        {
            // ① 删除
            foreach (var oldKey in _removedGroupNames)
                configManager.RemoveTagGroup(plcName, oldKey);
            _removedGroupNames.Clear();

            // ② 存量组：脏或改名 → 整组替换（GroupID 调整/描述/改名全部经 UpdateTagGroup 提交）
            foreach (var row in GroupRows)
            {
                if (row.IsNew) continue;
                string oldKey = row.SourceGroupName;
                bool renamed = !string.Equals(oldKey, row.GroupName?.Trim(), StringComparison.Ordinal);
                if (!row.IsDirty && !renamed)
                    continue; // 无变化组行跳过（GroupID 交换已置脏走差异判定）

                if (!configManager.UpdateTagGroup(plcName, oldKey, row.BuildForSubmit()))
                    throw new InvalidOperationException($"组 [{oldKey}] 更新失败（组不存在或新组名被占用）");
            }

            // ③ 新增组（空组落盘即丢——Excel 无行承载，现场确认后接受；保存成功提示含此语义）
            foreach (var row in GroupRows)
            {
                if (!row.IsNew) continue;
                configManager.AddTagGroup(plcName, row.GroupName.Trim(), row.BuildForSubmit());
            }
        }

        /// <summary>
        /// 保存/删除组后收敛：组工作集 + 标签工作集全重快照（组表为最新），恢复组选中。
        /// 标签工作集显式无条件重快照——不依赖 SelectedGroupName 变化钩子（同名保存不触发 OnSelectedGroupNameChanged，
        /// 依赖钩子副作用会残留被删标签行/旧排序）；重绑后 View 统一清选（只展示不自动选中，2026-09-09 拍板）。
        /// </summary>
        private void ReloadAfterSave(string keepGroupKey)
        {
            LoadGroupRows(SelectedDevice.Name); // 组工作集重快照（含新排序；组行对象全换）
            // 恢复组选中：提交后的组键（改名后=新键；找不到=组被删/新增组不自动选中）
            TagGroupRow groupTarget = null;
            if (!string.IsNullOrEmpty(keepGroupKey))
                groupTarget = GroupRows.FirstOrDefault(g => string.Equals(g.SourceGroupName, keepGroupKey, StringComparison.Ordinal));
            SelectedGroupRow = groupTarget; // 触发钩子：SelectedGroupName=其键（键变化时连带重绑一次，无害）
            if (groupTarget == null)
                SelectedGroupName = null;

            RebuildTagRows(); // 标签工作集显式重快照（防同名保存漏刷）
        }

        /// <summary>
        /// 保存预检：必填校验 + 占用集查重（冲突整单拒存）。
        /// 占用规则：释放集 = 改名行旧键；占用集 = IsNew 行键 ∪ 改名行目标键；
        /// 冲突 = 占用集内部重复（两行争一名）或占用集 ∩ (组表现存键 − 释放集 − 删除清单) 非空。
        /// 先收集再比对 → 改名互换（A→B 且 B→A）不误判。
        /// </summary>
        private string ValidatePending(string groupName)
        {
            var table = configManager.GetTagTable(SelectedDevice.Name);
            var group = table != null && table.TryGetValue(groupName, out var g) ? g : null;
            if (group == null && (TagRows.Count > 0 || _removedTagNames.Count > 0))
                return $"组 [{groupName}] 不存在（可能已被移除），无法保存";

            // 组表现存键（基础占用）− 删除清单
            var remaining = new HashSet<string>(StringComparer.Ordinal);
            if (group != null)
            {
                foreach (var k in group.Tags.Keys)
                    remaining.Add(k);
            }
            foreach (var n in _removedTagNames)
                remaining.Remove(n);

            // 收集占用/释放 + 必填校验
            var occupy = new HashSet<string>(StringComparer.Ordinal);
            var release = new HashSet<string>(StringComparer.Ordinal);
            foreach (var row in TagRows)
            {
                string name = (row.TagName ?? string.Empty).Trim();
                if (string.IsNullOrEmpty(name))
                    return "标签名不能为空（请检查修改/新增行）";
                if (string.IsNullOrWhiteSpace(row.Address))
                    return $"标签 [{name}] 地址不能为空";
                // 长度下界按数据类型分档：String 下界 0 —— 0 是否合法、什么含义由协议解释（配置层刻意不做协议感知校验，
                // 协议不支持时由协议层报错，如 CIP 的 String 拒绝 ≤0）；其余类型的 Length 是数组元素个数，必须 ≥ 1
                if (row.DataType == PlcDataType.String ? row.Length < 0 : row.Length < 1)
                    return $"标签 [{name}] 长度非法：String 不能为负数，其余类型必须 ≥ 1";
                if (row.RefreshFreq < -1)
                    return $"标签 [{name}] 刷新频率非法（-1=不刷新 或正整数毫秒）";

                if (row.IsNew)
                {
                    if (!occupy.Add(name))
                        return $"标签名 [{name}] 重复（与另一新增/改名行冲突），请改名后保存";
                }
                else if (!string.Equals(name, row.SourceTagName, StringComparison.Ordinal))
                {
                    if (!string.IsNullOrEmpty(row.SourceTagName))
                        release.Add(row.SourceTagName);
                    if (!occupy.Add(name))
                        return $"改名冲突: [{row.SourceTagName}] → [{name}]（与另一新增/改名行重复）";
                }
            }

            // 占用集与"未被释放的现存键"比对
            foreach (var n in release)
                remaining.Remove(n);
            foreach (var n in occupy)
            {
                if (remaining.Contains(n))
                    return $"标签名 [{n}] 与组内现有标签重复，请改名后保存";
            }
            return null;
        }

        /// <summary>
        /// 提交执行（预检已过，理论不失败）：固定顺序——
        /// ① 删除（先删后增：防同名重建时新加行被误删）→ ② 存量行属性回写 + 改名键迁移（先挂后摘：AddTag 新键挂同实例 → RemoveTag 旧键，
        ///    队列残留旧名请求仍命中同实例，零丢写）→ ③ IsNew 行 AddTag → ④ 行状态收敛（CommitState）
        /// </summary>
        private void ApplyEdits(string plcName, string groupName)
        {
            var table = configManager.GetTagTable(plcName);
            if (table == null || !table.TryGetValue(groupName, out var group))
                throw new InvalidOperationException($"组 [{groupName}] 不存在");

            // ① 删除
            foreach (var oldName in _removedTagNames)
                configManager.RemoveTag(plcName, groupName, oldName);
            _removedTagNames.Clear();

            // ② 存量行：脏或改名 → 回写 + 键迁移
            foreach (var row in TagRows)
            {
                if (row.IsNew) continue;
                string oldKey = row.SourceTagName;
                bool renamed = !string.Equals(oldKey, row.TagName?.Trim(), StringComparison.Ordinal);
                if (!row.IsDirty && !renamed)
                    continue; // 无变化行跳过（展示列快照只读 Source，编辑列未动）

                row.ApplyToSource(); // Source.TagName 已同步新名（若改名）
                if (renamed)
                {
                    configManager.AddTag(plcName, groupName, row.Source); // 先挂（新键）
                    configManager.RemoveTag(plcName, groupName, oldKey);  // 后摘（旧键）
                }
            }

            // ③ 新增行
            foreach (var row in TagRows)
            {
                if (!row.IsNew) continue;
                configManager.AddTag(plcName, groupName, row.EnsureSource(groupName));
            }

            // ④ 收敛（行对象复用：清态 + 锚点刷新）
            foreach (var row in TagRows)
                row.CommitState();
        }

        #endregion

        #region 审计留痕（地址表明细组装）

        /// <summary>
        /// 地址表保存的字段级明细（**必须在 ApplyGroupEdits 之前调用**——提交会清删除清单与脏态）。
        ///
        /// 走"操作者意图"而非整表 diff：工作集里现成带着 IsNew / HasRealChanges() / 删除清单，
        /// 上千标签的地址表只改 3 处就只记 3 条明细，绝不整表比对。
        ///
        /// 路径规则（与 AuditFieldChange.FieldPath 注释一致）：
        /// - 组级：`组名`（新增/删除整组）、`组名.组名|组描述|组序号`（改组属性）
        /// - 标签级：`组名.标签名`（新增/删除整个标签）、`组名.标签名.字段`（改标签属性）
        /// </summary>
        /// <param name="commitGroupKey">标签提交用的组表键（当前组改名后的新键）</param>
        private List<AuditFieldChange> BuildTagAuditChanges(string commitGroupKey)
        {
            var changes = new List<AuditFieldChange>();

            // ── 组级：删除 → 存量改名/改属性 → 新增（与 ApplyGroupEdits 同序，读起来就是执行序）──
            foreach (var removedName in _removedGroupNames)
                changes.Add(NewAuditChange(removedName, AuditChangeType.Delete, null, null));

            foreach (var row in GroupRows)
            {
                string newName = (row.GroupName ?? string.Empty).Trim();
                if (row.IsNew)
                {
                    changes.Add(NewAuditChange(newName, AuditChangeType.Add, null, null));
                    continue;
                }
                if (!row.HasRealChanges()) continue;

                // 作用域取入集旧键：改名后仍能定位到"改之前是哪个组"
                string scope = row.SourceGroupName ?? string.Empty;
                AddAuditDiff(scope + ".组序号", row.Source.GroupID, row.GroupID, changes);
                AddAuditDiff(scope + ".组名", row.Source.GroupName ?? string.Empty, newName, changes);
                AddAuditDiff(scope + ".组描述", row.Source.GroupDesc ?? string.Empty, row.GroupDesc ?? string.Empty, changes);
            }

            // ── 标签级（只覆盖本次提交范围内的当前组）──
            string groupKey = string.IsNullOrWhiteSpace(commitGroupKey)
                ? (SelectedGroupName ?? string.Empty)
                : commitGroupKey;

            foreach (var removedName in _removedTagNames)
                changes.Add(NewAuditChange(JoinTagAuditPath(groupKey, removedName),
                    AuditChangeType.Delete, null, null));

            foreach (var row in TagRows)
            {
                string tagName = (row.TagName ?? string.Empty).Trim();
                if (row.IsNew)
                {
                    changes.Add(NewAuditChange(JoinTagAuditPath(groupKey, tagName),
                        AuditChangeType.Add, null, DescribeTagRow(row)));
                    continue;
                }
                if (!row.HasRealChanges()) continue;

                string scope = JoinTagAuditPath(groupKey, row.SourceTagName);
                AddAuditDiff(scope + ".序号", row.Source.TagID, row.TagID, changes);
                AddAuditDiff(scope + ".标签名", row.Source.TagName ?? string.Empty, tagName, changes);
                AddAuditDiff(scope + ".地址", row.Source.Address ?? string.Empty, row.Address ?? string.Empty, changes);
                AddAuditDiff(scope + ".数据类型", row.Source.DataType, row.DataType, changes);
                AddAuditDiff(scope + ".长度", row.Source.Length, row.Length, changes);
                AddAuditDiff(scope + ".刷新频率", row.Source.RefreshFreq, row.RefreshFreq, changes);
                AddAuditDiff(scope + ".标签描述", row.Source.TagDesc ?? string.Empty, row.TagDesc ?? string.Empty, changes);
            }

            return changes;
        }

        /// <summary>标签路径拼接（组.标签；组名为空时只留标签名，避免出现 ".标签" 这种半截路径）。</summary>
        private static string JoinTagAuditPath(string groupName, string tagName)
        {
            string tag = string.IsNullOrWhiteSpace(tagName) ? "(未命名标签)" : tagName;
            return string.IsNullOrWhiteSpace(groupName) ? tag : groupName + "." + tag;
        }

        /// <summary>新增标签的可读描述（地址 / 类型 / 长度 / 刷新），让"新增的是什么"一眼可读。</summary>
        private static string DescribeTagRow(TagItemRow row)
        {
            string address = string.IsNullOrWhiteSpace(row.Address) ? "(空地址)" : row.Address;
            string freq = row.RefreshFreq < 0
                ? "不刷新"
                : "刷新" + row.RefreshFreq.ToString(CultureInfo.InvariantCulture);

            return string.Format(CultureInfo.InvariantCulture, "{0} / {1} / 长度{2} / {3}",
                address, row.DataType, row.Length, freq);
        }

        private static AuditFieldChange NewAuditChange(string fieldPath, AuditChangeType changeType,
            string oldValue, string newValue)
        {
            return new AuditFieldChange
            {
                FieldPath = fieldPath,
                ChangeType = changeType,
                OldValue = oldValue,
                NewValue = newValue
            };
        }

        /// <summary>字段级差异落一条（值相同不落——与 row.HasRealChanges 同判据，防止出现"零差异的明细"）。</summary>
        private static void AddAuditDiff<T>(string fieldPath, T oldValue, T newValue,
            List<AuditFieldChange> changes)
        {
            if (EqualityComparer<T>.Default.Equals(oldValue, newValue)) return;

            changes.Add(NewAuditChange(fieldPath, AuditChangeType.Update,
                Convert.ToString(oldValue, CultureInfo.InvariantCulture),
                Convert.ToString(newValue, CultureInfo.InvariantCulture)));
        }

        #endregion
    }
}
