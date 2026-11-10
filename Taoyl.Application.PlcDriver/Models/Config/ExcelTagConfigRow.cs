using MiniExcelLibs.Attributes;

namespace Taoyl.Application.PlcDriver.Models.Config
{
    /// <summary>
    /// Excel 地址表（PlcTagPolling_*.xlsx / 地址表模版.xlsx Sheet1）映射实体
    /// 15 列表头（2026-09-08 扩 3 列：组序号/组描述/标签描述；2026-09-09 列序重排：组属性列左置、子级+运行时列右随）；
    /// 列序 = 组序号、组名、组描述、组中序号、标签名称、标签描述、地址、数据类型、元素个数地址长度、
    ///        刷新频率(-1不刷新)、读写次数、值、最后读写时间、最后读写操作结果、最后读写信息
    /// 组属性（组序号/组描述）按组冗余：同组每行一致，加载取组首行；保存由程序从内存 TagGroup 统一写
    /// 加载按列名映射（[ExcelColumnName]），列声明顺序仅决定导出列序——存量旧文件列头名不变即可读，重排零兼容影响
    /// </summary>
    public class ExcelTagConfigRow
    {
        [ExcelColumnName("组序号")]
        public string GroupID { get; set; }

        [ExcelColumnName("组名")]
        public string GroupName { get; set; }

        [ExcelColumnName("组描述")]
        public string GroupDesc { get; set; }

        [ExcelColumnName("子序号")]
        public string TagID { get; set; }

        [ExcelColumnName("标签名称")]
        public string TagName { get; set; }

        [ExcelColumnName("标签描述")]
        public string TagDesc { get; set; }

        [ExcelColumnName("地址")]
        public string Address { get; set; }

        [ExcelColumnName("数据类型")]
        public string DataType { get; set; }

        [ExcelColumnName("元素个数地址长度")]
        public string Length { get; set; }

        [ExcelColumnName("刷新频率")]
        public string RefreshFreq { get; set; }

        [ExcelColumnName("读写次数")]
        public string RWTimes { get; set; }

        [ExcelColumnName("值")]
        public string Value { get; set; }

        [ExcelColumnName("最后读写时间")]
        public string LastOperationTime { get; set; }

        [ExcelColumnName("最后读写操作结果")]
        public string LastOperationResult { get; set; }

        [ExcelColumnName("最后读写信息")]
        public string LastOperationMsg { get; set; }
    }
}
