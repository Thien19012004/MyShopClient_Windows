using MyShopClient.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services.Product
{
    // IProductService - interface only. Product-related DTOs and enums live under Models namespace.
    public interface IProductService
    {
        Task<ApiResult<ProductPageResult>> GetProductsAsync(
            ProductQueryOptions options,
            CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> DeleteProductAsync(
            int productId,
            CancellationToken cancellationToken = default);

        // for ProductDetailPage / add / edit
        Task<ApiResult<ProductDetailDto>> GetProductByIdAsync(
            int productId,
            CancellationToken cancellationToken = default);

        Task<ApiResult<ProductDetailDto>> CreateProductAsync(
            ProductCreateInput input,
            CancellationToken cancellationToken = default);

        Task<ApiResult<ProductDetailDto>> UpdateProductAsync(
            int productId,
            ProductUpdateInput input,
            CancellationToken cancellationToken = default);

        Task<ApiResult<int>> ImportProductsFromExcelAsync(Stream excelStream);
    }
}
