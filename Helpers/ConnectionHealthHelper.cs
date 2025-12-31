using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Helpers
{
    /// <summary>
    /// Helper to check and maintain HttpClient connection health
    /// </summary>
    public static class ConnectionHealthHelper
    {
        private static DateTime _lastSuccessfulRequest = DateTime.UtcNow;
        private static readonly TimeSpan MaxIdleTime = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Check if connection might be stale and needs refresh
        /// </summary>
        public static bool IsConnectionStale()
        {
            var idleTime = DateTime.UtcNow - _lastSuccessfulRequest;
            return idleTime > MaxIdleTime;
        }

        /// <summary>
        /// Mark connection as healthy after successful request
        /// </summary>
        public static void MarkConnectionHealthy()
        {
            _lastSuccessfulRequest = DateTime.UtcNow;
        }

        /// <summary>
        /// Perform a lightweight health check ping to server
        /// </summary>
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
                        response.StatusCode == System.Net.HttpStatusCode.NotFound; // 404 is OK, means server is up

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

        /// <summary>
        /// Ensure connection is healthy before making important requests
        /// </summary>
        public static async Task<bool> EnsureConnectionHealthyAsync(HttpClient client, CancellationToken cancellationToken = default)
        {
            if (!IsConnectionStale())
            {
                return true; // Connection is fresh
            }

            Debug.WriteLine("[Connection Health] Connection may be stale, performing health check...");
            return await PingServerAsync(client, cancellationToken: cancellationToken);
        }

        /// <summary>
        /// Get idle time duration
        /// </summary>
        public static TimeSpan GetIdleTime()
        {
            return DateTime.UtcNow - _lastSuccessfulRequest;
        }

        /// <summary>
        /// Reset health tracking (e.g., after app resume)
        /// </summary>
        public static void Reset()
        {
            _lastSuccessfulRequest = DateTime.UtcNow;
            Debug.WriteLine("[Connection Health] Health tracking reset");
        }
    }
}
