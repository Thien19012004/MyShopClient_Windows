using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;

namespace MyShopClient.Views
{
    public sealed partial class DashboardHomePage : Page
    {
        public DashboardViewModel ViewModel => (DashboardViewModel)DataContext;

        public DashboardHomePage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetService<DashboardViewModel>();
        }
    }
}
