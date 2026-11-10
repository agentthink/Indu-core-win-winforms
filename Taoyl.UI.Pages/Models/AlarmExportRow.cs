using CsvHelper.Configuration.Attributes;
using Taoyl.Application.Alarm.Models.Dtos;

namespace Taoyl.UI.Pages.Models
{
    /// <summary>报警导出行（CSV 中文表头）。</summary>
    public class AlarmExportRow
    {
        [Name("产生时间")]
        public string RaiseTime { get; set; }

        [Name("类型")]
        public string Type { get; set; }

        [Name("报警代码")]
        public string Code { get; set; }

        [Name("来源")]
        public string Source { get; set; }

        [Name("报警描述")]
        public string Message { get; set; }

        [Name("行动指南")]
        public string ActionGuide { get; set; }

        /// <summary>空 = 未处理</summary>
        [Name("处理人员")]
        public string HandleUser { get; set; }

        /// <summary>空 = 未处理</summary>
        [Name("处理时间")]
        public string HandleTime { get; set; }

        public static AlarmExportRow From(AlarmRecordDto r)
        {
            return new AlarmExportRow
            {
                RaiseTime = r.RaiseTime.ToString("yyyy-MM-dd HH:mm:ss"),
                Type = r.Type,
                Code = r.Code,
                Source = r.Source,
                Message = r.Message,
                ActionGuide = r.ActionGuide,
                HandleUser = r.HandleUser,
                HandleTime = r.HandleTime?.ToString("yyyy-MM-dd HH:mm:ss")
            };
        }
    }
}
