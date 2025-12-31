using System;

namespace MyShopClient.Helpers
{
    /// <summary>
/// Helper ?? x? lý URLs
    /// </summary>
    public static class UrlHelper
    {
      /// <summary>
        /// Convert relative URL thành absolute URL
        /// </summary>
        public static string ToAbsoluteUrl(string url, string baseUrl)
        {
    if (string.IsNullOrWhiteSpace(url))
     return url;

       // N?u ?ã là absolute URL thì return nguyên
  if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
         url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
       url.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
        {
         return url;
          }

          // N?u là relative URL thì ghép v?i base URL
         if (url.StartsWith("/"))
  {
                if (string.IsNullOrWhiteSpace(baseUrl))
                    baseUrl = "http://localhost:5135";

     // ??m b?o baseUrl không có '/' ? cu?i
        baseUrl = baseUrl.TrimEnd('/');

return baseUrl + url;
         }

  return url;
        }
    }
}
