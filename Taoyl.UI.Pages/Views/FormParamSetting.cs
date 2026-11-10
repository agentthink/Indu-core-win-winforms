using Sunny.UI;
using System;
using Taoyl.UI.FormMain.Helpers;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.UI.Pages
{
    public partial class FormParamSetting : UIPage
    {
        public ParamSettingVM ViewModel { get; set; }

        public FormParamSetting() 
        { 
            InitializeComponent();
            UiDpi.EnableDoubleBuffered(this);
            tabMain.ItemSize = new System.Drawing.Size(UiDpi.Scale(200), UiDpi.Scale(40));
        }

        // ============ 生命周期 ============
        protected override void OnLoad(EventArgs e)
        {
            if (ViewModel == null) throw new InvalidOperationException("ParamSettingVM 未注入");
            UiDpi.SetRedraw(this, false);
            base.OnLoad(e);

            BuildSystemBase();
            BindViewModel();
            BridgeEvents();
        }
        /// <summary>
        /// 首帧显示完成（Form.Shown 每实例仅触发一次，与 OnLoad 配对）：WinForms 保证 OnShown 前
        /// 窗口定位/尺寸消息（WM_SIZE/WM_WINDOWPOSCHANGED）已处理完毕——此处恢复禁绘 + 整页重绘，
        /// 终态一次成帧。缓存复用切回（Remove→Add→Show）不重走 Load/Shown，本恢复不重复执行。
        /// </summary>
        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            UiDpi.SetRedraw(this, true);
            Invalidate(true);
        }
        // ==================== 系统/基础 ====================

        private void BuildSystemBase()
        {
            ConfigFormRenderer.Render(pnlSystemContent, ConfigFlatRow.Flatten(ViewModel.DevSettings.Config));
            ConfigFormRenderer.SetReadOnly(pnlSystemContent, readOnly: true);

            ConfigFormRenderer.Render(pnlBaseContent, ConfigFlatRow.Flatten(ViewModel.BaseSettings.Config));
            ConfigFormRenderer.SetReadOnly(pnlBaseContent, readOnly: true);

            ConfigFormRenderer.Render(pnlLogContent, ConfigFlatRow.Flatten(ViewModel.LogSettings.Config));
            ConfigFormRenderer.SetReadOnly(pnlLogContent, readOnly: true);
        }

        private void RebuildSystem()
        {
            ConfigFormRenderer.Render(pnlSystemContent, ConfigFlatRow.Flatten(ViewModel.DevSettings.Config));
            ConfigFormRenderer.SetReadOnly(pnlSystemContent, readOnly: !ViewModel.DevSettings.IsEditing);
        }
        private void RebuildBase()
        {
            ConfigFormRenderer.Render(pnlBaseContent, ConfigFlatRow.Flatten(ViewModel.BaseSettings.Config));
            ConfigFormRenderer.SetReadOnly(pnlBaseContent, readOnly: !ViewModel.BaseSettings.IsEditing);
        }
        private void RebuildLog()
        {
            ConfigFormRenderer.Render(pnlLogContent, ConfigFlatRow.Flatten(ViewModel.LogSettings.Config));
            ConfigFormRenderer.SetReadOnly(pnlLogContent, readOnly: !ViewModel.LogSettings.IsEditing);
        }

        // ==================== 数据绑定 ====================

        private void BindViewModel()
        {
            btnSystemEdit.DataBindings.Add("Enabled", ViewModel.DevSettings, nameof(DevSettingsVM.CanEdit));
            btnSystemSave.DataBindings.Add("Enabled", ViewModel.DevSettings, nameof(DevSettingsVM.CanSave));
            btnSystemCancel.DataBindings.Add("Enabled", ViewModel.DevSettings, nameof(DevSettingsVM.CanCancel));
            btnSystemReset.DataBindings.Add("Enabled", ViewModel.DevSettings, nameof(DevSettingsVM.IsEditing));
            btnBaseEdit.DataBindings.Add("Enabled", ViewModel.BaseSettings, nameof(BaseSettingsVM.CanEdit));
            btnBaseSave.DataBindings.Add("Enabled", ViewModel.BaseSettings, nameof(BaseSettingsVM.CanSave));
            btnBaseCancel.DataBindings.Add("Enabled", ViewModel.BaseSettings, nameof(BaseSettingsVM.CanCancel));
            btnBaseReset.DataBindings.Add("Enabled", ViewModel.BaseSettings, nameof(BaseSettingsVM.IsEditing));
            btnLogEdit.DataBindings.Add("Enabled", ViewModel.LogSettings, nameof(LogSettingsVM.CanEdit));
            btnLogSave.DataBindings.Add("Enabled", ViewModel.LogSettings, nameof(LogSettingsVM.CanSave));
            btnLogCancel.DataBindings.Add("Enabled", ViewModel.LogSettings, nameof(LogSettingsVM.CanCancel));
            btnLogReset.DataBindings.Add("Enabled", ViewModel.LogSettings, nameof(LogSettingsVM.IsEditing));
        }

        // ==================== 事件桥接 ====================

        private void BridgeEvents()
        {
            btnSystemEdit.Click   += (s, e) => { ViewModel.DevSettings.EditCommand.Execute(null);   RebuildSystem(); };
            btnSystemSave.Click   += (s, e) => { ViewModel.DevSettings.SaveCommand.Execute(null);   RebuildSystem(); };
            btnSystemCancel.Click += (s, e) => { ViewModel.DevSettings.CancelCommand.Execute(null); RebuildSystem(); };
            btnSystemReset.Click  += (s, e) => { ViewModel.DevSettings.ResetCommand.Execute(null);  RebuildSystem(); };
            btnBaseEdit.Click     += (s, e) => { ViewModel.BaseSettings.EditCommand.Execute(null);   RebuildBase(); };
            btnBaseSave.Click     += (s, e) => { ViewModel.BaseSettings.SaveCommand.Execute(null);   RebuildBase(); };
            btnBaseCancel.Click   += (s, e) => { ViewModel.BaseSettings.CancelCommand.Execute(null); RebuildBase(); };
            btnBaseReset.Click    += (s, e) => { ViewModel.BaseSettings.ResetCommand.Execute(null);  RebuildBase(); };
            btnLogEdit.Click      += (s, e) => { ViewModel.LogSettings.EditCommand.Execute(null);    RebuildLog(); };
            btnLogSave.Click      += (s, e) => { ViewModel.LogSettings.SaveCommand.Execute(null);    RebuildLog(); };
            btnLogCancel.Click    += (s, e) => { ViewModel.LogSettings.CancelCommand.Execute(null);  RebuildLog(); };
            btnLogReset.Click     += (s, e) => { ViewModel.LogSettings.ResetCommand.Execute(null);   RebuildLog(); };
        }
    }
}
