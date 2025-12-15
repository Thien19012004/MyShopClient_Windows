using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Diagnostics;
using MyShopClient.Models;

namespace MyShopClient.Services
{
    public interface IOrderService
    {
        Task<ApiResult<PagedOrderResult>> GetOrdersAsync(OrderQueryOptions options);
        Task<ApiResult<OrderDetailDto>> GetOrderByIdAsync(int orderId);
        Task<ApiResult<OrderDetailDto>> CreateOrderAsync(OrderCreateInput input);
        Task<ApiResult<OrderDetailDto>> UpdateOrderAsync(int orderId, OrderUpdateInput input);
        Task<ApiResult<bool>> DeleteOrderAsync(int orderId);
    }

    public class OrderService : IOrderService
    {
        private readonly HttpClient _httpClient;
        private readonly IServerConfigService _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public OrderService(HttpClient httpClient, IServerConfigService config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

        // -------- helper gửi GraphQL chung ----------
        private async Task<TData> SendGraphQlAsync<TData>(string query, object variables)
        {
            var payload = new { query, variables };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            // Log request body for debugging
            Debug.WriteLine("[GraphQL Request] Endpoint: " + new Uri(new Uri(_config.Current.BaseUrl), _config.GraphQlEndpoint));
            Debug.WriteLine("[GraphQL Request Body] " + json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var endpoint = new Uri(new Uri(_config.Current.BaseUrl), _config.GraphQlEndpoint);
            var response = await _httpClient.PostAsync(endpoint, content);
            var responseJson = await response.Content.ReadAsStringAsync();

            // Log response body for debugging
            Debug.WriteLine("[GraphQL Response Status] " + ((int)response.StatusCode) + " " + response.ReasonPhrase);
            Debug.WriteLine("[GraphQL Response Body] " + responseJson);

            GraphQlResponse<TData> graphQl;
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
                var msg = (graphQl != null && graphQl.Errors != null && graphQl.Errors.Length >0)
 ? graphQl.Errors[0].Message
 : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
                throw new Exception(msg);
            }

            if (graphQl == null)
                throw new Exception("GraphQL response is null.");

            if (graphQl.Errors != null && graphQl.Errors.Length >0)
                throw new Exception(graphQl.Errors[0].Message);

            if (graphQl.Data == null)
                throw new Exception("GraphQL response has no data.");

            return graphQl.Data;
        }

        // wrapper cho field data
        private class OrdersData
        {
            public ApiResult<OrderPageDto>? Orders { get; set; }
        }

        private class OrderByIdData
        {
            public ApiResult<OrderDetailDto>? OrderById { get; set; }
        }

        private class CreateOrderData
        {
            public ApiResult<OrderDetailDto>? CreateOrder { get; set; }
        }

        private class UpdateOrderData
        {
            public ApiResult<OrderDetailDto>? UpdateOrder { get; set; }
        }

        private class DeleteOrderData
        {
            public ApiResult<object>? DeleteOrder { get; set; }
        }
        private class OrdersRoot
        {
            public ApiResult<PagedOrderResult> Orders { get; set; } = default!;
        }
        // -------- GET LIST ----------
        public async Task<ApiResult<PagedOrderResult>> GetOrdersAsync(OrderQueryOptions opt)
        {
            var graphQlEndpoint = new Uri(new Uri(_config.Current.BaseUrl), _config.GraphQlEndpoint);

            // Convert date-only inputs to date string (yyyy-MM-dd) because GraphQL expects Date scalar
            string? fromStr = opt.FromDate?.ToString("yyyy-MM-dd");
            string? toStr = opt.ToDate?.ToString("yyyy-MM-dd");

            var query = @"
query GetOrders($page: Int!, $pageSize: Int!,
                $customerId: Int, $saleId: Int, $status: OrderStatus,
                $from: Date, $to: Date) {
  orders(
    pagination: { page: $page, pageSize: $pageSize }
    filter: { customerId: $customerId, saleId: $saleId, status: $status }
    dateRange: { from: $from, to: $to }
  ) {
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
        customerName
        saleName
        status
        totalPrice
        itemsCount
        createdAt
      }
    }
  }
}";

            var variables = new
            {
                page = opt.Page,
                pageSize = opt.PageSize,
                customerId = opt.CustomerId,
                saleId = opt.SaleId,
                status = string.IsNullOrWhiteSpace(opt.Status) ? null : opt.Status,
                // send date-only strings to match GraphQL Date scalar
                from = fromStr,
                to = toStr
            };

            var payload = new
            {
                query,
                variables
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);
            Debug.WriteLine("[GetOrders Request Body] " + json);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var httpResponse = await _httpClient.PostAsync(graphQlEndpoint, content);
            var responseString = await httpResponse.Content.ReadAsStringAsync();

            Debug.WriteLine("[GetOrders Response] Status: " + ((int)httpResponse.StatusCode) + " Body: " + responseString);

            if (!httpResponse.IsSuccessStatusCode)
            {
                return new ApiResult<PagedOrderResult>
                {
                    StatusCode = (int)httpResponse.StatusCode,
                    Success = false,
                    Message = responseString
                };
            }

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var gql = JsonSerializer.Deserialize<GraphQlResponse<OrdersRoot>>(responseString, options);

            if (gql == null)
            {
                return new ApiResult<PagedOrderResult>
                {
                    StatusCode =500,
                    Success = false,
                    Message = "Cannot parse GraphQL response"
                };
            }

            if (gql.Errors != null && gql.Errors.Length >0)
            {
                return new ApiResult<PagedOrderResult>
                {
                    StatusCode =500,
                    Success = false,
                    Message = gql.Errors[0].Message
                };
            }

            var apiResult = gql.Data!.Orders;
            return new ApiResult<PagedOrderResult>
            {
                StatusCode = apiResult.StatusCode,
                Success = apiResult.Success,
                Message = apiResult.Message,
                Data = apiResult.Data
            };
        }

        // -------- GET BY ID ----------
        public async Task<ApiResult<OrderDetailDto>> GetOrderByIdAsync(int orderId)
        {
            const string query = @"
query GetOrderById($id:Int!) {
  orderById(orderId:$id) {
    statusCode
    success
    message
    data {
      orderId
      customerId
      customerName
      customerPhone
      saleId
      saleName
      status
      totalPrice
      createdAt
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
                return data.OrderById ?? new ApiResult<OrderDetailDto>
                {
                    Success = false,
                    Message = "No orderById field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrderDetailDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // -------- CREATE ----------
        public async Task<ApiResult<OrderDetailDto>> CreateOrderAsync(OrderCreateInput input)
        {
            const string query = @"
mutation CreateOrder($customerId:Int!, $saleId:Int!, $items:[OrderItemInput!]!) {
  createOrder(
    input: {
      customerId: $customerId
      saleId: $saleId
      items: $items
    }
  ) {
    statusCode
    success
    message
    data {
      orderId
      customerName
      saleName
      status
      totalPrice
      createdAt
      items {
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
                customerId = input.CustomerId,
                saleId = input.SaleId,
                items = input.Items.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray()
            };

            try
            {
                var data = await SendGraphQlAsync<CreateOrderData>(query, variables);
                return data.CreateOrder ?? new ApiResult<OrderDetailDto>
                {
                    Success = false,
                    Message = "No createOrder field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrderDetailDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // -------- UPDATE (đổi status / items) ----------
        public async Task<ApiResult<OrderDetailDto>> UpdateOrderAsync(int orderId, OrderUpdateInput input)
        {
            const string query = @"
mutation UpdateOrder($id:Int!, $status:OrderStatus, $items:[OrderItemInput!]) {
  updateOrder(
    orderId: $id,
    input: {
      status: $status
      items: $items
    }
  ) {
    statusCode
    success
    message
    data {
      orderId
      status
      totalPrice
      items {
        productId
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
                status = input.Status,
                items = input.Items?.Select(i => new { productId = i.ProductId, quantity = i.Quantity }).ToArray()
            };

            try
            {
                var data = await SendGraphQlAsync<UpdateOrderData>(query, variables);
                return data.UpdateOrder ?? new ApiResult<OrderDetailDto>
                {
                    Success = false,
                    Message = "No updateOrder field in response."
                };
            }
            catch (Exception ex)
            {
                return new ApiResult<OrderDetailDto>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }

        // -------- DELETE ----------
        public async Task<ApiResult<bool>> DeleteOrderAsync(int orderId)
        {
            const string query = @"
mutation DeleteOrder($id:Int!) {
  deleteOrder(orderId:$id) {
    statusCode
    success
    message
  }
}";

            var variables = new { id = orderId };

            try
            {
                var data = await SendGraphQlAsync<DeleteOrderData>(query, variables);
                var res = data.DeleteOrder ?? new ApiResult<object>
                {
                    Success = false,
                    Message = "No deleteOrder field in response."
                };

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
                return new ApiResult<bool>
                {
                    Success = false,
                    Message = ex.Message
                };
            }
        }
    }
}
