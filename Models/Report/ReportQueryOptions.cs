namespace MyShopClient.Models
{
 public class ReportQueryOptions
 {
 public System.DateTime FromDate { get; set; }
 public System.DateTime ToDate { get; set; }
 // DAY / WEEK / MONTH / YEAR (enum GraphQL on server)
 public string GroupBy { get; set; } = "MONTH";

 public int? Top { get; set; }
 public int? CategoryId { get; set; }
 }
}
