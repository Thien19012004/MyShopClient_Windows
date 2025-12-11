using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // bool -> Visibility
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b && b)
                return Visibility.Visible;

            return Visibility.Collapsed;
        }

        // Visibility -> bool (thường không dùng, nhưng implement cho đủ interface)
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility v)
                return v == Visibility.Visible;

            return false;
        }
    }
}
