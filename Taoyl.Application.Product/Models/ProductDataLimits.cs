namespace Taoyl.Application.Product.Models
{
    /// <summary>
    /// 行数上限常量（两页面与服务共用的唯一真相源）。
    /// 实时页 500 默认 / 1000 封顶：500 行 × 55 列每次刷新 ≈ 450KB 分配，1 秒一次可忽略；
    /// 3000 行 × 1 秒进入 Gen0 敏感区，故实时页封顶压到 1000。
    /// 查询页服务端分页，内存只驻留单页，3000 为单页/导出封顶。
    /// </summary>
    public static class ProductDataLimits
    {
        /// <summary>实时页默认取最新行数。</summary>
        public const int DefaultLiveTake = 500;

        /// <summary>实时页单次取行封顶（收在仓储实现里，不靠调用方自觉）。</summary>
        public const int MaxLiveTake = 1000;

        /// <summary>查询页单页行数封顶（照审计 MaxPageSize 规则：收在仓储实现里）。</summary>
        public const int MaxQueryTake = 3000;

        /// <summary>查询页每页行数默认值（页码/页大小非法时的退路）。</summary>
        public const int DefaultQueryPageSize = 20;
    }
}
