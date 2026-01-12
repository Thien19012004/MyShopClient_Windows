using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using MyShopClient.Services.Product;
using MyShopClient.Services.Category;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{
    public partial class PromotionEditViewModel : ObservableObject
    {
        private readonly IPromotionService _promotionService;
        private readonly Func<Task> _reloadCallback;
        private readonly IProductService _productService;
   private readonly ICategoryService _categoryService;

        public PromotionEditViewModel(IPromotionService promotionService, Func<Task> reloadCallback, IProductService productService, ICategoryService categoryService)
        {
 _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
       _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
          _productService = productService ?? throw new ArgumentNullException(nameof(productService));
  _categoryService = categoryService ?? throw new ArgumentNullException(nameof(categoryService));

   SelectedProducts = new ObservableCollection<ProductItemDto>();
         SelectedCategories = new ObservableCollection<CategoryItemDto>();
        }

        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        private string? _error;
        public string? Error { get => _error; set { SetProperty(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
     public bool HasError => !string.IsNullOrWhiteSpace(Error);

        private int _editPromotionId;
  public int EditPromotionId { get => _editPromotionId; set => SetProperty(ref _editPromotionId, value); }

        private string? _editName;
        public string? EditName { get => _editName; set => SetProperty(ref _editName, value); }

        private string? _editDiscountPercentText;
        public string? EditDiscountPercentText { get => _editDiscountPercentText; set => SetProperty(ref _editDiscountPercentText, value); }

     private DateTimeOffset _editStartDate;
        public DateTimeOffset EditStartDate { get => _editStartDate; set => SetProperty(ref _editStartDate, value); }

        private DateTimeOffset _editEndDate;
        public DateTimeOffset EditEndDate { get => _editEndDate; set => SetProperty(ref _editEndDate, value); }

private PromotionScope _editScope;
     public PromotionScope EditScope
        {
            get => _editScope;
       set
  {
         SetProperty(ref _editScope, value);
 OnPropertyChanged(nameof(CanSelectProducts));
        OnPropertyChanged(nameof(CanSelectCategories));
         }
        }

    // Collections to display selected items
 public ObservableCollection<ProductItemDto> SelectedProducts { get; }
  public ObservableCollection<CategoryItemDto> SelectedCategories { get; }

        // Track dialogs
        private bool _isProductSelectorOpen;
        public bool IsProductSelectorOpen { get => _isProductSelectorOpen; set => SetProperty(ref _isProductSelectorOpen, value); }

        private bool _isCategorySelectorOpen;
        public bool IsCategorySelectorOpen { get => _isCategorySelectorOpen; set => SetProperty(ref _isCategorySelectorOpen, value); }

        // Lock states based on scope
    public bool CanSelectProducts => EditScope == PromotionScope.Product && !EditIsActive;
        public bool CanSelectCategories => EditScope == PromotionScope.Category && !EditIsActive;

        private bool _editIsActive;
        public bool EditIsActive
    {
   get => _editIsActive;
            set
{
      SetProperty(ref _editIsActive, value);
    OnPropertyChanged(nameof(CanSelectProducts));
    OnPropertyChanged(nameof(CanSelectCategories));
            }
        }

      [RelayCommand]
        private void OpenProductSelector()
        {
            IsProductSelectorOpen = true;
   }

        [RelayCommand]
        private void CloseProductSelector()
        {
            IsProductSelectorOpen = false;
        }

      [RelayCommand]
        private void OpenCategorySelector()
      {
        IsCategorySelectorOpen = true;
        }

        [RelayCommand]
      private void CloseCategorySelector()
      {
        IsCategorySelectorOpen = false;
        }

      [RelayCommand]
        private void RemoveProduct(ProductItemDto? product)
        {
   if (product == null) return;

       SelectedProducts.Remove(product);
        product.IsSelected = false;
        }

        [RelayCommand]
     private void RemoveCategory(CategoryItemDto? category)
        {
 if (category == null) return;

     SelectedCategories.Remove(category);
            category.IsSelected = false;
        }

     /// <summary>
 /// Sync checked items from ProductSelectorVm to SelectedProducts
     /// This is called when user closes the selector dialog
     /// IMPORTANT: This MERGES with existing selections, not REPLACES
     /// Only items visible in current page are synced (added/removed)
     /// Items not in current page are PRESERVED in SelectedProducts
        /// </summary>
        public void SyncSelectedProducts(IEnumerable<ProductItemDto> allProductsFromSelector)
        {
     if (allProductsFromSelector == null) return;

         // Get all checked items from current page/search
    var checkedItems = allProductsFromSelector.Where(p => p.IsSelected).ToList();
  var allItemsInSelector = allProductsFromSelector.ToList();

     // Add newly checked items to SelectedProducts
  foreach (var product in checkedItems)
   {
        if (!SelectedProducts.Any(sp => sp.ProductId == product.ProductId))
    {
          SelectedProducts.Add(product);
    }
    }

        // Remove ONLY items that are visible in selector AND unchecked
     // Items NOT in selector (e.g., from different search) are PRESERVED
      var toRemove = SelectedProducts
 .Where(sp => allItemsInSelector.Any(p => p.ProductId == sp.ProductId) && !sp.IsSelected)
     .ToList();
 foreach (var product in toRemove)
    {
  SelectedProducts.Remove(product);
        }
        }

 /// <summary>
   /// Sync checked items from CategorySelectorVm to SelectedCategories
  /// This is called when user closes the selector dialog
        /// IMPORTANT: This MERGES with existing selections, not REPLACES
        /// </summary>
        public void SyncSelectedCategories(IEnumerable<CategoryItemDto> allCategoriesFromSelector)
   {
       if (allCategoriesFromSelector == null) return;

            // Get all checked items from selector
  var checkedItems = allCategoriesFromSelector.Where(c => c.IsSelected).ToList();
      var allItemsInSelector = allCategoriesFromSelector.ToList();

     // Add newly checked items to SelectedCategories
      foreach (var category in checkedItems)
    {
      if (!SelectedCategories.Any(sc => sc.CategoryId == category.CategoryId))
 {
   SelectedCategories.Add(category);
}
    }

   // Remove ONLY items that are visible in selector AND unchecked
   // Items NOT in selector are PRESERVED
       var toRemove = SelectedCategories
     .Where(sc => allItemsInSelector.Any(c => c.CategoryId == sc.CategoryId) && !sc.IsSelected)
.ToList();
foreach (var category in toRemove)
 {
        SelectedCategories.Remove(category);
   }
    }

      /// <summary>
  /// Apply selected state to items in selector based on SelectedProducts
        /// </summary>
     public void ApplySelectedStatesToProducts(IEnumerable<ProductItemDto> productsFromSelector)
        {
          foreach (var product in productsFromSelector)
  {
                product.IsSelected = SelectedProducts.Any(sp => sp.ProductId == product.ProductId);
    }
        }

        /// <summary>
        /// Apply selected state to items in selector based on SelectedCategories
        /// </summary>
        public void ApplySelectedStatesToCategories(IEnumerable<CategoryItemDto> categoriesFromSelector)
        {
            foreach (var category in categoriesFromSelector)
     {
   category.IsSelected = SelectedCategories.Any(sc => sc.CategoryId == category.CategoryId);
      }
     }

        private static DateTimeOffset BuildLocalDateTimeOffset(DateTimeOffset selectedDate)
        {
    var selectedDateOnly = selectedDate.Date;
  var now = DateTime.Now;
 if (selectedDateOnly.Date == now.Date)
      {
     var combined = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Local);
      return new DateTimeOffset(combined);
  }
          else
  {
      var localMidnight = DateTime.SpecifyKind(selectedDateOnly.Date, DateTimeKind.Local);
      return new DateTimeOffset(localMidnight);
   }
        }

        public async Task DoOpenAsync(PromotionItemDto promotion, PromotionDetailDto detail)
    {
            Error = string.Empty;
        OnPropertyChanged(nameof(HasError));

      EditPromotionId = detail.PromotionId;
            EditName = detail.Name;
            EditDiscountPercentText = detail.DiscountPercent.ToString();

            // Convert server dates (assumed UTC) to local time
   var startLocal = DateTime.SpecifyKind(detail.StartDate, DateTimeKind.Utc).ToLocalTime();
        var endLocal = DateTime.SpecifyKind(detail.EndDate, DateTimeKind.Utc).ToLocalTime();
            EditStartDate = new DateTimeOffset(startLocal);
       EditEndDate = new DateTimeOffset(endLocal);

        EditScope = detail.Scope;

  var nowLocal = DateTime.Now;
    EditIsActive = nowLocal >= startLocal && nowLocal <= endLocal;

            // Clear previous selections
            SelectedProducts.Clear();
            SelectedCategories.Clear();

    // Load selected products/categories from detail
  if (detail.ProductIds != null && detail.ProductIds.Count > 0)
            {
    await LoadSelectedProductsAsync(detail.ProductIds);
    }

        if (detail.CategoryIds != null && detail.CategoryIds.Count > 0)
          {
         await LoadSelectedCategoriesAsync(detail.CategoryIds);
         }

        if (EditIsActive)
            {
   Error = "This promotion is currently active and cannot be edited.";
            }

 IsOpen = true;
   }

        /// <summary>
     /// Load product objects from API by IDs
        /// </summary>
        private async Task LoadSelectedProductsAsync(List<int> productIds)
        {
            try
   {
// Load all products and filter by IDs
              var options = new ProductQueryOptions
            {
  Page = 1,
    PageSize = 1000,
          SortField = ProductSortField.Name,
       SortAscending = true
                };

                var result = await _productService.GetProductsAsync(options);
        if (result.Success && result.Data != null)
           {
 var selectedSet = new HashSet<int>(productIds);
            foreach (var product in result.Data.Items)
    {
           if (selectedSet.Contains(product.ProductId))
   {
           SelectedProducts.Add(product);
       }
   }
             }
       }
          catch (Exception ex)
   {
                System.Diagnostics.Debug.WriteLine($"[EditVm] Error loading products: {ex.Message}");
            }
        }

        /// <summary>
        /// Load category objects from API by IDs
    /// </summary>
   private async Task LoadSelectedCategoriesAsync(List<int> categoryIds)
        {
     try
            {
       // Load all categories
                var result = await _categoryService.GetCategoriesAsync(null, 1, 1000);
   if (result.Success && result.Data != null)
  {
          var selectedSet = new HashSet<int>(categoryIds);
          foreach (var category in result.Data.Items)
 {
               if (selectedSet.Contains(category.CategoryId))
            {
    SelectedCategories.Add(category);
                }
        }
        }
   }
          catch (Exception ex)
    {
      System.Diagnostics.Debug.WriteLine($"[EditVm] Error loading categories: {ex.Message}");
         }
        }

        public void DoCancel()
        {
    IsOpen = false;
         Error = string.Empty;
    OnPropertyChanged(nameof(HasError));
        }

        public async Task<bool> DoConfirmAsync()
        {
     Error = string.Empty;
            OnPropertyChanged(nameof(HasError));

       if (EditIsActive)
            {
    Error = "This promotion is currently active and cannot be edited.";
            OnPropertyChanged(nameof(HasError));
     return false;
  }

            if (string.IsNullOrWhiteSpace(EditName))
   {
     Error = "Name is required.";
    OnPropertyChanged(nameof(HasError));
                return false;
    }

         if (!decimal.TryParse(EditDiscountPercentText, out var discount) || discount <= 0 || discount > 100)
     {
        Error = "Discount must be between 0 and 100.";
        OnPropertyChanged(nameof(HasError));
       return false;
       }

 if (EditStartDate.Date < DateTimeOffset.Now.Date)
            {
    Error = "Start date cannot be in the past.";
        OnPropertyChanged(nameof(HasError));
                return false;
    }

          if (EditEndDate <= EditStartDate)
   {
     Error = "End date must be after start date.";
           OnPropertyChanged(nameof(HasError));
  return false;
  }

       var productIds = SelectedProducts.Select(p => p.ProductId).ToList();
     var categoryIds = SelectedCategories.Select(c => c.CategoryId).ToList();

        if (!ValidateScope(EditScope, productIds, categoryIds, out var scopeError))
     {
              Error = scopeError;
       OnPropertyChanged(nameof(HasError));
         return false;
  }

            // Build local DateTimeOffsets correctly
            var startLocalDto = BuildLocalDateTimeOffset(EditStartDate);
         var endLocalDto = BuildLocalDateTimeOffset(EditEndDate);

        var input = new UpdatePromotionInput
            {
     Name = EditName,
       DiscountPercent = (int)discount,
   StartDate = startLocalDto.UtcDateTime,
    EndDate = endLocalDto.UtcDateTime,
                Scope = EditScope,
           ProductIds = EditScope == PromotionScope.Product ? productIds : null,
 CategoryIds = EditScope == PromotionScope.Category ? categoryIds : null
 };

   try
   {
   var result = await _promotionService.UpdatePromotionAsync(EditPromotionId, input);
                if (!result.Success)
              {
  Error = result.Message ?? "Update promotion failed.";
            OnPropertyChanged(nameof(HasError));
  return false;
     }

        IsOpen = false;
      await _reloadCallback();
       return true;
            }
    catch (Exception ex)
   {
       Error = ex.Message;
    OnPropertyChanged(nameof(HasError));
           return false;
     }
        }

   private static bool ValidateScope(PromotionScope scope, List<int> productIds, List<int> categoryIds, out string error)
      {
            error = string.Empty;
            switch (scope)
         {
    case PromotionScope.Product:
        if (productIds.Count == 0) { error = "PRODUCT scope requires at least 1 product selected."; return false; }
        return true;
  case PromotionScope.Category:
      if (categoryIds.Count == 0) { error = "CATEGORY scope requires at least 1 category selected."; return false; }
  return true;
                case PromotionScope.Order:
  return true;
       default:
              error = "Invalid scope.";
  return false;
            }
        }

      [RelayCommand] private void Cancel() => DoCancel();
        [RelayCommand] private async Task Confirm() => await DoConfirmAsync();

  // Add Open command so XAML can call EditVm.OpenCommand
        [RelayCommand]
   private async Task Open(PromotionItemDto? promotion)
     {
            if (promotion == null) return;
 Error = string.Empty;
         OnPropertyChanged(nameof(HasError));

            try
            {
                var detailRes = await _promotionService.GetPromotionByIdAsync(promotion.PromotionId);
          if (!detailRes.Success || detailRes.Data == null)
      {
          Error = detailRes.Message ?? "Cannot load promotion detail.";
         OnPropertyChanged(nameof(HasError));
  return;
             }

 await DoOpenAsync(promotion, detailRes.Data);
   }
       catch (Exception ex)
 {
      Error = ex.Message;
       OnPropertyChanged(nameof(HasError));
   }
        }

        /// <summary>
    /// Real-time sync when user toggles checkbox in ProductSelectorVm
    /// Call this whenever a product's IsSelected changes
    /// </summary>
    public void OnProductSelectionChanged(ProductItemDto product)
    {
        if (product == null) return;

        if (product.IsSelected)
     {
  // Add to selected if not already there
            if (!SelectedProducts.Any(sp => sp.ProductId == product.ProductId))
            {
     SelectedProducts.Add(product);
}
        }
      else
      {
   // Remove from selected if unchecked
var existing = SelectedProducts.FirstOrDefault(sp => sp.ProductId == product.ProductId);
    if (existing != null)
   {
 SelectedProducts.Remove(existing);
            }
     }
    }

    /// <summary>
    /// Real-time sync when user toggles checkbox in CategorySelectorVm
    /// Call this whenever a category's IsSelected changes
    /// </summary>
    public void OnCategorySelectionChanged(CategoryItemDto category)
    {
        if (category == null) return;

        if (category.IsSelected)
        {
  // Add to selected if not already there
            if (!SelectedCategories.Any(sc => sc.CategoryId == category.CategoryId))
            {
 SelectedCategories.Add(category);
            }
        }
   else
        {
          // Remove from selected if unchecked
       var existing = SelectedCategories.FirstOrDefault(sc => sc.CategoryId == category.CategoryId);
         if (existing != null)
     {
                SelectedCategories.Remove(existing);
   }
        }
    }
    }
}
