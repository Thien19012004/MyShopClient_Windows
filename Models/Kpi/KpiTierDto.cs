namespace MyShopClient.Models.Kpi
{
    public class KpiTierDto
    {
        public int KpiTierId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal MinRevenue { get; set; }
     public decimal BonusPercent { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }
}
