using System;

namespace MyShopClient.Helpers
{

    public static class UrlHelper
    {

        /// Convert relative URL thành absolute URL

        public static string ToAbsoluteUrl(string url, string baseUrl)
        {
            if (string.IsNullOrWhiteSpace(url))
                return url;

            if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                   url.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                 url.StartsWith("ms-appx://", StringComparison.OrdinalIgnoreCase))
            {
                return url;
            }

            if (url.StartsWith("/"))
            {
                if (string.IsNullOrWhiteSpace(baseUrl))
                    baseUrl = "http://localhost:5135";

                baseUrl = baseUrl.TrimEnd('/');

                return baseUrl + url;
            }

            return url;
        }
    }
}
