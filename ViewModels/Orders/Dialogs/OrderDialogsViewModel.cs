using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Order;
using MyShopClient.Services.Promotion;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public partial class OrderListViewModel
    {
        private ViewModels.Orders.OrderAddViewModel? _addVm;
        private ViewModels.Orders.OrderEditViewModel? _editVm;

        public ViewModels.Orders.OrderAddViewModel AddVm => _addVm ??= new ViewModels.Orders.OrderAddViewModel(_orderService, _promotionService, _customerService, _productService, async () => await LoadPageAsync(CurrentPage));
        public ViewModels.Orders.OrderEditViewModel EditVm => _editVm ??= new ViewModels.Orders.OrderEditViewModel(_orderService, _promotionService, async () => await LoadPageAsync(CurrentPage));

        [RelayCommand]
        private void OpenAddDialog()
        {
            AddVm.DoOpen();
        }

        [RelayCommand]
        private async Task OpenEditDialogAsync(OrderListItemDto? order)
        {
            if (order == null) return;
            var res = await _orderService.GetOrderByIdAsync(order.OrderId);
            if (!res.Success || res.Data == null)
            {
                ErrorMessage = res.Message ?? "Cannot load order detail.";
                OnPropertyChanged(nameof(HasError));
                return;
            }

            await EditVm.DoOpenAsync(order, res.Data);
        }
    }
}
