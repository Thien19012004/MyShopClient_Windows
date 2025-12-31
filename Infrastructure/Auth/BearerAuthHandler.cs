using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System;

namespace MyShopClient.Infrastructure.Auth
{
 public class BearerAuthHandler : DelegatingHandler
 {
 private readonly Func<Task<string?>> _getToken;
 private readonly Func<Task<bool>> _refreshToken;

 public BearerAuthHandler(Func<Task<string?>> getToken, Func<Task<bool>> refreshToken)
 {
 _getToken = getToken;
 _refreshToken = refreshToken;
 }

 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
 {
 // attach token
 var token = await _getToken();
 if (!string.IsNullOrWhiteSpace(token))
 {
 request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
 }

 var response = await base.SendAsync(request, cancellationToken);

 if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
 {
 // try refresh
 try
 {
 var ok = await _refreshToken();
 if (ok)
 {
 var newToken = await _getToken();
 if (!string.IsNullOrWhiteSpace(newToken))
 {
 // clone request
 var newRequest = await CloneHttpRequestMessageAsync(request);
 newRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

 // dispose previous response
 response.Dispose();

 // retry once
 return await base.SendAsync(newRequest, cancellationToken);
 }
 }
 }
 catch
 {
 // ignore and return original401
 }
 }

 return response;
 }

 private static async Task<HttpRequestMessage> CloneHttpRequestMessageAsync(HttpRequestMessage req)
 {
 var clone = new HttpRequestMessage(req.Method, req.RequestUri);

 // copy request headers
 foreach (var header in req.Headers)
 clone.Headers.TryAddWithoutValidation(header.Key, header.Value);

 // copy content (if any)
 if (req.Content != null)
 {
 var ms = new MemoryStream();
 await req.Content.CopyToAsync(ms);
 ms.Position =0;
 var copy = new StreamContent(ms);

 // copy content headers
 foreach (var header in req.Content.Headers)
 copy.Headers.TryAddWithoutValidation(header.Key, header.Value);

 clone.Content = copy;
 }

 // copy properties
#if NETSTANDARD2_0 || NET461
 foreach (var prop in req.Properties)
 clone.Properties.Add(prop);
#else
 foreach (var kvp in req.Options.ToList())
 clone.Options.Set(new System.Net.Http.HttpRequestOptionsKey<object>(kvp.Key.ToString()), kvp.Value);
#endif

 return clone;
 }
 }
}
