using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services.AppSettings;
using System;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    // Generic base viewmodel that encapsulates common paging behavior.
    public abstract partial class PagedListViewModel<TItem> : ObservableObject
    {
        private int _currentPage = 1;
        private int _pageSize = 10;
        private int _totalPages = 1;
        private int _totalItems = 0;
        private bool _isBusy;
        private string? _errorMessage;

        protected IAppSettingsService? AppSettings { get; }

        public int CurrentPage { get => _currentPage; protected set => SetProperty(ref _currentPage, value); }
        public int PageSize { get => _pageSize; protected set => SetProperty(ref _pageSize, value); }
        public int TotalPages { get => _totalPages; protected set => SetProperty(ref _totalPages, value); }
        public int TotalItems { get => _totalItems; protected set => SetProperty(ref _totalItems, value); }

        public bool IsBusy { get => _isBusy; protected set => SetProperty(ref _isBusy, value); }
        public string? ErrorMessage { get => _errorMessage; protected set => SetProperty(ref _errorMessage, value); }
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        protected PagedListViewModel(IAppSettingsService? appSettings = null, Func<IAppSettingsService, int>? pageSizeSelector = null)
        {
            AppSettings = appSettings;
            if (appSettings != null && pageSizeSelector != null)
            {
                try
                {
                    PageSize = pageSizeSelector(appSettings);
                }
                catch
                {
                    // ignore and keep default
                }
            }
        }

        // Public wrapper that handles IsBusy/Error guard and delegates actual data load to derived classes
        public async Task LoadPageAsync(int? page = null)
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            if (page.HasValue)
                CurrentPage = page.Value;

            try
            {
                await LoadPageCoreAsync(CurrentPage, PageSize);
            }
            catch (OperationCanceledException)
            {
                // ignore cancellations triggered by debounce/refresh
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                // Let derived class decide how to clear its collection; still set sensible paging defaults
                SetPageResult(1, PageSize, 0, 1);
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }
        }

        // Derived classes implement how to fetch and populate their item collection for the given page/pageSize
        protected abstract Task LoadPageCoreAsync(int page, int pageSize);

        // Helper for derived classes to update paging properties after receiving page result
        protected void SetPageResult(int page, int pageSize, int totalItems, int totalPages)
        {
            CurrentPage = page;
            PageSize = pageSize;
            TotalItems = totalItems;
            TotalPages = Math.Max(1, totalPages);
        }

        // Navigation helpers exposed as commands
        [RelayCommand]
        public Task NextPageAsync()
        {
            return CurrentPage < TotalPages ? LoadPageAsync(CurrentPage + 1) : Task.CompletedTask;
        }

        [RelayCommand]
        public Task PreviousPageAsync()
        {
            return CurrentPage > 1 ? LoadPageAsync(CurrentPage - 1) : Task.CompletedTask;
        }

        public Task ReloadCurrentPageAsync() => LoadPageAsync(CurrentPage);
    }
}
