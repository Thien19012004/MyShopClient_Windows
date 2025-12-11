using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace MyShopClient.Views
{
    public sealed partial class LoginPage : Page
    {
        public LoginViewModel ViewModel => (LoginViewModel)DataContext;

        public LoginPage()
        {
            this.InitializeComponent();
            DataContext = App.Services.GetService<LoginViewModel>();
        }

        private void PasswordBox_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (ViewModel != null)
            {
                ViewModel.Password = ((PasswordBox)sender).Password;
            }
        }
    }
}
