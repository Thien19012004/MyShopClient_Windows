using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Check n?u collection có ?úng N items
    /// Parameter: s? l??ng N c?n so sánh
    /// Return: Visibility.Visible n?u count == N, Collapsed n?u không
    /// </summary>
    public class CollectionHasExactlyConverter : IValueConverter
 {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
     if (value is List<string> list && parameter is string countStr)
        {
      if (int.TryParse(countStr, out int count))
  {
           return list.Count == count ? Visibility.Visible : Visibility.Collapsed;
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
