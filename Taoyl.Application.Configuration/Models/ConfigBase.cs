using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Taoyl.Application.Configuration.Models
{
    /// <summary>
    /// 所有配置模型的抽象基类，提供 INotifyPropertyChanged 通知、
    /// 递归脏标记和统一的属性赋值入口。
    /// </summary>
    public abstract class ConfigBase : INotifyPropertyChanged
    {
        private bool isDirty;

        /// <summary>
        /// 属性变更事件，用于 WinForms / WPF 数据绑定通知。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// 递归脏标记：自身或任意嵌套 ConfigBase 子对象或 Enumerable 列表中
        /// 有任一 ConfigBase 项的 IsDirty 为 true 时返回 true。
        /// </summary>
        [JsonIgnore]
        public bool IsDirty
        {
            get
            {
                if (isDirty)
                    return true;

                var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
                foreach (var prop in props)
                {
                    if (prop.GetCustomAttribute<JsonIgnoreAttribute>() != null)
                        continue;
                    if (prop.GetIndexParameters().Length > 0)
                        continue;

                    var val = prop.GetValue(this);
                    if (val is ConfigBase nested && nested.IsDirty)
                        return true;
                    if (val is IEnumerable<ConfigBase> list && list.Any(item => item != null && item.IsDirty))
                        return true;
                }
                return false;
            }
        }

        /// <summary>
        /// 属性的统一赋值入口，值变更时自动设置脏标记并触发 PropertyChanged 事件。
        /// 所有配置属性的 setter 必须调用此方法，不可直接赋值字段。
        /// </summary>
        /// <typeparam name="T">属性类型</typeparam>
        /// <param name="field">私有字段引用</param>
        /// <param name="value">新值</param>
        /// <param name="propertyName">自动填充的属性名</param>
        /// <returns>值是否发生变更</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;
            field = value;
            isDirty = true;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// 触发 PropertyChanged 事件。
        /// </summary>
        /// <param name="propertyName">属性名</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 强制标记自身为脏（不含递归），用于"重置默认值"后使 Save 可写入。
        /// </summary>
        public void SetDirty() => isDirty = true;

        /// <summary>
        /// 递归清除自身及所有嵌套 ConfigBase 子对象和 
        /// IEnumerable&lt;ConfigBase&gt; 列表中各项的脏标记。
        /// </summary>
        public void MarkClean()
        {
            isDirty = false;

            var props = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (var prop in props)
            {
                if (prop.GetIndexParameters().Length > 0)
                    continue;

                var val = prop.GetValue(this);
                if (val is ConfigBase nested)
                    nested.MarkClean();
                else if (val is IEnumerable<ConfigBase> list)
                {
                    foreach (var item in list)
                        item?.MarkClean();
                }
            }
        }

        /// <summary>
        /// 深拷贝为指定类型的配置实例（编辑草稿用）。
        /// </summary>
        /// <typeparam name="T">目标配置类型，须有无参构造函数</typeparam>
        public T Clone<T>() where T : ConfigBase, new()
        {
            var json = JsonConvert.SerializeObject(this);
            var clone = JsonConvert.DeserializeObject<T>(json);
            clone.MarkClean();
            return clone;
        }

        /// <summary>
        /// 把当前配置的值原地复制到目标实例（保存提交用）。
        /// </summary>
        /// <param name="target">目标配置实例，将被原地更新</param>
        /// <remarks>
        /// 必须使用默认 JsonConvert（ObjectCreationHandling = Auto）：
        /// PopulateObject 用 Auto 会「填充」嵌套对象，保持引用不变；
        /// 若改成 Replace 会「替换」嵌套对象，导致指向旧对象的窄接口
        /// （如 ICardLoginSettings 指向的 AutoLoginConfig）失效。切勿统一成 Replace。
        /// </remarks>
        public void CopyTo(ConfigBase target)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            var json = JsonConvert.SerializeObject(this);
            JsonConvert.PopulateObject(json, target);
        }
    }
}
