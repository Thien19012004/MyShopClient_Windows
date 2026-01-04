using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using MyShopClient.Controls;

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
    }
}
