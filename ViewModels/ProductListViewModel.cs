using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Models.Categories;
using MyShopClient.Models.Common;
using MyShopClient.Models.Products;
using MyShopClient.Services;
using MyShopClient.ViewModels.Common;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    /// <summary>
    /// Category option for dropdown selection
    /// </summary>
    public class CategoryOption
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>
    /// Main view model for product list management
    /// Handles product list, filtering, pagination, and CRUD operations
    /// </summary>
    public partial class ProductListViewModel : BaseViewModel
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        // Internal flag for preventing concurrent load operations
        private bool _isLoadingProducts;

        // ====== Collections ==========
        public ObservableCollection<ProductItemDto> Products { get; } = new();
        public ObservableCollection<CategoryOption> Categories { get; } = new();
        public ObservableCollection<string> SortOptions { get; } =
      new(new[] { "Name (A-Z)", "Name (Z-A)", "Price (Low → High)", "Price (High → Low)" });

        public ObservableCollection<CategoryItemDto> CategoryItems { get; } = new();

        // ====== Product List Filtering & Pagination ======
        [ObservableProperty] private CategoryOption? selectedCategory;
        [ObservableProperty] private string? searchText;
        [ObservableProperty] private string? minPriceText;
        [ObservableProperty] private string? maxPriceText;
        [ObservableProperty] private string selectedSortOption = "Name (A-Z)";

        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private int totalPages = 1;
        [ObservableProperty] private int totalItems = 0;

        // ====== Add Product Dialog ======
        [ObservableProperty] private bool isAddDialogOpen;
        [ObservableProperty] private string? addDialogError;
        public bool HasAddDialogError => !string.IsNullOrWhiteSpace(AddDialogError);

        [ObservableProperty] private string? newProductSku;
        [ObservableProperty] private string? newProductName;
        [ObservableProperty] private string? newProductImportPriceText;
        [ObservableProperty] private string? newProductSalePriceText;
        [ObservableProperty] private string? newProductStockQuantityText;
        [ObservableProperty] private string? newProductDescription;
        [ObservableProperty] private string? newProductImagePath;
        [ObservableProperty] private CategoryOption? newProductCategory;

        partial void OnAddDialogErrorChanged(string? value) => OnPropertyChanged(nameof(HasAddDialogError));

        // ====== Edit Product Dialog ======
        [ObservableProperty] private bool isEditDialogOpen;
        [ObservableProperty] private string? editDialogError;
        public bool HasEditDialogError => !string.IsNullOrWhiteSpace(EditDialogError);

        [ObservableProperty] private int editingProductId;
        [ObservableProperty] private string? editProductSku;
        [ObservableProperty] private string? editProductName;
        [ObservableProperty] private string? editImportPriceText;
        [ObservableProperty] private string? editSalePriceText;
        [ObservableProperty] private string? editStockQuantityText;
        [ObservableProperty] private string? editDescription;
        [ObservableProperty] private string? editImagePath;
        [ObservableProperty] private CategoryOption? editCategory;

        partial void OnEditDialogErrorChanged(string? value) => OnPropertyChanged(nameof(HasEditDialogError));

        // ====== Category Management Dialog ======
        [ObservableProperty] private bool isCategoryDialogOpen;
        [ObservableProperty] private string? categorySearchText;
        [ObservableProperty] private string? categoryNameText;
        [ObservableProperty] private string? categoryDescriptionText;
        [ObservableProperty] private CategoryItemDto? selectedCategoryItem;
        [ObservableProperty] private string? categoryDialogError;
        public bool HasCategoryDialogError => !string.IsNullOrWhiteSpace(CategoryDialogError);

        partial void OnCategoryDialogErrorChanged(string? value) => OnPropertyChanged(nameof(HasCategoryDialogError));

        partial void OnSelectedCategoryItemChanged(CategoryItemDto? value)
        {
            if (value == null)
            {
                CategoryNameText = string.Empty;
                CategoryDescriptionText = string.Empty;
            }
            else
            {
                CategoryNameText = value.Name;
                CategoryDescriptionText = value.Description;
            }
        }

        public ProductListViewModel(IProductService productService, ICategoryService categoryService)
        {
            _productService = productService;
            _categoryService = categoryService;

            _ = InitializeAsync();
        }

        // ========== INITIALIZATION ==========
        private async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await LoadPageAsync();
        }

        // ========== CATEGORY LOADING ==========
        private async Task LoadCategoriesAsync()
        {
            Categories.Clear();
            Categories.Add(new CategoryOption { Id = null, Name = "All" });

            try
            {
                var result = await _categoryService.GetCategoriesAsync(null, 1, 1000);
                if (result.Success && result.Data != null)
                {
                    foreach (var c in result.Data.Items)
                    {
                        Categories.Add(new CategoryOption
                        {
                            Id = c.CategoryId,
                            Name = c.Name
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                OnPropertyChanged(nameof(HasError));
            }

            // Initialize default selections
            SelectedCategory ??= Categories.FirstOrDefault();
            NewProductCategory ??= Categories.FirstOrDefault(c => c.Id != null);
            EditCategory ??= Categories.FirstOrDefault(c => c.Id != null);
        }

        private async Task LoadCategoryListAsync()
        {
            CategoryItems.Clear();
            CategoryDialogError = string.Empty;

            try
            {
                var result = await _categoryService.GetCategoriesAsync(
                    string.IsNullOrWhiteSpace(CategorySearchText) ? null : CategorySearchText,
                    1,
                    1000);

                if (!result.Success || result.Data == null)
                {
                    CategoryDialogError = result.Message ?? "Cannot load categories.";
                    return;
                }

                foreach (var c in result.Data.Items)
                {
                    CategoryItems.Add(c);
                }
            }
            catch (Exception ex)
            {
                CategoryDialogError = ex.Message;
            }
            finally
            {
                OnPropertyChanged(nameof(HasCategoryDialogError));
            }
        }

        // ========== PRODUCT LOADING & PAGINATION ==========
        private async Task LoadPageAsync(int? page = null)
        {
            if (_isLoadingProducts) return;
            _isLoadingProducts = true;

            ErrorMessage = string.Empty;

            if (page.HasValue)
                CurrentPage = page.Value;

            var options = new ProductQueryOptions
            {
                Page = CurrentPage,
                PageSize = PageSize,
                Search = SearchText,
                CategoryId = SelectedCategory?.Id,
                MinPrice = int.TryParse(MinPriceText, out var min) ? min : null,
                MaxPrice = int.TryParse(MaxPriceText, out var max) ? max : null,
                SortField = ParseSortOption(SelectedSortOption).Field,
                SortAscending = ParseSortOption(SelectedSortOption).Ascending
            };

            try
            {
                var result = await _productService.GetProductsAsync(options);

                if (!result.Success || result.Data == null)
                {
                    ErrorMessage = result.Message ?? "Cannot load products.";
                    Products.Clear();
                    TotalItems = 0;
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
                PageSize = pageData.PageSize;
                TotalItems = pageData.TotalItems;
                TotalPages = Math.Max(1, pageData.TotalPages);
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Products.Clear();
                TotalItems = 0;
                TotalPages = 1;
            }
            finally
            {
                _isLoadingProducts = false;
                OnPropertyChanged(nameof(HasError));
            }
        }

        private Task ReloadCurrentPageAsync() => LoadPageAsync(CurrentPage);

        private (ProductSortField Field, bool Ascending) ParseSortOption(string option)
        {
            return option switch
            {
                "Name (A-Z)" => (ProductSortField.Name, true),
                "Name (Z-A)" => (ProductSortField.Name, false),
                "Price (Low → High)" => (ProductSortField.SalePrice, true),
                "Price (High → Low)" => (ProductSortField.SalePrice, false),
                _ => (ProductSortField.Name, true)
            };
        }

        // ========== FILTER & PAGINATION COMMANDS ==========
        [RelayCommand]
        private Task ApplyFilterAsync() => LoadPageAsync(1);

        [RelayCommand]
        private Task SearchAsync() => LoadPageAsync(1);

        [RelayCommand]
        private Task NextPageAsync()
        {
            if (CurrentPage < TotalPages)
                return LoadPageAsync(CurrentPage + 1);
            return Task.CompletedTask;
        }

        [RelayCommand]
        private Task PreviousPageAsync()
        {
            if (CurrentPage > 1)
                return LoadPageAsync(CurrentPage - 1);
            return Task.CompletedTask;
        }

        // ========== DELETE PRODUCT ==========
        [RelayCommand]
        private async Task DeleteProductAsync(ProductItemDto? product)
        {
            if (product == null || IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;
            bool deleted = false;

            try
            {
                var result = await _productService.DeleteProductAsync(product.ProductId);
                if (!result.Success)
                {
                    ErrorMessage = result.Message ?? "Delete failed.";
                    return;
                }

                deleted = true;
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

            if (deleted)
            {
                await ReloadCurrentPageAsync();
            }
        }

        // ========== ADD PRODUCT DIALOG ==========
        [RelayCommand]
        private void AddProduct()
        {
            if (IsBusy) return;

            AddDialogError = string.Empty;
            NewProductSku = string.Empty;
            NewProductName = string.Empty;
            NewProductImportPriceText = string.Empty;
            NewProductSalePriceText = string.Empty;
            NewProductStockQuantityText = string.Empty;
            NewProductDescription = string.Empty;
            NewProductImagePath = string.Empty;
            NewProductCategory = Categories.FirstOrDefault(c => c.Id != null);

            IsAddDialogOpen = true;
        }

        [RelayCommand]
        private void CancelAddDialog() => IsAddDialogOpen = false;

        [RelayCommand]
        private async Task ConfirmAddProductAsync()
        {
            if (IsBusy) return;

            AddDialogError = string.Empty;

            // Validate input
            if (!ValidationHelper.ValidateProductSku(NewProductSku, out var skuError))
            {
                AddDialogError = skuError;
                return;
            }

            if (!ValidationHelper.ValidateProductName(NewProductName, out var nameError))
            {
                AddDialogError = nameError;
                return;
            }

            if (!ValidationHelper.ValidatePrice(NewProductImportPriceText, out var importError, "Import price"))
            {
                AddDialogError = importError;
                return;
            }

            if (!ValidationHelper.ValidatePrice(NewProductSalePriceText, out var saleError, "Sale price"))
            {
                AddDialogError = saleError;
                return;
            }

            if (!ValidationHelper.ValidateStockQuantity(NewProductStockQuantityText, out var stockError))
            {
                AddDialogError = stockError;
                return;
            }

            if (!ValidationHelper.ValidateCategorySelection(NewProductCategory, out var catError))
            {
                AddDialogError = catError;
                return;
            }

            IsBusy = true;
            bool created = false;

            try
            {
                var input = new ProductCreateInput
                {
                    Sku = NewProductSku!,
                    Name = NewProductName!,
                    ImportPrice = int.Parse(NewProductImportPriceText!),
                    SalePrice = int.Parse(NewProductSalePriceText!),
                    StockQuantity = int.Parse(NewProductStockQuantityText!),
                    Description = NewProductDescription ?? string.Empty,
                    CategoryId = NewProductCategory!.Id!.Value,
                    ImagePaths = string.IsNullOrWhiteSpace(NewProductImagePath)
         ? new()
        : new() { NewProductImagePath! }
                };

                var result = await _productService.CreateProductAsync(input);
                if (!result.Success)
                {
                    AddDialogError = result.Message ?? "Create product failed.";
                    return;
                }

                created = true;
            }
            catch (Exception ex)
            {
                AddDialogError = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasAddDialogError));
            }

            if (created)
            {
                IsAddDialogOpen = false;
                await ReloadCurrentPageAsync();
            }
        }

        // ========== EDIT PRODUCT DIALOG ==========
        [RelayCommand]
        private Task EditProductAsync(ProductItemDto? product)
        {
            if (product == null || IsBusy) return Task.CompletedTask;

            EditDialogError = string.Empty;
            EditingProductId = product.ProductId;
            EditProductSku = product.Sku;
            EditProductName = product.Name;
            EditImportPriceText = product.ImportPrice?.ToString();
            EditSalePriceText = product.SalePrice.ToString();
            EditStockQuantityText = product.StockQuantity.ToString();
            EditDescription = product.Description;
            EditImagePath = product.ImagePaths?.FirstOrDefault();

            EditCategory = Categories.FirstOrDefault(c => c.Id == product.CategoryId)
        ?? Categories.FirstOrDefault(c => c.Id != null)
    ?? Categories.FirstOrDefault();

            IsEditDialogOpen = true;
            return Task.CompletedTask;
        }

        [RelayCommand]
        private void CancelEditDialog() => IsEditDialogOpen = false;

        [RelayCommand]
        private async Task ConfirmEditProductAsync()
        {
            if (IsBusy) return;

            EditDialogError = string.Empty;

            // Validate input
            if (!ValidationHelper.ValidateProductName(EditProductName, out var nameError))
            {
                EditDialogError = nameError;
                return;
            }

            if (!ValidationHelper.ValidatePrice(EditImportPriceText, out var importError, "Import price"))
            {
                EditDialogError = importError;
                return;
            }

            if (!ValidationHelper.ValidatePrice(EditSalePriceText, out var saleError, "Sale price"))
            {
                EditDialogError = saleError;
                return;
            }

            if (!ValidationHelper.ValidateStockQuantity(EditStockQuantityText, out var stockError))
            {
                EditDialogError = stockError;
                return;
            }

            if (!ValidationHelper.ValidateCategorySelection(EditCategory, out var catError))
            {
                EditDialogError = catError;
                return;
            }

            IsBusy = true;
            bool updated = false;

            try
            {
                var input = new ProductUpdateInput
                {
                    Name = EditProductName!,
                    ImportPrice = int.Parse(EditImportPriceText!),
                    SalePrice = int.Parse(EditSalePriceText!),
                    StockQuantity = int.Parse(EditStockQuantityText!),
                    Description = EditDescription,
                    CategoryId = EditCategory!.Id!.Value,
                    ImagePaths = string.IsNullOrWhiteSpace(EditImagePath)
         ? null
           : new() { EditImagePath! }
                };

                var result = await _productService.UpdateProductAsync(EditingProductId, input);
                if (!result.Success)
                {
                    EditDialogError = result.Message ?? "Update product failed.";
                    return;
                }

                updated = true;
            }
            catch (Exception ex)
            {
                EditDialogError = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasEditDialogError));
            }

            if (updated)
            {
                IsEditDialogOpen = false;
                await ReloadCurrentPageAsync();
            }
        }

        // ========== CATEGORY MANAGEMENT DIALOG ==========
        [RelayCommand]
        private async Task OpenCategoryDialogAsync()
        {
            CategoryDialogError = string.Empty;
            IsCategoryDialogOpen = true;
            await LoadCategoryListAsync();
        }

        [RelayCommand]
        private async Task CategorySearchAsync() => await LoadCategoryListAsync();

        [RelayCommand]
        private void NewCategory()
        {
            SelectedCategoryItem = null;
            CategoryNameText = string.Empty;
            CategoryDescriptionText = string.Empty;
            CategoryDialogError = string.Empty;
        }

        [RelayCommand]
        private async Task SaveCategoryAsync()
        {
            if (!ValidationHelper.IsRequired(CategoryNameText, out var error))
   {
                CategoryDialogError = error;
    return;
            }

            IsBusy = true;
    CategoryDialogError = string.Empty;

        try
         {
      ApiResult<CategoryItemDto> result;

          if (SelectedCategoryItem == null)
          {
       var input = new CategoryCreateInput
       {
        Name = CategoryNameText!,
             Description = CategoryDescriptionText
     };
    result = await _categoryService.CreateCategoryAsync(input);
       }
  else
                {
    var input = new CategoryUpdateInput
        {
             Name = CategoryNameText,
          Description = CategoryDescriptionText
         };
         result = await _categoryService.UpdateCategoryAsync(
  SelectedCategoryItem.CategoryId, input);
       }

         if (!result.Success)
      {
 CategoryDialogError = result.Message ?? "Save category failed.";
         return;
                }

   // Reload all related data
      await LoadCategoryListAsync();
        await LoadCategoriesAsync();
       await LoadPageAsync(CurrentPage);
            }
            catch (Exception ex)
         {
        CategoryDialogError = ex.Message;
   }
          finally
    {
    IsBusy = false;
        OnPropertyChanged(nameof(HasCategoryDialogError));
         }
        }

    [RelayCommand]
     private async Task DeleteCategoryAsync(CategoryItemDto? item)
        {
            var target = item ?? SelectedCategoryItem;
     if (target == null) return;

            IsBusy = true;
            CategoryDialogError = string.Empty;

try
    {
        var result = await _categoryService.DeleteCategoryAsync(target.CategoryId);
         if (!result.Success)
  {
         CategoryDialogError = result.Message ?? "Delete category failed.";
 return;
         }

  SelectedCategoryItem = null;

 // Reload all related data
                await LoadCategoryListAsync();
await LoadCategoriesAsync();
    await LoadPageAsync(CurrentPage);
            }
     catch (Exception ex)
            {
          CategoryDialogError = ex.Message;
            }
   finally
            {
          IsBusy = false;
     OnPropertyChanged(nameof(HasCategoryDialogError));
        }
    }

[RelayCommand]
 private void CloseCategoryDialog() => IsCategoryDialogOpen = false;

        // ========== IMPORT & MISC ==========
        [RelayCommand]
        private void ImportData()
        {
         System.Diagnostics.Debug.WriteLine("ImportData (mock) clicked");
        }

   [RelayCommand]
   private void ViewProduct(ProductItemDto? product)
        {
  if (product == null) return;
            System.Diagnostics.Debug.WriteLine($"ViewProduct (mock) {product.ProductId}");
        }

     public async Task ImportFromExcelAsync(Stream excelStream)
        {
      if (IsBusy) return;

          IsBusy = true;
         ErrorMessage = string.Empty;

            try
     {
       var result = await _productService.ImportProductsFromExcelAsync(excelStream);

         if (!result.Success)
   {
      ErrorMessage = result.Message ?? "Import failed.";
       }
     else
      {
       await LoadPageAsync(1);
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
    }
}
