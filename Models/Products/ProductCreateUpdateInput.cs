using System.Collections.Generic;

namespace MyShopClient.Models.Products
{
    /// <summary>
    /// Input for creating a product
    /// </summary>
    public class ProductCreateInput
    {
        public string Sku { get; set; } = string.Empty;
      public string Name { get; set; } = string.Empty;
        public int ImportPrice { get; set; }
        public int SalePrice { get; set; }
     public int StockQuantity { get; set; }
    public string Description { get; set; } = string.Empty;
public int CategoryId { get; set; }
        public List<string> ImagePaths { get; set; } = new();
    }

    /// <summary>
    /// Input for updating a product
    /// </summary>
    public class ProductUpdateInput
    {
  public string? Name { get; set; }
        public int? ImportPrice { get; set; }
        public int? SalePrice { get; set; }
     public int? StockQuantity { get; set; }
        public string? Description { get; set; }
        public int? CategoryId { get; set; }
        public List<string>? ImagePaths { get; set; }
    }
}
