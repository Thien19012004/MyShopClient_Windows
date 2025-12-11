using System.Globalization;

namespace MyShopClient.ViewModels.Common
{
    /// <summary>
    /// Common validation utilities for ViewModels
    /// </summary>
    public static class ValidationHelper
    {
        /// <summary>
        /// Try parse string to integer with minimum value check
        /// </summary>
public static (bool Success, int Value) TryParseInt(string? text, int minValue = 0)
        {
   if (!int.TryParse(text, out var value) || value < minValue)
  return (false, -1);

    return (true, value);
        }

   /// <summary>
        /// Validate required string field
        /// </summary>
        public static bool IsRequired(string? value, out string? error)
       {
        if (string.IsNullOrWhiteSpace(value))
         {
        error = "This field is required.";
    return false;
      }

    error = null;
       return true;
     }

   /// <summary>
        /// Validate product name (required)
        /// </summary>
    public static bool ValidateProductName(string? name, out string? error)
  => IsRequired(name, out error);

        /// <summary>
 /// Validate product SKU (required)
    /// </summary>
     public static bool ValidateProductSku(string? sku, out string? error)
     => IsRequired(sku, out error);

        /// <summary>
        /// Validate price (non-negative integer)
      /// </summary>
public static bool ValidatePrice(string? priceText, out string? error, string fieldName = "Price")
        {
     if (!int.TryParse(priceText, out var price) || price < 0)
   {
    error = $"{fieldName} must be a non-negative integer.";
         return false;
           }

    error = null;
       return true;
    }

 /// <summary>
        /// Validate stock quantity (non-negative integer)
        /// </summary>
public static bool ValidateStockQuantity(string? quantityText, out string? error)
  {
        if (!int.TryParse(quantityText, out var quantity) || quantity < 0)
   {
             error = "Stock quantity must be a non-negative integer.";
         return false;
   }

   error = null;
    return true;
   }

  /// <summary>
        /// Validate category selection
  /// </summary>
        public static bool ValidateCategorySelection<T>(T? category, out string? error) where T : class
   {
   if (category == null)
       {
 error = "Please select a category.";
        return false;
         }

       error = null;
      return true;
    }
    }
}
