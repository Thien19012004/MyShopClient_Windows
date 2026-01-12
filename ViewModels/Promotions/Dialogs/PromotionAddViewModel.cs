using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{
    public partial class PromotionAddViewModel : ObservableObject
    {
        private readonly IPromotionService _promotionService;
        private readonly Func<Task> _reloadCallback;

        // Track selected IDs to preserve selection state across dialog opens
        private readonly HashSet<int> _selectedProductIds = new();
        private readonly HashSet<int> _selectedCategoryIds = new();

        public PromotionAddViewModel(IPromotionService promotionService, Func<Task> reloadCallback)
        {
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
            
            SelectedProducts = new ObservableCollection<ProductItemDto>();
            SelectedCategories = new ObservableCollection<CategoryItemDto>();
        }

        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        private string? _error;
        public string? Error { get => _error; set { SetProperty(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        private string? _newName;
        public string? NewName { get => _newName; set => SetProperty(ref _newName, value); }

        private string? _newDiscountPercentText = "10";
        public string? NewDiscountPercentText { get => _newDiscountPercentText; set => SetProperty(ref _newDiscountPercentText, value); }

        private DateTimeOffset _newStartDate = DateTimeOffset.Now;
        public DateTimeOffset NewStartDate { get => _newStartDate; set => SetProperty(ref _newStartDate, value); }

        private DateTimeOffset _newEndDate = DateTimeOffset.Now.AddDays(7);
        public DateTimeOffset NewEndDate { get => _newEndDate; set => SetProperty(ref _newEndDate, value); }

        private PromotionScope _newScope = PromotionScope.Order;
        public PromotionScope NewScope 
     { 
        get => _newScope; 
            set 
        { 
            SetProperty(ref _newScope, value);
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
        public bool CanSelectProducts => NewScope == PromotionScope.Product;
        public bool CanSelectCategories => NewScope == PromotionScope.Category;

        public void DoOpen()
        {
      Error = string.Empty;
   NewName = string.Empty;
NewDiscountPercentText = "10";
   NewStartDate = DateTimeOffset.Now;
            NewEndDate = DateTimeOffset.Now.AddDays(7);
            NewScope = PromotionScope.Order;
     
            // Don't clear selected items - preserve them
            // SelectedProducts and SelectedCategories keep their previous state
            
            IsOpen = true;
 OnPropertyChanged(nameof(HasError));
        }

        public void DoCancel()
        {
            IsOpen = false;
 Error = string.Empty;
            OnPropertyChanged(nameof(HasError));
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
            _selectedProductIds.Remove(product.ProductId);
        }

        [RelayCommand]
        private void RemoveCategory(CategoryItemDto? category)
        {
         if (category == null) return;
    
  SelectedCategories.Remove(category);
            _selectedCategoryIds.Remove(category.CategoryId);
        }

        /// <summary>
        /// Syncs the ProductSelectorVm's IsSelected state with our SelectedProducts collection
        /// and tracks selected IDs for preservation across dialog opens
 /// </summary>
        public void SyncSelectedProducts(IEnumerable<ProductItemDto> productsFromSelector)
        {
 var selectedFromSelector = productsFromSelector.Where(p => p.IsSelected).ToList();
   
  // Add newly selected items
            foreach (var product in selectedFromSelector)
       {
   if (!_selectedProductIds.Contains(product.ProductId))
              {
      SelectedProducts.Add(product);
      _selectedProductIds.Add(product.ProductId);
          }
            }
            
     // Remove unselected items
            var toRemove = SelectedProducts.Where(p => !selectedFromSelector.Any(s => s.ProductId == p.ProductId)).ToList();
   foreach (var product in toRemove)
     {
SelectedProducts.Remove(product);
       _selectedProductIds.Remove(product.ProductId);
  }
   }

        /// <summary>
        /// Syncs the CategorySelectorVm's IsSelected state with our SelectedCategories collection
        /// and tracks selected IDs for preservation across dialog opens
        /// </summary>
    public void SyncSelectedCategories(IEnumerable<CategoryItemDto> categoriesFromSelector)
        {
            var selectedFromSelector = categoriesFromSelector.Where(c => c.IsSelected).ToList();
 
            // Add newly selected items
            foreach (var category in selectedFromSelector)
    {
     if (!_selectedCategoryIds.Contains(category.CategoryId))
              {
    SelectedCategories.Add(category);
     _selectedCategoryIds.Add(category.CategoryId);
     }
       }
            
      // Remove unselected items
      var toRemove = SelectedCategories.Where(c => !selectedFromSelector.Any(s => s.CategoryId == c.CategoryId)).ToList();
            foreach (var category in toRemove)
   {
              SelectedCategories.Remove(category);
                _selectedCategoryIds.Remove(category.CategoryId);
   }
        }

        /// <summary>
        /// Marks products in the selector as selected if they're in our SelectedProducts collection
     /// </summary>
        public void ApplySelectedStatesToProducts(IEnumerable<ProductItemDto> productsFromSelector)
        {
         foreach (var product in productsFromSelector)
   {
         product.IsSelected = _selectedProductIds.Contains(product.ProductId);
            }
        }

        /// <summary>
        /// Marks categories in the selector as selected if they're in our SelectedCategories collection
   /// </summary>
        public void ApplySelectedStatesToCategories(IEnumerable<CategoryItemDto> categoriesFromSelector)
        {
            foreach (var category in categoriesFromSelector)
  {
          category.IsSelected = _selectedCategoryIds.Contains(category.CategoryId);
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

        public async Task<bool> DoConfirmAsync()
    {
     Error = string.Empty;
 OnPropertyChanged(nameof(HasError));

     if (string.IsNullOrWhiteSpace(NewName))
{
       Error = "Name is required.";
  OnPropertyChanged(nameof(HasError));
   return false;
            }

            if (!decimal.TryParse(NewDiscountPercentText, out var discount) || discount <= 0 || discount > 100)
      {
       Error = "Discount must be between 0 and 100.";
         OnPropertyChanged(nameof(HasError));
       return false;
        }

            if (NewStartDate.Date < DateTimeOffset.Now.Date)
         {
      Error = "Start date cannot be in the past.";
       OnPropertyChanged(nameof(HasError));
             return false;
      }

   if (NewEndDate <= NewStartDate)
            {
    Error = "End date must be after start date.";
        OnPropertyChanged(nameof(HasError));
         return false;
      }

      // Use selected collections
        var productIds = SelectedProducts.Select(p => p.ProductId).ToList();
            var categoryIds = SelectedCategories.Select(c => c.CategoryId).ToList();

            if (!ValidateScope(NewScope, productIds, categoryIds, out var scopeError))
{
                Error = scopeError;
     OnPropertyChanged(nameof(HasError));
       return false;
  }

            var startLocalDto = BuildLocalDateTimeOffset(NewStartDate);
 var endLocalDto = BuildLocalDateTimeOffset(NewEndDate);

    var input = new CreatePromotionInput
    {
            Name = NewName,
          DiscountPercent = (int)discount,
           StartDate = startLocalDto.UtcDateTime,
      EndDate = endLocalDto.UtcDateTime,
           Scope = NewScope,
       ProductIds = NewScope == PromotionScope.Product ? productIds : null,
         CategoryIds = NewScope == PromotionScope.Category ? categoryIds : null
            };

     try
            {
                var result = await _promotionService.CreatePromotionAsync(input);
         if (!result.Success)
     {
      Error = result.Message ?? "Create promotion failed.";
     OnPropertyChanged(nameof(HasError));
       return false;
                }

                // Clear selections after successful creation
     IsOpen = false;
             SelectedProducts.Clear();
     SelectedCategories.Clear();
     _selectedProductIds.Clear();
   _selectedCategoryIds.Clear();
                
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

        // Commands for XAML
        [RelayCommand] private void Open() => DoOpen();
        [RelayCommand] private void Cancel() => DoCancel();
  [RelayCommand] private async Task Confirm() => await DoConfirmAsync();
    }
}
