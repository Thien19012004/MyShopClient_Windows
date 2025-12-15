using Microsoft.UI.Xaml.Controls;
using MyShopClient.Views;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MyShopClient.Services
{
    public interface INavigationService
    {
        void NavigateToLogin();
        void NavigateToMainShell();
        void NavigateToConfig();
        void NavigateToProducts();
        void NavigateToOrders();
    }

    public class NavigationService : INavigationService
    {
        private Frame Frame => MainWindow.RootFrameInstance!;

        public void NavigateToLogin()
        {
            Frame.Navigate(typeof(LoginPage));
        }

        public void NavigateToMainShell()
        {
            Frame.Navigate(typeof(DashboardPage)); 
        }

        public void NavigateToConfig()
        {
            Frame.Navigate(typeof(ConfigPage)); // sau này bạn tạo thêm
        }

        public void NavigateToProducts()
        {
            Frame.Navigate(typeof(ProductPage));
        }

        public void NavigateToOrders()         {
            Frame.Navigate(typeof(OrderPage));
        }
    }
}
