using MyShopClient.Models;
using MyShopClient.Services;
using MyShopClient.Services.Connection;
using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Infrastructure.GraphQL
{
    public class GraphQLClient : IGraphQLClient
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IServerConfigService _config;
        private readonly IConnectionErrorHandler _connectionErrorHandler;
        private readonly JsonSerializerOptions _jsonOptions;
        private const int MaxRetries = 3;
        private static readonly TimeSpan[] RetryDelays =
    {
            TimeSpan.FromMilliseconds(100),
      TimeSpan.FromMilliseconds(500),
            TimeSpan.FromSeconds(1)
        };
        private static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

        public GraphQLClient(IHttpClientFactory httpFactory, IServerConfigService config, IConnectionErrorHandler connectionErrorHandler)
        {
            _httpFactory = httpFactory;
            _config = config;
            _connectionErrorHandler = connectionErrorHandler;
            _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        }

        public async Task<T?> SendAsync<T>(string query, object? variables = null, CancellationToken ct = default)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var client = _httpFactory.CreateClient("Api");
                    var url = _config.GraphQlEndpoint;
                    var payload = new { query, variables };

                    using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    using var response = await client.PostAsJsonAsync(url, payload, linkedCts.Token);

                    string content;
                    try
                    {
                        content = await response.Content.ReadAsStringAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException("Response reading timeout");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {content}");
                    }

                    var gql = JsonSerializer.Deserialize<GraphQlResponse<T>>(content, _jsonOptions);
                    if (gql == null) throw new GraphQlException("Empty GraphQL response.");
                    if (gql.Errors != null && gql.Errors.Length > 0)
                    {
                        throw new GraphQlException(gql.Errors[0].Message, gql.Errors);
                    }

                    return gql.Data;
                }
                catch (Exception ex) when (IsTransientError(ex) && attempt < MaxRetries - 1 && !ct.IsCancellationRequested)
                {
                    lastException = ex;
                    Debug.WriteLine($"[GraphQL] Transient error on attempt {attempt + 1}: {ex.Message}. Retrying...");

                    try
                    {
                        await Task.Delay(RetryDelays[attempt], ct);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    // Notify connection error handler for fatal connection errors after all retries
                    if (attempt >= MaxRetries - 1)
                    {
                        _connectionErrorHandler.NotifyConnectionError(ex);
                    }
                    lastException = ex;

                    if (attempt < MaxRetries - 1 && !ct.IsCancellationRequested)
                    {
                        Debug.WriteLine($"[GraphQL] Connection error on attempt {attempt + 1}: {ex.Message}. Retrying...");
                        try
                        {
                            await Task.Delay(RetryDelays[attempt], ct);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                    }
                }
            }

            // Final check: if last exception is a connection error, notify handler
            if (lastException != null && IsConnectionError(lastException))
            {
                _connectionErrorHandler.NotifyConnectionError(lastException);
            }

            throw lastException ?? new HttpRequestException("Request failed after retries");
        }

        public async Task<T?> SendMultipartAsync<T>(MultipartFormDataContent content, CancellationToken ct = default)
        {
            Exception? lastException = null;

            for (int attempt = 0; attempt < MaxRetries; attempt++)
            {
                try
                {
                    var client = _httpFactory.CreateClient("Api");
                    var url = _config.GraphQlEndpoint;

                    using var timeoutCts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                    using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                    using var response = await client.PostAsync(url, content, linkedCts.Token);

                    string responseContent;
                    try
                    {
                        responseContent = await response.Content.ReadAsStringAsync(linkedCts.Token);
                    }
                    catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested)
                    {
                        throw new TimeoutException("Multipart response reading timeout");
                    }

                    if (!response.IsSuccessStatusCode)
                    {
                        throw new HttpRequestException($"HTTP {(int)response.StatusCode} {response.ReasonPhrase}: {responseContent}");
                    }

                    var gql = JsonSerializer.Deserialize<GraphQlResponse<T>>(responseContent, _jsonOptions);
                    if (gql == null) throw new GraphQlException("Empty GraphQL response.");
                    if (gql.Errors != null && gql.Errors.Length > 0)
                    {
                        throw new GraphQlException(gql.Errors[0].Message, gql.Errors);
                    }

                    return gql.Data;
                }
                catch (Exception ex) when (IsTransientError(ex) && attempt < MaxRetries - 1 && !ct.IsCancellationRequested)
                {
                    lastException = ex;
                    Debug.WriteLine($"[GraphQL] Transient error on attempt {attempt + 1}: {ex.Message}. Retrying...");

                    content?.Dispose();
                    content = new MultipartFormDataContent();

                    try
                    {
                        await Task.Delay(RetryDelays[attempt], ct);
                    }
                    catch (OperationCanceledException)
                    {
                        throw;
                    }
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex) when (IsConnectionError(ex))
                {
                    if (attempt >= MaxRetries - 1)
                    {
                        _connectionErrorHandler.NotifyConnectionError(ex);
                    }
                    lastException = ex;

                    if (attempt < MaxRetries - 1 && !ct.IsCancellationRequested)
                    {
                        Debug.WriteLine($"[GraphQL] Connection error on attempt {attempt + 1}: {ex.Message}. Retrying...");
                        content?.Dispose();
                        content = new MultipartFormDataContent();

                        try
                        {
                            await Task.Delay(RetryDelays[attempt], ct);
                        }
                        catch (OperationCanceledException)
                        {
                            throw;
                        }
                    }
                }
            }

            if (lastException != null && IsConnectionError(lastException))
            {
                _connectionErrorHandler.NotifyConnectionError(lastException);
            }

            throw lastException ?? new HttpRequestException("Request failed after retries");
        }

        public async Task<string> IntrospectSchemaAsync(CancellationToken ct = default)
        {
            const string introspect = @"
{ __schema { queryType { name } types { name fields { name args { name } } } } }
";
            try
            {
                var client = _httpFactory.CreateClient("Api");
                var url = _config.GraphQlEndpoint;
                var payload = new { query = introspect, variables = (object?)null };

                using var timeoutCts = new CancellationTokenSource(DefaultTimeout);
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);

                using var response = await client.PostAsJsonAsync(url, payload, linkedCts.Token);
                var content = await response.Content.ReadAsStringAsync(linkedCts.Token);
                Debug.WriteLine("[GraphQL] Introspection result:\n" + content);
                return content;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[GraphQL] Introspection failed: " + ex.Message);
                return string.Empty;
            }
        }

        /// <summary>
        /// Determines if an exception is a transient network error that should be retried.
        /// </summary>
        private static bool IsTransientError(Exception ex)
        {
 // ObjectDisposedException on NetworkStream is a transient error
            if (ex is ObjectDisposedException)
       return true;
      
            if (ex is IOException || ex is SocketException || ex is HttpRequestException || ex is TimeoutException)
        return true;

      if (ex.InnerException != null)
     return IsTransientError(ex.InnerException);

  return false;
      }

     /// <summary>
        /// Determines if an exception is a fatal connection error (server unreachable)
     /// </summary>
        private static bool IsConnectionError(Exception ex)
   {
        // ObjectDisposedException on NetworkStream indicates connection was closed
        if (ex is ObjectDisposedException disposedEx)
            {
       var objectName = disposedEx.ObjectName?.ToLowerInvariant() ?? "";
          if (objectName.Contains("networkstream") || objectName.Contains("socket"))
      {
        return true;
      }
            }
          
            // SocketException with various connection errors
        if (ex is SocketException socketEx)
{
        return socketEx.SocketErrorCode == SocketError.HostNotFound ||
   socketEx.SocketErrorCode == SocketError.HostUnreachable ||
       socketEx.SocketErrorCode == SocketError.NetworkUnreachable ||
   socketEx.SocketErrorCode == SocketError.ConnectionRefused ||
     socketEx.SocketErrorCode == SocketError.ConnectionReset ||
      socketEx.SocketErrorCode == SocketError.ConnectionAborted ||
       socketEx.SocketErrorCode == SocketError.TimedOut;
       }

     // IOException with "forcibly closed" message
   if (ex is IOException ioEx)
        {
          var message = ioEx.Message?.ToLowerInvariant() ?? "";
                if (message.Contains("forcibly closed") ||
 message.Contains("transport connection") ||
           message.Contains("remote host") ||
 message.Contains("disposed"))
              {
     return true;
        }
                if (ioEx.InnerException != null)
    {
               return IsConnectionError(ioEx.InnerException);
    }
            }

            // HttpRequestException often wraps SocketException or IOException
if (ex is HttpRequestException httpEx && httpEx.InnerException != null)
        {
        return IsConnectionError(httpEx.InnerException);
     }

      // Check inner exception for other types
     if (ex.InnerException != null)
   {
              return IsConnectionError(ex.InnerException);
        }

  return false;
   }
    }
}
