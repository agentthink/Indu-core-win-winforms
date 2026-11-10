// 对应类库：Taoyl.Application.Product
using Autofac;
using Taoyl.Application.Product.Interfaces;
using Taoyl.Application.Product.Repository;
using Taoyl.Application.Product.Services;

namespace Taoyl.IFALaunch.Modules
{
    /// <summary>
    /// 产品数据模块：具体产品仓储 + 服务（均单例）。
    /// 服务同时注册为 IProductDataSource——通用两页经 IEnumerable&lt;IProductDataSource&gt; 集合注入取全部表
    /// （DeviceManager 同范式：注册多个、一次全给）。
    /// 🔴 新增产品表 = Entity + DTO + 具体仓储接口/实现 + 服务接口 + 服务实现 + MappingRegister + 此处两行注册，
    /// 页面 / IProductDataSource / 泛型仓储 零改动。
    /// </summary>
    public class ProductModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<CellinfoProductRepository>()
                   .As<ICellinfoProductRepository>()
                   .SingleInstance();

            builder.RegisterType<ProductCellInfoService>()
                   .As<IProductCellInfoService>()
                   .As<IProductDataSource>()
                   .SingleInstance();
        }
    }
}
