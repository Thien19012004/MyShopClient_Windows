using Microsoft.Win32;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.Controls;
using MyShopClient.Views;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MyShopClient.Services.OnBoarding
{
  
    internal record TourStep(
    string Title,
    string Body,
    Func<DashboardPage, Frame, SidebarControl, Task> EnsurePageAsync,
    Func<DashboardPage, Frame, SidebarControl, FrameworkElement?> ResolveTarget);

    public class OnboardingService : IOnboardingService
    {
        private const string RegPath = @"Software\\MyShopClient";
        private const string CompletedKey = "OnboardingCompleted";

        public bool IsCompleted => ReadBool(CompletedKey, false);

        public async Task TryRunIfFirstLaunchAsync(DashboardPage dashboardPage, Frame contentFrame, SidebarControl sidebar, OnboardingOverlay overlay)
        {
            if (IsCompleted) return;
            await ShowWelcomeAsync(dashboardPage, contentFrame, sidebar, overlay);
        }

        public Task StartTourAsync(DashboardPage dashboardPage, Frame contentFrame, SidebarControl sidebar, OnboardingOverlay overlay)
        => RunAsync(dashboardPage, contentFrame, sidebar, overlay);

        public void MarkCompleted() => WriteBool(CompletedKey, true);

        private async Task ShowWelcomeAsync(DashboardPage dashboardPage, Frame contentFrame, SidebarControl sidebar, OnboardingOverlay overlay)
        {
            var dlg = new ContentDialog
            {
                XamlRoot = dashboardPage.XamlRoot,
                Title = "Chào mừng bạn đến MyShop",
                Content = new TextBlock
                {
                    Text = "Bạn muốn xem hướng dẫn nhanh các chức năng chính không?",
                    TextWrapping = TextWrapping.Wrap
                },
                PrimaryButtonText = "Start tour",
                CloseButtonText = "Skip"
            };

            var res = await dlg.ShowAsync();
            if (res == ContentDialogResult.Primary)
            {
                await RunAsync(dashboardPage, contentFrame, sidebar, overlay);
            }
            else
            {
                MarkCompleted();
            }
        }

        private async Task RunAsync(DashboardPage dashboardPage, Frame contentFrame, SidebarControl sidebar, OnboardingOverlay overlay)
        {
            var steps = BuildSteps();
            int index =0;

            var tcs = new TaskCompletionSource();

            async Task ShowCurrentAsync()
            {
                await steps[index].EnsurePageAsync(dashboardPage, contentFrame, sidebar);
                await WaitForLoadedAsync(contentFrame);

                FrameworkElement? target = null;
                for (int i =0; i <15 && target == null; i++)
                {
                    target = steps[index].ResolveTarget(dashboardPage, contentFrame, sidebar);
                    if (target == null) await Task.Delay(80);
                }

                overlay.Show(
                steps[index].Title,
                steps[index].Body,
                showBack: index >0,
                showSkip: true,
                nextText: index == steps.Count -1 ? "Finish" : "Next");

                if (target != null)
                {
                    // Ensure layout is up to date before positioning
                    await dashboardPage.DispatcherQueue.EnqueueAsync(() => { });
                    overlay.PositionTo(target);
                }
            }

            void Cleanup()
            {
                overlay.NextRequested -= OnNext;
                overlay.BackRequested -= OnBack;
                overlay.SkipRequested -= OnSkip;
                overlay.Hide();
            }

            async void OnNext(object? s, EventArgs e)
            {
                if (index >= steps.Count -1)
                {
                    Cleanup();
                    MarkCompleted();
                    sidebar.SetActiveMenu(SidebarMenu.Dashboard);
                    contentFrame.Navigate(typeof(DashboardHomePage));
                    tcs.TrySetResult();
                    return;
                }

                index++;
                await ShowCurrentAsync();
            }

            async void OnBack(object? s, EventArgs e)
            {
                if (index <=0) return;
                index--;
                await ShowCurrentAsync();
            }

            void OnSkip(object? s, EventArgs e)
            {
                Cleanup();
                MarkCompleted();
                tcs.TrySetResult();
            }

            overlay.NextRequested += OnNext;
            overlay.BackRequested += OnBack;
            overlay.SkipRequested += OnSkip;

            await ShowCurrentAsync();
            await tcs.Task;
        }

        private static List<TourStep> BuildSteps()
        {
            static FrameworkElement? FindTarget(Frame frame, string name)
            {
                if (frame.Content is not FrameworkElement root) return null;
                return root.FindName(name) as FrameworkElement;
            }

            return new List<TourStep>
 {
 new(
 "Sidebar Navigation",
 "Dùng menu trái để chuyển giữa Dashboard, Products, Orders…",
 EnsureDashboardAsync,
 (dash, frame, sidebar) => sidebar),

 new(
 "Products",
 "Thêm sản phẩm mới bằng nút Add.\nBạn cũng có thể tìm kiếm/lọc theo tên, SKU, category.",
 EnsureProductsAsync,
 (dash, frame, sidebar) => FindTarget(frame, "AddProductButton") ?? FindTarget(frame, "SearchTextBox")),

 new(
 "Customers",
 "Quản lý khách hàng, click để xem chi tiết & lịch sử mua.",
 EnsureCustomersAsync,
 (dash, frame, sidebar) => FindTarget(frame, "AddCustomerButton")),

 new(
 "Orders",
 "Tạo đơn hàng bằng nút Add Order.\nDate filter giúp lọc theo khoảng ngày.",
 EnsureOrdersAsync,
 (dash, frame, sidebar) => FindTarget(frame, "AddOrderButton") ?? FindTarget(frame, "OrderDateRangePanel")),

 new(
 "Promotions",
 "Tạo promotion/voucher theo scope.",
 EnsurePromotionsAsync,
 (dash, frame, sidebar) => FindTarget(frame, "AddPromotionButton")),

 new(
 "Reports",
 "Chọn khoảng thời gian & kiểu nhóm (ngày/tuần/tháng…).",
 EnsureReportsAsync,
 (dash, frame, sidebar) => FindTarget(frame, "ReportGroupByPanel") ?? FindTarget(frame, "ReportDateRangePanel")),

 new(
 "Settings",
 "Đổi các tuỳ chọn hệ thống.",
 EnsureSettingsAsync,
 (dash, frame, sidebar) => FindTarget(frame, "SettingsHeader")),
 };
        }

        private static Task EnsureDashboardAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Dashboard);
            if (frame.CurrentSourcePageType != typeof(DashboardHomePage))
                frame.Navigate(typeof(DashboardHomePage));
            return Task.CompletedTask;
        }

        private static Task EnsureProductsAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Products);
            if (frame.CurrentSourcePageType != typeof(ProductPage))
                frame.Navigate(typeof(ProductPage));
            return Task.CompletedTask;
        }

        private static Task EnsureCustomersAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Customers);
            if (frame.CurrentSourcePageType != typeof(CustomerPage))
                frame.Navigate(typeof(CustomerPage));
            return Task.CompletedTask;
        }

        private static Task EnsureOrdersAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Orders);
            if (frame.CurrentSourcePageType != typeof(OrderPage))
                frame.Navigate(typeof(OrderPage));
            return Task.CompletedTask;
        }

        private static Task EnsurePromotionsAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Promotions);
            if (frame.CurrentSourcePageType != typeof(PromotionPage))
                frame.Navigate(typeof(PromotionPage));
            return Task.CompletedTask;
        }

        private static Task EnsureReportsAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Reports);
            if (frame.CurrentSourcePageType != typeof(ReportPage))
                frame.Navigate(typeof(ReportPage));
            return Task.CompletedTask;
        }

        private static Task EnsureSettingsAsync(DashboardPage dashboard, Frame frame, SidebarControl sidebar)
        {
            sidebar.SetActiveMenu(SidebarMenu.Settings);
            if (frame.CurrentSourcePageType != typeof(SettingsPage))
                frame.Navigate(typeof(SettingsPage));
            return Task.CompletedTask;
        }

        private static Task WaitForLoadedAsync(Frame frame)
        {
            if (frame.Content is FrameworkElement existing && existing.IsLoaded)
                return Task.CompletedTask;

            var tcs = new TaskCompletionSource();

            void Handler(object s, RoutedEventArgs e)
            {
                if (s is FrameworkElement fe) fe.Loaded -= Handler;
                tcs.TrySetResult();
            }

            if (frame.Content is FrameworkElement root)
                root.Loaded += Handler;
            else
                frame.Loaded += Handler;

            return tcs.Task;
        }

        private static bool ReadBool(string name, bool fallback)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegPath);
                if (key == null) return fallback;
                var raw = key.GetValue(name);
                return raw switch
                {
                    int i => i != 0,
                    string s when int.TryParse(s, out var i) => i != 0,
                    _ => fallback
                };
            }
            catch
            {
                return fallback;
            }
        }

        private static void WriteBool(string name, bool value)
        {
            try
            {
                using var key = Registry.CurrentUser.CreateSubKey(RegPath);
                key?.SetValue(name, value ? 1 : 0, RegistryValueKind.DWord);
            }
            catch
            {
            }
        }
    }

    internal static class DispatcherQueueExtensions
    {
        public static Task EnqueueAsync(this DispatcherQueue queue, Action action)
        {
            var tcs = new TaskCompletionSource();
            queue.TryEnqueue(() =>
            {
                try { action(); tcs.TrySetResult(); }
                catch (Exception ex) { tcs.TrySetException(ex); }
            });
            return tcs.Task;
        }
    }
}
