using MyShopClient.Models;
using MyShopClient.Services;
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
        private readonly JsonSerializerOptions _jsonOptions;
        private const int MaxRetries = 3;
        private static readonly TimeSpan[] RetryDelays = { TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(500), TimeSpan.FromSeconds(1) };

        public GraphQLClient(IHttpClientFactory httpFactory, IServerConfigService config)
        {
            _httpFactory = httpFactory;
            _config = config;
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
                    using var response = await client.PostAsJsonAsync(url, payload, ct);
                    var content = await response.Content.ReadAsStringAsync(ct);

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
                    await Task.Delay(RetryDelays[attempt], ct);
                }
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
                    using var response = await client.PostAsync(url, content, ct);
                    var responseContent = await response.Content.ReadAsStringAsync(ct);

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
                    await Task.Delay(RetryDelays[attempt], ct);
                }
            }

            throw lastException ?? new HttpRequestException("Request failed after retries");
        }

        public async Task<string> IntrospectSchemaAsync(CancellationToken ct = default)
        {
            // Introspection query: list types and their fields/args (can be large)
            const string introspect = @"
{ __schema { queryType { name } types { name fields { name args { name } } } } }
";
            try
            {
                var client = _httpFactory.CreateClient("Api");
                var url = _config.GraphQlEndpoint;
                var payload = new { query = introspect, variables = (object?)null };
                using var response = await client.PostAsJsonAsync(url, payload, ct);
                var content = await response.Content.ReadAsStringAsync(ct);
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
            // Direct transient exceptions
            if (ex is IOException || ex is SocketException || ex is HttpRequestException)
                return true;

            // Check inner exceptions
            if (ex.InnerException != null)
                return IsTransientError(ex.InnerException);

            return false;
        }
    }
}
