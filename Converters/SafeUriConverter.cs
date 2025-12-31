using System;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Converter an toàn chuy?n string thành Uri, tr? v? null n?u không h?p l?
    /// </summary>
    public class SafeUriConverter : IValueConverter
    {
     public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string urlString && !string.IsNullOrWhiteSpace(urlString))
    {
       // Ki?m tra n?u là text nh? "Uploading..." thì return null
             if (urlString.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
      urlString.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
urlString.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
                {
          if (Uri.TryCreate(urlString, UriKind.Absolute, out Uri? uri))
       {
   return uri;
      }
              }
  }
        
            // Tr? v? placeholder image n?u URL không h?p l?
     return new Uri("ms-appx:///Assets/placeholder-image.png", UriKind.Absolute);
        }

     public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
   if (value is Uri uri)
   {
           return uri.ToString();
 }
          return string.Empty;
        }
    }
}
