using System.Collections.Generic;

namespace MyShopClient.Models
{
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
}
