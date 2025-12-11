using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models.Categories;
using MyShopClient.Models.Common;
using MyShopClient.Models.Products;
using MyShopClient.ViewModels.Common;
using System;
using System.Collections.Generic;

namespace MyShopClient.ViewModels
{
    /// <summary>
    /// Dialog state container for Add Product dialog
    /// </summary>
   public partial class AddProductDialogState : ObservableObject
    {
        [ObservableProperty] private string? dialogError;

      public bool HasError => !string.IsNullOrWhiteSpace(DialogError);

     [ObservableProperty] private string? sku;
   [ObservableProperty] private string? name;
   [ObservableProperty] private string? importPriceText;
        [ObservableProperty] private string? salePriceText;
        [ObservableProperty] private string? stockQuantityText;
       [ObservableProperty] private string? description;
    [ObservableProperty] private string? imagePath;
 [ObservableProperty] private CategoryOption? category;

  /// <summary>
        /// Reset all fields to empty state
   /// </summary>
        public void Reset()
   {
      DialogError = string.Empty;
 Sku = string.Empty;
 Name = string.Empty;
  ImportPriceText = string.Empty;
     SalePriceText = string.Empty;
        StockQuantityText = string.Empty;
         Description = string.Empty;
        ImagePath = string.Empty;
        Category = null;
     }

      /// <summary>
    /// Validate all fields for creating a product
    /// </summary>
     public bool Validate(out string? error)
 {
    error = null;

  if (!ValidationHelper.ValidateProductSku(Sku, out var skuError))
 {
    error = skuError;
          return false;
      }

    if (!ValidationHelper.ValidateProductName(Name, out var nameError))
      {
      error = nameError;
       return false;
        }

    if (!ValidationHelper.ValidatePrice(ImportPriceText, out var importError, "Import price"))
      {
  error = importError;
   return false;
        }

      if (!ValidationHelper.ValidatePrice(SalePriceText, out var saleError, "Sale price"))
      {
  error = saleError;
            return false;
           }

    if (!ValidationHelper.ValidateStockQuantity(StockQuantityText, out var stockError))
        {
     error = stockError;
   return false;
        }

    if (!ValidationHelper.ValidateCategorySelection(Category, out var catError))
          {
       error = catError;
       return false;
 }

   return true;
     }

  /// <summary>
/// Convert dialog state to ProductCreateInput
      /// </summary>
      public ProductCreateInput ToCreateInput()
    {
  var importPrice = int.Parse(ImportPriceText!);
 var salePrice = int.Parse(SalePriceText!);
    var stockQty = int.Parse(StockQuantityText!);

 return new ProductCreateInput
  {
     Sku = Sku!,
   Name = Name!,
   ImportPrice = importPrice,
        SalePrice = salePrice,
StockQuantity = stockQty,
      Description = Description ?? string.Empty,
  CategoryId = Category!.Id!.Value,
   ImagePaths = string.IsNullOrWhiteSpace(ImagePath)
    ? new()
   : new() { ImagePath! }
 };
  }
  }
}
