using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace MyShopClient.Infrastructure.Http
{
    public class LoggingHandler : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Debug.WriteLine($"[HTTP] {request.Method} {request.RequestUri}");
            if (request.Content != null)
            {
                var req = await request.Content.ReadAsStringAsync(cancellationToken);
                Debug.WriteLine($"[HTTP] Request Body: {req}");
            }
            var response = await base.SendAsync(request, cancellationToken);
            Debug.WriteLine($"[HTTP] RESPONSE {(int)response.StatusCode} {response.ReasonPhrase}");
            if (response.Content != null)
            {
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                Debug.WriteLine($"[HTTP] Response Body: {body}");
            }
            return response;
        }
    }
}
