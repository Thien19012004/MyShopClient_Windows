using MyShopClient.Models.Common;
using MyShopClient.Models.Products;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    /// <summary>
    /// Interface for Product service operations
    /// </summary>
    public interface IProductService
    {
        /// <summary>
        /// Get paginated list of products with optional filtering and sorting
        /// </summary>
        Task<ApiResult<ProductPageResult>> GetProductsAsync(
            ProductQueryOptions options,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Get a single product by ID
        /// </summary>
        Task<ApiResult<ProductDetailDto>> GetProductByIdAsync(
            int productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Create a new product
        /// </summary>
        Task<ApiResult<ProductDetailDto>> CreateProductAsync(
            ProductCreateInput input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Update an existing product
        /// </summary>
        Task<ApiResult<ProductDetailDto>> UpdateProductAsync(
            int productId,
            ProductUpdateInput input,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Delete a product by ID
        /// </summary>
        Task<ApiResult<bool>> DeleteProductAsync(
            int productId,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Import products from Excel file
        /// </summary>
        Task<ApiResult<int>> ImportProductsFromExcelAsync(Stream excelStream);
    }
}
