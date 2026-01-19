using System;
using System.Collections.Generic;

namespace MyShopClient.Models
{
    public class PromotionDetailDto
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public PromotionScope Scope { get; set; }
        public List<int> ProductIds { get; set; } = new();
        public List<int> CategoryIds { get; set; } = new();
    }
}
