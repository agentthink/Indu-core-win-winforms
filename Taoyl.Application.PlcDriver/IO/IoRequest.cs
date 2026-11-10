using Taoyl.Application.PlcDriver.Models;

namespace Taoyl.Application.PlcDriver.IO
{
    /// <summary>
    /// IO请求类型
    /// </summary>
    public enum IoRequestKind
    {
        /// <summary>读请求</summary>
        Read,

        /// <summary>写请求</summary>
        Write
    }

    /// <summary>
    /// IO请求（统一命令载体）
    /// 字段：Kind/Address/DataType/Length/Value(写值)/GroupName/TagName(回写查找)
    /// 无信号槽、无 IsPoll、无批量字段
    /// </summary>
    public class IoRequest
    {
        /// <summary>
        /// 请求类型（读/写，不分缓存与直接）
        /// </summary>
        public IoRequestKind Kind { get; }

        /// <summary>
        /// PLC地址
        /// </summary>
        public string Address { get; }

        /// <summary>
        /// 数据类型
        /// </summary>
        public PlcDataType DataType { get; }

        /// <summary>
        /// 元素个数（数组长度或字符串长度）
        /// </summary>
        public int Length { get; }

        /// <summary>
        /// 写入值（写请求使用，读请求为 null）
        /// </summary>
        public object Value { get; }

        /// <summary>
        /// 所属组名（提交回写查找用）
        /// </summary>
        public string GroupName { get; }

        /// <summary>
        /// 标签名（提交回写查找用）
        /// </summary>
        public string TagName { get; }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="kind">请求类型</param>
        /// <param name="address">PLC地址</param>
        /// <param name="dataType">数据类型</param>
        /// <param name="length">元素个数</param>
        /// <param name="value">写入值（读请求传 null）</param>
        /// <param name="groupName">所属组名</param>
        /// <param name="tagName">标签名</param>
        public IoRequest(IoRequestKind kind, string address, PlcDataType dataType, int length, object value, string groupName, string tagName)
        {
            Kind = kind;
            Address = address;
            DataType = dataType;
            Length = length;
            Value = value;
            GroupName = groupName;
            TagName = tagName;
        }
    }
}
