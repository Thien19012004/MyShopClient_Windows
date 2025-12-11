using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Services.Helpers
{
    /// <summary>
    /// Base HTTP client for GraphQL operations
    /// </summary>
    public abstract class GraphQlClientBase
    {
        protected readonly HttpClient HttpClient;
        protected readonly IServerConfigService ServerConfig;

        protected GraphQlClientBase(HttpClient httpClient, IServerConfigService serverConfig)
        {
    HttpClient = httpClient;
  ServerConfig = serverConfig;
        }

        /// <summary>
        /// Post GraphQL query/mutation and get strongly-typed response
        /// </summary>
        protected async Task<TPayload> PostGraphQlAsync<TPayload>(
      string query,
      object? variables = null,
       CancellationToken cancellationToken = default)
      {
 var url = ServerConfig.GraphQlEndpoint;
var requestBody = new { query, variables };

            using var response = await HttpClient.PostAsJsonAsync(url, requestBody, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
              throw new HttpRequestException(
               $"GraphQL endpoint returned HTTP {(int)response.StatusCode}: {content}");
   }

            try
     {
       return GraphQlHelper.ExtractData<TPayload>(content);
            }
       catch (Exception ex)
            {
    throw new InvalidOperationException($"Failed to parse GraphQL response: {ex.Message}", ex);
            }
        }
  }
}
