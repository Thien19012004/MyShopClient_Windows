using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Services;
using System.Threading.Tasks;

namespace MyShopClient.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _navigation;

        [ObservableProperty]
        private string greetingText = "Welcome back";

        [ObservableProperty]
        private string userName = "User";

        // Demo metrics – sau này bạn thay bằng API gọi từ server
        [ObservableProperty] private int totalCustomers = 1456;
        [ObservableProperty] private decimal revenue = 3345m;
        [ObservableProperty] private int profitPercent = 60;
        [ObservableProperty] private int totalInvoices = 1135;

        public DashboardViewModel(IAuthService auth, INavigationService navigation)
        {
            _auth = auth;
            _navigation = navigation;

            var current = _auth.CurrentUser;
            if (current != null)
            {
                UserName = string.IsNullOrWhiteSpace(current.FullName)
                    ? current.Username
                    : current.FullName;
            }

            GreetingText = $"Welcome back, {UserName}";
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _auth.LogoutAsync();
            _navigation.NavigateToLogin();
        }

        [RelayCommand]
        private void OpenProducts()
        {
            _navigation.NavigateToProducts();
        }

    }
}
