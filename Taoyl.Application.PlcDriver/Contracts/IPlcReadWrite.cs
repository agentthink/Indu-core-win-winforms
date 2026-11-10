using Taoyl.Application.PlcDriver.IO;
using Taoyl.Application.PlcDriver.Models;
using System.Collections.Generic;

namespace Taoyl.Application.PlcDriver.Contracts
{
    /// <summary>
    /// PLC读写操作接口（结果统一 ResponseResult 化）
    /// 职责：提供类型安全的读写方法，支持缓存和直接读写
    /// 失败语义：一律返回 Fail（携带 ErrorCode/Message），不抛异常——调用方自行判断 IsSuccess 处理
    /// </summary>
    public interface IPlcReadWrite
    {
        // ===== 直接读写（立即与PLC通信）=====

        /// <summary>
        /// 执行通用IO请求（立即同步执行）
        /// 用途：调试界面等按 地址/数据类型 直接读写的统一入口，免去按类型分派泛型调用；
        /// 读请求：携带 GroupName/TagName 时值自动回填该标签值容器（UI 重绘即见），标签不存在则 Fail
        /// 写请求：Value 必须为与 DataType 匹配的强类型值（协议层写策略直接消费，文本解析请用 PlcTextValue.Parse）
        /// </summary>
        /// <param name="request">IO请求（Kind/Address/DataType/Length/Value/GroupName/TagName）</param>
        /// <returns>操作结果（成功/失败原因）</returns>
        ResponseResult ExecuteIo(IoRequest request);

        /// <summary>
        /// 读取单个标签值（泛型，立即读取）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <returns>操作结果（Success 携带值 / Fail 携带错误码与信息）</returns>
        ResponseResult<T> ReadTag<T>(string groupName, string tagName);

        /// <summary>
        /// 读取整个标签组（立即读取）
        /// </summary>
        /// <param name="groupName">标签组名称</param>
        /// <returns>操作结果（Success 携带标签字典，失败标签以 Message/默认值占位；Fail=组不存在或执行异常）</returns>
        ResponseResult<Dictionary<string, object>> ReadGroup(string groupName);

        /// <summary>
        /// 写入单个标签值（立即写入）
        /// </summary>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <param name="value">要写入的值</param>
        /// <returns>操作结果（成功/失败原因）</returns>
        ResponseResult WriteTag(string groupName, string tagName, object value);

        /// <summary>
        /// 写入整个标签组（立即写入）
        /// </summary>
        /// <param name="groupName">标签组名称</param>
        /// <param name="values">标签值字典</param>
        /// <returns>操作结果（任一标签失败即 Fail）</returns>
        ResponseResult WriteGroup(string groupName, Dictionary<string, object> values);


        // ===== 缓存读写（从缓存中读取，异步写入）=====

        /// <summary>
        /// 从缓存中读取单个标签值（泛型；默认深拷贝数组）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <param name="deepCopy">数组是否深拷贝（默认 true 防业务层修改污染缓存；false 零拷贝适合高频读，调用方负责不改数组）</param>
        /// <returns>操作结果（Fail=标签不存在/值未刷新/类型转换失败）</returns>
        ResponseResult<T> ReadTagCached<T>(string groupName, string tagName, bool deepCopy = true);

        /// <summary>
        /// 从缓存中读取整个标签组（返回深拷贝）
        /// </summary>
        /// <param name="groupName">标签组名称</param>
        /// <returns>操作结果（Success 携带标签字典）</returns>
        ResponseResult<Dictionary<string, object>> ReadGroupCached(string groupName);

        /// <summary>
        /// 写入单个标签值到缓存（异步写入PLC）
        /// </summary>
        /// <typeparam name="T">值类型</typeparam>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <param name="value">要写入的值</param>
        /// <returns>操作结果（Fail=标签不存在/入队失败）</returns>
        ResponseResult WriteTagCached<T>(string groupName, string tagName, T value);

        /// <summary>
        /// 尝试从缓存中读取单个标签值（无结果对象开销，适合高频读取）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <param name="value">输出参数：标签值</param>
        /// <returns>是否读取成功</returns>
        bool TryReadTagCached<T>(string groupName, string tagName, out T value);


        // ===== 纯表访问（本地地址表值，不走队列不通讯）=====

        /// <summary>
        /// 获取标签当前表值（纯表读：不通讯不刷新，返回最近一次轮询/写入的值；未刷新返回 default）
        /// 与缓存读区别：直接查表零请求开销，适合 UI 绑定/监控展示本地值；需要实时值请用 ReadTag/ReadTagCached
        /// 数组类型返回克隆副本（防调用方修改污染缓存，与 ReadTagCached 默认 deepCopy 对齐）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <returns>操作结果（Fail=标签不存在；未刷新=Success(default)）</returns>
        ResponseResult<T> GetValue<T>(string groupName, string tagName);

        /// <summary>
        /// 更新标签表值（纯表写：不通讯不入队，仅改本地表值；何时写 PLC 由业务层自行决定，通常配合 WriteTag/WriteTagCached）
        /// </summary>
        /// <param name="groupName">标签组名称</param>
        /// <param name="tagName">标签名称</param>
        /// <param name="value">要写入的值</param>
        /// <returns>操作结果（Fail=标签不存在/类型转换失败）</returns>
        ResponseResult SetValue(string groupName, string tagName, object value);


        // ===== 底层读写（动态地址，不依赖标签配置）=====

        /// <summary>
        /// 原始读取（根据数据类型读取指定地址）
        /// </summary>
        /// <typeparam name="T">返回值类型</typeparam>
        /// <param name="address">PLC地址</param>
        /// <param name="dataType">数据类型</param>
        /// <returns>操作结果（Success 携带值 / Fail 携带错误码与信息）</returns>
        ResponseResult<T> ReadRaw<T>(string address, PlcDataType dataType);

        /// <summary>
        /// 原始写入（根据数据类型写入指定地址）
        /// </summary>
        /// <param name="address">PLC地址</param>
        /// <param name="value">要写入的值</param>
        /// <param name="dataType">数据类型</param>
        /// <returns>操作结果（成功/失败原因）</returns>
        ResponseResult WriteRaw(string address, object value, PlcDataType dataType);
    }
}
