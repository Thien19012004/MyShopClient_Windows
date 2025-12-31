using System.Collections.Generic;

namespace MyShopClient.Models
{
 public class OrderUpdateInput
 {
 public OrderStatus? Status { get; set; }
 public List<int>? PromotionIds { get; set; }
 public List<OrderItemInput>? Items { get; set; }
 }
}
