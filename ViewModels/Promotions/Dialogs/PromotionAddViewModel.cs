using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using System;
using System.Collections.ObjectModel;
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

        // ? Collections thay vì text
     public ObservableCollection<ProductItemDto> SelectedProducts { get; }
        public ObservableCollection<CategoryItemDto> SelectedCategories { get; }

        // ? Track dialogs
        private bool _isProductSelectorOpen;
        public bool IsProductSelectorOpen { get => _isProductSelectorOpen; set => SetProperty(ref _isProductSelectorOpen, value); }

        private bool _isCategorySelectorOpen;
     public bool IsCategorySelectorOpen { get => _isCategorySelectorOpen; set => SetProperty(ref _isCategorySelectorOpen, value); }

        // ? Lock states d?a vào scope
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
     SelectedProducts.Clear();
   SelectedCategories.Clear();
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

      // ? Use selected collections instead of parsing text
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

    private static bool ValidateScope(PromotionScope scope, System.Collections.Generic.List<int> productIds, System.Collections.Generic.List<int> categoryIds, out string error)
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
