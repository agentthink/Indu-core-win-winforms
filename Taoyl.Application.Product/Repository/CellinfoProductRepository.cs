using Taoyl.Application.DataBase;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Models.Entities;

namespace Taoyl.Application.Product.Repository
{
    /// <summary>
    /// 电芯注液产品仓储实现：当前完全复用泛型基类，无特殊数据访问逻辑。
    /// 将来的特殊操作（批量 / 事务 / 存储过程）在此追加，基类保持通用。
    /// </summary>
    public class CellinfoProductRepository : ProductRepository<ProductCellInfo>, ICellinfoProductRepository
    {
        public CellinfoProductRepository(SqlSugarHelper dbHelper) : base(dbHelper)
        {
        }
    }
}
