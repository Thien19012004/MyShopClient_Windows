using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using System;
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
        public PromotionScope NewScope { get => _newScope; set => SetProperty(ref _newScope, value); }

        private string? _newProductIdsText;
        public string? NewProductIdsText { get => _newProductIdsText; set => SetProperty(ref _newProductIdsText, value); }

        private string? _newCategoryIdsText;
        public string? NewCategoryIdsText { get => _newCategoryIdsText; set => SetProperty(ref _newCategoryIdsText, value); }

        public void DoOpen()
        {
            Error = string.Empty;
            NewName = string.Empty;
            NewDiscountPercentText = "10";
            NewStartDate = DateTimeOffset.Now;
            NewEndDate = DateTimeOffset.Now.AddDays(7);
            NewScope = PromotionScope.Order;
            NewProductIdsText = string.Empty;
            NewCategoryIdsText = string.Empty;
            IsOpen = true;
            OnPropertyChanged(nameof(HasError));
        }

        public void DoCancel()
        {
            IsOpen = false;
            Error = string.Empty;
            OnPropertyChanged(nameof(HasError));
        }

        private static DateTimeOffset BuildLocalDateTimeOffset(DateTimeOffset selectedDate)
        {
            // Use today's current time when selecting today to avoid timezone shift to previous day when converting to UTC.
            var selectedDateOnly = selectedDate.Date; // DateTime (date component) with Kind=Unspecified
            var now = DateTime.Now;
            if (selectedDateOnly.Date == now.Date)
            {
                // combine today's date with current local time
                var combined = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second, DateTimeKind.Local);
                return new DateTimeOffset(combined);
            }
            else
            {
                // preserve date at midnight local
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
                Error = "Discount must be between0 and100.";
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

            var productIds = ParseIds(NewProductIdsText);
            var categoryIds = ParseIds(NewCategoryIdsText);

            if (!ValidateScope(NewScope, productIds, categoryIds, out var scopeError))
            {
                Error = scopeError;
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            // Build local DateTimeOffsets correctly
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
                    if (productIds.Count == 0) { error = "PRODUCT scope requires productIds."; return false; }
                    if (categoryIds.Count > 0) { error = "PRODUCT scope must not have categoryIds."; return false; }
                    return true;
                case PromotionScope.Category:
                    if (categoryIds.Count == 0) { error = "CATEGORY scope requires categoryIds."; return false; }
                    if (productIds.Count > 0) { error = "CATEGORY scope must not have productIds."; return false; }
                    return true;
                case PromotionScope.Order:
                    if (productIds.Count > 0 || categoryIds.Count > 0) { error = "ORDER scope must not have productIds/categoryIds."; return false; }
                    return true;
                default:
                    error = "Invalid scope.";
                    return false;
            }
        }

        private static System.Collections.Generic.List<int> ParseIds(string? text)
        {
            if (string.IsNullOrWhiteSpace(text)) return new();
            return text.Split(',')
            .Select(s => s.Trim())
            .Where(s => int.TryParse(s, out _))
            .Select(int.Parse)
            .Where(id => id > 0)
            .Distinct()
            .ToList();
        }

        // Commands for XAML
        [RelayCommand] private void Open() => DoOpen();
        [RelayCommand] private void Cancel() => DoCancel();
        [RelayCommand] private async Task Confirm() => await DoConfirmAsync();
    }
}
