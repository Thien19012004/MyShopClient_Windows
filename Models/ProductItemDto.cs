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
        
        // Flag ?? track ?ã load ch?a
        public bool ImagesLoaded { get; set; }
    }

    public class ProductDetailDto
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
        public List<string>? ImagePaths { get; set; }
    }

    public class ProductCreateInput
    {
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int ImportPrice { get; set; }
        public int SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public List<string>? ImagePaths { get; set; }
    }

    public class ProductUpdateInput
    {
        public string Name { get; set; } = string.Empty;
        public int ImportPrice { get; set; }
        public int SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public int CategoryId { get; set; }
        public string? Description { get; set; }
        public List<string>? ImagePaths { get; set; }
    }

    public class ProductPageResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<ProductItemDto> Items { get; set; } = new();
    }
}
