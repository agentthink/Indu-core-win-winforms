using Taoyl.Application.Product.Models.Dtos;

namespace Taoyl.Application.Product.Interfaces
{
    /// <summary>
    /// 电芯注液产品服务：流程子类操作入口（DTO 进出，实体不出服务栈）。
    /// 增 / 删 / 改 / 查四件事：插入按条码补录，全部围绕电芯条码 KeyName 定位记录。
    /// 通用实时/查询页读法走 <see cref="IProductDataSource"/>，不在本接口。
    /// </summary>
    public interface IProductCellInfoService
    {
        /// <summary>插入一条采集记录（第一工位），返回受影响行数。</summary>
        int Insert(ProductCellInfoDto dto);

        /// <summary>按实体主键更新（后续工位补录字段），返回受影响行数。</summary>
        int Update(ProductCellInfoDto dto);

        /// <summary>按电芯条码删除，返回受影响行数。</summary>
        int DeleteByKeyName(string keyName);

        /// <summary>按电芯条码查单条；无匹配返回 null。</summary>
        ProductCellInfoDto GetByKeyName(string keyName);
    }
}
