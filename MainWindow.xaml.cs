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

            // Đợi window activate lần đầu rồi mới quyết định đi đâu
            this.Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs e)
        {
            if (_initialized) return;
            _initialized = true;

            var auth = App.Services.GetRequiredService<IAuthService>();
            var nav = App.Services.GetRequiredService<INavigationService>();
            var settings = App.Services.GetRequiredService<IAppSettingsService>();

            // thử auto login bằng credentials đã lưu
            var auto = await auth.TryAutoLoginAsync();

            if (auto)
            {
                // đảm bảo rằng chúng ta có giá trị last page mặc định
                settings.LastVisitedPage ??= nameof(DashboardHomePage);

                // vào shell; shell sẽ tự động khôi phục LastVisitedPage
                nav.NavigateToMainShell();
            }
            else
            {
                nav.NavigateToLogin();
            }
        }
    }
}
