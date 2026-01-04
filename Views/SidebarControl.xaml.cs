using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace MyShopClient.Views
{
    public enum SidebarMenu
    {
        Dashboard,
        Products,
        Orders,
        Customers,
        Promotions,
        Reports,
        Settings,
    }

    public sealed partial class SidebarControl : UserControl
    {
        private bool _isCollapsed = false;
        private SidebarMenu _activeMenu = SidebarMenu.Dashboard;

        public SidebarControl()
        {
            this.InitializeComponent();
            SetActiveMenu(SidebarMenu.Dashboard);
            UpdateVisualState();
        }

        // ===== Sự kiện để DashboardPage nghe và điều hướng =====
        public event EventHandler? DashboardRequested;
        public event EventHandler? ProductsRequested;
        public event EventHandler? LogoutRequested;
        public event EventHandler? OrdersRequested;
        public event EventHandler? CustomersRequested;
        public event EventHandler? PromotionsRequested;
        public event EventHandler? ReportsRequested;
        public event EventHandler? SettingsRequested;

        private void Dashboard_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Dashboard);
            DashboardRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Products_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Products);
            ProductsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Logout_Click(object sender, RoutedEventArgs e)
        {
            LogoutRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Orders_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Orders);
            OrdersRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Customers_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Customers);
            CustomersRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Promotions_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Promotions);
            PromotionsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Reports_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Reports);
            ReportsRequested?.Invoke(this, EventArgs.Empty);
        }

        private void Settings_Click(object sender, RoutedEventArgs e)
        {
            SetActiveMenu(SidebarMenu.Settings);
            SettingsRequested?.Invoke(this, EventArgs.Empty);
        }

        // ===== Thu gọn / mở rộng sidebar =====
        private void HamburgerButton_Click(object sender, RoutedEventArgs e)
        {
            _isCollapsed = !_isCollapsed;
            UpdateVisualState();
        }

        private void UpdateVisualState()
        {
            // Tăng chiều rộng khi thu gọn để icon không bị cắt
            // 72–80 là vừa, mình dùng 72 cho gọn
            this.Width = _isCollapsed ? 72 : 230;

            var visibility = _isCollapsed ? Visibility.Collapsed : Visibility.Visible;

            TitlePanel.Visibility = visibility;
            MainMenuHeader.Visibility = visibility;
            DashboardText.Visibility = visibility;
            ProductsText.Visibility = visibility;
            OrdersText.Visibility = visibility;
            CustomersText.Visibility = visibility;
            PromotionsText.Visibility = visibility;
            ReportsText.Visibility = visibility;
            SettingsText.Visibility = visibility;
            LogoutText.Visibility = visibility;
        }


        // ===== Highlight menu đang active =====
        public void SetActiveMenu(SidebarMenu menu)
        {
            _activeMenu = menu;

            var activeBrush = (SolidColorBrush)Resources["SidebarActiveBrush"];
            var inactiveBrush = (SolidColorBrush)Resources["SidebarInactiveBrush"];

            var activeText = new SolidColorBrush(Colors.Black);
            var inactiveText = (SolidColorBrush)Resources["SidebarIconColor"];

            DashboardButton.Background = (menu == SidebarMenu.Dashboard) ? activeBrush : inactiveBrush;
            ProductsButton.Background = (menu == SidebarMenu.Products) ? activeBrush : inactiveBrush;
            OrdersButton.Background = (menu == SidebarMenu.Orders) ? activeBrush : inactiveBrush;
            CustomersButton.Background = (menu == SidebarMenu.Customers) ? activeBrush : inactiveBrush;
            PromotionsButton.Background = (menu == SidebarMenu.Promotions) ? activeBrush : inactiveBrush;
            ReportsButton.Background = (menu == SidebarMenu.Reports) ? activeBrush : inactiveBrush;
            SettingsButton.Background = (menu == SidebarMenu.Settings) ? activeBrush : inactiveBrush;

            // màu chữ: active thì trắng, inactive thì hơi mờ
            DashboardButton.Foreground = (menu == SidebarMenu.Dashboard) ? activeText : inactiveText;
            ProductsButton.Foreground = (menu == SidebarMenu.Products) ? activeText : inactiveText;
            OrdersButton.Foreground = (menu == SidebarMenu.Orders) ? activeText : inactiveText;
            CustomersButton.Foreground = (menu == SidebarMenu.Customers) ? activeText : inactiveText;
            PromotionsButton.Foreground = (menu == SidebarMenu.Promotions) ? activeText : inactiveText;
            ReportsButton.Foreground = (menu == SidebarMenu.Reports) ? activeText : inactiveText;
            SettingsButton.Foreground = (menu == SidebarMenu.Settings) ? activeText : inactiveText;
        }
    }
}
