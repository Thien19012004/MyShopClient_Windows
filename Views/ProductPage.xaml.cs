using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using MyShopClient.ViewModels;
using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Storage.Pickers;
using WinRT;

namespace MyShopClient.Views
{
  public sealed partial class ProductPage : Page
    {
        public ProductListViewModel ViewModel => (ProductListViewModel)DataContext;

        public ProductPage()
        {
  this.InitializeComponent();
            DataContext = App.Services.GetService<ProductListViewModel>();
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
    }
}
