using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyShopClient.Infrastructure.Http
{
 // Simple transient retry handler with exponential backoff + jitter
 public class RetryHandler : DelegatingHandler
 {
 private readonly int _maxRetries;
 private readonly int _baseDelayMs;

 public RetryHandler(int maxRetries =3, int baseDelayMs =200)
 {
 _maxRetries = maxRetries;
 _baseDelayMs = baseDelayMs;
 }

 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
 {
 Exception? lastException = null;

 for (int attempt =0; attempt <= _maxRetries; attempt++)
 {
 try
 {
 var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
 if (response.IsSuccessStatusCode)
 {
 return response;
 }

 // Treat5xx as transient
 if ((int)response.StatusCode >=500 && attempt < _maxRetries)
 {
 // fall through to retry
 }
 else
 {
 return response;
 }
 }
 catch (Exception ex) when (!(ex is OperationCanceledException))
 {
 lastException = ex;
 Debug.WriteLine($"[RetryHandler] Attempt {attempt +1} failed: {ex.Message}");
 }

 // delay before next attempt
 int delay = _baseDelayMs * (int)Math.Pow(2, attempt);
 delay += new Random().Next(0,200);
 try { await Task.Delay(delay, cancellationToken).ConfigureAwait(false); } catch { }
 }

 if (lastException != null)
 throw lastException;

 throw new HttpRequestException("Request failed after retries.");
 }
 }
}
