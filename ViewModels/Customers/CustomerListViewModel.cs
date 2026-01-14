using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.AppSettings;
using MyShopClient.Services.Customer;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

namespace MyShopClient.ViewModels
{
    // Core paging, collections and filter responsibilities
    public partial class CustomerListViewModel : SelectableListViewModel<CustomerListItemDto>
    {
        private readonly ICustomerService _customerService;

        public ObservableCollection<CustomerListItemDto> Customers { get; } = new();

        // debounce version for search-as-you-type
        private int _searchVersion;

        // Filter
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

        // Summary
        public int TotalCustomersOnPage => Customers.Count;
        public int SelectedCustomersCount => SelectedItems.Count;
        public bool HasSelectedCustomers => SelectedItems.Count > 0;

        // Delete / single-delete state
        [ObservableProperty] private bool isDeleteConfirmOpen;
        [ObservableProperty] private CustomerListItemDto? customerToDelete;
        // Note: bulk-delete dialog state is provided by SelectableListViewModel

        // Edit dialog order count shown in XAML
        [ObservableProperty] private int editCustomerOrderCount;

        // Bulk delete message (computed) - used by UI and by subscription
        public string BulkDeleteConfirmMessage => $"Are you sure you want to delete {SelectedCustomersCount} selected customer{(SelectedCustomersCount != 1 ? "s" : "")}?";

        public CustomerListViewModel(ICustomerService customerService, IAppSettingsService appSettings)
        : base(appSettings, s => s.CustomersPageSize)
        {
            _customerService = customerService;

            // Attach selection tracking for items
            AttachSelectionTracker(Customers);
            // When SelectedItems changes, raise summary properties
            SelectedItems.CollectionChanged += (s, e) =>
            {
                OnPropertyChanged(nameof(HasSelectedCustomers));
                OnPropertyChanged(nameof(SelectedCustomersCount));
                OnPropertyChanged(nameof(BulkDeleteConfirmMessage));
            };

            _ = LoadPageAsync();
        }

        protected override async Task LoadPageCoreAsync(int page, int pageSize)
        {
            var options = new CustomerQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                Search = SearchText
            };

            var result = await _customerService.GetCustomersAsync(options);

            if (!result.Success || result.Data == null)
            {
                ErrorMessage = result.Message ?? "Cannot load customers.";
                Customers.Clear();
                SetPageResult(1, pageSize, 0, 1);
                return;
            }

            var pageData = result.Data;

            Customers.Clear();
            foreach (var c in pageData.Items)
            {
                Customers.Add(c);
            }

            SetPageResult(pageData.Page, pageData.PageSize, pageData.TotalItems, pageData.TotalPages);
            OnPropertyChanged(nameof(TotalCustomersOnPage));
        }

        // Filter / paging commands
        [RelayCommand]
        private Task ApplyFilterAsync() => LoadPageAsync(1);

        [RelayCommand]
        private Task SearchAsync() => LoadPageAsync(1);
    }
}
