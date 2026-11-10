// 对应类库：Taoyl.UI.FormMain + Taoyl.UI.Pages
// 注入规则：窗体 = 属性注入（PropertiesAutowired）；VM 与服务（含 MenuManager）= 构造注入，
// 非窗体类型一律不加 PropertiesAutowired（Autofac 默认走参数最多的构造函数，挂上=冗余残留）。
// 页面注册统一走 RegisterPage<T>：Named 名 = typeof(T).FullName，与 Menu.json 的 DllName + Class 拼接规则一致。
using Autofac;
using System.Windows.Forms;
using Taoyl.Application.Configuration.Models;
using Taoyl.UI.FormMain.Services;
using Taoyl.UI.FormMain.ViewModels;
using Taoyl.UI.Pages;
using Taoyl.UI.Pages.Components;
using Taoyl.UI.Pages.ViewModels;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 界面模块：注册主窗体、子窗体、ViewModel 及菜单管理器
    /// </summary>
    public class FormModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // === UI 服务 ===

            builder.RegisterType<UserActivityMonitor>()
                   .As<IUserActivityMonitor>()
                   .SingleInstance();

            builder.RegisterType<SunnyUIMessageBoxService>()
                   .As<IMessageBoxService>()
                   .SingleInstance();

            builder.RegisterType<NavigationService>()
                   .As<INavigationService>()
                   .SingleInstance();

            // === 菜单管理器 ===
            builder.RegisterType<MenuManager>()
                   .SingleInstance();

            // === ViewModels ===
            builder.RegisterType<MainViewModel>()
                   .SingleInstance();

            builder.RegisterType<MainPageViewModel>()
                   .SingleInstance();

            builder.RegisterType<LoginViewModel>()
                   .InstancePerDependency();

            builder.RegisterType<UserManagerViewModel>()
                  .SingleInstance();

            builder.RegisterType<UserEditViewModel>()
                  .InstancePerDependency();

            // 角色管理 VM
            builder.RegisterType<RoleManagerViewModel>()
                   .SingleInstance();

            // 参数设置 VM
            builder.RegisterType<DevSettingsVM>().SingleInstance();
            builder.RegisterType<BaseSettingsVM>().SingleInstance();
            builder.RegisterType<LogSettingsVM>().SingleInstance();
            builder.RegisterType<ParamSettingVM>().SingleInstance();
            builder.RegisterType<RecipeEditVM<MachineARecipeConfig>>().SingleInstance();

            // 审计留痕查询页 VM（构造注入：读侧 IAuditQueryService + IAuthService + IMessageBoxService
            // + ICsvService 导出 + 写侧 IAuditService + IUserSession 身份快照——导出审计记录本身要留痕）
            builder.RegisterType<AuditQueryViewModel>()
                   .SingleInstance();

            // 报警查询页 VM（构造注入：IAlarmService.Query 只读收口 + IAuthService 导出门禁
            // + IMessageBoxService + ICsvService 导出 + 写侧 IAuditService + IUserSession 身份快照——
            // 导出成功即留痕）
            builder.RegisterType<AlarmQueryViewModel>()
                   .SingleInstance();

            // 产品实时页 VM（构造注入：IEnumerable<IProductDataSource> 集合注入全部产品表——
            // DeviceManager 同范式「注册多个、一次全给」+ IMessageBoxService。新增产品表本页零改动）
            builder.RegisterType<ProductLiveViewModel>()
                   .SingleInstance();

            // 产品数据查询页 VM（构造注入：IEnumerable<IProductDataSource> 集合注入 + IAuthService 导出门禁
            // Btn:Product_Export + IMessageBoxService + 写侧 IAuditService + IUserSession 身份快照；
            // 服务端分页 + MiniExcel xlsx 导出，导出成功即留痕）
            builder.RegisterType<ProductQueryViewModel>()
                   .SingleInstance();

            // 报警弹窗 View + VM（视图瞬态、状态常驻）：
            // VM 单例持三通道策略字典（展示策略+BindingList 数据源+IsShowing 显示标记，跨瞬态窗体存活，防重复起窗）；
            // 窗体每次 Resolve 瞬态弹出，ctor 注入同一 VM，Bind(channel) 按通道策略绑定对应数据源
            builder.RegisterType<AlarmDialogViewModel>()
                   .SingleInstance();

            builder.RegisterType<FormAlarmDialog>()
                   .InstancePerDependency();

            // 报警弹窗呈现器（自足单例）：激活 = Bootstrapper.InitComponents 主线程显式 Resolve
            builder.RegisterType<AlarmPopupPresenter>()
                   .SingleInstance();

            // 流程监控页面 VM
            builder.RegisterType<ProcessMonitorViewModel>()
                   .SingleInstance();

            // 多语言配置页 VM（FrmResourceConfig 属性注入；构造注入 IResourceManager + IMessageBoxService）
            builder.RegisterType<ResourceConfigViewModel>()
                   .SingleInstance();

            // PLC 调试页面（纯运行时调试器；VM 构造注入 IDeviceOrchestration 编排宽口 + IPlcConfigManager + IAuthService + IMessageBoxService）
            builder.RegisterType<PlcDebugViewModel>()
                   .SingleInstance();

            // MES 调试页面 VM（构造注入 IMesConfigManager + IMesClient + IAuthService + IMessageBoxService）。
            // 🔴 必须注册：FormMesDebug 靠 PropertiesAutowired 注入它，漏了就是 ViewModel == null →
            // OnLoad 直接 return → 所有按钮都没绑事件，表现为「点了完全没反应」。
            builder.RegisterType<MesDebugViewModel>()
                   .SingleInstance();

            // === Views（主窗体单例）===
            builder.RegisterType<Taoyl.UI.FormMain.FormMain>()
                   .PropertiesAutowired()
                   .SingleInstance();

            // === 登录窗体（每次新建）===
            builder.RegisterType<Taoyl.UI.FormMain.FormLogin>()
                   .PropertiesAutowired()
                   .InstancePerDependency();

            builder.RegisterType<Taoyl.UI.Pages.FormUserEdit>()
                   .InstancePerDependency();

            // === 子窗体（Named 页面，统一走 RegisterPage<T>）===

            RegisterPage<Taoyl.UI.Pages.FormMainPage>(builder);

            // 产品实时数据页（通用两页之一；菜单节点 ProductLive Level=100，
            // 集合注入 IEnumerable<IProductDataSource>，新表自动进下拉）
            RegisterPage<Taoyl.UI.Pages.FormProductLive>(builder);

            // 产品数据查询页（通用两页之二；菜单节点 ProductQuery Level=100，
            // 导出独立权限码 Btn:Product_Export Level=300，故「能查 ≠ 能导」）
            RegisterPage<Taoyl.UI.Pages.FormProductQuery>(builder);

            RegisterPage<Taoyl.UI.Pages.FrmResourceConfig>(builder);

            // 流程监控页面
            RegisterPage<Taoyl.UI.Pages.FormProcessMonitor>(builder);

            RegisterPage<Taoyl.UI.Pages.FormPLCDebug>(builder);

            // MES 调试页（菜单节点 MesDebug Level=300；发送独立权限码 Btn:MesDebug_Send）
            RegisterPage<Taoyl.UI.Pages.FormMesDebug>(builder);

            // 用户管理
            RegisterPage<Taoyl.UI.Pages.FormUserManager>(builder);

            // 角色管理
            RegisterPage<Taoyl.UI.Pages.FormRoleManager>(builder);

            // 参数设置
            RegisterPage<Taoyl.UI.Pages.FormParamSetting>(builder);

            // 配方管理
            RegisterPage<Taoyl.UI.Pages.FormRecipeManager>(builder);

            // 审计留痕查询页（只读页；菜单节点 Level=700 ⇒ 仅管理员可见，
            // 导出为独立权限码 Btn:Audit_Export，故「能查 ≠ 能导」）
            RegisterPage<Taoyl.UI.Pages.FormAudit>(builder);

            // 报警查询页（只读页；一级菜单 Level=100，导出独立权限码 Btn:Alarm_Export Level=300）
            RegisterPage<Taoyl.UI.Pages.FormAlarmQuery>(builder);
        }

        /// <summary>
        /// 页面注册统一入口：Named 名 = 类型全名（typeof(T).FullName），
        /// 与 Menu.json 的 DllName（命名空间）+ Class（类名）拼接规则一致，
        /// 类型改名/挪命名空间时注册名自动跟随，杜绝手写字符串静默漂移
        /// </summary>
        private static void RegisterPage<TPage>(ContainerBuilder builder) where TPage : Form
        {
            builder.RegisterType<TPage>()
                   .Named<Form>(typeof(TPage).FullName)
                   .PropertiesAutowired()
                   .SingleInstance();
        }
    }
}
