namespace MyShopClient.Models
{
    // Dùng chung cho mọi API trả về: statusCode, success, message, data
    public class ApiResult<T>
    {
        public int StatusCode { get; set; }
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
    }

    public class GraphQlResponse<T>
    {
        public T? Data { get; set; }
        public GraphQlError[]? Errors { get; set; }
    }

    public class GraphQlError
    {
        public string Message { get; set; } = string.Empty;
    }
}
