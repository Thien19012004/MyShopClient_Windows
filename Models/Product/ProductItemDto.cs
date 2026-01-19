using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Generic;

namespace MyShopClient.Models
{
    public partial class ProductItemDto : ObservableObject
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int SalePrice { get; set; }
        public int ImportPrice { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public string? Description { get; set; }

        // Lazy-loaded images
        [ObservableProperty]
        private List<string>? imagePaths;


        public bool ImagesLoaded { get; set; }


        [ObservableProperty]
        private bool isSelected;
    }
}
