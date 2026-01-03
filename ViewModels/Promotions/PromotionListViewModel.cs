using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Promotion;
using MyShopClient.Services.AppSettings;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Threading;

namespace MyShopClient.ViewModels
{
    // Use SelectableListViewModel to get selection & bulk-delete behavior
    public partial class PromotionListViewModel : SelectableListViewModel<PromotionItemDto>
    {
        private readonly IPromotionService _promotionService;
        private readonly IAppSettingsService _appSettings;

        public ObservableCollection<PromotionItemDto> Promotions { get; } = new();

        // debounce version for search-as-you-type
        private int _searchVersion;

        public ObservableCollection<PromotionScope> ScopeOptions { get; } = new(new[] { PromotionScope.Product, PromotionScope.Category, PromotionScope.Order });

        // For the filter dropdown we expose a display list including "All"
        public ObservableCollection<string> ScopeFilterOptions { get; } = new(new[] { "All", "Product", "Category", "Order" });

        [ObservableProperty] private string? searchText;

        partial void OnSearchTextChanged(string? value)
        {
            _ = DebounceSearchAsync();
        }

        private async Task DebounceSearchAsync()
        {
            var version = Interlocked.Increment(ref _searchVersion);
            try
            {
                await Task.Delay(300);
                if (version != _searchVersion) return;

                await LoadPageAsync(1);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(HasError));
            }
        }

        [ObservableProperty] private bool onlyActive = false;
        // This nullable property is what the backend query uses
        [ObservableProperty] private PromotionScope? selectedScopeFilter;

        // Selected display string from the UI filter (can be "All")
        [ObservableProperty] private string selectedScopeDisplay = "All";

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        [ObservableProperty] private int totalPromotionsOnPage;
        [ObservableProperty] private int totalActiveOnPage;

        // child VMs
        private ViewModels.Promotions.PromotionAddViewModel? _addVm;
        private ViewModels.Promotions.PromotionEditViewModel? _editVm;

        public ViewModels.Promotions.PromotionAddViewModel AddVm => _addVm ??= new ViewModels.Promotions.PromotionAddViewModel(_promotionService, async () => await LoadPageAsync(CurrentPage));
        public ViewModels.Promotions.PromotionEditViewModel EditVm => _editVm ??= new ViewModels.Promotions.PromotionEditViewModel(_promotionService, async () => await LoadPageAsync(CurrentPage));

        public PromotionListViewModel(IPromotionService promotionService, IAppSettingsService appSettings)
        : base(appSettings, s => s.PromotionsPageSize)
        {
            _promotionService = promotionService;
            _appSettings = appSettings;

            // apply persisted page size from settings
            PageSize = _appSettings.PromotionsPageSize;

            // attach selection tracking for Promotions collection
            AttachSelectionTracker(Promotions);

            _ = InitializeAsync();

            Promotions.CollectionChanged += (s, e) =>
            {
                if (e.NewItems != null)
                {
                    foreach (PromotionItemDto it in e.NewItems)
                        it.PropertyChanged += PromotionItem_PropertyChanged;
                }
                if (e.OldItems != null)
                {
                    foreach (PromotionItemDto it in e.OldItems)
                        it.PropertyChanged -= PromotionItem_PropertyChanged;
                }
                UpdateSelectionState();
            };
        }

        private void PromotionItem_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(PromotionItemDto.IsSelected))
                UpdateSelectionState();
        }

        // Legacy-friendly property names used in XAML
        public int SelectedPromotionsCount => SelectedItems.Count;
        public bool HasSelectedPromotions => SelectedItems.Count >0;

        private void UpdateSelectionState()
        {
            // Notify legacy bindings
            OnPropertyChanged(nameof(SelectedPromotionsCount));
            OnPropertyChanged(nameof(HasSelectedPromotions));
        }

        private async Task InitializeAsync() => await LoadPageAsync();

        // Implement core page loading used by base PagedListViewModel
        protected override async Task LoadPageCoreAsync(int page, int pageSize)
        {
            ErrorMessage = string.Empty;

            var options = new PromotionQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                Search = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText,
                OnlyActive = OnlyActive,
                At = OnlyActive ? DateTime.UtcNow : null,
                Scope = SelectedScopeFilter
            };

            try
            {
                var result = await _promotionService.GetPromotionsAsync(options);
                if (!result.Success || result.Data == null)
                {
                    ErrorMessage = result.Message ?? "Cannot load promotions.";
                    Promotions.Clear();
                    SetPageResult(1, pageSize,0,1);
                    TotalPromotionsOnPage = TotalActiveOnPage =0;
                    return;
                }

                var pageData = result.Data;
                Promotions.Clear();
                foreach (var promo in pageData.Items) Promotions.Add(promo);

                // Debug
                System.Diagnostics.Debug.WriteLine($"[Promotions] Loaded page={pageData.Page} pageSize={pageData.PageSize} totalItems={pageData.TotalItems} totalPages={pageData.TotalPages}");

                SetPageResult(pageData.Page, pageData.PageSize, pageData.TotalItems, pageData.TotalPages);

                // ensure UI bindings update for base paging properties
                OnPropertyChanged(nameof(CurrentPage));
                OnPropertyChanged(nameof(PageSize));
                OnPropertyChanged(nameof(TotalItems));
                OnPropertyChanged(nameof(TotalPages));

                TotalPromotionsOnPage = Promotions.Count;
                TotalActiveOnPage = Promotions.Count(p => p.IsActive);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Promotions.Clear();
                SetPageResult(1, pageSize,0,1);
                TotalPromotionsOnPage = TotalActiveOnPage =0;
            }
            finally
            {
                OnPropertyChanged(nameof(HasError));
            }
        }

        // ========= Commands =========
        [RelayCommand]
        private Task ApplyFilterAsync()
        {
            // Map the display selection to the nullable enum used for querying
            if (string.IsNullOrWhiteSpace(SelectedScopeDisplay) || SelectedScopeDisplay == "All")
                SelectedScopeFilter = null;
            else if (Enum.TryParse<PromotionScope>(SelectedScopeDisplay, out var parsed))
                SelectedScopeFilter = parsed;
            else
                SelectedScopeFilter = null;

            return LoadPageAsync(1);
        }

        // ------ Delete (single) ------
        [RelayCommand]
        private async Task DeletePromotionAsync(PromotionItemDto? promotion)
        {
            if (promotion == null) return;
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _promotionService.DeletePromotionAsync(promotion.PromotionId);
                if (!result.Success || (result.Data != null && result.Data == false))
                {
                    ErrorMessage = result.Message ?? "Delete promotion failed.";
                }
                else
                {
                    // remove locally so UI updates immediately
                    Promotions.Remove(promotion);
                    UpdateSelectionState();

                    // reload to re-sync
                    await Task.Delay(200);
                    await LoadPageAsync(CurrentPage);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }
        }

        // Derived class hook: used by SelectableListViewModel bulk delete flow
        protected override async Task<bool> DeleteItemsAsync(PromotionItemDto[] items)
        {
            if (items == null || items.Length ==0) return false;

            var attempted = items.Length;
            var success =0;
            var failedIds = new List<int>();

            foreach (var it in items)
            {
                try
                {
                    var res = await _promotionService.DeletePromotionAsync(it.PromotionId);
                    if (res.Success && (res.Data == null || res.Data == true))
                    {
                        // remove locally
                        var local = Promotions.FirstOrDefault(p => p.PromotionId == it.PromotionId);
                        if (local != null) Promotions.Remove(local);
                        success++;
                    }
                    else
                    {
                        failedIds.Add(it.PromotionId);
                    }
                }
                catch
                {
                    failedIds.Add(it.PromotionId);
                }
            }

            if (success >0)
            {
                // re-sync page
                await Task.Delay(200);
                await LoadPageAsync(CurrentPage);
            }

            if (failedIds.Count >0 && success ==0)
            {
                ErrorMessage = $"Failed to delete any of the selected {attempted} promotion(s).";
            }
            else if (failedIds.Count >0)
            {
                ErrorMessage = $"Deleted {success} of {attempted} promotion(s). {failedIds.Count} failed. Failed IDs: {string.Join(",", failedIds)}";
            }

            OnPropertyChanged(nameof(HasError));
            return success >0;
        }

        // Single delete confirmation (explicit properties)
        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen { get => _isDeleteConfirmOpen; set => SetProperty(ref _isDeleteConfirmOpen, value); }

        private PromotionItemDto? _promotionToDelete;
        public PromotionItemDto? PromotionToDelete { get => _promotionToDelete; set => SetProperty(ref _promotionToDelete, value); }

        private string _deleteConfirmMessage = string.Empty;
        public string DeleteConfirmMessage { get => _deleteConfirmMessage; set => SetProperty(ref _deleteConfirmMessage, value); }

        [RelayCommand]
        private void OpenDeleteConfirm(PromotionItemDto? promotion)
        {
            if (promotion == null) return;
            PromotionToDelete = promotion;
            DeleteConfirmMessage = $"Are you sure you want to delete Promotion #{promotion.PromotionId}?";
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteConfirm()
        {
            IsDeleteConfirmOpen = false;
            PromotionToDelete = null;
        }

        [RelayCommand]
        private async Task ConfirmDeletePromotionAsync()
        {
            IsDeleteConfirmOpen = false;
            if (PromotionToDelete == null) return;
            if (IsBusy) return;

            // reuse DeletePromotionAsync
            await DeletePromotionAsync(PromotionToDelete);

            PromotionToDelete = null;
        }
    }
}
