using MyShopClient.Models;
using MyShopClient.Services;
using System.Net.Http;
using System.Net.Http.Json;
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

 public GraphQLClient(IHttpClientFactory httpFactory, IServerConfigService config)
 {
 _httpFactory = httpFactory;
 _config = config;
 _jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
 }

 public async Task<T?> SendAsync<T>(string query, object? variables = null, CancellationToken ct = default)
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
 if (gql == null) throw new HttpRequestException("Empty GraphQL response.");
 if (gql.Errors != null && gql.Errors.Length >0)
 {
 throw new HttpRequestException(gql.Errors[0].Message);
 }

 return gql.Data;
 }

 public async Task<T?> SendMultipartAsync<T>(MultipartFormDataContent content, CancellationToken ct = default)
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
 if (gql == null) throw new HttpRequestException("Empty GraphQL response.");
 if (gql.Errors != null && gql.Errors.Length >0)
 {
 throw new HttpRequestException(gql.Errors[0].Message);
 }

 return gql.Data;
 }
 }
}
