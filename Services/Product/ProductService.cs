using MyShopClient.Models;
using MyShopClient.Services.Product;
using MyShopClient.Infrastructure.GraphQL;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;


namespace MyShopClient.Services
{
    public class ProductService : IProductService
    {
        private readonly IGraphQLClient _gql;

        public ProductService(IGraphQLClient gql)
        {
            _gql = gql;
        }

        // ============================================================
        // Mapping enum FE -> enum GraphQL
        // ============================================================
        private static string ToGraphQlSortField(ProductSortField field)
        {
            // BE: NAME, SALE_PRICE, IMPORT_PRICE, STOCK_QUANTITY, CREATED_AT
            return field switch
            {
                ProductSortField.Name => "NAME",
                ProductSortField.SalePrice => "SALE_PRICE",
                ProductSortField.ImportPrice => "IMPORT_PRICE",
                ProductSortField.StockQuantity => "STOCK_QUANTITY",
                ProductSortField.CreatedAt => "CREATED_AT",
                _ => "NAME"
            };
        }

        private static string ToStringLiteral(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return "null";

            var s = value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");

            return $"\"{s}\"";
        }

        private static string ToNullableIntLiteral(int? value)
        => value.HasValue ? value.Value.ToString() : "null";

        private string BuildGetProductsQuery(ProductQueryOptions opt)
        {
            string searchLiteral = ToStringLiteral(opt.Search);
            string categoryLiteral = ToNullableIntLiteral(opt.CategoryId);
            string minPriceLiteral = ToNullableIntLiteral(opt.MinPrice);
            string maxPriceLiteral = ToNullableIntLiteral(opt.MaxPrice);
            string sortFieldLiteral = ToGraphQlSortField(opt.SortField);
            string ascLiteral = opt.SortAscending ? "true" : "false";

            // Use template from Infrastructure.GraphQL.ProductsQueries
            var query = string.Format(Infrastructure.GraphQL.ProductsQueries.GetProductsTemplate,
            opt.Page,
            opt.PageSize,
            searchLiteral,
            categoryLiteral,
            minPriceLiteral,
            maxPriceLiteral,
            sortFieldLiteral,
            ascLiteral);

            return query;
        }

        private class GetProductsPayload
        {
            // JSON: { "data": { "products": { ... } } }
            public ApiResult<ProductPageResult> Products { get; set; } = null!;
        }

        // ============================================================
        // GetProducts
        // ============================================================
        public async Task<ApiResult<ProductPageResult>> GetProductsAsync(
        ProductQueryOptions opt,
        CancellationToken cancellationToken = default)
        {
            var query = BuildGetProductsQuery(opt);
            var data = await _gql.SendAsync<GetProductsPayload>(query, null, cancellationToken);

            return data?.Products ?? new ApiResult<ProductPageResult>
            {
                StatusCode =500,
                Success = false,
                Message = "No data from server"
            };
        }

        // ============================================================
        // DeleteProduct
        // ============================================================
        private const string DeleteProductMutation = Infrastructure.GraphQL.ProductsQueries.DeleteProductMutation;

        private class DeleteProductPayload
        {
            public ApiResult<object?> DeleteProduct { get; set; } = null!;
        }

        public async Task<ApiResult<bool>> DeleteProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
        {
            var variables = new { productId };

            var data = await _gql.SendAsync<DeleteProductPayload>(
            DeleteProductMutation,
            variables,
            cancellationToken);

            var inner = data?.DeleteProduct;

            return new ApiResult<bool>
            {
                StatusCode = inner?.StatusCode ??500,
                Success = inner?.Success ?? false,
                Message = inner?.Message,
                Data = inner?.Success ?? false
            };
        }

        // ============================================================
        // GetProductById
        // ============================================================
        private const string GetProductByIdQuery = Infrastructure.GraphQL.ProductsQueries.GetProductByIdQuery;

        private class GetProductByIdPayload
        {
            public ApiResult<ProductDetailDto> ProductById { get; set; } = null!;
        }

        public async Task<ApiResult<ProductDetailDto>> GetProductByIdAsync(
        int productId,
        CancellationToken cancellationToken = default)
        {
            var variables = new { productId };

            var data = await _gql.SendAsync<GetProductByIdPayload>(
            GetProductByIdQuery,
            variables,
            cancellationToken);

            return data?.ProductById ?? new ApiResult<ProductDetailDto>
            {
                StatusCode =500,
                Success = false,
                Message = "No data from server"
            };
        }

        // ============================================================
        // CreateProduct
        // (type biến có thể cần chỉnh lại nếu server đặt tên khác)
        // ============================================================
        private const string CreateProductMutation = Infrastructure.GraphQL.ProductsQueries.CreateProductMutation;

        private class CreateProductPayload
        {
            public ApiResult<ProductDetailDto> CreateProduct { get; set; } = null!;
        }

        public async Task<ApiResult<ProductDetailDto>> CreateProductAsync(
        ProductCreateInput input,
        CancellationToken cancellationToken = default)
        {
            var variables = new { input };

            var data = await _gql.SendAsync<CreateProductPayload>(
            CreateProductMutation,
            variables,
            cancellationToken);

            return data?.CreateProduct ?? new ApiResult<ProductDetailDto>
            {
                StatusCode =500,
                Success = false,
                Message = "No data from server"
            };
        }

        // ============================================================
        // UpdateProduct
        // ============================================================
        private const string UpdateProductMutation = Infrastructure.GraphQL.ProductsQueries.UpdateProductMutation;

        private class UpdateProductPayload
        {
            public ApiResult<ProductDetailDto> UpdateProduct { get; set; } = null!;
        }

        public async Task<ApiResult<ProductDetailDto>> UpdateProductAsync(
        int productId,
        ProductUpdateInput input,
        CancellationToken cancellationToken = default)
        {
            var variables = new { productId, input };

            var data = await _gql.SendAsync<UpdateProductPayload>(
            UpdateProductMutation,
            variables,
            cancellationToken);

            return data?.UpdateProduct ?? new ApiResult<ProductDetailDto>
            {
                StatusCode =500,
                Success = false,
                Message = "No data from server"
            };
        }

        public async Task<ApiResult<int>> ImportProductsFromExcelAsync(Stream excelStream)
        {
            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

            int imported =0;

            try
            {
                using var package = new ExcelPackage(excelStream);
                var ws = package.Workbook.Worksheets[0]; // sheet đầu tiên

                if (ws.Dimension == null)
                {
                    return new ApiResult<int>
                    {
                        Success = false,
                        StatusCode =400,
                        Message = "Excel file is empty."
                    };
                }

                // Giả sử cột:
                //1: SKU
                //2: Name
                //3: ImportPrice
                //4: SalePrice
                //5: StockQuantity
                //6: CategoryId
                //7: Description
                //8: ImagePath (optional)
                // Row1 là header, data bắt đầu từ row2
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
                    ? new List<string>() // danh sách rỗng
                    : new List<string> { imagePath } //1 phần tử
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
                    StatusCode =200,
                    Data = imported,
                    Message = $"Imported {imported} products."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<int>
                {
                    Success = false,
                    StatusCode =500,
                    Message = ex.Message
                };
            }
        }
    }
}