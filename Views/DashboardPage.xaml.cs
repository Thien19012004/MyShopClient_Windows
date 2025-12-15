using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.Services;

namespace MyShopClient.Views
{
    public sealed partial class DashboardPage : Page
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;

        public DashboardPage()
        {
            this.InitializeComponent();

            _authService = App.Services.GetRequiredService<IAuthService>();
            _navigation = App.Services.GetRequiredService<INavigationService>();

            DataContext = App.Services.GetRequiredService<ViewModels.DashboardViewModel>();

            Loaded += DashboardPage_Loaded;
        }

        private void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content == null)
            {
                Sidebar.SetActiveMenu(SidebarMenu.Dashboard);
                ContentFrame.Navigate(typeof(DashboardHomePage));
            }
        }

        private void Sidebar_DashboardRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Dashboard);

            if (ContentFrame.CurrentSourcePageType != typeof(DashboardHomePage))
                ContentFrame.Navigate(typeof(DashboardHomePage));
        }

        private void Sidebar_ProductsRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Products);

            if (ContentFrame.CurrentSourcePageType != typeof(ProductPage))
                ContentFrame.Navigate(typeof(ProductPage));
        }

        private async void Sidebar_LogoutRequested(object sender, System.EventArgs e)
        {
            await _authService.LogoutAsync();
            _navigation.NavigateToLogin();
        }

        private void Sidebar_OrdersRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Orders);
            if (ContentFrame.CurrentSourcePageType != typeof(OrderPage))
                ContentFrame.Navigate(typeof(OrderPage));
        }
    }
}
