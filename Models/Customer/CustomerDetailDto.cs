namespace MyShopClient.Models
{
 // Customer detail
 public class CustomerDetailDto
 {
 public int CustomerId { get; set; }
 public string Name { get; set; } = string.Empty;
 public string Phone { get; set; } = string.Empty;
 public string Email { get; set; } = string.Empty;
 public string Address { get; set; } = string.Empty;
 public int OrderCount { get; set; }
 }
}
