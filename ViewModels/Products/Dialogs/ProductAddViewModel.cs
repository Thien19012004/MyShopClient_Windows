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
    public partial class ProductAddViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly IImageUploadService _imageUploadService;
        private readonly Func<Task> _reloadCallback;

        public ObservableCollection<ProductImageItem> NewProductImages { get; } = new();

        public ProductAddViewModel(IProductService productService, IImageUploadService imageUploadService, Func<Task> reloadCallback)
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

        public string? Sku { get; set; }
        public string? Name { get; set; }
        public string? ImportPriceText { get; set; }
        public string? SalePriceText { get; set; }
        public string? StockQuantityText { get; set; }
        public string? Description { get; set; }
        public CategoryOption? Category { get; set; }
        public string? ImagePath { get; set; }

        public void DoOpen(ObservableCollection<ProductImageItem> initialImages, CategoryOption? defaultCategory)
        {
            Error = string.Empty;
            Sku = string.Empty;
            Name = string.Empty;
            ImportPriceText = string.Empty;
            SalePriceText = string.Empty;
            StockQuantityText = string.Empty;
            Description = string.Empty;
            ImagePath = string.Empty;
            NewProductImages.Clear();
            if (initialImages != null)
            {
                foreach (var it in initialImages) NewProductImages.Add(it);
            }
            Category = defaultCategory;
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

            if (string.IsNullOrWhiteSpace(Sku) || string.IsNullOrWhiteSpace(Name))
            {
                Error = "SKU và Name là bắt buộc.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (!int.TryParse(ImportPriceText, out var importPrice) || importPrice < 0)
            {
                Error = "Import price phải là số nguyên không âm.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (!int.TryParse(SalePriceText, out var salePrice) || salePrice < 0)
            {
                Error = "Sale price phải là số nguyên không âm.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (!int.TryParse(StockQuantityText, out var stock) || stock < 0)
            {
                Error = "Stock quantity phải là số nguyên không âm.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (Category == null || Category.Id == null)
            {
                Error = "Vui lòng chọn Category.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            try
            {
                var imagePaths = NewProductImages.Where(img => !string.IsNullOrWhiteSpace(img.Url) && img.Url != "Uploading...").Select(img => img.Url).ToList();

                var input = new ProductCreateInput
                {
                    Sku = Sku!,
                    Name = Name!,
                    ImportPrice = importPrice,
                    SalePrice = salePrice,
                    StockQuantity = stock,
                    Description = Description ?? string.Empty,
                    CategoryId = Category.Id.Value,
                    ImagePaths = imagePaths
                };

                var result = await _productService.CreateProductAsync(input);
                if (!result.Success)
                {
                    Error = result.Message ?? "Create product failed.";
                    OnPropertyChanged(nameof(HasError));
                    return false;
                }

                IsOpen = false;
                Error = string.Empty;
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
        private void Open()
        {
            DoOpen(new ObservableCollection<ProductImageItem>(), null);
        }

        [RelayCommand]
        private void Cancel()
        {
            DoCancel();
        }

        [RelayCommand]
        private async Task Confirm()
        {
            await DoConfirmAsync();
        }
    }
}
