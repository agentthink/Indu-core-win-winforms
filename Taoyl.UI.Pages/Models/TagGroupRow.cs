using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using Taoyl.Application.PlcDriver.Models;

namespace Taoyl.UI.Pages.Models
{
    /// <summary>
    /// 地址表组工作行（UI 工作集模型，纯 POCO 无 INotify——仿 TagItemRow）
    ///
    /// 定位：调试界面组 DGV（dgvGroupList）编辑缓冲区——组级操作（改名/改描述/调序/增删）落在工作集，
    /// 点「保存」才经 UpdateTagGroup 提交组表 + 落盘；驱动在保存前只看到旧组表（编辑事务隔离）。
    ///
    /// 属性三组：
    /// ① 编辑列（DGV 直绑可改，DataPropertyName 写回本对象）：GroupName（组名——改名=CD Key 迁移语义，保存提交）/ GroupDesc（组描述）
    /// ② 展示列（只读绑定）：GroupID 组序号（组间排序，调序走上/下移按钮交换）/ ChildCount（组内标签数，计算属性读 Source.Tags）
    /// ③ 状态锚：Source（真实 TagGroup 引用）/ SourceGroupName（入集定格 CD 键，改名定位用）/ IsNew / IsDirty
    ///
    /// 行对象生命周期：切实例/放弃 = RebuildGroupRows 全量重建（丢弃工作集）；保存 = ApplyToSource 回写后 CommitState 收敛。
    /// </summary>
    public class TagGroupRow
    {
        #region 编辑列（DGV 直绑可改）

        /// <summary>组名称（业务字段 = CD Key 一致；改名只改副本，键迁移移交保存 ApplyGroupEdits）</summary>
        [Description("组名")]
        public string GroupName { get; set; }

        /// <summary>组描述（组语义备注）</summary>
        [Description("组描述")]
        public string GroupDesc { get; set; }

        #endregion

        #region 展示列（只读绑定）

        /// <summary>组序号（组间排序；调序走上/下移按钮，本列只读展示）</summary>
        [Description("组序号")]
        public int GroupID { get; set; }

        /// <summary>组内标签数（计算属性：实时读真实组表，保存前即最新）</summary>
        [Description("标签数")]
        public int ChildCount => Source?.Tags?.Count ?? 0;

        #endregion

        #region 状态锚

        /// <summary>真实组表组引用（保存回写目标；IsNew 行入集为 null，保存时 EnsureSource 新建）</summary>
        public TagGroup Source { get; private set; }

        /// <summary>入集时刻组表 CD 键（定格不变；保存时与 Source 的 GroupName/新组名比较检测改名）</summary>
        public string SourceGroupName { get; private set; }

        /// <summary>未保存新增组（无组表条目，驱动不认）</summary>
        public bool IsNew { get; private set; }

        /// <summary>未保存修改（编辑列相对组表有差异；保存/放弃后清除）</summary>
        public bool IsDirty { get; set; }

        #endregion

        #region 工厂

        /// <summary>
        /// 从组表真实组建工作行（存量行快照）：编辑列拷贝 + Source/SourceGroupName 锚定
        /// </summary>
        public static TagGroupRow FromSource(TagGroup group, string groupName)
        {
            if (group == null)
                throw new ArgumentNullException(nameof(group));

            return new TagGroupRow
            {
                GroupName = groupName,
                GroupDesc = group.GroupDesc,
                GroupID = group.GroupID,
                Source = group,
                SourceGroupName = groupName,
            };
        }

        /// <summary>
        /// 新建工作行（IsNew：Source=null，保存时 EnsureSource 建 TagGroup 经 AddTagGroup 进组表）
        /// </summary>
        /// <param name="groupId">组序号（默认 = 组表最大 + 1）</param>
        /// <param name="groupName">默认组名（全表不重名）</param>
        public static TagGroupRow CreateNew(int groupId, string groupName)
        {
            return new TagGroupRow
            {
                GroupID = groupId,
                GroupName = groupName,
                IsNew = true,
            };
        }

        #endregion

        #region 提交

        /// <summary>
        /// 是否有真实未保存差异（组名/组描述/组序号 vs 组表 Source；IsNew 草稿 Source=null 恒有差异）。
        /// ⚠️ GroupID 参与判定——MoveGroup 交换组序号后必须置脏，否则 ApplyGroupEdits 以"无脏且未改名"跳过该行
        /// → UpdateTagGroup 不执行 → 组排序不落盘（2026-09-10 现场验证坐实并修复：原实现漏比 GroupID）。
        /// 交换后与 Source 一致（上移又移回原位）自动恢复非脏。
        /// </summary>
        public bool HasRealChanges()
        {
            if (Source == null) return true;
            return GroupID != Source.GroupID
                || !string.Equals((GroupName ?? string.Empty).Trim(), Source.GroupName ?? string.Empty, StringComparison.Ordinal)
                || !string.Equals(GroupDesc ?? string.Empty, Source.GroupDesc ?? string.Empty, StringComparison.Ordinal);
        }

        /// <summary>
        /// 构造提交用完整 TagGroup（存量行：Tags 沿用旧 Source.Tags 实时引用——组结构编辑不动标签字典；
        /// IsNew 行：Source=null → Tags 空字典待填充）。⚠️ 不直改 Source 字段——组身份三重一致
        /// （CD Key=GroupName=组内 TagItem.GroupName）写入点收敛在 ConfigManager.UpdateTagGroup/AddTagGroup，UI 只经此构造对象提交。
        /// </summary>
        public TagGroup BuildForSubmit()
        {
            return new TagGroup
            {
                GroupName = (GroupName ?? string.Empty).Trim(),
                GroupDesc = GroupDesc ?? string.Empty,
                GroupID = GroupID,
                Tags = Source?.Tags ?? new ConcurrentDictionary<string, TagItem>(),
            };
        }

        /// <summary>
        /// 保存成功收敛（存量行整体替换后调用——Source 引用已被 UpdateTagGroup 换新组对象，重建锚点）：
        /// 入参 = 提交后组表实际存在的新组对象（调用方 UpdateTagGroup 后现取）。IsNew 行 AddTagGroup 后同此现取。
        /// </summary>
        public void CommitState(TagGroup submittedGroup, string submittedKey)
        {
            IsNew = false;
            IsDirty = false;
            Source = submittedGroup;
            SourceGroupName = submittedKey;
        }

        #endregion
    }
}
