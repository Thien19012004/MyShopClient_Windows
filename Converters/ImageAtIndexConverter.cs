using System;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
   


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
                         
                            System.Diagnostics.Debug.WriteLine($"Relative URL detected: {imageUrl}");
                        }
                    }
                }
            }

     
            return new Uri("ms-appx:///Assets/placeholder-image.png", UriKind.Absolute);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
