namespace MyShopClient.Models.Common
{
    /// <summary>
    /// Generic API response wrapper
    /// </summary>
    public class ApiResult<T>
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
 public T? Data { get; set; }
    }
}
