using System;
using System.Collections.Generic;

namespace MyShopClient.Models
{
    public class UpdatePromotionInput
    {
        public string? Name { get; set; }
        public int? DiscountPercent { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public PromotionScope? Scope { get; set; }

        public List<int>? ProductIds { get; set; }
        public List<int>? CategoryIds { get; set; }
    }
}
