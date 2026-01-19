namespace MyShopClient.Models
{
    // Options query list
    public class CustomerQueryOptions
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string? Search { get; set; }
    }
}
