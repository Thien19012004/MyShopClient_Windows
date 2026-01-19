using System.Collections.Generic;

namespace MyShopClient.Models
{
    public class OrderCreateInput
    {
        public int CustomerId { get; set; }
        public int SaleId { get; set; }
        public List<int>? PromotionIds { get; set; }
        public List<OrderItemInput> Items { get; set; } = new();
    }
}
