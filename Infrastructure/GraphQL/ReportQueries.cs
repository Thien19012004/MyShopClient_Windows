namespace MyShopClient.Infrastructure.GraphQL
{
 public static class ReportQueries
 {
 public const string ProductSalesQuery = @"
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

 public const string RevenueProfitQuery = @"
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

 public const string OverviewQuery = @"
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

 public const string LowStockQuery = @"
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

 public const string TopSellingQuery = @"
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

 public const string RecentOrdersQuery = @"
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

 public const string DailyRevenueQuery = @"
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
 }
}
