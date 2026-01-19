using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Helpers
{

    /// Helper to check and maintain HttpClient connection health

    public static class ConnectionHealthHelper
    {
        private static DateTime _lastSuccessfulRequest = DateTime.UtcNow;
        private static readonly TimeSpan MaxIdleTime = TimeSpan.FromMinutes(5);

      
        public static bool IsConnectionStale()
        {
            var idleTime = DateTime.UtcNow - _lastSuccessfulRequest;
            return idleTime > MaxIdleTime;
        }

  
        public static void MarkConnectionHealthy()
        {
            _lastSuccessfulRequest = DateTime.UtcNow;
        }

  
        public static async Task<bool> PingServerAsync(HttpClient client, string? healthCheckUrl = null, CancellationToken cancellationToken = default)
        {
            try
            {
                // Use simple HEAD request to check server
                var url = healthCheckUrl ?? "/";

                var request = new HttpRequestMessage(HttpMethod.Head, url);
                request.Headers.ConnectionClose = false;

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromSeconds(5));

                var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cts.Token);

                bool isHealthy = response.IsSuccessStatusCode ||
                        response.StatusCode == System.Net.HttpStatusCode.NotFound; 

                if (isHealthy)
                {
                    MarkConnectionHealthy();
                    Debug.WriteLine("[Connection Health] Server ping successful");
                }
                else
                {
                    Debug.WriteLine($"[Connection Health] Server ping failed with status: {response.StatusCode}");
                }

                return isHealthy;
            }
            catch (ObjectDisposedException ex)
            {
                Debug.WriteLine($"[Connection Health] ObjectDisposedException during ping: {ex.Message}");
                Debug.WriteLine("[Connection Health] Connection was disposed - will be recreated on next request");
                return false;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"[Connection Health] Server ping failed: {ex.Message}");
                return false;
            }
        }

      
        public static async Task<bool> EnsureConnectionHealthyAsync(HttpClient client, CancellationToken cancellationToken = default)
        {
            if (!IsConnectionStale())
            {
                return true; 
            }

            Debug.WriteLine("[Connection Health] Connection may be stale, performing health check...");
            return await PingServerAsync(client, cancellationToken: cancellationToken);
        }

     
        public static TimeSpan GetIdleTime()
        {
            return DateTime.UtcNow - _lastSuccessfulRequest;
        }

     
        public static void Reset()
        {
            _lastSuccessfulRequest = DateTime.UtcNow;
            Debug.WriteLine("[Connection Health] Health tracking reset");
        }
    }
}
