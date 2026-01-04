using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT;
using MyShopClient.Controls;

namespace MyShopClient.Views
{
  public sealed partial class ProductPage : Page
    {
        public ProductListViewModel ViewModel => (ProductListViewModel)DataContext;
        private Grid _headerWideGrid;
        private Grid _headerNarrowGrid;
        private BlueCheckBox _selectAllWide;
        private BlueCheckBox _selectAllNarrow;
        private const double NarrowThreshold = 1100;

        public ProductPage()
        {
  this.InitializeComponent();
            DataContext = App.Services.GetService<ProductListViewModel>();
            CacheControls();

            Loaded += ProductPage_Loaded;
            SizeChanged += ProductPage_SizeChanged;
        }

        private void ProductPage_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateLayoutState(ActualWidth);
        }

        private void ProductPage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateLayoutState(e.NewSize.Width);
        }

        private void CacheControls()
        {
            _headerWideGrid = (Grid)FindName("HeaderWideGrid");
            _headerNarrowGrid = (Grid)FindName("HeaderNarrowGrid");
            _selectAllWide = (BlueCheckBox)FindName("SelectAllProductCheckBoxWide");
            _selectAllNarrow = (BlueCheckBox)FindName("SelectAllProductCheckBoxNarrow");
        }

        private void UpdateLayoutState(double width)
        {
            bool isNarrow = width < NarrowThreshold;

            if (_headerWideGrid != null && _headerNarrowGrid != null)
            {
                _headerWideGrid.Visibility = isNarrow ? Visibility.Collapsed : Visibility.Visible;
                _headerNarrowGrid.Visibility = isNarrow ? Visibility.Visible : Visibility.Collapsed;
            }

            ProductListView.ItemTemplate = (DataTemplate)Resources[isNarrow ? "ProductNarrowTemplate" : "ProductWideTemplate"];

            if (_selectAllWide != null && _selectAllNarrow != null)
            {
                _selectAllWide.IsChecked = _selectAllNarrow.IsChecked;
            }
        }

     private async void ImportButton_Click(object sender, RoutedEventArgs e)
{
     var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

          picker.SuggestedStartLocation = PickerLocationId.Downloads;
         picker.FileTypeFilter.Add(".xlsx");
    picker.FileTypeFilter.Add(".xls");

            var file = await picker.PickSingleFileAsync();
    if (file == null) return;

            using Stream stream = await file.OpenStreamForReadAsync();
       await ViewModel.ImportFromExcelAsync(stream);
}

        private async void AddUploadImageButton_Click(object sender, RoutedEventArgs e)
        {
         var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
  picker.FileTypeFilter.Add(".jpg");
picker.FileTypeFilter.Add(".jpeg");
   picker.FileTypeFilter.Add(".png");
   picker.FileTypeFilter.Add(".gif");

  var file = await picker.PickSingleFileAsync();
            if (file == null) return;

        using Stream stream = await file.OpenStreamForReadAsync();
      await ViewModel.UploadImageForNewProductAsync(stream, file.Name);
        }

        private async void EditUploadImageButton_Click(object sender, RoutedEventArgs e)
    {
            var picker = new FileOpenPicker();
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(App.MainWindow);
   WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

 picker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
     picker.FileTypeFilter.Add(".jpg");
            picker.FileTypeFilter.Add(".jpeg");
            picker.FileTypeFilter.Add(".png");
            picker.FileTypeFilter.Add(".gif");

         var file = await picker.PickSingleFileAsync();
  if (file == null) return;

using Stream stream = await file.OpenStreamForReadAsync();
            await ViewModel.UploadImageForEditProductAsync(stream, file.Name);
        }

        private void SelectAllProductCheckBox_Click(object sender, RoutedEventArgs e)
        {
            var isChecked = (sender as BlueCheckBox)?.IsChecked ?? false;
            if (_selectAllWide != null) _selectAllWide.IsChecked = isChecked;
            if (_selectAllNarrow != null) _selectAllNarrow.IsChecked = isChecked;

            var vm = (ProductListViewModel)DataContext;
            if (vm == null) return;
            foreach (var p in vm.Products)
            {
                p.IsSelected = isChecked;
            }
        }

        private void CategorySearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var vm = ViewModel;
            if (vm?.CategorySearchCommand?.CanExecute(null) == true)
            {
                vm.CategorySearchCommand.Execute(null);
            }
        }
    }
}
