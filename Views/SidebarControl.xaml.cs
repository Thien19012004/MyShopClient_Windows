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
        Products
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
            SettingsText.Visibility = visibility;
            LogoutText.Visibility = visibility;
        }


        // ===== Highlight menu đang active =====
        public void SetActiveMenu(SidebarMenu menu)
        {
            _activeMenu = menu;

            var activeBrush = (SolidColorBrush)Resources["SidebarActiveBrush"];
            var inactiveBrush = (SolidColorBrush)Resources["SidebarInactiveBrush"];

            DashboardButton.Background = (menu == SidebarMenu.Dashboard) ? activeBrush : inactiveBrush;
            ProductsButton.Background = (menu == SidebarMenu.Products) ? activeBrush : inactiveBrush;

            // màu chữ: active thì trắng, inactive thì hơi mờ
            DashboardButton.Foreground = (menu == SidebarMenu.Dashboard)
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Colors.Gainsboro);

            ProductsButton.Foreground = (menu == SidebarMenu.Products)
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush(Colors.Gainsboro);
        }
    }
}
