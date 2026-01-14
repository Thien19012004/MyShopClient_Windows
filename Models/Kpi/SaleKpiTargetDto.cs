using System;

namespace MyShopClient.Models.Kpi
{
    public class SaleKpiTargetDto
    {
        public int SaleKpiTargetId { get; set; }
        public int SaleId { get; set; }
        public string SaleName { get; set; } = string.Empty;
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal Progress { get; set; }
        public int? KpiTierId { get; set; }
        public string? KpiTierName { get; set; }
        public decimal BonusAmount { get; set; }
        public DateTime? CalculatedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SetMonthlyTargetInput
    {
        public int SaleId { get; set; }
        public int Year { get; set; }
        public int Month { get; set; }
        public decimal TargetRevenue { get; set; }
    }
}
