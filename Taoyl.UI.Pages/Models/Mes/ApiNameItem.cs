namespace Taoyl.UI.Pages.Models.Mes
{
    /// <summary>
    /// 左栏接口列表的行。
    /// 🔴 必须存在：DGV 的列绑定要「属性名」，直接绑 BindingList&lt;string&gt; 时，
    /// string 唯一可绑的属性是 Length，单元格会显示名字长度（已踩）。
    /// </summary>
    public sealed class ApiNameItem
    {
        public ApiNameItem(string name, string displayName)
        {
            Name = name;
            DisplayName = displayName;
        }

        /// <summary>接口名 = MesApiDefinition.Name，程序 Invoke 用的技术键。</summary>
        public string Name { get; }

        /// <summary>接口描述 = MesApiDefinition.DisplayName，只给人看，允许为空。</summary>
        public string DisplayName { get; }
    }
}
