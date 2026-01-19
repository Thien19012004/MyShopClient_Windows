using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;

namespace MyShopClient.Models
{
    // DTO for Promotion item in list
    public partial class PromotionItemDto : ObservableObject
    {
        public int PromotionId { get; set; }
        public string Name { get; set; } = string.Empty;
        public int DiscountPercent { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        // scope + relations
        public PromotionScope Scope { get; set; }
        public List<int> ProductIds { get; set; } = new();
        public List<int> CategoryIds { get; set; } = new();

        // Backend returns these counts
        public int ProductCount { get; set; }
        public int CategoryCount { get; set; }

        // Helper properties
        public bool IsActive => IsActiveAt(DateTime.UtcNow);
        public bool IsActiveAt(DateTime at) => at >= StartDate && at <= EndDate;
        public string Status => IsActive ? "Active" : (DateTime.UtcNow < StartDate ? "Upcoming" : "Expired");

        // UI selection
        [ObservableProperty]
        private bool isSelected;
    }
}
