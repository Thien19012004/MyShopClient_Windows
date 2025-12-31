namespace MyShopClient.Models
{
 // Sales data point for a product over time
 public class ProductSalesPointDto
 {
 public string Period { get; set; } = string.Empty; // e.g.2025-01,2025-01-01 ...
 public int Value { get; set; } // units sold
 }
}
