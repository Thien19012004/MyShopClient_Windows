using MyShopClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;

namespace MyShopClient.Services
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IServerConfigService _config;
        private readonly JsonSerializerOptions _jsonOptions;

        public ReportService(HttpClient httpClient, IServerConfigService config)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
                // removed JsonStringEnumConverter: we'll send enum names as proper strings
            };
        }

        private static string NormalizeGroupBy(string? group)
        {
            if (string.IsNullOrWhiteSpace(group)) return "MONTH";
            // Server expects enum names like DAY, WEEK, MONTH, YEAR (unified uppercase)
            return group.Trim().ToUpperInvariant();
        }

        private async Task<GraphQlResponse<TRoot>?> PostGraphQlAsync<TRoot>(string query, object variables)
        {
            var endpoint = new Uri(new Uri(_config.Current.BaseUrl), _config.GraphQlEndpoint);

            var payload = new
            {
                query,
                variables
            };

            var json = JsonSerializer.Serialize(payload, _jsonOptions);

            // Debug logging: request
            Debug.WriteLine("[ReportService] GraphQL Endpoint: " + endpoint);
            Debug.WriteLine("[ReportService] Request JSON: " + json);

            var content = new StringContent(json, Encoding.UTF8, "application/json");

            HttpResponseMessage response;
            string responseJson;
            try
            {
                response = await _httpClient.PostAsync(endpoint, content);
                responseJson = await response.Content.ReadAsStringAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("[ReportService] HTTP request failed: " + ex.Message);
                throw;
            }

            // Debug logging: response
            Debug.WriteLine("[ReportService] Response status: " + ((int)response.StatusCode) + " " + response.ReasonPhrase);
            Debug.WriteLine("[ReportService] Response JSON: " + responseJson);

            var gql = JsonSerializer.Deserialize<GraphQlResponse<TRoot>>(responseJson, _jsonOptions);

            if (!response.IsSuccessStatusCode)
            {
                var msg = gql?.Errors != null && gql.Errors.Length > 0
                    ? gql.Errors[0].Message
                    : $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

                return new GraphQlResponse<TRoot>
                {
                    Data = default,
                    Errors = new[] { new GraphQlError { Message = msg } }
                };
            }

            return gql;
        }

        // ----- wrappers cho root field -----
        private class ProductSalesRoot
        {
            public ApiResult<List<ProductSalesSeriesDto>>? ReportProductSalesSeries { get; set; }
        }

        private class RevenueProfitRoot
        {
            public ApiResult<List<RevenueProfitPointDto>>? ReportRevenueProfitSeries { get; set; }
        }

        private class OverviewRoot
        {
            public ReportOverviewResult? ReportOverview { get; set; }
        }

        private class LowStockRoot
        {
            public ReportLowStockResult? ReportLowStockProducts { get; set; }
        }

        private class TopSellingRoot
        {
            public ReportTopSellingResult? ReportTopSellingProducts { get; set; }
        }

        private class RecentOrdersRoot
        {
            public ReportRecentOrdersResult? ReportRecentOrders { get; set; }
        }

        private class DailyRevenueRoot
        {
            public ReportDailyRevenueResult? ReportDailyRevenueInMonth { get; set; }
        }

        public async Task<ApiResult<List<ProductSalesSeriesDto>>> GetProductSalesSeriesAsync(ReportQueryOptions opt)
        {
            const string query = @"
query ReportProductSales($from: Date!, $to: Date!, $groupBy: ReportGroupBy!, $top: Int, $categoryId: Int) {
  reportProductSalesSeries(
    dateRange: { from: $from, to: $to }
    groupBy: $groupBy
    filter: { top: $top, categoryId: $categoryId }
  ) {
    statusCode
    success
    message
    data {
      productId
      sku
      name
      points {
        period
        value
      }
    }
  }
}";
            var variables = new
            {
                from = opt.FromDate.ToString("yyyy-MM-dd"),
                to = opt.ToDate.ToString("yyyy-MM-dd"),
                groupBy = NormalizeGroupBy(opt.GroupBy),
                top = opt.Top,
                categoryId = opt.CategoryId
            };

            var gql = await PostGraphQlAsync<ProductSalesRoot>(query, variables);

            if (gql == null)
            {
                return new ApiResult<List<ProductSalesSeriesDto>>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Cannot parse GraphQL response"
                };
            }

            if (gql.Errors != null && gql.Errors.Length > 0)
            {
                return new ApiResult<List<ProductSalesSeriesDto>>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = gql.Errors[0].Message
                };
            }

            return gql.Data!.ReportProductSalesSeries
                   ?? new ApiResult<List<ProductSalesSeriesDto>>
                   {
                       Success = false,
                       StatusCode = 500,
                       Message = "No reportProductSalesSeries field"
                   };
        }

        public async Task<ApiResult<List<RevenueProfitPointDto>>> GetRevenueProfitSeriesAsync(ReportQueryOptions opt)
        {
            const string query = @"
query ReportRevenueProfit($from: Date!, $to: Date!, $groupBy: ReportGroupBy!) {
  reportRevenueProfitSeries(
    dateRange: { from: $from, to: $to }
    groupBy: $groupBy
  ) {
    statusCode
    success
    message
    data {
      period
      revenue
      profit
    }
  }
}";

            var variables = new
            {
                from = opt.FromDate.ToString("yyyy-MM-dd"),
                to = opt.ToDate.ToString("yyyy-MM-dd"),
                groupBy = NormalizeGroupBy(opt.GroupBy)
            };

            var gql = await PostGraphQlAsync<RevenueProfitRoot>(query, variables);

            if (gql == null)
            {
                return new ApiResult<List<RevenueProfitPointDto>>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = "Cannot parse GraphQL response"
                };
            }

            if (gql.Errors != null && gql.Errors.Length > 0)
            {
                return new ApiResult<List<RevenueProfitPointDto>>
                {
                    Success = false,
                    StatusCode = 500,
                    Message = gql.Errors[0].Message
                };
            }

            return gql.Data!.ReportRevenueProfitSeries
                   ?? new ApiResult<List<RevenueProfitPointDto>>
                   {
                       Success = false,
                       StatusCode = 500,
                       Message = "No reportRevenueProfitSeries field"
                   };
        }

        public async Task<ApiResult<ReportOverviewDto>> GetOverviewAsync()
        {
      const string query = @"
query ReportOverview {
  reportOverview {
    statusCode
    success
    message
    data {
totalProducts
      totalOrdersToday
    revenueToday
    }
  }
}";

       var gql = await PostGraphQlAsync<OverviewRoot>(query, new { });

    if (gql == null || gql.Errors != null && gql.Errors.Length > 0)
            {
     return new ApiResult<ReportOverviewDto>
      {
              Success = false,
     StatusCode = 500,
  Message = gql?.Errors?[0].Message ?? "Failed to load overview"
   };
     }

  var result = gql.Data!.ReportOverview;
         if (result == null || !result.Success)
     {
        return new ApiResult<ReportOverviewDto>
     {
   Success = false,
         StatusCode = result?.StatusCode ?? 500,
Message = result?.Message ?? "No data"
        };
 }

 return new ApiResult<ReportOverviewDto>
            {
   Success = true,
        StatusCode = result.StatusCode,
     Message = result.Message,
        Data = result.Data
        };
        }

        public async Task<ApiResult<System.Collections.Generic.List<LowStockProductDto>>> GetLowStockProductsAsync(int threshold = 5, int take = 5)
        {
          const string query = @"
query ReportLowStock($threshold: Int!, $take: Int!) {
  reportLowStockProducts(threshold: $threshold, take: $take) {
    statusCode
    success
message
    data {
      productId
      sku
      name
 stockQuantity
    }
  }
}";

  var variables = new { threshold, take };
        var gql = await PostGraphQlAsync<LowStockRoot>(query, variables);

   if (gql == null || gql.Errors != null && gql.Errors.Length > 0)
        {
      return new ApiResult<System.Collections.Generic.List<LowStockProductDto>>
         {
      Success = false,
            StatusCode = 500,
          Message = gql?.Errors?[0].Message ?? "Failed to load low stock"
          };
    }

         var result = gql.Data!.ReportLowStockProducts;
   if (result == null || !result.Success)
      {
    return new ApiResult<System.Collections.Generic.List<LowStockProductDto>>
                {
      Success = false,
  StatusCode = result?.StatusCode ?? 500,
 Message = result?.Message ?? "No data"
       };
       }

   return new ApiResult<System.Collections.Generic.List<LowStockProductDto>>
{
  Success = true,
       StatusCode = result.StatusCode,
                Message = result.Message,
      Data = result.Data
            };
      }

        public async Task<ApiResult<System.Collections.Generic.List<TopSellingProductDto>>> GetTopSellingProductsAsync(string fromDate, string toDate, int take = 5)
        {
  const string query = @"
query ReportTopSelling($from: Date!, $to: Date!, $take: Int!) {
  reportTopSellingProducts(
    dateRange: { from: $from, to: $to }
    take: $take
  ) {
    statusCode
    success
    message
    data {
      productId
      sku
      name
      totalQuantity
      totalRevenue
    }
  }
}";
            var variables = new { from = fromDate, to = toDate, take };
            var gql = await PostGraphQlAsync<TopSellingRoot>(query, variables);

            if (gql == null || gql.Errors != null && gql.Errors.Length > 0)
    {
       return new ApiResult<System.Collections.Generic.List<TopSellingProductDto>>
                {
          Success = false,
          StatusCode = 500,
       Message = gql?.Errors?[0].Message ?? "Failed to load top selling"
    };
     }

    var result = gql.Data!.ReportTopSellingProducts;
  if (result == null || !result.Success)
            {
   return new ApiResult<System.Collections.Generic.List<TopSellingProductDto>>
  {
              Success = false,
      StatusCode = result?.StatusCode ?? 500,
  Message = result?.Message ?? "No data"
         };
   }

      return new ApiResult<System.Collections.Generic.List<TopSellingProductDto>>
  {
  Success = true,
  StatusCode = result.StatusCode,
 Message = result.Message,
      Data = result.Data
      };
        }

        public async Task<ApiResult<System.Collections.Generic.List<RecentOrderDto>>> GetRecentOrdersAsync(int take = 3)
        {
 const string query = @"
query ReportRecentOrders($take: Int!) {
  reportRecentOrders(take: $take) {
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
    }
  }
}";
        var variables = new { take };
            var gql = await PostGraphQlAsync<RecentOrdersRoot>(query, variables);

            if (gql == null || gql.Errors != null && gql.Errors.Length > 0)
            {
                return new ApiResult<System.Collections.Generic.List<RecentOrderDto>>
    {
        Success = false,
    StatusCode = 500,
             Message = gql?.Errors?[0].Message ?? "Failed to load recent orders"
      };
            }

  var result = gql.Data!.ReportRecentOrders;
      if (result == null || !result.Success)
     {
         return new ApiResult<System.Collections.Generic.List<RecentOrderDto>>
     {
        Success = false,
    StatusCode = result?.StatusCode ?? 500,
    Message = result?.Message ?? "No data"
         };
  }

 return new ApiResult<System.Collections.Generic.List<RecentOrderDto>>
   {
     Success = true,
    StatusCode = result.StatusCode,
        Message = result.Message,
       Data = result.Data
   };
      }

        public async Task<ApiResult<System.Collections.Generic.List<DailyRevenueDto>>> GetDailyRevenueInMonthAsync(int year, int month)
   {
            const string query = @"
query ReportDailyRevenue($year: Int!, $month: Int!) {
  reportDailyRevenueInMonth(year: $year, month: $month) {
    statusCode
    success
    message
    data {
      date
      revenue
    }
  }
}";

        var variables = new { year, month };
            var gql = await PostGraphQlAsync<DailyRevenueRoot>(query, variables);

            if (gql == null || gql.Errors != null && gql.Errors.Length > 0)
   {
         return new ApiResult<System.Collections.Generic.List<DailyRevenueDto>>
{
    Success = false,
       StatusCode = 500,
             Message = gql?.Errors?[0].Message ?? "Failed to load daily revenue"
    };
}

            var result = gql.Data!.ReportDailyRevenueInMonth;
        if (result == null || !result.Success)
            {
            return new ApiResult<System.Collections.Generic.List<DailyRevenueDto>>
      {
        Success = false,
           StatusCode = result?.StatusCode ?? 500,
            Message = result?.Message ?? "No data"
 };
   }

     return new ApiResult<System.Collections.Generic.List<DailyRevenueDto>>
            {
   Success = true,
    StatusCode = result.StatusCode,
                Message = result.Message,
    Data = result.Data
    };
 }
    }
}
