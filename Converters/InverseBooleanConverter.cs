using System;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        // bool -> !bool
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;

            return true;
        }

        // !bool -> bool
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;

            return false;
        }
    }
}
