using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{

    public class FirstImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<string> imagePaths && imagePaths.Count > 0)
            {
                var firstImage = imagePaths[0];
                if (!string.IsNullOrWhiteSpace(firstImage))
                {
           
                    return firstImage;
                }
            }

    
            return "ms-appx:///Assets/placeholder-image.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
