using System.Collections.Generic;
using System.Threading.Tasks;
using MyShopClient.Models;

namespace MyShopClient.Services.Report
{
    public interface IReportService
    {
        // Existing methods
        Task<ApiResult<List<ProductSalesSeriesDto>>> GetProductSalesSeriesAsync(ReportQueryOptions options);
        Task<ApiResult<List<RevenueProfitPointDto>>> GetRevenueProfitSeriesAsync(ReportQueryOptions options);

        // Dashboard methods - return ApiResult<TData> not the wrapper Result itself
        Task<ApiResult<ReportOverviewDto>> GetOverviewAsync();
        Task<ApiResult<List<LowStockProductDto>>> GetLowStockProductsAsync(int threshold = 5, int take = 5);
        Task<ApiResult<List<TopSellingProductDto>>> GetTopSellingProductsAsync(string fromDate, string toDate, int take = 5);
        Task<ApiResult<List<RecentOrderDto>>> GetRecentOrdersAsync(int take = 3);
        Task<ApiResult<List<DailyRevenueDto>>> GetDailyRevenueInMonthAsync(int year, int month);
    }
}
