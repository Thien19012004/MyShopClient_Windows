namespace MyShopClient.Models
{
 public class LowStockProductDto
 {
 public int ProductId { get; set; }
 public string Sku { get; set; } = string.Empty;
 public string Name { get; set; } = string.Empty;
 public int StockQuantity { get; set; }
 }
}
