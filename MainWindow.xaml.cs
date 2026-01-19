using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.Services.AppSettings;
using MyShopClient.Services.Auth;
using MyShopClient.Services.Navigation;
using MyShopClient.Views;

namespace MyShopClient
{
    public sealed partial class MainWindow : Window
    {
        public static Frame? RootFrameInstance { get; private set; }

        private bool _initialized = false;

        public MainWindow()
        {
            this.InitializeComponent();
            RootFrameInstance = RootFrame;

            
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            var auth = App.Services.GetRequiredService<IAuthService>();
            var nav = App.Services.GetRequiredService<INavigationService>();
            var settings = App.Services.GetRequiredService<IAppSettingsService>();

            var auto = await auth.TryAutoLoginAsync();

            if (auto)
            {

                settings.LastVisitedPage ??= nameof(DashboardHomePage);


                nav.NavigateToMainShell();
            }
            else
            {
                nav.NavigateToLogin();
            }
        }
    }
}
