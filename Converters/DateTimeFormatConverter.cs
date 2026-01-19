using Microsoft.UI.Xaml.Data;
using System;

namespace MyShopClient.Converters
{
    /// <summary>
    /// Converts DateTime to a friendly readable format
    /// </summary>
    public class DateTimeFormatConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is DateTime dateTime)
  {
        // N?u có parameter, s? d?ng format ?ó
           if (parameter is string format && !string.IsNullOrEmpty(format))
   {
        return dateTime.ToString(format);
        }

      // Format m?c ??nh: dd/MM/yyyy HH:mm
                return dateTime.ToString("dd/MM/yyyy HH:mm");
            }

    if (value is DateTimeOffset dateTimeOffset)
   {
         if (parameter is string format && !string.IsNullOrEmpty(format))
      {
        return dateTimeOffset.ToString(format);
       }

         return dateTimeOffset.ToString("dd/MM/yyyy HH:mm");
    }

   // N?u là string ISO 8601, parse và format l?i
            if (value is string dateString && !string.IsNullOrEmpty(dateString))
    {
          if (DateTime.TryParse(dateString, out DateTime parsedDate))
         {
        if (parameter is string format && !string.IsNullOrEmpty(format))
           {
              return parsedDate.ToString(format);
       }

     return parsedDate.ToString("dd/MM/yyyy HH:mm");
            }
  }

            return value?.ToString() ?? string.Empty;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
 {
  throw new NotImplementedException();
        }
    }
}
