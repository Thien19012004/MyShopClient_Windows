using System.Collections.Generic;

namespace MyShopClient.Models.Kpi
{
    public class KpiTierPageResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<KpiTierDto> Items { get; set; } = new();
    }

    public class SaleKpiTargetPageResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<SaleKpiTargetDto> Items { get; set; } = new();
    }

    public class KpiCommissionPageResult
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<KpiCommissionDto> Items { get; set; } = new();
    }
}
