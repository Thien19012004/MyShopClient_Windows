using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MyShopClient.Models;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System;
using System.Linq;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;
using MyShopClient.Services.Auth;
using MyShopClient.Services.Report;
using MyShopClient.Services.Navigation;

namespace MyShopClient.ViewModels
{
    public partial class DashboardViewModel : ObservableObject
    {
        private readonly IAuthService _auth;
        private readonly INavigationService _navigation;
        private readonly IReportService _reportService;

        [ObservableProperty]
        private string greetingText = "Welcome back";

        [ObservableProperty]
        private string userName = "User";

        [ObservableProperty] private bool isBusy;
        [ObservableProperty] private string? errorMessage;

        // Overview metrics
        [ObservableProperty] private int totalProducts;
        [ObservableProperty] private int totalOrdersToday;
        [ObservableProperty] private string revenueTodayText = "$0";

        // Low stock products
        public ObservableCollection<LowStockProductDto> LowStockProducts { get; } = new();

        // Top selling products
        public ObservableCollection<TopSellingProductDto> TopSellingProducts { get; } = new();

        // Recent orders
        public ObservableCollection<RecentOrderDto> RecentOrders { get; } = new();

        // Daily revenue chart
        [ObservableProperty] private ISeries[] dailyRevenueSeries = Array.Empty<ISeries>();
        [ObservableProperty] private Axis[] dailyRevenueXAxes = Array.Empty<Axis>();
        [ObservableProperty] private Axis[] dailyRevenueYAxes = Array.Empty<Axis>();

        public DashboardViewModel(IAuthService auth, INavigationService navigation, IReportService reportService)
        {
            _auth = auth;
            _navigation = navigation;
            _reportService = reportService;

            var current = _auth.CurrentUser;
            if (current != null)
            {
                UserName = string.IsNullOrWhiteSpace(current.FullName)
                        ? current.Username
             : current.FullName;
            }

            GreetingText = $"Welcome back, {UserName}";

            // Initialize chart axes
            DailyRevenueYAxes = new[] { new Axis { Name = "Revenue" } };
            DailyRevenueXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };

            _ = LoadDashboardDataAsync();
        }

        private async Task LoadDashboardDataAsync()
        {
            if (IsBusy) return;
            IsBusy = true;
            ErrorMessage = string.Empty;

            try
            {
                var now = DateTime.Now;
                var firstDayOfMonth = new DateTime(now.Year, now.Month, 1);
                var lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);

                // Load all data in parallel
                var overviewTask = _reportService.GetOverviewAsync();
                var lowStockTask = _reportService.GetLowStockProductsAsync(5, 5);
                var topSellingTask = _reportService.GetTopSellingProductsAsync(
               firstDayOfMonth.ToString("yyyy-MM-dd"),
                      lastDayOfMonth.ToString("yyyy-MM-dd"),
                       5);
                var recentOrdersTask = _reportService.GetRecentOrdersAsync(3);
                var dailyRevenueTask = _reportService.GetDailyRevenueInMonthAsync(now.Year, now.Month);

                await Task.WhenAll(overviewTask, lowStockTask, topSellingTask, recentOrdersTask, dailyRevenueTask);

                // Overview
                var overviewResult = await overviewTask;
                if (overviewResult.Success && overviewResult.Data != null)
                {
                    TotalProducts = overviewResult.Data.TotalProducts;
                    TotalOrdersToday = overviewResult.Data.TotalOrdersToday;
                    RevenueTodayText = FormatCurrency(overviewResult.Data.RevenueToday);
                }
                else
                {
                    SetError(overviewResult.Message ?? "Không thể tải dữ liệu tổng quan.");
                }

                // Low stock
                var lowStockResult = await lowStockTask;
                if (lowStockResult.Success && lowStockResult.Data != null)
                {
                    LowStockProducts.Clear();
                    foreach (var item in lowStockResult.Data)
                    {
                        LowStockProducts.Add(item);
                    }
                }
                else
                {
                    SetError(lowStockResult.Message ?? "Không thể tải danh sách tồn kho thấp.");
                }

                // Top selling
                var topSellingResult = await topSellingTask;
                if (topSellingResult.Success && topSellingResult.Data != null)
                {
                    TopSellingProducts.Clear();
                    foreach (var item in topSellingResult.Data)
                    {
                        TopSellingProducts.Add(item);
                    }
                }
                else
                {
                    SetError(topSellingResult.Message ?? "Không thể tải top sản phẩm bán chạy.");
                }

                // Recent orders
                var recentOrdersResult = await recentOrdersTask;
                if (recentOrdersResult.Success && recentOrdersResult.Data != null)
                {
                    RecentOrders.Clear();
                    foreach (var item in recentOrdersResult.Data)
                    {
                        RecentOrders.Add(item);
                    }
                }
                else
                {
                    SetError(recentOrdersResult.Message ?? "Không thể tải đơn hàng gần đây.");
                }

                // Daily revenue chart
                var dailyRevenueResult = await dailyRevenueTask;
                if (dailyRevenueResult.Success && dailyRevenueResult.Data != null)
                {
                    BuildDailyRevenueChart(dailyRevenueResult.Data);
                }
                else
                {
                    SetError(dailyRevenueResult.Message ?? "Không thể tải biểu đồ doanh thu.");
                }
            }
            catch (Exception ex)
            {
                ErrorMessage = ex.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void BuildDailyRevenueChart(System.Collections.Generic.List<DailyRevenueDto> data)
        {
            if (data == null || data.Count == 0)
            {
                DailyRevenueSeries = Array.Empty<ISeries>();
                DailyRevenueXAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                return;
            }

            var orderedData = data.OrderBy(d => d.Date).ToList();
            var labels = orderedData.Select(d =>
        {
            if (DateTime.TryParse(d.Date, out var dt))
                return dt.ToString("dd/MM");
            return d.Date;
        }).ToArray();

            var values = orderedData.Select(d => (double)d.Revenue).ToArray();

            DailyRevenueSeries = new ISeries[]
      {
      new LineSeries<double>
    {
          Name = "Revenue",
  Values = values,
         GeometrySize = 10,
         Stroke = new SolidColorPaint(SKColors.DeepSkyBlue) { StrokeThickness = 3 },
             GeometryFill = new SolidColorPaint(SKColors.DeepSkyBlue),
         GeometryStroke = new SolidColorPaint(SKColors.White) { StrokeThickness = 2 },
               Fill = new SolidColorPaint(SKColors.DeepSkyBlue.WithAlpha(30)),
    LineSmoothness = 0.5
   }
            };

            DailyRevenueXAxes = new[]
            {
                new Axis
           {
        Labels = labels,
       LabelsRotation = 45,
         TextSize = 11
        }
            };

            DailyRevenueYAxes = new[]
            {
         new Axis
            {
Name = "Revenue",
         TextSize = 11,
   MinLimit = 0,
      LabelsPaint = new SolidColorPaint(SKColors.Gray)
           }
       };
        }

        private string FormatCurrency(decimal amount)
        {
            return "$" + amount.ToString("N0");
        }

        private void SetError(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return;
            if (!string.IsNullOrWhiteSpace(ErrorMessage)) return;
            ErrorMessage = message;
        }

        [RelayCommand]
        private async Task RefreshAsync()
        {
            await LoadDashboardDataAsync();
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            await _auth.LogoutAsync();
            _navigation.NavigateToLogin();
        }

        [RelayCommand]
        private void OpenProducts()
        {
            _navigation.NavigateToProducts();
        }
    }
}
