namespace MyShopClient.Models
{
 public class CategoryItemDto
 {
 public int CategoryId { get; set; }
 public string Name { get; set; } = string.Empty;
 public string? Description { get; set; }
 public int ProductCount { get; set; }
 }
}
