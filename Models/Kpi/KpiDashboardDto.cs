using System;
using System.Collections.Generic;

namespace MyShopClient.Models.Kpi
{
    public class KpiDashboardDto
    {
        public int SaleId { get; set; }
        public string SaleName { get; set; } = string.Empty;
        public int CurrentYear { get; set; }
        public int CurrentMonth { get; set; }
        public decimal TargetRevenue { get; set; }
        public decimal ActualRevenue { get; set; }
        public decimal Progress { get; set; }
        public decimal RemainingRevenue { get; set; }
        public decimal EstimatedBaseCommission { get; set; }
        public decimal EstimatedBonusCommission { get; set; }
        public decimal EstimatedTotalCommission { get; set; }
        public int? CurrentKpiTierId { get; set; }
        public string? CurrentKpiTierName { get; set; }
        public int TotalOrdersThisMonth { get; set; }
        public int TotalOrdersPaid { get; set; }
        public List<KpiTierDto> AvailableTiers { get; set; } = new();
    }
}
