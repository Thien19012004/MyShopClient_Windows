using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services;
using MyShopClient.Services.AppSettings;
using MyShopClient.Services.Category;
using MyShopClient.Services.ImageUpload;
using MyShopClient.Services.Product;
using MyShopClient.ViewModels.Products;
using MyShopClient.ViewModels.Products.Dialogs;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public class CategoryOption
    {
        public int? Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    // Use common paging + selection base classes
    public partial class ProductListViewModel : SelectableListViewModel<ProductItemDto>
    {
        private readonly IProductService _productService;
        private readonly ICategoryService _categoryService;
        private readonly IImageUploadService _imageUploadService;
        private readonly IServerConfigService _serverConfig;
        private readonly IAppSettingsService _appSettings;

        private int _searchVersion;
        private CancellationTokenSource? _loadCts;

        // Dialogs container
        public ProductDialogsViewModel Dialogs { get; }

        // cờ chỉ dùng nội bộ cho việc load product list
        private bool _isLoadingProducts;

        public ObservableCollection<ProductItemDto> Products { get; } = new();
        public ObservableCollection<CategoryOption> Categories { get; } = new();
        // Categories used specifically for Add/Edit dialogs (exclude "All")
        public ObservableCollection<CategoryOption> DialogCategories { get; } = new();
        public ObservableCollection<string> SortOptions { get; } =
            new(new[] { "Name (A-Z)", "Name (Z-A)", "Price (Low → High)", "Price (High → Low)" });

        // list category cho dialog Manage Categor
        public ObservableCollection<CategoryItemDto> CategoryItems { get; } = new();

        // danh sách ảnh cho Add Product dialog
        public ObservableCollection<ProductImageItem> NewProductImages => Dialogs.AddVm.NewProductImages;

        // danh sách ảnh cho Edit Product dialog
        public ObservableCollection<ProductImageItem> EditProductImages => Dialogs.EditVm.EditProductImages;

        // ====== bộ lọc / phân trang sản phẩm ======
        [ObservableProperty] private CategoryOption? selectedCategory;
        [ObservableProperty] private string? searchText;
        [ObservableProperty] private string? minPriceText;
        [ObservableProperty] private string? maxPriceText;
        [ObservableProperty] private string selectedSortOption = "Name (A-Z)";

        // search-as-you-type debounce handler (awaited + single-flight on UI context)
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

                // if a newer change happened, skip this run
                if (version != _searchVersion) return;

                await LoadPageAsync(1);
            }
            catch (Exception ex)
            {
                // surface unexpected errors instead of crashing
                ErrorMessage = ex.Message;
                OnPropertyChanged(nameof(HasError));
            }
        }

        // ====== Add Product dialog ======
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

        public ProductListViewModel(
            IProductService productService,
            ICategoryService categoryService,
            IImageUploadService imageUploadService,
            IServerConfigService serverConfig,
            IAppSettingsService appsettings)
            : base(appsettings, s => s.ProductsPageSize)
        {
            _productService = productService;
            _categoryService = categoryService;
            _imageUploadService = imageUploadService;
            _serverConfig = serverConfig;
            _appSettings = appsettings;

            // create dialogs and delete viewmodels with reload callback
            Func<Task> reloadCallback = async () => await ReloadCurrentPageAsync();
            var addVm = new ProductAddViewModel(_productService, _imageUploadService, reloadCallback);
            var editVm = new ProductEditViewModel(_productService, _imageUploadService, reloadCallback);
            var deleteVm = new ProductDeleteViewModel(_productService, reloadCallback);
            Dialogs = new ProductDialogsViewModel(addVm, editVm, deleteVm);

            // attach selection tracker for Products collection
            AttachSelectionTracker(Products);
            SelectedItems.CollectionChanged += (s, e) => UpdateSelectionState();

            _ = InitializeAsync();
        }

        [ObservableProperty]
        private bool hasSelectedProducts;

        [ObservableProperty]
        private int selectedProductsCount;

        [ObservableProperty]
        private string bulkDeleteConfirmMessage = string.Empty;

        private void UpdateSelectionState()
        {
            SelectedProductsCount = SelectedItems.Count;
            HasSelectedProducts = SelectedItems.Count > 0;
            if (SelectedItems.Count > 0)
            {
                var ids = string.Join(", ", SelectedItems.OfType<ProductItemDto>().Select(p => $"#{p.ProductId}"));
                BulkDeleteConfirmMessage = $"Are you sure you want to delete {SelectedItems.Count} product(s)?\n\nProducts: {ids}";
            }
            else
            {
                BulkDeleteConfirmMessage = string.Empty;
            }
        }

        // Helper method để convert relative URL thành absolute URL
        private string ToAbsoluteUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                url.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (url.StartsWith("/"))
            {
                var baseUrl = _serverConfig.Current.BaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl))
                    baseUrl = "http://localhost:5135";

                baseUrl = baseUrl.TrimEnd('/');

                return baseUrl + url;
            }

            return url;
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
            DialogCategories.Clear();

            try
            {
                var result = await _categoryService.GetCategoriesAsync(null, 1, 1000);
                if (result.Success && result.Data != null)
                {
                    foreach (var c in result.Data.Items)
                    {
                        var opt = new CategoryOption
                        {
                            Id = c.CategoryId,
                            Name = c.Name
                        };
                        Categories.Add(opt);
                        // also add to dialog-only list (exclude the "All" option)
                        DialogCategories.Add(opt);
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
            NewProductCategory ??= DialogCategories.FirstOrDefault();
            EditCategory ??= DialogCategories.FirstOrDefault();
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

        // ========== paging core ==========
        protected override async Task LoadPageCoreAsync(int page, int pageSize)
        {
            ErrorMessage = string.Empty;

            // cancel previous load if any
            _loadCts?.Cancel();
            _loadCts = new CancellationTokenSource();
            var token = _loadCts.Token;

            int? minPrice = int.TryParse(MinPriceText, out var min) ? min : null;
            int? maxPrice = int.TryParse(MaxPriceText, out var max) ? max : null;

            var (field, asc) = ParseSortOption(SelectedSortOption);

            var options = new ProductQueryOptions
            {
                Page = page,
                PageSize = pageSize,
                Search = SearchText,
                CategoryId = SelectedCategory?.Id,
                MinPrice = minPrice,
                MaxPrice = maxPrice,
                SortField = field,
                SortAscending = asc
            };

            try
            {
                var result = await _productService.GetProductsAsync(options, token);

                if (!result.Success || result.Data == null)
                {
                    ErrorMessage = result.Message ?? "Cannot load products.";
                    Products.Clear();
                    SetPageResult(1, pageSize, 0, 1);
                    return;
                }

                var pageData = result.Data;

                Products.Clear();
                SelectedItems.Clear();
                foreach (var p in pageData.Items)
                {
                    Products.Add(p);
                }

                SetPageResult(pageData.Page, pageData.PageSize, pageData.TotalItems, Math.Max(1, pageData.TotalPages));

                _ = LoadProductImagesAsync(token);
            }
            catch (OperationCanceledException)
            {
                // ignore cancelled load
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
                Products.Clear();
                SetPageResult(1, pageSize, 0, 1);
            }
            finally
            {
                OnPropertyChanged(nameof(HasError));
            }
        }

        // Lazy load images cho tất cả products trong list
        private async Task LoadProductImagesAsync(CancellationToken token)
        {
            var baseUrl = _serverConfig.Current.BaseUrl;
            var snapshot = Products.ToList();

            var tasks = snapshot
                .Where(p => !p.ImagesLoaded)
                .Select(async product =>
                {
                    try
                    {
                        var detail = await _productService.GetProductByIdAsync(product.ProductId, token);
                        if (detail.Success && detail.Data != null && detail.Data.ImagePaths != null)
                        {
                            var absoluteUrls = detail.Data.ImagePaths
                                .Select(url => Helpers.UrlHelper.ToAbsoluteUrl(url, baseUrl))
                                .ToList();

                            product.ImagePaths = absoluteUrls;
                            product.ImagesLoaded = true;
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore
                    }
                    catch
                    {
                        // Silent fail - ảnh sẽ dùng placeholder
                    }
                });

            await Task.WhenAll(tasks);
        }

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

        [RelayCommand]
        private Task ApplyFilterAsync() => LoadPageAsync(1);

        [RelayCommand]
        private Task SearchAsync() => LoadPageAsync(1);

        // ================= DELETE PRODUCT (single) =================

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

        // ------ Single delete confirmation (overlay) ------
        private bool _isDeleteConfirmOpen;
        public bool IsDeleteConfirmOpen { get => _isDeleteConfirmOpen; set => SetProperty(ref _isDeleteConfirmOpen, value); }

        private ProductItemDto? _productToDelete;
        public ProductItemDto? ProductToDelete { get => _productToDelete; set => SetProperty(ref _productToDelete, value); }

        private string _deleteConfirmMessage = string.Empty;
        public string DeleteConfirmMessage { get => _deleteConfirmMessage; set => SetProperty(ref _deleteConfirmMessage, value); }

        [RelayCommand]
        private void OpenDeleteConfirm(ProductItemDto? product)
        {
            if (product == null) return;
            ProductToDelete = product;
            DeleteConfirmMessage = $"Are you sure you want to delete Product #{product.ProductId}?";
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteConfirm()
        {
            IsDeleteConfirmOpen = false;
            ProductToDelete = null;
        }

        [RelayCommand]
        private async Task ConfirmDeleteProductAsync()
        {
            IsDeleteConfirmOpen = false;
            if (ProductToDelete == null) return;
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var result = await _productService.DeleteProductAsync(ProductToDelete.ProductId);
                if (!result.Success)
                {
                    ErrorMessage = result.Message ?? "Delete product failed.";
                }
                else
                {
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
                ProductToDelete = null;
            }
        }

        // ================= IMAGE MANAGEMENT (ADD DIALOG) =================

        [RelayCommand]
        private void AddImageUrlToNewProduct()
        {
            if (string.IsNullOrWhiteSpace(NewProductImagePath))
            {
                AddDialogError = "Please enter image URL.";
                return;
            }

            NewProductImages.Add(new ProductImageItem
            {
                Url = NewProductImagePath,
                PublicId = string.Empty
            });

            NewProductImagePath = string.Empty;
            AddDialogError = string.Empty;
        }

        // TEST: Thêm ảnh test để verify binding
        [RelayCommand]
        private void AddTestImage()
        {
            System.Diagnostics.Debug.WriteLine("[Test] Adding test image");
            NewProductImages.Add(new ProductImageItem
            {
                Url = "https://via.placeholder.com/150",
                PublicId = "test-placeholder"
            });
            System.Diagnostics.Debug.WriteLine($"[Test] NewProductImages count: {NewProductImages.Count}");
        }

        [RelayCommand]
        private async Task RemoveImageFromNewProductAsync(ProductImageItem? image)
        {
            if (image == null) return;

            if (!string.IsNullOrWhiteSpace(image.PublicId))
            {
                image.IsDeleting = true;
                try
                {
                    await _imageUploadService.DeleteImageAsync(image.PublicId);
                }
                catch
                {
                }
                finally
                {
                    image.IsDeleting = false;
                }
            }

            NewProductImages.Remove(image);
        }

        // ================= IMAGE MANAGEMENT (EDIT DIALOG) =================

        [RelayCommand]
        private void AddImageUrlToEditProduct()
        {
            if (string.IsNullOrWhiteSpace(EditImagePath))
            {
                EditDialogError = "Please enter image URL.";
                return;
            }

            EditProductImages.Add(new ProductImageItem
            {
                Url = EditImagePath,
                PublicId = string.Empty
            });

            EditImagePath = string.Empty;
            EditDialogError = string.Empty;
        }

        [RelayCommand]
        private async Task RemoveImageFromEditProductAsync(ProductImageItem? image)
        {
            if (image == null) return;

            if (!string.IsNullOrWhiteSpace(image.PublicId))
            {
                image.IsDeleting = true;
                try
                {
                    await _imageUploadService.DeleteImageAsync(image.PublicId);
                }
                catch
                {
                }
                finally
                {
                    image.IsDeleting = false;
                }
            }

            EditProductImages.Remove(image);
        }

        // ================= UPLOAD IMAGE FROM FILE =================

        public async Task UploadImageForNewProductAsync(Stream imageStream, string fileName)
        {
            var imageItem = new ProductImageItem
            {
                Url = "Uploading...",
                PublicId = string.Empty,
                IsUploading = true
            };

            NewProductImages.Add(imageItem);

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Upload] Starting upload for {fileName}");

                var result = await _imageUploadService.UploadImageAsync(imageStream, fileName);

                System.Diagnostics.Debug.WriteLine($"[Upload] Result - Success: {result.Success}, Message: {result.Message}");

                if (result.Success && result.Data != null)
                {
                    imageItem.Url = result.Data.Url;
                    imageItem.PublicId = result.Data.PublicId;
                    System.Diagnostics.Debug.WriteLine($"[Upload] Image uploaded successfully. URL: {result.Data.Url}, PublicId: {result.Data.PublicId}");
                }
                else
                {
                    imageItem.ErrorMessage = result.Message ?? "Upload failed";
                    imageItem.Url = string.Empty;
                    System.Diagnostics.Debug.WriteLine($"[Upload] Upload failed: {imageItem.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                imageItem.ErrorMessage = ex.Message;
                imageItem.Url = string.Empty;
                System.Diagnostics.Debug.WriteLine($"[Upload] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Upload] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                imageItem.IsUploading = false;
            }
        }

        public async Task UploadImageForEditProductAsync(Stream imageStream, string fileName)
        {
            var imageItem = new ProductImageItem
            {
                Url = "Uploading...",
                PublicId = string.Empty,
                IsUploading = true
            };

            EditProductImages.Add(imageItem);

            try
            {
                System.Diagnostics.Debug.WriteLine($"[Upload] Starting upload for {fileName}");

                var result = await _imageUploadService.UploadImageAsync(imageStream, fileName);

                System.Diagnostics.Debug.WriteLine($"[Upload] Result - Success: {result.Success}, Message: {result.Message}");

                if (result.Success && result.Data != null)
                {
                    imageItem.Url = result.Data.Url;
                    imageItem.PublicId = result.Data.PublicId;
                    System.Diagnostics.Debug.WriteLine($"[Upload] Image uploaded successfully. URL: {result.Data.Url}, PublicId: {result.Data.PublicId}");
                }
                else
                {
                    imageItem.ErrorMessage = result.Message ?? "Upload failed";
                    imageItem.Url = string.Empty;
                    System.Diagnostics.Debug.WriteLine($"[Upload] Upload failed: {imageItem.ErrorMessage}");
                }
            }
            catch (Exception ex)
            {
                imageItem.ErrorMessage = ex.Message;
                imageItem.Url = string.Empty;
                System.Diagnostics.Debug.WriteLine($"[Upload] Exception: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"[Upload] Stack trace: {ex.StackTrace}");
            }
            finally
            {
                imageItem.IsUploading = false;
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
            NewProductImages.Clear();

            NewProductCategory = Categories.FirstOrDefault(c => c.Id != null);

            Dialogs.AddVm.DoOpen(NewProductImages, NewProductCategory);
        }

        [RelayCommand]
        private void CancelAddDialog()
        {
            Dialogs.AddVm.DoCancel();
        }

        [RelayCommand]
        private async Task ConfirmAddProductAsync()
        {
            await Dialogs.AddVm.DoConfirmAsync();
        }
        // ================= EDIT PRODUCT DIALOG =================

        [RelayCommand]
        private async Task EditProductAsync(ProductItemDto? product)
        {
            if (product == null) return;
            if (IsBusy) return;

            EditDialogError = string.Empty;
            IsBusy = true;

            try
            {
                await Dialogs.EditVm.DoOpenAsync(product, EditCategory, _serverConfig.Current.BaseUrl);
            }
            catch (Exception ex)
            {
                EditDialogError = ex.Message;
                System.Diagnostics.Debug.WriteLine($"[EditProduct] Exception: {ex.Message}");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void CancelEditDialog()
        {
            Dialogs.EditVm.DoCancel();
        }

        [RelayCommand]
        private async Task ConfirmEditProductAsync()
        {
            await Dialogs.EditVm.DoConfirmAsync(EditCategory);
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

        // ===== Bulk delete via common base =====
        protected override async Task<bool> DeleteItemsAsync(ProductItemDto[] items)
        {
            if (items == null || items.Length == 0) return false;
            var attempted = items.Length;
            var success = 0;
            var failedIds = new System.Collections.Generic.List<int>();

            foreach (var p in items)
            {
                try
                {
                    var res = await _productService.DeleteProductAsync(p.ProductId);
                    if (res.Success)
                    {
                        success++;
                    }
                    else
                    {
                        failedIds.Add(p.ProductId);
                    }
                }
                catch
                {
                    failedIds.Add(p.ProductId);
                }
            }

            if (failedIds.Count > 0)
            {
                ErrorMessage = failedIds.Count == attempted
                    ? $"Failed to delete any of the selected {attempted} product(s)."
                    : $"Deleted {success} product(s). Failed to delete {failedIds.Count} product(s). Failed IDs: {string.Join(",", failedIds)}";
            }

            OnPropertyChanged(nameof(HasError));
            return success > 0;
        }
    }
}
