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
using MyShopClient.Services.Product;
using MyShopClient.Infrastructure.GraphQL;
using MyShopClient.Services.Customer;
using MyShopClient.Services.ImageUpload;
using MyShopClient.Services.Auth;
using MyShopClient.Services.PdfExport;
using MyShopClient.Services.Promotion;
using MyShopClient.Services.Report;
using MyShopClient.Services.Order;
using MyShopClient.Services.Category;
using MyShopClient.Services.Navigation;
using MyShopClient.Services.AppSettings;
using MyShopClient.Services.OnBoarding;
using MyShopClient.Services.SecureStorage;
using MyShopClient.Services.Kpi;
using MyShopClient.ViewModels.Kpi;

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

        // App settings (registry): page size, last visited page
        services.AddSingleton<IAppSettingsService, AppSettingsService>();

        // --- Infrastructure handlers ---
        services.AddTransient<MyShopClient.Infrastructure.Http.RetryHandler>();
        services.AddTransient<MyShopClient.Infrastructure.Http.LoggingHandler>();
        // Register BearerAuthHandler that reads token from IAuthService.CurrentUser?.Token at runtime
        services.AddTransient<MyShopClient.Infrastructure.Auth.BearerAuthHandler>(sp =>
            new MyShopClient.Infrastructure.Auth.BearerAuthHandler(
                () => Task.FromResult(sp.GetRequiredService<IAuthService>().CurrentUser?.Token),
                () => sp.GetRequiredService<IAuthService>().RefreshTokenAsync()));


        // 2. Configure named HttpClient "Api" via IHttpClientFactory and expose a singleton HttpClient
        services.AddHttpClient("Api", (sp, client) =>
        {
            var cfgService = sp.GetRequiredService<IServerConfigService>();
            var baseUrl = cfgService.Current.BaseUrl;

            if (string.IsNullOrWhiteSpace(baseUrl))
                baseUrl = "http://localhost:5135";

            if (!baseUrl.EndsWith("/"))
                baseUrl += "/";

            client.BaseAddress = new Uri(baseUrl, UriKind.Absolute);
            client.Timeout = TimeSpan.FromMinutes(5);

            client.DefaultRequestHeaders.ConnectionClose = false;
            client.DefaultRequestHeaders.Add("Keep-Alive", "timeout=600, max=1000");
        })
        .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
        {
            PooledConnectionLifetime = TimeSpan.FromMinutes(10),
            PooledConnectionIdleTimeout = TimeSpan.FromMinutes(5),
            MaxConnectionsPerServer = 20,
            EnableMultipleHttp2Connections = true,

            KeepAlivePingDelay = TimeSpan.FromSeconds(30),
            KeepAlivePingTimeout = TimeSpan.FromSeconds(15),
            KeepAlivePingPolicy = System.Net.Http.HttpKeepAlivePingPolicy.Always,

            ResponseDrainTimeout = TimeSpan.FromSeconds(10),
            ConnectTimeout = TimeSpan.FromSeconds(30),
            AutomaticDecompression = System.Net.DecompressionMethods.All
        })
        .AddHttpMessageHandler(sp => sp.GetRequiredService<MyShopClient.Infrastructure.Http.RetryHandler>())
        .AddHttpMessageHandler(sp => sp.GetRequiredService<MyShopClient.Infrastructure.Http.LoggingHandler>())
        // Add BearerAuthHandler so Authorization header is attached to outgoing requests
        .AddHttpMessageHandler(sp => sp.GetRequiredService<MyShopClient.Infrastructure.Auth.BearerAuthHandler>());

        // Expose a singleton HttpClient instance created from the factory so existing services that depend on
        // HttpClient (constructor-injected) continue to share the same instance and behavior (preserve logic).
        services.AddSingleton<HttpClient>(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("Api"));

        // Register GraphQL client abstraction (uses the named "Api" client internally)
        services.AddSingleton<IGraphQLClient, GraphQLClient>();

        // 3. Các service khác
        services.AddSingleton<ISecureStorageService, SecureStorageService>();
        services.AddSingleton<IAuthService, AuthService>();
        services.AddSingleton<INavigationService, NavigationService>();
        services.AddSingleton<IProductService, ProductService>();
        services.AddSingleton<ICategoryService, CategoryService>();
        services.AddSingleton<IOrderService, OrderService>();
        services.AddSingleton<ICustomerService, CustomerService>();
        services.AddSingleton<IReportService, ReportService>();
        services.AddSingleton<IImageUploadService, ImageUploadService>();
        services.AddSingleton<IPromotionService, PromotionService>();
        services.AddSingleton<IPdfExportService, PdfExportService>();
        services.AddSingleton<IKpiService, KpiService>();

        // 4. ViewModels
        services.AddTransient<LoginViewModel>();
        services.AddTransient<ConfigViewModel>();
        services.AddTransient<DashboardViewModel>();
        services.AddTransient<ProductListViewModel>();
        services.AddTransient<OrderListViewModel>();
        services.AddTransient<CustomerListViewModel>();
        services.AddTransient<ReportViewModel>();
        services.AddTransient<PromotionListViewModel>();
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<KpiViewModel>();

        // Register OnboardingService so Dashboard can run onboarding tour on first launch and from Settings if needed.
        services.AddSingleton<IOnboardingService, OnboardingService>();

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
