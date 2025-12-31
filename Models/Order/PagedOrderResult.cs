using System.Collections.Generic;

namespace MyShopClient.Models
{
 public class PagedOrderResult
 {
 public int Page { get; set; }
 public int PageSize { get; set; }
 public int TotalItems { get; set; }
 public int TotalPages { get; set; }
 public List<OrderListItemDto> Items { get; set; } = new();
 }
}
