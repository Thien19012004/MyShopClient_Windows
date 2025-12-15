using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public class CategoryOption
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public partial class ProductListViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;

        // cờ chỉ dùng nội bộ cho việc load product list
        private bool _isLoadingProducts;

        public ObservableCollection<ProductItemDto> Products { get; } = new();
        public ObservableCollection<CategoryOption> Categories { get; } = new();
        public ObservableCollection<string> SortOptions { get; } =
            new(new[] { "Name (A-Z)", "Name (Z-A)", "Price (Low → High)", "Price (High → Low)" });

        // list category cho dialog Manage Category
        public ObservableCollection<CategoryItemDto> CategoryItems { get; } = new();

        // ====== bộ lọc / phân trang sản phẩm ======
        [ObservableProperty] private CategoryOption? selectedCategory;
        [ObservableProperty] private string? searchText;
        [ObservableProperty] private string? minPriceText;
        [ObservableProperty] private string? maxPriceText;
        [ObservableProperty] private string selectedSortOption = "Name (A-Z)";

        [ObservableProperty] private int currentPage = 1;
        [ObservableProperty] private int pageSize = 10;
        [ObservableProperty] private int totalPages = 1;
        [ObservableProperty] private int totalItems = 0;

        [ObservableProperty] private bool isBusy; // cho Add/Delete/Update/Category
        [ObservableProperty] private string? errorMessage;
        public bool HasError => !string.IsNullOrWhiteSpace(ErrorMessage);

        // ====== Add Product dialog ======
        [ObservableProperty] private bool isAddDialogOpen;

        [ObservableProperty] private string? addDialogError;
        public bool HasAddDialogError => !string.IsNullOrWhiteSpace(AddDialogError);
        partial void OnAddDialogErrorChanged(string? value) => OnPropertyChanged(nameof(HasAddDialogError));

        [ObservableProperty] private string? newProductSku;
        [ObservableProperty] private string? newProductName;
        [ObservableProperty] private string? newProductImportPriceText;
        [ObservableProperty] private string? newProductSalePriceText;
        [ObservableProperty] private string? newProductStockQuantityText;
        [ObservableProperty] private string? newProductDescription;
        [ObservableProperty] private string? newProductImagePath;
        [ObservableProperty] private CategoryOption? newProductCategory;

        // ====== Edit Product dialog ======
        [ObservableProperty] private bool isEditDialogOpen;

        [ObservableProperty] private string? editDialogError;
        public bool HasEditDialogError => !string.IsNullOrWhiteSpace(EditDialogError);
        partial void OnEditDialogErrorChanged(string? value) => OnPropertyChanged(nameof(HasEditDialogError));

        [ObservableProperty] private int editingProductId;
        [ObservableProperty] private string? editProductSku;
        [ObservableProperty] private string? editProductName;
        [ObservableProperty] private string? editImportPriceText;
        [ObservableProperty] private string? editSalePriceText;
        [ObservableProperty] private string? editStockQuantityText;
        [ObservableProperty] private string? editDescription;
        [ObservableProperty] private string? editImagePath;
        [ObservableProperty] private CategoryOption? editCategory;

        // ====== Manage Category dialog ======
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

        private async Task InitializeAsync()
        {
            await LoadCategoriesAsync();
            await LoadPageAsync();
        }

        // ========== LOAD CATEGORY cho combobox / Add/Edit ==========
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

            SelectedCategory ??= Categories.FirstOrDefault();
            NewProductCategory ??= Categories.FirstOrDefault(c => c.Id != null);
            EditCategory ??= Categories.FirstOrDefault(c => c.Id != null);
        }

        // ========== LOAD CATEGORY list cho dialog Manage ==========
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

        // ================= CORE LOAD PRODUCTS =================

        private async Task LoadPageAsync(int? page = null)
        {
            if (_isLoadingProducts) return;
            _isLoadingProducts = true;

            ErrorMessage = string.Empty;

            if (page.HasValue)
                CurrentPage = page.Value;

            int? minPrice = int.TryParse(MinPriceText, out var min) ? min : null;
            int? maxPrice = int.TryParse(MaxPriceText, out var max) ? max : null;

            var (field, asc) = ParseSortOption(SelectedSortOption);

            var options = new ProductQueryOptions
            {
                Page = CurrentPage,
                PageSize = PageSize,
                Search = SearchText,
                CategoryId = SelectedCategory?.Id,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortField = field,
                SortAscending = asc
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

        private (ProductSortField field, bool asc) ParseSortOption(string option)
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

        // ================= COMMANDS FILTER / PAGING =================

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

        // ================= DELETE PRODUCT =================

        [RelayCommand]
        private async Task DeleteProductAsync(ProductItemDto? product)
        {
            if (product == null) return;
            if (IsBusy) return;

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

        // ================= ADD PRODUCT DIALOG =================

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
        private void CancelAddDialog()
        {
            IsAddDialogOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmAddProductAsync()
        {
            if (IsBusy) return;

            AddDialogError = string.Empty;

            if (string.IsNullOrWhiteSpace(NewProductSku) ||
                string.IsNullOrWhiteSpace(NewProductName))
            {
                AddDialogError = "SKU và Name là bắt buộc.";
                return;
            }

            if (!int.TryParse(NewProductImportPriceText, out var importPrice) || importPrice < 0)
            {
                AddDialogError = "Import price phải là số nguyên không âm.";
                return;
            }

            if (!int.TryParse(NewProductSalePriceText, out var salePrice) || salePrice < 0)
            {
                AddDialogError = "Sale price phải là số nguyên không âm.";
                return;
            }

            if (!int.TryParse(NewProductStockQuantityText, out var stock) || stock < 0)
            {
                AddDialogError = "Stock quantity phải là số nguyên không âm.";
                return;
            }

            if (NewProductCategory == null || NewProductCategory.Id == null)
            {
                AddDialogError = "Vui lòng chọn Category.";
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
                    ImportPrice = importPrice,
                    SalePrice = salePrice,
                    StockQuantity = stock,
                    Description = NewProductDescription ?? string.Empty,
                    CategoryId = NewProductCategory.Id.Value,
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

        // ================= EDIT PRODUCT DIALOG =================

        [RelayCommand]
        private Task EditProductAsync(ProductItemDto? product)
        {
            if (product == null) return Task.CompletedTask;
            if (IsBusy) return Task.CompletedTask;

            EditDialogError = string.Empty;

            EditingProductId = product.ProductId;
            EditProductSku = product.Sku;
            EditProductName = product.Name;
            EditImportPriceText = product.ImportPrice.ToString();
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
        private void CancelEditDialog()
        {
            IsEditDialogOpen = false;
        }

        [RelayCommand]
        private async Task ConfirmEditProductAsync()
        {
            if (IsBusy) return;

            EditDialogError = string.Empty;

            if (string.IsNullOrWhiteSpace(EditProductName))
            {
                EditDialogError = "Name is required.";
                return;
            }

            if (!int.TryParse(EditImportPriceText, out var importPrice) || importPrice < 0)
            {
                EditDialogError = "Import price must be a non-negative integer.";
                return;
            }

            if (!int.TryParse(EditSalePriceText, out var salePrice) || salePrice < 0)
            {
                EditDialogError = "Sale price must be a non-negative integer.";
                return;
            }

            if (!int.TryParse(EditStockQuantityText, out var stock) || stock < 0)
            {
                EditDialogError = "Stock quantity must be a non-negative integer.";
                return;
            }

            if (EditCategory == null || EditCategory.Id == null)
            {
                EditDialogError = "Please select category.";
                return;
            }

            IsBusy = true;
            bool updated = false;

            try
            {
                var input = new ProductUpdateInput
                {
                    Name = EditProductName!,
                    ImportPrice = importPrice,
                    SalePrice = salePrice,
                    StockQuantity = stock,
                    Description = EditDescription,
                    CategoryId = EditCategory.Id.Value,
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

        // ================= MANAGE CATEGORY DIALOG =================

        [RelayCommand]
        private async Task OpenCategoryDialogAsync()
        {
            CategoryDialogError = string.Empty;
            IsCategoryDialogOpen = true;
            await LoadCategoryListAsync();
        }

        [RelayCommand]
        private async Task CategorySearchAsync()
        {
            await LoadCategoryListAsync();
        }

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
            if (string.IsNullOrWhiteSpace(CategoryNameText))
            {
                CategoryDialogError = "Name is required.";
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
                        Name = CategoryNameText!,
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

                // reload cả list trong dialog và combo Category chung
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
        private void CloseCategoryDialog()
        {
            IsCategoryDialogOpen = false;
        }

        // ================= MOCK KHÁC =================

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
                    // Sau khi import xong → reload lại product list
                    await LoadPageAsync(1);   // về page 1 cho dễ thấy
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
