using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using MyShopClient.Services.Customer;
using System;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public partial class CustomerAddViewModel : ObservableObject
    {
        private readonly ICustomerService _customerService;
        private readonly Func<Task> _reloadCallback;

        public CustomerAddViewModel(ICustomerService customerService, Func<Task> reloadCallback)
        {
            _customerService = customerService ?? throw new ArgumentNullException(nameof(customerService));
            _reloadCallback = reloadCallback ?? throw new ArgumentNullException(nameof(reloadCallback));
        }

        private bool _isOpen;
        public bool IsOpen { get => _isOpen; set => SetProperty(ref _isOpen, value); }

        private string? _error;
        public string? Error { get => _error; set { SetProperty(ref _error, value); OnPropertyChanged(nameof(HasError)); } }
        public bool HasError => !string.IsNullOrWhiteSpace(Error);

        private string? _name;
        public string? Name { get => _name; set => SetProperty(ref _name, value); }

        private string? _phone;
        public string? Phone { get => _phone; set => SetProperty(ref _phone, value); }

        private string? _email;
        public string? Email { get => _email; set => SetProperty(ref _email, value); }

        private string? _address;
        public string? Address { get => _address; set => SetProperty(ref _address, value); }

        // public methods renamed
        public void DoOpen()
        {
            Error = string.Empty;
            Name = string.Empty;
            Phone = string.Empty;
            Email = string.Empty;
            Address = string.Empty;
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

            if (string.IsNullOrWhiteSpace(Name))
            {
                Error = "Name is required.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            if (string.IsNullOrWhiteSpace(Phone))
            {
                Error = "Phone is required.";
                OnPropertyChanged(nameof(HasError));
                return false;
            }

            try
            {
                var input = new CustomerCreateInput
                {
                    Name = Name!,
                    Phone = Phone!,
                    Email = Email ?? string.Empty,
                    Address = Address ?? string.Empty
                };

                var result = await _customerService.CreateCustomerAsync(input);
                if (!result.Success)
                {
                    Error = result.Message ?? "Create customer failed.";
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

        // Commands for XAML binding
        [RelayCommand]
        private void Open()
        {
            DoOpen();
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
