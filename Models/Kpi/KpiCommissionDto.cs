using System;

namespace MyShopClient.Models.Kpi
{
    public class KpiCommissionDto
    {
 public int KpiCommissionId { get; set; }
   public int SaleId { get; set; }
        public string SaleName { get; set; } = string.Empty;
        public int Year { get; set; }
       public int Month { get; set; }
        public decimal BaseCommission { get; set; }
     public decimal BonusCommission { get; set; }
   public decimal TotalCommission { get; set; }
        public int? KpiTierId { get; set; }
        public string? KpiTierName { get; set; }
        public decimal TotalRevenue { get; set; }
        public int TotalOrders { get; set; }
        public DateTime CalculatedAt { get; set; }
  }

    public class CalculateMonthlyKpiInput
    {
   public int Year { get; set; }
 public int Month { get; set; }
     public int? SaleId { get; set; }
    }
}
