using System;

namespace MyShopClient.Models
{
    public class PromotionQueryOptions
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
        public bool OnlyActive { get; set; } = false;
        public DateTime? At { get; set; }
        public PromotionScope? Scope { get; set; }
    }
}
