using System;
using System.Collections;
using System.Collections.Generic;
using Mapster;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Models;
using Taoyl.Application.Product.Models.Dtos;
using Taoyl.Application.Product.Models.Entities;
using Taoyl.Application.Product.Query;

namespace Taoyl.Application.Product.Services
{
    /// <summary>
    /// 电芯注液产品服务实现（单例）。
    /// 职责三件：① 元数据声明（Key/Title/TimeField/Columns）；② 筛选条件翻译 + 排序字段决策；
    /// ③ DTO ↔ Entity 映射。数据访问全部交给具体仓储，本类不碰 SqlSugar、不构表达式树。
    /// 写路径同步直写（每件一条频率低，拖节拍再套队列——接口不变，调用方零改动）；
    /// 读侧上抛（查询失败必须让 UI 看见，照报警读侧规则）。
    /// </summary>
    public class ProductCellInfoService : IProductCellInfoService, IProductDataSource
    {
        private readonly ICellinfoProductRepository repository;

        public ProductCellInfoService(ICellinfoProductRepository repository)
        {
            this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        #region IProductDataSource 元数据

        public string Key => "product_cellinfo";

        public string Title => "电芯注液信息";

        public string TimeField => nameof(ProductCellInfoDto.EntryTime);

        public IReadOnlyList<ProductColumn> Columns => ProductSchema.Of<ProductCellInfoDto>();

        #endregion

        #region IProductCellInfoService（流程子类）

        public int Insert(ProductCellInfoDto dto)
        {
            if (dto == null) return 0;
            return repository.Insert(dto.Adapt<ProductCellInfo>());
        }

        public int Update(ProductCellInfoDto dto)
        {
            if (dto == null) return 0;
            return repository.Update(dto.Adapt<ProductCellInfo>());
        }

        public int DeleteByKeyName(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return 0;
            return repository.DeleteWhere(r => r.KeyName == keyName);
        }

        public ProductCellInfoDto GetByKeyName(string keyName)
        {
            if (string.IsNullOrWhiteSpace(keyName)) return null;

            var entity = repository.QueryFirst(r => r.KeyName == keyName);
            return entity == null ? null : entity.Adapt<ProductCellInfoDto>();
        }

        #endregion

        #region IProductDataSource 查询（条件翻译 + 排序字段决策 + 映射）

        IList IProductDataSource.QueryLatest(int take, string sortField, bool descending)
        {
            // 「取消排序」态回退时间列倒序，方向钉死倒序（不随页面方向选择泄漏）
            var field = string.IsNullOrEmpty(sortField) ? TimeField : sortField;
            var effectiveDescending = string.IsNullOrEmpty(sortField) || descending;

            return repository.QueryTop(null, field, effectiveDescending, take)
                             .Adapt<List<ProductCellInfoDto>>();
        }

        IList IProductDataSource.QueryPage(ProductQuerySpec spec, int pageIndex, int pageSize, out int total)
        {
            var filter = ProductExpressionBuilder.BuildFilter<ProductCellInfo>(spec, TimeField);

            return repository.QueryPage(filter, TimeField, true, pageIndex, pageSize, out total)
                             .Adapt<List<ProductCellInfoDto>>();
        }

        #endregion
    }
}
