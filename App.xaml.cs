using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShopClient.Services;
using MyShopClient.ViewModels;
using System;
using System.Net.Http;
using System.Diagnostics;
using System.Threading.Tasks;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using SkiaSharp;

namespace MyShopClient;

public partial class App : Application
{
    public static IServiceProvider Services { get; private set; } = null!;
  public static string CurrentVersion => "1.0.0";
    public static Window MainWindow { get; private set; }


    public App()
    {
      this.InitializeComponent();

        // Configure LiveCharts for WinUI
        LiveCharts.Configure(config =>
            config
                // Use SkiaSharp backend for rendering
                .AddSkiaSharp()
     // Add default mappers
       .AddDefaultMappers()
          // Add light theme
         .AddLightTheme()
        );

        // Global exception handlers to capture stack traces for debugging
   this.UnhandledException += App_UnhandledException;
        AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;
        TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;

        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();    

    // 1. Đăng ký ServerConfigService trước
        services.AddSingleton<IServerConfigService, ServerConfigService>();

        // 2. HttpClient với SocketsHttpHandler để quản lý connection pool tốt hơn
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

            var handler = new SocketsHttpHandler
            {
      PooledConnectionLifetime = TimeSpan.FromMinutes(5),
        PooledConnectionIdleTimeout = TimeSpan.FromMinutes(2),
         MaxConnectionsPerServer = 10,
           EnableMultipleHttp2Connections = true
 };

 var client = new HttpClient(handler, disposeHandler: false)
    {
     BaseAddress = new Uri(baseUrl, UriKind.Absolute),
    Timeout = TimeSpan.FromSeconds(100)
   };

    return client;
        });

    // 3. Các service khác
   services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IProductService, ProductService>();
services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<IReportService, ReportService>();

     // 4. ViewModels
        services.AddTransient<LoginViewModel>();
  services.AddTransient<ConfigViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductListViewModel>();
 services.AddTransient<OrderListViewModel>();
   services.AddTransient<ReportViewModel>();

        Services = services.BuildServiceProvider();
    }

    private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
 {
   try
     {
    Debug.WriteLine("[Global] UnobservedTaskException: " + e.Exception);
     }
        catch { }
    }

    private void CurrentDomain_UnhandledException(object? sender, System.UnhandledExceptionEventArgs e)
    {
        try
      {
Debug.WriteLine("[Global] CurrentDomain.UnhandledException: " + e.ExceptionObject?.ToString());
      }
    catch { }
  }

    private void App_UnhandledException(object? sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
    {
   try
     {
     Debug.WriteLine("[Global] App.UnhandledException: " + e.Exception.ToString());
    // prevent app from crashing so we can inspect the issue during debugging
    e.Handled = true;
        }
        catch { }
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        var window = new MainWindow();
      MainWindow = window;
      window.Activate();
  }
}
