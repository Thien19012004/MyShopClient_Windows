using System.Collections.Generic;

namespace MyShopClient.Models.Product
{
    public class ProductListResult
    {
        public bool IsSuccess { get; }
        public string? ErrorMessage { get; }
        public IReadOnlyList<ProductItemDto> Items { get; }
        public int TotalCount { get; }

        public ProductListResult(bool isSuccess, string? errorMessage,
            IReadOnlyList<ProductItemDto> items, int totalCount)
        {
            IsSuccess = isSuccess;
            ErrorMessage = errorMessage;
            Items = items;
            TotalCount = totalCount;
        }

        public static ProductListResult Success(
            IReadOnlyList<ProductItemDto> items, int totalCount)
            => new(true, null, items, totalCount);

        public static ProductListResult Fail(string message)
            => new(false, message, new List<ProductItemDto>(), 0);
    }
}
