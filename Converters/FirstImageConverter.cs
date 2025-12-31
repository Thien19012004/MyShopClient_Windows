using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    /// <summary>
    /// L?y ?nh ??u tiên t? list ImagePaths.
    /// N?u không có ?nh ? tr? v? placeholder.
    /// </summary>
    public class FirstImageConverter : IValueConverter
    {
        public object? Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is List<string> imagePaths && imagePaths.Count > 0)
            {
                var firstImage = imagePaths[0];
                if (!string.IsNullOrWhiteSpace(firstImage))
                {
                    // Tr? v? URL c?a ?nh ??u tiên
                    return firstImage;
                }
            }

            // Placeholder image n?u không có ?nh ho?c list r?ng
            // NOTE: ProductListItemDto không có imagePaths ? luôn return placeholder
            return "ms-appx:///Assets/placeholder-image.png";
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
