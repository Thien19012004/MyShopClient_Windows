using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;

namespace MyShopClient.Views
{
    public sealed partial class OrderPage : Page
    {
        public OrderListViewModel ViewModel => (OrderListViewModel)DataContext;

        public OrderPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetService<OrderListViewModel>();
        }
    }
}
