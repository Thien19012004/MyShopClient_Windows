using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MyShopClient.Views
{
    public sealed partial class ReportPage : Page
    {
        public ReportViewModel ViewModel { get; }

        public ReportPage()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<ReportViewModel>();
            DataContext = ViewModel;
        }
    }
}
