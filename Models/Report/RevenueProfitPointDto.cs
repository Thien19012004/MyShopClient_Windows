namespace MyShopClient.Models
{
 public class RevenueProfitPointDto
 {
 public string Period { get; set; } = string.Empty; // yyyy-MM, yyyy-MM-dd ...
 public int Revenue { get; set; }
 public int Profit { get; set; }
 }
}
