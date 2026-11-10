using Taoyl.Application.Product.Models.Entities;

namespace Taoyl.Application.Product.Interfaces
{
    /// <summary>
    /// 电芯注液产品仓储：当前无特殊数据访问需求，仅作为该产品的数据访问入口标识。
    /// 将来若出现批量插入 / 跨表事务 / 存储过程等超出通用 CRUD 的操作，直接在本接口追加方法，
    /// 服务层构造签名不变（依赖本接口而非泛型基接口）。
    /// </summary>
    public interface ICellinfoProductRepository : IProductRepository<ProductCellInfo>
    {
    }
}
