using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using MyShopClient.Models;

namespace MyShopClient.Services.Order
{


    public class OrderService : IOrderService
    {
        private readonly HttpClient _httpClient;
        private readonly IServerConfigService _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public OrderService(HttpClient httpClient, IServerConfigService config)
        {
            _httpClient = httpClient;
            _config = config;
            // CRITICAL: Do NOT use JsonStringEnumConverter as it overrides custom converters!
            // Only use our custom OrderStatusJsonConverter (registered via attribute on the enum)
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        private async Task<TData> SendGraphQlAsync<TData>(string query, object variables)
        {
            var payload = new { query, variables };
            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var endpoint = new Uri(new Uri(_config.Current.BaseUrl), _config.GraphQlEndpoint);
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            Debug.WriteLine("[GraphQL] Status: " + (int)response.StatusCode + " " + response.ReasonPhrase);
            Debug.WriteLine("[GraphQL] Body: " + responseJson);

            GraphQlResponse<TData>? graphQl;
            try
            {
                graphQl = JsonSerializer.Deserialize<GraphQlResponse<TData>>(responseJson, _jsonOptions);
            }
            catch (Exception ex)
            {
                throw new Exception($"Cannot parse GraphQL response. Raw: {responseJson}", ex);
            }

            if (!response.IsSuccessStatusCode)
            {
                var msg = graphQl?.Errors != null && graphQl.Errors.Length > 0
 ? graphQl.Errors[0].Message
 : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                throw new Exception(msg);
            }

            if (graphQl == null) throw new Exception("GraphQL response is null.");
            if (graphQl.Errors != null && graphQl.Errors.Length > 0) throw new Exception(graphQl.Errors[0].Message);
            if (graphQl.Data == null) throw new Exception("GraphQL response has no data.");

            return graphQl.Data;
        }

        private class OrdersRoot { public ApiResult<PagedOrderResult> Orders { get; set; } = default!; }
        private class OrderByIdData { public ApiResult<OrderDetailDto>? OrderById { get; set; } }
        private class CreateOrderData { public ApiResult<OrderDetailDto>? CreateOrder { get; set; } }
        private class UpdateOrderData { public ApiResult<OrderDetailDto>? UpdateOrder { get; set; } }
        private class DeleteOrderData { public ApiResult<object>? DeleteOrder { get; set; } }

        public async Task<ApiResult<PagedOrderResult>> GetOrdersAsync(OrderQueryOptions opt)
        {
            const string query = @"
query($pagination: PaginationInput, $filter: OrderFilterInput, $dateRange: DateRangeInput) {
 orders(pagination: $pagination, filter: $filter, dateRange: $dateRange) {
 statusCode
 success
 message
 data {
 page
 pageSize
 totalItems
 totalPages
 items {
 orderId
 status
 createdAt
 customerName
 saleName
 orderDiscountAmount
 totalPrice
 itemsCount
 }
 }
 }
}";
            string? fromStr = opt.FromDate?.ToString("yyyy-MM-dd");
            string? toStr = opt.ToDate?.ToString("yyyy-MM-dd");

            var variables = new
            {
                pagination = new { page = opt.Page, pageSize = opt.PageSize },
                filter = new { customerId = opt.CustomerId, saleId = opt.SaleId, status = opt.Status },
                dateRange = opt.FromDate == null && opt.ToDate == null ? null : new { from = fromStr, to = toStr }
            };

            var gql = await SendGraphQlAsync<OrdersRoot>(query, variables);
            var apiResult = gql.Orders;
            return new ApiResult<PagedOrderResult>
            {
                StatusCode = apiResult.StatusCode,
                Success = apiResult.Success,
                Message = apiResult.Message,
                Data = apiResult.Data
            };
        }

        public async Task<ApiResult<OrderDetailDto>> GetOrderByIdAsync(int orderId)
        {
            const string query = @"
query($id: Int!) {
 orderById(orderId: $id) {
 statusCode
 success
 message
 data {
 orderId
 status
 createdAt
 customerId
 customerName
 customerPhone
 saleId
 saleName
 subtotal
 orderDiscountAmount
 orderDiscountPercentApplied
 totalPrice
 promotionIds
 items {
 orderItemId
 productId
 productName
 quantity
 unitPrice
 totalPrice
 }
 }
 }
}";

            var variables = new { id = orderId };
            try
            {
                var data = await SendGraphQlAsync<OrderByIdData>(query, variables);
                return data.OrderById ?? new ApiResult<OrderDetailDto> { Success = false, Message = "No orderById field in response." };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrderDetailDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResult<OrderDetailDto>> CreateOrderAsync(OrderCreateInput input)
        {
            const string query = @"
mutation($input: CreateOrderInput!) {
 createOrder(input: $input) {
 statusCode
 success
 message
 data {
 orderId
 status
 createdAt
 subtotal
 orderDiscountAmount
 totalPrice
 promotionIds
 items {
 orderItemId
 productId
 productName
 quantity
 unitPrice
 totalPrice
 }
 }
 }
}";

            var variables = new
            {
                input = new
                {
                    customerId = input.CustomerId,
                    saleId = input.SaleId,
                    promotionIds = input.PromotionIds,
                    items = input.Items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray()
                }
            };

            try
            {
                var data = await SendGraphQlAsync<CreateOrderData>(query, variables);
                return data.CreateOrder ?? new ApiResult<OrderDetailDto> { Success = false, Message = "No createOrder field in response." };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrderDetailDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResult<OrderDetailDto>> UpdateOrderAsync(int orderId, OrderUpdateInput input)
        {
            const string query = @"
mutation($id: Int!, $input: UpdateOrderInput!) {
 updateOrder(orderId: $id, input: $input) {
 statusCode
 success
 message
 data {
 orderId
 status
 subtotal
 orderDiscountAmount
 totalPrice
 promotionIds
 items {
 orderItemId
 productId
 productName
 quantity
 unitPrice
 totalPrice
 }
 }
 }
}";

            var variables = new
            {
                id = orderId,
                input = new
                {
                    status = input.Status,
                    promotionIds = input.PromotionIds,
                    items = input.Items?.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray()
                }
            };

            try
            {
                var data = await SendGraphQlAsync<UpdateOrderData>(query, variables);
                return data.UpdateOrder ?? new ApiResult<OrderDetailDto> { Success = false, Message = "No updateOrder field in response." };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrderDetailDto> { Success = false, Message = ex.Message };
            }
        }

        public async Task<ApiResult<bool>> DeleteOrderAsync(int orderId)
        {
            const string query = @"
mutation($id: Int!) {
 deleteOrder(orderId: $id) {
 statusCode
 success
 message
 }
}";

            var variables = new { id = orderId };

            try
            {
                var data = await SendGraphQlAsync<DeleteOrderData>(query, variables);
                var res = data.DeleteOrder ?? new ApiResult<object> { Success = false, Message = "No deleteOrder field in response." };

                return new ApiResult<bool>
                {
                    Success = res.Success,
                    StatusCode = res.StatusCode,
                    Message = res.Message,
                    Data = res.Success
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<bool> { Success = false, Message = ex.Message };
            }
        }
    }
}
