using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{
    public partial class PromotionEditViewModel : ObservableObject
    {
        private readonly IPromotionService _promotionService;
        private readonly Func<Task> _reloadCallback;

        public PromotionEditViewModel(IPromotionService promotionService, Func<Task> reloadCallback)
        {
            _promotionService = promotionService ?? throw new ArgumentNullException(nameof(promotionService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
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
        public PromotionScope EditScope { get => _editScope; set => SetProperty(ref _editScope, value); }

        private string? _editProductIdsText;
        public string? EditProductIdsText { get => _editProductIdsText; set => SetProperty(ref _editProductIdsText, value); }

        private string? _editCategoryIdsText;
        public string? EditCategoryIdsText { get => _editCategoryIdsText; set => SetProperty(ref _editCategoryIdsText, value); }

        private bool _editIsActive;
        public bool EditIsActive { get => _editIsActive; set => SetProperty(ref _editIsActive, value); }

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
            EditProductIdsText = string.Join(",", detail.ProductIds);
            EditCategoryIdsText = string.Join(",", detail.CategoryIds);

            var nowLocal = DateTime.Now;
            EditIsActive = nowLocal >= startLocal && nowLocal <= endLocal;

            if (EditIsActive)
            {
                Error = "This promotion is currently active and cannot be edited.";
            }

            IsOpen = true;
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
                Error = "Discount must be between0 and100.";
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

            var productIds = ParseIds(EditProductIdsText);
            var categoryIds = ParseIds(EditCategoryIdsText);

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

        [RelayCommand] private void Cancel() => DoCancel();
        [RelayCommand] private async Task Confirm() => await DoConfirmAsync();

        // Add Open command so XAML can call Dialogs_EditVm.OpenCommand
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
    }
}
