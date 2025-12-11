namespace MyShopClient.Models.Products
{
    /// <summary>
  /// Product list item DTO - for displaying in list/table
    /// </summary>
    public class ProductItemDto
    {
 public int ProductId { get; set; }
  public string Sku { get; set; } = string.Empty;
public string Name { get; set; } = string.Empty;
        public decimal? ImportPrice { get; set; }
        public decimal SalePrice { get; set; }
        public int StockQuantity { get; set; }
        public string CategoryName { get; set; } = string.Empty;
     public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
     public System.Collections.Generic.List<string> ImagePaths { get; set; } = new();
    }
}
