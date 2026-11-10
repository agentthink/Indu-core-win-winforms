namespace Taoyl.Application.Share.Auth
{
    /// <summary>
    /// 权限码常量集中定义，避免散弹式硬编码
    /// 命名规则：Page_页面 / Btn_页面_功能
    /// </summary>
    public static class PermissionCodes
    {
        // 页面权限
        public const string PageMainPage = "Page:MainPage";
        public const string PageSystemInfo = "Page:SystemInfo";
        public const string PageDataHome = "Page:DataHome";

        // 数据查询页
        public const string PageDataQuery = "Page:DataQuery";
        public const string PageParamSetting = "Page:ParamSetting";
        public const string PageRecipeManager = "Page:RecipeManager";
        public const string PageSettings = "Page:Settings";
        public const string PageProcessMonitor = "Page:ProcessMonitor";
        public const string PageUserManager = "Page:UserManager";
        public const string PageRoleManager = "Page:RoleManager";
        public const string PageDeviceManager = "Page:DeviceManager";
        public const string PagePLCDebug = "Page:PLCDebug";
        public const string PageTranslateViewer = "Page:TranslateViewer";
        public const string PageAudit = "Page:Audit";

        // 主界面流程按钮
        public const string BtnProcessStart = "Btn:Process_Start";
        public const string BtnProcessPause = "Btn:Process_Pause";
        public const string BtnProcessReset = "Btn:Process_Reset";

        //流程监控页按钮
        public const string BtnProcessMonitorStart = "Btn:ProcessMonitor_Start";
        public const string BtnProcessMonitorPause = "Btn:ProcessMonitor_Pause";
        public const string BtnProcessMonitorReset = "Btn:ProcessMonitor_Reset";
        public const string BtnProcessMointorInit = "Btn:ProcessMonitor_Init";
        public const string BtnProcessMonitorInitAll = "Btn:ProcessMonitor_InitAll";
        public const string BtnProcessMonitorToggleEnabled = "Btn:ProcessMonitor_ToggleEnabled";
        public const string BtnProcessMonitorTogglePauseOthers = "Btn:ProcessMonitor_TogglePauseOthers";


        // 用户管理页按钮
        public const string BtnUserManagerAdd = "Btn:UserManager_Add";
        public const string BtnUserManagerEdit = "Btn:UserManager_Edit";
        public const string BtnUserManagerDelete = "Btn:UserManager_Delete";
        public const string BtnUserManagerImport = "Btn:UserManager_Import";
        public const string BtnUserManagerExport = "Btn:UserManager_Export";
        // 参数设置按钮
        public const string BtnParamSystem_Edit  = "Btn:ParamSystem_Edit";
        public const string BtnParamSystem_Save  = "Btn:ParamSystem_Save";
        public const string BtnParamSystem_Reset = "Btn:ParamSystem_Reset";
        public const string BtnParamBase_Edit    = "Btn:ParamBase_Edit";
        public const string BtnParamBase_Save    = "Btn:ParamBase_Save";
        public const string BtnParamBase_Reset   = "Btn:ParamBase_Reset";
        public const string BtnParamLog_Edit     = "Btn:ParamLog_Edit";
        public const string BtnParamLog_Save     = "Btn:ParamLog_Save";
        public const string BtnParamLog_Reset    = "Btn:ParamLog_Reset";

        // 配方管理按钮
        public const string BtnRecipe_New    = "Btn:Recipe_New";
        public const string BtnRecipe_Edit   = "Btn:Recipe_Edit";
        public const string BtnRecipe_Save   = "Btn:Recipe_Save";
        public const string BtnRecipe_Delete = "Btn:Recipe_Delete";
        public const string BtnRecipe_Switch = "Btn:Recipe_Switch";
        public const string BtnRecipe_Import = "Btn:Recipe_Import";
        public const string BtnRecipe_Export = "Btn:Recipe_Export";

        // 角色管理页按钮
        public const string BtnRoleManager_Add    = "Btn:RoleManager_Add";
        public const string BtnRoleManager_Save   = "Btn:RoleManager_Save";
        public const string BtnRoleManager_Delete = "Btn:RoleManager_Delete";
        public const string BtnRoleManager_Reset  = "Btn:RoleManager_Reset";

        // PLC 调试页按钮（**一命令一码**：菜单页 Level=300 可浏览，按钮码 Level=500 显式覆盖
        // ⇒ 技师能看不能动，对标流程监控页「页面 100 / 按钮 500」分工模型）。
        // 为什么拆到这个粒度：原先按动作域归并成 6 码（一个「连接配置管理」码罩住
        // 新增/编辑/保存/取消/删除/重建六个按钮），现场想给「能改配置但不许删设备」这类分工留口子时
        // 无处下手——归并码把权限粒度绑死在实现分组上，实现一分组变，权限模型跟着被迫变。
        // 取消类（CancelPlc / CancelTagEdit）也各自设码，取值与对应的「进入编辑」码同档：
        // 进得来就出得去，否则编辑态会被门禁卡死（PLC 页 VM 是单例，编辑态跨窗体存活）。

        // 连接配置
        public const string BtnPLCDebugAddPlc        = "Btn:PLCDebug_AddPlc";        // 新增连接配置
        public const string BtnPLCDebugEditPlc       = "Btn:PLCDebug_EditPlc";       // 编辑连接配置
        public const string BtnPLCDebugSavePlc       = "Btn:PLCDebug_SavePlc";       // 保存连接配置
        public const string BtnPLCDebugCancelPlc     = "Btn:PLCDebug_CancelPlc";     // 取消连接配置编辑
        public const string BtnPLCDebugDeletePlc     = "Btn:PLCDebug_DeletePlc";     // 删除连接配置
        public const string BtnPLCDebugRebuildPlc    = "Btn:PLCDebug_RebuildPlc";    // 重建设备实例

        // 连接与 IO
        public const string BtnPLCDebugConnect       = "Btn:PLCDebug_Connect";       // 连接设备
        public const string BtnPLCDebugDisconnect    = "Btn:PLCDebug_Disconnect";    // 断开设备
        public const string BtnPLCDebugRead          = "Btn:PLCDebug_Read";          // 读值测试
        public const string BtnPLCDebugWrite         = "Btn:PLCDebug_Write";         // 写值测试
        public const string BtnPLCDebugImport        = "Btn:PLCDebug_Import";        // 导入地址表
        public const string BtnPLCDebugExport        = "Btn:PLCDebug_Export";        // 导出地址表
        public const string BtnPLCDebugSaveTag       = "Btn:PLCDebug_SaveTag";       // 保存地址表

        // 地址表
        public const string BtnPLCDebugEditTag       = "Btn:PLCDebug_EditTag";       // 进入地址表编辑
        public const string BtnPLCDebugCancelTag     = "Btn:PLCDebug_CancelTag";     // 取消地址表编辑
        public const string BtnPLCDebugAddRow        = "Btn:PLCDebug_AddRow";        // 地址表新增行
        public const string BtnPLCDebugRemoveRow     = "Btn:PLCDebug_RemoveRow";     // 地址表删除行
        public const string BtnPLCDebugMoveUp        = "Btn:PLCDebug_MoveUp";        // 地址表行上移
        public const string BtnPLCDebugMoveDown      = "Btn:PLCDebug_MoveDown";      // 地址表行下移
        public const string BtnPLCDebugSetCurrentFreq = "Btn:PLCDebug_SetCurrentFreq"; // 设当前组刷新频率
        public const string BtnPLCDebugSetAllFreq    = "Btn:PLCDebug_SetAllFreq";    // 设全部组刷新频率

        // 审计留痕页按钮（数据出库独立授权：能查 ≠ 能导，故导出单独设码）
        public const string BtnAuditExport = "Btn:Audit_Export";              // 导出审计记录 CSV

        // 报警查询页按钮（数据出库独立授权，与审计同规则：能查 ≠ 能导）
        public const string BtnAlarmExport = "Btn:Alarm_Export";              // 导出报警记录 CSV

        // 产品数据查询页按钮（数据出库独立授权，与审计/报警同规则：能查 ≠ 能导）
        public const string BtnProductExport = "Btn:Product_Export";          // 导出产品数据 xlsx

        // MES 调试页按钮（页面 Level=300；与 PLC 调试同规则，按动作逐码而不是归并成一个「MES 配置管理」码）
        // 🔴 发送单独设码：保存只改本地 Mes.json，发送会真打到 MES 生产系统，风险不同级
        public const string BtnMesDebugAdd    = "Btn:MesDebug_Add";     // 新增接口
        public const string BtnMesDebugEdit   = "Btn:MesDebug_Edit";    // 进入编辑态
        public const string BtnMesDebugSave   = "Btn:MesDebug_Save";    // 保存接口配置
        public const string BtnMesDebugCancel = "Btn:MesDebug_Cancel";  // 取消编辑
        public const string BtnMesDebugDelete = "Btn:MesDebug_Delete";  // 删除接口
        public const string BtnMesDebugSend   = "Btn:MesDebug_Send";    // 发送请求
    }
}