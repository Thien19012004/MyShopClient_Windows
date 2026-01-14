using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Infrastructure.GraphQL
{
    public interface IGraphQLClient
    {
        Task<T?> SendAsync<T>(string query, object? variables = null, CancellationToken ct = default);
        Task<T?> SendMultipartAsync<T>(MultipartFormDataContent content, CancellationToken ct = default);
    }
}
