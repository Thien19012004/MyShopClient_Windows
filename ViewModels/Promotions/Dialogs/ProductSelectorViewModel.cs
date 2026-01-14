using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Product;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MyShopClient.ViewModels.Promotions
{
    /// <summary>
    /// Product Selector dialog - shows product list with checkboxes
  /// Selection state is managed by the caller (AddVm/EditVm via SelectedProducts)
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
        [ObservableProperty] private int selectedCount = 0;

    // Callback to apply selected states when products load (for real-time search sync)
    public Action<IEnumerable<ProductItemDto>>? OnProductsLoaded { get; set; }

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
 RecalculateSelectedCount();
     return;
   }

    var pageData = result.Data;

     // Clear old products BUT preserve IsSelected state for items still in SelectedProducts
     // This way when we restore checkboxes, they have correct state
     Products.Clear();

   foreach (var p in pageData.Items)
 {
 // Subscribe to IsSelected changes for real-time count update
         p.PropertyChanged += (s, e) =>
 {
    if (e.PropertyName == nameof(ProductItemDto.IsSelected))
  {
      RecalculateSelectedCount();
   }
   };
 Products.Add(p);
}

     CurrentPage = pageData.Page;
    TotalPages = Math.Max(1, pageData.TotalPages);

 RecalculateSelectedCount();
 
 // Call callback to apply selected states if set
 // This restores checkboxes for items that match SelectedProducts (from AddVm/EditVm)
 OnProductsLoaded?.Invoke(Products);
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

      public void ResetSearchState()
    {
    SearchText = string.Empty;
     CurrentPage = 1;
   }

 public void UpdateSelectedCount(int count)
      {
      SelectedCount = count;
      }

  public void RecalculateSelectedCount()
      {
  // Count from SelectedProducts cache in parent ViewModel (AddVm/EditVm)
        // NOT from Products which is filtered by search
  // The actual count is managed by OnProductsLoaded callback
  SelectedCount = Products.Count(p => p.IsSelected);
      }
    /// <summary>
    /// Apply selected state to items in selector based on SelectedProducts
    /// Called when products are loaded to restore checkboxes based on current selections
    /// </summary>
    public void ApplySelectedStatesToProducts(IEnumerable<ProductItemDto> selectedProducts)
    {
     if (selectedProducts == null) return;

        // Mark all products in selector that match selectedProducts
        foreach (var product in Products)
        {
      product.IsSelected = selectedProducts.Any(sp => sp.ProductId == product.ProductId);
        }
    }

    /// <summary>
    /// Get all checked products from current page for syncing
    /// </summary>
public IEnumerable<ProductItemDto> GetCheckedProducts()
{
        return Products.Where(p => p.IsSelected);
    }
  }
}
