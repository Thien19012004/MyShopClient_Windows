using System.Collections.Generic;

namespace MyShopClient.Models
{
    public class OrderDetailDto
    {
        public int OrderId { get; set; }
        public int? CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerPhone { get; set; } = string.Empty;
        public int SaleId { get; set; }
        public string SaleName { get; set; } = string.Empty;
        public OrderStatus Status { get; set; }
        public System.DateTime CreatedAt { get; set; }

        public int Subtotal { get; set; }
        public int OrderDiscountAmount { get; set; }
        public int OrderDiscountPercentApplied { get; set; }
        public int TotalPrice { get; set; }
        public List<int> PromotionIds { get; set; } = new();

        public List<OrderItemDto> Items { get; set; } = new();
    }
}
