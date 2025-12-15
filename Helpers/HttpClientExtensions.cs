using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Helpers
{
    /// <summary>
    /// Extension methods for HttpClient with retry logic
    /// </summary>
    public static class HttpClientExtensions
    {
 /// <summary>
        /// Send HTTP request with automatic retry on network errors
  /// </summary>
        public static async Task<HttpResponseMessage> SendWithRetryAsync(
  this HttpClient client,
         HttpRequestMessage request,
        int maxRetries = 3,
     int delayMilliseconds = 1000,
     CancellationToken cancellationToken = default)
    {
      Exception lastException = null;

 for (int attempt = 0; attempt <= maxRetries; attempt++)
   {
        try
         {
             // Clone request for retry (HttpRequestMessage can only be sent once)
              var clonedRequest = await CloneHttpRequestMessageAsync(request);
 
        var response = await client.SendAsync(clonedRequest, cancellationToken);
   return response;
       }
        catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
         {
              lastException = ex;
    
     System.Diagnostics.Debug.WriteLine(
      $"[HttpRetry] Attempt {attempt + 1}/{maxRetries + 1} failed: {ex.Message}");
         
       // Wait before retry with exponential backoff
   int delay = delayMilliseconds * (int)Math.Pow(2, attempt);
              await Task.Delay(delay, cancellationToken);
    }
            }

            // All retries failed
  throw new HttpRequestException(
        $"Request failed after {maxRetries + 1} attempts. See inner exception for details.",
     lastException);
        }

        /// <summary>
        /// Check if error is transient (network related) and should be retried
        /// </summary>
        private static bool IsTransientError(Exception ex)
   {
          return ex is IOException ||
       ex is SocketException ||
            ex is HttpRequestException ||
         ex is TaskCanceledException && !((TaskCanceledException)ex).CancellationToken.IsCancellationRequested;
        }

        /// <summary>
        /// Clone HttpRequestMessage for retry
  /// </summary>
        private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage request)
{
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
   {
      Version = request.Version
      };

       // Copy headers
    foreach (var header in request.Headers)
 {
      clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            // Copy content
     if (request.Content != null)
            {
       var content = await request.Content.ReadAsByteArrayAsync();
       clone.Content = new ByteArrayContent(content);

            // Copy content headers
       foreach (var header in request.Content.Headers)
 {
               clone.Content.Headers.TryAddWithoutValidation(header.Key, header.Value);
         }
          }

     return clone;
  }

  /// <summary>
        /// PostAsJsonAsync with retry logic
        /// </summary>
        public static async Task<HttpResponseMessage> PostAsJsonWithRetryAsync<T>(
            this HttpClient client,
         string requestUri,
        T value,
       int maxRetries = 3,
            CancellationToken cancellationToken = default)
        {
            Exception lastException = null;

            for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
         try
      {
  var response = await client.PostAsJsonAsync(requestUri, value, cancellationToken);
           return response;
             }
    catch (Exception ex) when (IsTransientError(ex) && attempt < maxRetries)
    {
         lastException = ex;

  System.Diagnostics.Debug.WriteLine(
              $"[HttpRetry] POST {requestUri} attempt {attempt + 1}/{maxRetries + 1} failed: {ex.Message}");
        
     int delay = 1000 * (int)Math.Pow(2, attempt);
            await Task.Delay(delay, cancellationToken);
          }
     }

            throw new HttpRequestException(
     $"POST {requestUri} failed after {maxRetries + 1} attempts.",
       lastException);
        }
    }
}
