using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services.AppSettings;
using System;

namespace MyShopClient.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly IAppSettingsService _settings;

        private static readonly int[] AllowedPageSizes = new[] { 5, 10, 20, 50, 100 };

        public int[] PageSizeOptions => AllowedPageSizes;

        [ObservableProperty] private int productsPageSize;
        [ObservableProperty] private int ordersPageSize;
        [ObservableProperty] private int customersPageSize;
        [ObservableProperty] private int promotionsPageSize;
        [ObservableProperty] private int reportsPageSize;

        public SettingsViewModel(IAppSettingsService settings)
        {
            _settings = settings;

            ProductsPageSize = _settings.ProductsPageSize;
            OrdersPageSize = _settings.OrdersPageSize;
            CustomersPageSize = _settings.CustomersPageSize;
            PromotionsPageSize = _settings.PromotionsPageSize;
            ReportsPageSize = _settings.ReportsPageSize;
        }

        private static int NormalizePageSize(int value)
        {
            return Array.IndexOf(AllowedPageSizes, value) >= 0 ? value : 10;
        }

        partial void OnProductsPageSizeChanged(int value) => _settings.ProductsPageSize = NormalizePageSize(value);
        partial void OnOrdersPageSizeChanged(int value) => _settings.OrdersPageSize = NormalizePageSize(value);
        partial void OnCustomersPageSizeChanged(int value) => _settings.CustomersPageSize = NormalizePageSize(value);
        partial void OnPromotionsPageSizeChanged(int value) => _settings.PromotionsPageSize = NormalizePageSize(value);
        partial void OnReportsPageSizeChanged(int value) => _settings.ReportsPageSize = NormalizePageSize(value);

        [RelayCommand]
        private void ResetDefaults()
        {
            ProductsPageSize = 10;
            OrdersPageSize = 10;
            CustomersPageSize = 10;
            PromotionsPageSize = 10;
            ReportsPageSize = 10;

        }
    }
}

