using System.Collections.Generic;

namespace MyShopClient.Models
{
    // Paged result
    public class CustomerPageDto
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<CustomerListItemDto> Items { get; set; } = new();
    }
}
