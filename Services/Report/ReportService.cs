using MyShopClient.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Diagnostics;
using MyShopClient.Infrastructure.GraphQL;

namespace MyShopClient.Services.Report
{
    public class ReportService : IReportService
    {
        private readonly HttpClient _httpClient;
        private readonly IServerConfigService _config;
        private readonly JsonSerializerOptions _jsonOptions;
        private readonly IGraphQLClient _gql;

        public ReportService(HttpClient httpClient, IServerConfigService config, IGraphQLClient gql)
        {
            _httpClient = httpClient;
            _config = config;
            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
                // removed JsonStringEnumConverter: we'll send enum names as proper strings
            };
            _gql = gql;
        }

        private static string NormalizeGroupBy(string? group)
        {
            if (string.IsNullOrWhiteSpace(group)) return "MONTH";
            // Server expects enum names like DAY, WEEK, MONTH, YEAR (unified uppercase)
            return group.Trim().ToUpperInvariant();
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
            var query = ReportQueries.ProductSalesQuery;
            var variables = new
            {
                from = opt.FromDate.ToString("yyyy-MM-dd"),
                to = opt.ToDate.ToString("yyyy-MM-dd"),
                groupBy = NormalizeGroupBy(opt.GroupBy),
                top = opt.Top,
                categoryId = opt.CategoryId
            };

            var data = await _gql.SendAsync<ProductSalesRoot>(query, variables);
            var inner = data?.ReportProductSalesSeries;

            if (inner == null)
            {
                return new ApiResult<List<ProductSalesSeriesDto>>
                {
                    Success = false,
                    StatusCode =500,
                    Message = "No reportProductSalesSeries field"
                };
            }

            return inner;
        }

        public async Task<ApiResult<List<RevenueProfitPointDto>>> GetRevenueProfitSeriesAsync(ReportQueryOptions opt)
        {
            var query = ReportQueries.RevenueProfitQuery;
            var variables = new
            {
                from = opt.FromDate.ToString("yyyy-MM-dd"),
                to = opt.ToDate.ToString("yyyy-MM-dd"),
                groupBy = NormalizeGroupBy(opt.GroupBy)
            };

            var data = await _gql.SendAsync<RevenueProfitRoot>(query, variables);
            var inner = data?.ReportRevenueProfitSeries;

            if (inner == null)
            {
                return new ApiResult<List<RevenueProfitPointDto>>
                {
                    Success = false,
                    StatusCode =500,
                    Message = "No reportRevenueProfitSeries field"
                };
            }

            return inner;
        }

        public async Task<ApiResult<ReportOverviewDto>> GetOverviewAsync()
        {
            var query = ReportQueries.OverviewQuery;
            var data = await _gql.SendAsync<OverviewRoot>(query, new { });

            var result = data?.ReportOverview;
            if (result == null || !result.Success)
            {
                return new ApiResult<ReportOverviewDto>
                {
                    Success = false,
                    StatusCode = result?.StatusCode ??500,
                    Message = result?.Message ?? "Failed to load overview"
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

        public async Task<ApiResult<List<LowStockProductDto>>> GetLowStockProductsAsync(int threshold =5, int take =5)
        {
            var query = ReportQueries.LowStockQuery;
            var variables = new { threshold, take };
            var data = await _gql.SendAsync<LowStockRoot>(query, variables);

            var result = data?.ReportLowStockProducts;
            if (result == null || !result.Success)
            {
                return new ApiResult<List<LowStockProductDto>>
                {
                    Success = false,
                    StatusCode = result?.StatusCode ??500,
                    Message = result?.Message ?? "No data"
                };
            }

            return new ApiResult<List<LowStockProductDto>>
            {
                Success = true,
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            };
        }

        public async Task<ApiResult<List<TopSellingProductDto>>> GetTopSellingProductsAsync(string fromDate, string toDate, int take =5)
        {
            var query = ReportQueries.TopSellingQuery;
            var variables = new { from = fromDate, to = toDate, take };
            var data = await _gql.SendAsync<TopSellingRoot>(query, variables);

            var result = data?.ReportTopSellingProducts;
            if (result == null || !result.Success)
            {
                return new ApiResult<List<TopSellingProductDto>>
                {
                    Success = false,
                    StatusCode = result?.StatusCode ??500,
                    Message = result?.Message ?? "No data"
                };
            }

            return new ApiResult<List<TopSellingProductDto>>
            {
                Success = true,
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            };
        }

        public async Task<ApiResult<List<RecentOrderDto>>> GetRecentOrdersAsync(int take =3)
        {
            var query = ReportQueries.RecentOrdersQuery;
            var variables = new { take };
            var data = await _gql.SendAsync<RecentOrdersRoot>(query, variables);

            var result = data?.ReportRecentOrders;
            if (result == null || !result.Success)
            {
                return new ApiResult<List<RecentOrderDto>>
                {
                    Success = false,
                    StatusCode = result?.StatusCode ??500,
                    Message = result?.Message ?? "No data"
                };
            }

            return new ApiResult<List<RecentOrderDto>>
            {
                Success = true,
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            };
        }

        public async Task<ApiResult<List<DailyRevenueDto>>> GetDailyRevenueInMonthAsync(int year, int month)
        {
            var query = ReportQueries.DailyRevenueQuery;
            var variables = new { year, month };
            var data = await _gql.SendAsync<DailyRevenueRoot>(query, variables);

            var result = data?.ReportDailyRevenueInMonth;
            if (result == null || !result.Success)
            {
                return new ApiResult<List<DailyRevenueDto>>
                {
                    Success = false,
                    StatusCode = result?.StatusCode ??500,
                    Message = result?.Message ?? "No data"
                };
            }

            return new ApiResult<List<DailyRevenueDto>>
            {
                Success = true,
                StatusCode = result.StatusCode,
                Message = result.Message,
                Data = result.Data
            };
        }
    }
}
