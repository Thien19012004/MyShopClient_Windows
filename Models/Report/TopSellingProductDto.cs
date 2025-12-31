namespace MyShopClient.Models
{
 public class TopSellingProductDto
 {
 public int ProductId { get; set; }
 public string Sku { get; set; } = string.Empty;
 public string Name { get; set; } = string.Empty;
 public int TotalQuantity { get; set; }
 public decimal TotalRevenue { get; set; }
 }
}
