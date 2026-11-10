namespace Taoyl.Application.MesDriver.Models
{
    /// <summary>
    /// 单个名值对，同时用于请求头（Headers）、URL 查询参数（QueryParams）与表单正文（Form）。
    /// 用 List 而非 Dictionary 承载：保序（Postman 里头的顺序可见）、允许重名头、
    /// JSON 里可读性优于对象字面量。
    /// </summary>
    public class MesHeader
    {
        public string Name { get; set; }

        public string Value { get; set; }

        /// <summary>
        /// 说明文字，仅供调试界面展示（如「接口调用方认证ID」）。
        /// 🔴 驱动完全不读取本字段——它不参与发送、不参与合并、不影响任何行为。
        /// 存在的理由：19 个接口 × 每个若干参数，现场工程师靠键名记不住用途。
        /// </summary>
        public string Description { get; set; }

        public MesHeader()
        {
        }

        public MesHeader(string name, string value, string description = null)
        {
            Name = name;
            Value = value;
            Description = description;
        }

        public MesHeader Clone()
        {
            return new MesHeader(Name, Value, Description);
        }
    }
}
