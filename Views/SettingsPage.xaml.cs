using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using System.Linq;
using System.Threading.Tasks;

namespace MyShopClient.Views
{
 public sealed partial class SettingsPage : Page
 {
 public SettingsViewModel ViewModel => (SettingsViewModel)DataContext;

 public SettingsPage()
 {
 this.InitializeComponent();
 DataContext = App.Services.GetRequiredService<SettingsViewModel>();
 }

 private async void RunTourAgain_Click(object sender, RoutedEventArgs e)
 {
 // SettingsPage is hosted inside DashboardPage's ContentFrame.
 // We find the DashboardPage by walking up the visual tree.
 var dashboard = FindAncestor<DashboardPage>(this);
 if (dashboard != null)
 {
 await dashboard.StartOnboardingTourAsync();
 }
 }

 private static T? FindAncestor<T>(DependencyObject start) where T : DependencyObject
 {
 DependencyObject? current = start;
 while (current != null)
 {
 if (current is T match) return match;
 current = Microsoft.UI.Xaml.Media.VisualTreeHelper.GetParent(current);
 }
 return null;
 }
 }
}
