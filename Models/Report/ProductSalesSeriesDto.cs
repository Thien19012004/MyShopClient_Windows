using System.Collections.Generic;

namespace MyShopClient.Models
{
 public class ProductSalesSeriesDto
 {
 public int ProductId { get; set; }
 public string Sku { get; set; } = string.Empty;
 public string Name { get; set; } = string.Empty;
 public List<ProductSalesPointDto> Points { get; set; } = new();
 }
}
