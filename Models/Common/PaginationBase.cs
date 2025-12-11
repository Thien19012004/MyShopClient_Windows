using System.Collections.Generic;

namespace MyShopClient.Models.Common
{
    /// <summary>
    /// Base class for paginated results
    /// </summary>
    public abstract class PaginationBase<T>
    {
        public int Page { get; set; }
   public int PageSize { get; set; }
        public int TotalItems { get; set; }
  public int TotalPages { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
