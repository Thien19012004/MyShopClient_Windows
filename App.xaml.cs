using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShopClient.Services;
using MyShopClient.ViewModels;
using System;
using System.Net.Http;

namespace MyShopClient;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
    public static string CurrentVersion => "1.0.0";
    public static Window MainWindow { get; private set; }


    public App()
    {
        this.InitializeComponent();

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();    

        // 1. Đăng ký ServerConfigService trước, để dùng ở HttpClient
        services.AddSingleton<IServerConfigService, ServerConfigService>();

        // 2. HttpClient với BaseAddress lấy từ ServerConfig.Current.BaseUrl
        services.AddSingleton<HttpClient>(sp =>
        {
            var cfgService = sp.GetRequiredService<IServerConfigService>();
            var baseUrl = cfgService.Current.BaseUrl;

            // fallback nếu file config trống
            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = "http://localhost:5135";

            // đảm bảo có dấu '/'
            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            return new HttpClient
            {
                BaseAddress = new Uri(baseUrl, UriKind.Absolute)
            };
        });

        // 3. Các service khác
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IOrderService, OrderService>();



        // 4. ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ConfigViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<OrderListViewModel>();


        Services = services.BuildServiceProvider();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
        MainWindow = window;
        window.Activate();
    }
}
