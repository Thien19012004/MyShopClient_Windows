using CommunityToolkit.Mvvm.ComponentModel;
using MyShopClient.Models.Categories;
using MyShopClient.Models.Products;
using MyShopClient.ViewModels.Common;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MyShopClient.ViewModels
{
 /// <summary>
    /// Dialog state container for Edit Product dialog
    /// </summary>
    public partial class EditProductDialogState : ObservableObject
   {
        [ObservableProperty]
        private string? dialogError;

       public bool HasError => !string.IsNullOrWhiteSpace(DialogError);

       [ObservableProperty]
     private int productId;

      [ObservableProperty]
     private string? sku;

     [ObservableProperty]
    private string? name;

    [ObservableProperty]
       private string? importPriceText;

    [ObservableProperty]
   private string? salePriceText;

        [ObservableProperty]
        private string? stockQuantityText;

 [ObservableProperty]
       private string? description;

 [ObservableProperty]
   private string? imagePath;

    [ObservableProperty]
       private CategoryOption? category;

    /// <summary>
 /// Load from ProductItemDto
      /// </summary>
      public void LoadFromProduct(ProductItemDto product, IEnumerable<CategoryOption> allCategories)
  {
    DialogError = string.Empty;
      ProductId = product.ProductId;
  Sku = product.Sku;
      Name = product.Name;
 ImportPriceText = product.ImportPrice?.ToString();
  SalePriceText = product.SalePrice.ToString();
    StockQuantityText = product.StockQuantity.ToString();
   Description = product.Description;
   ImagePath = product.ImagePaths?.FirstOrDefault();
       Category = allCategories.FirstOrDefault(c => c.Id == product.CategoryId)
     ?? allCategories.FirstOrDefault(c => c.Id != null)
    ?? allCategories.FirstOrDefault();
    }

 /// <summary>
        /// Validate all fields for editing a product
  /// </summary>
     public bool Validate(out string? error)
  {
     error = null;

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
/// Convert dialog state to ProductUpdateInput
   /// </summary>
  public ProductUpdateInput ToUpdateInput()
{
       var importPrice = int.Parse(ImportPriceText!);
 var salePrice = int.Parse(SalePriceText!);
      var stockQty = int.Parse(StockQuantityText!);

     return new ProductUpdateInput
       {
    Name = Name!,
ImportPrice = importPrice,
   SalePrice = salePrice,
           StockQuantity = stockQty,
   Description = Description,
     CategoryId = Category!.Id!.Value,
      ImagePaths = string.IsNullOrWhiteSpace(ImagePath)
        ? null
  : new() { ImagePath! }
      };
}
    }
}
