using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services.Product;
using System;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels.Products
{
    public partial class ProductDeleteViewModel : ObservableObject
    {
        private readonly IProductService _productService;
        private readonly Func<Task> _reloadCallback;

        public ProductDeleteViewModel(IProductService productService, Func<Task> reloadCallback)
        {
            _productService = productService ?? throw new ArgumentNullException(nameof(productService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
        }

        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        private string? _error;
        public string? Error { get => _error; set { SetProperty(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        private int _targetId;
        public int TargetId { get => _targetId; set => SetProperty(ref _targetId, value); }

        public void Open(int productId)
        {
            TargetId = productId;
            Error = string.Empty;
            IsOpen = true;
        }

        public void Cancel()
        {
            IsOpen = false;
            Error = string.Empty;
        }

        public async Task<bool> ConfirmAsync()
        {
            Error = string.Empty;
            OnPropertyChanged(nameof(HasError));
            try
            {
                var res = await _productService.DeleteProductAsync(TargetId);
                if (!res.Success)
                {
                    Error = res.Message ?? "Delete failed.";
                    OnPropertyChanged(nameof(HasError));
                    return false;
                }
                await _reloadCallback();
                IsOpen = false;
                return true;
            }
            catch (Exception ex)
            {
                Error = ex.Message;
                OnPropertyChanged(nameof(HasError));
                return false;
            }
        }

        [RelayCommand]
        private void OpenCommand(int id) => Open(id);

        [RelayCommand]
        private void CancelCommand() => Cancel();

        [RelayCommand]
        private async Task ConfirmCommand() => await ConfirmAsync();
    }
}
