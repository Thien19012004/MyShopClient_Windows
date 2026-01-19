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
            product.IsSelected = false;
        }

        [RelayCommand]
        private void RemoveCategory(CategoryItemDto? category)
        {
            if (category == null) return;

            SelectedCategories.Remove(category);
            category.IsSelected = false;
        }



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


        public void ApplySelectedStatesToProducts(IEnumerable<ProductItemDto> productsFromSelector)
        {
            foreach (var product in productsFromSelector)
            {
                product.IsSelected = SelectedProducts.Any(sp => sp.ProductId == product.ProductId);
            }
        }

 
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

            // Use selected collections - they are the source of truth
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
