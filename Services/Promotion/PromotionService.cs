using MyShopClient.Models;
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using MyShopClient.Infrastructure.GraphQL;

namespace MyShopClient.Services.Promotion
{
 public class PromotionService : IPromotionService
 {
 private readonly HttpClient _httpClient;
 private readonly IServerConfigService _serverConfig;
 private readonly JsonSerializerOptions _jsonOptions;
 private readonly IGraphQLClient _gql;

 public PromotionService(HttpClient httpClient, IServerConfigService serverConfig, IGraphQLClient gql)
 {
 _httpClient = httpClient;
 _serverConfig = serverConfig;

 // CRITICAL: Do NOT use JsonStringEnumConverter as it overrides custom converters!
 // Only use our custom PromotionScopeJsonConverter (registered via attribute on the enum)
 _jsonOptions = new JsonSerializerOptions
 {
 PropertyNameCaseInsensitive = true
 };
 _gql = gql;
 }

 private static bool IsTransientError(Exception ex)
 {
 return ex is System.IO.IOException
 || ex is System.Net.Sockets.SocketException
 || ex is HttpRequestException
 || ex is ObjectDisposedException
 || ex is TaskCanceledException tce && !tce.CancellationToken.IsCancellationRequested;
 }

 private class GetPromotionsPayload
 {
 public ApiResult<PromotionPageResult> Promotions { get; set; } = null!;
 }

 private class GetPromotionByIdPayload
 {
 public ApiResult<PromotionDetailDto> PromotionById { get; set; } = null!;
 }

 private class CreatePromotionPayload
 {
 public ApiResult<PromotionDetailDto> CreatePromotion { get; set; } = null!;
 }

 private class UpdatePromotionPayload
 {
 public ApiResult<PromotionDetailDto> UpdatePromotion { get; set; } = null!;
 }

 private class DeletePromotionPayload
 {
 public ApiResult<object?> DeletePromotion { get; set; } = null!;
 }

 public async Task<ApiResult<PromotionPageResult>> GetPromotionsAsync(
 PromotionQueryOptions options,
 CancellationToken cancellationToken = default)
 {
 var query = PromotionQueries.GetPromotionsQuery;

 // Build filter object conditionally
 var filter = new System.Collections.Generic.Dictionary<string, object?>();

 if (!string.IsNullOrWhiteSpace(options.Search))
 filter["search"] = options.Search;

 filter["onlyActive"] = options.OnlyActive;

 if (options.Scope.HasValue)
 filter["scope"] = options.Scope.Value;

 var variables = new
 {
 pagination = new { page = options.Page, pageSize = options.PageSize },
 filter
 };

 var data = await _gql.SendAsync<GetPromotionsPayload>(query, variables, cancellationToken);
 return data?.Promotions ?? new ApiResult<PromotionPageResult> { StatusCode =500, Success = false, Message = "No data from server" };
 }

 public async Task<ApiResult<PromotionDetailDto>> GetPromotionByIdAsync(int promotionId, CancellationToken cancellationToken = default)
 {
 var query = PromotionQueries.GetPromotionByIdQuery;
 var variables = new { id = promotionId };
 var data = await _gql.SendAsync<GetPromotionByIdPayload>(query, variables, cancellationToken);
 return data?.PromotionById ?? new ApiResult<PromotionDetailDto> { StatusCode =500, Success = false, Message = "No data from server" };
 }

 public async Task<ApiResult<PromotionDetailDto>> CreatePromotionAsync(CreatePromotionInput input, CancellationToken cancellationToken = default)
 {
 var query = PromotionQueries.CreatePromotionMutation;
 var variables = new
 {
 input = new
 {
 name = input.Name,
 discountPercent = input.DiscountPercent,
 startDate = input.StartDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
 endDate = input.EndDate.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
 scope = input.Scope,
 productIds = input.ProductIds,
 categoryIds = input.CategoryIds
 }
 };
 var data = await _gql.SendAsync<CreatePromotionPayload>(query, variables, cancellationToken);
 return data?.CreatePromotion ?? new ApiResult<PromotionDetailDto> { StatusCode =500, Success = false, Message = "No data from server" };
 }

 public async Task<ApiResult<PromotionDetailDto>> UpdatePromotionAsync(int promotionId, UpdatePromotionInput input, CancellationToken cancellationToken = default)
 {
 var query = PromotionQueries.UpdatePromotionMutation;
 var variables = new
 {
 id = promotionId,
 input = new
 {
 name = input.Name,
 discountPercent = input.DiscountPercent,
 startDate = input.StartDate.HasValue ? input.StartDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null,
 endDate = input.EndDate.HasValue ? input.EndDate.Value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ") : null,
 scope = input.Scope,
 productIds = input.ProductIds,
 categoryIds = input.CategoryIds
 }
 };
 var data = await _gql.SendAsync<UpdatePromotionPayload>(query, variables, cancellationToken);
 return data?.UpdatePromotion ?? new ApiResult<PromotionDetailDto> { StatusCode =500, Success = false, Message = "No data from server" };
 }

 public async Task<ApiResult<bool>> DeletePromotionAsync(int promotionId, CancellationToken cancellationToken = default)
 {
 var query = PromotionQueries.DeletePromotionMutation;
 var variables = new { id = promotionId };
 var data = await _gql.SendAsync<DeletePromotionPayload>(query, variables, cancellationToken);
 var inner = data?.DeletePromotion;
 return new ApiResult<bool> { StatusCode = inner?.StatusCode ??500, Success = inner?.Success ?? false, Message = inner?.Message, Data = inner?.Success ?? false };
 }
 }
}
