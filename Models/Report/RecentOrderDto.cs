namespace MyShopClient.Models
{
 public class RecentOrderDto
 {
 public int OrderId { get; set; }
 public string CustomerName { get; set; } = string.Empty;
 public string SaleName { get; set; } = string.Empty;
 public string Status { get; set; } = string.Empty;
 public decimal TotalPrice { get; set; }
 public string CreatedAt { get; set; } = string.Empty;
 }
}
