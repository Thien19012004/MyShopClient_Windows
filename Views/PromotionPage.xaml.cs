using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;

namespace MyShopClient.Views
{
    public sealed partial class PromotionPage : Page
    {
     public PromotionListViewModel ViewModel { get; }

      public PromotionPage()
{
            this.InitializeComponent();
        ViewModel = App.Services.GetRequiredService<PromotionListViewModel>();
            this.DataContext = ViewModel;
        }

        private void SelectAllPromotionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            var isChecked = (sender as Controls.BlueCheckBox)?.IsChecked ?? false;
            foreach (var p in ViewModel.Promotions)
            {
                p.IsSelected = isChecked;
            }
        }
    }
}
