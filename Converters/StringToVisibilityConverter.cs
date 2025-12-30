using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;
using System;

namespace MyShopClient.Converters
{
 // Converts a string to Visibility: Visible when not null or whitespace, Collapsed otherwise
 public class StringToVisibilityConverter : IValueConverter
 {
 public object Convert(object value, Type targetType, object parameter, string language)
 {
 var s = value as string;
 return string.IsNullOrWhiteSpace(s) ? Visibility.Collapsed : Visibility.Visible;
 }

 public object ConvertBack(object value, Type targetType, object parameter, string language)
 {
 throw new NotImplementedException();
 }
 }
}
