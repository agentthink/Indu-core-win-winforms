using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Taoyl.Application.AuthService.Interfaces;
using Taoyl.Application.Share.Config;

namespace Taoyl.Application.AuthService.Services
{
    /// <summary>
    /// 键盘钩子服务 - 用于刷卡器/扫码枪输入
    /// </summary>
    public class KeyboardHookService : ICardLoginProvider, IDisposable
    {
        #region Windows API

        [StructLayout(LayoutKind.Sequential)]
        private struct KeyBoardHookStruct
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public int dwExtraInfo;
        }

        
        private delegate IntPtr HookProc(int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, HookProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        #endregion

        #region 常量

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_SYSKEYDOWN = 0x0104;

        #endregion

        #region 字段和属性

        private readonly ICardLoginSettings settings;
        private IntPtr _hookID = IntPtr.Zero;
        private HookProc _proc;
        private string _currentCode = string.Empty;
        private DateTime _lastKeyTime = DateTime.MinValue;
        private readonly object _lock = new object();
        private bool _disposed = false;

        /// <summary>
        /// 是否启用刷卡登录（实时读配置）。
        /// </summary>
        public bool IsEnabled => settings.EnableCardLogin;

        /// <summary>
        /// 刷卡输入长度（实时读配置）。
        /// </summary>
        public int CardInputLength => settings.CardInputLength;

        /// <summary>
        /// 键盘输入超时时间（毫秒，实时读配置）。
        /// </summary>
        public int KeyInputTimeout => settings.KeyInputTimeout;

        #endregion

        #region 事件

        /// <summary>
        /// 刷卡读取完成事件
        /// </summary>
        public event Action<string> CardRead;

        #endregion

        #region 构造函数

        /// <summary>
        /// 构造键盘钩子服务，刷卡参数通过契约实时读取。
        /// </summary>
        public KeyboardHookService(ICardLoginSettings settings)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _proc = HookCallback;
        }

        #endregion

        #region 公共方法

        public void Start()=> StartHook();
        public void Stop() => StopHook();

        /// <summary>
        /// 启动键盘钩子
        /// </summary>
        public bool StartHook()
        {
            if (_hookID != IntPtr.Zero)
                return true; // 已经启动

            try
            {
                using (Process curProcess = Process.GetCurrentProcess())
                using (ProcessModule curModule = curProcess.MainModule)
                {
                    _hookID = SetWindowsHookEx(WH_KEYBOARD_LL, _proc,
                        GetModuleHandle(curModule.ModuleName), 0);
                }

                return _hookID != IntPtr.Zero;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"设置键盘钩子失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 停止键盘钩子
        /// </summary>
        public void StopHook()
        {
            if (_hookID != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_hookID);
                _hookID = IntPtr.Zero;
            }
        }

        /// <summary>
        /// 清空当前输入
        /// </summary>
        public void ClearInput()
        {
            lock (_lock)
            {
                _currentCode = string.Empty;
                _lastKeyTime = DateTime.MinValue;
            }
        }

        #endregion

        #region 私有方法

        /// <summary>
        /// 键盘钩子回调函数 - 修正参数类型
        /// </summary>
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                // 只处理按键按下事件
                int wParamValue = wParam.ToInt32();
                if (wParamValue == WM_KEYDOWN || wParamValue == WM_SYSKEYDOWN)
                {
                    int vkCode = Marshal.ReadInt32(lParam);

                    // 获取按键字符（参考KeyPress_o的逻辑）
                    string keyChar = GetCharFromKey(vkCode);
                    if (!string.IsNullOrEmpty(keyChar))
                    {
                        ProcessKeyInput(keyChar);
                    }
                }
            }

            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        /// <summary>
        /// 处理按键输入
        /// </summary>
        private void ProcessKeyInput(string keyChar)
        {
            lock (_lock)
            {
                DateTime now = DateTime.Now;

                // 检查是否需要重置（超过超时时间）
                if ((now - _lastKeyTime).TotalMilliseconds > KeyInputTimeout)
                {
                    _currentCode = string.Empty;
                }

                // 累积字符
                _currentCode += keyChar;
                _lastKeyTime = now;

                // 达到指定长度触发事件
                if (_currentCode.Length >= CardInputLength)
                {
                    string completeCode = _currentCode;
                    _currentCode = string.Empty; // 重置

                    // 触发事件（异步执行，避免阻塞钩子线程）
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        CardRead?.Invoke(completeCode);
                    });
                }
            }
        }

        /// <summary>
        /// 从虚拟键码获取字符
        /// </summary>
        private string GetCharFromKey(int vkCode)
        {
            try
            {
                // 处理数字键（主键盘0-9）
                if (vkCode >= 48 && vkCode <= 57)
                {
                    return ((char)vkCode).ToString();
                }

                // 处理数字键（小键盘0-9）
                if (vkCode >= 96 && vkCode <= 105)
                {
                    return (vkCode - 96).ToString();
                }

                // 处理字母键（A-Z）
                if (vkCode >= 65 && vkCode <= 90)
                {
                    // 检查Shift键状态
                    bool shiftPressed = (GetAsyncKeyState(0x10) & 0x8000) != 0;
                    char baseChar = (char)('a' + (vkCode - 65));
                    return shiftPressed ? char.ToUpper(baseChar).ToString() : baseChar.ToString();
                }

                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        [DllImport("user32.dll")]
        private static extern short GetAsyncKeyState(int vKey);

        #endregion

        #region IDisposable实现

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                }

                StopHook();

                _disposed = true;
            }
        }

        ~KeyboardHookService()
        {
            Dispose(false);
        }

        #endregion
    }
}