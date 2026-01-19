using System;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
   
    /// Converter an toàn chuyen string thành Uri, tra ve null neu không hop ly
 
    public class SafeUriConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is string urlString && !string.IsNullOrWhiteSpace(urlString))
            {
              
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
