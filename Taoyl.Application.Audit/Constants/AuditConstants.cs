using System.ComponentModel;

namespace Taoyl.Application.Audit.Constants
{
    /// <summary>
    /// 审计页面维度——**语义 = 权限码里的"页面"**：
    /// 值（[Description]）与 PermissionCodes.Page* 同构（用户管理 / 角色管理 / 配方管理 / PLC调试 …），
    /// 回答"这个动作发生在哪个页面"。审计页的筛选与展示一律叫"页面"，不再叫"域 / 作用域"。
    ///
    /// 唯一例外是 <see cref="System"/>：登录 / 登出 / 登录失败发生在登录窗体，没有菜单页面可归，
    /// 故保留"系统"作为**跨页面的会话类**归属。
    ///
    /// 🔴 **红线：成员的 [Description] 文案即存储契约**——落库、筛选、导出一律用它，
    /// 一经投产**不得修改**（改文案 = 改数据语义：同一列出现两种说法 ⇒ 筛选与统计直接错乱）。
    /// 变更流程只有一条：新增成员。文案的非空与列宽由 <see cref="AuditEnumText"/> 在静态构造期断言。
    ///
    /// 为什么是 enum 而不是 const string（2026-09-18 复核，**推翻**更早的"定稿，勿回退为 enum"）：
    /// 旧决断列了 4 条理由，逐条复核结果：
    /// ① "枚举存 int ⇒ 库不可读 ⇒ 要再维护一份值↔文案映射表" —— **已被本方案消解**：
    ///    落库的是 [Description] 文案本身（不是 int），库依旧人眼可读、Excel 直开可读，也就不需要映射表；
    /// ② "审计零计算参与，数值编码优势用不上" —— 仍成立，但它是"不构成反对"，不是"支持 const string"；
    /// ③ "改文案不动作值对历史事实是负资产" —— **仍然成立，且在新方案下更严重**（文案就是存储值）
    ///    ⇒ 已升级为本注释顶部的红线；
    /// ④ "项目已有 PermissionCodes 用 const string 的先例" —— **弱化**：权限码是**外部契约**
    ///    （要进 Menu.json、落 sys_permissions 表、与现场现有配置对齐），审计维度是**进程内词汇**，
    ///    两者对"外部可改"的要求不同级。
    ///
    /// 换来的新收益（**本次改型的决定性理由**）：编译期类型安全。
    /// const string 时代 AuditEntry(category, action, …) 两个参数写反**不报错**——照样编译、照样落库，
    /// 只是那一行记录永远查不出来。enum 化把这个盲区直接消灭。
    ///
    /// 成员值显式赋号锁定顺序：下拉与展示顺序 = 声明顺序 = 值序（页面在前、跨页面会话在后，勿随意调整）。
    /// </summary>
    public enum AuditCategory
    {
        /// <summary>用户管理页：用户 CRUD / 角色分配（Page:UserManager）</summary>
        [Description("用户管理")] User = 1,

        /// <summary>角色管理页：角色与授权（Page:RoleManager）</summary>
        [Description("角色管理")] Role = 2,

        /// <summary>参数设置页：系统参数 / 基础参数落盘（Page:ParamSetting）</summary>
        [Description("参数设置")] Config = 3,

        /// <summary>PLC 调试页：连接 / 地址表 / 读写测试（Page:PLCDebug）</summary>
        [Description("PLC调试")] Device = 4,

        /// <summary>配方管理页：配方保存 / 切换 / 导入 / 导出（Page:RecipeManager）</summary>
        [Description("配方管理")] Recipe = 5,

        /// <summary>流程监控页：流程动作（Page:FormProcessMonitor）</summary>
        [Description("流程监控")] Process = 6,

        /// <summary>审计记录页：审计数据导出（Page:Audit）</summary>
        [Description("审计记录")] Audit = 7,

        /// <summary>产品数据页：导出 xlsx（数据出库，Page:DataQuery）</summary>
        [Description("产品数据")] Product = 8,

        /// <summary>报警查询页：导出 CSV（数据出库，Page:AlarmQuery）</summary>
        [Description("报警")] Alarm = 9,

        /// <summary>
        /// 主界面：全局流程启停复位（批量作用于全部流程）。
        /// 没有菜单页面码——这三个按钮长在主窗体上、不经页面导航，故 <c>Page:*</c> 无对应项，
        /// 守护它们的权限码是 <c>Btn:Process_Start/Pause/Reset</c>。
        /// </summary>
        [Description("主界面")] Main = 10,

        /// <summary>
        /// 多语言配置页：翻译资源的保存 / 导入 / 导出 / 提取（Page:TranslateViewer）。
        /// 该页在 Menu.json 中**只有页面码、没有按钮码**，其命令体不做权限门禁
        /// （与 PLC 调试页等"能进页面即能操作"的页面同规则），故此处的命令一律成功即留痕。
        /// </summary>
        [Description("多语言配置")] Resource = 11,

        /// <summary>
        /// MES 调试页：接口配置的新增 / 保存 / 删除（Page:MesDebug）。
        /// ⚠️ 只覆盖界面路径——手改 Mes.json 不产生记录（审计落点 = UI 命令层，
        /// 驱动层零依赖、结构上不知道审计存在）。
        /// </summary>
        [Description("MES调试")] Mes = 12,

        /// <summary>
        /// 系统（跨页面）：登录 / 登出 / 登录失败——发生在登录窗体，无菜单页面可归。
        /// ⚠️ 值 13（2026-11-05 由 11 上调：新增「产品数据」「报警」两个页面分类占 8、9；
        /// 更早由 10 上调、原为 8）：下拉项由 <c>Enum.GetValues</c> 按**值序**填充，
        /// 而本类的排序约定是"页面在前、跨页面会话在后"——新页面插进来后，
        /// 若本项仍保持原值就会被夹在页面中间。<c>值不落库</c>（落库的是 [Description] 文案），故上调零数据影响。
        /// </summary>
        [Description("系统")] System = 13,
    }

    /// <summary>
    /// 审计命令级动词——**语义 = 操作者点下的那个按钮 / 发起的那个命令**。
    ///
    /// 与权限码刻意**不合码**（也不由权限码派生），三条理由：
    /// 1. 权限码是 RBAC 资产，管理员可增删改并（数据驱动）；审计是历史事实，数年不变。
    ///    合码 ⇒ 管理员改一次码，历史记录就变成"未知动作"。
    /// 2. 存在**无码动作**：登录 / 登出 / 登录失败——发生在登录窗体，不经命令层门禁，没有按钮码可挂。
    /// 3. 审计零计算参与（不 join 权限表、不据码分支），合码拿不到任何收益。
    ///    （权限码答"谁被允许"，审计动词答"谁做了什么"，本就是两个维度：同一个"保存"动词五个页面都有，
    ///     却各归各的页面码——合码只会立刻丢掉动作粒度。）
    ///
    /// **门禁被拒不产生任何动词**：命令体首行就被拦下、没碰任何数据，记进审计只会混进一批
    /// "什么都没发生"的行（现场约定）。
    ///
    /// 但**可追溯性必须建**：每个挂点本身就写在 `if (!HasPermission(码))` 的守护体内，
    /// 所以"动作 ↔ 权限码"天然一一对应；需要反查时按本类动词 grep 挂点即可定位。
    /// </summary>
    public enum AuditAction
    {
        /// <summary>新建对象</summary>
        [Description("新建")] Add = 1,

        /// <summary>修改对象</summary>
        [Description("修改")] Update = 2,

        /// <summary>删除对象</summary>
        [Description("删除")] Delete = 3,

        /// <summary>保存（配置节 / 配方 / 设备 / 地址表整体落盘）</summary>
        [Description("保存")] Save = 4,

        /// <summary>导入（覆盖或新建）</summary>
        [Description("导入")] Import = 5,

        /// <summary>导出（数据出库）</summary>
        [Description("导出")] Export = 6,

        /// <summary>
        /// 进入编辑态。🔴 **已停用新增记录（2026-09-20）**：进编辑态此刻盘 / 缓存 / 实例零变化，
        /// 真正的账由落盘动作（Save / Add / Update）来记——"编辑"不再产出新记录。
        /// 成员保留：历史库已落"编辑"文案，删成员会让旧数据读不出语义。
        /// </summary>
        [Description("编辑")] Edit = 7,

        /// <summary>恢复默认值（配置页回默认值 / 角色权限重置）；是否落盘决定有无明细</summary>
        [Description("重置")] Reset = 8,

        /// <summary>切换生效对象（配方切换——切错配方直接报废，现场高危动作）</summary>
        [Description("切换")] Switch = 9,

        /// <summary>PLC 标签值读取测试（明细留空——读不改数据，记值即刷屏）</summary>
        [Description("读标签")] ReadTag = 10,

        /// <summary>PLC 标签值写入测试</summary>
        [Description("写标签")] WriteTag = 11,

        /// <summary>PLC 连接</summary>
        [Description("连接")] Connect = 12,

        /// <summary>PLC 断开</summary>
        [Description("断开")] Disconnect = 13,

        /// <summary>PLC 实例重建（释放并重建，不改配置不落盘）</summary>
        [Description("重建")] Rebuild = 14,

        /// <summary>登录成功（手动与刷卡同一动作）</summary>
        [Description("登录")] Login = 15,

        /// <summary>登出（手动与超时同一动作——都是会话结束）</summary>
        [Description("登出")] Logout = 16,

        /// <summary>登录失败（用户名或密码错误 / 卡号无效）</summary>
        [Description("登录失败")] LoginFailed = 17,

        /// <summary>分配角色（当前无挂点：用户改角色走 Update + 明细行"角色 旧→新"，保留为预留词汇）</summary>
        [Description("分配角色")] AssignRoles = 18,

        /// <summary>
        /// 流程启动 / 继续（主界面批量启动与流程监控单流程启动共用）。
        /// 「继续」（Paused → Running）**不另立动词**：它与"启动"共用同一个按钮、同一个权限码
        /// （<c>Btn:Process_Start</c> / <c>Btn:ProcessMonitor_Start</c>），按钮文本随状态在"启动/继续"间切换
        /// ——按本类语义"动词 = 操作者点下的那个按钮"，两者本就是同一个动作。
        /// </summary>
        [Description("启动")] Start = 19,

        /// <summary>流程暂停（Running → Paused）</summary>
        [Description("暂停")] Pause = 20,

        /// <summary>
        /// 流程复位（Exception → Paused，仅消除异常、不自动继续）。
        /// ⚠️ 与 <see cref="Reset"/>（"重置"= 恢复默认值，改的是**数据**）**不是一回事**——
        /// 本动词改的是**运行态**。两者文案也不同（"流程复位" / "重置"），落库可分辨。
        /// </summary>
        [Description("流程复位")] ResetProcess = 21,

        /// <summary>
        /// 流程初始化（单个流程 / 整机）；两者 Target 区分（流程名 / "整机"）。
        /// ⚠️ 与 <see cref="Rebuild"/>（PLC 实例重建）无关——那个重建的是设备连接实例。
        /// </summary>
        [Description("初始化")] Initialize = 22,

        /// <summary>
        /// 流程启用 / 停用切换，明细行记"启用 否→是"。
        /// 为什么不复用 <see cref="Update"/>：Update 在本仓是"改某个对象的字段"（配置 / 用户 / 角色），
        /// 而 Toggle 是"翻一个流程的运行开关"、改的是内存态配置而非落盘配置对象，
        /// 复用会让审计页的"修改"列混进一批看不出改了什么的对象。
        /// </summary>
        [Description("启用停用")] ToggleEnabled = 23,

        /// <summary>流程异常时是否连带暂停其他流程的开关切换，明细行记"异停 是→否"</summary>
        [Description("异停设置")] TogglePauseOthers = 24,

        /// <summary>
        /// 从已打开的窗体实例提取 UI 资源（多语言配置页）。
        /// 提取**只进内存缓存、不落盘**（落盘另有"保存"一条），故与 <see cref="Save"/> 是两回事：
        /// 复用 Update 会让"改了翻译"与"抓了原文"在审计表里分不清。
        /// </summary>
        [Description("提取")] Extract = 25,
    }

    /// <summary>
    /// 审计结果——**只有成功 / 失败两态**（对齐威纶通等主流 HMI 的操作记录设计）。
    ///
    /// 为什么不要"拒绝 / 被拒"这一态：被拒不是与成功 / 失败并列的**结果**，而是"这次动作没成功"的**原因**；
    /// 把原因塞进结果维度，会让这一列同时承担两件事，筛选与统计立刻变糊。
    /// 而且门禁被拒**不产生业务动作**（命令体首行就被拦下、没碰任何数据）——记进来只会让审计表
    /// 混进一批"什么都没发生"的行。故门禁拒绝一律不留痕（现场约定），本维度只回答"这次动作做成了没有"。
    ///
    /// 失败 = 业务执行确实失败（返回 false / 抛业务异常被捕获）。异常一路上抛到 UI 的场景零记录
    /// （见 IAuditService 铁律：宁漏记不记假账）。
    /// </summary>
    public enum AuditResult
    {
        /// <summary>成功</summary>
        [Description("成功")] Success = 1,

        /// <summary>失败（业务执行失败——返回 false / 抛业务异常被捕获）</summary>
        [Description("失败")] Fail = 2,
    }

    /// <summary>
    /// 明细变更类型——回答"数据发生了什么"（与 <see cref="AuditAction"/> 回答"做了什么动作"分属两层，勿混）。
    /// 例：PLC 写标签 = Action「写标签」+ ChangeType「修改」，不是"写入"。
    /// </summary>
    public enum AuditChangeType
    {
        /// <summary>新增字段/元素（OldValue 为空）</summary>
        [Description("新增")] Add = 1,

        /// <summary>值变更（OldValue 可知时填写，不可知则留空——见 AuditFieldChange.OldValue 约定）</summary>
        [Description("修改")] Update = 2,

        /// <summary>删除字段/元素（NewValue 为空）</summary>
        [Description("删除")] Delete = 3,
    }
}
