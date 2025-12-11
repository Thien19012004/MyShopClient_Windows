using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;

namespace MyShopClient.Views
{
    public sealed partial class ConfigPage : Page
    {
        public ConfigViewModel ViewModel => (ConfigViewModel)DataContext;

        public ConfigPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetService<ConfigViewModel>();
        }
    }
}
