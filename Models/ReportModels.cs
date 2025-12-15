using System;
using System.Collections.Generic;

namespace MyShopClient.Models
{
    // Điểm dữ liệu bán hàng theo thời gian của 1 sản phẩm
    public class ProductSalesPointDto
    {
        public string Period { get; set; } = string.Empty; // ví dụ: 2025-01, 2025-01-01 ...
        public int Value { get; set; }                    // số lượng bán
    }

    // Series bán hàng cho 1 sản phẩm
    public class ProductSalesSeriesDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public List<ProductSalesPointDto> Points { get; set; } = new();
    }

    // Điểm dữ liệu doanh thu / lợi nhuận
    public class RevenueProfitPointDto
    {
        public string Period { get; set; } = string.Empty; // yyyy-MM, yyyy-MM-dd ...
        public int Revenue { get; set; }
        public int Profit { get; set; }
    }

    // Options chung cho 2 report
    public class ReportQueryOptions
    {
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        // DAY / WEEK / MONTH / YEAR (enum GraphQL trên server)
        public string GroupBy { get; set; } = "MONTH";

        public int? Top { get; set; }       // top sản phẩm
        public int? CategoryId { get; set; } // nếu cần filter theo category
    }

    // Dashboard Overview
    public class ReportOverviewDto
    {
        public int TotalProducts { get; set; }
        public int TotalOrdersToday { get; set; }
        public decimal RevenueToday { get; set; }
    }

    public class ReportOverviewResult : ApiResult<ReportOverviewDto> { }

    // Low Stock Products
    public class LowStockProductDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
    }

    public class ReportLowStockResult : ApiResult<List<LowStockProductDto>> { }

    // Top Selling Products
    public class TopSellingProductDto
    {
        public int ProductId { get; set; }
        public string Sku { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TotalQuantity { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class ReportTopSellingResult : ApiResult<List<TopSellingProductDto>> { }

    // Recent Orders
    public class RecentOrderDto
    {
        public int OrderId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string SaleName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
        public string CreatedAt { get; set; } = string.Empty;
    }

    public class ReportRecentOrdersResult : ApiResult<List<RecentOrderDto>> { }

    // Daily Revenue
    public class DailyRevenueDto
    {
        public string Date { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
    }

    public class ReportDailyRevenueResult : ApiResult<List<DailyRevenueDto>> { }
}
