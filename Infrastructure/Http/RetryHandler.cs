using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Net.Sockets;
using System.IO;

namespace MyShopClient.Infrastructure.Http
{
    // Simple transient retry handler with exponential backoff + jitter
  
    public class RetryHandler : DelegatingHandler
    {
        private readonly int _maxRetries;
        private readonly int _baseDelayMs;

        public RetryHandler(int maxRetries = 3, int baseDelayMs = 200)
        {
            _maxRetries = maxRetries;
            _baseDelayMs = baseDelayMs;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt <= _maxRetries; attempt++)
            {
                try
                {
                    var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        return response;
                    }

            
                    if ((int)response.StatusCode >= 500 && attempt < _maxRetries)
                    {
             
                        Debug.WriteLine($"[RetryHandler] Got {response.StatusCode}, retrying...");
                    }
                    else
                    {
                        return response;
                    }
                }
                catch (OperationCanceledException)
                {
                   
                    Debug.WriteLine($"[RetryHandler] Operation cancelled on attempt {attempt + 1}");
                    throw;
                }
                catch (Exception ex) when (IsTransientNetworkError(ex) && attempt < _maxRetries)
                {
                    lastException = ex;
                    Debug.WriteLine($"[RetryHandler] Attempt {attempt + 1} failed with transient error: {ex.GetType().Name}: {ex.Message}");
                }
                catch (Exception ex)
                {
         
                    Debug.WriteLine($"[RetryHandler] Non-transient error: {ex.GetType().Name}: {ex.Message}");
                    throw;
                }

              
                if (attempt < _maxRetries)
                {
                    int delay = _baseDelayMs * (int)Math.Pow(2, attempt);
                    delay += new Random().Next(0, 200);
                    try
                    {
                        await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }
            }

            if (lastException != null)
                throw lastException;

            throw new HttpRequestException("Request failed after retries.");
        }

        private static bool IsTransientNetworkError(Exception ex)
        {
            return ex is IOException ||
                   ex is SocketException ||
                   ex is HttpRequestException ||
                   (ex is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested);
        }
    }
}
