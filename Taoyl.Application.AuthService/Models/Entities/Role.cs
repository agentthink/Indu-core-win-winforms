using SqlSugar;

namespace Taoyl.Application.AuthService.Models.Entities
{
    /// <summary>
    /// 角色实体
    /// </summary>
    [SugarTable("sys_role", TableDescription = "角色表")]
    public class Role
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true)]
        public int Id { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "角色")]
        public string RoleName { get; set; }

        [SugarColumn(IsNullable = false, ColumnDescription = "角色等级")]
        public int Level { get; set; }

        [SugarColumn(IsNullable = false, DefaultValue = "0", ColumnDescription = "是否内置")]
        public int IsBuiltIn { get; set; }

        [SugarColumn(Length = 200, IsNullable = true, ColumnDescription = "备注")]
        public string Remark { get; set; }
    }

}
