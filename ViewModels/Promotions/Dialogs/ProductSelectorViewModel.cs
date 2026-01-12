using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Product;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Promotions
{
    /// <summary>
    /// Reusable Product Selector dialog - shows product list with checkboxes
    /// </summary>
    public partial class ProductSelectorViewModel : ObservableObject
    {
     private readonly IProductService _productService;
        private int _searchVersion;
        private CancellationTokenSource? _loadCts;

        public ObservableCollection<ProductItemDto> Products { get; } = new();

        [ObservableProperty] private string? searchText;
        [ObservableProperty] private int? selectedCategoryId;
   [ObservableProperty] private string? errorMessage;
     [ObservableProperty] private bool isBusy;
        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int totalPages = 1;
     [ObservableProperty] private int pageSize = 10;

    public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

 public ProductSelectorViewModel(IProductService productService)
        {
 _productService = productService ?? throw new ArgumentNullException(nameof(productService));
 }

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

                CurrentPage = 1;
        await LoadProductsAsync();
   }
      catch (Exception ex)
            {
        ErrorMessage = ex.Message;
    }
        }

 public async Task LoadProductsAsync()
        {
            if (IsBusy) return;

            IsBusy = true;
ErrorMessage = string.Empty;

            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
 var token = _loadCts.Token;

         try
         {
      var options = new ProductQueryOptions
  {
                    Page = CurrentPage,
           PageSize = PageSize,
       Search = SearchText,
        CategoryId = SelectedCategoryId,
         SortField = ProductSortField.Name,
   SortAscending = true
     };

                var result = await _productService.GetProductsAsync(options, token);

      if (!result.Success || result.Data == null)
           {
  ErrorMessage = result.Message ?? "Cannot load products.";
     Products.Clear();
     TotalPages = 1;
         return;
       }

    var pageData = result.Data;
       Products.Clear();

         foreach (var p in pageData.Items)
        {
           Products.Add(p);
        }

       CurrentPage = pageData.Page;
    TotalPages = Math.Max(1, pageData.TotalPages);
            }
      catch (OperationCanceledException)
            {
  // ignore cancelled load
    }
            catch (Exception ex)
   {
           ErrorMessage = ex.Message;
       Products.Clear();
   }
       finally
          {
        IsBusy = false;
     }
        }

        [RelayCommand]
        private async Task NextPage()
        {
  if (CurrentPage < TotalPages)
            {
     CurrentPage++;
         await LoadProductsAsync();
            }
   }

        [RelayCommand]
        private async Task PreviousPage()
     {
      if (CurrentPage > 1)
   {
 CurrentPage--;
         await LoadProductsAsync();
   }
 }

      [RelayCommand]
        private async Task ApplyFilter()
    {
            CurrentPage = 1;
   await LoadProductsAsync();
      }
    }
}
