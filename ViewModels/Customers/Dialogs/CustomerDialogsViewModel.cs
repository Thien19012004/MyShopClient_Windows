using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Customer;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    // Dialog related logic now delegated to dedicated viewmodels
    public partial class CustomerListViewModel
    {
        private CustomerAddViewModel? _addVm;
        private CustomerEditViewModel? _editVm;

        public CustomerAddViewModel AddVm => _addVm ??= new CustomerAddViewModel(_customerService, async () => await LoadPageAsync(CurrentPage));
        public CustomerEditViewModel EditVm => _editVm ??= new CustomerEditViewModel(_customerService, async () => await LoadPageAsync(CurrentPage));

        [RelayCommand]
        private void OpenAddDialog()
        {
            AddVm.DoOpen();
        }

        [RelayCommand]
        private async Task OpenEditDialogAsync(CustomerListItemDto? customer)
        {
            if (customer == null) return;
            EditCustomerOrderCount = customer.OrderCount;
            await EditVm.DoOpenAsync(customer);
        }
    }
}
