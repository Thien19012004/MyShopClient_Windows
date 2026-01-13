using Microsoft.UI.Xaml.Data;
using System;
using System.Globalization;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Formats a numeric value with thousand separators for better readability.
    /// Example: 80000000 -> "80,000,000"
    /// </summary>
    public class NumberFormatConverter : IValueConverter
    {
   public object Convert(object value, Type targetType, object parameter, string language)
    {
            if (value == null) return string.Empty;

            try
      {
       decimal number = System.Convert.ToDecimal(value);

     // Use Vietnamese culture for dot separator or invariant for comma
       // Using N0 format for no decimal places with thousand separators
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
