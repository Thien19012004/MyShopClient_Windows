namespace MyShopClient.Models.Products
{
    /// <summary>
    /// Sort field options for products
    /// </summary>
    public enum ProductSortField
    {
    Name,
        SalePrice,
     ImportPrice,
        StockQuantity,
      CreatedAt
    }

    /// <summary>
    /// Query options for filtering and searching products
    /// </summary>
    public class ProductQueryOptions
    {
        public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
   public string? Search { get; set; }
        public int? CategoryId { get; set; }
        public int? MinPrice { get; set; }
        public int? MaxPrice { get; set; }
        public ProductSortField SortField { get; set; } = ProductSortField.Name;
        public bool SortAscending { get; set; } = true;
    }
}
