using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    /// <summary>
    /// L?y ?nh t?i index ???c ch? ??nh t? list
    /// Parameter: index c?n l?y (0, 1, 2, ...)
    /// </summary>
    public class ImageAtIndexConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<string> list && parameter is string indexStr)
          {
      if (int.TryParse(indexStr, out int index) && list.Count > index)
                {
        var imageUrl = list[index];
   if (!string.IsNullOrWhiteSpace(imageUrl))
        {
      // Ki?m tra và t?o Uri ?úng cách
            if (imageUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
          imageUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
            imageUrl.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
        {
       if (Uri.TryCreate(imageUrl, UriKind.Absolute, out Uri? uri))
       {
     return uri;
 }
       }
    else if (imageUrl.StartsWith("/"))
        {
       // Relative URL - c?n base URL t? config
     // T?m th?i return placeholder, c?n có base URL ?? x? lý
           System.Diagnostics.Debug.WriteLine($"Relative URL detected: {imageUrl}");
  }
  }
      }
            }

  // Placeholder n?u không có ?nh ho?c URL không h?p l?
       return new Uri("ms-appx:///Assets/placeholder-image.png", UriKind.Absolute);
        }

      public object ConvertBack(object value, Type targetType, object parameter, string language)
     {
        throw new NotImplementedException();
    }
    }
}
