using CommunityToolkit.Mvvm.ComponentModel;
using System;

namespace Taoyl.UI.FormMain.ViewModels
{
    /// <summary>
    /// ViewModel基类
    /// 继承CommunityToolkit.Mvvm的ObservableObject，提供源生成器支持
    /// 添加资源释放支持
    /// </summary>
    public abstract class ViewModelBase : ObservableObject, IDisposable
    {
        private bool disposed;

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 释放资源的保护方法
        /// </summary>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    OnDispose();
                }
                disposed = true;
            }
        }

        /// <summary>
        /// 子类重写此方法执行具体的资源释放逻辑
        /// </summary>
        protected virtual void OnDispose() { }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~ViewModelBase()
        {
            Dispose(false);
        }
    }
}