using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using Taoyl.Application.AuthService.Interfaces;

namespace Taoyl.UI.FormMain.ViewModels
{
    /// <summary>
    /// 登录对话框ViewModel
    /// </summary>
    public partial class LoginViewModel : ViewModelBase
    {
        private readonly IAuthService authService;

        #region 可观察属性
        [ObservableProperty]
        private string userName = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private string errorMessage = string.Empty;

        [ObservableProperty]
        private bool hasError;
        #endregion

        #region 事件
        /// <summary>
        /// 登录完成事件（参数简化：View只关心是否成功）
        /// </summary>
        public event Action<bool> LoginCompleted;
        public event Action<bool> LoginCancel;
        #endregion

        #region 命令
        public IRelayCommand LoginCommand { get; }
        public IRelayCommand CancelCommand { get; }
        #endregion

        #region 构造函数
        public LoginViewModel(IAuthService authService)
        {
            this.authService = authService ?? throw new ArgumentNullException(nameof(authService));
            LoginCommand = new RelayCommand(ExecuteLogin);
            CancelCommand = new RelayCommand(ExecuteCancel);
        }
        #endregion

        #region 命令执行
        private void ExecuteLogin()
        {
            if (string.IsNullOrWhiteSpace(UserName))
            {
                ErrorMessage = "请输入用户名";
                HasError = true;
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入密码";
                HasError = true;
                return;
            }

            var result = authService.Login(UserName, Password);
            if (!result.success)
            {
                ErrorMessage = result.message;
                HasError = true;
                LoginCompleted?.Invoke(false);
                return;
            }

            LoginCompleted?.Invoke(true);
        }

        private void ExecuteCancel()
        {
            LoginCancel?.Invoke(true);

        }
        #endregion

        #region 公共方法
        /// <summary>
        /// 清除错误状态
        /// </summary>
        public void ClearError()
        {
            ErrorMessage = string.Empty;
            HasError = false;
        }
        #endregion
    }
}