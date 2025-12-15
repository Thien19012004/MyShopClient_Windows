using MyShopClient.Models;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    public enum ProductSortField
    {
        Name,
        SalePrice,
        ImportPrice,
        StockQuantity,
        CreatedAt
        // nếu server có enum khác (e.g. CREATED_AT) thì thêm vào đây
    }

    public class ProductQueryOptions
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public ProductSortField SortField { get; set; } = ProductSortField.Name;
        public bool SortAscending { get; set; } = true;
    }

    public interface IProductService
    {
        Task<ApiResult<ProductPageResult>> GetProductsAsync(
            ProductQueryOptions options,
            CancellationToken cancellationToken = default);

        Task<ApiResult<bool>> DeleteProductAsync(
            int productId,
            CancellationToken cancellationToken = default);

        // để sẵn cho ProductDetailPage / add / edit sau
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
