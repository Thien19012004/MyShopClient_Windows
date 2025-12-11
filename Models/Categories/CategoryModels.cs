using MyShopClient.Models.Common;
using System.Collections.Generic;

namespace MyShopClient.Models.Categories
{
    /// <summary>
    /// Category item DTO - for list/management
    /// </summary>
    public class CategoryItemDto
    {
        public int CategoryId { get; set; }
   public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int ProductCount { get; set; }
    }

    /// <summary>
    /// Paginated category list response
    /// </summary>
    public class CategoryPageDto : PaginationBase<CategoryItemDto>
    {
    }

    /// <summary>
    /// Input for creating a category
    /// </summary>
  public class CategoryCreateInput
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Input for updating a category
    /// </summary>
    public class CategoryUpdateInput
  {
        public string? Name { get; set; }
        public string? Description { get; set; }
    }
}
