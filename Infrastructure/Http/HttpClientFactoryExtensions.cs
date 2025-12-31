using System;
using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using MyShopClient.Infrastructure.Http;
using MyShopClient.Infrastructure.Auth;

namespace MyShopClient.Infrastructure.Http
{
 public static class HttpClientFactoryExtensions
 {
 public static IHttpClientBuilder AddApiClient(this IServiceCollection services, string name, Func<IServiceProvider, string?>? baseAddressProvider = null)
 {
 var builder = services.AddHttpClient(name, (sp, client) =>
 {
 if (baseAddressProvider != null)
 {
 var addr = baseAddressProvider(sp);
 if (!string.IsNullOrWhiteSpace(addr)) client.BaseAddress = new Uri(addr);
 }
 })
 .AddHttpMessageHandler(() => new RetryHandler())
 .AddHttpMessageHandler(() => new LoggingHandler());

 return builder;
 }
 }
}
