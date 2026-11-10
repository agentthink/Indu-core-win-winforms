using System.Collections.Generic;
using System.Data;

namespace Taoyl.Application.FileUtil
{
    /// <summary>
    /// CSV 操作抽象。
    ///
    /// 归属说明：CSV 是通用文件能力（流程侧导出采集数据、UI 侧导出查询报表），
    /// 不属于流程基座（TaskProcess 已无此契约），故与实现一起留在 FileUtil。
    /// </summary>
    public interface ICsvService
    {
        void Records<T>(IEnumerable<T> data, string filePath);
        void Record<T>(T data, string filePath);

        IEnumerable<T> ReadAll<T>(string path);
        DataTable ReadAllToDataTable(string path);
        DataTable ReadAllToDataTable(string path, int maxRows);
    }
}
