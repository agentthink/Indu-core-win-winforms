namespace Taoyl.Application.PlcDriver.Models.Cache
{
    /// <summary>
    /// 值容器类型擦除视图（非泛型）
    /// 用途：TagItem 持 ITypedValue 引用（组内混类型，TagItem 保持非泛型），具体泛型由 TypedValueHolder&lt;T&gt; 实现
    /// 高频路径走强类型 TypedValueHolder&lt;T&gt;（Set/TryGet 零装箱，不经过 object）；
    /// GetObject/SetObject 为低频 object 视图（调试界面/兼容回写/配置重载迁移用，此刻才装箱拆箱）
    /// </summary>
    public interface ITypedValue
    {
        /// <summary>
        /// 数据类型（与 TagItem.DataType 一致，创建时定型）
        /// </summary>
        PlcDataType DataType { get; }

        /// <summary>
        /// 是否已赋值（false=从未刷新，等价旧 Value==null 语义；低频弱一致可接受）
        /// </summary>
        bool HasValue { get; }

        /// <summary>
        /// 获取值（object 视图，低频；未刷新返回 null，等价旧 Value==null；值类型此刻装箱）
        /// </summary>
        object GetObject();

        /// <summary>
        /// 设置值（object 视图，低频；null=清除值复位未刷新；类型不匹配转换失败保留旧值不抛）
        /// </summary>
        void SetObject(object value);
    }
}
