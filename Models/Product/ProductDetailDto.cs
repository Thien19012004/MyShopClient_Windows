using System.Collections.Generic;

namespace MyShopClient.Models
{
 public class ProductDetailDto
 {
 public int ProductId { get; set; }
 public string Sku { get; set; } = string.Empty;
 public string Name { get; set; } = string.Empty;
 public int SalePrice { get; set; }
 public int ImportPrice { get; set; }
 public int StockQuantity { get; set; }
 public int CategoryId { get; set; }
 public string? CategoryName { get; set; }
 public string? Description { get; set; }
 public List<string>? ImagePaths { get; set; }
 }
}
