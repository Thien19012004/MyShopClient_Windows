using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace MyShopClient.Converters
{

    /// Formats a numeric value with thousand separators for better readability.


    public class NumberFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value == null) return string.Empty;

            try
            {
                decimal number = System.Convert.ToDecimal(value);

                return number.ToString("N0", CultureInfo.InvariantCulture);
            }
            catch
            {
                return value.ToString() ?? string.Empty;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
