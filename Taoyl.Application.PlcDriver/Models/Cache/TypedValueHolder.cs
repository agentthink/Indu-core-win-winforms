using System;
using System.Threading;

namespace Taoyl.Application.PlcDriver.Models.Cache
{
    /// <summary>
    /// 强类型值容器（零装箱读写）
    /// 内部 T _value 直接存储，Set/TryGet 泛型方法全程强类型，不经过 object
    /// 可见性：单一写者（IoWorker 提交）+ 多读者（业务线程）；泛型值类型字段不能声明 volatile（C# 限制），
    /// 读写两侧各一次 Interlocked.MemoryBarrier（release/acquire 语义，x86 上近乎无开销）
    /// _hasValue：区分"未刷新"(false) 与"已赋值"(true)，等价旧 Value==null 语义（业务层判断依赖）
    /// </summary>
    /// <typeparam name="T">值类型（与 PlcDataType 一一对应，工厂创建定型）</typeparam>
    public sealed class TypedValueHolder<T> : ITypedValue
    {
        #region 成员变量
        private T _value;
        private bool _hasValue;
        #endregion

        #region 构造函数
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dataType">数据类型（定型终生不变）</param>
        public TypedValueHolder(PlcDataType dataType)
        {
            DataType = dataType;
        }
        #endregion

        #region ITypedValue 属性
        /// <summary>
        /// 数据类型（创建时定型）
        /// </summary>
        public PlcDataType DataType { get; }

        /// <summary>
        /// 是否已赋值（false=未刷新；低频读取，弱一致可接受）
        /// </summary>
        public bool HasValue
        {
            get { return _hasValue; }
        }
        #endregion

        #region 强类型方法（零装箱零拆箱，高频路径）
        /// <summary>
        /// 设置值（强类型，零装箱；IoWorker 提交/策略闭包调用，单一写者）
        /// </summary>
        public void Set(T value)
        {
            _value = value;
            _hasValue = true;
            Interlocked.MemoryBarrier(); // release：值+状态写完后发布
        }

        /// <summary>
        /// 尝试取值（强类型，零拆箱；false=未赋值/未刷新；业务线程多读者）
        /// </summary>
        public bool TryGet(out T value)
        {
            Interlocked.MemoryBarrier(); // acquire：读值前同步写者发布
            value = _value;
            return _hasValue;
        }
        #endregion

        #region ITypedValue 实现（object 视图，低频）
        /// <summary>
        /// 获取值（object 视图；未刷新返回 null 等价旧 Value==null；值类型此刻装箱一次）
        /// </summary>
        object ITypedValue.GetObject()
        {
            Interlocked.MemoryBarrier();
            return _hasValue ? (object)_value : null;
        }

        /// <summary>
        /// 设置值（object 视图；null=清除值复位未刷新；类型不匹配尝试转换，失败保留旧值不抛——旧 Value 直存不抛，行为保持）
        /// </summary>
        void ITypedValue.SetObject(object value)
        {
            if (value == null)
            {
                // null = 清除值（复位未刷新状态）
                Interlocked.MemoryBarrier();
                _value = default(T);
                _hasValue = false;
                Interlocked.MemoryBarrier();
                return;
            }

            if (value is T typed)
            {
                Set(typed);
                return;
            }

            // 类型不匹配：尝试转换；失败静默保留旧值（低频路径，下次轮询/写入覆盖）
            try
            {
                Set((T)Convert.ChangeType(value, typeof(T)));
            }
            catch (Exception)
            {
                // 忽略：保留旧值不抛（调用方主流程不受影响）
            }
        }
        #endregion
    }
}
