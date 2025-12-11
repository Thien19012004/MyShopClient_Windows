using MyShopClient.Models.Common;
using System.Collections.Generic;

namespace MyShopClient.Models.Products
{
    /// <summary>
    /// Product detail DTO - for editing/viewing single product
    /// </summary>
  public class ProductDetailDto
    {
     public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public decimal? ImportPrice { get; set; }
    public decimal SalePrice { get; set; }
        public int StockQuantity { get; set; }
   public string? Description { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public List<string> ImagePaths { get; set; } = new();
    }

    /// <summary>
    /// Paginated product list response
    /// </summary>
public class ProductPageResult : PaginationBase<ProductItemDto>
  {
    }
}
