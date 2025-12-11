using MyShopClient.Models.Common;
using MyShopClient.Models.Products;
using MyShopClient.Services.Helpers;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    /// <summary>
    /// Service for Product GraphQL operations
    /// </summary>
    public class ProductService : GraphQlClientBase, IProductService
    {
        private class ProductsPayload
        {
            public ApiResult<ProductPageResult> Products { get; set; } = null!;
        }

        private class ProductByIdPayload
        {
            public ApiResult<ProductDetailDto> ProductById { get; set; } = null!;
        }

        private class CreateProductPayload
        {
            public ApiResult<ProductDetailDto> CreateProduct { get; set; } = null!;
        }

        private class UpdateProductPayload
        {
            public ApiResult<ProductDetailDto> UpdateProduct { get; set; } = null!;
        }

        private class DeleteProductPayload
        {
            public ApiResult<object?> DeleteProduct { get; set; } = null!;
        }

        public ProductService(HttpClient httpClient, IServerConfigService serverConfig)
            : base(httpClient, serverConfig)
        {
        }

        /// <summary>
        /// Build GraphQL query for getting products with filters
        /// </summary>
        private string BuildGetProductsQuery(ProductQueryOptions opt)
        {
            var search = GraphQlHelper.ToStringLiteral(opt.Search);
            var categoryId = GraphQlHelper.ToNullableIntLiteral(opt.CategoryId);
            var minPrice = GraphQlHelper.ToNullableIntLiteral(opt.MinPrice);
            var maxPrice = GraphQlHelper.ToNullableIntLiteral(opt.MaxPrice);
            var sortField = ProductSortHelper.ToGraphQlField(opt.SortField);
            var asc = GraphQlHelper.ToBoolLiteral(opt.SortAscending);

            return $@"
query GetProducts {{
  products(
    pagination: {{ page: {opt.Page}, pageSize: {opt.PageSize} }}
    filter: {{ search: {search}, categoryId: {categoryId}, minPrice: {minPrice}, maxPrice: {maxPrice} }}
    sort: {{ field: {sortField}, asc: {asc} }}
  ) {{
    statusCode
    success
    message
    data {{
      page
      pageSize
      totalItems
      totalPages
      items {{
        productId
        sku
        name
        salePrice
        importPrice
        stockQuantity
        categoryName
      }}
    }}
  }}
}}";
        }

        // ============================================================
        //  GetProducts
        // ============================================================
        public async Task<ApiResult<ProductPageResult>> GetProductsAsync(
            ProductQueryOptions opt,
            CancellationToken cancellationToken = default)
        {
            var query = BuildGetProductsQuery(opt);

            try
            {
                var payload = await PostGraphQlAsync<ProductsPayload>(query, null, cancellationToken);
                return payload.Products ?? new ApiResult<ProductPageResult>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "No data from server"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<ProductPageResult>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ============================================================
        //  GetProductById
        // ============================================================
        private const string GetProductByIdQuery = @"
query GetProductById($productId: Int!) {
  productById(productId: $productId) {
    statusCode
    success
    message
    data {
      productId
      sku
      name
      importPrice
      salePrice
      stockQuantity
      description
      categoryId
      categoryName
      imagePaths
    }
  }
}";

        public async Task<ApiResult<ProductDetailDto>> GetProductByIdAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            var variables = new { productId };

            try
            {
                var payload = await PostGraphQlAsync<ProductByIdPayload>(GetProductByIdQuery, variables, cancellationToken);
                return payload.ProductById ?? new ApiResult<ProductDetailDto>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "No data from server"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<ProductDetailDto>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ============================================================
        //  CreateProduct
        //  (type biến có thể cần chỉnh lại nếu server đặt tên khác)
        // ============================================================
        private const string CreateProductMutation = @"
mutation CreateProduct($input: CreateProductInput!) {
  createProduct(input: $input) {
    statusCode
    success
    message
    data {
      productId
      sku
      name
      salePrice
      stockQuantity
    }
  }
}";

        public async Task<ApiResult<ProductDetailDto>> CreateProductAsync(
            ProductCreateInput input,
            CancellationToken cancellationToken = default)
        {
            var variables = new { input };

            try
            {
                var payload = await PostGraphQlAsync<CreateProductPayload>(CreateProductMutation, variables, cancellationToken);
                return payload.CreateProduct ?? new ApiResult<ProductDetailDto>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "No data from server"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<ProductDetailDto>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ============================================================
        //  UpdateProduct
        // ============================================================
        private const string UpdateProductMutation = @"
mutation UpdateProduct($productId: Int!, $input: UpdateProductInput!) {
  updateProduct(productId: $productId, input: $input) {
    statusCode
    success
    message
    data {
      productId
      sku
      name
      salePrice
      stockQuantity
    }
  }
}";

        public async Task<ApiResult<ProductDetailDto>> UpdateProductAsync(
            int productId,
            ProductUpdateInput input,
            CancellationToken cancellationToken = default)
        {
            var variables = new { productId, input };

            try
            {
                var payload = await PostGraphQlAsync<UpdateProductPayload>(UpdateProductMutation, variables, cancellationToken);
                return payload.UpdateProduct ?? new ApiResult<ProductDetailDto>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = "No data from server"
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<ProductDetailDto>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // ============================================================
        //  DeleteProduct
        // ============================================================
        private const string DeleteProductMutation = @"
mutation DeleteProduct($productId: Int!) {
  deleteProduct(productId: $productId) {
    statusCode
    success
    message
  }
}";

        public async Task<ApiResult<bool>> DeleteProductAsync(
            int productId,
            CancellationToken cancellationToken = default)
        {
            var variables = new { productId };

            try
            {
                var payload = await PostGraphQlAsync<DeleteProductPayload>(DeleteProductMutation, variables, cancellationToken);
                var inner = payload.DeleteProduct;

                return new ApiResult<bool>
                {
                    StatusCode = inner?.StatusCode ?? 500,
                    Success = inner?.Success ?? false,
                    Message = inner?.Message,
                    Data = inner?.Success ?? false
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<bool>
                {
                    StatusCode = 500,
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        public async Task<ApiResult<int>> ImportProductsFromExcelAsync(Stream excelStream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int imported = 0;

            try
            {
                using var package = new ExcelPackage(excelStream);
                var ws = package.Workbook.Worksheets[0]; // sheet đầu tiên

                if (ws.Dimension == null)
                {
                    return new ApiResult<int>
                    {
                        Success = false,
                        StatusCode = 400,
                        Message = "Excel file is empty."
                    };
                }

                // Giả sử cột:
                // 1: SKU
                // 2: Name
                // 3: ImportPrice
                // 4: SalePrice
                // 5: StockQuantity
                // 6: CategoryId
                // 7: Description
                // 8: ImagePath (optional)
                // Row 1 là header, data bắt đầu từ row 2
                int rowStart = 2;
                int rowEnd = ws.Dimension.End.Row;

                for (int row = rowStart; row <= rowEnd; row++)
                {
                    var sku = ws.Cells[row, 1].GetValue<string>()?.Trim();
                    if (string.IsNullOrWhiteSpace(sku))
                        continue; // bỏ qua dòng trống

                    var name = ws.Cells[row, 2].GetValue<string>()?.Trim() ?? string.Empty;
                    var importPrice = ws.Cells[row, 3].GetValue<decimal>();
                    var salePrice = ws.Cells[row, 4].GetValue<decimal>();
                    var stockQty = ws.Cells[row, 5].GetValue<int>();
                    var categoryId = ws.Cells[row, 6].GetValue<int>();
                    var description = ws.Cells[row, 7].GetValue<string>() ?? string.Empty;
                    var imagePath = ws.Cells[row, 8].GetValue<string>();

                    var input = new ProductCreateInput
                    {
                        Sku = sku,
                        Name = name,
                        ImportPrice = (int)importPrice,
                        SalePrice = (int)salePrice,
                        StockQuantity = stockQty,
                        CategoryId = categoryId,
                        Description = description,
                        ImagePaths = string.IsNullOrWhiteSpace(imagePath)
                        ? new List<string>()                  // danh sách rỗng
                        : new List<string> { imagePath }      // 1 phần tử
                    };


                    var res = await CreateProductAsync(input);
                    if (res.Success)
                    {
                        imported++;
                    }
                    else
                    {
                        // tuỳ anh: log / bỏ qua / break
                        System.Diagnostics.Debug.WriteLine(
                            $"Import row {row} failed: {res.Message}");
                    }
                }

                return new ApiResult<int>
                {
                    Success = true,
                    StatusCode = 200,
                    Data = imported,
                    Message = $"Imported {imported} products."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<int>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = ex.Message
                };
            }
        }
    }
}
