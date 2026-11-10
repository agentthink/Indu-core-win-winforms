using System;
using System.ComponentModel;
using System.Linq;
using Taoyl.Application.PlcDriver.Models;
using Taoyl.Application.PlcDriver.Models.Cache;

namespace Taoyl.UI.Pages.Models
{
    /// <summary>
    /// 地址表工作行（UI 工作集模型，纯 POCO 无 INotify）
    ///
    /// 定位：调试界面编辑缓冲区——界面增删改全部落在工作集，点「保存」才提交组表（Source 回写）+ 落盘。
    /// 驱动在保存前永远只看到旧组表（编辑事务隔离）；脏行明黄/新增行淡青由 View CellFormatting 按行状态上色。
    ///
    /// 属性三组：
    /// ① 编辑列（DGV 直绑可改，DataPropertyName 写回本对象）：TagID/TagName/Address/DataType/Length/RefreshFreq/WriteValue
    ///    WriteValue 为点动写输入缓冲（非地址表配置字段，编辑不置脏——置脏会禁读写锁死写入入口）；
    /// ② 展示列（只读绑定，RefreshTagRowValues 1s 节拍快照写入，预格式化字符串）：ValueText/RWTimesText/OpTimeText/OpResultText/OpMsgText；
    ///    快照只碰展示列永不碰编辑列 → 编辑中的单元格不受任何影响；值变化才由 VM ResetItem 行级通知 DGV。
    /// ③ 状态锚：Source（真实 TagItem 引用，保存回写目标）/ SourceTagName（入集定格键，改名定位用）/
    ///    IsNew（未保存新增）/ IsDirty（未保存修改）。
    ///
    /// 刷新通道：展示列赋值无 INotify，VM.RefreshTagRowValues 逐行比对变化后 ResetItem(idx)（ProcessMonitor 先例行级粒度）。
    /// 行对象生命周期：切组/切实例/放弃 = RebuildTagRows 全量重建（丢弃工作集）；保存 = ApplyToSource 回写后 CommitState 收敛（行对象复用）。
    /// </summary>
    public class TagItemRow
    {
        #region 编辑列（DGV 直绑可改）

        /// <summary>组中序号（[Description] 供 DGV 列头反射，文案与 TagItem 对齐）</summary>
        [Description("序号")]
        public int TagID { get; set; }

        /// <summary>标签名称（组内唯一；改名只改副本，键迁移移交保存 ApplyEdits）</summary>
        [Description("标签名")]
        public string TagName { get; set; }

        /// <summary>PLC地址（如"D100"、"DB100.DBW0"、"E0.1"）</summary>
        [Description("地址")]
        public string Address { get; set; }

        /// <summary>数据类型（含数组类型；下拉列绑 Display/Value 包装列表，显示枚举成员 [Description] 短名）</summary>
        [Description("数据类型")]
        public PlcDataType DataType { get; set; }

        /// <summary>元素个数（数组长度或字符串长度）</summary>
        [Description("长度")]
        public int Length { get; set; } = 1;

        /// <summary>刷新频率（毫秒），-1 = 不参与轮询刷新</summary>
        [Description("刷新频率")]
        public int RefreshFreq { get; set; } = -1;

        /// <summary>标签描述（现场语义备注，随地址表 Excel 持久化；2026-09-08 扩列）</summary>
        [Description("标签描述")]
        public string TagDesc { get; set; }

        /// <summary>调试写入值（点动写输入缓冲；非地址表配置字段——编辑不置脏、不回写 Source）</summary>
        [Description("写入值")]
        public string WriteValue { get; set; }

        #endregion

        #region 展示列（只读绑定，快照写入，预格式化）

        /// <summary>最近操作结果文本（成功/失败）——RefreshSnapshot 拼串与 View 结果列红绿判定同源引用，改文案只改此处</summary>
        public const string OpResultSuccessText = "成功";
        public const string OpResultFailText = "失败";

        /// <summary>值（预格式化：数组逗号展开，与导出语义一致）</summary>
        [Description("值")]
        public string ValueText { get; set; } = string.Empty;

        /// <summary>读写次数</summary>
        [Description("读写次数")]
        public string RWTimesText { get; set; } = string.Empty;

        /// <summary>最近操作时间（HH:mm:ss，未操作过为空串）</summary>
        [Description("最近操作时间")]
        public string OpTimeText { get; set; } = string.Empty;

        /// <summary>最近结果（成功/失败，未操作过为空串）</summary>
        [Description("最近结果")]
        public string OpResultText { get; set; } = string.Empty;

        /// <summary>最近信息（失败原因等）</summary>
        [Description("最近信息")]
        public string OpMsgText { get; set; } = string.Empty;

        #endregion

        #region 状态锚

        /// <summary>真实组表标签引用（保存回写目标；IsNew 行入集为 null，保存时 EnsureSource 新建）</summary>
        public TagItem Source { get; private set; }

        /// <summary>入集时刻组表键（定格不变；保存时与 Source.TagName 比较检测改名）</summary>
        public string SourceTagName { get; private set; }

        /// <summary>未保存新增行（无组表条目，驱动不认）</summary>
        public bool IsNew { get; private set; }

        /// <summary>未保存修改（编辑列相对组表有差异；保存/放弃后清除）</summary>
        public bool IsDirty { get; set; }

        #endregion

        #region 工厂

        /// <summary>
        /// 从组表真实标签建工作行（存量行快照）：编辑列拷贝 + Source/SourceTagName 锚定 + 展示列初快照
        /// </summary>
        public static TagItemRow FromSource(TagItem tag)
        {
            if (tag == null)
                throw new ArgumentNullException(nameof(tag));

            var row = new TagItemRow
            {
                TagID = tag.TagID,
                TagName = tag.TagName,
                Address = tag.Address,
                DataType = tag.DataType,
                Length = tag.Length,
                RefreshFreq = tag.RefreshFreq,
                TagDesc = tag.TagDesc,
                WriteValue = tag.WriteValue,
                Source = tag,
                SourceTagName = tag.TagName,
            };
            row.RefreshSnapshot(); // 初值 = 组表当前值（避免首屏空 1s 等节拍）
            return row;
        }

        /// <summary>
        /// 新建工作行（IsNew：Source=null，保存时 AddTag 进组表）
        /// </summary>
        /// <param name="tagId">组内序号（默认 = 组内最大 + 1）</param>
        /// <param name="tagName">默认标签名（组内不重名）</param>
        public static TagItemRow CreateNew(int tagId, string tagName)
        {
            return new TagItemRow
            {
                TagID = tagId,
                TagName = tagName,
                Address = string.Empty,
                DataType = PlcDataType.Bool,
                Length = 1,
                RefreshFreq = -1,
                IsNew = true,
            };
        }

        #endregion

        #region 快照与提交

        /// <summary>
        /// 是否有真实未保存差异（编辑列 vs 组表 Source 逐字段比对；IsNew 草稿 Source=null 恒有差异——入组必新增）。
        /// 置脏判定统一入口：NotifyRowEdited/MoveRow/序号重排/删后收紧/设频率/改名回滚 全走 row.IsDirty = row.HasRealChanges()，
        /// 操作改回原值即自动恢复非脏。前提：Source 配置字段在编辑会话期稳定（IoWorker 只写值/统计，不碰编辑列字段）。
        /// </summary>
        public bool HasRealChanges()
        {
            if (Source == null) return true;
            return TagID != Source.TagID
                || !string.Equals((TagName ?? string.Empty).Trim(), Source.TagName, StringComparison.Ordinal)
                || !string.Equals(Address ?? string.Empty, Source.Address ?? string.Empty, StringComparison.Ordinal)
                || DataType != Source.DataType
                || Length != Source.Length
                || RefreshFreq != Source.RefreshFreq
                || !string.Equals(TagDesc ?? string.Empty, Source.TagDesc ?? string.Empty, StringComparison.Ordinal);
        }

        /// <summary>展示格式化：数组逗号展开（与导出语义一致），null → 空串（原 VM 层 FormatValue 归位——展示格式化属行模型自身职责）</summary>
        private static string FormatValue(object value)
        {
            if (value == null) return string.Empty;
            if (value is Array arr) return JoinArray(arr);
            return value.ToString();
        }

        private static string JoinArray(Array arr)
        {
            if (arr == null || arr.Length == 0) return string.Empty;
            return string.Join(",", arr.Cast<object>());
        }

        /// <summary>
        /// 1s 节拍快照：读 Source 值/统计 → 拼展示列（预格式化）；仅实际变化才返回 true（VM 据此 ResetItem）
        /// 只碰展示列，永不碰编辑列；Source=null（IsNew）返回 false（展示列保持空）
        /// </summary>
        public bool RefreshSnapshot()
        {
            if (Source == null)
                return false;

            string v = FormatValue(Source.Value);
            string r = Source.RWTimes.ToString();
            string t = Source.LastOperationTime == DateTime.MinValue
                ? string.Empty
                : Source.LastOperationTime.ToString("HH:mm:ss");
            string o = Source.LastOperationTime == DateTime.MinValue
                ? string.Empty
                : Source.LastOperationResult ? OpResultSuccessText : OpResultFailText;
            string m = Source.LastOperationMsg ?? string.Empty;

            bool changed = false;
            if (ValueText != v) { ValueText = v; changed = true; }
            if (RWTimesText != r) { RWTimesText = r; changed = true; }
            if (OpTimeText != t) { OpTimeText = t; changed = true; }
            if (OpResultText != o) { OpResultText = o; changed = true; }
            if (OpMsgText != m) { OpMsgText = m; changed = true; }
            return changed;
        }

        /// <summary>
        /// 长度下界归一（与 ConfigManager 加载期归一、ValidatePending 预检同规则）：
        /// String 下界 0（0 的合法性与含义由协议解释，配置层不做协议感知校验）；其余类型的 Length 是数组元素个数，下界 1
        /// </summary>
        private static int NormalizeLength(PlcDataType dataType, int length)
        {
            return dataType == PlcDataType.String ? Math.Max(0, length) : Math.Max(1, length);
        }

        /// <summary>
        /// 保存提交·回写：编辑列全量回写 Source（组表真值），同实例引用不变（值容器/读写统计保留）
        /// DataType 变更 → 重建值容器（值清空等首轮轮询，自旧 CellValueChanged 即时重建逻辑搬入，保存才生效）
        /// WriteValue 为临时缓冲不回写；Length 按类型归一下界（预检已拦非法值，此处兜底）
        /// </summary>
        public void ApplyToSource()
        {
            if (Source == null)
                return;

            Source.TagID = TagID;
            Source.TagName = (TagName ?? string.Empty).Trim();

            if (Source.DataType != DataType && Source.TypedValue != null)
                Source.TypedValue = TypedValueHolderFactory.Create(DataType);
            Source.DataType = DataType;

            Source.Address = Address ?? string.Empty;
            Source.Length = NormalizeLength(DataType, Length);
            Source.RefreshFreq = RefreshFreq;
            Source.TagDesc = TagDesc ?? string.Empty;
        }

        /// <summary>
        /// 保存提交·新建：IsNew 行建真实 TagItem（GroupName=当前组 + 值容器定型）
        /// </summary>
        /// <param name="groupName">所属组名（进组表必需）</param>
        public TagItem EnsureSource(string groupName)
        {
            if (Source == null)
            {
                Source = new TagItem
                {
                    GroupName = groupName,
                    TagID = TagID,
                    TagName = (TagName ?? string.Empty).Trim(),
                    Address = Address ?? string.Empty,
                    DataType = DataType,
                    Length = NormalizeLength(DataType, Length),
                    RefreshFreq = RefreshFreq,
                    TagDesc = TagDesc ?? string.Empty,
                };
                Source.EnsureTypedValue();
            }
            return Source;
        }

        /// <summary>
        /// 保存成功收敛：清脏态 + 锚点刷新（SourceTagName = 当前名；改名行保存后键已迁移）
        /// </summary>
        public void CommitState()
        {
            IsNew = false;
            IsDirty = false;
            SourceTagName = Source?.TagName ?? (TagName ?? string.Empty).Trim();
        }

        #endregion
    }
}
