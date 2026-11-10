using System;
using System.ComponentModel;
using Taoyl.Application.PlcDriver.Models.Cache;

namespace Taoyl.Application.PlcDriver.Models
{
    /// <summary>
    /// PLC标签项（模板15列纯数据载体，2026-09-08 扩 TagDesc；GroupSortID 语义澄清改名 TagID）
    /// 列：GroupName/GroupID/组序号、GroupDesc/组描述、TagID(原GroupSortID)/组内序号、RefreshFreq(-1不刷新)/TagName/Address/DataType/RWTimes/Length/TagDesc/标签描述/Value/LastOperationTime/LastOperationResult/LastOperationMsg
    /// 值存储：TypedValue 强类型容器（零装箱高频通道）；Value(object) 为低频 object 视图（内部走容器，此刻才装箱）
    /// </summary>
    public class TagItem
    {
        /// <summary>
        /// 所属组名（组身份冗余副本——CD Key 为运行时权威，本字段随加载/UpdateTagGroup 同步，纯归属标记）
        /// </summary>
        public string GroupName { get; set; }

        /// <summary>
        /// 组内序号（标签在所属组内的展示/落盘顺序；原 GroupSortID 语义澄清改名——驱动运行零依赖，仅排序展示）
        /// </summary>
        public int TagID { get; set; }

        /// <summary>
        /// 标签名称（唯一标识，组内唯一）
        /// </summary>
        [Description("标签名")]
        public string TagName { get; set; }

        /// <summary>
        /// 标签描述（2026-09-08 新增：现场语义备注，随地址表 Excel 持久化）
        /// </summary>
        [Description("标签描述")]
        public string TagDesc { get; set; }

        /// <summary>
        /// PLC地址（如"D100"、"DB100.DBW0"、"E0.1"）
        /// </summary>
        [Description("地址")]
        public string Address { get; set; }

        /// <summary>
        /// 数据类型（含数组类型，组内可混类型）
        /// </summary>
        [Description("数据类型")]
        public PlcDataType DataType { get; set; }

        /// <summary>
        /// 长度：数组类型 = 元素个数（ByteArray 亦然，与 Read/Write 的 length 参数同义）；
        /// String = 字符数（协议层按此填充/截断）；标签层不做字节换算，寻址粒度由协议层解释
        /// </summary>
        [Description("长度")]
        public int Length { get; set; } = 1;

        /// <summary>
        /// 刷新频率（毫秒），-1 = 不参与轮询刷新（替代旧 Autoread 标志）
        /// </summary>
        [Description("刷新频率")]
        public int RefreshFreq { get; set; } = -1;

        
        /// <summary>
        /// 读写次数统计（每次IO自增，≥9999归0）
        /// </summary>
        [Description("读写次数")]
        public int RWTimes { get; set; }

        /// <summary>
        /// 当前值（object 只读视图，低频）
        /// 定位：UI 绑定视图（DataGridView 的 DataPropertyName 只能绑属性不能绑方法，调试界面直绑此列显示）
        /// 内部走强类型容器（此刻才装箱）：未刷新返回 null（等价旧 Value==null 语义）；
        /// 写值勿经此属性——统一走 tag.TypedValue.SetObject(value)（object 低频）/ TypedValueHolder.Set（强类型高频）
        /// </summary>
        [Description("值")]
        public object Value
        {
            get
            {
                var typedValue = _typedValue;
                return typedValue != null ? typedValue.GetObject() : null;
            }
        }

        /// <summary>
        /// 调试写入值（纯调试输入槽，仅供调试界面写入测试填写；不参与轮询、不进地址表 Excel 模板）
        /// </summary>
        [Description("写入值")]
        public string WriteValue { get; set; }

        

        /// <summary>
        /// 强类型值容器（volatile 引用：IoWorker 提交/重载替换 vs 业务线程读的可见性）
        /// 值读写统一走容器：高频强类型通道（TypedValueHolder&lt;T&gt;.Set/TryGet 零装箱）；
        /// 低频 object 写 tag.TypedValue.SetObject(value)；Value(object) 仅只读视图（此刻才装箱）
        /// </summary>
        private volatile ITypedValue _typedValue;

        /// <summary>
        /// 强类型值容器（组内混类型，TagItem 保持非泛型，经 ITypedValue 擦除引用）
        /// 定型：配置加载期按 DataType 创建（TagItem.DataType 定型即容器泛型定型）；
        /// invariant：进地址表的 TagItem 必须已定型（加载/AddTag/AddTagGroup 入口 Ensure）
        /// </summary>
        public ITypedValue TypedValue
        {
            get => _typedValue;
            set => _typedValue = value;
        }

        /// <summary>
        /// 确保值容器已定型（懒创建 + 失配重建：按当前 DataType 走工厂；invariant 入口调用，DataType 须已赋值）
        /// 容器泛型由 DataType 定型（见 TypedValueHolder&lt;T&gt;）：DataType 运行期被改动后容器即失配，
        /// 读策略将无从投递值（ApplyRead 报"值容器类型不匹配"）——故此处检测失配即重建（新容器未刷新，语义=等首轮轮询）
        /// </summary>
        public void EnsureTypedValue()
        {
            var existing = _typedValue;
            if (existing == null || existing.DataType != DataType)
            {
                _typedValue = TypedValueHolderFactory.Create(DataType);
            }
        }

        

        /// <summary>
        /// 最后读写时间（到期判断基准，初始 MinValue 即首次立即到期）
        /// </summary>
        [Description("最近操作时间")]
        public DateTime LastOperationTime { get; set; }

        /// <summary>
        /// 最后读写操作结果（true=成功）
        /// </summary>
        [Description("最近结果")]
        public bool LastOperationResult { get; set; }

        /// <summary>
        /// 最后读写信息（失败原因等）
        /// </summary>
        [Description("最近信息")]
        public string LastOperationMsg { get; set; }

        /// <summary>
        /// 是否到期需要刷新（RefreshFreq>0 且距上次操作已超过刷新频率）
        /// </summary>
        /// <param name="now">当前时间</param>
        public bool IsDue(DateTime now)
        {
            return RefreshFreq > 0 && (now - LastOperationTime).TotalMilliseconds >= RefreshFreq;
        }
    }
}
