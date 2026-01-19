namespace MyShopClient.Models.Kpi
{
    public class CreateKpiTierInput
    {
        public string Name { get; set; } = string.Empty;
        public decimal MinRevenue { get; set; }
        public decimal BonusPercent { get; set; }
        public string? Description { get; set; }
        public int DisplayOrder { get; set; }
    }

    public class UpdateKpiTierInput
    {
        public string? Name { get; set; }
        public decimal? MinRevenue { get; set; }
        public decimal? BonusPercent { get; set; }
        public string? Description { get; set; }
        public int? DisplayOrder { get; set; }
    }
}
