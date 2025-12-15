using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;

namespace MyShopClient.Views
{
    public sealed partial class DashboardHomePage : Page
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardHomePage()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<DashboardViewModel>();
            DataContext = ViewModel;
        }
    }
}
