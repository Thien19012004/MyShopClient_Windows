using System;

namespace MyShopClient.Models
{
 public class OrderQueryOptions
 {
 public int Page { get; set; } =1;
 public int PageSize { get; set; } =10;

 public int? CustomerId { get; set; }
 public int? SaleId { get; set; }
 public OrderStatus? Status { get; set; }

 public DateTime? FromDate { get; set; }
 public DateTime? ToDate { get; set; }
 }
}
