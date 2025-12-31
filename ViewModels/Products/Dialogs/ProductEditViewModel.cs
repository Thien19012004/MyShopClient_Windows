using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.ImageUpload;
using MyShopClient.Services.Product;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Products.Dialogs
{
    public partial class ProductEditViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IImageUploadService _imageUploadService;
        private readonly Func<Task> _reloadCallback;

        public ObservableCollection<ProductImageItem> EditProductImages { get; } = new();

        public ProductEditViewModel(IProductService productService, IImageUploadService imageUploadService, Func<Task> reloadCallback)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _imageUploadService = imageUploadService ?? throw new ArgumentNullException(nameof(imageUploadService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
        }

        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        private string? _error;
        public string? Error { get => _error; set { SetProperty(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        public int EditingProductId { get; private set; }
        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? ImportPriceText { get; set; }
        public string? SalePriceText { get; set; }
        public string? StockQuantityText { get; set; }
        public string? Description { get; set; }
        public CategoryOption? Category { get; set; }
        public string? ImagePath { get; set; }

        public async Task DoOpenAsync(ProductItemDto product, CategoryOption? defaultCategory, string baseUrl)
        {
            Error = string.Empty;
            IsOpen = false;
            EditProductImages.Clear();

            try
            {
                var detailResult = await _productService.GetProductByIdAsync(product.ProductId);
                if (!detailResult.Success || detailResult.Data == null)
                {
                    Error = detailResult.Message ?? "Cannot load product detail.";
                    OnPropertyChanged(nameof(HasError));
                    return;
                }
                var detail = detailResult.Data;

                EditingProductId = detail.ProductId;
                Sku = detail.Sku;
                Name = detail.Name;
                ImportPriceText = detail.ImportPrice.ToString();
                SalePriceText = detail.SalePrice.ToString();
                StockQuantityText = detail.StockQuantity.ToString();
                Description = detail.Description;

                Category = defaultCategory ?? new CategoryOption { Id = detail.CategoryId, Name = detail.CategoryName ?? string.Empty };

                if (detail.ImagePaths != null)
                {
                    foreach (var imagePath in detail.ImagePaths)
                    {
                        var absoluteUrl = Helpers.UrlHelper.ToAbsoluteUrl(imagePath, baseUrl);
                        EditProductImages.Add(new ProductImageItem { Url = absoluteUrl, PublicId = string.Empty });
                    }
                }

                IsOpen = true;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                OnPropertyChanged(nameof(HasError));
            }
        }

        public void DoCancel()
        {
            IsOpen = false;
            EditProductImages.Clear();
            Error = string.Empty;
            OnPropertyChanged(nameof(HasError));
        }

        public async Task<bool> DoConfirmAsync(CategoryOption? selectedCategory)
        {
            Error = string.Empty;
            OnPropertyChanged(nameof(HasError));

            if (string.IsNullOrWhiteSpace(Name))
            {
                Error = "Name is required.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (!int.TryParse(ImportPriceText, out var importPrice) || importPrice < 0)
            {
                Error = "Import price must be a non-negative integer.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (!int.TryParse(SalePriceText, out var salePrice) || salePrice < 0)
            {
                Error = "Sale price must be a non-negative integer.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (!int.TryParse(StockQuantityText, out var stock) || stock < 0)
            {
                Error = "Stock quantity must be a non-negative integer.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (selectedCategory == null || selectedCategory.Id == null)
            {
                Error = "Please select category.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            try
            {
                var imagePaths = EditProductImages.Where(img => !string.IsNullOrWhiteSpace(img.Url) && img.Url != "Uploading...").Select(img => img.Url).ToList();

                var input = new ProductUpdateInput
                {
                    Name = Name!,
                    ImportPrice = importPrice,
                    SalePrice = salePrice,
                    StockQuantity = stock,
                    Description = Description,
                    CategoryId = selectedCategory.Id.Value,
                    ImagePaths = imagePaths.Any() ? imagePaths : null
                };

                var result = await _productService.UpdateProductAsync(EditingProductId, input);
                if (!result.Success)
                {
                    Error = result.Message ?? "Update product failed.";
                    OnPropertyChanged(nameof(HasError));
                    return false;
                }

                IsOpen = false;
                EditProductImages.Clear();
                OnPropertyChanged(nameof(HasError));
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

        // Commands for XAML
        [RelayCommand]
        private void Open(ProductItemDto product)
        {
            _ = DoOpenAsync(product, null, string.Empty);
        }

        [RelayCommand]
        private void Cancel()
        {
            DoCancel();
        }

        [RelayCommand]
        private async Task Confirm()
        {
            await DoConfirmAsync(Category);
        }
    }
}
