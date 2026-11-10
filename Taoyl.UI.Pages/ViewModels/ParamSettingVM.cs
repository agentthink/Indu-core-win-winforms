using System;

namespace Taoyl.UI.Pages.ViewModels
{
    /// <summary>
    /// 参数设置壳 ViewModel，统一持有系统参数、基础参数、日志参数三个子 VM。
    /// </summary>
    public partial class ParamSettingVM
    {
        public DevSettingsVM DevSettings { get; }
        public BaseSettingsVM BaseSettings { get; }
        public LogSettingsVM LogSettings { get; }

        public ParamSettingVM(
            DevSettingsVM devSettings,
            BaseSettingsVM baseSettings,
            LogSettingsVM logSettings)
        {
            DevSettings    = devSettings    ?? throw new ArgumentNullException(nameof(devSettings));
            BaseSettings   = baseSettings   ?? throw new ArgumentNullException(nameof(baseSettings));
            LogSettings    = logSettings    ?? throw new ArgumentNullException(nameof(logSettings));
        }

        public void Dispose()
        {
            DevSettings.Dispose();
            BaseSettings.Dispose();
            LogSettings.Dispose();
        }
    }
}
