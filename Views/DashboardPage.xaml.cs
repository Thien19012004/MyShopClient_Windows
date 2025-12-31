using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.Services.AppSettings;
using MyShopClient.Services.Auth;
using MyShopClient.Services.Navigation;
using MyShopClient.Services.OnBoarding;
using System.Threading.Tasks;

namespace MyShopClient.Views
{
    public sealed partial class DashboardPage : Page
    {
        private readonly IAuthService _authService;
        private readonly INavigationService _navigation;
        private readonly IAppSettingsService _appSettings;
        private readonly IOnboardingService _onboarding;

        public DashboardPage()
        {
            this.InitializeComponent();

            _authService = App.Services.GetRequiredService<IAuthService>();
            _navigation = App.Services.GetRequiredService<INavigationService>();
            _appSettings = App.Services.GetRequiredService<IAppSettingsService>();
            _onboarding = App.Services.GetRequiredService<IOnboardingService>();

            DataContext = App.Services.GetRequiredService<ViewModels.DashboardViewModel>();

            Loaded += DashboardPage_Loaded;
        }

        private async void DashboardPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (ContentFrame.Content == null)
            {
                var last = _appSettings.LastVisitedPage;

                // Restore last visited page inside shell
                switch (last)
                {
                    case nameof(ProductPage):
                        Sidebar.SetActiveMenu(SidebarMenu.Products);
                        ContentFrame.Navigate(typeof(ProductPage));
                        break;
                    case nameof(OrderPage):
                        Sidebar.SetActiveMenu(SidebarMenu.Orders);
                        ContentFrame.Navigate(typeof(OrderPage));
                        break;
                    case nameof(CustomerPage):
                        Sidebar.SetActiveMenu(SidebarMenu.Customers);
                        ContentFrame.Navigate(typeof(CustomerPage));
                        break;
                    case nameof(PromotionPage):
                        Sidebar.SetActiveMenu(SidebarMenu.Promotions);
                        ContentFrame.Navigate(typeof(PromotionPage));
                        break;
                    case nameof(ReportPage):
                        Sidebar.SetActiveMenu(SidebarMenu.Reports);
                        ContentFrame.Navigate(typeof(ReportPage));
                        break;
                    case nameof(SettingsPage):
                        Sidebar.SetActiveMenu(SidebarMenu.Settings);
                        ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                    default:
                        Sidebar.SetActiveMenu(SidebarMenu.Dashboard);
                        ContentFrame.Navigate(typeof(DashboardHomePage));
                        break;
                }
            }

            // On first launch, show onboarding welcome (dialog) and optionally run tour.
            await _onboarding.TryRunIfFirstLaunchAsync(this, ContentFrame, Sidebar, OnboardingOverlay);
        }

        public Task StartOnboardingTourAsync() => _onboarding.StartTourAsync(this, ContentFrame, Sidebar, OnboardingOverlay);

        private void Sidebar_DashboardRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Dashboard);
            _appSettings.LastVisitedPage = nameof(DashboardHomePage);

            if (ContentFrame.CurrentSourcePageType != typeof(DashboardHomePage))
                ContentFrame.Navigate(typeof(DashboardHomePage));
        }

        private void Sidebar_ProductsRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Products);
            _appSettings.LastVisitedPage = nameof(ProductPage);

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
            _appSettings.LastVisitedPage = nameof(OrderPage);

            if (ContentFrame.CurrentSourcePageType != typeof(OrderPage))
                ContentFrame.Navigate(typeof(OrderPage));
        }

        private void Sidebar_CustomersRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Customers);
            _appSettings.LastVisitedPage = nameof(CustomerPage);

            if (ContentFrame.CurrentSourcePageType != typeof(CustomerPage))
                ContentFrame.Navigate(typeof(CustomerPage));
        }

        private void Sidebar_PromotionsRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Promotions);
            _appSettings.LastVisitedPage = nameof(PromotionPage);

            if (ContentFrame.CurrentSourcePageType != typeof(PromotionPage))
                ContentFrame.Navigate(typeof(PromotionPage));
        }

        private void Sidebar_ReportsRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Reports);
            _appSettings.LastVisitedPage = nameof(ReportPage);

            if (ContentFrame.CurrentSourcePageType != typeof(ReportPage))
                ContentFrame.Navigate(typeof(ReportPage));
        }

        private void Sidebar_SettingsRequested(object sender, System.EventArgs e)
        {
            Sidebar.SetActiveMenu(SidebarMenu.Settings);
            _appSettings.LastVisitedPage = nameof(SettingsPage);

            if (ContentFrame.CurrentSourcePageType != typeof(SettingsPage))
                ContentFrame.Navigate(typeof(SettingsPage));
        }
    }
}
