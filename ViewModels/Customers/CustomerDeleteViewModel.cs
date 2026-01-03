using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Customer;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    // Delete and bulk-delete logic
    public partial class CustomerListViewModel
    {
        private string _deleteConfirmMessage = string.Empty;
        public string DeleteConfirmMessage
        {
            get => _deleteConfirmMessage;
            private set => SetProperty(ref _deleteConfirmMessage, value);
        }

        [RelayCommand]
        private void OpenDeleteConfirm(CustomerListItemDto? customer)
        {
            if (customer == null) return;
            CustomerToDelete = customer;
            DeleteConfirmMessage = $"Are you sure you want to delete Customer #{customer.CustomerId}?";
            IsDeleteConfirmOpen = true;
        }

        [RelayCommand]
        private void CancelDeleteConfirm()
        {
            IsDeleteConfirmOpen = false;
            CustomerToDelete = null;
            DeleteConfirmMessage = string.Empty;
        }

        [RelayCommand]
        private async Task ConfirmDeleteCustomerAsync()
        {
            if (CustomerToDelete == null || IsBusy) return;

            IsBusy = true;
            ErrorMessage = string.Empty;
            ErrorMessage = string.Empty;
            bool deleted = false;

            try
            {
                var result = await _customerService.DeleteCustomerAsync(CustomerToDelete.CustomerId);
                if (!result.Success)
                {
                    ErrorMessage = result.Message ?? "Delete failed.";
                    return;
                }

                deleted = true;
            }
            catch (System.Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
                OnPropertyChanged(nameof(HasError));
            }

            IsDeleteConfirmOpen = false;
            CustomerToDelete = null;
            DeleteConfirmMessage = string.Empty;

            if (deleted)
            {
                await LoadPageAsync(CurrentPage);
            }
        }

        // Bulk-delete commands now delegate to base ConfirmBulkDeleteAsync via SelectedItems
        protected override async Task<bool> DeleteItemsAsync(CustomerListItemDto[] items)
        {
            // items contains the selected items
            var ids = items.Select(i => i.CustomerId).ToArray();
            var result = await _customerService.BulkDeleteCustomersAsync(ids);
            return result.Success;
        }
    }
}
