using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Check n?u collection có item t?i index ???c ch? ??nh
/// Parameter: index c?n check (0, 1, 2, ...)
  /// Return: Visibility.Visible n?u có item, Collapsed n?u không
    /// </summary>
    public class CollectionHasItemsConverter : IValueConverter
    {
    public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<string> list && parameter is string indexStr)
     {
     if (int.TryParse(indexStr, out int index))
       {
                    return list.Count > index ? Visibility.Visible : Visibility.Collapsed;
        }
            }
   return Visibility.Collapsed;
        }

     public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
 throw new NotImplementedException();
        }
    }
}
