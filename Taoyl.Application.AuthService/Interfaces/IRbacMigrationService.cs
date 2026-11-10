namespace Taoyl.Application.AuthService.Interfaces
{
    /// <summary>
    /// RBAC 整机迁移服务：跨机导出 / 导入（含 Menu 指纹强校验与失败自动恢复）。
    /// 以 RoleName / Code 为锚点，不依赖自增 ID。
    /// </summary>
    public interface IRbacMigrationService
    {
        /// <summary>导出角色（含手动权限 Code）+ 用户（含角色名）到 JSON 包；失败返回 false。</summary>
        bool ExportUsers(string filePath);

        /// <summary>
        /// 导入 RBAC 包：备份 → 指纹校验 → 清库重建 → 名称锚点重建 → 强制注销；
        /// 异常自动从备份恢复。未匹配项经 errorMsg 返回（不阻断导入）。
        /// </summary>
        bool ImportUsers(string filePath, out string errorMsg);
    }

}
