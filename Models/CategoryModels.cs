using System.Collections.Generic;

namespace MyShopClient.Models
{
  public class CategoryItemDto
    {
        public int CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
  }

    public class CategoryPageDto
    {
    public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages { get; set; }
        public List<CategoryItemDto> Items { get; set; } = new();
    }

    public class CategoryCreateInput
    {
        public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

    public class CategoryUpdateInput
    {
      public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
