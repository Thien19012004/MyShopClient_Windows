using System;
using Microsoft.UI.Xaml.Data;

namespace MyShopClient.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;

            return true;
        }

    
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is bool b)
                return !b;

            return false;
        }
    }
}
