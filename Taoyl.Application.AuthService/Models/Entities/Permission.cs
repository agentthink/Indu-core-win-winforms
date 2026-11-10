using SqlSugar;

namespace Taoyl.Application.AuthService.Models.Entities
{
    /// <summary>
    /// 权限实体（菜单权限 + 功能权限）
    /// </summary>
    [SugarTable("sys_permission", TableDescription = "权限表")]
    public class Permission
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(Length = 100, IsNullable = false)]
        public string Code { get; set; }

        [SugarColumn(Length = 100, IsNullable = false)]
        public string Name { get; set; }

        [SugarColumn(IsNullable = false)]
        public int Type { get; set; }

        [SugarColumn(Length = 100, IsNullable = true)]
        public string ParentCode { get; set; }

        [SugarColumn(IsNullable = false, DefaultValue = "0")]
        public int Sort { get; set; }

        [SugarColumn(IsNullable = true)]
        public int? Level { get; set; }

        [SugarColumn(Length = 100, IsNullable = true)]
        public string FormClass { get; set; }

        [SugarColumn(Length = 100, IsNullable = true)]
        public string DllName { get; set; }

        [SugarColumn(IsNullable = false, DefaultValue = "1")]
        public int IsActive { get; set; }
    }

}
