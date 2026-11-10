using System;
using System.Collections.Generic;
using System.Linq;
using Taoyl.UI.FormMain.Models;

namespace Taoyl.UI.FormMain.Services
{
    /// <summary>
    /// 导航服务实现
    /// 只负责解析菜单、创建窗体实例、触发导航事件
    /// 不操作任何UI控件
    /// </summary>
    public class NavigationService : INavigationService
    {
        private readonly MenuManager menuManager;

        public event EventHandler<NavigationEventArgs> Navigated;

        public NavigationService(MenuManager menuManager)
        {
            this.menuManager = menuManager ?? throw new ArgumentNullException(nameof(menuManager));
        }

        /// <summary>
        /// 导航到指定菜单页面
        /// </summary>
        public void NavigateTo(string menuName)
        {
            var menuItem = FindMenuItem(menuName);
            if (menuItem == null)
                throw new ArgumentException($"菜单不存在: {menuName}");

            var form = menuManager.GetFormInstance(menuItem);
            if (form == null)
                throw new InvalidOperationException($"无法创建窗体: {menuItem.DisplayName}");

            // 只触发事件，由View决定如何显示窗体
            Navigated?.Invoke(this, new NavigationEventArgs
            {
                MenuName = menuName,
                Form = form
            });
        }

        /// <summary>
        /// 返回上一页（待实现导航历史栈）
        /// </summary>
        public void GoBack()
        {
            // 可实现导航历史栈
        }

        /// <summary>
        /// 递归查找菜单项
        /// </summary>
        private MenuItem FindMenuItem(string menuName)
        {
            return menuManager.GetAllMenuItems()
                .FirstOrDefault(m => m.Name == menuName);
        }

    }


    /// <summary>
    /// 导航事件参数
    /// </summary>
    public class NavigationEventArgs : EventArgs
    {
        public string MenuName { get; set; } = string.Empty;
        public System.Windows.Forms.Form Form { get; set; }
    }

    /// <summary>
    /// 导航服务接口
    /// ViewModel通过此接口请求导航，不直接操作窗体
    /// </summary>
    public interface INavigationService
    {
        /// <summary>
        /// 导航到指定菜单
        /// </summary>
        void NavigateTo(string menuName);

        /// <summary>
        /// 返回上一页
        /// </summary>
        void GoBack();

        /// <summary>
        /// 导航完成事件
        /// </summary>
        event EventHandler<NavigationEventArgs> Navigated;
    }
}