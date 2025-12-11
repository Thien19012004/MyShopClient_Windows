using MyShopClient.Models.Products;

namespace MyShopClient.Services.Helpers
{
    /// <summary>
    /// Helper for converting product sort options
    /// </summary>
    public static class ProductSortHelper
    {
  /// <summary>
        /// Convert FE ProductSortField enum to GraphQL sort field string
        /// </summary>
      public static string ToGraphQlField(ProductSortField field)
        {
            return field switch
        {
       ProductSortField.Name => "NAME",
              ProductSortField.SalePrice => "SALE_PRICE",
         ProductSortField.ImportPrice => "IMPORT_PRICE",
          ProductSortField.StockQuantity => "STOCK_QUANTITY",
   ProductSortField.CreatedAt => "CREATED_AT",
             _ => "NAME"
       };
        }
    }
}
