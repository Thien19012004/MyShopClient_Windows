using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShopClient.Controls;
using System.Linq;

namespace MyShopClient.Views
{
    public sealed partial class PromotionPage : Page
    {
        public PromotionListViewModel ViewModel { get; }

        private Grid _headerWideGrid;
        private Grid _headerNarrowGrid;
        private BlueCheckBox _selectAllWide;
        private BlueCheckBox _selectAllNarrow;
        private const double NarrowThreshold = 1100;

        public PromotionPage()
        {
            this.InitializeComponent();
            ViewModel = App.Services.GetRequiredService<PromotionListViewModel>();
            this.DataContext = ViewModel;

            CacheControls();

            Loaded += PromotionPage_Loaded;
            SizeChanged += PromotionPage_SizeChanged;
        }

        private void PromotionPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutState(ActualWidth);
        }

        private void PromotionPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutState(e.NewSize.Width);
        }

        private void CacheControls()
        {
            _headerWideGrid = (Grid)FindName("HeaderWideGrid");
            _headerNarrowGrid = (Grid)FindName("HeaderNarrowGrid");
            _selectAllWide = (BlueCheckBox)FindName("SelectAllPromotionCheckBoxWide");
            _selectAllNarrow = (BlueCheckBox)FindName("SelectAllPromotionCheckBoxNarrow");
        }

        private void UpdateLayoutState(double width)
        {
            bool isNarrow = width < NarrowThreshold;

            if (_headerWideGrid != null && _headerNarrowGrid != null)
            {
                _headerWideGrid.Visibility = isNarrow ? Visibility.Collapsed : Visibility.Visible;
                _headerNarrowGrid.Visibility = isNarrow ? Visibility.Visible : Visibility.Collapsed;
            }

            PromotionListView.ItemTemplate = (DataTemplate)Resources[isNarrow ? "PromotionNarrowTemplate" : "PromotionWideTemplate"];

            if (_selectAllWide != null && _selectAllNarrow != null)
            {
                _selectAllWide.IsChecked = _selectAllNarrow.IsChecked;
            }
        }

        private void SelectAllPromotionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (ViewModel == null) return;
            bool isChecked = (sender as BlueCheckBox)?.IsChecked ?? false;

            if (_selectAllWide != null) _selectAllWide.IsChecked = isChecked;
            if (_selectAllNarrow != null) _selectAllNarrow.IsChecked = isChecked;

            foreach (var p in ViewModel.Promotions)
            {
                p.IsSelected = isChecked;
            }
        }

        /// <summary>
        /// Handle product checkbox changes in selector dialogs (real-time sync)
        /// Selections are stored in AddVm.SelectedProducts / EditVm.SelectedProducts (local cache)
        /// which persists across search/reload of ProductSelectorVm.Products
        /// </summary>
        private void ProductCheckBox_Tapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
        {
            if (sender is not BlueCheckBox checkbox) return;
            if (checkbox.DataContext is not MyShopClient.Models.ProductItemDto product) return;

            // Use DispatcherQueue to run AFTER the binding updates
            // This ensures checkbox.IsChecked and product.IsSelected are already synced by binding
            DispatcherQueue.TryEnqueue(() =>
            {
                // Now product.IsSelected has the correct value from binding
                bool isSelected = product.IsSelected;

                // Determine if we're in ADD or EDIT dialog based on context
                if (ViewModel.AddVm.IsProductSelectorOpen)
                {
                    // Sync to AddVm.SelectedProducts (local cache)
                    if (isSelected)
                    {
                        if (!ViewModel.AddVm.SelectedProducts.Any(p => p.ProductId == product.ProductId))
                        {
                            ViewModel.AddVm.SelectedProducts.Add(product);
                        }
                    }
                    else
                    {
                        var existing = ViewModel.AddVm.SelectedProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
                        if (existing != null)
                            ViewModel.AddVm.SelectedProducts.Remove(existing);
                    }
                }
                else if (ViewModel.EditVm.IsProductSelectorOpen)
                {
                    // Sync to EditVm.SelectedProducts (local cache)
                    if (isSelected)
                    {
                        if (!ViewModel.EditVm.SelectedProducts.Any(p => p.ProductId == product.ProductId))
                        {
                            ViewModel.EditVm.SelectedProducts.Add(product);
                        }
                    }
                    else
                    {
                        var existing = ViewModel.EditVm.SelectedProducts.FirstOrDefault(p => p.ProductId == product.ProductId);
                        if (existing != null)
                            ViewModel.EditVm.SelectedProducts.Remove(existing);
                    }
                }
            });
        }
    }
}
