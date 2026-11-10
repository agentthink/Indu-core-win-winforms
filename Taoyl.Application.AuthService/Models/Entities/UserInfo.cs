using SqlSugar;

namespace Taoyl.Application.AuthService.Models.Entities
{
    /// <summary>
    /// 系统用户落库实体
    /// </summary>
    [SugarTable("sys_users", TableDescription = "系统用户表")]
    public class UserInfo
    {
        [SugarColumn(IsPrimaryKey = true, IsIdentity = true, ColumnDescription = "用户ID")]
        public int UserId { get; set; }

        [SugarColumn(Length = 50, IsNullable = false, ColumnDescription = "用户名")]
        public string Username { get; set; }

        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "工号")]
        public string UserNumber { get; set; }

        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "卡号")]
        public string CardCode { get; set; }

        [SugarColumn(Length = 30, IsNullable = true, ColumnDescription = "最后登录时间")]
        public string LastLoginTime { get; set; }

        [SugarColumn(Length = 50, IsNullable = true, ColumnDescription = "添加人")]
        public string AddPeople { get; set; }

        [SugarColumn(Length = 100, IsNullable = false, ColumnDescription = "密码")]
        public string Password { get; set; }

        [SugarColumn(Length = 30, IsNullable = true, ColumnDescription = "创建时间")]
        public string CreateTime { get; set; }
    }

}
